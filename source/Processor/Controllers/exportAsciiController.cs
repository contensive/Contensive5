
using System;
using Contensive.BaseClasses;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class ExportAsciiController : IDisposable {
        //
        //====================================================================================================
        //
        public static string exportAscii_GetAsciiExport(CoreController core, string ContentName, int PageSize, int PageNumber) {
            string result = "";
            try {
                string Delimiter = null;
                string Copy = "";
                string TableName = null;
                string FieldNameVariant = null;
                string FieldName = null;
                string UcaseFieldName = null;
                string iContentName = null;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                string TestFilename;
                //
                TestFilename = "AsciiExport" + GenericController.getRandomInteger(core) + ".txt";
                //
                iContentName = GenericController.encodeText(ContentName);
                if (PageSize == 0) {
                    PageSize = 1000;
                }
                if (PageNumber == 0) {
                    PageNumber = 1;
                }
                //
                // ----- Check for special case iContentNames
                //
                core.webServer.responseContentType = "text/plain";
                //core.html.enableOutputBuffer(false);
                TableName = DbController.getDbObjectTableName(MetadataController.getContentTablename(core, iContentName));
                switch (GenericController.toUCase(TableName)) {
                    case "CCMEMBERS":
                        //
                        // ----- People and member content export
                        //
                        if (!core.session.isAuthenticatedAdmin()) {
                            sb.Append("Warning: You must be a site administrator to export this information.");
                        } else {
                            using (var csData = new CsModel(core)) {
                                csData.open(iContentName, "", "ID", false, 0, "", PageSize, PageNumber);
                                //
                                // ----- print out the field names
                                //
                                if (csData.ok()) {
                                    sb.Append("\"EID\"");
                                    Delimiter = ",";
                                    FieldNameVariant = csData.getFirstFieldName();
                                    while (!string.IsNullOrEmpty(FieldNameVariant)) {
                                        FieldName = GenericController.encodeText(FieldNameVariant);
                                        UcaseFieldName = GenericController.toUCase(FieldName);
                                        if ((UcaseFieldName != "USERNAME") && (UcaseFieldName != "PASSWORD")) {
                                            sb.Append(Delimiter + "\"" + FieldName + "\"");
                                        }
                                        FieldNameVariant = csData.getNextFieldName();
                                    }
                                    sb.Append(Environment.NewLine);
                                }
                                //
                                // ----- print out the values
                                //
                                while (csData.ok()) {
                                    if (!(csData.getBoolean("Developer"))) {
                                        Copy = SecurityController.encodeToken(core, csData.getInteger("ID"), core.doc.profileStartTime.AddDays(30));
                                        sb.Append("\"" + Copy + "\"");
                                        Delimiter = ",";
                                        FieldNameVariant = csData.getFirstFieldName();
                                        while (!string.IsNullOrEmpty(FieldNameVariant)) {
                                            FieldName = GenericController.encodeText(FieldNameVariant);
                                            UcaseFieldName = GenericController.toUCase(FieldName);
                                            if ((UcaseFieldName != "USERNAME") && (UcaseFieldName != "PASSWORD")) {
                                                Copy = csData.getText(FieldName);
                                                if (!string.IsNullOrEmpty(Copy)) {
                                                    Copy = GenericController.strReplace(Copy, "\"", "'");
                                                    Copy = GenericController.strReplace(Copy, Environment.NewLine, " ");
                                                    Copy = GenericController.strReplace(Copy, "\r", " ");
                                                    Copy = GenericController.strReplace(Copy, "\n", " ");
                                                }
                                                sb.Append(Delimiter + "\"" + Copy + "\"");
                                            }
                                            FieldNameVariant = csData.getNextFieldName();
                                        }
                                        sb.Append(Environment.NewLine);
                                    }
                                    csData.goNext();
                                }
                            }
                        }
                        // End Case
                        break;
                    default:
                        //
                        // ----- All other content
                        //
                        if (!core.session.isAuthenticatedContentManager(iContentName)) {
                            sb.Append("Error: You must be a content manager to export this data.");
                        } else {
                            using (var csData = new CsModel(core)) {
                                csData.open(iContentName, "", "ID", false, 0, "", PageSize, PageNumber);
                                //
                                // ----- print out the field names
                                if (csData.ok()) {
                                    Delimiter = "";
                                    FieldNameVariant = csData.getFirstFieldName();
                                    while (!string.IsNullOrEmpty(FieldNameVariant)) {
                                        core.wwwFiles.appendFile(TestFilename, Delimiter + "\"" + FieldNameVariant + "\"");
                                        Delimiter = ",";
                                        FieldNameVariant = csData.getNextFieldName();
                                    }
                                    core.wwwFiles.appendFile(TestFilename, Environment.NewLine);
                                }
                                //
                                // ----- print out the values
                                while (csData.ok()) {
                                    Delimiter = "";
                                    FieldNameVariant = csData.getFirstFieldName();
                                    while (!string.IsNullOrEmpty(FieldNameVariant)) {
                                        switch (csData.getFieldTypeId(GenericController.encodeText(FieldNameVariant))) {
                                            case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                            case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                            case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                                            case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                                            case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                            case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode:
                                                Copy = csData.getTextEncoded(GenericController.encodeText(FieldNameVariant));
                                                break;
                                            case CPContentBaseClass.FieldTypeIdEnum.Lookup:
                                                Copy = csData.getText(GenericController.encodeText(FieldNameVariant));
                                                break;
                                            case CPContentBaseClass.FieldTypeIdEnum.Redirect:
                                            case CPContentBaseClass.FieldTypeIdEnum.ManyToMany:
                                                break;
                                            default:
                                                Copy = csData.getText(GenericController.encodeText(FieldNameVariant));
                                                break;
                                        }
                                        if (!string.IsNullOrEmpty(Copy)) {
                                            Copy = GenericController.strReplace(Copy, "\"", "'");
                                            Copy = GenericController.strReplace(Copy, Environment.NewLine, " ");
                                            Copy = GenericController.strReplace(Copy, "\r", " ");
                                            Copy = GenericController.strReplace(Copy, "\n", " ");
                                        }
                                        core.wwwFiles.appendFile(TestFilename, Delimiter + "\"" + Copy + "\"");
                                        Delimiter = ",";
                                        FieldNameVariant = csData.getNextFieldName();
                                    }
                                    core.wwwFiles.appendFile(TestFilename, Environment.NewLine);
                                    csData.goNext();
                                }
                            }
                        }
                        break;
                }
                result = core.wwwFiles.readFileText(TestFilename);
                core.wwwFiles.deleteFile(TestFilename);
            } catch (Exception ex) {
                LogController.logError( core,ex);
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        //
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
        ~ExportAsciiController()  {
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