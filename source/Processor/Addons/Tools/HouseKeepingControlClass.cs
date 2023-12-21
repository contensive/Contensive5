
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class HouseKeepingControlClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return get(((CPClass)cpBase));
        }
        //
        //========================================================================
        //
        //========================================================================
        //
        public static string get(CPClass cp) {
            string tempGetForm_HouseKeepingControl = null;
            try {
                //
                StringBuilderLegacyController Content = new StringBuilderLegacyController();
                string Copy = null;
                string SQL = null;
                string Button = null;
                int PagesTotal = 0;
                string Caption = null;
                DateTime DateValue = default;
                string AgeInDays = null;
                int ArchiveRecordAgeDays = 0;
                string ArchiveTimeOfDay = null;
                bool ArchiveAllowFileClean = false;
                string ButtonList = "";
                string Description = null;
                //
                Button = cp.core.docProperties.getText(RequestNameButton);
                if (Button == ButtonCancel) {
                    //
                    //
                    //
                    return cp.core.webServer.redirect("/" + cp.core.appConfig.adminRoute, "HouseKeepingControl, Cancel Button Pressed");
                } else if (!cp.core.session.isAuthenticatedAdmin()) {
                    //
                    //
                    //
                    ButtonList = ButtonCancel;
                    Content.add(AdminUIController.getFormBodyAdminOnly());
                } else {
                    //
                    string tableBody = "";
                    //
                    // Set defaults
                    //
                    ArchiveRecordAgeDays = (cp.core.siteProperties.getInteger("ArchiveRecordAgeDays", 2));
                    ArchiveTimeOfDay = cp.core.siteProperties.getText("ArchiveTimeOfDay", "12:00:00 AM");
                    ArchiveAllowFileClean = (cp.core.siteProperties.getBoolean("ArchiveAllowFileClean", false));
                    //
                    // Process Requests
                    //
                    switch (Button) {
                        case ButtonOK:
                        case ButtonSave:
                        //
                        ArchiveRecordAgeDays = cp.core.docProperties.getInteger("ArchiveRecordAgeDays");
                        cp.core.siteProperties.setProperty("ArchiveRecordAgeDays", GenericController.encodeText(ArchiveRecordAgeDays));
                        //
                        ArchiveTimeOfDay = cp.core.docProperties.getText("ArchiveTimeOfDay");
                        cp.core.siteProperties.setProperty("ArchiveTimeOfDay", ArchiveTimeOfDay);
                        //
                        ArchiveAllowFileClean = cp.core.docProperties.getBoolean("ArchiveAllowFileClean");
                        cp.core.siteProperties.setProperty("ArchiveAllowFileClean", GenericController.encodeText(ArchiveAllowFileClean));
                        break;
                    }
                    //
                    if (Button == ButtonOK) {
                        return cp.core.webServer.redirect("/" + cp.core.appConfig.adminRoute, "StaticPublishControl, OK Button Pressed");
                    }
                    //
                    // ----- Status
                    //
                    tableBody += HtmlController.tableRowStart() + "<td colspan=\"3\" class=\"ccPanel3D ccAdminEditSubHeader\"><b>Status</b>" + tableCellEnd + kmaEndTableRow;
                    //
                    // ----- Visits Found
                    //
                    PagesTotal = 0;
                    SQL = "SELECT Count(ID) as Result FROM ccVisits;";
                    using (var csData = new CsModel(cp.core)) {
                        csData.openSql(SQL);
                        if (csData.ok()) {
                            PagesTotal = csData.getInteger("Result");
                        }
                    }
                    tableBody += AdminUIController.getEditRowLegacy(cp.core, SpanClassAdminNormal + PagesTotal, "Visits Found", "", false, false, "");
                    //
                    // ----- Oldest Visit
                    //
                    Copy = "unknown";
                    AgeInDays = "unknown";
                    using (var csData = new CsModel(cp.core)) {
                        SQL = cp.core.db.getSQLSelect("ccVisits", "DateAdded", "", "ID", "", 1);
                        csData.openSql(SQL);
                        if (csData.ok()) {
                            DateValue = csData.getDate("DateAdded");
                            if (DateValue != DateTime.MinValue) {
                                Copy = GenericController.encodeText(DateValue);
                                AgeInDays = GenericController.encodeText(encodeInteger(Math.Floor(encodeNumber(cp.core.doc.profileStartTime - DateValue))));
                            }
                        }
                    }
                    tableBody += (AdminUIController.getEditRowLegacy(cp.core, SpanClassAdminNormal + Copy + " (" + AgeInDays + " days)", "Oldest Visit", "", false, false, ""));
                    //
                    // ----- Viewings Found
                    //
                    PagesTotal = 0;
                    SQL = "SELECT Count(ID) as result  FROM ccViewings;";
                    using (var csData = new CsModel(cp.core)) {
                        csData.openSql(SQL);
                        if (csData.ok()) {
                            PagesTotal = csData.getInteger("Result");
                        }
                        csData.close();
                    }
                    tableBody += (AdminUIController.getEditRowLegacy(cp.core, SpanClassAdminNormal + PagesTotal, "Viewings Found", "", false, false, ""));
                    //
                    tableBody += (HtmlController.tableRowStart() + "<td colspan=\"3\" class=\"ccPanel3D ccAdminEditSubHeader\"><b>Options</b>" + tableCellEnd + kmaEndTableRow);
                    //
                    Caption = "Archive Age";
                    Copy = HtmlController.inputText_Legacy(cp.core, "ArchiveRecordAgeDays", ArchiveRecordAgeDays.ToString(), -1, 20) + "&nbsp;Number of days to keep visit records. 0 disables housekeeping.";
                    tableBody += (AdminUIController.getEditRowLegacy(cp.core, Copy, Caption));
                    //
                    Caption = "Housekeeping Time";
                    Copy = HtmlController.inputText_Legacy(cp.core, "ArchiveTimeOfDay", ArchiveTimeOfDay, -1, 20) + "&nbsp;The time of day when record deleting should start.";
                    tableBody += (AdminUIController.getEditRowLegacy(cp.core, Copy, Caption));
                    //
                    Caption = "Purge Content Files";
                    Copy = HtmlController.checkbox("ArchiveAllowFileClean", ArchiveAllowFileClean) + "&nbsp;Delete Contensive content files with no associated database record.";
                    tableBody += (AdminUIController.getEditRowLegacy(cp.core, Copy, Caption));
                    //
                    Content.add(AdminUIController.editTable(tableBody));
                    Content.add(HtmlController.inputHidden(rnAdminSourceForm, AdminformHousekeepingControl));
                    ButtonList = ButtonCancel + ",Refresh," + ButtonSave + "," + ButtonOK;
                }
                //
                Caption = "Data Housekeeping Control";
                Description = "This tool is used to control the database record housekeeping process. This process deletes visit history records, so care should be taken before making any changes.";
                tempGetForm_HouseKeepingControl = AdminUIController.getToolBody(cp.core, Caption, ButtonList, "", false, false, Description, "", 0, Content.text);
                //
                cp.core.html.addTitle(Caption,Caption);
            } catch (Exception ex) {
                LogControllerX.logError(cp.core, ex);
            }
            return tempGetForm_HouseKeepingControl;
        }
        //
    }
}

