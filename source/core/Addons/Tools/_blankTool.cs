﻿
using System;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Contensive.Processor;
using Contensive.Processor.Models.DbModels;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.genericController;
using static Contensive.Processor.constants;
//
namespace Contensive.Addons.Tools {
    //
    public class _blankToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return sampleTool((CPClass)cpBase);
        }
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        //
        public static string sampleTool(CPClass cp) {
            string returnHtml = "";
            coreController core = cp.core;
            try {
                stringBuilderLegacyController Stream = new stringBuilderLegacyController();
                Stream.Add(adminUIController.getToolFormTitle("Run Manual Query", "This tool runs an SQL statement on a selected datasource. If there is a result set, the set is printed in a table."));
                //
                // process form
                string button = cp.Doc.GetText("button");
                int countryId = cp.Doc.GetInteger("countryId");
                int PageSize = cp.Doc.GetInteger("pagesize");            
                if ( button == ButtonRun) {
                    //
                }
                //
                // display form
                bool isEmptyList = false;
                Stream.Add(adminUIController.getToolFormInputRow(core, "Caption", adminUIController.getDefaultEditor_LookupContent(core, "countryId", countryId, Processor.Models.Complex.cdefModel.getContentId(core, "countries"), ref isEmptyList)));
                Stream.Add(adminUIController.getToolFormInputRow(core, "Caption", adminUIController.getDefaultEditor_Text(core, "PageSize", PageSize.ToString())));
                //
                // -- assemble form
                returnHtml = adminUIController.getToolForm(core, Stream.Text, ButtonCancel + "," + ButtonRun);

            } catch (Exception) {
                throw;
            }
            return returnHtml;
        }
    }
}
