
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.AdminSite {
    //
    public class GetAjaxVisitPropertyClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// GetAjaxVisitPropertyClass remote method
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
                gd.col = new List<ColsType>();
                gd.row = new List<RowsType>();
                gd.IsEmpty = false;
                for (var ptr = 0; ptr <= Args.GetUpperBound(0); ptr++) {
                    ColsType col = new ColsType();
                    CellType cell = new CellType();
                    string[] ArgNameValue = Args[ptr].Split('=');
                    col.Id = ArgNameValue[0];
                    col.Label = ArgNameValue[0];
                    col.Type = "string";
                    string PropertyValue = "";
                    if (ArgNameValue.GetUpperBound(0) > 0) {
                        PropertyValue = ArgNameValue[1];
                    }
                    cell.v = core.visitProperty.getText(ArgNameValue[0], PropertyValue);
                    gd.row[0].Cell.Add(cell);
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
