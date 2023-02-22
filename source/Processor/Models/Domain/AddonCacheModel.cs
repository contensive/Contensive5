
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// A caching system for addons. When constructed it reads a domain cache model or constructs one that
    /// provides fast access to addon lists.
    /// </summary>
    //
    public class AddonCacheModel {
        public bool isEmpty { 
            get {
                return dictIdAddon.Count == 0;
            }
        }   
        private Dictionary<int, AddonModel> dictIdAddon { get; set; } = new Dictionary<int, AddonModel>();
        private Dictionary<string, int> dictGuidId { get; set; } = new Dictionary<string, int>();
        private Dictionary<string, int> dictNameId { get; set; } = new Dictionary<string, int>();
        //
        private Dictionary<int, List<int>> dependencyDictionary { get; set; } = new Dictionary<int, List<int>>();
        //
        private List<int> onBodyEndIdList { get; set; } = new();
        private List<int> onBodyStartIdList { get; set; } = new();
        private List<int> onNewVisitIdList { get; set; } = new();
        private List<int> OnPageEndIdList { get; set; } = new();
        private List<int> OnPageStartIdList { get; set; } = new();
        private List<int> remoteMethodIdList { get; set; } = new();
        /// <summary>
        /// Each addon includes text to be added to the robots.txt response.
        /// </summary>
        public string robotsTxt { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// return an new empty object. Required for cache read
        /// </summary>
        public AddonCacheModel()  { }
        //
        //====================================================================================================
        /// <summary>
        /// construct an instance of the class, populating all lists from the DbModels during the load
        /// </summary>
        /// <param name="core"></param>
        public AddonCacheModel(CoreController core) {
            foreach (AddonModel addon in DbBaseModel.createList<AddonModel>(core.cpParent, "")) {
                add(core, addon);
            }
            foreach (var includeRule in AddonIncludeRuleModel.createList<AddonIncludeRuleModel>(core.cpParent, "", "addonId,includedAddonID")) {
                if (!dependencyDictionary.ContainsKey(includeRule.addonId)) dependencyDictionary.Add(includeRule.addonId, new List<int>());
                dependencyDictionary[includeRule.addonId].Add(includeRule.includedAddonId);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// add an addon to the internal store
        /// </summary>
        /// <param name="core"></param>
        /// <param name="addon"></param>
        private void add(CoreController core, AddonModel addon) {
            if (!dictIdAddon.ContainsKey(addon.id)) {
                dictIdAddon.Add(addon.id, addon);
                if (string.IsNullOrEmpty(addon.ccguid)) {
                    addon.ccguid = GenericController.getGUID();
                    addon.save(core.cpParent);
                }
                if (!dictGuidId.ContainsKey(addon.ccguid.ToLowerInvariant())) {
                    dictGuidId.Add(addon.ccguid.ToLowerInvariant(), addon.id);
                    if (string.IsNullOrEmpty(addon.name.Trim())) {
                        addon.name = "addon " + addon.id.ToString();
                        addon.save(core.cpParent);
                    }
                    if (!dictNameId.ContainsKey(addon.name.ToLowerInvariant())) {
                        dictNameId.Add(addon.name.ToLowerInvariant(), addon.id);
                    }
                }
                if (!string.IsNullOrEmpty(addon.aliasList)) {
                    foreach (var aliasName in addon.aliasList.Split(',')) {
                        if(!dictNameId.ContainsKey(aliasName)) {
                            dictNameId.Add(aliasName.ToLowerInvariant(), addon.id);
                        }
                    }
                }
            }
            if ((addon.onBodyEnd) && (!onBodyEndIdList.Contains(addon.id))) onBodyEndIdList.Add(addon.id);
            if ((addon.onBodyStart) && (!onBodyStartIdList.Contains(addon.id))) onBodyStartIdList.Add(addon.id);
            if ((addon.onNewVisitEvent) && (!onNewVisitIdList.Contains(addon.id)))onNewVisitIdList.Add(addon.id);
            if ((addon.onPageEndEvent) && (!OnPageEndIdList.Contains(addon.id))) OnPageEndIdList.Add(addon.id);
            if ((addon.onPageStartEvent) && (!OnPageStartIdList.Contains(addon.id))) OnPageStartIdList.Add(addon.id);
            if ((addon.remoteMethod) && (!remoteMethodIdList.Contains(addon.id))) remoteMethodIdList.Add(addon.id);
            if (!string.IsNullOrWhiteSpace(addon.robotsTxt)) robotsTxt += Environment.NewLine + addon.robotsTxt;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get an addon from its guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public AddonModel create(string guid) {
            if (dictGuidId.ContainsKey(guid.ToLowerInvariant())) {
                return create(this.dictGuidId[guid.ToLowerInvariant()]);
            }
            return null;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get an addon from its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public AddonModel createByUniqueName(string name) {
            if (dictNameId.ContainsKey(name.ToLowerInvariant())) {
                return create(this.dictNameId[name.ToLowerInvariant()]);
            }


            return null;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get an addon model from it's Id
        /// </summary>
        /// <param name="addonId"></param>
        /// <returns></returns>
        public AddonModel create(int addonId) {
            if (dictIdAddon.ContainsKey(addonId)) {
                return this.dictIdAddon[addonId];
            }
            return null;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a list of addons from a list of addon Id
        /// </summary>
        /// <param name="addonIdList"></param>
        /// <returns></returns>
        private List<AddonModel> getAddonList(List<int> addonIdList) {
            List<AddonModel> result = new List<AddonModel>();
            foreach (int addonId in addonIdList) {
                result.Add(create(addonId));
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return all addons marked as onBodyEnd
        /// </summary>
        /// <returns></returns>
        public List<AddonModel> getOnBodyEndAddonList()  {
            return getAddonList(onBodyEndIdList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return all addons marked as onBodyStart
        /// </summary>
        /// <returns></returns>
        public List<AddonModel> getOnBodyStartAddonList()  {
            return getAddonList(onBodyStartIdList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return all addons marked as onNewVisit
        /// </summary>
        /// <returns></returns>
        public List<AddonModel> getOnNewVisitAddonList()  {
            return getAddonList(onNewVisitIdList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return all addons marked as onPageEnd
        /// </summary>
        /// <returns></returns>
        public List<AddonModel> getOnPageEndAddonList()  {
            return getAddonList(OnPageEndIdList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return all addons marked as onPageStart
        /// </summary>
        /// <returns></returns>
        public List<AddonModel> getOnPageStartAddonList()  {
            return getAddonList(OnPageStartIdList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return all addons marked as remote methods
        /// </summary>
        /// <returns></returns>
        public List<AddonModel> getRemoteMethodAddonList()  {
            return getAddonList(remoteMethodIdList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a list of addons that the provided addonId depends on
        /// </summary>
        /// <param name="AddonId"></param>
        /// <returns></returns>
        public List<AddonModel> getDependsOnList(int AddonId) {
            if( !dependencyDictionary.ContainsKey(AddonId)) {
                return new List<AddonModel>();
            } else {
                return getAddonList(dependencyDictionary[AddonId]);
            }
        }
    }
}