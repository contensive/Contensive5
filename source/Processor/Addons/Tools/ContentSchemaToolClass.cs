﻿
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using NLog;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class ContentSchemaToolClass : Contensive.BaseClasses.AddonBaseClass {
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
            return get(((CPClass)cpBase).core);
        }
        //   
        //=============================================================================
        //
        public static string get(CoreController core) {
            string body = null;
            try {
                if (core.cpParent.Doc.GetText(RequestNameButton) == ButtonCancel) { return string.Empty; }
                //
                int TableColSpan = 0;
                bool TableEvenRow = false;
                string SQL = null;
                string TableName = null;
                string ButtonList;
                //
                ButtonList = ButtonCancel;
                //
                TableColSpan = 3;
                body += Controllers.HtmlController.tableStart(2, 0, 0);
                SQL = "SELECT DISTINCT ccTables.Name as TableName, ccFields.Name as FieldName, ccFieldTypes.Name as FieldType"
                        + " FROM ((ccContent LEFT JOIN ccTables ON ccContent.ContentTableId = ccTables.ID) LEFT JOIN ccFields ON ccContent.Id = ccFields.ContentID) LEFT JOIN ccFieldTypes ON ccFields.Type = ccFieldTypes.ID"
                        + " ORDER BY ccTables.Name, ccFields.Name;";
                using (var csData = new CsModel(core)) {
                    csData.openSql(SQL);
                    TableName = "";
                    while (csData.ok()) {
                        if (TableName != csData.getText("TableName")) {
                            TableName = csData.getText("TableName");
                            body += Controllers.HtmlController.tableRow("<B>" + TableName + "</b>", TableColSpan, TableEvenRow);
                        }
                        body += Controllers.HtmlController.tableRowStart();
                        body += Controllers.HtmlController.td("&nbsp;", "", 0, TableEvenRow);
                        body += Controllers.HtmlController.td(csData.getText("FieldName"), "", 0, TableEvenRow);
                        body += Controllers.HtmlController.td(csData.getText("FieldType"), "", 0, TableEvenRow);
                        body += kmaEndTableRow;
                        TableEvenRow = !TableEvenRow;
                        csData.goNext();
                    }
                }
                //
                // Field Type Definitions
                //
                body += Controllers.HtmlController.tableRow("<br><br><B>Field Type Definitions</b>", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("Boolean - Boolean values 0 and 1 are stored in a database long integer field type", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("Lookup - References to related records stored as database long integer field type", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("Integer - database long integer field type", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("Float - database floating point value", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("Date - database DateTime field type.", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("AutoIncrement - database long integer field type. Field automatically increments when a record is added.", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("Text - database character field up to 255 characters.", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("LongText - database character field up to 64K characters.", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("TextFile - references a filename in the Content Files folder. Database character field up to 255 characters. ", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("File - references a filename in the Content Files folder. Database character field up to 255 characters. ", TableColSpan, TableEvenRow);
                body += Controllers.HtmlController.tableRow("Redirect - This field has no database equivelent. No Database field is required.", TableColSpan, TableEvenRow);
                //
                // Spacers
                //
                body += Controllers.HtmlController.tableRowStart();
                body += Controllers.HtmlController.td(nop2(20, 1), "20");
                body += Controllers.HtmlController.td(nop2(300, 1), "300");
                body += Controllers.HtmlController.td("&nbsp;", "100%");
                body += kmaEndTableRow;
                body += kmaEndTable;
                //
                var layout = core.cpParent.AdminUI.CreateLayoutBuilder();
                layout.title = "Get Content Database Schema";
                layout.description = "This tool displays all tables and fields required for the current Content Defintions.";
                layout.body = body;
                foreach (string button in (ButtonList).Split(',')) {
                    if (string.IsNullOrWhiteSpace(button)) continue;
                    layout.addFormButton(button.Trim());
                }
                return layout.getHtml();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return body;
        }
        //
    }
}

