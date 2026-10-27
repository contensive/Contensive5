
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

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

