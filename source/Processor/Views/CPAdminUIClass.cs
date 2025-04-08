using Contensive.BaseClasses;
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.Models.Db;
using Contensive.WidgetDashboard.Models;
using NLog.LayoutRenderers.Wrappers;
using System;
using System.Collections.Generic;

namespace Contensive.Processor {
    public class CPAdminUIClass : CPAdminUIBaseClass {
        //
        public CPAdminUIClass(Controllers.CoreController core) {
            this.core = core;
        }
        //
        private readonly Controllers.CoreController core;
        //
        // ====================================================================================================
        //
        public override LayoutBuilderBaseClass CreateLayoutBuilder() {
            return new LayoutBuilder.LayoutBuilderClass(core.cpParent);
        }

        public override LayoutBuilderTwoColumnLeftBaseClass CreateLayoutBuilderTwoColumnLeft() {
            return new LayoutBuilder.LayoutBuilderTwoColumnLeft(core.cpParent);
        }

        public override LayoutBuilderTwoColumnRightBaseClass CreateLayoutBuilderTwoColumnRight() {
            return new LayoutBuilder.LayoutBuilderTwoColumnRight(core.cpParent);
        }

        public override LayoutBuilderListBaseClass CreateLayoutBuilderList() {
            return new LayoutBuilder.LayoutBuilderListClass(core.cpParent);
        }

        public override LayoutBuilderNameValueBaseClass CreateLayoutBuilderNameValue() {
            return new LayoutBuilder.LayoutBuilderNameValueClass(core.cpParent);
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
        //
        // ====================================================================================================
        //
        public override string GetDateTimeEditor(string htmlName, DateTime? htmlValue, string htmlId, bool readOnly)
            => Controllers.EditControls.AdminUIEditorController.getDateTimeEditor(core, htmlName, htmlValue, readOnly, htmlId, false);

        public override string GetDateTimeEditor(string htmlName, DateTime? htmlValue, string htmlId)
            => Controllers.EditControls.AdminUIEditorController.getDateTimeEditor(core, htmlName, htmlValue, false, htmlId, false);

        public override string GetDateTimeEditor(string htmlName, DateTime? htmlValue)
            => Controllers.EditControls.AdminUIEditorController.getDateTimeEditor(core, htmlName, htmlValue, false, "", false);

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
        /// implement widget dashboard
        /// </summary>
        /// <param name="dashName"></param>
        /// <param name="dashTitle"></param>
        /// <param name="widgetGuidList"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string GetWidgetDashboard(string dashName, string dashTitle, List<string> widgetGuidList) {
            string layout = core.cpParent.Layout.GetLayout(Contensive.WidgetDashboard.Constants.dashboardLayoutGuid, Contensive.WidgetDashboard.Constants.dashboardLayoutName, Contensive.WidgetDashboard.Constants.dashboardLayoutPathFilename);
            DashboardConfigModel viewModel = DashboardConfigModel.create(core.cpParent, dashName);
            viewModel.title = dashTitle;
            return core.cpParent.Mustache.Render(layout, viewModel);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// deprecated
        /// </summary>
        /// <returns></returns>
        [Obsolete("Deprecated. Use CreateLayoutBuilderTool()", true)]
        public override LayoutBuilderToolFormBaseClass NewToolForm() {
            throw new NotImplementedException("Deprecated. All report and tool helper classes are implemented through the NugetPackage Contensive.PortalApi");
        }
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