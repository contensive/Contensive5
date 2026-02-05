using Contensive.BaseClasses;
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Models.View;
using NLog.LayoutRenderers.Wrappers;
using System;
using System.Collections.Generic;

namespace Contensive.Processor {
    public class CPAdminUIClass : CPAdminUIBaseClass {
        //
        public CPAdminUIClass(Controllers.CoreController core) {
            this.core = core;
            this.cp = core.cpParent;
        }
        //
        private readonly Controllers.CoreController core;
        private readonly CPClass cp;
        //
        // ====================================================================================================
        //
        //
        public override string GetPortalHtml(string portalGuid) {
            // 
            // -- run portal
            core.docProperties.setProperty("setPortalGuid", portalGuid);
            return core.addon.execute(Constants.guidAddonPortalFramework, new CPUtilsBaseClass.addonExecuteContext {
                addonType = CPUtilsBaseClass.addonContext.ContextAdmin
            });
        }
        //
        public override LayoutBuilderBaseClass CreateLayoutBuilder() {
            return new LayoutBuilder.LayoutBuilderClass(cp);
        }

        public override LayoutBuilderTwoColumnLeftBaseClass CreateLayoutBuilderTwoColumnLeft() {
            return new LayoutBuilder.LayoutBuilderTwoColumnLeft(cp);
        }

        public override LayoutBuilderTwoColumnRightBaseClass CreateLayoutBuilderTwoColumnRight() {
            return new LayoutBuilder.LayoutBuilderTwoColumnRight(cp);
        }

        public override LayoutBuilderListBaseClass CreateLayoutBuilderList() {
            return new LayoutBuilder.LayoutBuilderListClass(cp);
        }

        public override LayoutBuilderNameValueBaseClass CreateLayoutBuilderNameValue() {
            return new LayoutBuilder.LayoutBuilderNameValueClass(cp);
        }

        public override LayoutBuilderTabbedBodyBaseClass CreateLayoutBuilderTabbedBody() {
            return new LayoutBuilder.LayoutBuilderTabbedBodyClass(cp);
        }

        //
        // ====================================================================================================
        //
        private static void getDefaultValues<T>(CPBaseClass cp, int createdModifiedById, string contentSqlSelect, List<int> childIdList, Dictionary<string, string> defaultValues) where T : DbBaseModel { }
        //
        // ====================================================================================================
        //
        public override string GetBooleanEditor(string htmlName, bool htmlValue, string htmlId, bool readOnly, bool required)
            => Controllers.EditControls.AdminUIEditorController.getBooleanEditor(core, htmlName, htmlValue, readOnly, htmlId, required);
        //
        // ====================================================================================================
        //
        public override string GetBooleanEditor(string htmlName, bool htmlValue, string htmlId, bool readOnly)
            => Controllers.EditControls.AdminUIEditorController.getBooleanEditor(core, htmlName, htmlValue, readOnly, htmlId);
        //
        // ====================================================================================================
        //
        public override string GetBooleanEditor(string htmlName, bool htmlValue, string htmlId)
            => Controllers.EditControls.AdminUIEditorController.getBooleanEditor(core, htmlName, htmlValue, false, htmlId, false);
        //
        // ====================================================================================================
        //
        public override string GetBooleanEditor(string htmlName, bool htmlValue)
            => Controllers.EditControls.AdminUIEditorController.getBooleanEditor(core, htmlName, htmlValue, false, "", false);
        //
        // ====================================================================================================
        //
        public override string GetCurrencyEditor(string htmlName, double? htmlValue, string htmlId, bool readOnly, bool required)
            => Controllers.EditControls.AdminUIEditorController.getCurrencyEditor(core, htmlName, htmlValue, readOnly, "", required, "");

