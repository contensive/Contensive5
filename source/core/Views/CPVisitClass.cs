﻿
using System;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Contensive.Processor;
using Contensive.Processor.Models.DbModels;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.genericController;
using static Contensive.Processor.constants;
//
namespace Contensive.Processor {
    public class CPVisitClass : BaseClasses.CPVisitBaseClass, IDisposable {
        //
        #region COM GUIDs
        public const string ClassId = "3562FB08-178D-4AD1-A923-EAEAAF33FE84";
        public const string InterfaceId = "A1CC6FCB-810B-46C4-8232-D3166CACCBAD";
        public const string EventsId = "2AFEB1A8-5B27-45AC-A9DF-F99849BE1FAE";
        #endregion
        //
        private Contensive.Processor.Controllers.coreController core;
        private CPClass cp;
        protected bool disposed = false;
        //
        public CPVisitClass(Contensive.Processor.Controllers.coreController coreObj, CPClass cpParent) : base() {
            this.core = coreObj;
            cp = cpParent;
        }
        //
        // dispose
        //
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                appendDebugLog(".dispose, dereference main, csv");
                if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                    cp = null;
                    core = null;
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
        }

        public override bool CookieSupport
        {
            get {
                return core.session.visit.CookieSupport;
            }
        }
        //
        //
        //
        public override string GetProperty(string PropertyName, string DefaultValue = "", int TargetVisitId = 0) {
            if (TargetVisitId == 0) {
                return core.visitProperty.getText(PropertyName, DefaultValue);
            } else {
                return core.visitProperty.getText(PropertyName, DefaultValue, TargetVisitId);
            }
        }
        //
        //
        //
        public override int Id {
            get {
                return core.session.visit.id;
            }
        }
        //
        //=======================================================================================================
        //
        public override DateTime LastTime
        {
            get {
                return core.session.visit.LastVisitTime;
            }
        }
        //
        //=======================================================================================================
        //
        public override int LoginAttempts
        {
            get {
                return core.session.visit.LoginAttempts;
            }
        }
        //
        //=======================================================================================================
        //
        public override string Name
        {
            get {
                return core.session.visit.name;
            }
        }
        //
        //=======================================================================================================
        //
        public override int Pages
        {
            get {
                return core.session.visit.PageVisits;
            }
        }
        //
        //=======================================================================================================
        //
        public override string Referer
        {
            get {
                return core.session.visit.HTTP_REFERER;
            }
        }
        //
        //=======================================================================================================
        //
        public override void SetProperty(string PropertyName, string Value, int TargetVisitId = 0) {
            if (TargetVisitId == 0) {
                core.visitProperty.setProperty(PropertyName, Value);
            } else {
                core.visitProperty.setProperty(PropertyName, Value, TargetVisitId);
            }
        }
        //
        //=======================================================================================================
        //
        public override bool GetBoolean(string PropertyName, string DefaultValue = "") {
            return genericController.encodeBoolean(GetProperty(PropertyName, DefaultValue));
        }
        //
        //=======================================================================================================
        //
        //=======================================================================================================
        //
        public override DateTime GetDate(string PropertyName, string DefaultValue = "") {
            return genericController.encodeDate(GetProperty(PropertyName, DefaultValue));
        }
        //
        //=======================================================================================================
        //
        //=======================================================================================================
        //
        public override int GetInteger(string PropertyName, string DefaultValue = "") {
            return cp.Utils.EncodeInteger(GetProperty(PropertyName, DefaultValue));
        }
        //
        //=======================================================================================================
        //
        //=======================================================================================================
        //
        public override double GetNumber(string PropertyName, string DefaultValue = "") {
            return cp.Utils.EncodeNumber(GetProperty(PropertyName, DefaultValue));
        }
        //
        //=======================================================================================================
        //
        //=======================================================================================================
        //
        public override string GetText(string FieldName, string DefaultValue = "") {
            return GetProperty(FieldName, DefaultValue);
        }

        public override int StartDateValue
        {
            get {
                return core.session.visit.StartDateValue;
            }
        }

        public override DateTime StartTime
        {
            get {
                return core.session.visit.StartTime;
            }
        }
        //
        //
        //
        private void appendDebugLog(string copy) {
        }
        //
        // testpoint
        //
        private void tp(string msg) {
            //Call appendDebugLog(msg)
        }
        #region  IDisposable Support 
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~CPVisitClass() {
            Dispose(false);
            //todo  NOTE: The base class Finalize method is automatically called from the destructor:
            //base.Finalize();
        }
        #endregion
    }
}