﻿
using System;

namespace Contensive.BaseClasses {
    public abstract class CPVisitorBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// set the visit property to force the mobile state true
        /// </summary>
        public abstract bool ForceBrowserMobile { get; set; }
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
        /// Set the key value for this visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, string value);
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
        /// Set the key value for this visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, int value);
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
        /// Set the key value for this visitor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, DateTime value);
        //
        //====================================================================================================
        /// <summary>
        /// return the id of the last user authenticated to this visitor
        /// </summary>
        public abstract int UserId { get; }
        //
        //====================================================================================================
        // deprecated
        //
        [Obsolete("Use the Get that returns the appropriate type.")]
        public abstract string GetProperty(string key, string defaultValue, int targetVisitorId);
        //
        [Obsolete("Use the Get that returns the appropriate type.")]
        public abstract string GetProperty(string key, string defaultValue);
        //
        [Obsolete("Use the Get that returns the appropriate type.")]
        public abstract string GetProperty(string key);
        //
        [Obsolete("Use the Get that returns the appropriate defaultvalue type.")]
        public abstract bool GetBoolean(string key, string defaultValue);
        //
        [Obsolete("Use the Get that returns the appropriate defaultvalue type.")]
        public abstract DateTime GetDate(string key, string defaultValue);
        //
        [Obsolete("Use the Get that returns the appropriate defaultvalue type.")]
        public abstract int GetInteger(string key, string defaultValue);
        //
        [Obsolete("Use the Get that returns the appropriate defaultvalue type.")]
        public abstract double GetNumber(string key, string defaultValue);
        //
        [Obsolete("Cannot set the property of another visitor.")]
        public abstract void SetProperty(string key, string value, int targetVisitorid);
    }
}