        //
        // ====================================================================================================
        //
        public override string GetCurrencyEditor(string htmlName, double? htmlValue, string htmlId, bool readOnly)
            => Controllers.EditControls.AdminUIEditorController.getCurrencyEditor(core, htmlName, htmlValue, readOnly, "", false, "");
        //
        // ====================================================================================================
        //
        public override string GetCurrencyEditor(string htmlName, double? htmlValue, string htmlId)
            => Controllers.EditControls.AdminUIEditorController.getCurrencyEditor(core, htmlName, htmlValue, false, "", false, "");
        //
        // ====================================================================================================
        //
        public override string GetCurrencyEditor(string htmlName, double? htmlValue)
            => Controllers.EditControls.AdminUIEditorController.getCurrencyEditor(core, htmlName, htmlValue, false, "", false, "");
        //
        // ====================================================================================================
        //
        public override string GetDateTimeEditor(string htmlName, DateTime? htmlValue, string htmlId, bool readOnly, bool required)
            => Controllers.EditControls.AdminUIEditorController.getDateTimeEditor(core, htmlName, htmlValue, readOnly, htmlId, required);
        public override string GetDateTimeEditor(string htmlName, DateTime? htmlValue, string htmlId, bool readOnly)
            => Controllers.EditControls.AdminUIEditorController.getDateTimeEditor(core, htmlName, htmlValue, readOnly, htmlId, false);

        public override string GetDateTimeEditor(string htmlName, DateTime? htmlValue, string htmlId)
            => Controllers.EditControls.AdminUIEditorController.getDateTimeEditor(core, htmlName, htmlValue, false, htmlId, false);

        public override string GetDateTimeEditor(string htmlName, DateTime? htmlValue)
            => Controllers.EditControls.AdminUIEditorController.getDateTimeEditor(core, htmlName, htmlValue, false, "", false);
        //
        // ====================================================================================================
        //
        public override string GetDateEditor(string htmlName, DateTime? htmlValue, string htmlId, bool readOnly, bool required)
            => Controllers.EditControls.AdminUIEditorController.getDateEditor(core, htmlName, htmlValue, readOnly, htmlId, required);
        public override string GetDateEditor(string htmlName, DateTime? htmlValue, string htmlId, bool readOnly)
            => Controllers.EditControls.AdminUIEditorController.getDateEditor(core, htmlName, htmlValue, readOnly, htmlId, false);

        public override string GetDateEditor(string htmlName, DateTime? htmlValue, string htmlId)
            => Controllers.EditControls.AdminUIEditorController.getDateEditor(core, htmlName, htmlValue, false, htmlId, false);

        public override string GetDateEditor(string htmlName, DateTime? htmlValue)
            => Controllers.EditControls.AdminUIEditorController.getDateEditor(core, htmlName, htmlValue, false, "", false);

        //
        // ====================================================================================================
        //
        public override string GetEditRow(string caption, string editor)
            => Controllers.AdminUIController.getEditRow(core, editor, caption, "");

        public override string GetEditRow(string caption, string editor, string help)
            => Controllers.AdminUIController.getEditRow(core, editor, caption, help);

        public override string GetEditRow(string caption, string editor, string help, string htmlId)
            => Controllers.AdminUIController.getEditRow(core, editor, caption, help, false, false, htmlId);

        public override string GetEditRow(string caption, string editor, string help, string htmlId, bool required)
            => Controllers.AdminUIController.getEditRow(core, editor, caption, help, required, false, htmlId);

        public override string GetEditRow(string caption, string editor, string help, string htmlId, bool required, bool blockBottomRule)
            => Controllers.AdminUIController.getEditRow(core, editor, caption, help, required, false, htmlId, "", blockBottomRule, "");

        public override string GetFileEditor(string htmlName, string currentPathFilename, string htmlId, bool readOnly, bool required)
            => Controllers.EditControls.AdminUIEditorController.getFileEditor(core, htmlName, currentPathFilename, readOnly, htmlId, required, "");

        public override string GetFileEditor(string htmlName, string currentPathFilename, string htmlId, bool readOnly)
            => Controllers.EditControls.AdminUIEditorController.getFileEditor(core, htmlName, currentPathFilename, readOnly, htmlId, false, "");

