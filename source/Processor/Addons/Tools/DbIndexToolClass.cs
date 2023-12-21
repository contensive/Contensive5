
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using System.Data;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class DbIndexToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return get(((CPClass)cpBase).core);
        }
        //=============================================================================
        //   Print the manual query form
        //=============================================================================
        //
        public static string get(CoreController core ) {
            string result = null;
            try {
                //
                int Count = 0;
                int Pointer = 0;
                int TableId = 0;
                string TableName = "";
                string FieldName = null;
                string IndexName = null;
                string DataSource = "";
                DataTable RSSchema = null;
                string Button = null;
                string[,] Rows = null;
                int RowMax = 0;
                int RowPointer = 0;
                string Copy = "";
                bool TableRowEven = false;
                int TableColSpan = 0;
                string ButtonList;
                //
                ButtonList = ButtonCancel + "," + ButtonSelect;
                result = AdminUIController.getHeaderTitleDescription("Modify Database Indexes", "This tool adds and removes database indexes.");
                //
                // Process Input
                //
                Button = core.docProperties.getText("Button");
                TableId = core.docProperties.getInteger("TableID");
                //
                // Get Tablename and DataSource
                //
                using (var csData = new CsModel(core)) {
                    csData.openRecord("Tables", TableId, "Name,DataSourceID");
                    if (csData.ok()) {
                        TableName = csData.getText("name");
                        DataSource = csData.getText("DataSourceID");
                    }
                }
                using (var db = new DbController(core, DataSource)) {
                    //
                    if ((TableId != 0) && (TableId == core.docProperties.getInteger("previoustableid")) && (!string.IsNullOrEmpty(Button))) {
                        //
                        // Drop Indexes
                        //
                        Count = core.docProperties.getInteger("DropCount");
                        if (Count > 0) {
                            for (Pointer = 0; Pointer < Count; Pointer++) {
                                if (core.docProperties.getBoolean("DropIndex." + Pointer)) {
                                    IndexName = core.docProperties.getText("DropIndexName." + Pointer);
                                    result += "<br>Dropping index [" + IndexName + "] from table [" + TableName + "]";
                                    db.deleteIndex(TableName, IndexName);
                                }
                            }
                        }
                        //
                        // Add Indexes
                        //
                        Count = core.docProperties.getInteger("AddCount");
                        if (Count > 0) {
                            for (Pointer = 0; Pointer < Count; Pointer++) {
                                if (core.docProperties.getBoolean("AddIndex." + Pointer)) {
                                    FieldName = core.docProperties.getText("AddIndexFieldName." + Pointer);
                                    IndexName = TableName + FieldName;
                                    result += "<br>Adding index [" + IndexName + "] to table [" + TableName + "] for field [" + FieldName + "]";
                                    db.createSQLIndex(TableName, IndexName, FieldName);
                                }
                            }
                        }
                    }
                    //
                    TableColSpan = 3;
                    result += HtmlController.tableStart(2, 0, 0);
                    //
                    // Select Table Form
                    //
                    result += HtmlController.tableRow("<br><br><B>Select table to index</b>", TableColSpan, false);
                    result += HtmlController.tableRow(core.html.selectFromContent("TableID", TableId, "Tables", "", "Select a SQL table to start"), TableColSpan, false);
                    if (TableId != 0) {
                        //
                        // Add/Drop Indexes form
                        //
                        result += HtmlController.inputHidden("PreviousTableID", TableId);
                        //
                        // Drop Indexes
                        //
                        result += HtmlController.tableRow("<br><br><B>Select indexes to remove</b>", TableColSpan, TableRowEven);
                        RSSchema = db.getIndexSchemaData(TableName);


                        if (RSSchema.Rows.Count == 0) {
                            //
                            // ----- no result
                            //
                            Copy += core.dateTimeNowMockable + " A schema was returned, but it contains no indexs.";
                            result += HtmlController.tableRow(Copy, TableColSpan, TableRowEven);
                        } else {

                            Rows = db.convertDataTabletoArray(RSSchema);
                            RowMax = Rows.GetUpperBound(1);
                            for (RowPointer = 0; RowPointer <= RowMax; RowPointer++) {
                                IndexName = GenericController.encodeText(Rows[5, RowPointer]);
                                if (!string.IsNullOrEmpty(IndexName)) {
                                    result += HtmlController.tableRowStart();
                                    Copy = HtmlController.checkbox("DropIndex." + RowPointer, false) + HtmlController.inputHidden("DropIndexName." + RowPointer, IndexName) + GenericController.encodeText(IndexName);
                                    result += HtmlController.td(Copy, "", 0, TableRowEven);
                                    result += HtmlController.td(GenericController.encodeText(Rows[17, RowPointer]), "", 0, TableRowEven);
                                    result += HtmlController.td("&nbsp;", "", 0, TableRowEven);
                                    result += kmaEndTableRow;
                                    TableRowEven = !TableRowEven;
                                }
                            }
                            result += HtmlController.inputHidden("DropCount", RowMax + 1);
                        }
                        //
                        // Add Indexes
                        //
                        TableRowEven = false;
                        result += HtmlController.tableRow("<br><br><B>Select database fields to index</b>", TableColSpan, TableRowEven);
                        RSSchema = db.getColumnSchemaData(TableName);
                        if (RSSchema.Rows.Count == 0) {
                            //
                            // ----- no result
                            //
                            Copy += core.dateTimeNowMockable + " A schema was returned, but it contains no indexs.";
                            result += HtmlController.tableRow(Copy, TableColSpan, TableRowEven);
                        } else {

                            Rows = db.convertDataTabletoArray(RSSchema);
                            //
                            RowMax = Rows.GetUpperBound(1);
                            for (RowPointer = 0; RowPointer <= RowMax; RowPointer++) {
                                result += HtmlController.tableRowStart();
                                Copy = HtmlController.checkbox("AddIndex." + RowPointer, false) + HtmlController.inputHidden("AddIndexFieldName." + RowPointer, Rows[3, RowPointer]) + GenericController.encodeText(Rows[3, RowPointer]);
                                result += HtmlController.td(Copy, "", 0, TableRowEven);
                                result += HtmlController.td("&nbsp;", "", 0, TableRowEven);
                                result += HtmlController.td("&nbsp;", "", 0, TableRowEven);
                                result += kmaEndTableRow;
                                TableRowEven = !TableRowEven;
                            }
                            result += HtmlController.inputHidden("AddCount", RowMax + 1);
                        }
                        //
                        // Spacers
                        //
                        result += HtmlController.tableRowStart();
                        result += HtmlController.td(nop2(300, 1), "200");
                        result += HtmlController.td(nop2(200, 1), "200");
                        result += HtmlController.td("&nbsp;", "100%");
                        result += kmaEndTableRow;
                    }
                    result += kmaEndTable;
                    //
                    // Buttons
                    //
                    result = AdminUIController.getToolForm(core, result, ButtonList);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
    }
}

