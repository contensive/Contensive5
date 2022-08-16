
using System;
using System.Data;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using System.Globalization;
//
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// Manage User, Visit and Visitor properties
    /// </summary>
    //
    public class PropertyModelClass {
        //
        private readonly CoreController core;
        /// <summary>
        /// Type of properties
        /// </summary>
        public enum PropertyTypeEnum {
            /// <summary>
            /// user properties
            /// </summary>
            user = 0,
            /// <summary>
            /// visit properties
            /// </summary>
            visit = 1,
            /// <summary>
            /// visitor properties
            /// </summary>
            visitor = 2
        }
        /// <summary>
        /// The propertyType for instance of PropertyModel 
        /// </summary>
        private readonly PropertyTypeEnum propertyType;
        /// <summary>
        /// The key used for property references from this instance (visitId, visitorId, or memberId)
        /// </summary>
        private readonly int propertyKeyId;
        //
        //
        // todo change array to dictionary
        private string[,] localCache;
        private KeyPtrController propertyCache_nameIndex;
        private bool localCacheLoaded = false;
        private int localCacheCnt;
        //
        //==============================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="core"></param>
        /// <param name="propertyType"></param>
        /// <remarks></remarks>
        public PropertyModelClass(CoreController core, PropertyTypeEnum propertyType) {
            this.core = core;
            this.propertyType = propertyType;
            propertyKeyId = propertyType switch {
                PropertyTypeEnum.visit => core.session.visit.id,
                PropertyTypeEnum.visitor => core.session.visitor.id,
                PropertyTypeEnum.user => core.session.user.id,
                _ => core.session.user.id,
            };
        }
        //
        //====================================================================================================
        /// <summary>
        /// clear a value from the database
        /// </summary>
        /// <param name="key"></param>
        public void clearProperty(string key) {
            if (string.IsNullOrWhiteSpace(key)) { return; }
            if (propertyKeyId <= 0) { return; }
            //
            // -- clear local cache
            setProperty(key, string.Empty);
            //
            // -- remove from db 
            core.db.executeNonQuery("Delete from ccProperties where (TypeID=" + (int)propertyType + ")and(KeyID=" + propertyKeyId + ")and(name=" + DbController.encodeSQLText(key) + ")");
        }
        //
        //====================================================================================================
        /// <summary>
        /// set property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="PropertyValue"></param>
        public void setProperty(string propertyName, double PropertyValue) => setProperty(propertyName, PropertyValue.ToString("R"), propertyKeyId);
        //
        //====================================================================================================
        /// <summary>
        /// set property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="PropertyValue"></param>
        public void setProperty(string propertyName, bool PropertyValue) => setProperty(propertyName, PropertyValue.ToString(CultureInfo.InvariantCulture), propertyKeyId);
        //
        //====================================================================================================
        /// <summary>
        /// set property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="PropertyValue"></param>
        public void setProperty(string propertyName, DateTime PropertyValue) => setProperty(propertyName, PropertyValue.ToString(CultureInfo.InvariantCulture), propertyKeyId);
        //
        //====================================================================================================
        /// <summary>
        /// set property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="PropertyValue"></param>
        public void setProperty(string propertyName, int PropertyValue) => setProperty(propertyName, PropertyValue.ToString(CultureInfo.InvariantCulture), propertyKeyId);
        //
        //====================================================================================================
        /// <summary>
        /// set property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="PropertyValue"></param>
        public void setProperty(string propertyName, string PropertyValue) => setProperty(propertyName, PropertyValue, propertyKeyId);
        //
        //====================================================================================================
        /// <summary>
        /// set property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="PropertyValue"></param>
        public void setProperty(string propertyName, object PropertyValue) {
            setProperty(propertyName, Newtonsoft.Json.JsonConvert.SerializeObject(PropertyValue), propertyKeyId);
        }
        //
        //====================================================================================================
        /// <summary>
        /// set property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <param name="keyId">keyId is like vistiId, vistorId, userId</param>
        public void setProperty(string propertyName, string propertyValue, int keyId) {
            try {
                if (propertyKeyId < 0) { return; }
                if (!localCacheLoaded) { loadLocalCache(keyId); }
                int Ptr = -1;
                if (localCacheCnt > 0) { Ptr = propertyCache_nameIndex.getPtr(propertyName); }
                if (Ptr < 0) {
                    //
                    // -- cache miss, create new property
                    Ptr = localCacheCnt;
                    localCacheCnt += 1;
                    string[,] tempVar = new string[3, Ptr + 1];
                    if (localCache != null) {
                        for (int Dimension0 = 0; Dimension0 < localCache.GetLength(0); Dimension0++) {
                            int CopyLength = Math.Min(localCache.GetLength(1), tempVar.GetLength(1));
                            for (int Dimension1 = 0; Dimension1 < CopyLength; Dimension1++) {
                                tempVar[Dimension0, Dimension1] = localCache[Dimension0, Dimension1];
                            }
                        }
                    }
                    localCache = tempVar;
                    localCache[0, Ptr] = propertyName;
                    localCache[1, Ptr] = propertyValue;
                    propertyCache_nameIndex.setPtr(propertyName, Ptr);
                    //
                    // insert a new property record, get the ID back and save it in cache
                    //
                    using (var csData = new CsModel(core)) {
                        if (csData.insert("Properties")) {
                            localCache[2, Ptr] = csData.getText("ID");
                            csData.set("name", propertyName);
                            csData.set("FieldValue", propertyValue);
                            csData.set("TypeID", (int)propertyType);
                            csData.set("KeyID", keyId.ToString());
                        }
                    }
                    return;
                }
                //
                // -- cache hit, return if no change
                if (localCache[1, Ptr] == propertyValue) { return; }
                //
                // -- cache hit, property changed
                localCache[1, Ptr] = propertyValue;
                //
                // -- save to db
                int RecordId = GenericController.encodeInteger(localCache[2, Ptr]);
                string SQLNow = DbController.encodeSQLDate(core.dateTimeNowMockable);
                core.db.executeNonQuery("update ccProperties set FieldValue=" + DbController.encodeSQLText(propertyValue) + ",ModifiedDate=" + SQLNow + " where id=" + RecordId);
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        public void setProperty(string propertyName, int propertyValue, int keyId)
            => setProperty(propertyName, propertyValue.ToString(CultureInfo.InvariantCulture), keyId);
        //
        public void setProperty(string propertyName, double propertyValue, int keyId)
            => setProperty(propertyName, propertyValue.ToString(CultureInfo.InvariantCulture), keyId);
        //
        public void setProperty(string propertyName, bool propertyValue, int keyId)
            => setProperty(propertyName, propertyValue.ToString(CultureInfo.InvariantCulture), keyId);
        //
        public void setProperty(string propertyName, DateTime propertyValue, int keyId)
            => setProperty(propertyName, propertyValue.ToString(CultureInfo.InvariantCulture), keyId);
        //
        //====================================================================================================
        /// <summary>
        /// get a property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public DateTime getDate(string propertyName) => encodeDate(getText(propertyName, encodeText(DateTime.MinValue), propertyKeyId));
        //
        //====================================================================================================
        /// <summary>
        /// get a property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public DateTime getDate(string propertyName, DateTime defaultValue) => encodeDate(getText(propertyName, encodeText(defaultValue), propertyKeyId));
        //
        //====================================================================================================
        /// <summary>
        /// get a property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public double getNumber(string propertyName) => encodeNumber(getText(propertyName, encodeText(0), propertyKeyId));
        //
        //====================================================================================================
        /// <summary>
        /// get a property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public double getNumber(string propertyName, double defaultValue) => encodeNumber(getText(propertyName, encodeText(defaultValue), propertyKeyId));
        //
        //====================================================================================================
        /// <summary>
        /// get a property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool getBoolean(string propertyName) => encodeBoolean(getText(propertyName, encodeText(false), propertyKeyId));
        //
        //====================================================================================================
        /// <summary>
        /// get a property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public bool getBoolean(string propertyName, bool defaultValue) => encodeBoolean(getText(propertyName, encodeText(defaultValue), propertyKeyId));
        //
        //====================================================================================================
        /// <summary>
        /// get an integer property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public int getInteger(string propertyName) => encodeInteger(getText(propertyName, encodeText(0), propertyKeyId));
        //
        //====================================================================================================
        /// <summary>
        /// get an integer property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public int getInteger(string propertyName, int defaultValue) => encodeInteger(getText(propertyName, encodeText(defaultValue), propertyKeyId));
        //
        //====================================================================================================
        /// <summary>
        /// get a string property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public string getText(string propertyName) => getText(propertyName, "", propertyKeyId);
        //
        //====================================================================================================
        /// <summary>
        /// get a string property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string getText(string propertyName, string defaultValue) => getText(propertyName, defaultValue, propertyKeyId);
        //
        //====================================================================================================
        /// <summary>
        /// get an object property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public T getObject<T>(string propertyName) {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(getText(propertyName, string.Empty, propertyKeyId));
        }
        //
        //====================================================================================================
        /// <summary>
        /// get a string property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="defaultValue"></param>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public string getText(string propertyName, string defaultValue, int keyId) {
            try {
                string returnString = "";
                //
                if (propertyKeyId < 0) { return ""; }
                //
                if (!localCacheLoaded) { loadLocalCache(keyId); }
                //
                int Ptr = -1;
                bool Found = false;
                if (localCacheCnt > 0) {
                    Ptr = propertyCache_nameIndex.getPtr(propertyName);
                    if (Ptr >= 0) {
                        returnString = encodeText(localCache[1, Ptr]);
                        Found = true;
                    }
                }
                //
                if (!Found) {
                    returnString = defaultValue;
                    setProperty(propertyName, defaultValue, keyId);
                }
                return returnString;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// delete all properties for this user or visitor or visit
        /// </summary>
        /// <param name="keyId"></param>
        public void deleteAll(int keyId) {
            if (keyId <= 0) { return; }
            core.db.executeNonQuery("Delete from ccProperties where (TypeID=" + (int)propertyType + ")and(KeyID=" + keyId + ")");
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyId"></param>
        private void loadLocalCache(int keyId) {
            try {
                //
                propertyCache_nameIndex = new KeyPtrController();
                localCacheCnt = 0;
                //
                using (DataTable dt = core.db.executeQuery("select Name,FieldValue,ID from ccProperties where (active<>0)and(TypeID=" + (int)propertyType + ")and(KeyID=" + keyId + ")")) {
                    if (dt.Rows.Count > 0) {
                        localCache = new string[3, dt.Rows.Count];
                        foreach (DataRow dr in dt.Rows) {
                            string Name = GenericController.encodeText(dr[0]);
                            localCache[0, localCacheCnt] = Name;
                            localCache[1, localCacheCnt] = GenericController.encodeText(dr[1]);
                            localCache[2, localCacheCnt] = GenericController.encodeInteger(dr[2]).ToString();
                            propertyCache_nameIndex.setPtr(Name.ToLowerInvariant(), localCacheCnt);
                            localCacheCnt += 1;
                        }
                        localCacheCnt = dt.Rows.Count;
                    }
                }
                localCacheLoaded = true;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
    }
}
