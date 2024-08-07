﻿
using System;
using System.Linq;
using System.Collections.Generic;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Controllers {
    //
    /// <summary>
    ///   A LinkAlias name is a unique string that identifies a page on the site.
    ///   A page on the site is generated from the PageID, and the QueryStringSuffix
    ///   PageID - obviously, this is the ID of the page
    ///   QueryStringSuffix - other things needed on the Query to display the correct content.
    ///   The Suffix is needed in cases like when an Add-on is embedded in a page. The URL to that content becomes the pages
    ///   Link, plus the suffix needed to find the content.
    ///   When you make the menus, look up the most recent Link Alias entry with the pageID, and a blank QueryStringSuffix
    ///   The Link Alias table no longer needs the Link field.
    /// </summary>
    public class LinkAliasController {
        //
        //====================================================================================================
        /// <summary>
        /// Returns the Alias link (SourceLink) from the actual link (DestinationLink).
        /// if not found, it creates the link alias from the default
        /// </summary>
        public static string getLinkAlias(CoreController core, int pageId, string queryStringSuffix, string defaultPageName) {
            string result;
            //
            // -- get link alias from cacheStore
            string linkAliasKey = $"{pageId}.{queryStringSuffix}";
            if (core.cacheRuntime.linkAliasKeyDict.ContainsKey(linkAliasKey)) {
                //
                // -- get link alias from cache created for routemap
                result = core.cacheRuntime.linkAliasKeyDict[linkAliasKey].name;
                if (result.left(1) != "/") { result = "/" + result; }
                return result;
            }
            //
            // -- failover - get link alias from table
            List<LinkAliasModel> linkAliasList = LinkAliasModel.createPageList(core.cpParent, pageId, queryStringSuffix);
            if (linkAliasList.Count == 0) {
                //
                // -- this page/qs does not have a link alias, add the default page name
                return addLinkAlias(core, defaultPageName, pageId, queryStringSuffix);
            }
            result = linkAliasList.First().name;
            if (result.left(1) != "/") { result = "/" + result; }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="linkAliasPhrase">non-normalized link alias</param>
        /// <param name="PageID"></param>
        /// <param name="QueryStringSuffix"></param>
        /// <param name="OverRideDuplicate">Should alway be true except in admin edit page where the user may not realize</param>
        /// <param name="DupCausesWarning">Always false except in admin edit where the user needs a warning</param>
        public static string addLinkAlias(CoreController core, string linkAliasPhrase, int PageID, string QueryStringSuffix, bool OverRideDuplicate, bool DupCausesWarning) {
            string tempVar = "";
            return addLinkAlias(core, linkAliasPhrase, PageID, QueryStringSuffix, OverRideDuplicate, DupCausesWarning, ref tempVar);
        }
        //
        //====================================================================================================
        //
        public static string addLinkAlias(CoreController core, string linkAliasPhrase, int PageID, string QueryStringSuffix) {
            string tempVar = "";
            return addLinkAlias(core, linkAliasPhrase, PageID, QueryStringSuffix, true, false, ref tempVar);
        }
        //
        //====================================================================================================
        /// <summary>
        /// add a link alias to a page as the primary
        /// </summary>
        public static string addLinkAlias(CoreController core, string linkAliasPhrase, int pageId, string queryStringSuffix, bool overRideDuplicate, bool dupCausesWarning, ref string return_WarningMessage) {
            string hint = "";
            try {
                //
                logger.Trace($"{core.logCommonMessage},addLinkAlias, enter, linkAliasPhrase [" + linkAliasPhrase + "], pageID [" + pageId + "], queryStringSuffix [" + queryStringSuffix + "], overRideDuplicate [" + overRideDuplicate + "], dupCausesWarning [" + dupCausesWarning + "]");
                //
                string normalizedLinkAlias = LinkAliasModel.normalizeLinkAlias(core.cpParent, linkAliasPhrase);
                if (string.IsNullOrEmpty(normalizedLinkAlias)) {
                    //
                    logger.Trace($"{core.logCommonMessage},addLinkAlias exit, blank linkalias");
                    return ""; 
                }
                //
                logger.Trace($"{core.logCommonMessage},addLinkAlias, normalized normalizedLinkAlias [" + normalizedLinkAlias + "]");
                //
                // Make sure there is not a folder or page in the wwwroot that matches this Alias
                //
                if (GenericController.toLCase(normalizedLinkAlias) == GenericController.toLCase("/" + core.appConfig.name)) {
                    //
                    // This alias points to the cclib folder
                    //
                    return_WarningMessage = ""
                        + "The Page URL being created (" + normalizedLinkAlias + ") can not be used because there is a virtual directory in your website directory that already uses this name."
                        + " Please change it to ensure the Page URL is unique. To set or change the Page URL, use the Page URL tab and select a name not used by another page.";
                } else if (GenericController.toLCase(normalizedLinkAlias) == "/cclib") {
                    //
                    // This alias points to the cclib folder
                    //
                    return_WarningMessage = ""
                        + "The Page URL being created (" + normalizedLinkAlias + ") can not be used because there is a virtual directory in your website directory that already uses this name."
                        + " Please change it to ensure the Page URL is unique. To set or change the Page URL, use the Page URL tab and select a name not used by another page.";
                } else if (core.wwwFiles.pathExists(core.appConfig.localWwwPath + "\\" + normalizedLinkAlias.Substring(1))) {
                    //
                    // This alias points to a different link, call it an error
                    //
                    return_WarningMessage = ""
                        + "The Page URL being created (" + normalizedLinkAlias + ") can not be used because there is a folder in your website directory that already uses this name."
                        + " Please change it to ensure the Page URL is unique. To set or change the Page URL, use the Page URL tab and select a name not used by another page.";
                } else {
                    //
                    // Make sure there is one here for this
                    //
                    bool invalidateLinkAliasTableCache = false;
                    int linkAliasId = 0;
                    using (var csData = new CsModel(core)) {
                        csData.open("Link Aliases", "name=" + DbController.encodeSQLText(normalizedLinkAlias), "", false, 0, "Name,PageID,QueryStringSuffix");
                        if (!csData.ok()) {
                            //
                            logger.Trace($"{core.logCommonMessage},addLinkAlias, not found in Db, add");
                            //
                            // Alias not found, create a Link Aliases
                            //
                            csData.close();
                            csData.insert("Link Aliases");
                            if (csData.ok()) {
                                csData.set("Name", normalizedLinkAlias);
                                csData.set("Pageid", pageId);
                                csData.set("QueryStringSuffix", queryStringSuffix);
                                invalidateLinkAliasTableCache = true;
                            }
                        } else {
                            int recordPageId = csData.getInteger("pageID");
                            string recordQss = csData.getText("QueryStringSuffix").ToLowerInvariant();
                            //
                            logger.Trace($"{core.logCommonMessage},addLinkAlias, linkalias record found by its name, record recordPageId [" + recordPageId + "], record QueryStringSuffix [" + recordQss + "]");
                            //
                            // Alias found, verify the pageid & QueryStringSuffix
                            //
                            int CurrentLinkAliasId = 0;
                            bool resaveLinkAlias = false;
                            if ((recordQss == queryStringSuffix.ToLowerInvariant()) && (pageId == recordPageId)) {
                                CurrentLinkAliasId = csData.getInteger("id");
                                //
                                logger.Trace($"{core.logCommonMessage},addLinkAlias, linkalias matches name, pageid, and querystring of linkalias [" + CurrentLinkAliasId + "]");
                                //
                                // it maches a current entry for this link alias, if the current entry is not the highest number id,
                                //   remove it and add this one
                                //
                                string sql = string.IsNullOrEmpty(queryStringSuffix) ? "QueryStringSuffix is null" : $"QueryStringSuffix={DbController.encodeSQLText(queryStringSuffix)}";
                                sql = $"select top 1 id from ccLinkAliases where (pageid={recordPageId})and({sql}) order by id desc";
                                using (var CS3 = new CsModel(core)) {
                                    CS3.openSql(sql);
                                    if (CS3.ok()) {
                                        resaveLinkAlias = (CurrentLinkAliasId != CS3.getInteger("id"));
                                    }
                                }
                                if (resaveLinkAlias) {
                                    //
                                    logger.Trace($"{core.logCommonMessage},addLinkAlias, another Page URL matches this pageId and QS. Move this to the top position");
                                    //
                                    core.db.executeNonQuery("delete from ccLinkAliases where id=" + CurrentLinkAliasId);
                                    using (var CS3 = new CsModel(core)) {
                                        CS3.insert("Link Aliases");
                                        if (CS3.ok()) {
                                            CS3.set("Name", normalizedLinkAlias);
                                            CS3.set("Pageid", pageId);
                                            CS3.set("QueryStringSuffix", queryStringSuffix);
                                        }
                                    }
                                }
                            } else {
                                //
                                logger.Trace($"{core.logCommonMessage},addLinkAlias, linkalias matches name, but pageid and querystring are different. Add this a newest linkalias");
                                //
                                // link alias matches, but id/qs does not -- this is either a change, or a duplicate that needs to be blocked
                                //
                                if (overRideDuplicate) {
                                    //
                                    logger.Trace($"{core.logCommonMessage},addLinkAlias, overRideDuplicate true, change the Page URL to the new link");
                                    //
                                    // change the Link Alias to the new link
                                    csData.set("Pageid", pageId);
                                    csData.set("QueryStringSuffix", queryStringSuffix);
                                    invalidateLinkAliasTableCache = true;
                                } else if (dupCausesWarning) {
                                    //
                                    logger.Trace($"{core.logCommonMessage},addLinkAlias, overRideDuplicate false, dupCausesWarning true, just return user warning if this is from admin");
                                    //
                                    if (recordPageId == 0) {
                                        int PageContentCId = Models.Domain.ContentMetadataModel.getContentId(core, "Page Content");
                                        return_WarningMessage = ""
                                            + "This page has been saved, but the Page URL could not be created (" + normalizedLinkAlias + ") because it is already in use for another page."
                                            + " To use Page URLs (friendly page names) for this page, the Page URL value must be unique on this site. To set or change the Page URL, clicke the Page URL tab and select a name not used by another page or a folder in your website.";
                                    } else {
                                        int PageContentCid = Models.Domain.ContentMetadataModel.getContentId(core, "Page Content");
                                        return_WarningMessage = ""
                                            + "This page has been saved, but the Page URL could not be created (" + normalizedLinkAlias + ") because it is already in use for another page (<a href=\"?af=4&cid=" + PageContentCid + "&id=" + recordPageId + "\">edit</a>)."
                                            + " To use Page URLs (friendly page names) for this page, the Page URL value must be unique. To set or change the Page URL, click the Page URL tab and select a name not used by another page or a folder in your website.";
                                    }
                                }
                            }
                        }
                        linkAliasId = csData.getInteger("id");
                        csData.close();
                    }
                    if (invalidateLinkAliasTableCache) {
                        //
                        // -- invalidate all linkAlias
                        core.cache.invalidateRecordKey(linkAliasId, LinkAliasModel.tableMetadata.tableNameLower);
                        //
                        // -- invalidate routemap
                        core.routeMapRebuild();
                    }
                }
                //
                logger.Trace($"{core.logCommonMessage},addLinkAlias, exit");
                //
                return normalizedLinkAlias;
            } catch (Exception ex) {
                logger.Error($"{core.logCommonMessage}", ex, "addLinkAlias exception, hint [" + hint + "]");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}

