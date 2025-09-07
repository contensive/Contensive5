
using System;
using Contensive.Processor.Controllers;

namespace Contensive.Processor {
    public class CPVisitClass : BaseClasses.CPVisitBaseClass, IDisposable {
        //
        private readonly CPClass cp;
        //
        //=======================================================================================================
        /// <summary>
        /// Clear a property
        /// </summary>
        /// <param name="key"></param>
        public override void ClearProperty(string key) {
            cp.core.visitProperty.clearProperty(key);
        }
        //
        //=======================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cp"></param>
        public CPVisitClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //=======================================================================================================
        /// <summary>
        /// return true if cookies supported
        /// </summary>
        public override bool CookieSupport {
            get {
                return cp.core.session.visit.cookieSupport;
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// return the visit id
        /// </summary>
        public override int Id {
            get {
                if (cp?.core?.session?.visit == null) { return 0; }
                if (cp.core.session.visit.id != 0) { return cp.core.session.visit.id; }
                cp.core.session.verifyUser();
                return cp.core.session.visit.id;
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// return the time of the last visit
        /// </summary>
        public override DateTime LastTime {
            get {
                return GenericController.encodeDate(cp.core.session.visit.lastVisitTime);
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// return the login attempts
        /// </summary>
        public override int LoginAttempts {
            get {
                return cp.core.session.visit.loginAttempts;
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// return the name of the visit
        /// </summary>
        public override string Name {
            get {
                return cp.core.session.visit.name;
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// return the number of hits
        /// </summary>
        public override int Pages {
            get {
                return cp.core.session.visit.pageVisits;
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// return the referer
        /// </summary>
        public override string Referer {
            get {
                return cp.core.session.visit.http_referer;
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// Set a key value pair for this visit
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, string value) {
            cp.core.visitProperty.setProperty(key, value);
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, string value, int TargetVisitId) {
            if (TargetVisitId == 0) {
                cp.core.visitProperty.setProperty(key, value);
            } else {
                cp.core.visitProperty.setProperty(key, value, TargetVisitId);
            }
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, int value) {
            cp.core.visitProperty.setProperty(key, value);
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, int value, int TargetVisitId) {
            if (TargetVisitId == 0) {
                cp.core.visitProperty.setProperty(key, value);
            } else {
                cp.core.visitProperty.setProperty(key, value, TargetVisitId);
            }
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, double value) {
            cp.core.visitProperty.setProperty(key, value);
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, double value, int TargetVisitId) {
            if (TargetVisitId == 0) {
                cp.core.visitProperty.setProperty(key, value);
            } else {
                cp.core.visitProperty.setProperty(key, value, TargetVisitId);
            }
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, bool value) {
            cp.core.visitProperty.setProperty(key, value);
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, bool value, int TargetVisitId) {
            if (TargetVisitId == 0) {
                cp.core.visitProperty.setProperty(key, value);
            } else {
                cp.core.visitProperty.setProperty(key, value, TargetVisitId);
            }
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, DateTime value) {
            cp.core.visitProperty.setProperty(key, value);
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string key, DateTime value, int TargetVisitId) {
            if (TargetVisitId == 0) {
                cp.core.visitProperty.setProperty(key, value);
            } else {
                cp.core.visitProperty.setProperty(key, value, TargetVisitId);
            }
        }
        //
        //=======================================================================================================
        //
        public override bool GetBoolean(string key, bool defaultValue) {
            return cp.core.visitProperty.getBoolean(key, defaultValue);
        }
        //
        public override bool GetBoolean(string key) {
            return cp.core.visitProperty.getBoolean(key);
        }
        //
        //=======================================================================================================
        //
        public override DateTime GetDate(string key, DateTime defaultValue) {
            return cp.core.visitProperty.getDate(key, defaultValue);
        }
        //
        public override DateTime GetDate(string key) {
            return cp.core.visitProperty.getDate(key);
        }
        //
        //=======================================================================================================
        //
        public override int GetInteger(string key, int defaultValue) {
            return cp.core.visitProperty.getInteger(key, defaultValue);
        }
        //
        public override int GetInteger(string key) {
            return cp.core.visitProperty.getInteger(key);
        }
        //
        //=======================================================================================================
        //
        public override double GetNumber(string key, double defaultValue) {
            return cp.core.visitProperty.getNumber(key, defaultValue);
        }
        //
        public override double GetNumber(string key) {
            return cp.core.visitProperty.getNumber(key);
        }
        //
        //=======================================================================================================
        //
        public override string GetText(string key, string defaultValue) {
            return cp.core.visitProperty.getText(key, defaultValue);
        }
        //
        public override string GetText(string key) {
            return cp.core.visitProperty.getText(key);
        }
        //
        //=======================================================================================================
        //
        public override T GetObject<T>(string key) {
            return cp.core.visitProperty.getObject<T>(key);
        }
        //
        //=======================================================================================================
        //
        public override int StartDateValue {
            get {
                return GenericController.encodeInteger(cp.core.doc.profileStartTime.ToOADate());
            }
        }
        //
        //=======================================================================================================
        //
        public override DateTime StartTime {
            get {
                return GenericController.encodeDate(cp.core.session.visit.startTime);
            }
        }
        //
        //=======================================================================================================
        // deprecated
        //
        //
        //
        //
        [Obsolete("Deprecated", true )]
        public override string GetProperty(string key, string defaultValue, int TargetVisitId) {
            if (TargetVisitId == 0) {
                return cp.core.visitProperty.getText(key, defaultValue);
            } else {
                return cp.core.visitProperty.getText(key, defaultValue, TargetVisitId);
            }
        }
        //
        [Obsolete("Deprecated", false)]
        public override string GetProperty(string key, string defaultValue) {
            return cp.core.visitProperty.getText(key, defaultValue);
        }
        //
        [Obsolete("Deprecated", false)]
        public override string GetProperty(string key) {
            return cp.core.visitProperty.getText(key);
        }
        //
        [Obsolete("Deprecated", false)]
        public override bool GetBoolean(string key, string defaultValue) {
            return GetBoolean(key, GenericController.encodeBoolean(defaultValue));
        }
        //
        [Obsolete("Deprecated", false)]
        public override DateTime GetDate(string key, string defaultValue) {
            return GetDate(key, GenericController.encodeDate(defaultValue));
        }
        //
        [Obsolete("Deprecated", false)]
        public override int GetInteger(string key, string defaultValue) {
            return GetInteger(key, GenericController.encodeInteger(defaultValue));
        }
        //
        [Obsolete("Deprecated", false)]
        public override double GetNumber(string key, string defaultValue) {
            return GetNumber(key, GenericController.encodeNumber(defaultValue));
        }
        //
        //=======================================================================================================
        // dispose
        //
        #region  IDisposable Support 
        //
        protected virtual void Dispose(bool disposing_visit) {
            if (!this.disposed_visit) {
                if (disposing_visit) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_visit = true;
        }
        protected bool disposed_visit;
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CPVisitClass()  {
            Dispose(false);
        }
        #endregion
    }
}