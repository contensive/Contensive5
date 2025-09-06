
using System;

namespace Contensive.BaseClasses {
    public abstract class CPVisitorBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// remove the property
        /// </summary>
        /// <param name="key"></param>
        public abstract void ClearProperty(string key);
        //
        //====================================================================================================
        /// <summary>
        /// return the visitor property from its key. If missing, set and return the defaultValue.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract string GetText(string key, string defaultValue);
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract string GetText(string key);
        //
        //====================================================================================================
        /// <summary>
        /// return the visitor property from its key. If missing, set and return the defaultValue.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key, bool defaultValue);
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key);
        //
        //====================================================================================================
        /// <summary>
        /// return the visitor property from its key. If missing, set and return the defaultValue.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key, DateTime defaultValue);
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return the user property for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract T GetObject<T>(string key);
        //
        //====================================================================================================
        /// <summary>
        /// return the visitor property from its key. If missing, set and return the defaultValue.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key, int defaultValue);
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key);
        //
        //====================================================================================================
        /// <summary>
        /// return the visitor property from its key. If missing, set and return the defaultValue.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key, double defaultValue);
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key);
        //
        //====================================================================================================
        /// <summary>
        /// return the visitor id
        /// </summary>
        public abstract int Id { get; }
        //
        //====================================================================================================
        /// <summary>
        ///  return true if this is a new visitor
        /// </summary>
        public abstract bool IsNew { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Set property value for this visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, string value);
        //
        //====================================================================================================
        /// <summary>
        /// Set property value for a specific visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="targetVisitorid"></param>
        public abstract void SetProperty(string key, string value, int targetVisitorid);
        //
        //====================================================================================================
        /// <summary>
        /// Set the key value for this visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, bool value);
        //
        //====================================================================================================
        /// <summary>
        /// Set property value for a specific visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="targetVisitorid"></param>
        public abstract void SetProperty(string key, bool value, int targetVisitorid);
        //
        //====================================================================================================
        /// <summary>
        /// Set the key value for this visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, int value);
        //
        //====================================================================================================
        /// <summary>
        /// Set property value for a specific visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="targetVisitorid"></param>
        public abstract void SetProperty(string key, int value, int targetVisitorid);
        //
        //====================================================================================================
        /// <summary>
        /// Set the key value for this visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, double value);
        //
        //====================================================================================================
        /// <summary>
        /// Set property value for a specific visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="targetVisitorid"></param>
        public abstract void SetProperty(string key, double value, int targetVisitorid);
        //
        //====================================================================================================
        /// <summary>
        /// Set the key value for this visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, DateTime value);
        //
        //====================================================================================================
        /// <summary>
        /// Set property value for a specific visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="targetVisitorid"></param>
        public abstract void SetProperty(string key, DateTime value, int targetVisitorid);
        //
        //====================================================================================================
        /// <summary>
        /// return the id of the last user authenticated to this visitor
        /// </summary>
        public abstract int UserId { get; }
        //
        //====================================================================================================
        // deprecated
        /// <summary>
        /// Use the Get that returns the appropriate defaultvalue type.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="targetVisitorId"></param>
        /// <returns></returns>
        [Obsolete("Use the Get that returns the appropriate type.")]
        public abstract string GetProperty(string key, string defaultValue, int targetVisitorId);
        /// <summary>
        /// Use the Get that returns the appropriate defaultvalue type.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use the Get that returns the appropriate type.")]
        public abstract string GetProperty(string key, string defaultValue);
        /// <summary>
        /// Use the Get that returns the appropriate defaultvalue type.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Obsolete("Use the Get that returns the appropriate type.")]
        public abstract string GetProperty(string key);
        /// <summary>
        /// Use the Get that returns the appropriate defaultvalue type.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use the Get that returns the appropriate defaultvalue type.")]
        public abstract bool GetBoolean(string key, string defaultValue);
        /// <summary>
        /// Use the Get that returns the appropriate defaultvalue type.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use the Get that returns the appropriate defaultvalue type.")]
        public abstract DateTime GetDate(string key, string defaultValue);
        /// <summary>
        /// Use the Get that returns the appropriate defaultvalue type.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use the Get that returns the appropriate defaultvalue type.")]
        public abstract int GetInteger(string key, string defaultValue);
        //
        /// <summary>
        /// Use the Get that returns the appropriate defaultvalue type.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use the Get that returns the appropriate defaultvalue type.")]
        public abstract double GetNumber(string key, string defaultValue);
        //
        /// <summary>
        /// set the visit property to force the mobile state true
        /// </summary>
        [Obsolete("Deprecated.", false)]
        public abstract bool ForceBrowserMobile { get; set; }
    }
}

