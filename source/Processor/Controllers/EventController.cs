
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Data;

namespace Contensive.Processor.Controllers {
    public class EventController {
        //
        //====================================================================================================
        /// <summary>
        /// Throw an addon event, which will call all addons registered to handle it
        /// </summary>
        /// <param name="eventNameIdOrGuid"></param>
        /// <returns></returns>
        public static string throwEvent(CoreController core, string eventNameIdOrGuid) {
            string returnString = "";
            try {
                using var cs = new CsModel(core);
                string sql = "select distinct c.addonId"
                    + " from ((ccAddonEvents e"
                    + " left join ccAddonEventCatchers c on c.eventId=e.id)"
                    + " left join ccAggregateFunctions a on a.id=c.addonid)"
                    + " where ";
                if (eventNameIdOrGuid.isNumeric()) {
                    sql += "e.id=" + DbController.encodeSQLNumber(double.Parse(eventNameIdOrGuid));
                } else if (GenericController.isGuid(eventNameIdOrGuid)) {
                    sql += "e.ccGuid=" + DbController.encodeSQLText(eventNameIdOrGuid);
                } else {
                    sql += "e.name=" + DbController.encodeSQLText(eventNameIdOrGuid);
                }
                sql += " order by c.addonid desc";
                if (!cs.openSql(sql)) {
                    //
                    // event not found
                    if (eventNameIdOrGuid.isNumeric()) {
                        //
                        // can not create an id
                    } else if (GenericController.isGuid(eventNameIdOrGuid)) {
                        //
                        // create event with Guid and id for name
                        using var cs2 = new CsModel(core);
                        cs2.insert("add-on Events");
                        cs2.set("ccguid", eventNameIdOrGuid);
                        cs2.set("name", "Event " + cs2.getInteger("id").ToString());
                    } else if (!string.IsNullOrEmpty(eventNameIdOrGuid)) {
                        //
                        // create event with name
                        using var cs3 = new CsModel(core);
                        cs3.insert("add-on Events");
                        cs3.set("name", eventNameIdOrGuid);
                    }
                } else {
                    //
                    // -- event found, check if there are addons to run
                    while (cs.ok()) {
                        int addonid = cs.getInteger("addonid");
                        if (addonid != 0) {
                            var addon = core.cacheRuntime.addonCache.create(addonid);
                            returnString += core.addon.execute(addon, new CPUtilsBaseClass.addonExecuteContext {
                                addonType = CPUtilsBaseClass.addonContext.ContextSimple,
                                errorContextMessage = "calling handler addon id [" + addonid + "] for event [" + eventNameIdOrGuid + "]"
                            });
                        }
                        cs.goNext();
                    }
                }
                cs.close();
            } catch (Exception ex) {
                LogController.logError(core, ex);
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

