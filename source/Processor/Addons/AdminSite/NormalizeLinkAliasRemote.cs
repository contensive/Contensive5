
using Amazon.SimpleNotificationService;
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Models.View;
using NLog;
using System;
using System.Collections.Generic;
using Twilio.TwiML.Voice;

namespace Contensive.Processor.Addons {
    //
    //====================================================================================================
    /// <summary>
    /// search for addons or content
    /// </summary>
    public class NormalizeLinkAliasRemote : AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// Admin site addon
        /// </summary>
        /// <param name="cpBase"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cpBase) {
            CPClass cp = (CPClass)cpBase;
            try {
                //
                //-- authorization
                if (!cp.User.IsAdmin) { return AjaxResponse.getResponseNotAuthorized(cp); }
                //
                NormalizeLinkAliasRemote_Request request = cp.JSON.Deserialize<NormalizeLinkAliasRemote_Request>(cp.Request.Body);
                if (request is null) { return AjaxResponse.getResponseArgumentInvalid(cp); }
                //
                NormalizeLinkAliasRemote_Response result = new() {
                    pageUrl = LinkAliasModel.normalizeLinkAlias(cp, request.text) ?? string.Empty
                };
                if (string.IsNullOrEmpty(result.pageUrl)) { return AjaxResponse.getResponse(cp, result); }
                string pageUrlNoSlash = result.pageUrl.Substring(1);
                if (!cp.core.routeMap.routeDictionary.ContainsKey(pageUrlNoSlash)) {
                    result.isAvailable = true;
                    return AjaxResponse.getResponse(cp, result);
                }
                //
                RouteMapModel.RouteClass currentRoute = cp.core.routeMap.routeDictionary[pageUrlNoSlash];
                switch (currentRoute.routeType) {
                    case RouteMapModel.RouteTypeEnum.linkForward: {
                            result.isUsedByLinkForward = true;
                            LinkForwardModel linkForward = DbBaseModel.create<LinkForwardModel>(cp, currentRoute.linkForwardId);
                            if (linkForward is not null) {
                                result.editUrl = AdminUIEditButtonController.getEditUrl(cp.core, cp.Content.GetID("link forwards"), linkForward.id);
                            }
                            return AjaxResponse.getResponse(cp, result);
                        }
                    case RouteMapModel.RouteTypeEnum.remoteMethod: {
                            result.isUsedByRemoteMethod = true;
                            result.editUrl = AdminUIEditButtonController.getEditUrl(cp.core, cp.Content.GetID("Add-ons"), currentRoute.remoteMethodAddonId);
                            return AjaxResponse.getResponse(cp, result);
                        }
                    case RouteMapModel.RouteTypeEnum.admin: {
                            result.isUsedByAdmin = true;
                            //result.currentRouteDescription = $"Admin site Url";
                            return AjaxResponse.getResponse(cp, result);
                        }
                    case RouteMapModel.RouteTypeEnum.linkAlias: {
                            result.isUsedByOtherPage = true;
                            LinkAliasModel linkAlias = DbBaseModel.create<LinkAliasModel>(cp, currentRoute.linkAliasId);
                            result.editUrl = AdminUIEditButtonController.getEditUrl(cp.core, cp.Content.GetID("page content"), currentRoute.linkAliasPageId);
                            if (linkAlias is not null) {
                                PageContentModel page = DbBaseModel.create<PageContentModel>(cp, linkAlias.pageId);
                                if (page is not null) {
                                    if(page.id==request.pageId) {
                                        // -- same page
                                        result.isUsedByCurrentPage = true;
                                        result.isUsedByOtherPage = false;
                                    } else {
                                        // -- different page
                                        result.isUsedByCurrentPage = false;
                                        result.isUsedByOtherPage = true;
                                        result.otherPageDetail = $"[page {page.id}, {page.name}]";
                                    }
                                }
                            }
                            return AjaxResponse.getResponse(cp, result);
                        }
                    default: {
                            return AjaxResponse.getResponse(cp, result);
                        }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"SaveEditModalRemote, {cp.core.logCommonMessage}");
                return AjaxResponse.getResponseServerError(cp);
            }
        }
        //
        public class NormalizeLinkAliasRemote_Request {
            public string text { get; set; }
            public int pageId { get; set; }
        }
        //
        public class NormalizeLinkAliasRemote_Response {
            public string pageUrl { get; set; }
            public bool isAvailable {  get; set; }
            public bool isUsedByCurrentPage { get; set; }
            public bool isUsedByOtherPage { get; set; }
            public string otherPageDetail { get; set; }
            public bool isUsedByLinkForward { get; set; }
            public bool isUsedByRemoteMethod { get; set; }
            public bool isUsedByAdmin { get; set; }
            //public string currentRouteDescription { get; set; }
            public string editUrl { get; set; }
        }
    }
}
