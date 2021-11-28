
using System.Collections.Generic;
//
namespace Contensive.Processor.Models.Domain {
    //
    // Google Data Object construction in GetRemoteQuery
    //
    //
    public class ColsType {
        public string Type { get; set; }
        public string Id { get; set; }
        public string Label { get; set; }
        public string Pattern { get; set; }
    }
    //
    //
    public class CellType {
        public string v { get; set; }
        public string f { get; set; }
        public string p { get; set; }
    }
    //
    //
    public class RowsType {
        public List<CellType> Cell { get; set; }
    }
    //
    //
    public class GoogleDataType {
        public bool IsEmpty { get; set; }
        public List<ColsType> col { get; set; }
        public List<RowsType> row { get; set; }
    }
    //
    //
    public enum GoogleVisualizationStatusEnum {
        OK = 1,
        warning = 2,
        ErrorStatus = 3
    }
    //
    //
    public class GoogleVisualizationType {
        public string version { get; set; }
        public string reqid { get; set; }
        public GoogleVisualizationStatusEnum status { get; set; }
        public string[] warnings { get; set; }
        public string[] errors { get; set; }
        public string sig { get; set; }
        public GoogleDataType table { get; set; }
    }

}