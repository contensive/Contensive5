
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor.Controllers {
    public static class EventController {
        //
        //====================================================================================================
        /// <summary>
        /// Throw an addon event, which will call all addons registered to handle it
        /// </summary>
        /// <param name="core"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static string throwEventByName(CoreController core, string eventName)
            => throwEventByName(core, eventName, new Dictionary<string, string>());
        //
        //====================================================================================================
        /// <summary>
        /// Throw an addon event with arguments, which will call all addons registered to handle it.
        /// Each catcher runs with the arguments as its argumentKeyValuePairs, so they are copied into doc properties and the catcher reads them with cp.Doc.GetText(key).
        /// </summary>
        /// <param name="core"></param>
        /// <param name="eventName"></param>
        /// <param name="eventArguments">null is treated as no arguments</param>
        /// <returns></returns>
        public static string throwEventByName(CoreController core, string eventName, Dictionary<string, string> eventArguments) {
            //
            core.cpParent.Log.Trace($"EventController.throwEventByName(string [{eventName}], arguments [{eventArguments?.Count ?? 0}]) enter");
            //
            string returnString = "";
            try {
                using var cs = new CsModel(core);
                string sql = @$"
                    select 
                        distinct a.id 
                    from 
                        ccAddonEvents e 
                        left join ccAddonEventCatchers c on c.eventId=e.id 
                        left join ccAggregateFunctions a on a.id=c.addonid
                    where 
                        e.name={DbController.encodeSQLText(eventName)}
                        --and(a.id is not null)
                    order by 
                        a.id desc
                    ";
                if (!cs.openSql(sql)) {
                    //
                    // event not found
                    using var cs3 = new CsModel(core);
                    cs3.insert("add-on Events");
                    cs3.set("name", eventName);
                } else {
                    //
                    // -- event found, check if there are addons to run
                    // -- argument keys become doc properties while the catchers run. Save the current values and restore (or clear) them after, so arguments never leak into the rest of the request
                    var savedProperties = saveDocProperties(core, eventArguments);
                    try {
                        while (cs.ok()) {
                            int addonid = cs.getInteger("id");
                            if (addonid != 0) {
                                var addon = core.cacheRuntime.addonCache.create(addonid);
                                if (addon != null) {
                                    //
                                    core.cpParent.Log.Trace($"EventController.throwEventByName, calling addon [{addon.id}, {addon.name}] for event [{eventName}]");
                                    //
                                    returnString += core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                                        addonType = CPUtilsBaseClass.addonContext.ContextSimple,
                                        argumentKeyValuePairs = copyArguments(eventArguments),
                                        errorContextMessage = $"calling handler addon id [{addonid}] for event [{eventName}]"
                                    });
                                }
                            }
                            cs.goNext();
                        }
                    } finally {
                        restoreDocProperties(core, savedProperties);
                    }
                }
                cs.close();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return returnString;
        }
        //
        //====================================================================================================
        /// <summary>
        /// the doc property of each argument key before the event. null means the key was not set.
        /// </summary>
        private static Dictionary<string, DocPropertyModel> saveDocProperties(CoreController core, Dictionary<string, string> eventArguments) {
            var result = new Dictionary<string, DocPropertyModel>();
            if (eventArguments == null) { return result; }
            foreach (var kvp in eventArguments) {
                if (string.IsNullOrEmpty(kvp.Key) || result.ContainsKey(kvp.Key)) { continue; }
                result.Add(kvp.Key, core.docProperties.containsKey(kvp.Key) ? core.docProperties.getProperty(kvp.Key) : null);
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// put back the doc properties saved before the event. Keys that were not set before are set to empty (there is no remove).
        /// </summary>
        private static void restoreDocProperties(CoreController core, Dictionary<string, DocPropertyModel> savedProperties) {
            try {
                foreach (var kvp in savedProperties) {
                    if (kvp.Value == null) {
                        core.docProperties.setProperty(kvp.Key, "");
                    } else {
                        core.docProperties.setProperty(kvp.Key, kvp.Value);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage},restoreDocProperties");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// a new dictionary for each catcher, so one catcher cannot change the arguments another catcher receives. null keys and values are skipped or made empty.
        /// </summary>
        /// <param name="eventArguments"></param>
        /// <returns></returns>
        public static Dictionary<string, string> copyArguments(Dictionary<string, string> eventArguments) {
            var result = new Dictionary<string, string>();
            if (eventArguments == null) { return result; }
            foreach (var kvp in eventArguments) {
                if (string.IsNullOrEmpty(kvp.Key)) { continue; }
                result[kvp.Key] = kvp.Value ?? "";
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}

