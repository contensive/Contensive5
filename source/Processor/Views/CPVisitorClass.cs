
using System;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor {
    public class CPVisitorClass : BaseClasses.CPVisitorBaseClass, IDisposable {
        //
        private readonly CPClass cp;
        //
        //=======================================================================================================
        /// <summary>
        /// Clear a property
        /// </summary>
        /// <param name="key"></param>
        public override void ClearProperty(string key) {
            cp.core.visitorProperty.clearProperty(key);
        }
        //
        //=======================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        public CPVisitorClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //=======================================================================================================
        /// <summary>
        /// set or get if the browser is forced mobile, once set true (mobile) is cannot be set back to browser
        /// </summary>
        [Obsolete("Deprecated.")]
        public override bool ForceBrowserMobile { get; set; }
        //
        //=======================================================================================================
        /// <summary>
        /// return the visitor property from its key. If missing, set and return the defaultValue.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override bool GetBoolean(string key, bool defaultValue) => cp.core.visitorProperty.getBoolean(key, defaultValue);
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool GetBoolean(string key) => cp.core.visitorProperty.getBoolean(key);
        //
        //=======================================================================================================
        /// <summary>
        /// return the visitor property from its key. If missing, set and return the defaultValue.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override DateTime GetDate(string key, DateTime defaultValue) => cp.core.visitorProperty.getDate(key, defaultValue);
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override DateTime GetDate(string key) => cp.core.visitorProperty.getDate(key);
        //
        //=======================================================================================================
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override T GetObject<T>(string key) => cp.core.visitorProperty.getObject<T>(key);
        //
        //=======================================================================================================
        /// <summary>
        /// return the visitor property from its key. If missing, set and return the defaultValue.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override int GetInteger(string key, int defaultValue) => cp.core.visitorProperty.getInteger(key, defaultValue);
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override int GetInteger(string key) => cp.core.visitorProperty.getInteger(key);
        //
        //=======================================================================================================
        /// <summary>
        /// return the visitor property from its key. If missing, set and return the defaultValue.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override double GetNumber(string key, double defaultValue) => cp.core.visitorProperty.getNumber(key, defaultValue);
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override double GetNumber(string key) => cp.core.visitorProperty.getNumber(key);
        //
        //=======================================================================================================
        /// <summary>
        /// return the visitor property from its key. If missing, set and return the defaultValue.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override string GetText(string key, string defaultValue) => cp.core.visitorProperty.getText(key, defaultValue);
        /// <summary>
        /// return the visitor property from its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override string GetText(string key) => cp.core.visitorProperty.getText(key);
        //
        //=======================================================================================================
        /// <summary>
        /// return the visitor id
        /// </summary>
        public override int Id {
            get {
                return cp.core.session.visitor.id;
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// return true if the visitor is new 
        /// </summary>
        public override bool IsNew {
            get {
                return cp.core.session.visit.visitorNew;
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// set the key value 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, string value) {
            cp.core.visitorProperty.setProperty(key, value);
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, string value, int TargetVisitorid) {
            if (TargetVisitorid == 0) {
                cp.core.visitorProperty.setProperty(key, value);
            } else {
                cp.core.visitorProperty.setProperty(key, value, TargetVisitorid);
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// set the key value 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, bool value) {
            cp.core.visitorProperty.setProperty(key, value);
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, bool value, int TargetVisitorid) {
            if (TargetVisitorid == 0) {
                cp.core.visitorProperty.setProperty(key, value);
            } else {
                cp.core.visitorProperty.setProperty(key, value, TargetVisitorid);
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// set the key value 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, int value) {
            cp.core.visitorProperty.setProperty(key, value);
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, int value, int TargetVisitorid) {
            if (TargetVisitorid == 0) {
                cp.core.visitorProperty.setProperty(key, value);
            } else {
                cp.core.visitorProperty.setProperty(key, value, TargetVisitorid);
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// set the key value 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, double value) {
            cp.core.visitorProperty.setProperty(key, value);
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, double value, int TargetVisitorid) {
            if (TargetVisitorid == 0) {
                cp.core.visitorProperty.setProperty(key, value);
            } else {
                cp.core.visitorProperty.setProperty(key, value, TargetVisitorid);
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// set the key value 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, DateTime value) {
            cp.core.visitorProperty.setProperty(key, value);
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, DateTime value, int TargetVisitorid) {
            if (TargetVisitorid == 0) {
                cp.core.visitorProperty.setProperty(key, value);
            } else {
                cp.core.visitorProperty.setProperty(key, value, TargetVisitorid);
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// return the id of the visitor
        /// </summary>
        public override int UserId {
            get {
                return cp.core.session.visitor.memberId;
            }
        }
        //
        //=======================================================================================================
        // deprecated
        //
        [Obsolete("Cannot set the property of a different visitor.", false)]
        public override string GetProperty(string PropertyName, string DefaultValue, int TargetVisitorId) {
            if (TargetVisitorId == 0) {
                return cp.core.visitorProperty.getText(PropertyName, DefaultValue);
            } else {
                return cp.core.visitorProperty.getText(PropertyName, DefaultValue, TargetVisitorId);
            }
        }
        //
        [Obsolete("Use the get for the appropriate return type.", false)]
        public override string GetProperty(string PropertyName, string DefaultValue) {
            return cp.core.visitorProperty.getText(PropertyName, DefaultValue);
        }
        //
        [Obsolete("Use the get for the appropriate return type.", false)]
        public override string GetProperty(string PropertyName) {
            return cp.core.visitorProperty.getText(PropertyName);
        }
        //
        [Obsolete("Use the get for the appropriate default type.", false)]
        public override DateTime GetDate(string key, string defaultValue) => cp.core.visitorProperty.getDate(key, encodeDate(defaultValue));
        //
        [Obsolete("Use the get for the appropriate default type.", false)]
        public override int GetInteger(string key, string defaultValue) => cp.core.visitorProperty.getInteger(key, encodeInteger(defaultValue));
        //
        [Obsolete("Use the get for the appropriate default type.", false)]
        public override double GetNumber(string key, string defaultValue) => cp.core.visitorProperty.getNumber(key, encodeNumber(defaultValue));
        //
        [Obsolete("Use the get for the appropriate default type.", false)]
        public override bool GetBoolean(string key, string defaultValue) => cp.core.visitorProperty.getBoolean(key, encodeBoolean(defaultValue));
        //
        #region  IDisposable Support 
        //
        // dispose
        //
        protected virtual void Dispose(bool disposing_visitor) {
            if (!this.disposed_visitor) {
                if (disposing_visitor) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_visitor = true;
        }
        protected bool disposed_visitor;
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CPVisitorClass() {
            Dispose(false);
        }
        #endregion
    }
}