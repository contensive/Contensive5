using Contensive.BaseClasses;
using Contensive.PageWidgetExample.Models.Db;
using Contensive.Models.Db;
using System;
using System.Drawing.Imaging;
using LayoutModel = Contensive.Models.Db.LayoutModel;

namespace Contensive.PageWidgetExample.Addons {
    public class OnInstallAddon : AddonBaseClass {
        // 
        // ====================================================================================================
        // 
        public override object Execute(CPBaseClass CP) {
            try {
                // 
                // -- update layout records
                CP.Layout.updateLayout(Constants.guidLayoutPageWidgetExample, Constants.nameLayoutPageWidgetExample, Constants.pathFilenameLayoutPageWidgetExampleBS5);
                //
                return string.Empty;
            } catch (Exception ex) {
                CP.Site.ErrorReport(ex);
                throw;
            }
        }
        /// <summary>
        /// delete content definition (meta)
        /// </summary>
        /// <param name="contentName"></param>
        private static void deleteContentMeta(CPBaseClass CP, string contentName) {

            // -- delete the old ccContent record and fields that go with it
            if (string.IsNullOrEmpty(contentName)) { return; }
            ContentModel oldModel = DbBaseModel.createByUniqueName<ContentModel>(CP, contentName);
            if (oldModel == null) { return; }
            DbBaseModel.deleteRows<ContentFieldModel>(CP, $"contentid={oldModel.id}");
            DbBaseModel.delete<ContentModel>(CP, oldModel.id);
        }
    }
}