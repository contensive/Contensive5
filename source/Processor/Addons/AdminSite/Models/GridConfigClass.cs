
using System;
using System.Collections.Generic;
using System.Linq;
using Contensive.Processor.Controllers;
using NLog;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Addons.AdminSite {
    public class GridConfigClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        internal const int groupListCntMax = 10;
        //
        public bool loaded { get; set; }
        public int contentID { get; set; }
        public int pageNumber { get; set; }
        public int recordsPerPage { get; set; }
        public int recordTop { get; set; }
        public Dictionary<string, IndexConfigFindWordClass> findWords { get; set; }
        public bool activeOnly { get; set; }
        public bool lastEditedByMe { get; set; }
        public bool lastEditedToday { get; set; }
        public bool lastEditedPast7Days { get; set; }
        public bool lastEditedPast30Days { get; set; }
        public bool open { get; set; }
        public Dictionary<string, IndexConfigSortClass> sorts { get; set; }
        public int groupListCnt { get; set; }
        public string[] groupList { get; set; }
        public List<IndexConfigColumnClass> columns { get; set; }
        public int subCDefID { get; set; }
        /// <summary>
        /// if true, the listgrid includes a delete checkbox row
        /// </summary>
        public bool allowDelete { get; set; }
        //
        public bool allowFind { get; set; }
        //
        public bool allowAddRow { get; set; }
        //
        public bool allowColumnSort { get; set; }
        public bool allowHeaderAtBottom { get; set; }
        //
        //=================================================================================
        /// <summary>
        /// Load the index config, if it is empty, setup defaults
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        /// <returns></returns>
        public static GridConfigClass get(CoreController core, AdminDataModel adminData) {
            GridConfigClass result = new() {
                contentID = adminData.adminContent.id,
                activeOnly = false,
                lastEditedByMe = false,
                lastEditedToday = false,
                lastEditedPast7Days = false,
                lastEditedPast30Days = false,
                loaded = true,
                open = false,
                pageNumber = 1,
                recordsPerPage = adminData.listViewRecordsPerPage,
                recordTop = 0,
                groupList = new string[groupListCntMax],
                groupListCnt = 0,
                columns = new List<IndexConfigColumnClass>(),
                sorts = new Dictionary<string, IndexConfigSortClass>(StringComparer.InvariantCultureIgnoreCase),
                findWords = new Dictionary<string, IndexConfigFindWordClass>(StringComparer.InvariantCultureIgnoreCase),
                allowDelete = true,
                allowFind = true,
                allowAddRow = false,
                allowColumnSort = true,
                allowHeaderAtBottom = true
            };
            try {
                //
                // Setup Member Properties
                string ConfigList = core.userProperty.getText(AdminDataModel.IndexConfigPrefix + encodeText(adminData.adminContent.id), "");
                if (!string.IsNullOrEmpty(ConfigList)) {
                    //
                    // load values
                    //
                    ConfigList += Environment.NewLine;
                    string[] ConfigListLines = GenericController.splitNewLine(ConfigList);
                    int Ptr = 0;
                    var fieldsThatAllowNotAuthorable = new List<string> { "id", "dateadded", "createdby", "modifieddate", "modifiedby", "ccguid", "contentcontrolid", "sortorder", "active" };
                    while (Ptr < ConfigListLines.GetUpperBound(0)) {
                        //
                        // check next line
                        //
                        string ConfigListLine = GenericController.toLCase(ConfigListLines[Ptr]);
                        if (!string.IsNullOrEmpty(ConfigListLine)) {
                            if (ConfigListLine.Equals("columns")) {
                                Ptr += 1;
                                while (!string.IsNullOrEmpty(ConfigListLines[Ptr])) {
                                    string Line = ConfigListLines[Ptr];
                                    string[] LineSplit = Line.Split('\t');
                                    if (LineSplit.GetUpperBound(0) > 0) {
                                        string fieldName = LineSplit[0].Trim().ToLowerInvariant();
                                        if (!string.IsNullOrWhiteSpace(fieldName)) {
                                            if (adminData.adminContent.fields.ContainsKey(fieldName)) {
                                                if (adminData.adminContent.fields[fieldName].authorable || fieldsThatAllowNotAuthorable.Contains(fieldName, StringComparer.OrdinalIgnoreCase)) {
                                                    result.columns.Add(new IndexConfigColumnClass {
                                                        Name = fieldName,
                                                        Width = encodeInteger(LineSplit[1]),
                                                        SortDirection = 0,
                                                        SortPriority = 0
                                                    });
                                                }
                                            }
                                        }
                                    }
                                    Ptr += 1;
                                }

                            } else if (ConfigListLine.Equals("sorts")) {
                                Ptr += 1;
                                int orderPtr = 0;
                                while (!string.IsNullOrEmpty(ConfigListLines[Ptr])) {
                                    string[] LineSplit = ConfigListLines[Ptr].Split('\t');
                                    if (LineSplit.GetUpperBound(0) == 1) {
                                        string fieldName = LineSplit[0].Trim().ToLowerInvariant();
                                        if (!string.IsNullOrWhiteSpace(fieldName)) {
                                            result.sorts.Add(fieldName, new IndexConfigSortClass {
                                                fieldName = fieldName,
                                                direction = ((LineSplit[1] == "1") ? 1 : 2),
                                                order = ++orderPtr
                                            });
                                        }
                                    }
                                    Ptr += 1;
                                }
                            }
                        }
                        Ptr += 1;
                    }
                    if (result.recordsPerPage <= 0) {
                        result.recordsPerPage = Constants.RecordsPerPageDefault;
                    }
                }
                //
                // Setup Visit Properties
                ConfigList = core.visitProperty.getText(AdminDataModel.IndexConfigPrefix + encodeText(adminData.adminContent.id), "");
                if (!string.IsNullOrEmpty(ConfigList)) {
                    //
                    // load values
                    ConfigList += Environment.NewLine;
                    string[] ConfigListLines = GenericController.splitNewLine(ConfigList);
                    int Ptr = 0;
                    while (Ptr < ConfigListLines.GetUpperBound(0)) {
                        //
                        // check next line
                        string ConfigListLine = GenericController.toLCase(ConfigListLines[Ptr]);
                        if (!string.IsNullOrEmpty(ConfigListLine)) {
                            switch (ConfigListLine) {
                                case "findwordlist":
                                    Ptr += 1;
                                    while (!string.IsNullOrEmpty(ConfigListLines[Ptr])) {
                                        string Line = ConfigListLines[Ptr];
                                        string[] LineSplit = Line.Split('\t');
                                        if (LineSplit.GetUpperBound(0) > 1) {
                                            result.findWords.Add(LineSplit[0], new IndexConfigFindWordClass {
                                                Name = LineSplit[0],
                                                Value = LineSplit[1],
                                                MatchOption = (FindWordMatchEnum)GenericController.encodeInteger(LineSplit[2])
                                            });
                                        }
                                        Ptr += 1;
                                    }
                                    break;
                                case "grouplist":
                                    Ptr += 1;
                                    while ((Ptr < ConfigListLines.GetUpperBound(0)) && !string.IsNullOrEmpty(ConfigListLines[Ptr])) {
                                        if (result.groupListCnt < groupListCntMax) {
                                            result.groupList[result.groupListCnt] = ConfigListLines[Ptr];
                                            result.groupListCnt += 1;
                                        }
                                        Ptr += 1;
                                    }
                                    break;
                                case "cdeflist":
                                    Ptr += 1;
                                    result.subCDefID = GenericController.encodeInteger(ConfigListLines[Ptr]);
                                    break;
                                case "indexfiltercategoryid":
                                    // -- remove deprecated value
                                    Ptr += 1;
                                    break;
                                case "indexfilteractiveonly":
                                    result.activeOnly = true;
                                    break;
                                case "indexfilterlasteditedbyme":
                                    result.lastEditedByMe = true;
                                    break;
                                case "indexfilterlasteditedtoday":
                                    result.lastEditedToday = true;
                                    break;
                                case "indexfilterlasteditedpast7days":
                                    result.lastEditedPast7Days = true;
                                    break;
                                case "indexfilterlasteditedpast30days":
                                    result.lastEditedPast30Days = true;
                                    break;
                                case "indexfilteropen":
                                    result.open = true;
                                    break;
                                case "recordsperpage":
                                    Ptr += 1;
                                    result.recordsPerPage = GenericController.encodeInteger(ConfigListLines[Ptr]);
                                    if (result.recordsPerPage <= 0) {
                                        result.recordsPerPage = 50;
                                    }
                                    result.recordTop = DbController.getStartRecord(result.recordsPerPage, result.pageNumber);
                                    break;
                                case "pagenumber":
                                    Ptr += 1;
                                    result.pageNumber = GenericController.encodeInteger(ConfigListLines[Ptr]);
                                    if (result.pageNumber <= 0) {
                                        result.pageNumber = 1;
                                    }
                                    result.recordTop = DbController.getStartRecord(result.recordsPerPage, result.pageNumber);
                                    break;
                                default:
                                    break;
                            }
                        }
                        Ptr += 1;
                    }
                    if (result.recordsPerPage <= 0) {
                        result.recordsPerPage = Constants.RecordsPerPageDefault;
                    }
                }
                //
                // Setup defaults if not loaded
                //
                if ((result.columns.Count == 0) && (adminData.adminContent.adminColumns.Count > 0)) {
                    foreach (var keyValuePair in adminData.adminContent.adminColumns) {
                        result.columns.Add(new IndexConfigColumnClass {
                            Name = keyValuePair.Value.Name.ToLowerInvariant(),
                            Width = keyValuePair.Value.Width
                        });
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
            return result;
        }
    }
    //
    public class IndexConfigSortClass {
        public string fieldName { get; set; }
        // 1=forward, 2=reverse, 0=ignore/remove this sort
        public int direction { get; set; }
        // 1...n, if multiple sorts, the order of the sort
        public int order { get; set; }
    }
    //
    public class IndexConfigFindWordClass {
        public string Name { get; set; }
        public string Value { get; set; }
        public int Type { get; set; }
        public FindWordMatchEnum MatchOption { get; set; }
    }
    //
    public class IndexConfigColumnClass {
        public string Name { get; set; }
        public int Width { get; set; }
        public int SortPriority { get; set; }
        public int SortDirection { get; set; }
    }
}
