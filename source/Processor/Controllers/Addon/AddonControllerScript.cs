
using Contensive.Models.Db;
using Contensive.Processor.Exceptions;
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
    public static class AddonControllerScript {
        //
        //====================================================================================================
        /// <summary>
        /// execute vb script
        /// </summary>
        /// <param name="core"></param>
        /// <param name="addon"></param>
        /// <returns></returns>
        public static string execute_Script_VBScript( CoreController core, ref AddonModel addon) {
            string returnText = "";
            try {
                // todo - move locals
                using (var engine = new Microsoft.ClearScript.Windows.VBScriptEngine()) {
                    //var engine = new Microsoft.ClearScript.Windows.VBScriptEngine(Microsoft.ClearScript.Windows.WindowsScriptEngineFlags.EnableDebugging);
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
                    //
                    // -- adding cclib
                    try {
                        MainCsvScriptCompatibilityClass mainCsv = new MainCsvScriptCompatibilityClass(core);
                        engine.AddHostObject("ccLib", mainCsv);
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string errorMessage = getScriptEngineExceptionMessage(ex, "Adding cclib compatibility object ");
                        LogController.logError(core, ex, errorMessage);
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        LogController.logError(core, ex);
                        throw;
                    }
                    //
                    // -- adding cp
                    try {
                        engine.AddHostObject("cp", core.cpParent);
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string errorMessage = getScriptEngineExceptionMessage(ex, "Adding cp object ");
                        LogController.logError(core, ex, errorMessage);
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        LogController.logError(core, ex);
                        throw;
                    }
                    //
                    // -- execute code
                    try {
                        engine.Execute(addon.scriptingCode);
                        object returnObj = engine.Evaluate(entryPoint);
                        returnText = AddonController.convertAddonReturntoString(returnObj);
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string errorMessage = getScriptEngineExceptionMessage(ex, "executing script ");
                        LogController.logError(core, ex, errorMessage);
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        string addonDescription = AddonController.getAddonDescription(core, addon);
                        string errorMessage = "Error executing addon script, " + addonDescription;
                        throw new GenericException(errorMessage, ex);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnText;
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
                        LogController.logError(core, ex, errorMessage);
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        LogController.logError(core, ex, "Clearscript Javascript exception.");
                        throw;
                    }
                    //
                    // -- load cp
                    try {
                        engine.AddHostObject("cp", core.cpParent);
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string errorMessage = getScriptEngineExceptionMessage(ex, "Adding cp object");
                        LogController.logError(core, ex, errorMessage);
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        LogController.logError(core, ex, "Clearscript Javascript exception.");
                        throw;
                    }
                    //
                    // -- execute code
                    try {
                        engine.Execute(addon.scriptingCode);
                        object returnObj = engine.Evaluate(entryPoint);
                        returnText = AddonController.convertAddonReturntoString(returnObj);
                    } catch (Microsoft.ClearScript.ScriptEngineException ex) {
                        string errorMessage = getScriptEngineExceptionMessage(ex, "executing script");
                        LogController.logError(core, ex, errorMessage);
                        throw new GenericException(errorMessage, ex);
                    } catch (Exception ex) {
                        string addonDescription = AddonController.getAddonDescription(core, addon);
                        string errorMessage = "Error executing addon script, " + addonDescription;
                        throw new GenericException(errorMessage, ex);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
            return returnText;
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
    }
}