        public override string GetFileEditor(string htmlName, string currentPathFilename, string htmlId)
            => Controllers.EditControls.AdminUIEditorController.getFileEditor(core, htmlName, currentPathFilename, false, htmlId, false, "");

        public override string GetFileEditor(string htmlName, string currentPathFilename)
            => Controllers.EditControls.AdminUIEditorController.getFileEditor(core, htmlName, currentPathFilename, false, "", false, "");

        public override string GetFileEditor(string htmlName)
            => Controllers.EditControls.AdminUIEditorController.getFileEditor(core, htmlName, "", false, "", false, "");

        public override string GetHtmlCodeEditor(string htmlName, string htmlValue, string htmlId, bool readOnly, bool required)
            => Controllers.EditControls.AdminUIEditorController.getHtmlCodeEditor(core, htmlName, htmlValue, readOnly, htmlId, required);

        public override string GetHtmlCodeEditor(string htmlName, string htmlValue, string htmlId, bool readOnly)
            => Controllers.EditControls.AdminUIEditorController.getHtmlCodeEditor(core, htmlName, htmlValue, readOnly, htmlId, false);

        public override string GetHtmlCodeEditor(string htmlName, string htmlValue, string htmlId)
            => Controllers.EditControls.AdminUIEditorController.getHtmlCodeEditor(core, htmlName, htmlValue, false, htmlId, false);

        public override string GetHtmlCodeEditor(string htmlName, string htmlValue)
            => Controllers.EditControls.AdminUIEditorController.getHtmlCodeEditor(core, htmlName, htmlValue, false, "");

        public override string GetHtmlEditor(string htmlName, string htmlValue, string htmlId, bool readOnly, bool required)
            => Controllers.EditControls.AdminUIEditorController.getHtmlEditor(core, htmlName, htmlValue, "", "", "", readOnly, htmlId);

        public override string GetHtmlEditor(string htmlName, string htmlValue, string htmlId, bool readOnly)
            => Controllers.EditControls.AdminUIEditorController.getHtmlEditor(core, htmlName, htmlValue, "", "", "", readOnly, htmlId);

        public override string GetHtmlEditor(string htmlName, string htmlValue, string htmlId)
            => Controllers.EditControls.AdminUIEditorController.getHtmlEditor(core, htmlName, htmlValue, "", "", "", false);

        public override string GetHtmlEditor(string htmlName, string htmlValue)
            => Controllers.EditControls.AdminUIEditorController.getHtmlEditor(core, htmlName, htmlValue, "", "", "", false, "");

        public override string GetImageEditor(string htmlName, string currentPathFilename, string htmlId, bool readOnly, bool required)
            => Controllers.EditControls.AdminUIEditorController.getImageEditor(core, htmlName, currentPathFilename, "", readOnly, "");

        public override string GetImageEditor(string htmlName, string currentPathFilename, string htmlId, bool readOnly)
            => Controllers.EditControls.AdminUIEditorController.getImageEditor(core, htmlName, currentPathFilename, "", readOnly, "");

        public override string GetImageEditor(string htmlName, string currentPathFilename, string htmlId)
            => Controllers.EditControls.AdminUIEditorController.getImageEditor(core, htmlName, currentPathFilename, "", false, "");

        public override string GetImageEditor(string htmlName, string currentPathFilename)
            => Controllers.EditControls.AdminUIEditorController.getImageEditor(core, htmlName, currentPathFilename, "", false, "");

        public override string GetIntegerEditor(string htmlName, int? htmlValue, string htmlId, bool readOnly, bool required) {
            return Controllers.EditControls.AdminUIEditorController.getIntegerEditor(core, htmlName, htmlValue, readOnly, htmlId, required, "");
        }

        public override string GetIntegerEditor(string htmlName, int? htmlValue, string htmlId, bool readOnly) {
            return Controllers.EditControls.AdminUIEditorController.getIntegerEditor(core, htmlName, htmlValue, readOnly, htmlId, false, "");
        }

