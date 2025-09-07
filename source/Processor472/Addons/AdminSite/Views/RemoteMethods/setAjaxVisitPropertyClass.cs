
using System;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.AdminSite {
    //
    public class SetAjaxVisitPropertyClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// getFieldEditorPreference remote method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;

                string ArgList = core.docProperties.getText("args");
                string[] Args = ArgList.Split('&');
                GoogleDataType gd = new GoogleDataType();
                gd.IsEmpty = true;
                for (var Ptr = 0; Ptr <= Args.GetUpperBound(0); Ptr++) {
                    string[] ArgNameValue = Args[Ptr].Split('=');
                    string PropertyName = ArgNameValue[0];
                    string PropertyValue = "";
                    if (ArgNameValue.GetUpperBound(0) > 0) {
                        PropertyValue = ArgNameValue[1];
                    }
                    core.visitProperty.setProperty(PropertyName, PropertyValue);
                }
                result = RemoteQueryController.format(core, gd, RemoteQueryController.RemoteFormatEnum.RemoteFormatJsonNameValue);
                result = HtmlController.encodeHtml(result);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
