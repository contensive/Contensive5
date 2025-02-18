
using Contensive.BaseClasses;
using System;

namespace Contensive.Processor.Controllers {
    public static class EventController {
        //
        //====================================================================================================
        /// <summary>
        /// Throw an addon event, which will call all addons registered to handle it
        /// </summary>
        /// <param name="eventGuid"></param>
        /// <returns></returns>
        public static string throwEventByGuid(CoreController core, string eventGuid) {
            string returnString = "";
            try {
                using var cs = new CsModel(core);
                string sql = "" +
                    $"select distinct c.addonId" +
                    $" from ((ccAddonEvents e" +
                    $" left join ccAddonEventCatchers c on c.eventId=e.id) " +
                    $" left join ccAggregateFunctions a on a.id=c.addonid)" +
                    $" where e.ccGuid={DbController.encodeSQLText(eventGuid)}" +
                    $" order by c.addonid desc";
                if (!cs.openSql(sql)) {
                    //
                    // event not found
                    using var cs2 = new CsModel(core);
                    cs2.insert("add-on Events");
                    cs2.set("ccguid", eventGuid);
                    cs2.set("name", "Event " + cs2.getInteger("id").ToString());
                } else {
                    //
                    // -- event found, check if there are addons to run
                    while (cs.ok()) {
                        int addonid = cs.getInteger("addonid");
                        if (addonid != 0) {
                            var addon = core.cacheRuntime.addonCache.create(addonid);
                            returnString += core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                                addonType = CPUtilsBaseClass.addonContext.ContextSimple,
                                errorContextMessage = "calling handler addon id [" + addonid + "] for event [" + eventGuid + "]"
                            });
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
        /// <summary>
        /// Throw an addon event, which will call all addons registered to handle it
        /// </summary>
        /// <param name="core"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static string throwEventByName(CoreController core, string eventName) {
            string returnString = "";
            try {
                using var cs = new CsModel(core);
                string sql = "select distinct c.addonId" +
                    " from ((ccAddonEvents e" +
                    " left join ccAddonEventCatchers c on c.eventId=e.id)" +
                    " left join ccAggregateFunctions a on a.id=c.addonid)" +
                    " where" +
                    " e.name=" + DbController.encodeSQLText(eventName);
                sql += " order by c.addonid desc";
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
                        int addonid = cs.getInteger("addonid");
                        if (addonid != 0) {
                            var addon = core.cacheRuntime.addonCache.create(addonid);
                            returnString += core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                                addonType = CPUtilsBaseClass.addonContext.ContextSimple,
                                errorContextMessage = "calling handler addon id [" + addonid + "] for event [" + eventName + "]"
                            });
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
        /// <summary>
        /// Throw an addon event, which will call all addons registered to handle it
        /// </summary>
        /// <param name="core"></param>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public static string throwEventById(CoreController core, int eventId) {
            string returnString = "";
            try {
                using var cs = new CsModel(core);
                string sql = "" +
                    "select distinct c.addonId" +
                    " from ((ccAddonEvents e" +
                    " left join ccAddonEventCatchers c on c.eventId=e.id)" +
                    " left join ccAggregateFunctions a on a.id=c.addonid)" +
                    $" where e.id={eventId} " +
                    $" order by c.addonid desc";
                if (!cs.openSql(sql)) {
                    //
                    // event not found, cannot create an id
                    return "";
                } else {
                    //
                    // -- event found, check if there are addons to run
                    while (cs.ok()) {
                        int addonid = cs.getInteger("addonid");
                        if (addonid != 0) {
                            var addon = core.cacheRuntime.addonCache.create(addonid);
                            returnString += core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                                addonType = CPUtilsBaseClass.addonContext.ContextSimple,
                                errorContextMessage = "calling handler addon id [" + addonid + "] for event [" + eventId + "]"
                            });
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

