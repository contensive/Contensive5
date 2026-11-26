
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
    /// Entries hold the JSON exactly as written to or read from Redis, and the document deserialized from it.
    /// The document content is a JObject/JArray/primitive, never the typed object, so getObject still builds a new instance per read.
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
        /// result of an add, so the caller can update its counters
        /// </summary>
        public enum SetResult {
            /// <summary>The entry was stored.</summary>
            Stored,
            /// <summary>The cap was reached. All entries were cleared, then the entry was stored.</summary>
            StoredAfterClear,
            /// <summary>The entry was too large. It was not stored and any older entry for the key was removed.</summary>
            Skipped
        }
        //
        private sealed class Entry {
            public string json;
            public CacheDocumentClass doc;
            public bool isMiss;
        }
        //
        private readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
        private readonly object sync = new();
        private long _clears;
        //
        // ====================================================================================================
        /// <summary>
        /// number of entries
        /// </summary>
        public int count { get { lock (sync) { return entries.Count; } } }
        //
        /// <summary>
        /// number of times the cap cleared all entries
        /// </summary>
        public long capClears { get { lock (sync) { return _clears; } } }
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
                        entries.Remove(hash);
                        return false;
                    }
                }
                doc = entry.doc;
                return true;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Store the result of a successful Redis read. doc is the document already deserialized from json.
        /// </summary>
        public SetResult setFromRemoteRead(string hash, string json, CacheDocumentClass doc) {
            return set(hash, new Entry { json = json, doc = doc, isMiss = false }, json?.Length ?? 0);
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
            return set(hash, new Entry { json = json, doc = null, isMiss = false }, json?.Length ?? 0);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Remove the entry for a key, if present.
        /// </summary>
        public void remove(string hash) {
            if (hash == null) { return; }
            lock (sync) { entries.Remove(hash); }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Remove all entries. Does not count as a cap clear.
        /// </summary>
        public void clear() {
            lock (sync) { entries.Clear(); }
        }
        //
        // ====================================================================================================
        //
        private SetResult set(string hash, Entry entry, int jsonLength) {
            if (hash == null) { return SetResult.Skipped; }
            lock (sync) {
                if (!entry.isMiss && (string.IsNullOrEmpty(entry.json) || (jsonLength > maxEntryChars))) {
                    //
                    // -- not stored. Remove any older entry so a later read cannot return a value older than this one
                    entries.Remove(hash);
                    return SetResult.Skipped;
                }
                SetResult result = SetResult.Stored;
                if (!entries.ContainsKey(hash) && (entries.Count >= maxEntries)) {
                    entries.Clear();
                    _clears++;
                    result = SetResult.StoredAfterClear;
                }
                entries[hash] = entry;
                return result;
            }
        }
    }
}
