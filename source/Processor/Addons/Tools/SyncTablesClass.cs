﻿
using System;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using NLog;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class SyncTablesClass : Contensive.BaseClasses.AddonBaseClass {
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
        //=============================================================================
        /// <summary>
        /// Go through all Content Definitions and create appropriate tables and fields.
        /// </summary>
        /// <returns></returns>
        public static string get(CoreController core) {
            string returnValue = "";
            try {
                Processor.Models.Domain.ContentMetadataModel metadata = null;
                StringBuilderLegacyController Stream = new StringBuilderLegacyController();
                string[,] ContentNameArray = null;
                int ContentNameCount = 0;
                string TableName = null;
                string ButtonList;
                //
                ButtonList = ButtonCancel + "," + ButtonRun;
                //
                if (core.docProperties.getText("Button") != "") {
                    //
                    //   Run Tools
                    //
                    Stream.add("Synchronizing Tables to Content Definitions<br>");
                    using (var csData = new CsModel(core)) {
                        csData.open("Content", "", "", false, 0, "id");
                        if (csData.ok()) {
                            do {
                                metadata = Processor.Models.Domain.ContentMetadataModel.create(core, csData.getInteger("id"));
                                TableName = metadata.tableName;
                                Stream.add("Synchronizing Content " + metadata.name + " to table " + TableName + "<br>");
                                using (var db = new DbController(core, metadata.dataSourceName)) {
                                    db.createSQLTable(TableName);
                                    if (metadata.fields.Count > 0) {
                                        foreach (var keyValuePair in metadata.fields) {
                                            ContentFieldMetadataModel field = keyValuePair.Value;
                                            Stream.add("...Field " + field.nameLc + "<br>");
                                            db.createSQLTableField(TableName, field.nameLc, field.fieldTypeId);
                                        }
                                    }
                                }
                                csData.goNext();
                            } while (csData.ok());
                            ContentNameArray = csData.getRows();
                            ContentNameCount = ContentNameArray.GetUpperBound(1) + 1;
                        }
                    }
                }
                //
                var layout = core.cpParent.AdminUI.CreateLayoutBuilder();
                layout.title = "Synchronize Tables to Content Definitions";
                layout.description = "This tools goes through all Content Definitions and creates any necessary Tables and Table Fields to support the Definition.";
                layout.body = Stream.text;
                foreach (string button in (ButtonList).Split(',')) {
                    if (string.IsNullOrWhiteSpace(button)) continue;
                    layout.addFormButton(button.Trim());
                }
                return layout.getHtml();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
    }
}