        public override string GetIntegerEditor(string htmlName, int? htmlValue, string htmlId) {
            return Controllers.EditControls.AdminUIEditorController.getIntegerEditor(core, htmlName, htmlValue, false, htmlId, false, "");
        }
        public override string GetIntegerEditor(string htmlName, int? htmlValue) {
            return Controllers.EditControls.AdminUIEditorController.getIntegerEditor(core, htmlName, htmlValue, false, "", false, "");
        }
        //=> Controllers.AdminUIEditorController.getIntegerEditor(core, htmlName, htmlValue, false, "", false, "");

        public override string GetLinkEditor(string htmlName, int? htmlValue, string htmlId, bool readOnly, bool required)
            => Controllers.EditControls.AdminUIEditorController.getLinkEditor(core, htmlName, (htmlValue ?? 0).ToString(), readOnly, htmlId, required);

        public override string GetLinkEditor(string htmlName, int? htmlValue, string htmlId, bool readOnly)
            => Controllers.EditControls.AdminUIEditorController.getLinkEditor(core, htmlName, (htmlValue ?? 0).ToString(), readOnly, htmlId, false);

        public override string GetLinkEditor(string htmlName, int? htmlValue, string htmlId)
            => Controllers.EditControls.AdminUIEditorController.getLinkEditor(core, htmlName, (htmlValue ?? 0).ToString(), false, htmlId, false);

        public override string GetLinkEditor(string htmlName, int? htmlValue)
            => Controllers.EditControls.AdminUIEditorController.getLinkEditor(core, htmlName, (htmlValue ?? 0).ToString(), false, "", false);

        public override string GetLongTextEditor(string htmlName, string htmlValue, string htmlId, bool readOnly, bool required) {
            return Controllers.EditControls.AdminUIEditorController.getLongTextEditor(core, htmlName, htmlValue, readOnly, htmlId, required, "");
        }

        public override string GetLongTextEditor(string htmlName, string htmlValue, string htmlId, bool readOnly) {
            return Controllers.EditControls.AdminUIEditorController.getLongTextEditor(core, htmlName, htmlValue, readOnly, htmlId, false, "");
        }

        public override string GetLongTextEditor(string htmlName, string htmlValue, string htmlId) {
            return Controllers.EditControls.AdminUIEditorController.getLongTextEditor(core, htmlName, htmlValue, false, htmlId, false, "");
        }

        public override string GetLongTextEditor(string htmlName, string htmlValue) {
            return Controllers.EditControls.AdminUIEditorController.getLongTextEditor(core, htmlName, htmlValue, false, "", false, "");
        }

