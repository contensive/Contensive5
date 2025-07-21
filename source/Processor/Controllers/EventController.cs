
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Microsoft.Extensions.Logging;
using System;

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
        public static string throwEventByName(CoreController core, string eventName) {
            //
            core.cpParent.Log.Trace($"EventController.throwEventByName(string [{eventName}]) enter");
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
                        and(a.id is not null)
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
                                    errorContextMessage = "calling handler addon id [" + addonid + "] for event [" + eventName + "]"
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
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}

