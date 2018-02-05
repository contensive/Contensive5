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
using Contensive.Core;
using Contensive.Core.Models.DbModels;
using Contensive.Core.Controllers;
using static Contensive.Core.Controllers.genericController;
using static Contensive.Core.constants;
// 
namespace Contensive.Core {
    public class mainCsvScriptCompatibilityClass {
        public const string ClassId = "D9099AAE-3FCB-4398-B94C-19EE7FA97B2B";
        public const string InterfaceId = "CE342EA5-339F-4C31-9F90-F878F527E17A";
        public const string EventsId = "21D9D0FB-9B5B-43C2-A7A5-3C84ABFAF90A";
        // 
        private coreController core;
        public mainCsvScriptCompatibilityClass(coreController core) {
            this.core = core;
        }
        //
        public void SetViewingProperty( string propertyName , string propertyValue ) {
            core.siteProperties.setProperty(propertyName, propertyValue);
        }

    }
}