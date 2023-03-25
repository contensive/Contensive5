
using System;
using System.Collections.Generic;
using Contensive.BaseClasses;

namespace Contensive.HtmlImport {
    namespace Controllers {
        // 
        // ====================================================================================================
        /// <summary>
        ///     ''' 
        ///     ''' </summary>
        ///     ''' <remarks></remarks>
        public class ApplicationController : IDisposable {
            // 
            private readonly CPBaseClass cp;
            // 
            // ====================================================================================================
            /// <summary>
            /// Errors accumulated during rendering.
            /// </summary>
            public List<ResponseErrorClass> responseErrorList { get; set; } = new List<ResponseErrorClass>();
            // 
            // ====================================================================================================
            /// <summary>
            /// data accumulated during rendering
            /// </summary>
            public List<ResponseNodeClass> responseNodeList { get; set; } = new List<ResponseNodeClass>();
            // 
            // ====================================================================================================
            /// <summary>
            /// list of name/time used to performance analysis
            /// </summary>
            public List<ResponseProfileClass> responseProfileList { get; set; } = new List<ResponseProfileClass>();
            // 
            // ====================================================================================================
            /// <summary>
            /// Constructor, authentication can be disabled
            /// </summary>
            /// <param name="cp"></param>
            /// <param name="requiresAuthentication"></param>
            public ApplicationController(CPBaseClass cp, bool requiresAuthentication) {
                this.cp = cp;
                if ((requiresAuthentication & !cp.User.IsAuthenticated)) {
                    throw new UnauthorizedAccessException();
                }
            }
            // 
            // ====================================================================================================
            /// <summary>
            /// constructor, authentication required
            /// </summary>
            /// <param name="cp"></param>
            public ApplicationController(CPBaseClass cp) {
                this.cp = cp;
            }
            // 
            // ==========================================================================================
            // -- Disposable support
            //
            protected bool disposed = false;
            /// <summary>
            /// dispose
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void dispose(bool disposing) {
                if (!this.disposed) {
                    if (disposing) {
                        //
                        // -- dispose non-managed resources
                    }
                }
                this.disposed = true;
            }
            // Do not change or add Overridable to these methods.
            // Put cleanup code in Dispose(ByVal disposing As Boolean).
            public void Dispose() {
                dispose(true);
                GC.SuppressFinalize(this);
            }
            ~ApplicationController() {
                dispose(false);
                //base.Finalize();
            }
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// list of events and their stopwatch times
        /// </summary>
        [Serializable()]
        public class ResponseProfileClass {
            public string name;
            public long time;
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// data store for jsonPackage
        /// </summary>
        [Serializable()]
        public class ResponseNodeClass {
            public string dataFor = "";
            public object data; // IEnumerable(Of Object)
        }
        // 
        // ====================================================================================================
        /// <summary>
        ///         ''' error list for jsonPackage
        ///         ''' </summary>
        [Serializable()]
        public class ResponseErrorClass {
            public int number = 0;
            public string description = "";
        }
    }
}
