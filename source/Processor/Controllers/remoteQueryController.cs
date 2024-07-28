
using System;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Processor.Models.Domain;
//
namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class RemoteQueryController : IDisposable {
        //
        public enum RemoteFormatEnum {
            RemoteFormatJsonTable = 1,
            RemoteFormatJsonNameArray = 2,
            RemoteFormatJsonNameValue = 3
        }
        //
        //
        public static string format(CoreController core, GoogleDataType gd, RemoteFormatEnum RemoteFormat) {
            //
            StringBuilderLegacyController s = null;
            string ColDelim = null;
            string RowDelim = null;
            int ColPtr = 0;
            int RowPtr = 0;
            //
            // Select output format
            //
            s = new StringBuilderLegacyController();
            switch (RemoteFormat) {
                case RemoteFormatEnum.RemoteFormatJsonNameValue:
                    //
                    //
                    //
                    s.add("{");
                    if (!gd.IsEmpty) {
                        ColDelim = "";
                        for (ColPtr = 0; ColPtr <= gd.col.Count; ColPtr++) {
                            s.add(ColDelim + gd.col[ColPtr].Id + ":'" + gd.row[0].Cell[ColPtr].v + "'");
                            ColDelim = ",";
                        }
                    }
                    s.add("}");
                    break;
                case RemoteFormatEnum.RemoteFormatJsonNameArray:
                    //
                    //
                    //
                    s.add("{");
                    if (!gd.IsEmpty) {
                        ColDelim = "";
                        for (ColPtr = 0; ColPtr <= gd.col.Count; ColPtr++) {
                            s.add(ColDelim + gd.col[ColPtr].Id + ":[");
                            ColDelim = ",";
                            RowDelim = "";
                            for (RowPtr = 0; RowPtr <= gd.row.Count ; RowPtr++) {
                                var tempVar = gd.row[RowPtr].Cell[ColPtr];
                                s.add(RowDelim + "'" + tempVar.v + "'");
                                RowDelim = ",";
                            }
                            s.add("]");
                        }
                    }
                    s.add("}");
                    break;
                case RemoteFormatEnum.RemoteFormatJsonTable:
                    //
                    //
                    //
                    s.add("{");
                    if (!gd.IsEmpty) {
                        s.add("cols: [");
                        ColDelim = "";
                        for (ColPtr = 0; ColPtr <= gd.col.Count; ColPtr++) {
                            var tempVar2 = gd.col[ColPtr];
                            s.add(ColDelim + "{id: '" + GenericController.encodeJavascriptStringSingleQuote(tempVar2.Id) + "', label: '" + GenericController.encodeJavascriptStringSingleQuote(tempVar2.Label) + "', type: '" + GenericController.encodeJavascriptStringSingleQuote(tempVar2.Type) + "'}");
                            ColDelim = ",";
                        }
                        s.add("],rows:[");
                        RowDelim = "";
                        for (RowPtr = 0; RowPtr <= gd.row.Count; RowPtr++) {
                            s.add(RowDelim + "{c:[");
                            RowDelim = ",";
                            ColDelim = "";
                            for (ColPtr = 0; ColPtr <= gd.col.Count; ColPtr++) {
                                var tempVar3 = gd.row[RowPtr].Cell[ColPtr];
                                s.add(ColDelim + "{v: '" + GenericController.encodeJavascriptStringSingleQuote(tempVar3.v) + "'}");
                                ColDelim = ",";
                            }
                            s.add("]}");
                        }
                        s.add("]");
                    }
                    s.add("}");
                    break;
            }
            return s.text;
            //
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed;
        //
        public void Dispose()  {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~RemoteQueryController()  {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);
            
            
        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }
    //
}