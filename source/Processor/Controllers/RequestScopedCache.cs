
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Request-scoped L1 cache used by CacheController in front of the remote (Redis) tier.
    /// One instance per CacheController, so one per web request. Job 20261126-contensive-render-speed, item 2.
    /// This class has no dependency on CoreController so it can be unit tested on its own (ProcessorTests links this file).
    /// </summary>
    /// <remarks>
    /// Consistency rules:
    /// 1. Within one web request, reads see Redis as it was when each key was first read, plus every write this core made.
    ///    Writes by other processes or other cores during the request are not seen once the key is in L1.
    ///    The process memory tier has the same property today, and requests are short.
    /// 2. Nested cores in the same request (an addon that creates its own CPClass) are non-web cores, so L1 is off for them.
    ///    Their writes go to Redis but are not seen by the web core's L1 if the web core already read that key.
    /// 3. Nothing outlives the request: the owning CacheController clears L1 when it is disposed.
    /// 4. Every write made through the owning CacheController must call setFromWrite (or remove) so a later read in the same
    ///    request never returns a value older than this core's own write.
    /// Each entry holds one form of the document, never both (review m2). A remote read stores only the document it already deserialized.
    /// A write stores only the JSON; the first read deserializes it, keeps the document and drops the JSON. The document is what every
    /// read needs, so keeping the JSON as well would only double the memory.
    /// The document content is a JObject/JArray/primitive, never the typed object, so getObject still builds a new instance per read.
    /// Memory is bounded by an entry cap (maxEntries) and a size budget (maxTotalChars, the sum of the JSON lengths of the stored entries).
    /// Reaching either one clears all entries before the add (SetResult.StoredAfterClear). Clearing is always safe: a cleared key is read from Redis again.
    /// Every operation takes a lock. A web core is normally used by one thread, but addons may start threads with the same cp.
    /// </remarks>
    internal sealed class RequestScopedCache {
        //
        /// <summary>
        /// When an add would go past this count, all entries are cleared first.
        /// </summary>
        public const int maxEntries = 5000;
        //
        /// <summary>
        /// Documents whose JSON is longer than this are not stored. Large documents are kept by CacheRuntimeController already.
        /// </summary>
        public const int maxEntryChars = 262144;
        //
        /// <summary>
        /// Size budget for one request: the sum of the JSON lengths (chars) of all stored entries. When an add would go past it, all entries are cleared first.
        /// The JSON length is used as the size measure for an entry whether it holds the JSON or the deserialized document.
        /// </summary>
        public const int maxTotalChars = 4194304;
        //
        /// <summary>
        /// result of an add, so the caller can update its counters
        /// </summary>
        public enum SetResult {
            /// <summary>The entry was stored.</summary>
            Stored,
            /// <summary>The entry cap or the size budget was reached. All entries were cleared, then the entry was stored.</summary>
            StoredAfterClear,
            /// <summary>The entry was too large. It was not stored and any older entry for the key was removed.</summary>
            Skipped
        }
        //
        private sealed class Entry {
            public string json;
            public CacheDocumentClass doc;
            public bool isMiss;
            public int chars;
        }
        //
        private readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
        private readonly object sync = new();
        private long _clears;
        private long _totalChars;
        //
        // ====================================================================================================
        /// <summary>
        /// number of entries
        /// </summary>
        public int count { get { lock (sync) { return entries.Count; } } }
        //
        /// <summary>
        /// number of times the entry cap or the size budget cleared all entries
        /// </summary>
        public long capClears { get { lock (sync) { return _clears; } } }
        //
        /// <summary>
        /// the sum of the JSON lengths (chars) of the stored entries, compared with maxTotalChars
        /// </summary>
        public long totalChars { get { lock (sync) { return _totalChars; } } }
        //
        /// <summary>
        /// Test hook. Number of entries that still hold JSON (written and not read yet). A remote-read entry or a read entry holds only the document.
        /// </summary>
        public int jsonEntryCount {
            get {
                lock (sync) {
                    int n = 0;
                    foreach (Entry e in entries.Values) {
                        if (e.json != null) { n++; }
                    }
                    return n;
                }
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Read an entry. Returns false if the key is not in L1.
        /// If true and isMiss is true, Redis returned null for the key earlier in this request; doc is null.
        /// If true and isMiss is false, doc is the document (deserialized from the stored JSON on first read, then memoized).
        /// </summary>
        public bool tryGet(string hash, out CacheDocumentClass doc, out bool isMiss) {
            doc = null;
            isMiss = false;
            if (hash == null) { return false; }
            lock (sync) {
                if (!entries.TryGetValue(hash, out Entry entry)) { return false; }
                if (entry.isMiss) {
                    isMiss = true;
                    return true;
                }
                if (entry.doc == null) {
                    entry.doc = Newtonsoft.Json.JsonConvert.DeserializeObject<CacheDocumentClass>(entry.json);
                    if (entry.doc == null) {
                        //
                        // -- json was "null" or empty, treat as not in L1
                        removeEntry(hash);
                        return false;
                    }
                    //
                    // -- keep only the document from now on
                    entry.json = null;
                }
                doc = entry.doc;
                return true;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Store the result of a successful Redis read. doc is the document already deserialized from json. Only the document is kept; json is used for its length.
        /// </summary>
        public SetResult setFromRemoteRead(string hash, string json, CacheDocumentClass doc) {
            if (doc == null || string.IsNullOrEmpty(json)) {
                remove(hash);
                return SetResult.Skipped;
            }
            return set(hash, new Entry { json = null, doc = doc, isMiss = false }, json.Length);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Store a negative entry: Redis returned null for this key in this request.
        /// </summary>
        public SetResult setMiss(string hash) {
            return set(hash, new Entry { isMiss = true }, 0);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Store a write made by the owning core. json is the document JSON as written to Redis. It is deserialized on the next read.
        /// </summary>
        public SetResult setFromWrite(string hash, string json) {
            if (string.IsNullOrEmpty(json)) {
                remove(hash);
                return SetResult.Skipped;
            }
            return set(hash, new Entry { json = json, doc = null, isMiss = false }, json.Length);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Remove the entry for a key, if present.
        /// </summary>
        public void remove(string hash) {
            if (hash == null) { return; }
            lock (sync) { removeEntry(hash); }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Remove all entries. Does not count as a cap clear.
        /// </summary>
        public void clear() {
            lock (sync) {
                entries.Clear();
                _totalChars = 0;
            }
        }
        //
        // ====================================================================================================
        //
        // -- call inside lock(sync)
        private void removeEntry(string hash) {
            if (entries.TryGetValue(hash, out Entry old)) {
                _totalChars -= old.chars;
                entries.Remove(hash);
            }
        }
        //
        private SetResult set(string hash, Entry entry, int jsonLength) {
            if (hash == null) { return SetResult.Skipped; }
            lock (sync) {
                //
                // -- the new entry replaces any older one for this key. Remove it first, so an oversize skip cannot leave an older value behind
                removeEntry(hash);
                if (!entry.isMiss && (jsonLength > maxEntryChars)) {
                    return SetResult.Skipped;
                }
                entry.chars = entry.isMiss ? 0 : jsonLength;
                SetResult result = SetResult.Stored;
                if ((entries.Count >= maxEntries) || (_totalChars + entry.chars > maxTotalChars)) {
                    entries.Clear();
                    _totalChars = 0;
                    _clears++;
                    result = SetResult.StoredAfterClear;
                }
                entries[hash] = entry;
                _totalChars += entry.chars;
                return result;
            }
        }
    }
}
