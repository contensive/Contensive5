using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Control export of data records
    /// </summary>
    public static class ExportDataRecordController {
        // 
        // ====================================================================================================
        /// <summary>
        /// Create XML string for the Data node of the collection file from DataRecordList (list from collection record data-records).
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="dataExportList">crlf delimited list of records to export. Each line is either the name of hte content, or content-record-guid</param>
        /// <param name="tempPathFileList"></param>
        /// <param name="tempExportPath"></param>
        /// <returns></returns>
        public static string getNodeList(CPBaseClass cp, List<CollectionDataRecordModel> dataExportList, List<string> tempPathFileList, string tempExportPath) {
            try {
                var nodeList = new StringBuilder();
                if (true) {
                    var RecordNodes = new StringBuilder();
                    //
                    // -- sort data by contentname, then recordname, then recordGuid
                    dataExportList = dataExportList.OrderBy(x => x.contentName).ThenBy(x => x.recordName).ThenBy(x => x.recordGuid).ToList();
                    //
                    // -- dataExportList -- enumerate the list of data to be exported. Each dataExport can have a list of records, each record will have a list of fields
                    foreach (CollectionDataRecordModel dataExport in dataExportList) {
                        //
                        // -- export one line from the data-record
                        string FieldNodes = "";
                        int dataExportContentId = cp.Content.GetID(dataExport.contentName);
                        if (dataExportContentId <= 0) {
                            //
                            // -- bad data export, output comment and move to next
                            RecordNodes.Append(""
                                + Environment.NewLine + "\t"
                                        + "<!-- data missing, content not found during export, "
                                        + "content=\"" + dataExport.contentName + "\" "
                                        + "guid=\"" + dataExport.recordGuid + "\" "
                                        + "name=\"" + dataExport.recordName + "\" -->");
                        } else {
                            string Criteria;
                            if (!string.IsNullOrEmpty(dataExport.recordGuid)) {
                                // 
                                // guid {726ED098-5A9E-49A9-8840-767A74F41D01} format
                                Criteria = "ccguid=" + cp.Db.EncodeSQLText(dataExport.recordGuid);
                            } else if (string.IsNullOrEmpty(dataExport.recordName)) {
                                // 
                                // name and guid empty, export all records
                                Criteria = "";
                            } else if ((dataExport.recordName.Length == 36) & (dataExport.recordName.Substring(8, 1) == "-")) {
                                // 
                                // use name as guid 726ED098-5A9E-49A9-8840-767A74F41D01 format
                                Criteria = "ccguid=" + cp.Db.EncodeSQLText(dataExport.recordName);
                            } else if ((dataExport.recordName.Length == 32) & (dataExport.recordName.IndexOf(" ") == -1)) {
                                // 
                                // use name as guid 726ED0985A9E49A98840767A74F41D01 format
                                Criteria = "ccguid=" + cp.Db.EncodeSQLText(dataExport.recordName);
                            } else {
                                // 
                                // use name as-is
                                Criteria = "name=" + cp.Db.EncodeSQLText(dataExport.recordName);
                            }
                            using (CPCSBaseClass csDataExportRecords = cp.CSNew()) {
                                if (!csDataExportRecords.Open(dataExport.contentName, Criteria, "id")) {
                                    //
                                    // -- data records being exported.Comment and go to next DataExport (set of records, line in the collection file)
                                    RecordNodes.Append(""
                                        + Environment.NewLine + "\t"
                                        + "<!-- data missing, record not found during export, "
                                        + "content=\"" + dataExport.contentName + "\" "
                                        + "guid=\"" + dataExport.recordGuid + "\" "
                                        + "name=\"" + dataExport.recordName + "\" -->");
                                } else {
                                    // 
                                    // -- get output 'columns', the inner loop enumeration, the fields within each record that will be exported
                                    var contentFieldExportList = new List<ContentFieldModel>();
                                    List<string> ignoreFields = new List<string> { "id", "contentcontrolid", "editsourceid", "editarchive", "editblank", "contentcategoryid", "createdby", "modifiedby", "dateadded", "modifieddate" };
                                    List<int> ignoreTypes = new List<int> { (int)CPContentBaseClass.FieldTypeIdEnum.Redirect, (int)CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement };
                                    foreach (var metaField in DbBaseModel.createList<ContentFieldModel>(cp, "contentid=" + dataExportContentId)) {
                                        if (!string.IsNullOrEmpty(metaField.name)) {
                                            if (!ignoreFields.Contains(metaField.name.ToLowerInvariant())) {
                                                if (!ignoreTypes.Contains(metaField.type)) {
                                                    contentFieldExportList.Add(DbBaseModel.create<ContentFieldModel>(cp, metaField.id));
                                                }
                                            }
                                        }
                                    }
                                    // 
                                    // -- enumerate the records to output, then the fields within each record
                                    dataExport.recordGuid = "";
                                    while (csDataExportRecords.OK()) {
                                        FieldNodes = "";
                                        dataExport.recordName = csDataExportRecords.GetText("name");
                                        dataExport.recordGuid = csDataExportRecords.GetText("ccguid");
                                        if (dataExport.recordGuid == "") {
                                            dataExport.recordGuid = cp.Utils.CreateGuid();
                                            csDataExportRecords.SetField("ccGuid", dataExport.recordGuid);
                                        }
                                        foreach (var contentField in contentFieldExportList) {
                                            switch (contentField.type) {
                                                case (int)CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                                        //
                                                        // -- many-to-many
                                                        string nodeInnerText = "";
                                                        string ruleContent = cp.Content.GetName(contentField.manyToManyRuleContentId);
                                                        string secondaryContentName = cp.Content.GetName(contentField.manyToManyContentId);
                                                        if ((!string.IsNullOrEmpty(ruleContent))
                                                            && (!string.IsNullOrEmpty(contentField.manyToManyRulePrimaryField))
                                                            && (!string.IsNullOrEmpty(contentField.manyToManyRuleSecondaryField))
                                                            && (!string.IsNullOrEmpty(secondaryContentName))) {
                                                            using (CPCSBaseClass csRule = cp.CSNew()) {
                                                                csRule.Open(ruleContent, contentField.manyToManyRulePrimaryField + "=" + csDataExportRecords.GetInteger("id"));
                                                                while (csRule.OK()) {
                                                                    int secondaryContentRecordId = csRule.GetInteger(contentField.manyToManyRuleSecondaryField);
                                                                    using (CPCSBaseClass CS3 = cp.CSNew()) {
                                                                        CS3.Open(secondaryContentName, "ID=" + secondaryContentRecordId);
                                                                        if (CS3.OK()) {
                                                                            string Guid = CS3.GetText("ccGuid");
                                                                            if (Guid == "") {
                                                                                Guid = cp.Utils.CreateGuid();
                                                                                CS3.SetField("ccGuid", Guid);
                                                                            }
                                                                            nodeInnerText = nodeInnerText + Environment.NewLine + "\t\t" + "<selection>" + Guid + "</selection>";
                                                                        }
                                                                        CS3.Close();
                                                                    }
                                                                    csRule.GoNext();
                                                                }
                                                                csRule.Close();
                                                            }
                                                        }
                                                        if (string.IsNullOrEmpty(nodeInnerText)) {
                                                            FieldNodes += Environment.NewLine + "\t\t\t" + "<field name=\"" + System.Net.WebUtility.HtmlEncode(contentField.name) + "\" />";
                                                        } else {
                                                            FieldNodes += Environment.NewLine + "\t\t\t" + "<field name=\"" + System.Net.WebUtility.HtmlEncode(contentField.name) + "\">" + nodeInnerText + Environment.NewLine + "\t\t" + "</field>";
                                                        }
                                                        break;
                                                    }
                                                case (int)CPContentBaseClass.FieldTypeIdEnum.File:
                                                case (int)CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                                                        // 
                                                        // files -- copy pathFilename to tmp folder and save pathFilename to fieldValue
                                                        string FieldValue = csDataExportRecords.GetText(contentField.name).ToString();
                                                        if (!string.IsNullOrWhiteSpace(FieldValue)) {
                                                            string pathFilename = FieldValue;
                                                            cp.CdnFiles.Copy(pathFilename, tempExportPath + pathFilename, cp.TempFiles);
                                                            if (!tempPathFileList.Contains(tempExportPath + pathFilename)) {
                                                                tempPathFileList.Add(tempExportPath + pathFilename);
                                                                string path = FileController.getPath(pathFilename);
                                                                string filename = FileController.getFilename(pathFilename);
                                                                nodeList.Append(Environment.NewLine + "\t\t\t\t" + "<Resource name=\"" + System.Net.WebUtility.HtmlEncode(filename) + "\" type=\"content\" path=\"" + System.Net.WebUtility.HtmlEncode(path) + "\" />");
                                                            }
                                                        }
                                                        FieldNodes += Environment.NewLine + "\t\t\t" + "<field name=\"" + System.Net.WebUtility.HtmlEncode(contentField.name) + "\">" + FieldValue + "</field>";
                                                        break;
                                                    }

                                                case (int)CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                                        // 
                                                        // -- boolean
                                                        string FieldValue = csDataExportRecords.GetBoolean(contentField.name).ToString();
                                                        FieldNodes += Environment.NewLine + "\t\t\t" + "<field name=\"" + System.Net.WebUtility.HtmlEncode(contentField.name) + "\">" + FieldValue + "</field>";
                                                        break;
                                                    }

                                                case (int)CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                                case (int)CPContentBaseClass.FieldTypeIdEnum.FileJavaScript:
                                                case (int)CPContentBaseClass.FieldTypeIdEnum.FileText:
                                                case (int)CPContentBaseClass.FieldTypeIdEnum.FileXML: {
                                                        // 
                                                        // -- text files
                                                        string FieldValue = csDataExportRecords.GetText(contentField.name);
                                                        FieldValue = ExportController.encodeCData(FieldValue);
                                                        FieldNodes += Environment.NewLine + "\t\t\t" + "<field name=\"" + System.Net.WebUtility.HtmlEncode(contentField.name) + "\">" + FieldValue + "</field>";
                                                        break;
                                                    }

                                                case (int)CPContentBaseClass.FieldTypeIdEnum.Integer: {
                                                        // 
                                                        // -- integer
                                                        string FieldValue = csDataExportRecords.GetInteger(contentField.name).ToString();
                                                        FieldNodes += Environment.NewLine + "\t\t\t" + "<field name=\"" + System.Net.WebUtility.HtmlEncode(contentField.name) + "\">" + FieldValue + "</field>";
                                                        break;
                                                    }

                                                case (int)CPContentBaseClass.FieldTypeIdEnum.Currency:
                                                case (int)CPContentBaseClass.FieldTypeIdEnum.Float: {
                                                        // 
                                                        // -- numbers
                                                        string FieldValue = csDataExportRecords.GetNumber(contentField.name).ToString();
                                                        FieldNodes += Environment.NewLine + "\t\t\t" + "<field name=\"" + System.Net.WebUtility.HtmlEncode(contentField.name) + "\">" + FieldValue + "</field>";
                                                        break;
                                                    }

                                                case (int)CPContentBaseClass.FieldTypeIdEnum.Date: {
                                                        // 
                                                        // -- date
                                                        string outputValue = "";
                                                        DateTime outputDateValue = csDataExportRecords.GetDate(contentField.name);
                                                        if (!outputDateValue.Equals(DateTime.MinValue)) {
                                                            outputValue = csDataExportRecords.GetDate(contentField.name).ToString();
                                                        }
                                                        FieldNodes += Environment.NewLine + "\t\t\t" + "<field name=\"" + System.Net.WebUtility.HtmlEncode(contentField.name) + "\">" + outputValue + "</field>";
                                                        break;
                                                    }

                                                case (int)CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                                        // 
                                                        // -- lookup
                                                        string outputValue = "";
                                                        int lookupRecordId = csDataExportRecords.GetInteger(contentField.name);
                                                        if ((lookupRecordId != 0)) {
                                                            if (contentField.lookupContentId > 0) {
                                                                // 
                                                                // content lookup
                                                                // 
                                                                string contentFieldLookupContentName = cp.Content.GetName(contentField.lookupContentId);
                                                                if (!string.IsNullOrEmpty(contentFieldLookupContentName)) {
                                                                    using (CPCSBaseClass CSlookup = cp.CSNew()) {
                                                                        CSlookup.OpenRecord(contentFieldLookupContentName, lookupRecordId);
                                                                        if (CSlookup.OK()) {
                                                                            outputValue = CSlookup.GetText("ccguid");
                                                                            if (string.IsNullOrEmpty(outputValue)) {
                                                                                outputValue = cp.Utils.CreateGuid();
                                                                                CSlookup.SetField("ccGuid", outputValue);
                                                                            }
                                                                        }
                                                                        CSlookup.Close();
                                                                    }
                                                                }
                                                            } else if (!string.IsNullOrEmpty(contentField.lookupList)) {
                                                                // 
                                                                // list lookup, ok to save integer
                                                                // 
                                                                outputValue = lookupRecordId.ToString();
                                                            }
                                                        }
                                                        FieldNodes += Environment.NewLine + "\t\t\t" + "<field name=\"" + System.Net.WebUtility.HtmlEncode(contentField.name) + "\">" + outputValue + "</field>";
                                                        break;
                                                    }

                                                default: {
                                                        // 
                                                        // text types
                                                        string FieldValue = csDataExportRecords.GetText(contentField.name);
                                                        FieldValue = ExportController.encodeCData(FieldValue);
                                                        FieldNodes += Environment.NewLine + "\t\t\t" + "<field name=\"" + System.Net.WebUtility.HtmlEncode(contentField.name) + "\">" + FieldValue + "</field>";
                                                        break;
                                                    }
                                            }
                                        }
                                        RecordNodes.Append(""
                                                + Environment.NewLine + "\t\t" + "<record content=\"" + System.Net.WebUtility.HtmlEncode(dataExport.contentName) + "\" guid=\"" + dataExport.recordGuid + "\" name=\"" + System.Net.WebUtility.HtmlEncode(dataExport.recordName) + "\">"
                                                + FieldNodes
                                                + Environment.NewLine + "\t\t" + "</record>");
                                        csDataExportRecords.GoNext();
                                    }
                                }
                                csDataExportRecords.Close();
                            }
                        }
                    }
                    if (RecordNodes.Length > 0)
                        nodeList.Append(""
                            + Environment.NewLine + "\t" + "<data>"
                            + RecordNodes.ToString()
                            + Environment.NewLine + "\t" + "</data>");
                }
                return nodeList.ToString();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
