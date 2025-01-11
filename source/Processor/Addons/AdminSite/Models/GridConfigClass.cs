
using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3.Model;
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
        public Dictionary<string, GridConfigFindWordClass> findWords { get; set; }
        public bool activeOnly { get; set; }
        public bool lastEditedByMe { get; set; }
        public bool lastEditedToday { get; set; }
        public bool lastEditedPast7Days { get; set; }
        public bool lastEditedPast30Days { get; set; }
        public bool open { get; set; }
        public Dictionary<string, GridConfigSortClass> sorts { get; set; }
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
        /// <summary>
        /// Constructor
        /// </summary>
        public GridConfigClass() {

        }
        //
        /// <summary>
        /// Constructor for admin to replace get() method
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        public GridConfigClass(CoreController core, AdminDataModel adminData) {
            try {
                //
                // -- default values
                contentID = adminData.adminContent.id;
                activeOnly = false;
                lastEditedByMe = false;
                lastEditedToday = false;
                lastEditedPast7Days = false;
                lastEditedPast30Days = false;
                loaded = true;
                open = false;
                pageNumber = 1;
                recordsPerPage = adminData.listViewRecordsPerPage;
                recordTop = 0;
                groupList = new string[groupListCntMax];
                groupListCnt = 0;
                columns = new List<IndexConfigColumnClass>();
                sorts = new Dictionary<string, GridConfigSortClass>(StringComparer.InvariantCultureIgnoreCase);
                findWords = new Dictionary<string, GridConfigFindWordClass>(StringComparer.InvariantCultureIgnoreCase);
                allowDelete = true;
                allowFind = true;
                allowAddRow = false;
                allowColumnSort = true;
                allowHeaderAtBottom = true;
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
                                                    columns.Add(new IndexConfigColumnClass {
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
                                            sorts.Add(fieldName, new GridConfigSortClass {
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
                    if (recordsPerPage <= 0) {
                        recordsPerPage = Constants.RecordsPerPageDefault;
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
                                            findWords.Add(LineSplit[0], new GridConfigFindWordClass {
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
                                        if (groupListCnt < groupListCntMax) {
                                            groupList[groupListCnt] = ConfigListLines[Ptr];
                                            groupListCnt += 1;
                                        }
                                        Ptr += 1;
                                    }
                                    break;
                                case "cdeflist":
                                    Ptr += 1;
                                    subCDefID = GenericController.encodeInteger(ConfigListLines[Ptr]);
                                    break;
                                case "indexfiltercategoryid":
                                    // -- remove deprecated value
                                    Ptr += 1;
                                    break;
                                case "indexfilteractiveonly":
                                    activeOnly = true;
                                    break;
                                case "indexfilterlasteditedbyme":
                                    lastEditedByMe = true;
                                    break;
                                case "indexfilterlasteditedtoday":
                                    lastEditedToday = true;
                                    break;
                                case "indexfilterlasteditedpast7days":
                                    lastEditedPast7Days = true;
                                    break;
                                case "indexfilterlasteditedpast30days":
                                    lastEditedPast30Days = true;
                                    break;
                                case "indexfilteropen":
                                    open = true;
                                    break;
                                case "recordsperpage":
                                    Ptr += 1;
                                    recordsPerPage = GenericController.encodeInteger(ConfigListLines[Ptr]);
                                    if (recordsPerPage <= 0) {
                                        recordsPerPage = 50;
                                    }
                                    recordTop = DbController.getStartRecord(recordsPerPage, pageNumber);
                                    break;
                                case "pagenumber":
                                    Ptr += 1;
                                    pageNumber = GenericController.encodeInteger(ConfigListLines[Ptr]);
                                    if (pageNumber <= 0) {
                                        pageNumber = 1;
                                    }
                                    recordTop = DbController.getStartRecord(recordsPerPage, pageNumber);
                                    break;
                                default:
                                    break;
                            }
                        }
                        Ptr += 1;
                    }
                    if (recordsPerPage <= 0) {
                        recordsPerPage = Constants.RecordsPerPageDefault;
                    }
                }
                //
                // Setup defaults if not loaded
                //
                if ((columns.Count == 0) && (adminData.adminContent.adminColumns.Count > 0)) {
                    foreach (var keyValuePair in adminData.adminContent.adminColumns) {
                        columns.Add(new IndexConfigColumnClass {
                            Name = keyValuePair.Value.Name.ToLowerInvariant(),
                            Width = keyValuePair.Value.Width
                        });
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        /// <summary>
        /// Constructor for admin to replace get() method
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminData"></param>
        public GridConfigClass(CoreController core, GridConfigRequest request) {
            try {
                //
                // -- default values
                activeOnly = false;
                lastEditedByMe = false;
                lastEditedToday = false;
                lastEditedPast7Days = false;
                lastEditedPast30Days = false;
                loaded = true;
                open = false;
                pageNumber = 1;
                recordsPerPage = request.defaultRecordsPerPage;
                recordTop = 0;
                groupList = new string[groupListCntMax];
                groupListCnt = 0;
                columns = new List<IndexConfigColumnClass>();
                sorts = new Dictionary<string, GridConfigSortClass>(StringComparer.InvariantCultureIgnoreCase);
                findWords = new Dictionary<string, GridConfigFindWordClass>(StringComparer.InvariantCultureIgnoreCase);
                allowDelete = true;
                allowFind = true;
                allowAddRow = false;
                allowColumnSort = true;
                allowHeaderAtBottom = true;
                //
                // Setup Member Properties
                string ConfigList = core.userProperty.getText(request.gridPropertiesSaveName, "");
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
                                            columns.Add(new IndexConfigColumnClass {
                                                Name = fieldName,
                                                Width = encodeInteger(LineSplit[1]),
                                                SortDirection = 0,
                                                SortPriority = 0
                                            });
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
                                            sorts.Add(fieldName, new GridConfigSortClass {
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
                    if (recordsPerPage <= 0) {
                        recordsPerPage = Constants.RecordsPerPageDefault;
                    }
                }
                //
                // Setup Visit Properties
                ConfigList = core.visitProperty.getText(request.gridPropertiesSaveName, "");
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
                                            findWords.Add(LineSplit[0], new GridConfigFindWordClass {
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
                                        if (groupListCnt < groupListCntMax) {
                                            groupList[groupListCnt] = ConfigListLines[Ptr];
                                            groupListCnt += 1;
                                        }
                                        Ptr += 1;
                                    }
                                    break;
                                case "cdeflist":
                                    Ptr += 1;
                                    subCDefID = GenericController.encodeInteger(ConfigListLines[Ptr]);
                                    break;
                                case "indexfiltercategoryid":
                                    // -- remove deprecated value
                                    Ptr += 1;
                                    break;
                                case "indexfilteractiveonly":
                                    activeOnly = true;
                                    break;
                                case "indexfilterlasteditedbyme":
                                    lastEditedByMe = true;
                                    break;
                                case "indexfilterlasteditedtoday":
                                    lastEditedToday = true;
                                    break;
                                case "indexfilterlasteditedpast7days":
                                    lastEditedPast7Days = true;
                                    break;
                                case "indexfilterlasteditedpast30days":
                                    lastEditedPast30Days = true;
                                    break;
                                case "indexfilteropen":
                                    open = true;
                                    break;
                                case "recordsperpage":
                                    Ptr += 1;
                                    recordsPerPage = GenericController.encodeInteger(ConfigListLines[Ptr]);
                                    if (recordsPerPage <= 0) {
                                        recordsPerPage = 50;
                                    }
                                    recordTop = DbController.getStartRecord(recordsPerPage, pageNumber);
                                    break;
                                case "pagenumber":
                                    Ptr += 1;
                                    pageNumber = GenericController.encodeInteger(ConfigListLines[Ptr]);
                                    if (pageNumber <= 0) {
                                        pageNumber = 1;
                                    }
                                    recordTop = DbController.getStartRecord(recordsPerPage, pageNumber);
                                    break;
                                default:
                                    break;
                            }
                        }
                        Ptr += 1;
                    }
                    if (recordsPerPage <= 0) {
                        recordsPerPage = Constants.RecordsPerPageDefault;
                    }
                }
                //
                // Setup defaults if not loaded
                //
                if ((columns.Count == 0) && (request.sortableFields.Count > 0)) {
                    foreach (var sortableField in request.sortableFields) {
                        columns.Add(new IndexConfigColumnClass {
                            Name = sortableField.ToLowerInvariant(),
                            Width = 10
                        });
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
    }
    //
    /// <summary>
    /// When creating a new grid, the request object is used to define the grid
    /// </summary>
    public class GridConfigRequest {
        /// <summary>
        /// The number of rows per page to display by default
        /// </summary>
        public int defaultRecordsPerPage { get; set; }
        /// <summary>
        /// grid customizations can be made by users, and are stored in either user properties or visit properties. This prefix is used to identify the properties.
        /// This string is the cache name that identifies this use of the grid system. 
        /// For example, if this grid system is for the account list, the gridSavePrefix might the "AccountList"
        /// </summary>
        public string gridPropertiesSaveName { get; set; }
        /// <summary>
        /// The list of fields that can be sorted by the user
        /// </summary>
        public List<string> sortableFields { get; set; }
    }
    /// <summary>
    /// A class to define a sort for a grid
    /// </summary>
    public class GridConfigSortClass {
        /// <summary>
        /// The field name to sort by
        /// </summary>
        public string fieldName { get; set; }
        /// <summary>
        /// 1=forward, 2=reverse, 0=ignore/remove this sort
        /// </summary>
        public int direction { get; set; }
        /// <summary>
        /// 1...n, if multiple sorts, the order of the sort
        /// </summary>
        public int order { get; set; }
    }
    /// <summary>
    /// A class to define a find word for a grid
    /// </summary>
    public class GridConfigFindWordClass {
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
