
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
using Contensive.BaseModels;
using NLog;
using Contensive.Processor.Controllers.EditControls;
using System.Text;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class ManualQueryXClass : Contensive.BaseClasses.AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return get((CPClass)cpBase);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Run manual query
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static string get(CPClass cp) {
            CoreController core = cp.core;
            try {
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                //
                // Get the members SQL Queue
                //
                string sqlHistoryPathFilename = core.userProperty.getText("SQLArchive");
                if (string.IsNullOrEmpty(sqlHistoryPathFilename)) {
                    sqlHistoryPathFilename = "sqlToolArchive\\SQLArchive" + core.session.user.id.ToString("000000000") + ".txt";
                    core.userProperty.setProperty("SQLArchive", sqlHistoryPathFilename);
                }
                string sqlHistory = core.cdnFiles.readFileText(sqlHistoryPathFilename);
                //
                // Read in arguments if available
                //
                int Timeout = core.docProperties.getInteger("Timeout");
                if (Timeout == 0) {
                    Timeout = 30;
                }
                //
                int pageSize = core.docProperties.getInteger("PageSize");
                if (pageSize == 0) {
                    pageSize = 10;
                }
                //
                int pageNumber = core.docProperties.getInteger("PageNumber");
                if (pageNumber == 0) {
                    pageNumber = 1;
                }
                //
                string SQL = core.docProperties.getText("SQL");
                if (string.IsNullOrEmpty(SQL)) {
                    SQL = core.docProperties.getText("SQLList");
                }
                DataSourceModel datasource = DataSourceModel.create(core.cpParent, core.docProperties.getInteger("dataSourceid"));
                //
                if ((core.docProperties.getText("button")) == ButtonRun) {
                    //
                    // Add this SQL to the members SQL list
                    //
                    if (!string.IsNullOrEmpty(SQL)) {
                        string SQLArchiveOld = sqlHistory.Replace(SQL + Environment.NewLine, "");
                        sqlHistory = SQL.Replace( Environment.NewLine, " ") + Environment.NewLine;
                        int LineCounter = 0;
                        while ((LineCounter < 10) && (!string.IsNullOrEmpty(SQLArchiveOld))) {
                            string line = getLine(ref SQLArchiveOld).Trim();
                            if (!string.IsNullOrWhiteSpace(line)) {
                                sqlHistory += line + Environment.NewLine;
                            }
                        }
                        core.cdnFiles.saveFile(sqlHistoryPathFilename, sqlHistory);
                    }
                    //
                    // Run the SQL
                    //
                    string errBefore = ErrorController.getDocExceptionHtmlList(core);
                    if (!string.IsNullOrWhiteSpace(errBefore)) {
                        // -- error in interface, should be fixed before attempting query
                        Stream.add("<br>" + core.dateTimeNowMockable + " SQL NOT executed. The following errors were detected before execution");
                        Stream.add(errBefore);
                    } else {
                        Stream.add("<p>" + core.dateTimeNowMockable + " Executing sql [" + SQL + "] on DataSource [" + datasource.name + "]");
                        DataTable dt = null;
                        try {
                            if(datasource.name.ToLowerInvariant()=="default" || string.IsNullOrEmpty(datasource.name)) {
                                dt = core.db.executeQuery(SQL, DbController.getStartRecord(pageSize, pageNumber), pageSize);
                            } else {
                                var db = new DbController(core, datasource.name);
                                dt = db.executeQuery(SQL, DbController.getStartRecord(pageSize, pageNumber), pageSize);
                            }
                        } catch (Exception ex) {
                            //
                            // ----- error
                            Stream.add("<br>" + core.dateTimeNowMockable + " SQL execution returned the following error");
                            Stream.add("<br>" + ex.Message);
                        }
                        string errSql = ErrorController.getDocExceptionHtmlList(core);
                        if (!string.IsNullOrWhiteSpace(errSql)) {
                            Stream.add("<br>" + core.dateTimeNowMockable + " SQL execution returned the following error");
                            Stream.add("<br>" + errSql);
                            core.doc.errorList.Clear();
                        } else {
                            Stream.add("<br>" + core.dateTimeNowMockable + " SQL executed successfully");
                            if (dt == null) {
                                Stream.add("<br>" + core.dateTimeNowMockable + " SQL returned invalid data.");
                            } else if (dt.Rows == null) {
                                Stream.add("<br>" + core.dateTimeNowMockable + " SQL returned invalid data rows.");
                            } else if (dt.Rows.Count == 0) {
                                Stream.add("<br>" + core.dateTimeNowMockable + " The SQL returned no data.");
                            } else {
                                //
                                // ----- print results
                                //
                                Stream.add("<br>" + core.dateTimeNowMockable + " The following results were returned");
                                Stream.add("<br></p>");
                                //
                                // --- Create the Fields for the new table
                                //
                                int FieldCount = dt.Columns.Count;
                                Stream.add("<table class=\"table table-bordered table-hover table-sm table-striped\">");
                                Stream.add("<thead class=\"thead - inverse\"><tr>");
                                foreach (DataColumn dc in dt.Columns) Stream.add("<th>" + dc.ColumnName + "</th>");
                                Stream.add("</tr></thead>");
                                //
                                string[,] resultArray = core.db.convertDataTabletoArray(dt);
                                //
                                int RowMax = resultArray.GetUpperBound(1);
                                int ColumnMax = resultArray.GetUpperBound(0);
                                string RowStart = "<tr>";
                                string RowEnd = "</tr>";
                                string ColumnStart = "<td>";
                                string ColumnEnd = "</td>";
                                int RowPointer = 0;
                                for (RowPointer = 0; RowPointer <= RowMax; RowPointer++) {
                                    Stream.add(RowStart);
                                    int ColumnPointer = 0;
                                    for (ColumnPointer = 0; ColumnPointer <= ColumnMax; ColumnPointer++) {
                                        string CellData = resultArray[ColumnPointer, RowPointer];
                                        if (isNull(CellData)) {
                                            Stream.add(ColumnStart + "[null]" + ColumnEnd);
                                        } else if (string.IsNullOrEmpty(CellData)) {
                                            Stream.add(ColumnStart + "[empty]" + ColumnEnd);
                                        } else {
                                            Stream.add(ColumnStart + HtmlController.encodeHtml(GenericController.getText(CellData)) + ColumnEnd);
                                        }
                                    }
                                    Stream.add(RowEnd);
                                }
                                Stream.add("</table>");
                            }
                        }
                    }
                    Stream.add("<p>" + core.dateTimeNowMockable + " Done</p>");
                }
                //
                // Display form
                {
                    //
                    // -- sql form
                    int SQLRows = core.docProperties.getInteger("SQLRows");
                    if (SQLRows == 0) {
                        SQLRows = core.userProperty.getInteger("ManualQueryInputRows", 5);
                    } else {
                        core.userProperty.setProperty("ManualQueryInputRows", SQLRows.ToString());
                    }
                    Stream.add(AdminUIEditorController.getHtmlCodeEditor(core, "SQL", SQL, false, "SQL",false));
                    Stream.add("&nbsp;<INPUT TYPE=\"Text\" TabIndex=-1 NAME=\"SQLRows\" SIZE=\"3\" VALUE=\"" + SQLRows + "\" ID=\"\"  onchange=\"SQL.rows=SQLRows.value; return true\"> Rows");
                }
                //
                // -- data source
                bool isEmptyList = false;
                Stream.add(AdminUIController.getToolFormInputRow(core, "Data Source", AdminUIEditorController.getLookupContentEditor(core, "DataSourceID", datasource.id, ContentMetadataModel.getContentId(core, "data sources"), ref isEmptyList, false, "", "", false, "")));
                {
                    //
                    // -- sql list
                    string js = "var e = document.getElementById('SQLList');SQL.value=e.options[e.selectedIndex].text;";
                    List<string> lookupList = sqlHistory.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    string inputSelect = AdminUIEditorController.getLookupListEditor(core, "SQLList", 0 , lookupList,false, "SQLList","",false);
                    inputSelect = inputSelect.Replace("<select ", "<select onChange=\"" + js + "\" ");
                    Stream.add(AdminUIController.getToolFormInputRow(core, "Previous Queries", inputSelect));
                }
                //
                // -- page size
                if (isNull(pageSize)) pageSize = 100;
                Stream.add(AdminUIController.getToolFormInputRow(core, "Page Size", AdminUIEditorController.getTextEditor(core, "PageSize", pageSize.ToString())));
                //
                // -- page number
                if (isNull(pageNumber)) pageNumber = 1;
                Stream.add(AdminUIController.getToolFormInputRow(core, "Page Number", AdminUIEditorController.getTextEditor(core, "PageNumber", pageNumber.ToString())));
                //
                // -- timeout
                if (isNull(Timeout)) Timeout = 30;
                Stream.add(AdminUIController.getToolFormInputRow(core, "Timeout (sec)", AdminUIEditorController.getTextEditor(core, "Timeout", Timeout.ToString())));
                //
                // -- assemble form
                string ButtonList = ButtonCancel + "," + ButtonRun;

                //
                var layout = core.cpParent.AdminUI.CreateLayoutBuilder();
                layout.title = "Run Manual Query";
                layout.description = "This tool runs an SQL statement on a selected datasource. If there is a result set, the set is printed in a table.";
                layout.body = Stream.text;
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
    }
}

