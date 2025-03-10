﻿
using Contensive.Models.Db;
using Contensive.Exceptions;
using System;
using System.Collections;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// run addons
    /// - first routine should be constructor
    /// - disposable region at end
    /// - if disposable is not needed add: not IDisposable - not contained classes that need to be disposed
    /// </summary>
    public static class AddonScriptController {
        //
        //====================================================================================================
        /// <summary>
        /// execute vb script
        /// </summary>
        /// <param name="core"></param>
        /// <param name="addon"></param>
        /// <returns></returns>
        public static string execute_Script_VBScript( CoreController core, ref AddonModel addon) {
            string hint = "";
            try {
                // todo - move locals
                hint += "enter";
                // -- error in core 8, 
                // -- System.IO.FileNotFoundException: 'Could not load file or assembly 'WindowsBase, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'. The system cannot find the file specified.'
                // 
                // https://stackoverflow.com/questions/76864510/how-to-resolve-could-not-load-type-system-windows-threading-dispatchertimer
                //
                // -- https://docs.telerik.com/devtools/document-processing/knowledge-base/troubleshooting-windowsbase-error
                // implies assemblies not named windows, but namespaces are still windows
                //
                using (var engine = new Microsoft.ClearScript.Windows.VBScriptEngine()) {
                    string entryPoint = addon.scriptingEntryPoint;
                    if (string.IsNullOrEmpty(entryPoint)) {
                        //
                        // -- compatibility mode, if no entry point given, if the code starts with "function myFuncton()" and add "call myFunction()"
                        int pos = addon.scriptingCode.IndexOf("function", StringComparison.CurrentCultureIgnoreCase);
                        if (pos >= 0) {
                            entryPoint = addon.scriptingCode.Substring(pos + 9);
                            pos = entryPoint.IndexOf("\r");
                            if (pos > 0) {
                                entryPoint = entryPoint.Substring(0, pos);
                            }
                            pos = entryPoint.IndexOf("\n");
                            if (pos > 0) {
                                entryPoint = entryPoint.Substring(0, pos);
                            }
                            pos = entryPoint.IndexOf("(");
                            if (pos > 0) {
                                entryPoint = entryPoint.Substring(0, pos);
                            }
                        }
                    } else {
                        //
                        // -- etnry point provided, remove "()" if included and add to code
                        int pos = entryPoint.IndexOf("(");
                        if (pos > 0) {
                            entryPoint = entryPoint.Substring(0, pos);
                        }
                    }
                    hint += ",entrypoint[" + entryPoint + "]";
                    //
                    // -- adding cclib
                    try {
                        hint += ",add cclib";
                        MainCsvScriptCompatibilityClass mainCsv = new(core);
                        engine.AddHostObject("ccLib", mainCsv);
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string errorMessage = getScriptEngineExceptionMessage(ex, "Adding cclib compatibility object ex-6, hint[" + hint + "]");
                        logger.Error(ex, $"{errorMessage}, {core.logCommonMessage}");
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        logger.Error($"{core.logCommonMessage}", ex, "ex-7, hint[" + hint + "]");
                        throw;
                    }                    //
                    // -- adding cp
                    try {
                        hint += ",add cp";
                        engine.AddHostObject("cp", core.cpParent);
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string errorMessage = getScriptEngineExceptionMessage(ex, "Adding cp object, ex-5, hint[" + hint + "] ");
                        logger.Error(ex, $"{errorMessage}, {core.logCommonMessage}");
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        logger.Error($"{core.logCommonMessage}", ex, "ex-4, hint[" + hint + "]");
                        throw;
                    }
                    //
                    // -- execute code
                    try {
                        hint += ",execute code";
                        engine.Execute(addon.scriptingCode);
                        object returnObj = engine.Evaluate(entryPoint);
                        string returnText = AddonController.convertAddonReturntoString(returnObj);
                        //
                        // -- special case. Scripts that do not set return value, create empty object. It is a script bug, but too hard to fix all.
                        if (returnText == "{}") { return ""; }
                        return returnText;
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string errorMessage = getScriptEngineExceptionMessage(ex, "executing script, hint[" + hint + "]");
                        logger.Error(ex, $"{errorMessage}, {core.logCommonMessage}");
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        string addonDescription = AddonController.getAddonDescription(core, addon);
                        string errorMessage = "Error executing addon script, ex-3, hint[" + hint + "], " + addonDescription;
                        throw new GenericException(errorMessage, ex);
                    }
                }
            } catch (Exception ex) {
                logger.Error($"{core.logCommonMessage}", ex, "ex-1, hint [" + hint + "]");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// execute jscript script
        /// </summary>
        /// <param name="addon"></param>
        /// <param name="core"></param>
        /// <returns></returns>
        public static string execute_Script_JScript(CoreController core, ref AddonModel addon) {
            string returnText = "";
            try {
                // todo - move locals
                var flags = Microsoft.ClearScript.Windows.WindowsScriptEngineFlags.None; // .EnableDebugging;
                using (var engine = new Microsoft.ClearScript.Windows.JScriptEngine(flags)) {
                    //
                    string entryPoint = addon.scriptingEntryPoint;
                    if (string.IsNullOrEmpty(entryPoint)) {
                        //
                        // -- compatibility mode, if no entry point given, if the code starts with "function myFuncton()" and add "call myFunction()"
                        int pos = addon.scriptingCode.IndexOf("function", StringComparison.CurrentCultureIgnoreCase);
                        if (pos >= 0) {
                            entryPoint = addon.scriptingCode.Substring(pos + 9);
                            pos = entryPoint.IndexOf("\r");
                            if (pos > 0) {
                                entryPoint = entryPoint.Substring(0, pos);
                            }
                            pos = entryPoint.IndexOf("\n");
                            if (pos > 0) {
                                entryPoint = entryPoint.Substring(0, pos);
                            }
                            pos = entryPoint.IndexOf("(");
                            if (pos > 0) {
                                entryPoint = entryPoint.Substring(0, pos);
                            }
                        }
                    }
                    //
                    // -- verify entry point ends in "()"
                    int posClose = entryPoint.IndexOf("(");
                    if (posClose < 0) {
                        entryPoint = entryPoint.Trim() + "()";
                    }
                    //
                    // -- load cclib object
                    try {
                        MainCsvScriptCompatibilityClass mainCsv = new MainCsvScriptCompatibilityClass(core);
                        engine.AddHostObject("ccLib", mainCsv);
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string errorMessage = getScriptEngineExceptionMessage(ex, "Adding cclib compatibility object");
                        logger.Error(ex, $"{errorMessage}, {core.logCommonMessage}");
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        logger.Error($"{core.logCommonMessage}", ex, "Clearscript Javascript exception.");
                        throw;
                    }
                    //
                    // -- load cp
                    try {
                        engine.AddHostObject("cp", core.cpParent);
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string errorMessage = getScriptEngineExceptionMessage(ex, "Adding cp object");
                        logger.Error(ex, $"{errorMessage}, {core.logCommonMessage}");
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        logger.Error($"{core.logCommonMessage}", ex, "Clearscript Javascript exception.");
                        throw;
                    }
                    //
                    // -- execute code
                    try {
                        engine.Execute(addon.scriptingCode);
                        object returnObj = engine.Evaluate(entryPoint);
                        returnText = AddonController.convertAddonReturntoString(returnObj);
                        //
                        // -- special case. Scripts that do not set return value, create empty object. It is a script bug, but too hard to fix all.
                        if (returnText == "{}") { return ""; }
                        return returnText;
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string addonDescription = AddonController.getAddonDescription(core, addon);
                        string errorMessage = "Error executing addon script, " + addonDescription;
                        errorMessage = getScriptEngineExceptionMessage(ex, errorMessage);
                        logger.Error(ex, $"{errorMessage}, {core.logCommonMessage}");
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        string addonDescription = AddonController.getAddonDescription(core, addon);
                        string errorMessage = "Error executing addon script, " + addonDescription;
                        throw new GenericException(errorMessage, ex);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// translate script engine exception to message for log
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="scopeDescription"></param>
        /// <returns></returns>
        private static string getScriptEngineExceptionMessage(Microsoft.ClearScript.ScriptEngineException ex,string scopeDescription) {
            string errorMsg = "Clearscript exception, " + scopeDescription;
            errorMsg += "\nex [" + ex.Message + "]";
            if (ex.Data.Count > 0) {
                foreach (DictionaryEntry de in ex.Data) {
                    errorMsg += "\nkey [" + de.Key + "] = [" + de.Value + "]";
                }
            }
            if (ex.ErrorDetails != null) { errorMsg += "\n" + ex.ErrorDetails; }
            if (ex.InnerException != null) { errorMsg += "\nInner Exception: " + ex.InnerException.ToString(); }
            return errorMsg;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}