        public override string GetLookupContentEditor(string htmlName, int lookupContentId, int htmlValue, string htmlId, bool readOnly, bool required, string sqlFilter) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, htmlValue, lookupContentId, ref isEmptyList, readOnly, htmlId, "", required, sqlFilter);
        }

        public override string GetLookupContentEditor(string htmlName, int lookupContentId, int htmlValue, string htmlId, bool readOnly, bool required) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, htmlValue, lookupContentId, ref isEmptyList, readOnly, htmlId, "", required, "");
        }

        public override string GetLookupContentEditor(string htmlName, int lookupContentId, int htmlValue, string htmlId, bool readOnly) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, htmlValue, lookupContentId, ref isEmptyList, readOnly, htmlId, "", false, "");
        }

        public override string GetLookupContentEditor(string htmlName, int lookupContentId, int htmlValue, string htmlId) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, htmlValue, lookupContentId, ref isEmptyList, false, htmlId, "", false, "");
        }

        public override string GetLookupContentEditor(string htmlName, int lookupContentId, int htmlValue) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, htmlValue, lookupContentId, ref isEmptyList, false, "", "", false, "");
        }

        public override string GetLookupContentEditor(string htmlName, int lookupContentId) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, 0, lookupContentId, ref isEmptyList, false, "", "", false, "");
        }

        public override string GetLookupContentEditor(string htmlName, string lookupContentName, int htmlValue, string htmlId, bool readOnly, bool required, string sqlFilter) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, htmlValue, lookupContentName, ref isEmptyList, readOnly, htmlId, "", required, sqlFilter);
        }

        public override string GetLookupContentEditor(string htmlName, string lookupContentName, int htmlValue, string htmlId, bool readOnly, bool required) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, htmlValue, lookupContentName, ref isEmptyList, readOnly, htmlId, "", required, "");
        }

        public override string GetLookupContentEditor(string htmlName, string lookupContentName, int htmlValue, string htmlId, bool readOnly) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, htmlValue, lookupContentName, ref isEmptyList, readOnly, htmlId, "", false, "");
        }

        public override string GetLookupContentEditor(string htmlName, string lookupContentName, int htmlValue, string htmlId) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, htmlValue, lookupContentName, ref isEmptyList, false, htmlId, "", false, "");
        }

        public override string GetLookupContentEditor(string htmlName, string lookupContentName, int htmlValue) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, htmlValue, lookupContentName, ref isEmptyList, false, "", "", false, "");
        }

        public override string GetLookupContentEditor(string htmlName, string lookupContentName) {
            bool isEmptyList = false;
            return Controllers.EditControls.AdminUIEditorController.getLookupContentEditor(core, htmlName, 0, lookupContentName, ref isEmptyList, false, "", "", false, "");
        }

        public override string GetLookupListEditor(string htmlName, List<string> lookupList, string currentLookupName, string htmlId, bool readOnly, bool required) {
            return Controllers.EditControls.AdminUIEditorController.getLookupListEditor(core, htmlName, currentLookupName, lookupList, readOnly, htmlId, "", required);
        }

        public override string GetLookupListEditor(string htmlName, List<string> lookupList, string currentLookupName, string htmlId, bool readOnly) {
            return Controllers.EditControls.AdminUIEditorController.getLookupListEditor(core, htmlName, currentLookupName, lookupList, readOnly, htmlId, "", false);
        }

        public override string GetLookupListEditor(string htmlName, List<string> lookupList, string currentLookupName, string htmlId) {
            return Controllers.EditControls.AdminUIEditorController.getLookupListEditor(core, htmlName, currentLookupName, lookupList, false, htmlId, "", false);
        }

        public override string GetLookupListEditor(string htmlName, List<string> lookupList, string currentLookupName) {
            return Controllers.EditControls.AdminUIEditorController.getLookupListEditor(core, htmlName, currentLookupName, lookupList, false, "", "", false);
        }

        public override string GetLookupListEditor(string htmlName, List<string> lookupList, int currentLookupValue, string htmlId, bool readOnly, bool required) {
            return Controllers.EditControls.AdminUIEditorController.getLookupListEditor(core, htmlName, currentLookupValue, lookupList, readOnly, htmlId, "", required);
        }

        public override string GetLookupListEditor(string htmlName, List<string> lookupList, int currentLookupValue, string htmlId, bool readOnly) {
            return Controllers.EditControls.AdminUIEditorController.getLookupListEditor(core, htmlName, currentLookupValue, lookupList, readOnly, htmlId, "", false);
        }

        public override string GetLookupListEditor(string htmlName, List<string> lookupList, int currentLookupValue, string htmlId) {
            return Controllers.EditControls.AdminUIEditorController.getLookupListEditor(core, htmlName, currentLookupValue, lookupList, false, htmlId, "", false);
        }

        public override string GetLookupListEditor(string htmlName, List<string> lookupList, int currentLookupValue) {
            return Controllers.EditControls.AdminUIEditorController.getLookupListEditor(core, htmlName, currentLookupValue, lookupList, false, "", "", false);
        }

        public override string GetLookupListEditor(string htmlName, List<string> lookupList) {
            return Controllers.EditControls.AdminUIEditorController.getLookupListEditor(core, htmlName, 0, lookupList, false, "", "", false);
        }

        public override string GetMemberSelectEditor(string htmlName, int lookupPersonId, string groupGuid, string htmlId, bool readOnly, bool required) {
            return Controllers.EditControls.AdminUIEditorController.getMemberSelectEditor(core, htmlName, lookupPersonId, groupGuid, "", readOnly, htmlId, required, "");
        }

        public override string GetMemberSelectEditor(string htmlName, int lookupPersonId, string groupGuid, string htmlId, bool readOnly) {
            return Controllers.EditControls.AdminUIEditorController.getMemberSelectEditor(core, htmlName, lookupPersonId, groupGuid, "", readOnly, htmlId, false, "");
        }

        public override string GetMemberSelectEditor(string htmlName, int lookupPersonId, string groupGuid, string htmlId) {
            return Controllers.EditControls.AdminUIEditorController.getMemberSelectEditor(core, htmlName, lookupPersonId, groupGuid, "", false, htmlId, false, "");
        }

        public override string GetMemberSelectEditor(string htmlName, int lookupPersonId, string groupGuid) {
            return Controllers.EditControls.AdminUIEditorController.getMemberSelectEditor(core, htmlName, lookupPersonId, groupGuid, "", false, "", false, "");
        }

        public override string GetMemberSelectEditor(string htmlName, int lookupPersonId, int groupId, string htmlId, bool readOnly, bool required) {
            return Controllers.EditControls.AdminUIEditorController.getMemberSelectEditor(core, htmlName, lookupPersonId, groupId, "", readOnly, htmlId, required, "");
        }

        public override string GetMemberSelectEditor(string htmlName, int lookupPersonId, int groupId, string htmlId, bool readOnly) {
            return Controllers.EditControls.AdminUIEditorController.getMemberSelectEditor(core, htmlName, lookupPersonId, groupId, "", readOnly, htmlId, false, "");
        }

        public override string GetMemberSelectEditor(string htmlName, int lookupPersonId, int groupId, string htmlId) {
            return Controllers.EditControls.AdminUIEditorController.getMemberSelectEditor(core, htmlName, lookupPersonId, groupId, "", false, htmlId, false, "");
        }

        public override string GetMemberSelectEditor(string htmlName, int lookupPersonId, int groupId) {
            return Controllers.EditControls.AdminUIEditorController.getMemberSelectEditor(core, htmlName, lookupPersonId, groupId, "", false, "", false, "");
        }

        public override string GetNumberEditor(string htmlName, double? htmlValue, string htmlId, bool readOnly, bool required) {
            return Controllers.EditControls.AdminUIEditorController.getNumberEditor(core, htmlName, htmlValue, readOnly, htmlId, required, "");
        }

        public override string GetNumberEditor(string htmlName, double? htmlValue, string htmlId, bool readOnly) {
            return Controllers.EditControls.AdminUIEditorController.getNumberEditor(core, htmlName, htmlValue, readOnly, htmlId, false, "");
        }

        public override string GetNumberEditor(string htmlName, double? htmlValue, string htmlId) {
            return Controllers.EditControls.AdminUIEditorController.getNumberEditor(core, htmlName, htmlValue, false, htmlId, false, "");
        }

        public override string GetNumberEditor(string htmlName, double? htmlValue) {
            return Controllers.EditControls.AdminUIEditorController.getNumberEditor(core, htmlName, htmlValue, false, "", false, "");
        }

        public override string GetPasswordEditor(string htmlName, string htmlValue, string htmlId, bool readOnly, bool required) {
            return Controllers.EditControls.AdminUIEditorController.getPasswordEditor(core, htmlName, htmlValue, readOnly, htmlId);
        }

        public override string GetPasswordEditor(string htmlName, string htmlValue, string htmlId, bool readOnly) {
            return Controllers.EditControls.AdminUIEditorController.getPasswordEditor(core, htmlName, htmlValue, readOnly, htmlId);
        }

        public override string GetPasswordEditor(string htmlName, string htmlValue, string htmlId) {
            return Controllers.EditControls.AdminUIEditorController.getPasswordEditor(core, htmlName, htmlValue, false, htmlId);
        }

        public override string GetPasswordEditor(string htmlName, string htmlValue) {
            return Controllers.EditControls.AdminUIEditorController.getPasswordEditor(core, htmlName, htmlValue, false, "");
        }

        public override string GetSelectorStringEditor(string htmlName, string htmlValue, string selectorString, string htmlId, bool readOnly, bool required) {
            return Controllers.EditControls.AdminUIEditorController.getSelectorStringEditor(core, htmlName, htmlValue, selectorString);
        }

        public override string GetSelectorStringEditor(string htmlName, string htmlValue, string selectorString, string htmlId, bool readOnly) {
            return Controllers.EditControls.AdminUIEditorController.getSelectorStringEditor(core, htmlName, htmlValue, selectorString);
        }

        public override string GetSelectorStringEditor(string htmlName, string htmlValue, string selectorString, string htmlId) {
            return Controllers.EditControls.AdminUIEditorController.getSelectorStringEditor(core, htmlName, htmlValue, selectorString);
        }

        public override string GetSelectorStringEditor(string htmlName, string htmlValue, string selectorString) {
            return Controllers.EditControls.AdminUIEditorController.getSelectorStringEditor(core, htmlName, htmlValue, selectorString);
        }

        public override string GetTextEditor(string htmlName, string htmlValue, string htmlId, bool readOnly, bool required) {
            return Controllers.EditControls.AdminUIEditorController.getTextEditor(core, htmlName, htmlValue, readOnly, htmlId, required);
        }

        public override string GetTextEditor(string htmlName, string htmlValue, string htmlId, bool readOnly) {
            return Controllers.EditControls.AdminUIEditorController.getTextEditor(core, htmlName, htmlValue, readOnly, htmlId);
        }

        public override string GetTextEditor(string htmlName, string htmlValue, string htmlId) {
            return Controllers.EditControls.AdminUIEditorController.getTextEditor(core, htmlName, htmlValue, false, htmlId);
        }

        public override string GetTextEditor(string htmlName, string htmlValue) {
            return Controllers.EditControls.AdminUIEditorController.getTextEditor(core, htmlName, htmlValue);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// implement admin site widget dashboard
        /// </summary>
        /// <param name="dashName"></param>
        /// <param name="dashTitle"></param>
        /// <param name="widgetGuidList"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string GetWidgetDashboard() {
            string layout = cp.Layout.GetLayout(Contensive.Processor.Constants.dashboardLayoutGuid, Contensive.Processor.Constants.dashboardLayoutName, Contensive.Processor.Constants.dashboardLayoutPathFilename);
            DashboardViewModel viewModel = DashboardViewModel.create(cp, "");
            return cp.Mustache.Render(layout, viewModel);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// implement widget dashboard for a dashboard
        /// </summary>
        /// <param name="dashName"></param>
        /// <param name="dashTitle"></param>
        /// <param name="widgetGuidList"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string GetWidgetDashboard(string portalGuid) {
            string layout = cp.Layout.GetLayout(Contensive.Processor.Constants.dashboardLayoutGuid, Contensive.Processor.Constants.dashboardLayoutName, Contensive.Processor.Constants.dashboardLayoutPathFilename);
            DashboardViewModel viewModel = DashboardViewModel.create(cp, portalGuid);
            return cp.Mustache.Render(layout, viewModel);
        }
        // 
        //====================================================================================================
        // 
        /// <summary>
        /// return the link to the admin site portal and portal feature
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="portalFeatureGuid"></param>
        /// <returns></returns>
        public override string GetPortalFeatureLink(string portalGuid, string portalFeatureGuid) {
            return $"{cp.GetAppConfig().adminRoute}?addonGuid={cp.Utils.EncodeRequestVariable(Constants.guidAddonPortalFramework)}&setPortalGuid={cp.Utils.EncodeRequestVariable(portalGuid)}&dstfeatureguid={cp.Utils.EncodeRequestVariable(portalFeatureGuid)}";
        }
        // 
        //====================================================================================================
        /// <summary>
        /// redirect and return blank
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="portalFeatureGuid"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public override string RedirectToPortalFeature(string portalGuid, string portalFeatureGuid, string linkAppend) {
            // 
            // -- setup redirect and return blank, the flag for return to parent Addon
            linkAppend = string.IsNullOrEmpty(linkAppend) ? "" : linkAppend.StartsWith("&") ? linkAppend : "&" + linkAppend;
            cp.Response.Redirect(GetPortalFeatureLink( portalGuid, portalFeatureGuid) + linkAppend);
            return "";
        }
        // 
        //====================================================================================================
        /// <summary>
        /// redirect and return blank
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="portalFeatureGuid"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public override string RedirectToPortalFeature(string portalGuid, string portalFeatureGuid) {
            cp.Response.Redirect(GetPortalFeatureLink(portalGuid, portalFeatureGuid));
            return "";
        }
        // 
        // ===================================================================================
        /// <summary>
        /// Portal features should only be run from with the c5 adminui portal. If not, redirect to the portal with this feature set
        /// 
        /// return false if endpoint does not include the portalframework guid.
        /// use for 1-line validation check
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override bool EndpointContainsPortal() {
            //
            // -- return is from c5 adminui callback
            string callbackAddonGuid = cp.Doc.GetText("callbackAddonGuid");
            if (!string.IsNullOrEmpty(callbackAddonGuid)) { return true; }
            //
            // -- return false if endpoint does not include the portalframework guid and is not a callback from the c5 adminUI
            string requestAddonGuid = cp.Doc.GetText("addonguid");
            if (!string.IsNullOrEmpty(requestAddonGuid)) {
                if ((requestAddonGuid ?? "") == Constants.guidAddonPortalFramework)
                    return true;
                // 
                requestAddonGuid = requestAddonGuid.ToLowerInvariant().Replace("%7b", "{").Replace("%7d", "}");
                return (requestAddonGuid ?? "") == Constants.guidAddonPortalFramework;
            }
            // 
            // -- might be addonId (slower so migrate out of this)
            int requestAddonId = cp.Doc.GetInteger("addonid");
            if (requestAddonId == 0)
                return false;
            // 
            // -- attempt read the addonId from cache. request is not 0, so if they match it was good
            string cacheKeyPortalFrameworkAddonId = cp.Cache.CreateKey("portal-framework-addon-id");
            int portalFrameworkAddonId = cp.Cache.GetInteger(cacheKeyPortalFrameworkAddonId);
            if (portalFrameworkAddonId == requestAddonId)
                return true;
            // 
            portalFrameworkAddonId = Contensive.Models.Db.DbBaseModel.getRecordId<AddonModel>(cp, Constants.guidAddonPortalFramework);
            if (portalFrameworkAddonId == 0)
                return false;
            cp.Cache.Store(cacheKeyPortalFrameworkAddonId, portalFrameworkAddonId);
            return requestAddonId == portalFrameworkAddonId;
        }
        // 
        //====================================================================================================
        /// <summary>
        /// deprecated
        /// </summary>
        /// <returns></returns>
        [Obsolete("Deprecated. Use CreateLayoutBuilderTool()", true)]
        public override LayoutBuilderToolFormBaseClass NewToolForm() {
            throw new NotImplementedException("Deprecated. All report and tool helper classes are implemented through the NugetPackage Contensive.PortalApi");
        }
        // 
        //====================================================================================================
        /// <summary>
        /// deprecated
        /// </summary>
        /// <returns></returns>
        [Obsolete("Deprecated. Use CreateLayoutBuilderList()", true)]
        public override LayoutBuilderListBaseClass NewListReport() {
            throw new NotImplementedException("Deprecated. Use CreateLayoutBuilderList()");
        }
    }
}