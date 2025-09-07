
using System;
using Contensive.Processor.Controllers;

namespace Contensive.Processor {
    public class CPUserErrorClass : BaseClasses.CPUserErrorBaseClass {
        //
        private readonly CPClass cp;
        protected bool disposed;
        //
        //====================================================================================================
        //
        public CPUserErrorClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        //
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
               if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
        }
        //
        //====================================================================================================
        //
        public override void Add(string message) {
            ErrorController.addUserError(cp.core, message);
        }
        //
        //====================================================================================================
        //
        public override string GetList()  {
            return ErrorController.getUserError(cp.core);
        }
        //
        //====================================================================================================
        //
        public override bool OK()  {
            return cp.core.doc.userErrorList.Count.Equals(0);
        }
    }
}