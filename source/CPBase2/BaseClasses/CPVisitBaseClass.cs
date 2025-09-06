
using System;

namespace Contensive.BaseClasses {
    public abstract class CPVisitBaseClass {
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
        /// return true if the visit supports cookies
        /// </summary>
        public abstract bool CookieSupport { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Get the visit property that matches the key. If not found set and return the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract string GetText(string key, string defaultValue);
        /// <summary>
        /// Get the visit property that matches the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract string GetText(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Get the visit property that matches the key. If not found set and return the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key, bool defaultValue);
        /// <summary>
        /// Get the visit property that matches the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Get the visit property that matches the key. If not found set and return the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key, DateTime defaultValue);
        /// <summary>
        /// Get the visit property that matches the key.
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
        /// Get the visit property that matches the key. If not found set and return the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key, int defaultValue);
        /// <summary>
        /// Get the visit property that matches the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Get the visit property that matches the key. If not found set and return the default value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key, double defaultValue);
        /// <summary>
        /// Get the visit property that matches the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Return the visit id
        /// </summary>
        public abstract int Id { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The time of the last hit
        /// </summary>
        public abstract DateTime LastTime { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Return the number of login attempts
        /// </summary>
        public abstract int LoginAttempts { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Return the name of the visit
        /// </summary>
        public abstract string Name { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Return the number of hits to the application
        /// </summary>
        public abstract int Pages { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Return the referer for the visit
        /// </summary>
        public abstract string Referer { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Set a text value for the visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, string value);
        //
        //====================================================================================================
        /// <summary>
        /// Set a property value for the visit for a specific visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="targetVisitId"></param>
        public abstract void SetProperty(string key, string value, int targetVisitId);
        //
        //====================================================================================================
        /// <summary>
        /// Set a property value for the current visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, int value);
        //
        //====================================================================================================
        /// <summary>
        /// Set a property value for a specific visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="targetVisitId"></param>
        public abstract void SetProperty(string key, int value, int targetVisitId);
        //
        //====================================================================================================
        /// <summary>
        /// Set a property value for the current visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, double value);
        //
        //====================================================================================================
        /// <summary>
        /// Set a property value for a specific visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="targetVisitId"></param>
        public abstract void SetProperty(string key, double value, int targetVisitId);
        //
        //====================================================================================================
        /// <summary>
        /// Set a property value for the current visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, DateTime value);
        //
        //====================================================================================================
        /// <summary>
        /// Set a property value for a specific visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="targetVisitId"></param>
        public abstract void SetProperty(string key, DateTime value, int targetVisitId);
        //
        //====================================================================================================
        /// <summary>
        /// Set a property value for the current visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, bool value);
        //
        //====================================================================================================
        /// <summary>
        /// Set a property value for a specific visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="targetVisitId"></param>
        public abstract void SetProperty(string key, bool value, int targetVisitId);
        //
        //
        //====================================================================================================
        /// <summary>
        /// The date when the visit started
        /// </summary>
        public abstract int StartDateValue { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The time when the visit started
        /// </summary>
        public abstract DateTime StartTime { get; }
        //
        //====================================================================================================
        // deprecated
        //
        //
        [Obsolete("Use Get of the correct type", false)]
        public abstract string GetProperty(string key, string defaultValue, int targetVisitId);
        //
        [Obsolete("Use Get of the correct type", false)]
        public abstract string GetProperty(string key, string defaultValue);
        //
        [Obsolete("Use Get of the correct type", false)]
        public abstract string GetProperty(string key);
        //
        [Obsolete("Deprecated. Use the get with the correct default argumnet type", false)]
        public abstract bool GetBoolean(string key, string defaultValue);
        //
        [Obsolete("Deprecated. Use the get with the correct default argumnet type", false)]
        public abstract DateTime GetDate(string key, string defaultValue);
        //
        [Obsolete("Deprecated. Use the get with the correct default argumnet type", false)]
        public abstract int GetInteger(string key, string defaultValue);
        //
        [Obsolete("Deprecated. Use the get with the correct default argumnet type", false)]
        public abstract double GetNumber(string key, string defaultValue);
    }
}

