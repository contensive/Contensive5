
using System;
using System.Collections.Generic;

namespace Contensive.Addons.Status {
    /// <summary>
    /// Model for server diagnostics status, serialized to JSON and stored in site property "ServerDiagnosticsStatus".
    /// Written by the CLI serverdiagnostic command, read by the StatusRemoteMethod (/status remote method).
    /// </summary>
    public class ServerDiagnosticsStatusModel {
        public DateTime lastCheckDate { get; set; }
        //
        // -- Windows Update fields
        public int windowsUpdateCount { get; set; }
        public List<string> windowsUpdateTitles { get; set; }
        public bool windowsUpdateCheckSuccessful { get; set; }
        public string windowsUpdateErrorMessage { get; set; }
        //
        // -- Domain Bindings fields
        public bool domainBindingsValid { get; set; }
        public string domainBindingsErrorMessage { get; set; }
        //
        // -- Drive Space fields
        public bool driveSpaceValid { get; set; }
        public string driveSpaceErrorMessage { get; set; }
        //
        // -- Log File fields
        public bool logFilesValid { get; set; }
        public string logFilesErrorMessage { get; set; }
        //
        // -- Alarm fields
        public bool alarmsValid { get; set; }
        public string alarmsErrorMessage { get; set; }
        //
        // -- TLS fields
        public bool tlsValid { get; set; }
        public string tlsErrorMessage { get; set; }
        //
        // -- Overall status
        public bool allChecksSuccessful { get; set; }
    }
}
