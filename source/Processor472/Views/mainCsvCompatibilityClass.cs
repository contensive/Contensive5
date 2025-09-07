
using System;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
 
namespace Contensive.Processor {
    public class MainCsvScriptCompatibilityClass {
        private readonly CoreController core;
        public MainCsvScriptCompatibilityClass(CoreController core) {
            this.core = core;
        }
        //
        public void SetViewingProperty( string propertyName , string propertyValue ) {
            core.siteProperties.setProperty(propertyName, propertyValue);
        }
        //
        public string EncodeContent9(string Source, int personalizationPeopleId , string ContextContentName, int ContextRecordID, int ContextContactPeopleID, bool PlainText, bool AddLinkEID, bool EncodeActiveFormatting , bool EncodeActiveImages , bool EncodeActiveEditIcons , bool EncodeActivePersonalization, string AddAnchorQuery , string ProtocolHostString , bool IsEmailContent ,  int ignoreInt , String ignore_TemplateCaseOnly_Content, int addonContext ) {
            PersonModel userNullable = DbBaseModel.create<PersonModel>(core.cpParent, personalizationPeopleId);
            return ContentRenderController.renderContent(core, Source, userNullable, ContextContentName, ContextRecordID, ContextContactPeopleID, PlainText, AddLinkEID, EncodeActiveFormatting, EncodeActiveImages, EncodeActiveEditIcons, EncodeActivePersonalization, AddAnchorQuery, ProtocolHostString, IsEmailContent, (CPUtilsClass.addonContext)addonContext, core.session.isAuthenticated,  false);
        }
    }
}