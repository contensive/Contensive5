
using System;
using Contensive.Processor.Controllers;
using NLog;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class LogFileToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        internal const int AdminFormToolLogFileView = 120;
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return GetForm_LogFiles(((CPClass)cpBase).core);
        }
        //=============================================================================
        //   Print the manual query form
        //=============================================================================
        //
        private string GetForm_LogFiles(CoreController core) {
            string tempGetForm_LogFiles = null;
            try {
                string ButtonList = ButtonCancel;
                tempGetForm_LogFiles += "<P></P>";
                //
                string QueryOld = ".asp?";
                string QueryNew = GenericController.modifyQueryString(QueryOld, RequestNameAdminForm, AdminFormToolLogFileView, true);
                tempGetForm_LogFiles += GenericController.strReplace(GetForm_LogFiles_Details(core), QueryOld, QueryNew + "&", 1, 99, 1);
                //
                var layout = core.cpParent.AdminUI.CreateLayoutBuilder();
                layout.title = "Log File View";
                layout.description = "This tool displays the Contensive Log Files.";
                layout.body = tempGetForm_LogFiles;
                foreach (string button in (ButtonList).Split(',')) {
                    if (string.IsNullOrWhiteSpace(button)) continue;
                    layout.addFormButton(button.Trim());
                }
                return layout.getHtml();

            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //==============================================================================================
        //   Display a path in the Content Files with links to download and change folders
        //==============================================================================================
        //
        private string GetForm_LogFiles_Details(CoreController core) {
            string result = "";
            try {
                CPClass cp = core.cpParent;
                //
                string startPath = null;
                string CurrentPath = null;
                string SourceFolders = null;
                string[] FolderSplit = null;
                int FolderCount = 0;
                int FolderPointer = 0;
                string[] LineSplit = null;
                string FolderLine = null;
                string FolderName = null;
                string ParentPath = null;
                int Position = 0;
                string Filename = null;
                bool RowEven = false;
                string FileSize = null;
                string FileDate = null;
                string CellCopy = null;
                //
                int AdminFormTool = core.docProperties.getInteger(RequestNameAdminForm);
                //
                const string GetTableStart = "" +
                    "<table border=\"1\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\"><tr><TD>" +
                    "   <table class=\"table table-striped\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\"><tr>"
                    + "<td width=\"23\"><img src=\"/baseassets/spacer.gif\" height=\"1\" width=\"23\"></td>"
                    + "<td width=\"60%\"><img src=\"/baseassets/spacer.gif\" height=\"1\" width=\"1\"></td>"
                    + "<td width=\"20%\"><img src=\"/baseassets/spacer.gif\" height=\"1\" width=\"1\"></td>"
                    + "<td width=\"20%\"><img src=\"/baseassets/spacer.gif\" height=\"1\" width=\"1\"></td>"
                    + "</tr>";
                const string GetTableEnd = "</table></td></tr></table>";
                //
                const string SpacerImage = "<img src=\"/baseassets/spacer.gif\" width=\"23\" height=\"22\" border=\"0\">";
                const string FolderOpenImage = "<img src=\"/baseassets/iconfolderopen.gif\" width=\"23\" height=\"22\" border=\"0\">";
                const string FolderClosedImage = "<img src=\"/baseassets/iconfolderclosed.gif\" width=\"23\" height=\"22\" border=\"0\">";
                //
                // StartPath is the root - the top of the directory, it ends in the folder name (no slash)
                //
                result = "";
                startPath = core.programDataFiles.localAbsRootPath + "Logs\\";
                //
                // CurrentPath is what is concatinated on to StartPath to get the current folder, it must start with a slash
                //
                CurrentPath = core.docProperties.getText("SetPath");
                if (string.IsNullOrEmpty(CurrentPath)) {
                    CurrentPath = "\\";
                } else if (CurrentPath.left(1) != "\\") {
                    CurrentPath = "\\" + CurrentPath;
                }
                //
                // Parent Folder is the path to the parent of current folder, and must start with a slash
                //
                Position = CurrentPath.LastIndexOf("\\", StringComparison.InvariantCulture) + 1;
                if (Position == 1) {
                    ParentPath = "\\";
                } else {
                    ParentPath = CurrentPath.left(Position - 1);
                }
                //
                //
                string pathFilename = core.docProperties.getText("pathFilename");
                if (pathFilename != "") {
                    //
                    // Return the content of the file
                    //
                    core.webServer.responseContentType = "text/text";
                    result = core.programDataFiles.readFileText(startPath + pathFilename);
                    core.doc.continueProcessing = false;
                } else {
                    result += GetTableStart;
                    //
                    // Parent Folder Link
                    //
                    if (CurrentPath != ParentPath) {
                        FileSize = "";
                        FileDate = "";
                        result += getForm_LogFiles_Details_GetRow(core, "<A href=\"" + core.webServer.requestPage + "?SetPath=" + ParentPath + "\">" + FolderOpenImage + "</A>", "<A href=\"" + core.webServer.requestPage + "?SetPath=" + ParentPath + "\">" + ParentPath + "</A>", FileSize, FileDate, RowEven);
                    }
                    //
                    // Sub-Folders
                    //
                    string adminUrl = $"{cp.Site.GetText("adminurl")}?addonguid=%7B3CAAFC0C-8AC3-427E-AF73-BA1B7A12514B%7D";
                    SourceFolders = core.programDataFiles.getFolderNameList(startPath + CurrentPath);
                    if (!string.IsNullOrEmpty(SourceFolders)) {
                        FolderSplit = SourceFolders.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                        FolderCount = FolderSplit.GetUpperBound(0) + 1;
                        for (FolderPointer = 0; FolderPointer < FolderCount; FolderPointer++) {
                            FolderLine = FolderSplit[FolderPointer];
                            if (!string.IsNullOrEmpty(FolderLine)) {
                                LineSplit = FolderLine.Split('\t');
                                FolderName = LineSplit[0];
                                FileSize = LineSplit[1];
                                FileDate = LineSplit[2];
                                result += getForm_LogFiles_Details_GetRow(core, "<A href=\"" + adminUrl + "&SetPath=" + cp.Utils.EncodeUrl(CurrentPath + FolderName) + "\">" + FolderClosedImage + "</A>", "<A href=\"" + core.webServer.requestPage + "?SetPath=" + CurrentPath + "\\" + FolderName + "\">" + FolderName + "</A>", FileSize, FileDate, RowEven);
                            }
                        }
                    }
                    //
                    // Files
                    //
                    SourceFolders = UpgradeController.upgrade51ConvertFileInfoArrayToParseString(core.programDataFiles.getFileList(startPath + CurrentPath));
                    if (string.IsNullOrEmpty(SourceFolders)) {
                        FileSize = "";
                        FileDate = "";
                        result += getForm_LogFiles_Details_GetRow(core, SpacerImage, "no files were found in this folder", FileSize, FileDate, RowEven);
                    } else {
                        FolderSplit = SourceFolders.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                        FolderCount = FolderSplit.GetUpperBound(0) + 1;
                        for (FolderPointer = 0; FolderPointer < FolderCount; FolderPointer++) {
                            FolderLine = FolderSplit[FolderPointer];
                            if (!string.IsNullOrEmpty(FolderLine)) {
                                LineSplit = FolderLine.Split('\t');
                                Filename = LineSplit[0];
                                FileSize = LineSplit[5];
                                FileDate = LineSplit[3];
                                CellCopy = "<A href=\"" + adminUrl + "&pathFilename=" + CurrentPath + "\\" + Filename + "\" target=\"_blank\">" + Filename + "</A>";
                                result += getForm_LogFiles_Details_GetRow(core, SpacerImage, CellCopy, FileSize, FileDate, RowEven);
                            }
                        }
                    }
                    //
                    result += GetTableEnd;
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return result;
        }
        //
        //=============================================================================
        //   Table Rows
        //=============================================================================
        //
        public string getForm_LogFiles_Details_GetRow(CoreController core, string Cell0, string Cell1, string Cell2, string Cell3, bool RowEven) {
            string tempGetForm_LogFiles_Details_GetRow = null;
            //
            string ClassString = null;
            //
            if (GenericController.encodeBoolean(RowEven)) {
                RowEven = false;
                ClassString = " class=\"ccPanelRowEven\" ";
            } else {
                RowEven = true;
                ClassString = " class=\"ccPanelRowOdd\" ";
            }
            //
            Cell0 = GenericController.encodeText(Cell0);
            if (string.IsNullOrEmpty(Cell0)) {
                Cell0 = "&nbsp;";
            }
            //
            Cell1 = GenericController.encodeText(Cell1);
            //
            if (string.IsNullOrEmpty(Cell1)) {
                tempGetForm_LogFiles_Details_GetRow = "<tr><TD" + ClassString + " Colspan=\"4\">" + Cell0 + "</td></tr>";
            } else {
                tempGetForm_LogFiles_Details_GetRow = "<tr><TD" + ClassString + ">" + Cell0 + "</td><TD" + ClassString + ">" + Cell1 + "</td><td align=right " + ClassString + ">" + Cell2 + "</td><td align=right " + ClassString + ">" + Cell3 + "</td></tr>";
            }
            //
            return tempGetForm_LogFiles_Details_GetRow;
        }
        //
        //
    }
}

