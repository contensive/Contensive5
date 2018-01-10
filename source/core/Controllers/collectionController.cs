﻿
using System;
using System.Reflection;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Contensive.Core;
using Contensive.Core.Models.Entity;
using Contensive.Core.Controllers;
using static Contensive.Core.Controllers.genericController;
using static Contensive.Core.constants;
using System.IO;
using System.Data;
using System.Threading;
using Contensive.Core.Models.Complex;
using System.Linq;

namespace Contensive.Core.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// install addon collections
    /// </summary>
    public class collectionController {
        //
        //====================================================================================================
        /// <summary>
        /// Overlay a Src CDef on to the current one (Dst). Any Src CDEf entries found in Src are added to Dst.
        /// if SrcIsUserCDef is true, then the Src is overlayed on the Dst if there are any changes -- and .CDefChanged flag set
        /// </summary>
        private static bool addMiniCollectionSrcToDst(coreClass cpcore, ref miniCollectionModel dstCollection, miniCollectionModel srcCollection) {
            bool returnOk = true;
            try {
                string HelpSrc = null;
                bool HelpCustomChanged = false;
                bool HelpDefaultChanged = false;
                bool HelpChanged = false;
                string Copy = null;
                string n = null;
                Models.Complex.cdefFieldModel srcCdefField = null;
                Models.Complex.cdefModel dstCdef = null;
                Models.Complex.cdefFieldModel dstCdefField = null;
                bool IsMatch = false;
                string DstKey = null;
                string SrcKey = null;
                string DataBuildVersion = null;
                bool SrcIsNavigator = false;
                bool DstIsNavigator = false;
                
                string dstName = null;
                string SrcFieldName = null;
                bool updateDst = false;
                Models.Complex.cdefModel srcCdef = null;
                bool DebugSrcFound = false;
                bool DebugDstFound = false;
                //
                // If the Src is the BaseCollection, the Dst must be the Application Collectio
                //   in this case, reset any BaseContent or BaseField attributes in the application that are not in the base
                //
                if (srcCollection.isBaseCollection) {
                    foreach (var dstKeyValuePair in dstCollection.cdef) {
                        Models.Complex.cdefModel dstWorkingCdef = dstKeyValuePair.Value;
                        string contentName = dstWorkingCdef.Name;
                        if (dstCollection.cdef[contentName.ToLower()].IsBaseContent) {
                            //
                            // this application collection Cdef is marked base, verify it is in the base collection
                            //
                            if (!srcCollection.cdef.ContainsKey(contentName.ToLower())) {
                                //
                                // cdef in dst is marked base, but it is not in the src collection, reset the cdef.isBaseContent and all field.isbasefield
                                //
                                var tempVar = dstCollection.cdef[contentName.ToLower()];
                                tempVar.IsBaseContent = false;
                                tempVar.dataChanged = true;
                                foreach (var dstFieldKeyValuePair in tempVar.fields) {
                                    Models.Complex.cdefFieldModel field = dstFieldKeyValuePair.Value;
                                    if (field.isBaseField) {
                                        field.isBaseField = false;
                                        //field.Changed = True
                                    }
                                }
                            }
                        }
                    }
                }
                //
                //
                // -------------------------------------------------------------------------------------------------
                // Go through all CollectionSrc and find the CollectionDst match
                //   if it is an exact match, do nothing
                //   if the cdef does not match, set cdefext[Ptr].CDefChanged true
                //   if any field does not match, set cdefext...field...CDefChanged
                //   if the is no CollectionDst for the CollectionSrc, add it and set okToUpdateDstFromSrc
                // -------------------------------------------------------------------------------------------------
                //
                logController.appendLogInstall(cpcore, "Application: " + cpcore.serverConfig.appConfig.name + ", UpgradeCDef_AddSrcToDst");
                //
                foreach (var srcKeyValuePair in srcCollection.cdef) {
                    srcCdef = srcKeyValuePair.Value;
                    string srcName = srcCdef.Name;
                    DebugSrcFound = false;
                    if (srcName.IndexOf(cnNavigatorEntries)>=0 ) {
                        DebugSrcFound = true;
                    }
                    //
                    // Search for this cdef in the Dst
                    //
                    updateDst = false;
                    if (!dstCollection.cdef.ContainsKey(srcName.ToLower())) {
                        //
                        // add src to dst
                        //
                        dstCdef = new Models.Complex.cdefModel();
                        dstCollection.cdef.Add(srcName.ToLower(), dstCdef);
                        updateDst = true;
                    } else {
                        dstCdef = dstCollection.cdef[srcName.ToLower()];
                        dstName = srcName;
                        //
                        // found a match between Src and Dst
                        //
                        if (dstCdef.IsBaseContent == srcCdef.IsBaseContent) {
                            //
                            // Allow changes to user cdef only from user cdef, changes to base only from base
                            updateDst |= (dstCdef.ActiveOnly != srcCdef.ActiveOnly);
                            updateDst |= (dstCdef.AdminOnly != srcCdef.AdminOnly);
                            updateDst |= (dstCdef.DeveloperOnly != srcCdef.DeveloperOnly);
                            updateDst |= (dstCdef.AllowAdd != srcCdef.AllowAdd);
                            updateDst |= (dstCdef.AllowCalendarEvents != srcCdef.AllowCalendarEvents);
                            updateDst |= (dstCdef.AllowContentTracking != srcCdef.AllowContentTracking);
                            updateDst |= (dstCdef.AllowDelete != srcCdef.AllowDelete);
                            updateDst |= (dstCdef.AllowTopicRules != srcCdef.AllowTopicRules);
                            updateDst |= !TextMatch(dstCdef.ContentDataSourceName, srcCdef.ContentDataSourceName);
                            updateDst |= !TextMatch(dstCdef.ContentTableName, srcCdef.ContentTableName);
                            updateDst |= !TextMatch(dstCdef.DefaultSortMethod, srcCdef.DefaultSortMethod);
                            updateDst |= !TextMatch(dstCdef.DropDownFieldList, srcCdef.DropDownFieldList);
                            updateDst |= !TextMatch(dstCdef.EditorGroupName, srcCdef.EditorGroupName);
                            updateDst |= (dstCdef.IgnoreContentControl != srcCdef.IgnoreContentControl);
                            updateDst |= (dstCdef.Active != srcCdef.Active);
                            updateDst |= (dstCdef.AllowContentChildTool != srcCdef.AllowContentChildTool);
                            updateDst |= (dstCdef.parentID != srcCdef.parentID);
                            updateDst |= !TextMatch(dstCdef.IconLink, srcCdef.IconLink);
                            updateDst |= (dstCdef.IconHeight != srcCdef.IconHeight);
                            updateDst |= (dstCdef.IconWidth != srcCdef.IconWidth);
                            updateDst |= (dstCdef.IconSprites != srcCdef.IconSprites);
                            updateDst |= !TextMatch(dstCdef.installedByCollectionGuid, srcCdef.installedByCollectionGuid);
                            updateDst |= !TextMatch(dstCdef.guid, srcCdef.guid);
                            updateDst |= (dstCdef.IsBaseContent != srcCdef.IsBaseContent);
                        }
                    }
                    if (updateDst) {
                        //
                        // update the Dst with the Src
                        dstCdef.Active = srcCdef.Active;
                        dstCdef.ActiveOnly = srcCdef.ActiveOnly;
                        dstCdef.AdminOnly = srcCdef.AdminOnly;
                        dstCdef.AliasID = srcCdef.AliasID;
                        dstCdef.AliasName = srcCdef.AliasName;
                        dstCdef.AllowAdd = srcCdef.AllowAdd;
                        dstCdef.AllowCalendarEvents = srcCdef.AllowCalendarEvents;
                        dstCdef.AllowContentChildTool = srcCdef.AllowContentChildTool;
                        dstCdef.AllowContentTracking = srcCdef.AllowContentTracking;
                        dstCdef.AllowDelete = srcCdef.AllowDelete;
                        dstCdef.AllowTopicRules = srcCdef.AllowTopicRules;
                        dstCdef.guid = srcCdef.guid;
                        dstCdef.ContentControlCriteria = srcCdef.ContentControlCriteria;
                        dstCdef.ContentDataSourceName = srcCdef.ContentDataSourceName;
                        dstCdef.ContentTableName = srcCdef.ContentTableName;
                        dstCdef.dataSourceId = srcCdef.dataSourceId;
                        dstCdef.DefaultSortMethod = srcCdef.DefaultSortMethod;
                        dstCdef.DeveloperOnly = srcCdef.DeveloperOnly;
                        dstCdef.DropDownFieldList = srcCdef.DropDownFieldList;
                        dstCdef.EditorGroupName = srcCdef.EditorGroupName;
                        dstCdef.IconHeight = srcCdef.IconHeight;
                        dstCdef.IconLink = srcCdef.IconLink;
                        dstCdef.IconSprites = srcCdef.IconSprites;
                        dstCdef.IconWidth = srcCdef.IconWidth;
                        dstCdef.IgnoreContentControl = srcCdef.IgnoreContentControl;
                        dstCdef.installedByCollectionGuid = srcCdef.installedByCollectionGuid;
                        dstCdef.IsBaseContent = srcCdef.IsBaseContent;
                        dstCdef.IsModifiedSinceInstalled = srcCdef.IsModifiedSinceInstalled;
                        dstCdef.Name = srcCdef.Name;
                        dstCdef.parentID = srcCdef.parentID;
                        dstCdef.parentName = srcCdef.parentName;
                        dstCdef.SelectCommaList = srcCdef.SelectCommaList;
                        dstCdef.WhereClause = srcCdef.WhereClause;
                        dstCdef.includesAFieldChange = true;
                        dstCdef.dataChanged = true;
                    }
                    //
                    // Now check each of the field records for an addition, or a change
                    // DstPtr is still set to the Dst CDef
                    //
                    //Call AppendClassLogFile(cpcore.app.config.name,"UpgradeCDef_AddSrcToDst", "CollectionSrc.CDef[SrcPtr].fields.count=" & CollectionSrc.CDef[SrcPtr].fields.count)
                    foreach (var srcFieldKeyValuePair in srcCdef.fields) {
                        srcCdefField = srcFieldKeyValuePair.Value;
                        SrcFieldName = srcCdefField.nameLc;
                        updateDst = false;
                        if (!dstCollection.cdef.ContainsKey(srcName.ToLower())) {
                            //
                            // should have been the collection
                            //
                            throw (new ApplicationException("ERROR - cannot update destination content because it was not found after being added."));
                        } else {
                            dstCdef = dstCollection.cdef[srcName.ToLower()];
                            if (dstCdef.fields.ContainsKey(SrcFieldName.ToLower())) {
                                //
                                // Src field was found in Dst fields
                                //

                                dstCdefField = dstCdef.fields[SrcFieldName.ToLower()];
                                updateDst = false;
                                if (dstCdefField.isBaseField == srcCdefField.isBaseField) {
                                    updateDst |= (srcCdefField.active != dstCdefField.active);
                                    updateDst |= (srcCdefField.adminOnly != dstCdefField.adminOnly);
                                    updateDst |= (srcCdefField.authorable != dstCdefField.authorable);
                                    updateDst |= !TextMatch(srcCdefField.caption, dstCdefField.caption);
                                    updateDst |= (srcCdefField.contentId != dstCdefField.contentId);
                                    updateDst |= (srcCdefField.developerOnly != dstCdefField.developerOnly);
                                    updateDst |= (srcCdefField.editSortPriority != dstCdefField.editSortPriority);
                                    updateDst |= !TextMatch(srcCdefField.editTabName, dstCdefField.editTabName);
                                    updateDst |= (srcCdefField.fieldTypeId != dstCdefField.fieldTypeId);
                                    updateDst |= (srcCdefField.htmlContent != dstCdefField.htmlContent);
                                    updateDst |= (srcCdefField.indexColumn != dstCdefField.indexColumn);
                                    updateDst |= (srcCdefField.indexSortDirection != dstCdefField.indexSortDirection);
                                    updateDst |= (encodeInteger(srcCdefField.indexSortOrder) != genericController.encodeInteger(dstCdefField.indexSortOrder));
                                    updateDst |= !TextMatch(srcCdefField.indexWidth, dstCdefField.indexWidth);
                                    updateDst |= (srcCdefField.lookupContentID != dstCdefField.lookupContentID);
                                    updateDst |= !TextMatch(srcCdefField.lookupList, dstCdefField.lookupList);
                                    updateDst |= (srcCdefField.manyToManyContentID != dstCdefField.manyToManyContentID);
                                    updateDst |= (srcCdefField.manyToManyRuleContentID != dstCdefField.manyToManyRuleContentID);
                                    updateDst |= !TextMatch(srcCdefField.ManyToManyRulePrimaryField, dstCdefField.ManyToManyRulePrimaryField);
                                    updateDst |= !TextMatch(srcCdefField.ManyToManyRuleSecondaryField, dstCdefField.ManyToManyRuleSecondaryField);
                                    updateDst |= (srcCdefField.memberSelectGroupId_get(cpcore) != dstCdefField.memberSelectGroupId_get(cpcore));
                                    updateDst |= (srcCdefField.notEditable != dstCdefField.notEditable);
                                    updateDst |= (srcCdefField.password != dstCdefField.password);
                                    updateDst |= (srcCdefField.readOnly != dstCdefField.readOnly);
                                    updateDst |= (srcCdefField.redirectContentID != dstCdefField.redirectContentID);
                                    updateDst |= !TextMatch(srcCdefField.redirectID, dstCdefField.redirectID);
                                    updateDst |= !TextMatch(srcCdefField.redirectPath, dstCdefField.redirectPath);
                                    updateDst |= (srcCdefField.required != dstCdefField.required);
                                    updateDst |= (srcCdefField.RSSDescriptionField != dstCdefField.RSSDescriptionField);
                                    updateDst |= (srcCdefField.RSSTitleField != dstCdefField.RSSTitleField);
                                    updateDst |= (srcCdefField.Scramble != dstCdefField.Scramble);
                                    updateDst |= (srcCdefField.textBuffered != dstCdefField.textBuffered);
                                    updateDst |= (genericController.encodeText(srcCdefField.defaultValue) != genericController.encodeText(dstCdefField.defaultValue));
                                    updateDst |= (srcCdefField.uniqueName != dstCdefField.uniqueName);
                                    updateDst |= (srcCdefField.isBaseField != dstCdefField.isBaseField);
                                    updateDst |= !TextMatch(srcCdefField.get_lookupContentName(cpcore), dstCdefField.get_lookupContentName(cpcore));
                                    updateDst |= !TextMatch(srcCdefField.get_lookupContentName(cpcore), dstCdefField.get_lookupContentName(cpcore));
                                    updateDst |= !TextMatch(srcCdefField.get_ManyToManyRuleContentName(cpcore), dstCdefField.get_ManyToManyRuleContentName(cpcore));
                                    updateDst |= !TextMatch(srcCdefField.get_RedirectContentName(cpcore), dstCdefField.get_RedirectContentName(cpcore));
                                    updateDst |= !TextMatch(srcCdefField.installedByCollectionGuid, dstCdefField.installedByCollectionGuid);
                                }
                                //
                                // Check Help fields, track changed independantly so frequent help changes will not force timely cdef loads
                                //
                                HelpSrc = srcCdefField.HelpCustom;
                                HelpCustomChanged = !TextMatch(HelpSrc, srcCdefField.HelpCustom);
                                //
                                HelpSrc = srcCdefField.HelpDefault;
                                HelpDefaultChanged = !TextMatch(HelpSrc, srcCdefField.HelpDefault);
                                //
                                HelpChanged = HelpDefaultChanged || HelpCustomChanged;
                            } else {
                                //
                                // field was not found in dst, add it and populate
                                //
                                dstCdef.fields.Add(SrcFieldName.ToLower(), new Models.Complex.cdefFieldModel());
                                dstCdefField = dstCdef.fields[SrcFieldName.ToLower()];
                                updateDst = true;
                                HelpChanged = true;
                            }
                            //
                            // If okToUpdateDstFromSrc, update the Dst record with the Src record
                            //
                            if (updateDst) {
                                //
                                // Update Fields
                                //
                                dstCdefField.active = srcCdefField.active;
                                dstCdefField.adminOnly = srcCdefField.adminOnly;
                                dstCdefField.authorable = srcCdefField.authorable;
                                dstCdefField.caption = srcCdefField.caption;
                                dstCdefField.contentId = srcCdefField.contentId;
                                dstCdefField.defaultValue = srcCdefField.defaultValue;
                                dstCdefField.developerOnly = srcCdefField.developerOnly;
                                dstCdefField.editSortPriority = srcCdefField.editSortPriority;
                                dstCdefField.editTabName = srcCdefField.editTabName;
                                dstCdefField.fieldTypeId = srcCdefField.fieldTypeId;
                                dstCdefField.htmlContent = srcCdefField.htmlContent;
                                dstCdefField.indexColumn = srcCdefField.indexColumn;
                                dstCdefField.indexSortDirection = srcCdefField.indexSortDirection;
                                dstCdefField.indexSortOrder = srcCdefField.indexSortOrder;
                                dstCdefField.indexWidth = srcCdefField.indexWidth;
                                dstCdefField.lookupContentID = srcCdefField.lookupContentID;
                                dstCdefField.lookupList = srcCdefField.lookupList;
                                dstCdefField.manyToManyContentID = srcCdefField.manyToManyContentID;
                                dstCdefField.manyToManyRuleContentID = srcCdefField.manyToManyRuleContentID;
                                dstCdefField.ManyToManyRulePrimaryField = srcCdefField.ManyToManyRulePrimaryField;
                                dstCdefField.ManyToManyRuleSecondaryField = srcCdefField.ManyToManyRuleSecondaryField;
                                dstCdefField.memberSelectGroupId_set(cpcore, srcCdefField.memberSelectGroupId_get(cpcore));
                                dstCdefField.nameLc = srcCdefField.nameLc;
                                dstCdefField.notEditable = srcCdefField.notEditable;
                                dstCdefField.password = srcCdefField.password;
                                dstCdefField.readOnly = srcCdefField.readOnly;
                                dstCdefField.redirectContentID = srcCdefField.redirectContentID;
                                dstCdefField.redirectID = srcCdefField.redirectID;
                                dstCdefField.redirectPath = srcCdefField.redirectPath;
                                dstCdefField.required = srcCdefField.required;
                                dstCdefField.RSSDescriptionField = srcCdefField.RSSDescriptionField;
                                dstCdefField.RSSTitleField = srcCdefField.RSSTitleField;
                                dstCdefField.Scramble = srcCdefField.Scramble;
                                dstCdefField.textBuffered = srcCdefField.textBuffered;
                                dstCdefField.uniqueName = srcCdefField.uniqueName;
                                dstCdefField.isBaseField = srcCdefField.isBaseField;
                                dstCdefField.set_lookupContentName(cpcore, srcCdefField.get_lookupContentName(cpcore));
                                dstCdefField.set_ManyToManyContentName(cpcore, srcCdefField.get_ManyToManyContentName(cpcore));
                                dstCdefField.set_ManyToManyRuleContentName(cpcore, srcCdefField.get_ManyToManyRuleContentName(cpcore));
                                dstCdefField.set_RedirectContentName(cpcore, srcCdefField.get_RedirectContentName(cpcore));
                                dstCdefField.installedByCollectionGuid = srcCdefField.installedByCollectionGuid;
                                dstCdefField.dataChanged = true;
                                if (HelpChanged) {
                                    dstCdefField.HelpCustom = srcCdefField.HelpCustom;
                                    dstCdefField.HelpDefault = srcCdefField.HelpDefault;
                                    dstCdefField.HelpChanged = true;
                                }
                                dstCdef.includesAFieldChange = true;
                            }
                            //
                        }
                    }
                }
                //
                // -------------------------------------------------------------------------------------------------
                // Check SQL Indexes
                // -------------------------------------------------------------------------------------------------
                //
                foreach (miniCollectionModel.collectionSQLIndexModel srcSqlIndex in srcCollection.sqlIndexes) {
                    string srcName = (srcSqlIndex.DataSourceName + "-" + srcSqlIndex.TableName + "-" + srcSqlIndex.IndexName).ToLower();
                    updateDst = false;
                    //
                    // Search for this name in the Dst
                    bool indexFound = false;
                    bool indexChanged = false;
                    miniCollectionModel.collectionSQLIndexModel indexToUpdate = new miniCollectionModel.collectionSQLIndexModel() { };
                    foreach (miniCollectionModel.collectionSQLIndexModel dstSqlIndex in dstCollection.sqlIndexes) {
                        dstName = (dstSqlIndex.DataSourceName + "-" + dstSqlIndex.TableName + "-" + dstSqlIndex.IndexName).ToLower();
                        if (TextMatch(dstName, srcName)) {
                            //
                            // found a match between Src and Dst
                            indexFound = true;
                            indexToUpdate = dstSqlIndex;
                            indexChanged = !TextMatch(dstSqlIndex.FieldNameList, srcSqlIndex.FieldNameList);
                            break;
                        }
                    }
                    if (!indexFound) {
                        //
                        // add src to dst
                        dstCollection.sqlIndexes.Add(srcSqlIndex);
                    } else if (indexChanged && (indexToUpdate != null )) {
                        //
                        // update dst to src

                        indexToUpdate.dataChanged = true;
                        indexToUpdate.DataSourceName = srcSqlIndex.DataSourceName;
                        indexToUpdate.FieldNameList = srcSqlIndex.FieldNameList;
                        indexToUpdate.IndexName = srcSqlIndex.IndexName;
                        indexToUpdate.TableName = srcSqlIndex.TableName;
                    }
                }
                //
                //-------------------------------------------------------------------------------------------------
                // Check menus
                //-------------------------------------------------------------------------------------------------
                //
                DataBuildVersion = cpcore.siteProperties.dataBuildVersion;
                foreach (var srcKvp in srcCollection.menus) {
                    string srcKey = srcKvp.Key.ToLower() ;
                    miniCollectionModel.collectionMenuModel srcMenu = srcKvp.Value;
                    string srcName = srcMenu.Name.ToLower();
                    string srcGuid = srcMenu.Guid;
                    string SrcParentName = genericController.vbLCase(srcMenu.ParentName);
                    string SrcNameSpace = genericController.vbLCase(srcMenu.menuNameSpace);
                    SrcIsNavigator = srcMenu.IsNavigator;
                    updateDst = false;
                    //
                    // Search for match using guid
                    miniCollectionModel.collectionMenuModel dstMenuMatch = new miniCollectionModel.collectionMenuModel() { } ;
                    IsMatch = false;
                    foreach (var dstKvp in dstCollection.menus) {
                        string dstKey = dstKvp.Key.ToLower();
                        miniCollectionModel.collectionMenuModel dstMenu = dstKvp.Value;
                        string dstGuid = dstMenu.Guid;
                        if (dstGuid == srcGuid) {
                            DstIsNavigator = dstMenu.IsNavigator;
                            DstKey = genericController.vbLCase(dstMenu.Key);
                            IsMatch = (DstKey == SrcKey) && (SrcIsNavigator == DstIsNavigator);
                            if (IsMatch) {
                                dstMenuMatch = dstMenu;
                                break;
                            }

                        }
                    }
                    if (!IsMatch) {
                        //
                        // no match found on guid, try name and ( either namespace or parentname )
                        foreach (var dstKvp in dstCollection.menus) {
                            string dstKey = dstKvp.Key.ToLower();
                            miniCollectionModel.collectionMenuModel dstMenu = dstKvp.Value;
                            dstName = genericController.vbLCase(dstMenu.Name);
                            if ((srcName == dstName) && (SrcIsNavigator == DstIsNavigator)) {
                                if (SrcIsNavigator) {
                                    //
                                    // Navigator - check namespace if Dst.guid is blank (builder to new version of menu)
                                    IsMatch = (SrcNameSpace == genericController.vbLCase(dstMenu.menuNameSpace)) && (dstMenu.Guid == "");
                                } else {
                                    //
                                    // AdminMenu - check parentname
                                    IsMatch = (SrcParentName == genericController.vbLCase(dstMenu.ParentName));
                                }
                                if (IsMatch) {
                                    dstMenuMatch = dstMenu;
                                    break;
                                }
                            }
                        }
                    }
                    if(IsMatch) {
                        updateDst |= (dstMenuMatch.Active != srcMenu.Active);
                        updateDst |= (dstMenuMatch.AdminOnly != srcMenu.AdminOnly);
                        updateDst |= !TextMatch(dstMenuMatch.ContentName, srcMenu.ContentName);
                        updateDst |= (dstMenuMatch.DeveloperOnly != srcMenu.DeveloperOnly);
                        updateDst |= !TextMatch(dstMenuMatch.LinkPage, srcMenu.LinkPage);
                        updateDst |= !TextMatch(dstMenuMatch.Name, srcMenu.Name);
                        updateDst |= (dstMenuMatch.NewWindow != srcMenu.NewWindow);
                        updateDst |= !TextMatch(dstMenuMatch.SortOrder, srcMenu.SortOrder);
                        updateDst |= !TextMatch(dstMenuMatch.AddonName, srcMenu.AddonName);
                        updateDst |= !TextMatch(dstMenuMatch.NavIconType, srcMenu.NavIconType);
                        updateDst |= !TextMatch(dstMenuMatch.NavIconTitle, srcMenu.NavIconTitle);
                        updateDst |= !TextMatch(dstMenuMatch.menuNameSpace, srcMenu.menuNameSpace);
                        updateDst |= !TextMatch(dstMenuMatch.Guid, srcMenu.Guid);
                        dstCollection.menus.Remove(DstKey);
                    }
                    dstCollection.menus.Add(srcKey, srcMenu);
                }
                //
                //-------------------------------------------------------------------------------------------------
                // Check addons -- yes, this should be done.
                //-------------------------------------------------------------------------------------------------
                //
                //If False Then
                //    '
                //    ' remove this for now -- later add ImportCollections to track the collections (not addons)
                //    '
                //    '
                //    '
                //    For SrcPtr = 0 To srcCollection.AddOnCnt - 1
                //        SrcContentName = genericController.vbLCase(srcCollection.AddOns[SrcPtr].Name)
                //        okToUpdateDstFromSrc = False
                //        '
                //        ' Search for this name in the Dst
                //        '
                //        For DstPtr = 0 To dstCollection.AddOnCnt - 1
                //            DstName = genericController.vbLCase(dstCollection.AddOns[dstPtr].Name)
                //            If DstName = SrcContentName Then
                //                '
                //                ' found a match between Src and Dst
                //                '
                //                If SrcIsUserCDef Then
                //                    '
                //                    ' test for cdef attribute changes
                //                    '
                //                    With dstCollection.AddOns[dstPtr]
                //                        okToUpdateDstFromSrc = okToUpdateDstFromSrc Or Not TextMatch(cpcore,.ArgumentList, srcCollection.AddOns[SrcPtr].ArgumentList)
                //                        okToUpdateDstFromSrc = okToUpdateDstFromSrc Or Not TextMatch(cpcore,.Copy, srcCollection.AddOns[SrcPtr].Copy)
                //                        okToUpdateDstFromSrc = okToUpdateDstFromSrc Or Not TextMatch(cpcore,.Link, srcCollection.AddOns[SrcPtr].Link)
                //                        okToUpdateDstFromSrc = okToUpdateDstFromSrc Or Not TextMatch(cpcore,.Name, srcCollection.AddOns[SrcPtr].Name)
                //                        okToUpdateDstFromSrc = okToUpdateDstFromSrc Or Not TextMatch(cpcore,.ObjectProgramID, srcCollection.AddOns[SrcPtr].ObjectProgramID)
                //                        okToUpdateDstFromSrc = okToUpdateDstFromSrc Or Not TextMatch(cpcore,.SortOrder, srcCollection.AddOns[SrcPtr].SortOrder)
                //                    End With
                //                End If
                //                Exit For
                //            End If
                //        Next
                //        If DstPtr = dstCollection.AddOnCnt Then
                //            '
                //            ' CDef was not found, add it
                //            '
                //            Array.Resize( ref asdf,asdf) // redim preserve  dstCollection.AddOns(dstCollection.AddOnCnt)
                //            dstCollection.AddOnCnt = DstPtr + 1
                //            okToUpdateDstFromSrc = True
                //        End If
                //        If okToUpdateDstFromSrc Then
                //            With dstCollection.AddOns[dstPtr]
                //                '
                //                ' It okToUpdateDstFromSrc, update the Dst with the Src
                //                '
                //                .CDefChanged = True
                //                .ArgumentList = srcCollection.AddOns[SrcPtr].ArgumentList
                //                .Copy = srcCollection.AddOns[SrcPtr].Copy
                //                .Link = srcCollection.AddOns[SrcPtr].Link
                //                .Name = srcCollection.AddOns[SrcPtr].Name
                //                .ObjectProgramID = srcCollection.AddOns[SrcPtr].ObjectProgramID
                //                .SortOrder = srcCollection.AddOns[SrcPtr].SortOrder
                //            End With
                //        End If
                //    Next
                //End If
                //
                //-------------------------------------------------------------------------------------------------
                // Check styles
                //-------------------------------------------------------------------------------------------------
                //
                int srcStylePtr = 0;
                int dstStylePtr = 0;
                for (srcStylePtr = 0; srcStylePtr < srcCollection.styleCnt; srcStylePtr++) {
                    string srcName = genericController.vbLCase(srcCollection.styles[srcStylePtr].Name);
                    updateDst = false;
                    //
                    // Search for this name in the Dst
                    //
                    for (dstStylePtr = 0; dstStylePtr < dstCollection.styleCnt; dstStylePtr++) {
                        dstName = genericController.vbLCase(dstCollection.styles[dstStylePtr].Name);
                        if (dstName == srcName) {
                            //
                            // found a match between Src and Dst
                            updateDst |= !TextMatch(dstCollection.styles[dstStylePtr].Copy, srcCollection.styles[srcStylePtr].Copy);
                            break;
                        }
                    }
                    if (dstStylePtr == dstCollection.styleCnt) {
                        //
                        // CDef was not found, add it
                        //
                        Array.Resize(ref dstCollection.styles, dstCollection.styleCnt);
                        dstCollection.styleCnt = dstStylePtr + 1;
                        updateDst = true;
                    }
                    if (updateDst) {
                        var tempVar6 = dstCollection.styles[dstStylePtr];
                        //
                        // It okToUpdateDstFromSrc, update the Dst with the Src
                        //
                        tempVar6.dataChanged = true;
                        tempVar6.Copy = srcCollection.styles[srcStylePtr].Copy;
                        tempVar6.Name = srcCollection.styles[srcStylePtr].Name;
                    }
                }
                //
                //-------------------------------------------------------------------------------------------------
                // Add Collections
                //-------------------------------------------------------------------------------------------------
                //
                foreach( var import in srcCollection.collectionImports) {
                    dstCollection.collectionImports.Add(import);
                }
                //
                //-------------------------------------------------------------------------------------------------
                // Page Templates
                //-------------------------------------------------------------------------------------------------
                //
                //
                //-------------------------------------------------------------------------------------------------
                // Site Sections
                //-------------------------------------------------------------------------------------------------
                //
                //
                //-------------------------------------------------------------------------------------------------
                // Dynamic Menus
                //-------------------------------------------------------------------------------------------------
                //
                //
                //-------------------------------------------------------------------------------------------------
                // Page Content
                //-------------------------------------------------------------------------------------------------
                //
                //
                //-------------------------------------------------------------------------------------------------
                // Copy Content
                //-------------------------------------------------------------------------------------------------
                //
                //
            } catch (Exception ex) {
                cpcore.handleException(ex);
                throw;
            }
            return returnOk;
        }
        //
        //====================================================================================================
        //
        private static miniCollectionModel installCollection_GetApplicationMiniCollection(coreClass cpCore, bool isNewBuild) {
            miniCollectionModel returnColl = new miniCollectionModel();
            try {
                //
                string ExportFilename = null;
                string ExportPathPage = null;
                string CollectionData = null;
                //
                if (!isNewBuild) {
                    //
                    // if this is not an empty database, get the application collection, else return empty
                    //
                    ExportFilename = "cdef_export_" + encodeText(genericController.GetRandomInteger(cpCore)) + ".xml";
                    ExportPathPage = "tmp\\" + ExportFilename;
                    exportApplicationCDefXml(cpCore, ExportPathPage, true);
                    CollectionData = cpCore.privateFiles.readFile(ExportPathPage);
                    cpCore.privateFiles.deleteFile(ExportPathPage);
                    returnColl = installCollection_LoadXmlToMiniCollection(cpCore, CollectionData, false, false, isNewBuild, new miniCollectionModel());
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return returnColl;
        }
        //
        //====================================================================================================
        /// <summary>
        ///  Get an XML nodes attribute based on its name
        /// </summary>
        public static string GetXMLAttribute(coreClass cpCore, bool Found, XmlNode Node, string Name, string DefaultIfNotFound) {
            string returnAttr = "";
            try {
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlAttribute NodeAttribute = null;
                XmlNode ResultNode = null;
                string UcaseName = null;
                //
                Found = false;
                ResultNode = Node.Attributes.GetNamedItem(Name);
                if (ResultNode == null) {
                    UcaseName = genericController.vbUCase(Name);
                    foreach (XmlAttribute NodeAttribute in Node.Attributes) {
                        if (genericController.vbUCase(NodeAttribute.Name) == UcaseName) {
                            returnAttr = NodeAttribute.Value;
                            Found = true;
                            break;
                        }
                    }
                    if (!Found) {
                        returnAttr = DefaultIfNotFound;
                    }
                } else {
                    returnAttr = ResultNode.Value;
                    Found = true;
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return returnAttr;
        }
        //
        //====================================================================================================
        //
        private static double GetXMLAttributeNumber(coreClass cpCore, bool Found, XmlNode Node, string Name, string DefaultIfNotFound) {
            return encodeNumber(GetXMLAttribute(cpCore, Found, Node, Name, DefaultIfNotFound));
        }
        //
        //====================================================================================================
        //
        private static bool GetXMLAttributeBoolean(coreClass cpCore, bool Found, XmlNode Node, string Name, bool DefaultIfNotFound) {
            return genericController.encodeBoolean(GetXMLAttribute(cpCore, Found, Node, Name, encodeText(DefaultIfNotFound)));
        }
        //
        //====================================================================================================
        //
        private static int GetXMLAttributeInteger(coreClass cpCore, bool Found, XmlNode Node, string Name, int DefaultIfNotFound) {
            return genericController.encodeInteger(GetXMLAttribute(cpCore, Found, Node, Name, DefaultIfNotFound.ToString()));
        }
        //
        //====================================================================================================
        //
        private static bool TextMatch( string Source1, string Source2) {
            //return !((Source1 == null) || (Source2 == null) || (Source1.ToLower() != Source2.ToLower()));
            if ( (Source1 == null) || (Source2 == null )) {
                return false;
            }else {
                return (Source1.ToLower() == Source2.ToLower());
            }
        }
        //
        //====================================================================================================
        //
        private static string GetMenuNameSpace(coreClass cpCore, Dictionary<string,miniCollectionModel.collectionMenuModel> menus, miniCollectionModel.collectionMenuModel menu, string UsedIDList) {
            string returnAttr = "";
            try {
                string ParentName = null;
                int Ptr = 0;
                string Prefix = null;
                string LCaseParentName = null;

                //
                ParentName = menu.ParentName;
                if (!string.IsNullOrEmpty(ParentName)) {
                    LCaseParentName = genericController.vbLCase(ParentName);
                    foreach ( var kvp in menus) {
                        miniCollectionModel.collectionMenuModel testMenu = kvp.Value;
                        if (genericController.vbInstr(1, "," + UsedIDList + ",", "," + Ptr.ToString() + ",") == 0) {
                            if (LCaseParentName == genericController.vbLCase(testMenu.Name) && (menu.IsNavigator == testMenu.IsNavigator)) {
                                Prefix = GetMenuNameSpace(cpCore, menus, testMenu, UsedIDList + "," + menu.Guid);
                                if (string.IsNullOrEmpty(Prefix)) {
                                    returnAttr = ParentName;
                                } else {
                                    returnAttr = Prefix + "." + ParentName;
                                }
                                break;
                            }
                        }

                    }
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return returnAttr;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create an entry in the Sort Methods Table
        /// </summary>
        private static void VerifySortMethod(coreClass cpCore, string Name, string OrderByCriteria) {
            try {
                //
                DataTable dt = null;
                sqlFieldListClass sqlList = new sqlFieldListClass();
                //
                sqlList.add("name", cpCore.db.encodeSQLText(Name));
                sqlList.add("CreatedBy", "0");
                sqlList.add("OrderByClause", cpCore.db.encodeSQLText(OrderByCriteria));
                sqlList.add("active", SQLTrue);
                sqlList.add("ContentControlID", Models.Complex.cdefModel.getContentId(cpCore, "Sort Methods").ToString());
                //
                dt = cpCore.db.openTable("Default", "ccSortMethods", "Name=" + cpCore.db.encodeSQLText(Name), "ID", "ID", 1);
                if (dt.Rows.Count > 0) {
                    //
                    // update sort method
                    //
                    cpCore.db.updateTableRecord("Default", "ccSortMethods", "ID=" + genericController.encodeInteger(dt.Rows[0]["ID"]).ToString(), sqlList);
                } else {
                    //
                    // Create the new sort method
                    //
                    cpCore.db.insertTableRecord("Default", "ccSortMethods", sqlList);
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public static void VerifySortMethods(coreClass cpCore) {
            try {
                //
                logController.appendLogInstall(cpCore, "Verify Sort Records");
                //
                VerifySortMethod(cpCore, "By Name", "Name");
                VerifySortMethod(cpCore, "By Alpha Sort Order Field", "SortOrder");
                VerifySortMethod(cpCore, "By Date", "DateAdded");
                VerifySortMethod(cpCore, "By Date Reverse", "DateAdded Desc");
                VerifySortMethod(cpCore, "By Alpha Sort Order Then Oldest First", "SortOrder,ID");
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get a ContentID from the ContentName using just the tables
        /// </summary>
        private static void VerifyContentFieldTypes(coreClass cpCore) {
            try {
                //
                int RowsFound = 0;
                int CID = 0;
                bool TableBad = false;
                int RowsNeeded = 0;
                //
                // ----- make sure there are enough records
                //
                TableBad = false;
                RowsFound = 0;
                using (DataTable rs = cpCore.db.executeQuery("Select ID from ccFieldTypes order by id")) {
                    if (!isDataTableOk(rs)) {
                        //
                        // problem
                        //
                        TableBad = true;
                    } else {
                        //
                        // Verify the records that are there
                        //
                        RowsFound = 0;
                        foreach (DataRow dr in rs.Rows) {
                            RowsFound = RowsFound + 1;
                            if (RowsFound != genericController.encodeInteger(dr["ID"])) {
                                //
                                // Bad Table
                                //
                                TableBad = true;
                                break;
                            }
                        }
                    }

                }
                //
                // ----- Replace table if needed
                //
                if (TableBad) {
                    cpCore.db.deleteTable("Default", "ccFieldTypes");
                    cpCore.db.createSQLTable("Default", "ccFieldTypes");
                    RowsFound = 0;
                }
                //
                // ----- Add the number of rows needed
                //
                RowsNeeded = FieldTypeIdMax - RowsFound;
                if (RowsNeeded > 0) {
                    CID = Models.Complex.cdefModel.getContentId(cpCore, "Content Field Types");
                    if (CID <= 0) {
                        //
                        // Problem
                        //
                        cpCore.handleException(new ApplicationException("Content Field Types content definition was not found"));
                    } else {
                        while (RowsNeeded > 0) {
                            cpCore.db.executeQuery("Insert into ccFieldTypes (active,contentcontrolid)values(1," + CID + ")");
                            RowsNeeded = RowsNeeded - 1;
                        }
                    }
                }
                //
                // ----- Update the Names of each row
                //
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Integer' where ID=1;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Text' where ID=2;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='LongText' where ID=3;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Boolean' where ID=4;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Date' where ID=5;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='File' where ID=6;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Lookup' where ID=7;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Redirect' where ID=8;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Currency' where ID=9;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='TextFile' where ID=10;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Image' where ID=11;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Float' where ID=12;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='AutoIncrement' where ID=13;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='ManyToMany' where ID=14;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Member Select' where ID=15;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='CSS File' where ID=16;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='XML File' where ID=17;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Javascript File' where ID=18;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Link' where ID=19;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='Resource Link' where ID=20;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='HTML' where ID=21;");
                cpCore.db.executeQuery("Update ccFieldTypes Set active=1,Name='HTML File' where ID=22;");
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public static void exportApplicationCDefXml(coreClass cpCore, string privateFilesPathFilename, bool IncludeBaseFields) {
            try {
                collectionXmlController XML = null;
                string Content = null;
                //
                XML = new collectionXmlController(cpCore);
                Content = XML.getApplicationCollectionXml(IncludeBaseFields);
                cpCore.privateFiles.saveFile(privateFilesPathFilename, Content);
                XML = null;
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        private static bool downloadCollectionFiles(coreClass cpCore, string workingPath, string CollectionGuid, ref DateTime return_CollectionLastChangeDate, ref string return_ErrorMessage) {
            bool tempDownloadCollectionFiles = false;
            tempDownloadCollectionFiles = false;
            try {
                //
                int CollectionFileCnt = 0;
                string CollectionFilePath = null;
                XmlDocument Doc = new XmlDocument();
                string URL = null;
                string ResourceFilename = null;
                string ResourceLink = null;
                string CollectionVersion = null;
                string CollectionFileLink = null;
                XmlDocument CollectionFile = new XmlDocument();
                string Collectionname = null;
                int Pos = 0;
                string UserError = null;
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode CDefSection = null;
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode CDefInterfaces = null;
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode ActiveXNode = null;
                string errorPrefix = null;
                int downloadRetry = 0;
                const int downloadRetryMax = 3;
                //
                logController.appendLogInstall(cpCore, "downloading collection [" + CollectionGuid + "]");
                //
                //---------------------------------------------------------------------------------------------------------------
                // Request the Download file for this collection
                //---------------------------------------------------------------------------------------------------------------
                //
                Doc = new XmlDocument();
                URL = "http://support.contensive.com/GetCollection?iv=" + cpCore.codeVersion() + "&guid=" + CollectionGuid;
                errorPrefix = "DownloadCollectionFiles, Error reading the collection library status file from the server for Collection [" + CollectionGuid + "], download URL [" + URL + "]. ";
                downloadRetry = 0;
                int downloadDelay = 2000;
                do {
                    try {
                        tempDownloadCollectionFiles = true;
                        return_ErrorMessage = "";
                        //
                        // -- pause for a second between fetches to pace the server (<10 hits in 10 seconds)
                        Thread.Sleep(downloadDelay);
                        //
                        // -- download file
                        System.Net.WebRequest rq = System.Net.WebRequest.Create(URL);
                        rq.Timeout = 60000;
                        System.Net.WebResponse response = rq.GetResponse();
                        Stream responseStream = response.GetResponseStream();
                        XmlTextReader reader = new XmlTextReader(responseStream);
                        Doc.Load(reader);
                        break;
                        //Call Doc.Load(URL)
                    } catch (Exception ex) {
                        //
                        // this error could be data related, and may not be critical. log issue and continue
                        //
                        downloadDelay += 2000;
                        return_ErrorMessage = "There was an error while requesting the download details for collection [" + CollectionGuid + "]";
                        tempDownloadCollectionFiles = false;
                        logController.appendLogInstall(cpCore, errorPrefix + "There was a parse error reading the response [" + ex.ToString() + "]");
                    }
                    downloadRetry += 1;
                } while (downloadRetry < downloadRetryMax);
                if (string.IsNullOrEmpty(return_ErrorMessage)) {
                    //
                    // continue if no errors
                    //
                    if (Doc.DocumentElement.Name.ToLower() != genericController.vbLCase(DownloadFileRootNode)) {
                        return_ErrorMessage = "The collection file from the server was Not valid for collection [" + CollectionGuid + "]";
                        tempDownloadCollectionFiles = false;
                        logController.appendLogInstall(cpCore, errorPrefix + "The response has a basename [" + Doc.DocumentElement.Name + "] but [" + DownloadFileRootNode + "] was expected.");
                    } else {
                        //
                        //------------------------------------------------------------------
                        // Parse the Download File and download each file into the working folder
                        //------------------------------------------------------------------
                        //
                        if (Doc.DocumentElement.ChildNodes.Count == 0) {
                            return_ErrorMessage = "The collection library status file from the server has a valid basename, but no childnodes.";
                            logController.appendLogInstall(cpCore, errorPrefix + "The collection library status file from the server has a valid basename, but no childnodes. The collection was probably Not found");
                            tempDownloadCollectionFiles = false;
                        } else {
                            foreach (XmlNode CDefSection in Doc.DocumentElement.ChildNodes) {
                                switch (genericController.vbLCase(CDefSection.Name)) {
                                    case "collection":
                                        //
                                        // Read in the interfaces and save to Add-ons
                                        //
                                        ResourceFilename = "";
                                        ResourceLink = "";
                                        Collectionname = "";
                                        CollectionGuid = "";
                                        CollectionVersion = "";
                                        CollectionFileLink = "";
                                        foreach (XmlNode CDefInterfaces in CDefSection.ChildNodes) {
                                            switch (genericController.vbLCase(CDefInterfaces.Name)) {
                                                case "name":
                                                    Collectionname = CDefInterfaces.InnerText;
                                                    break;
                                                case "help":
                                                    //CollectionHelp = CDefInterfaces.innerText
                                                    cpCore.privateFiles.saveFile(workingPath + "Collection.hlp", CDefInterfaces.InnerText);
                                                    break;
                                                case "guid":
                                                    CollectionGuid = CDefInterfaces.InnerText;
                                                    break;
                                                case "lastchangedate":
                                                    return_CollectionLastChangeDate = genericController.encodeDate(CDefInterfaces.InnerText);
                                                    break;
                                                case "version":
                                                    CollectionVersion = CDefInterfaces.InnerText;
                                                    break;
                                                case "collectionfilelink":
                                                    CollectionFileLink = CDefInterfaces.InnerText;
                                                    CollectionFileCnt = CollectionFileCnt + 1;
                                                    if (!string.IsNullOrEmpty(CollectionFileLink)) {
                                                        Pos = CollectionFileLink.LastIndexOf("/") + 1;
                                                        if ((Pos <= 0) && (Pos < CollectionFileLink.Length)) {
                                                            //
                                                            // Skip this file because the collecion file link has no slash (no file)
                                                            //
                                                            logController.appendLogInstall(cpCore, errorPrefix + "Collection [" + Collectionname + "] was Not installed because the Collection File Link does Not point to a valid file [" + CollectionFileLink + "]");
                                                        } else {
                                                            CollectionFilePath = workingPath + CollectionFileLink.Substring(Pos);
                                                            cpCore.privateFiles.SaveRemoteFile(CollectionFileLink, CollectionFilePath);
                                                            // BuildCollectionFolder takes care of the unzipping.
                                                            //If genericController.vbLCase(Right(CollectionFilePath, 4)) = ".zip" Then
                                                            //    Call UnzipAndDeleteFile_AndWait(CollectionFilePath)
                                                            //End If
                                                            //DownloadCollectionFiles = True
                                                        }
                                                    }
                                                    break;
                                                case "activexdll":
                                                case "resourcelink":
                                                    //
                                                    // save the filenames and download them only if OKtoinstall
                                                    //
                                                    ResourceFilename = "";
                                                    ResourceLink = "";
                                                    foreach (XmlNode ActiveXNode in CDefInterfaces.ChildNodes) {
                                                        switch (genericController.vbLCase(ActiveXNode.Name)) {
                                                            case "filename":
                                                                ResourceFilename = ActiveXNode.InnerText;
                                                                break;
                                                            case "link":
                                                                ResourceLink = ActiveXNode.InnerText;
                                                                break;
                                                        }
                                                    }
                                                    if (string.IsNullOrEmpty(ResourceLink)) {
                                                        UserError = "There was an error processing a collection in the download file [" + Collectionname + "]. An ActiveXDll node with filename [" + ResourceFilename + "] contained no 'Link' attribute.";
                                                        logController.appendLogInstall(cpCore, errorPrefix + UserError);
                                                    } else {
                                                        if (string.IsNullOrEmpty(ResourceFilename)) {
                                                            //
                                                            // Take Filename from Link
                                                            //
                                                            Pos = ResourceLink.LastIndexOf("/") + 1;
                                                            if (Pos != 0) {
                                                                ResourceFilename = ResourceLink.Substring(Pos);
                                                            }
                                                        }
                                                        if (string.IsNullOrEmpty(ResourceFilename)) {
                                                            UserError = "There was an error processing a collection in the download file [" + Collectionname + "]. The ActiveX filename attribute was empty, and the filename could not be read from the link [" + ResourceLink + "].";
                                                            logController.appendLogInstall(cpCore, errorPrefix + UserError);
                                                        } else {
                                                            cpCore.privateFiles.SaveRemoteFile(ResourceLink, workingPath + ResourceFilename);
                                                        }
                                                    }
                                                    break;
                                            }
                                        }
                                        break;
                                }
                            }
                            if (CollectionFileCnt == 0) {
                                logController.appendLogInstall(cpCore, errorPrefix + "The collection was requested and downloaded, but was not installed because the download file did not have a collection root node.");
                            }
                        }
                    }
                }
                //
                // no - register anything that downloaded correctly - if this collection contains an import, and one of the imports has a problem, all the rest need to continue
                //
                //
                //If Not DownloadCollectionFiles Then
                //    '
                //    ' Must clear these out, if there is an error, a reset will keep the error message from making it to the page
                //    '
                //    Return_IISResetRequired = False
                //    Return_RegisterList = ""
                //End If
                //
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return tempDownloadCollectionFiles;
        }
        //
        //====================================================================================================
        //
        public static bool installCollectionFromRemoteRepo(coreClass cpCore, string collectionGuid, ref string return_ErrorMessage, string ImportFromCollectionsGuidList, bool IsNewBuild, ref List<string> nonCriticalErrorList) {
            bool UpgradeOK = true;
            try {
                if (string.IsNullOrWhiteSpace(collectionGuid)) {
                    logController.appendLog(cpCore, "collectionGuid is null", "debug");
                } else {
                    //
                    // normalize guid
                    if (collectionGuid.Length < 38) {
                        if (collectionGuid.Length == 32) {
                            collectionGuid = collectionGuid.Left(8) + "-" + collectionGuid.Substring(8, 4) + "-" + collectionGuid.Substring(12, 4) + "-" + collectionGuid.Substring(16, 4) + "-" + collectionGuid.Substring(20);
                        }
                        if (collectionGuid.Length == 36) {
                            collectionGuid = "{" + collectionGuid + "}";
                        }
                    }
                    //
                    // Install it if it is not already here
                    //
                    string CollectionVersionFolderName = GetCollectionPath(cpCore, collectionGuid);
                    if (string.IsNullOrEmpty(CollectionVersionFolderName)) {
                        //
                        // Download all files for this collection and build the collection folder(s)
                        //
                        string workingPath = cpCore.addon.getPrivateFilesAddonPath() + "temp_" + genericController.GetRandomInteger(cpCore) + "\\";
                        cpCore.privateFiles.createPath(workingPath);
                        //
                        DateTime CollectionLastChangeDate = default(DateTime);
                        UpgradeOK = downloadCollectionFiles(cpCore, workingPath, collectionGuid, ref CollectionLastChangeDate, ref return_ErrorMessage);
                        if (!UpgradeOK) {
                            //UpgradeOK = UpgradeOK;
                        } else {
                            List<string> collectionGuidList = new List<string>();
                            UpgradeOK = buildLocalCollectionReposFromFolder(cpCore, workingPath, CollectionLastChangeDate, ref collectionGuidList, ref return_ErrorMessage, false);
                            if (!UpgradeOK) {
                                //UpgradeOK = UpgradeOK;
                            }
                        }
                        //
                        cpCore.privateFiles.deleteFolder(workingPath);
                    }
                    //
                    // Upgrade the server from the collection files
                    //
                    if (UpgradeOK) {
                        UpgradeOK = installCollectionFromLocalRepo(cpCore, collectionGuid, cpCore.siteProperties.dataBuildVersion, ref return_ErrorMessage, ImportFromCollectionsGuidList, IsNewBuild, ref nonCriticalErrorList);
                        if (!UpgradeOK) {
                            //UpgradeOK = UpgradeOK;
                        }
                    }
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return UpgradeOK;
        }
        //
        //====================================================================================================
        public static bool installCollectionFromRemoteRepo(coreClass cpCore, string CollectionGuid, ref string return_ErrorMessage, string ImportFromCollectionsGuidList, bool IsNewBuild){
            var tmpList = new List<string> { };
            return installCollectionFromRemoteRepo(cpCore, CollectionGuid, ref return_ErrorMessage,ImportFromCollectionsGuidList, IsNewBuild, ref tmpList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Upgrades all collections, registers and resets the server if needed
        /// </summary>
        public static bool UpgradeLocalCollectionRepoFromRemoteCollectionRepo(coreClass cpCore, ref string return_ErrorMessage, ref string return_RegisterList, ref bool return_IISResetRequired, bool IsNewBuild, ref List<string> nonCriticalErrorList) {
            bool returnOk = true;
            try {
                bool localCollectionUpToDate = false;
                //string[] GuidArray = { };
                //int GuidCnt = 0;
                //int GuidPtr = 0;
                //int RequestPtr = 0;
                string SupportURL = null;
                string GuidList = null;
                DateTime CollectionLastChangeDate = default(DateTime);
                string workingPath = null;
                //string collectionServerXml = null;
                string LocalGuid = null;
                string LocalLastChangeDateStr = null;
                DateTime LocalLastChangeDate = default(DateTime);
                string LibName = "";
                bool LibSystem = false;
                string LibGUID = null;
                string LibLastChangeDateStr = null;
                string LibContensiveVersion = "";
                DateTime LibLastChangeDate = default(DateTime);
                XmlNode LocalLastChangeNode = null;
                XmlDocument LibraryCollections = new XmlDocument();
                //XmlDocument LocalCollections = new XmlDocument();
                //XmlDocument Doc = new XmlDocument();
                string Copy = null;
                //
                //-----------------------------------------------------------------------------------------------
                //   Load LocalCollections from the Collections.xml file
                //-----------------------------------------------------------------------------------------------
                //
                var localCollectionStoreList = new List<collectionStoreClass>();
                if ( getLocalCollectionStoreList(cpCore, ref localCollectionStoreList, ref return_ErrorMessage)) {
                    if (localCollectionStoreList.Count > 0) {
                        //
                        // Request collection updates 10 at a time
                        //
                        int packageSize = 0;
                        int packageNumber = 0;
                        foreach ( var collectionStore in localCollectionStoreList ) {
                            GuidList = GuidList + "," + collectionStore.guid;
                            packageSize += 1;
                            if (( packageSize>=10 ) | ( collectionStore == localCollectionStoreList.Last())) {
                                packageNumber += 1;
                                //
                                // -- send package of 10, or the last set
                                if (!string.IsNullOrEmpty(GuidList)) {
                                    logController.appendLogInstall(cpCore, "Fetch collection details for collections [" + GuidList + "]");
                                    GuidList = GuidList.Substring(1);
                                    //
                                    //-----------------------------------------------------------------------------------------------
                                    //   Load LibraryCollections from the Support Site
                                    //-----------------------------------------------------------------------------------------------
                                    //
                                    LibraryCollections = new XmlDocument();
                                    SupportURL = "http://support.contensive.com/GetCollectionList?iv=" + cpCore.codeVersion() + "&guidlist=" + EncodeRequestVariable(GuidList);
                                    bool loadOK = true;
                                    if ( packageNumber>1 ) {
                                        Thread.Sleep(2000);
                                    }
                                    try {
                                        LibraryCollections.Load(SupportURL);
                                    } catch (Exception) {
                                        Copy = "Error downloading or loading GetCollectionList from Support.";
                                        logController.appendLogInstall(cpCore, Copy + ", the request was [" + SupportURL + "]");
                                        return_ErrorMessage = return_ErrorMessage + "<P>" + Copy + "</P>";
                                        returnOk = false;
                                        loadOK = false;
                                    }
                                    if (loadOK) {
                                        {
                                            if (genericController.vbLCase(LibraryCollections.DocumentElement.Name) != genericController.vbLCase(CollectionListRootNode)) {
                                                Copy = "The GetCollectionList support site remote method returned an xml file with an invalid root node, [" + LibraryCollections.DocumentElement.Name + "] was received and [" + CollectionListRootNode + "] was expected.";
                                                logController.appendLogInstall(cpCore, Copy + ", the request was [" + SupportURL + "]");
                                                return_ErrorMessage = return_ErrorMessage + "<P>" + Copy + "</P>";
                                                returnOk = false;
                                            } else {
                                                //
                                                // -- Search for Collection Updates Needed
                                                foreach (var localTestCollection in localCollectionStoreList) {
                                                    localCollectionUpToDate = false;
                                                    LocalGuid = localTestCollection.guid.ToLower();
                                                    LocalLastChangeDate = localTestCollection.lastChangeDate;
                                                    //
                                                    // go through each collection on the Library and find the local collection guid
                                                    //
                                                    foreach (XmlNode LibListNode in LibraryCollections.DocumentElement.ChildNodes) {
                                                        if (localCollectionUpToDate) {
                                                            break;
                                                        }
                                                        switch (genericController.vbLCase(LibListNode.Name)) {
                                                            case "collection":
                                                                LibGUID = "";
                                                                LibLastChangeDateStr = "";
                                                                LibLastChangeDate = DateTime.MinValue;
                                                                foreach (XmlNode CollectionNode in LibListNode.ChildNodes) {
                                                                    switch (genericController.vbLCase(CollectionNode.Name)) {
                                                                        case "name":
                                                                            //
                                                                            LibName = genericController.vbLCase(CollectionNode.InnerText);
                                                                            break;
                                                                        case "system":
                                                                            //
                                                                            LibSystem = genericController.encodeBoolean(CollectionNode.InnerText);
                                                                            break;
                                                                        case "guid":
                                                                            //
                                                                            LibGUID = genericController.vbLCase(CollectionNode.InnerText);
                                                                            //LibGUID = genericController.vbReplace(LibGUID, "{", "")
                                                                            //LibGUID = genericController.vbReplace(LibGUID, "}", "")
                                                                            //LibGUID = genericController.vbReplace(LibGUID, "-", "")
                                                                            break;
                                                                        case "lastchangedate":
                                                                            //
                                                                            LibLastChangeDateStr = CollectionNode.InnerText;
                                                                            //LibLastChangeDateStr = LibLastChangeDateStr;
                                                                            break;
                                                                        case "contensiveversion":
                                                                            //
                                                                            LibContensiveVersion = CollectionNode.InnerText;
                                                                            break;
                                                                    }
                                                                }
                                                                if (!string.IsNullOrEmpty(LibGUID)) {
                                                                    if ((!string.IsNullOrEmpty(LibGUID)) & (LibGUID == LocalGuid) & ((string.IsNullOrEmpty(LibContensiveVersion)) || (string.CompareOrdinal(LibContensiveVersion, cpCore.codeVersion()) <= 0))) {
                                                                        logController.appendLogInstall(cpCore, "verify collection [" + LibGUID + "]");
                                                                        //
                                                                        // LibCollection matches the LocalCollection - process the upgrade
                                                                        //
                                                                        if (genericController.vbInstr(1, LibGUID, "58c9", 1) != 0) {
                                                                            //LibGUID = LibGUID;
                                                                        }
                                                                        if (!dateController.IsDate(LibLastChangeDateStr)) {
                                                                            LibLastChangeDate = DateTime.MinValue;
                                                                        } else {
                                                                            LibLastChangeDate = genericController.encodeDate(LibLastChangeDateStr);
                                                                        }
                                                                        // TestPoint 1.1 - Test each collection for upgrade
                                                                        if (LibLastChangeDate > LocalLastChangeDate) {
                                                                            //
                                                                            // LibLastChangeDate <>0, and it is > local lastchangedate
                                                                            //
                                                                            workingPath = cpCore.addon.getPrivateFilesAddonPath() + "\\temp_" + genericController.GetRandomInteger(cpCore) + "\\";
                                                                            logController.appendLogInstall(cpCore, "Upgrading Collection [" + LibGUID + "], Library name [" + LibName + "], because LocalChangeDate [" + LocalLastChangeDate + "] < LibraryChangeDate [" + LibLastChangeDate + "]");
                                                                            //
                                                                            // Upgrade Needed
                                                                            //
                                                                            cpCore.privateFiles.createPath(workingPath);
                                                                            //
                                                                            returnOk = downloadCollectionFiles(cpCore, workingPath, LibGUID, ref CollectionLastChangeDate, ref return_ErrorMessage);
                                                                            if (returnOk) {
                                                                                List<string> listGuidList = new List<string>();
                                                                                returnOk = buildLocalCollectionReposFromFolder(cpCore, workingPath, CollectionLastChangeDate, ref listGuidList, ref return_ErrorMessage, false);
                                                                            }
                                                                            //
                                                                            cpCore.privateFiles.deleteFolder(workingPath);
                                                                            //
                                                                            // Upgrade the apps from the collection files, do not install on any apps
                                                                            //
                                                                            if (returnOk) {
                                                                                returnOk = installCollectionFromLocalRepo(cpCore, LibGUID, cpCore.siteProperties.dataBuildVersion, ref return_ErrorMessage, "", IsNewBuild, ref nonCriticalErrorList);
                                                                            }
                                                                            //
                                                                            // make sure this issue is logged and clear the flag to let other local collections install
                                                                            //
                                                                            if (!returnOk) {
                                                                                logController.appendLogInstall(cpCore, "There was a problem upgrading Collection [" + LibGUID + "], Library name [" + LibName + "], error message [" + return_ErrorMessage + "], will clear error and continue with the next collection, the request was [" + SupportURL + "]");
                                                                                returnOk = true;
                                                                            }
                                                                        }
                                                                        //
                                                                        // this local collection has been resolved, go to the next local collection
                                                                        //
                                                                        localCollectionUpToDate = true;
                                                                        //
                                                                        if (!returnOk) {
                                                                            logController.appendLogInstall(cpCore, "There was a problem upgrading Collection [" + LibGUID + "], Library name [" + LibName + "], error message [" + return_ErrorMessage + "], will clear error and continue with the next collection");
                                                                            returnOk = true;
                                                                        }
                                                                    }
                                                                }
                                                                break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                packageSize = 0;
                                GuidList = "";
                            }
                        }
                    }
                };
                //collectionServerXml = getLocalCollectionStoreListXml(cpCore);
                //if (!string.IsNullOrEmpty(collectionServerXml)) {
                //    LocalCollections = new XmlDocument();
                //    try {
                //        LocalCollections.LoadXml(collectionServerXml);
                //    } catch (Exception) {
                //        Copy = "Error loading privateFiles\\addons\\Collections.xml";
                //        logController.appendLogInstall(cpCore, Copy);
                //        return_ErrorMessage = return_ErrorMessage + "<P>" + Copy + "</P>";
                //        returnOk = false;
                //    }
                //    if (returnOk) {
                //        if (genericController.vbLCase(LocalCollections.DocumentElement.Name) != genericController.vbLCase(CollectionListRootNode)) {
                //            Copy = "The addons\\Collections.xml has an invalid root node, [" + LocalCollections.DocumentElement.Name + "] was received and [" + CollectionListRootNode + "] was expected.";
                //            logController.appendLogInstall(cpCore, Copy);
                //            return_ErrorMessage = return_ErrorMessage + "<P>" + Copy + "</P>";
                //            returnOk = false;
                //        } else {
                //            //
                //            // Get a list of the collection guids on this server
                //            //

                //            GuidCnt = 0;
                //            if (genericController.vbLCase(LocalCollections.DocumentElement.Name) == "collectionlist") {
                //                foreach (XmlNode LocalListNode in LocalCollections.DocumentElement.ChildNodes) {
                //                    switch (genericController.vbLCase(LocalListNode.Name)) {
                //                        case "collection":
                //                            foreach (XmlNode CollectionNode in LocalListNode.ChildNodes) {
                //                                if (genericController.vbLCase(CollectionNode.Name) == "guid") {
                //                                    Array.Resize(ref GuidArray, GuidCnt + 1);
                //                                    GuidArray[GuidCnt] = CollectionNode.InnerText;
                //                                    GuidCnt = GuidCnt + 1;
                //                                    break;
                //                                }
                //                            }
                //                            break;
                //                    }
                //                }
                //            }



                //        }
                //    }
                //}
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return returnOk;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Upgrade a collection from the files in a working folder
        /// </summary>
        public static bool buildLocalCollectionReposFromFolder(coreClass cpCore, string sourcePrivateFolderPath, DateTime CollectionLastChangeDate, ref List<string> return_CollectionGUIDList, ref string return_ErrorMessage, bool allowLogging) {
            bool success = false;
            try {
                if (cpCore.privateFiles.pathExists(sourcePrivateFolderPath)) {
                    logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, processing files in private folder [" + sourcePrivateFolderPath + "]");
                    FileInfo[] SrcFileNamelist = cpCore.privateFiles.getFileList(sourcePrivateFolderPath);
                    foreach (FileInfo file in SrcFileNamelist) {
                        if ((file.Extension == ".zip") || (file.Extension == ".xml")) {
                            string collectionGuid = "";
                            success = buildLocalCollectionRepoFromFile(cpCore, sourcePrivateFolderPath + file.Name, CollectionLastChangeDate, ref collectionGuid, ref return_ErrorMessage, allowLogging);
                            return_CollectionGUIDList.Add(collectionGuid);
                        }
                    }
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return success;
        }
        //
        //====================================================================================================
        //
        public static bool buildLocalCollectionRepoFromFile(coreClass cpCore, string collectionPathFilename, DateTime CollectionLastChangeDate, ref string return_CollectionGUID, ref string return_ErrorMessage, bool allowLogging) {
            bool tempBuildLocalCollectionRepoFromFile = false;
            bool result = true;
            try {
                string CollectionVersionFolderName = "";
                DateTime ChildCollectionLastChangeDate = default(DateTime);
                string ChildWorkingPath = null;
                string ChildCollectionGUID = null;
                string ChildCollectionName = null;
                bool Found = false;
                XmlDocument CollectionFile = new XmlDocument();
                string Collectionname = "";
                DateTime NowTime = default(DateTime);
                int NowPart = 0;
                FileInfo[] SrcFileNamelist = null;
                string TimeStamp = null;
                int Pos = 0;
                string CollectionFolder = null;
                string CollectionGuid = "";
                bool IsFound = false;
                string Filename = null;
                XmlDocument Doc = new XmlDocument();
                bool StatusOK = false;
                string CollectionFileBaseName = null;
                collectionXmlController XMLTools = new collectionXmlController(cpCore);
                string CollectionFolderName = "";
                bool CollectionFileFound = false;
                bool ZipFileFound = false;
                string collectionPath = "";
                string collectionFilename = "";
                //
                // process all xml files in this workingfolder
                //
                if (allowLogging) {
                    logController.appendLog(cpCore, "BuildLocalCollectionFolder(), Enter");
                }
                //
                cpCore.privateFiles.splitPathFilename(collectionPathFilename, ref collectionPath, ref collectionFilename);
                if (!cpCore.privateFiles.pathExists(collectionPath)) {
                    //
                    // The working folder is not there
                    //
                    result = false;
                    return_ErrorMessage = "<p>There was a problem with the installation. The installation folder is not valid.</p>";
                    if (allowLogging) {
                        logController.appendLog(cpCore, "BuildLocalCollectionFolder(), " + return_ErrorMessage);
                    }
                    logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, CheckFileFolder was false for the private folder [" + collectionPath + "]");
                } else {
                    logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, processing files in private folder [" + collectionPath + "]");
                    //
                    // move collection file to a temp directory
                    //
                    string tmpInstallPath = "tmpInstallCollection" + genericController.createGuid().Replace("{", "").Replace("}", "").Replace("-", "") + "\\";
                    cpCore.privateFiles.copyFile(collectionPathFilename, tmpInstallPath + collectionFilename);
                    if (collectionFilename.ToLower().Substring(collectionFilename.Length - 4) == ".zip") {
                        cpCore.privateFiles.UnzipFile(tmpInstallPath + collectionFilename);
                        cpCore.privateFiles.deleteFile(tmpInstallPath + collectionFilename);
                    }
                    //
                    // install the individual files
                    //
                    SrcFileNamelist = cpCore.privateFiles.getFileList(tmpInstallPath);
                    if (true) {
                        //
                        // Process all non-zip files
                        //
                        foreach (FileInfo file in SrcFileNamelist) {
                            Filename = file.Name;
                            logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, processing files, filename=[" + Filename + "]");
                            if (genericController.vbLCase(Filename.Substring(Filename.Length - 4)) == ".xml") {
                                //
                                logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, processing xml file [" + Filename + "]");
                                //hint = hint & ",320"
                                CollectionFile = new XmlDocument();
                                bool loadOk = true;
                                try {
                                    CollectionFile.LoadXml(cpCore.privateFiles.readFile(tmpInstallPath + Filename));
                                } catch (Exception ex) {
                                    //
                                    // There was a parse error in this xml file. Set the return message and the flag
                                    // If another xml files shows up, and process OK it will cover this error
                                    //
                                    //hint = hint & ",330"
                                    return_ErrorMessage = "There was a problem installing the Collection File [" + tmpInstallPath + Filename + "]. The error reported was [" + ex.Message + "].";
                                    logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, error reading collection [" + collectionPathFilename + "]");
                                    //StatusOK = False
                                    loadOk = false;
                                }
                                if (loadOk) {
                                    //hint = hint & ",400"
                                    CollectionFileBaseName = genericController.vbLCase(CollectionFile.DocumentElement.Name);
                                    if ((CollectionFileBaseName != "contensivecdef") & (CollectionFileBaseName != CollectionFileRootNode) & (CollectionFileBaseName != genericController.vbLCase(CollectionFileRootNodeOld))) {
                                        //
                                        // Not a problem, this is just not a collection file
                                        //
                                        logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, xml base name wrong [" + CollectionFileBaseName + "]");
                                    } else {
                                        //
                                        // Collection File
                                        //
                                        //hint = hint & ",420"
                                        Collectionname = GetXMLAttribute(cpCore, IsFound, CollectionFile.DocumentElement, "name", "");
                                        if (string.IsNullOrEmpty(Collectionname)) {
                                            //
                                            // ----- Error condition -- it must have a collection name
                                            //
                                            result = false;
                                            return_ErrorMessage = "<p>There was a problem with this Collection. The collection file does not have a collection name.</p>";
                                            logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, collection has no name");
                                        } else {
                                            //
                                            //------------------------------------------------------------------
                                            // Build Collection folder structure in /Add-ons folder
                                            //------------------------------------------------------------------
                                            //
                                            //hint = hint & ",440"
                                            CollectionFileFound = true;
                                            CollectionGuid = GetXMLAttribute(cpCore, IsFound, CollectionFile.DocumentElement, "guid", Collectionname);
                                            if (string.IsNullOrEmpty(CollectionGuid)) {
                                                //
                                                // I hope I do not regret this
                                                //
                                                CollectionGuid = Collectionname;
                                            }

                                            CollectionVersionFolderName = GetCollectionPath(cpCore, CollectionGuid);
                                            if (!string.IsNullOrEmpty(CollectionVersionFolderName)) {
                                                //
                                                // This is an upgrade
                                                //
                                                Pos = genericController.vbInstr(1, CollectionVersionFolderName, "\\");
                                                if (Pos > 0) {
                                                    CollectionFolderName = CollectionVersionFolderName.Left( Pos - 1);
                                                }
                                            } else {
                                                //
                                                // This is an install
                                                //
                                                //hint = hint & ",460"
                                                CollectionFolderName = CollectionGuid;
                                                CollectionFolderName = genericController.vbReplace(CollectionFolderName, "{", "");
                                                CollectionFolderName = genericController.vbReplace(CollectionFolderName, "}", "");
                                                CollectionFolderName = genericController.vbReplace(CollectionFolderName, "-", "");
                                                CollectionFolderName = genericController.vbReplace(CollectionFolderName, " ", "");
                                                CollectionFolderName = Collectionname + "_" + CollectionFolderName;
                                            }
                                            CollectionFolder = cpCore.addon.getPrivateFilesAddonPath() + CollectionFolderName + "\\";
                                            if (!cpCore.privateFiles.pathExists(CollectionFolder)) {
                                                //
                                                // Create collection folder
                                                //
                                                //hint = hint & ",470"
                                                cpCore.privateFiles.createPath(CollectionFolder);
                                            }
                                            //
                                            // create a collection 'version' folder for these new files
                                            //
                                            TimeStamp = "";
                                            NowTime = DateTime.Now;
                                            NowPart = NowTime.Year;
                                            TimeStamp += NowPart.ToString();
                                            NowPart = NowTime.Month;
                                            if (NowPart < 10) {
                                                TimeStamp += "0";
                                            }
                                            TimeStamp += NowPart.ToString();
                                            NowPart = NowTime.Day;
                                            if (NowPart < 10) {
                                                TimeStamp += "0";
                                            }
                                            TimeStamp += NowPart.ToString();
                                            NowPart = NowTime.Hour;
                                            if (NowPart < 10) {
                                                TimeStamp += "0";
                                            }
                                            TimeStamp += NowPart.ToString();
                                            NowPart = NowTime.Minute;
                                            if (NowPart < 10) {
                                                TimeStamp += "0";
                                            }
                                            TimeStamp += NowPart.ToString();
                                            NowPart = NowTime.Second;
                                            if (NowPart < 10) {
                                                TimeStamp += "0";
                                            }
                                            TimeStamp += NowPart.ToString();
                                            CollectionVersionFolderName = CollectionFolderName + "\\" + TimeStamp;
                                            string CollectionVersionFolder = cpCore.addon.getPrivateFilesAddonPath() + CollectionVersionFolderName;
                                            string CollectionVersionPath = CollectionVersionFolder + "\\";
                                            cpCore.privateFiles.createPath(CollectionVersionPath);

                                            cpCore.privateFiles.copyFolder(tmpInstallPath, CollectionVersionFolder);
                                            //StatusOK = True
                                            //
                                            // Install activeX and search for importcollections
                                            //
                                            //hint = hint & ",500"
                                            foreach (XmlNode CDefSection in CollectionFile.DocumentElement.ChildNodes) {
                                                switch (genericController.vbLCase(CDefSection.Name)) {
                                                    case "resource":
                                                        //
                                                        // resource node, if executable node, save to RegisterList
                                                        //
                                                        //hint = hint & ",510"
                                                        //ResourceType = genericController.vbLCase(GetXMLAttribute(cpCore, IsFound, CDefSection, "type", ""))
                                                        //Dim resourceFilename As String = Trim(GetXMLAttribute(cpCore, IsFound, CDefSection, "name", ""))
                                                        //Dim resourcePathFilename As String = CollectionVersionPath & resourceFilename
                                                        //If resourceFilename = "" Then
                                                        //    '
                                                        //    ' filename is blank
                                                        //    '
                                                        //    'hint = hint & ",511"
                                                        //ElseIf Not cpCore.privateFiles.fileExists(resourcePathFilename) Then
                                                        //    '
                                                        //    ' resource is not here
                                                        //    '
                                                        //    'hint = hint & ",513"
                                                        //    result = False
                                                        //    return_ErrorMessage = "<p>There was a problem with the Collection File. The resource referenced in the collection file [" & resourceFilename & "] was not included in the resource files.</p>"
                                                        //    Call logController.appendInstallLog(cpCore, "BuildLocalCollectionFolder, The resource referenced in the collection file [" & resourceFilename & "] was not included in the resource files.")
                                                        //    'StatusOK = False
                                                        //Else
                                                        //    Select Case ResourceType
                                                        //        Case "executable"
                                                        //            '
                                                        //            ' Executable resources - add to register list
                                                        //            '
                                                        //            'hint = hint & ",520"
                                                        //            If False Then
                                                        //                '
                                                        //                ' file is already installed
                                                        //                '
                                                        //                'hint = hint & ",521"
                                                        //            Else
                                                        //                '
                                                        //                ' Add the file to be registered
                                                        //                '
                                                        //            End If
                                                        //        Case "www"
                                                        //        Case "file"
                                                        //    End Select
                                                        //End If
                                                        break;
                                                    case "interfaces":
                                                        //
                                                        // Compatibility only - this is deprecated - Install ActiveX found in Add-ons
                                                        //
                                                        //hint = hint & ",530"
                                                        //For Each CDefInterfaces In CDefSection.ChildNodes
                                                        //    AOName = GetXMLAttribute(cpCore, IsFound, CDefInterfaces, "name", "No Name")
                                                        //    If AOName = "" Then
                                                        //        AOName = "No Name"
                                                        //    End If
                                                        //    AOGuid = GetXMLAttribute(cpCore, IsFound, CDefInterfaces, "guid", AOName)
                                                        //    If AOGuid = "" Then
                                                        //        AOGuid = AOName
                                                        //    End If
                                                        //Next
                                                        break;
                                                    case "getcollection":
                                                    case "importcollection":
                                                        //
                                                        // -- Download Collection file into install folder
                                                        ChildCollectionName = GetXMLAttribute(cpCore, Found, CDefSection, "name", "");
                                                        ChildCollectionGUID = GetXMLAttribute(cpCore, Found, CDefSection, "guid", CDefSection.InnerText);
                                                        if (string.IsNullOrEmpty(ChildCollectionGUID)) {
                                                            ChildCollectionGUID = CDefSection.InnerText;
                                                        }
                                                        string statusMsg = "Installing collection [" + ChildCollectionName + ", " + ChildCollectionGUID + "] referenced from collection [" + Collectionname + "]";
                                                        logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, getCollection or importcollection, childCollectionName [" + ChildCollectionName + "], childCollectionGuid [" + ChildCollectionGUID + "]");
                                                        if (genericController.vbInstr(1, CollectionVersionPath, ChildCollectionGUID, 1) == 0) {
                                                            if (string.IsNullOrEmpty(ChildCollectionGUID)) {
                                                                //
                                                                // -- Needs a GUID to install
                                                                result = false;
                                                                return_ErrorMessage = statusMsg + ". The installation can not continue because an imported collection could not be downloaded because it does not include a valid GUID.";
                                                                logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, return message [" + return_ErrorMessage + "]");
                                                            } else if (GetCollectionPath(cpCore, ChildCollectionGUID) == "") {
                                                                logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, [" + ChildCollectionGUID + "], not found so needs to be installed");
                                                                //
                                                                // If it is not already installed, download and install it also
                                                                //
                                                                ChildWorkingPath = CollectionVersionPath + "\\" + ChildCollectionGUID + "\\";
                                                                //
                                                                // down an imported collection file
                                                                //
                                                                StatusOK = downloadCollectionFiles(cpCore, ChildWorkingPath, ChildCollectionGUID, ref ChildCollectionLastChangeDate, ref return_ErrorMessage);
                                                                if (!StatusOK) {

                                                                    logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, [" + statusMsg + "], downloadCollectionFiles returned error state, message [" + return_ErrorMessage + "]");
                                                                    if (string.IsNullOrEmpty(return_ErrorMessage)) {
                                                                        return_ErrorMessage = statusMsg + ". The installation can not continue because there was an unknown error while downloading the necessary collection file, [" + ChildCollectionGUID + "].";
                                                                    } else {
                                                                        return_ErrorMessage = statusMsg + ". The installation can not continue because there was an error while downloading the necessary collection file, guid [" + ChildCollectionGUID + "]. The error was [" + return_ErrorMessage + "]";
                                                                    }
                                                                } else {
                                                                    logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, [" + ChildCollectionGUID + "], downloadCollectionFiles returned OK");
                                                                    //
                                                                    // install the downloaded file
                                                                    //
                                                                    List<string> ChildCollectionGUIDList = new List<string>();
                                                                    StatusOK = buildLocalCollectionReposFromFolder(cpCore, ChildWorkingPath, ChildCollectionLastChangeDate, ref ChildCollectionGUIDList, ref return_ErrorMessage, allowLogging);
                                                                    if (!StatusOK) {
                                                                        logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, [" + statusMsg + "], BuildLocalCollectionFolder returned error state, message [" + return_ErrorMessage + "]");
                                                                        if (string.IsNullOrEmpty(return_ErrorMessage)) {
                                                                            return_ErrorMessage = statusMsg + ". The installation can not continue because there was an unknown error installing the included collection file, guid [" + ChildCollectionGUID + "].";
                                                                        } else {
                                                                            return_ErrorMessage = statusMsg + ". The installation can not continue because there was an unknown error installing the included collection file, guid [" + ChildCollectionGUID + "]. The error was [" + return_ErrorMessage + "]";
                                                                        }
                                                                    }
                                                                }
                                                                //
                                                                // -- remove child installation working folder
                                                                cpCore.privateFiles.deleteFolder(ChildWorkingPath);
                                                            } else {
                                                                //
                                                                //
                                                                //
                                                                logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, [" + ChildCollectionGUID + "], already installed");
                                                            }
                                                        }
                                                        break;
                                                }
                                                if (!string.IsNullOrEmpty(return_ErrorMessage)) {
                                                    //
                                                    // if error, no more nodes in this collection file
                                                    //
                                                    result = false;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(return_ErrorMessage)) {
                                //
                                // if error, no more files
                                //
                                result = false;
                                break;
                            }
                        }
                        if ((string.IsNullOrEmpty(return_ErrorMessage)) && (!CollectionFileFound)) {
                            //
                            // no errors, but the collection file was not found
                            //
                            if (ZipFileFound) {
                                //
                                // zip file found but did not qualify
                                //
                                return_ErrorMessage = "<p>There was a problem with the installation. The collection zip file was downloaded, but it did not include a valid collection xml file.</p>";
                            } else {
                                //
                                // zip file not found
                                //
                                return_ErrorMessage = "<p>There was a problem with the installation. The collection zip was not downloaded successfully.</p>";
                            }
                            //StatusOK = False
                        }
                    }
                    //
                    // delete the working folder
                    //
                    cpCore.privateFiles.deleteFolder(tmpInstallPath);
                }
                //
                // If the collection parsed correctly, update the Collections.xml file
                //
                if (string.IsNullOrEmpty(return_ErrorMessage)) {
                    UpdateConfig(cpCore, Collectionname, CollectionGuid, CollectionLastChangeDate, CollectionVersionFolderName);
                } else {
                    //
                    // there was an error processing the collection, be sure to save description in the log
                    //
                    result = false;
                    logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, ERROR Exiting, ErrorMessage [" + return_ErrorMessage + "]");
                }
                //
                logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder, Exiting with ErrorMessage [" + return_ErrorMessage + "]");
                //
                tempBuildLocalCollectionRepoFromFile = (string.IsNullOrEmpty(return_ErrorMessage));
                return_CollectionGUID = CollectionGuid;
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Upgrade Application from a local collection
        /// </summary>
        public static bool installCollectionFromLocalRepo(coreClass cpCore, string CollectionGuid, string ignore_BuildVersion, ref string return_ErrorMessage, string ImportFromCollectionsGuidList, bool IsNewBuild, ref List<string> nonCriticalErrorList) {
            bool result = true;
            try {
                string CollectionVersionFolderName = "";
                DateTime CollectionLastChangeDate = default(DateTime);
                string tempVar = "";
                GetCollectionConfig(cpCore, CollectionGuid, ref CollectionVersionFolderName, ref CollectionLastChangeDate, ref tempVar);
                if (string.IsNullOrEmpty(CollectionVersionFolderName)) {
                    result = false;
                    return_ErrorMessage = return_ErrorMessage + "<P>The collection was not installed from the local collections because the folder containing the Add-on's resources could not be found. It may not be installed locally.</P>";
                } else {
                    //
                    // Search Local Collection Folder for collection config file (xml file)
                    //
                    string CollectionVersionFolder = cpCore.addon.getPrivateFilesAddonPath() + CollectionVersionFolderName + "\\";
                    FileInfo[] srcFileInfoArray = cpCore.privateFiles.getFileList(CollectionVersionFolder);
                    if (srcFileInfoArray.Length == 0) {
                        result = false;
                        return_ErrorMessage = return_ErrorMessage + "<P>The collection was not installed because the folder containing the Add-on's resources was empty.</P>";
                    } else {
                        //
                        // collect list of DLL files and add them to the exec files if they were missed
                        List<string> assembliesInZip = new List<string>();
                        foreach (FileInfo file in srcFileInfoArray) {
                            if (file.Extension.ToLower() == "dll") {
                                if (!assembliesInZip.Contains(file.Name.ToLower())) {
                                    assembliesInZip.Add(file.Name.ToLower());
                                }
                            }
                        }
                        //
                        // -- Process the other files
                        //todo  NOTE: There is no C# equivalent to VB's implicit 'once only' variable initialization within loops, so the following variable declaration has been placed prior to the loop:
                        bool CollectionblockNavigatorNode_fileValueOK = false;
                        foreach (FileInfo file in srcFileInfoArray) {
                            if (genericController.vbLCase(file.Name.Substring(file.Name.Length - 4)) == ".xml") {
                                //
                                // -- XML file -- open it to figure out if it is one we can use
                                XmlDocument Doc = new XmlDocument();
                                string CollectionFilename = file.Name;
                                bool loadOK = true;
                                try {
                                    Doc.Load(cpCore.privateFiles.rootLocalPath + CollectionVersionFolder + file.Name);
                                } catch (Exception) {
                                    //
                                    // error - Need a way to reach the user that submitted the file
                                    //
                                    logController.appendLogInstall(cpCore, "There was an error reading the Meta data file [" + cpCore.privateFiles.rootLocalPath + CollectionVersionFolder + file.Name + "].");
                                    result = false;
                                    return_ErrorMessage = return_ErrorMessage + "<P>The collection was not installed because the xml collection file has an error</P>";
                                    loadOK = false;
                                }
                                if (loadOK) {
                                    if ((Doc.DocumentElement.Name.ToLower() == genericController.vbLCase(CollectionFileRootNode)) || (Doc.DocumentElement.Name.ToLower() == genericController.vbLCase(CollectionFileRootNodeOld))) {
                                        //
                                        //------------------------------------------------------------------------------------------------------
                                        // Collection File - import from sub so it can be re-entrant
                                        //------------------------------------------------------------------------------------------------------
                                        //
                                        bool IsFound = false;
                                        string Collectionname = GetXMLAttribute(cpCore, IsFound, Doc.DocumentElement, "name", "");
                                        if (string.IsNullOrEmpty(Collectionname)) {
                                            //
                                            // ----- Error condition -- it must have a collection name
                                            //
                                            //Call AppendAddonLog("UpgradeAppFromLocalCollection, collection has no name")
                                            logController.appendLogInstall(cpCore, "UpgradeAppFromLocalCollection, collection has no name");
                                            result = false;
                                            return_ErrorMessage = return_ErrorMessage + "<P>The collection was not installed because the collection name in the xml collection file is blank</P>";
                                        } else {
                                            bool CollectionSystem_fileValueOK = false;
                                            bool CollectionUpdatable_fileValueOK = false;
                                            //												Dim CollectionblockNavigatorNode_fileValueOK As Boolean
                                            bool CollectionSystem = genericController.encodeBoolean(GetXMLAttribute(cpCore, CollectionSystem_fileValueOK, Doc.DocumentElement, "system", ""));
                                            int Parent_NavID = appBuilderController.verifyNavigatorEntry(cpCore, addonGuidManageAddon, "", "Manage Add-ons", "", "", "", false, false, false, true, "", "", "", 0);
                                            bool CollectionUpdatable = genericController.encodeBoolean(GetXMLAttribute(cpCore, CollectionUpdatable_fileValueOK, Doc.DocumentElement, "updatable", ""));
                                            bool CollectionblockNavigatorNode = genericController.encodeBoolean(GetXMLAttribute(cpCore, CollectionblockNavigatorNode_fileValueOK, Doc.DocumentElement, "blockNavigatorNode", ""));
                                            string FileGuid = GetXMLAttribute(cpCore, IsFound, Doc.DocumentElement, "guid", Collectionname);
                                            if (string.IsNullOrEmpty(FileGuid)) {
                                                FileGuid = Collectionname;
                                            }
                                            if (CollectionGuid.ToLower() != genericController.vbLCase(FileGuid)) {
                                                //
                                                //
                                                //
                                                result = false;
                                                logController.appendLogInstall(cpCore, "Local Collection file contains a different GUID for [" + Collectionname + "] then Collections.xml");
                                                return_ErrorMessage = return_ErrorMessage + "<P>The collection was not installed because the unique number identifying the collection, called the guid, does not match the collection requested.</P>";
                                            } else {
                                                if (string.IsNullOrEmpty(CollectionGuid)) {
                                                    //
                                                    // I hope I do not regret this
                                                    //
                                                    CollectionGuid = Collectionname;
                                                }
                                                string onInstallAddonGuid = "";
                                                //
                                                //-------------------------------------------------------------------------------
                                                // ----- Pass 1
                                                // Go through all collection nodes
                                                // Process ImportCollection Nodes - so includeaddon nodes will work
                                                // these must be processes regardless of the state of this collection in this app
                                                // Get Resource file list
                                                //-------------------------------------------------------------------------------
                                                //
                                                //CollectionAddOnCnt = 0
                                                logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], pass 1");
                                                string wwwFileList = "";
                                                string ContentFileList = "";
                                                string ExecFileList = "";
                                                foreach (XmlNode CDefSection in Doc.DocumentElement.ChildNodes) {
                                                    switch (CDefSection.Name.ToLower()) {
                                                        case "resource":
                                                            //
                                                            // set wwwfilelist, contentfilelist, execfilelist
                                                            //
                                                            string resourceType = GetXMLAttribute(cpCore, IsFound, CDefSection, "type", "");
                                                            string resourcePath = GetXMLAttribute(cpCore, IsFound, CDefSection, "path", "");
                                                            string filename = GetXMLAttribute(cpCore, IsFound, CDefSection, "name", "");
                                                            //
                                                            logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], pass 1, resource found, name [" + filename + "], type [" + resourceType + "], path [" + resourcePath + "]");
                                                            //
                                                            filename = genericController.convertToDosSlash(filename);
                                                            string SrcPath = "";
                                                            string DstPath = resourcePath;
                                                            int Pos = genericController.vbInstr(1, filename, "\\");
                                                            if (Pos != 0) {
                                                                //
                                                                // Source path is in filename
                                                                //
                                                                SrcPath = filename.Left( Pos - 1);
                                                                filename = filename.Substring(Pos);
                                                                if (string.IsNullOrEmpty(resourcePath)) {
                                                                    //
                                                                    // No Resource Path give, use the same folder structure from source
                                                                    //
                                                                    DstPath = SrcPath;
                                                                } else {
                                                                    //
                                                                    // Copy file to resource path
                                                                    //
                                                                    DstPath = resourcePath;
                                                                }
                                                            }

                                                            string DstFilePath = genericController.vbReplace(DstPath, "/", "\\");
                                                            if (DstFilePath == "\\") {
                                                                DstFilePath = "";
                                                            }
                                                            if (!string.IsNullOrEmpty(DstFilePath)) {
                                                                if (DstFilePath.Left( 1) == "\\") {
                                                                    DstFilePath = DstFilePath.Substring(1);
                                                                }
                                                                if (DstFilePath.Substring(DstFilePath.Length - 1) != "\\") {
                                                                    DstFilePath = DstFilePath + "\\";
                                                                }
                                                            }

                                                            switch (resourceType.ToLower()) {
                                                                case "www":
                                                                    wwwFileList = wwwFileList + "\r\n" + DstFilePath + filename;
                                                                    logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], GUID [" + CollectionGuid + "], pass 1, copying file to www, src [" + CollectionVersionFolder + SrcPath + "], dst [" + cpCore.serverConfig.appConfig.appRootFilesPath + DstFilePath + "].");
                                                                    //Call logcontroller.appendInstallLog(cpCore, "install collection [" & Collectionname & "], GUID [" & CollectionGuid & "], pass 1, copying file to www, src [" & CollectionVersionFolder & SrcPath & "], dst [" & cpCore.serverConfig.clusterPath & cpCore.serverconfig.appConfig.appRootFilesPath & DstFilePath & "].")
                                                                    cpCore.privateFiles.copyFile(CollectionVersionFolder + SrcPath + filename, DstFilePath + filename, cpCore.appRootFiles);
                                                                    if (genericController.vbLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                                                                        logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], GUID [" + CollectionGuid + "], pass 1, unzipping www file [" + cpCore.serverConfig.appConfig.appRootFilesPath + DstFilePath + filename + "].");
                                                                        //Call logcontroller.appendInstallLog(cpCore, "install collection [" & Collectionname & "], GUID [" & CollectionGuid & "], pass 1, unzipping www file [" & cpCore.serverConfig.clusterPath & cpCore.serverconfig.appConfig.appRootFilesPath & DstFilePath & Filename & "].")
                                                                        cpCore.appRootFiles.UnzipFile(DstFilePath + filename);
                                                                    }
                                                                    break;
                                                                case "file":
                                                                case "content":
                                                                    ContentFileList = ContentFileList + "\r\n" + DstFilePath + filename;
                                                                    logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], GUID [" + CollectionGuid + "], pass 1, copying file to content, src [" + CollectionVersionFolder + SrcPath + "], dst [" + DstFilePath + "].");
                                                                    cpCore.privateFiles.copyFile(CollectionVersionFolder + SrcPath + filename, DstFilePath + filename, cpCore.cdnFiles);
                                                                    if (genericController.vbLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                                                                        logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], GUID [" + CollectionGuid + "], pass 1, unzipping content file [" + DstFilePath + filename + "].");
                                                                        cpCore.cdnFiles.UnzipFile(DstFilePath + filename);
                                                                    }
                                                                    break;
                                                                default:
                                                                    if (assembliesInZip.Contains(filename.ToLower())) {
                                                                        assembliesInZip.Remove(filename.ToLower());
                                                                    }
                                                                    ExecFileList = ExecFileList + "\r\n" + filename;
                                                                    break;
                                                            }
                                                            break;
                                                        case "getcollection":
                                                        case "importcollection":
                                                            //
                                                            // Get path to this collection and call into it
                                                            //
                                                            bool Found = false;
                                                            string ChildCollectionName = GetXMLAttribute(cpCore, Found, CDefSection, "name", "");
                                                            string ChildCollectionGUID = GetXMLAttribute(cpCore, Found, CDefSection, "guid", CDefSection.InnerText);
                                                            if (string.IsNullOrEmpty(ChildCollectionGUID)) {
                                                                ChildCollectionGUID = CDefSection.InnerText;
                                                            }
                                                            if ((ImportFromCollectionsGuidList + "," + CollectionGuid).IndexOf(ChildCollectionGUID, System.StringComparison.OrdinalIgnoreCase)  != -1) {
                                                                //
                                                                // circular import detected, this collection is already imported
                                                                //
                                                                logController.appendLogInstall(cpCore, "Circular import detected. This collection attempts to import a collection that had previously been imported. A collection can not import itself. The collection is [" + Collectionname + "], GUID [" + CollectionGuid + "], pass 1. The collection to be imported is [" + ChildCollectionName + "], GUID [" + ChildCollectionGUID + "]");
                                                            } else {
                                                                logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], pass 1, import collection found, name [" + ChildCollectionName + "], guid [" + ChildCollectionGUID + "]");
                                                                installCollectionFromRemoteRepo(cpCore, ChildCollectionGUID, ref return_ErrorMessage, ImportFromCollectionsGuidList, IsNewBuild, ref nonCriticalErrorList);
                                                                //if (true) {
                                                                //    installCollectionFromRemoteRepo(cpCore, ChildCollectionGUID, ref return_ErrorMessage, ImportFromCollectionsGuidList, IsNewBuild, ref nonCriticalErrorList);
                                                                //} else {
                                                                //    if (string.IsNullOrEmpty(ChildCollectionGUID)) {
                                                                //        logController.appendInstallLog(cpCore, "The importcollection node [" + ChildCollectionName + "] can not be upgraded because it does not include a valid guid.");
                                                                //    } else {
                                                                //        //
                                                                //        // This import occurred while upgrading an application from the local collections (Db upgrade or AddonManager)
                                                                //        // Its OK to install it if it is missing, but you do not need to upgrade the local collections from the Library
                                                                //        //
                                                                //        // 5/18/2008 -----------------------------------
                                                                //        // See if it is in the local collections storage. If yes, just upgrade this app with it. If not,
                                                                //        // it must be downloaded and the entire server must be upgraded
                                                                //        //
                                                                //        string ChildCollectionVersionFolderName = "";
                                                                //        DateTime ChildCollectionLastChangeDate = default(DateTime);
                                                                //        string tempVar2 = "";
                                                                //        GetCollectionConfig(cpCore, ChildCollectionGUID, ref ChildCollectionVersionFolderName, ref ChildCollectionLastChangeDate, ref tempVar2);
                                                                //        if (!string.IsNullOrEmpty(ChildCollectionVersionFolderName)) {
                                                                //            //
                                                                //            // It is installed in the local collections, update just this site
                                                                //            //
                                                                //            result &= installCollectionFromLocalRepo(cpCore, ChildCollectionGUID, cpCore.siteProperties.dataBuildVersion, ref return_ErrorMessage, ImportFromCollectionsGuidList + "," + CollectionGuid, IsNewBuild, ref nonCriticalErrorList);
                                                                //        }
                                                                //    }
                                                                //}
                                                            }
                                                            break;
                                                    }
                                                }
                                                //
                                                // -- any assemblies found in the zip that were not part of the resources section need to be added
                                                foreach (string filename in assembliesInZip) {
                                                    ExecFileList = ExecFileList + "\r\n" + filename;
                                                }
                                                //
                                                //-------------------------------------------------------------------------------
                                                // create an Add-on Collection record
                                                //-------------------------------------------------------------------------------
                                                //
                                                bool OKToInstall = false;
                                                logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], pass 1 done, create collection record.");
                                                AddonCollectionModel collection = AddonCollectionModel.create(cpCore, CollectionGuid);
                                                if (collection != null) {
                                                    //
                                                    // Upgrade addon
                                                    //
                                                    if (CollectionLastChangeDate == DateTime.MinValue) {
                                                        logController.appendLogInstall(cpCore, "collection [" + Collectionname + "], GUID [" + CollectionGuid + "], App has the collection, but the new version has no lastchangedate, so it will upgrade to this unknown (manual) version.");
                                                        OKToInstall = true;
                                                    } else if (collection.LastChangeDate < CollectionLastChangeDate) {
                                                        logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], GUID [" + CollectionGuid + "], App has an older version of collection. It will be upgraded.");
                                                        OKToInstall = true;
                                                    } else {
                                                        logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], GUID [" + CollectionGuid + "], App has an up-to-date version of collection. It will not be upgraded, but all imports in the new version will be checked.");
                                                        OKToInstall = false;
                                                    }
                                                } else {
                                                    //
                                                    // Install new on this application
                                                    //
                                                    collection = AddonCollectionModel.add(cpCore);
                                                    logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], GUID [" + CollectionGuid + "], App does not have this collection so it will be installed.");
                                                    OKToInstall = true;
                                                }
                                                string DataRecordList = "";
                                                if (!OKToInstall) {
                                                    //
                                                    // Do not install, but still check all imported collections to see if they need to be installed
                                                    // imported collections moved in front this check
                                                    //
                                                } else {
                                                    //
                                                    // ----- gather help nodes
                                                    //
                                                    string CollectionHelpLink = "";
                                                    foreach (XmlNode CDefSection in Doc.DocumentElement.ChildNodes) {
                                                        if (CDefSection.Name.ToLower() == "helplink") {
                                                            //
                                                            // only save the first
                                                            CollectionHelpLink = CDefSection.InnerText;
                                                            break;
                                                        }
                                                    }
                                                    //
                                                    // ----- set or clear all fields
                                                    collection.name = Collectionname;
                                                    collection.Help = "";
                                                    collection.ccguid = CollectionGuid;
                                                    collection.LastChangeDate = CollectionLastChangeDate;
                                                    if (CollectionSystem_fileValueOK) {
                                                        collection.System = CollectionSystem;
                                                    }
                                                    if (CollectionUpdatable_fileValueOK) {
                                                        collection.Updatable = CollectionUpdatable;
                                                    }
                                                    if (CollectionblockNavigatorNode_fileValueOK) {
                                                        collection.blockNavigatorNode = CollectionblockNavigatorNode;
                                                    }
                                                    collection.helpLink = CollectionHelpLink;
                                                    //
                                                    cpCore.db.deleteContentRecords("Add-on Collection CDef Rules", "CollectionID=" + collection.id);
                                                    cpCore.db.deleteContentRecords("Add-on Collection Parent Rules", "ParentID=" + collection.id);
                                                    //
                                                    // Store all resource found, new way and compatibility way
                                                    //
                                                    collection.ContentFileList = ContentFileList;
                                                    collection.ExecFileList = ExecFileList;
                                                    collection.wwwFileList = wwwFileList;
                                                    //
                                                    // ----- remove any current navigator nodes installed by the collection previously
                                                    //
                                                    if (collection.id != 0) {
                                                        cpCore.db.deleteContentRecords(cnNavigatorEntries, "installedbycollectionid=" + collection.id);
                                                    }
                                                    //
                                                    //-------------------------------------------------------------------------------
                                                    // ----- Pass 2
                                                    // Go through all collection nodes
                                                    // Process all cdef related nodes to the old upgrade
                                                    //-------------------------------------------------------------------------------
                                                    //
                                                    string CollectionWrapper = "";
                                                    logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], pass 2");
                                                    foreach (XmlNode CDefSection in Doc.DocumentElement.ChildNodes) {
                                                        switch (genericController.vbLCase(CDefSection.Name)) {
                                                            case "contensivecdef":
                                                                //
                                                                // old cdef xection -- take the inner
                                                                //
                                                                foreach (XmlNode ChildNode in CDefSection.ChildNodes) {
                                                                    CollectionWrapper += "\r\n" + ChildNode.OuterXml;
                                                                }
                                                                break;
                                                            case "cdef":
                                                            case "sqlindex":
                                                            case "style":
                                                            case "styles":
                                                            case "stylesheet":
                                                            case "adminmenu":
                                                            case "menuentry":
                                                            case "navigatorentry":
                                                                //
                                                                // handled by Upgrade class
                                                                CollectionWrapper += CDefSection.OuterXml;
                                                                break;
                                                        }
                                                    }
                                                    //
                                                    //-------------------------------------------------------------------------------
                                                    // ----- Post Pass 2
                                                    // if cdef were found, import them now
                                                    //-------------------------------------------------------------------------------
                                                    //
                                                    if (!string.IsNullOrEmpty(CollectionWrapper)) {
                                                        //
                                                        // -- Use the upgrade code to import this part
                                                        CollectionWrapper = "<" + CollectionFileRootNode + ">" + CollectionWrapper + "</" + CollectionFileRootNode + ">";
                                                        bool isBaseCollection = (baseCollectionGuid == CollectionGuid);
                                                        installCollectionFromLocalRepo_BuildDbFromXmlData(cpCore, CollectionWrapper, IsNewBuild, isBaseCollection, ref nonCriticalErrorList);
                                                        //
                                                        // -- Process nodes to save Collection data
                                                        XmlDocument NavDoc = new XmlDocument();
                                                        loadOK = true;
                                                        try {
                                                            NavDoc.LoadXml(CollectionWrapper);
                                                        } catch (Exception ex) {
                                                            //
                                                            // error - Need a way to reach the user that submitted the file
                                                            //
                                                            logController.appendLogInstall(cpCore, "Creating navigator entries, there was an error parsing the portion of the collection that contains cdef. Navigator entry creation was aborted. [There was an error reading the Meta data file.]");
                                                            result = false;
                                                            return_ErrorMessage = return_ErrorMessage + "<P>The collection was not installed because the xml collection file has an error.</P>";
                                                            loadOK = false;
                                                        }
                                                        if (loadOK) {
                                                            foreach (XmlNode CDefNode in NavDoc.DocumentElement.ChildNodes) {
                                                                switch (genericController.vbLCase(CDefNode.Name)) {
                                                                    case "cdef":
                                                                        string ContentName = GetXMLAttribute(cpCore, IsFound, CDefNode, "name", "");
                                                                        //
                                                                        // setup cdef rule
                                                                        //
                                                                        int ContentID = Models.Complex.cdefModel.getContentId(cpCore, ContentName);
                                                                        if (ContentID > 0) {
                                                                            int CS = cpCore.db.csInsertRecord("Add-on Collection CDef Rules", 0);
                                                                            if (cpCore.db.csOk(CS)) {
                                                                                cpCore.db.csSet(CS, "Contentid", ContentID);
                                                                                cpCore.db.csSet(CS, "CollectionID", collection.id);
                                                                            }
                                                                            cpCore.db.csClose(ref CS);
                                                                        }
                                                                        break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    //
                                                    //-------------------------------------------------------------------------------
                                                    // ----- Pass3
                                                    // create any data records
                                                    //
                                                    //   process after cdef builds
                                                    //   process seperate so another pass can create any lookup data from these records
                                                    //-------------------------------------------------------------------------------
                                                    //
                                                    logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], pass 3");
                                                    foreach (XmlNode CDefSection in Doc.DocumentElement.ChildNodes) {
                                                        switch (genericController.vbLCase(CDefSection.Name)) {
                                                            case "data":
                                                                //
                                                                // import content
                                                                //   This can only be done with matching guid
                                                                //
                                                                foreach (XmlNode ContentNode in CDefSection.ChildNodes) {
                                                                    if (genericController.vbLCase(ContentNode.Name) == "record") {
                                                                        //
                                                                        // Data.Record node
                                                                        //
                                                                        string ContentName = GetXMLAttribute(cpCore, IsFound, ContentNode, "content", "");
                                                                        if (string.IsNullOrEmpty(ContentName)) {
                                                                            logController.appendLogInstall(cpCore, "install collection file contains a data.record node with a blank content attribute.");
                                                                            result = false;
                                                                            return_ErrorMessage = return_ErrorMessage + "<P>Collection file contains a data.record node with a blank content attribute.</P>";
                                                                        } else {
                                                                            string ContentRecordGuid = GetXMLAttribute(cpCore, IsFound, ContentNode, "guid", "");
                                                                            string ContentRecordName = GetXMLAttribute(cpCore, IsFound, ContentNode, "name", "");
                                                                            if ((string.IsNullOrEmpty(ContentRecordGuid)) && (string.IsNullOrEmpty(ContentRecordName))) {
                                                                                logController.appendLogInstall(cpCore, "install collection file contains a data record node with neither guid nor name. It must have either a name or a guid attribute. The content is [" + ContentName + "]");
                                                                                result = false;
                                                                                return_ErrorMessage = return_ErrorMessage + "<P>The collection was not installed because the Collection file contains a data record node with neither name nor guid. This is not allowed. The content is [" + ContentName + "].</P>";
                                                                            } else {
                                                                                //
                                                                                // create or update the record
                                                                                //
                                                                                Models.Complex.cdefModel CDef = Models.Complex.cdefModel.getCdef(cpCore, ContentName);
                                                                                int cs = -1;
                                                                                if (!string.IsNullOrEmpty(ContentRecordGuid)) {
                                                                                    cs = cpCore.db.csOpen(ContentName, "ccguid=" + cpCore.db.encodeSQLText(ContentRecordGuid));
                                                                                } else {
                                                                                    cs = cpCore.db.csOpen(ContentName, "name=" + cpCore.db.encodeSQLText(ContentRecordName));
                                                                                }
                                                                                bool recordfound = true;
                                                                                if (!cpCore.db.csOk(cs)) {
                                                                                    //
                                                                                    // Insert the new record
                                                                                    //
                                                                                    recordfound = false;
                                                                                    cpCore.db.csClose(ref cs);
                                                                                    cs = cpCore.db.csInsertRecord(ContentName, 0);
                                                                                }
                                                                                if (cpCore.db.csOk(cs)) {
                                                                                    //
                                                                                    // Update the record
                                                                                    //
                                                                                    if (recordfound && (!string.IsNullOrEmpty(ContentRecordGuid))) {
                                                                                        //
                                                                                        // found by guid, use guid in list and save name
                                                                                        //
                                                                                        cpCore.db.csSet(cs, "name", ContentRecordName);
                                                                                        DataRecordList = DataRecordList + "\r\n" + ContentName + "," + ContentRecordGuid;
                                                                                    } else if (recordfound) {
                                                                                        //
                                                                                        // record found by name, use name is list but do not add guid
                                                                                        //
                                                                                        DataRecordList = DataRecordList + "\r\n" + ContentName + "," + ContentRecordName;
                                                                                    } else {
                                                                                        //
                                                                                        // record was created
                                                                                        //
                                                                                        cpCore.db.csSet(cs, "ccguid", ContentRecordGuid);
                                                                                        cpCore.db.csSet(cs, "name", ContentRecordName);
                                                                                        DataRecordList = DataRecordList + "\r\n" + ContentName + "," + ContentRecordGuid;
                                                                                    }
                                                                                }
                                                                                cpCore.db.csClose(ref cs);
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                break;
                                                        }
                                                    }
                                                    //
                                                    //-------------------------------------------------------------------------------
                                                    // ----- Pass 5, all other collection nodes
                                                    //
                                                    // Process all non-import <Collection> nodes
                                                    //-------------------------------------------------------------------------------
                                                    //
                                                    logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], pass 5");
                                                    foreach (XmlNode CDefSection in Doc.DocumentElement.ChildNodes) {
                                                        switch (genericController.vbLCase(CDefSection.Name)) {
                                                            case "cdef":
                                                            case "data":
                                                            case "help":
                                                            case "resource":
                                                            case "helplink":
                                                                //
                                                                // ignore - processed in previous passes
                                                                break;
                                                            case "getcollection":
                                                            case "importcollection":
                                                                //
                                                                // processed, but add rule for collection record
                                                                bool Found = false;
                                                                string ChildCollectionName = GetXMLAttribute(cpCore, Found, CDefSection, "name", "");
                                                                string ChildCollectionGUID = GetXMLAttribute(cpCore, Found, CDefSection, "guid", CDefSection.InnerText);
                                                                if (string.IsNullOrEmpty(ChildCollectionGUID)) {
                                                                    ChildCollectionGUID = CDefSection.InnerText;
                                                                }
                                                                if (!string.IsNullOrEmpty(ChildCollectionGUID)) {
                                                                    int ChildCollectionID = 0;
                                                                    int cs = -1;
                                                                    cs = cpCore.db.csOpen("Add-on Collections", "ccguid=" + cpCore.db.encodeSQLText(ChildCollectionGUID));
                                                                    if (cpCore.db.csOk(cs)) {
                                                                        ChildCollectionID = cpCore.db.csGetInteger(cs, "id");
                                                                    }
                                                                    cpCore.db.csClose(ref cs);
                                                                    if (ChildCollectionID != 0) {
                                                                        cs = cpCore.db.csInsertRecord("Add-on Collection Parent Rules", 0);
                                                                        if (cpCore.db.csOk(cs)) {
                                                                            cpCore.db.csSet(cs, "ParentID", collection.id);
                                                                            cpCore.db.csSet(cs, "ChildID", ChildCollectionID);
                                                                        }
                                                                        cpCore.db.csClose(ref cs);
                                                                    }
                                                                }
                                                                break;
                                                            case "scriptingmodule":
                                                            case "scriptingmodules":
                                                                result = false;
                                                                return_ErrorMessage = return_ErrorMessage + "<P>Collection includes a scripting module which is no longer supported. Move scripts to the code tab.</P>";
                                                                //    '
                                                                //    ' Scripting modules
                                                                //    '
                                                                //    ScriptingModuleID = 0
                                                                //    ScriptingName = GetXMLAttribute(cpcore,IsFound, CDefSection, "name", "No Name")
                                                                //    If ScriptingName = "" Then
                                                                //        ScriptingName = "No Name"
                                                                //    End If
                                                                //    ScriptingGuid = GetXMLAttribute(cpcore,IsFound, CDefSection, "guid", AOName)
                                                                //    If ScriptingGuid = "" Then
                                                                //        ScriptingGuid = ScriptingName
                                                                //    End If
                                                                //    Criteria = "(ccguid=" & cpCore.db.encodeSQLText(ScriptingGuid) & ")"
                                                                //    ScriptingModuleID = 0
                                                                //    CS = cpCore.db.cs_open("Scripting Modules", Criteria)
                                                                //    If cpCore.db.cs_ok(CS) Then
                                                                //        '
                                                                //        ' Update the Addon
                                                                //        '
                                                                //        Call logcontroller.appendInstallLog(cpCore, "UpgradeAppFromLocalCollection, GUID match with existing scripting module, Updating module [" & ScriptingName & "], Guid [" & ScriptingGuid & "]")
                                                                //    Else
                                                                //        '
                                                                //        ' not found by GUID - search name against name to update legacy Add-ons
                                                                //        '
                                                                //        Call cpCore.db.cs_Close(CS)
                                                                //        Criteria = "(name=" & cpCore.db.encodeSQLText(ScriptingName) & ")and(ccguid is null)"
                                                                //        CS = cpCore.db.cs_open("Scripting Modules", Criteria)
                                                                //        If cpCore.db.cs_ok(CS) Then
                                                                //            Call logcontroller.appendInstallLog(cpCore, "UpgradeAppFromLocalCollection, Scripting Module matched an existing Module that has no GUID, Updating to [" & ScriptingName & "], Guid [" & ScriptingGuid & "]")
                                                                //        End If
                                                                //    End If
                                                                //    If Not cpCore.db.cs_ok(CS) Then
                                                                //        '
                                                                //        ' not found by GUID or by name, Insert a new
                                                                //        '
                                                                //        Call cpCore.db.cs_Close(CS)
                                                                //        CS = cpCore.db.cs_insertRecord("Scripting Modules", 0)
                                                                //        If cpCore.db.cs_ok(CS) Then
                                                                //            Call logcontroller.appendInstallLog(cpCore, "UpgradeAppFromLocalCollection, Creating new Scripting Module [" & ScriptingName & "], Guid [" & ScriptingGuid & "]")
                                                                //        End If
                                                                //    End If
                                                                //    If Not cpCore.db.cs_ok(CS) Then
                                                                //        '
                                                                //        ' Could not create new
                                                                //        '
                                                                //        Call logcontroller.appendInstallLog(cpCore, "UpgradeAppFromLocalCollection, Scripting Module could not be created, skipping Scripting Module [" & ScriptingName & "], Guid [" & ScriptingGuid & "]")
                                                                //    Else
                                                                //        ScriptingModuleID = cpCore.db.cs_getInteger(CS, "ID")
                                                                //        Call cpCore.db.cs_set(CS, "code", CDefSection.InnerText)
                                                                //        Call cpCore.db.cs_set(CS, "name", ScriptingName)
                                                                //        Call cpCore.db.cs_set(CS, "ccguid", ScriptingGuid)
                                                                //    End If
                                                                //    Call cpCore.db.cs_Close(CS)
                                                                //    If ScriptingModuleID <> 0 Then
                                                                //        '
                                                                //        ' Add Add-on Collection Module Rule
                                                                //        '
                                                                //        CS = cpCore.db.cs_insertRecord("Add-on Collection Module Rules", 0)
                                                                //        If cpCore.db.cs_ok(CS) Then
                                                                //            Call cpCore.db.cs_set(CS, "Collectionid", CollectionID)
                                                                //            Call cpCore.db.cs_set(CS, "ScriptingModuleID", ScriptingModuleID)
                                                                //        End If
                                                                //        Call cpCore.db.cs_Close(CS)
                                                                //    End If
                                                                break;
                                                            case "sharedstyle":
                                                                result = false;
                                                                return_ErrorMessage = return_ErrorMessage + "<P>Collection includes a shared style which is no longer supported. Move styles to the default styles tab.</P>";

                                                                //    '
                                                                //    ' added 9/3/2012
                                                                //    ' Shared Style
                                                                //    '
                                                                //    sharedStyleId = 0
                                                                //    NodeName = GetXMLAttribute(cpcore,IsFound, CDefSection, "name", "No Name")
                                                                //    If NodeName = "" Then
                                                                //        NodeName = "No Name"
                                                                //    End If
                                                                //    nodeGuid = GetXMLAttribute(cpcore,IsFound, CDefSection, "guid", AOName)
                                                                //    If nodeGuid = "" Then
                                                                //        nodeGuid = NodeName
                                                                //    End If
                                                                //    Criteria = "(ccguid=" & cpCore.db.encodeSQLText(nodeGuid) & ")"
                                                                //    ScriptingModuleID = 0
                                                                //    CS = cpCore.db.cs_open("Shared Styles", Criteria)
                                                                //    If cpCore.db.cs_ok(CS) Then
                                                                //        '
                                                                //        ' Update the Addon
                                                                //        '
                                                                //        Call logcontroller.appendInstallLog(cpCore, "UpgradeAppFromLocalCollection, GUID match with existing shared style, Updating [" & NodeName & "], Guid [" & nodeGuid & "]")
                                                                //    Else
                                                                //        '
                                                                //        ' not found by GUID - search name against name to update legacy Add-ons
                                                                //        '
                                                                //        Call cpCore.db.cs_Close(CS)
                                                                //        Criteria = "(name=" & cpCore.db.encodeSQLText(NodeName) & ")and(ccguid is null)"
                                                                //        CS = cpCore.db.cs_open("shared styles", Criteria)
                                                                //        If cpCore.db.cs_ok(CS) Then
                                                                //            Call logcontroller.appendInstallLog(cpCore, "UpgradeAppFromLocalCollection, shared style matched an existing Module that has no GUID, Updating to [" & NodeName & "], Guid [" & nodeGuid & "]")
                                                                //        End If
                                                                //    End If
                                                                //    If Not cpCore.db.cs_ok(CS) Then
                                                                //        '
                                                                //        ' not found by GUID or by name, Insert a new
                                                                //        '
                                                                //        Call cpCore.db.cs_Close(CS)
                                                                //        CS = cpCore.db.cs_insertRecord("shared styles", 0)
                                                                //        If cpCore.db.cs_ok(CS) Then
                                                                //            Call logcontroller.appendInstallLog(cpCore, "UpgradeAppFromLocalCollection, Creating new shared style [" & NodeName & "], Guid [" & nodeGuid & "]")
                                                                //        End If
                                                                //    End If
                                                                //    If Not cpCore.db.cs_ok(CS) Then
                                                                //        '
                                                                //        ' Could not create new
                                                                //        '
                                                                //        Call logcontroller.appendInstallLog(cpCore, "UpgradeAppFromLocalCollection, shared style could not be created, skipping shared style [" & NodeName & "], Guid [" & nodeGuid & "]")
                                                                //    Else
                                                                //        sharedStyleId = cpCore.db.cs_getInteger(CS, "ID")
                                                                //        Call cpCore.db.cs_set(CS, "StyleFilename", CDefSection.InnerText)
                                                                //        Call cpCore.db.cs_set(CS, "name", NodeName)
                                                                //        Call cpCore.db.cs_set(CS, "ccguid", nodeGuid)
                                                                //        Call cpCore.db.cs_set(CS, "alwaysInclude", GetXMLAttribute(cpcore,IsFound, CDefSection, "alwaysinclude", "0"))
                                                                //        Call cpCore.db.cs_set(CS, "prefix", GetXMLAttribute(cpcore,IsFound, CDefSection, "prefix", ""))
                                                                //        Call cpCore.db.cs_set(CS, "suffix", GetXMLAttribute(cpcore,IsFound, CDefSection, "suffix", ""))
                                                                //        Call cpCore.db.cs_set(CS, "suffix", GetXMLAttribute(cpcore,IsFound, CDefSection, "suffix", ""))
                                                                //        Call cpCore.db.cs_set(CS, "sortOrder", GetXMLAttribute(cpcore,IsFound, CDefSection, "sortOrder", ""))
                                                                //    End If
                                                                //    Call cpCore.db.cs_Close(CS)
                                                                break;
                                                            case "addon":
                                                            case "add-on":
                                                                //
                                                                // Add-on Node, do part 1 of 2
                                                                //   (include add-on node must be done after all add-ons are installed)
                                                                //
                                                                InstallCollectionFromLocalRepo_addonNode_Phase1(cpCore, CDefSection, "ccguid", cpCore.siteProperties.dataBuildVersion, collection.id, ref result, ref return_ErrorMessage);
                                                                if (!result) {
                                                                    //result = result;
                                                                }
                                                                break;
                                                            case "interfaces":
                                                                //
                                                                // Legacy Interface Node
                                                                //
                                                                foreach (XmlNode CDefInterfaces in CDefSection.ChildNodes) {
                                                                    InstallCollectionFromLocalRepo_addonNode_Phase1(cpCore, CDefInterfaces, "ccguid", cpCore.siteProperties.dataBuildVersion, collection.id, ref result, ref return_ErrorMessage);
                                                                    if (!result) {
                                                                        //result = result;
                                                                    }
                                                                }
                                                                //Case "otherxml", "importcollection", "sqlindex", "style", "styles", "stylesheet", "adminmenu", "menuentry", "navigatorentry"
                                                                //    '
                                                                //    ' otherxml
                                                                //    '
                                                                //    If genericController.vbLCase(CDefSection.OuterXml) <> "<otherxml></otherxml>" Then
                                                                //        OtherXML = OtherXML & vbCrLf & CDefSection.OuterXml
                                                                //    End If
                                                                //    'Case Else
                                                                //    '    '
                                                                //    '    ' Unknown node in collection file
                                                                //    '    '
                                                                //    '    OtherXML = OtherXML & vbCrLf & CDefSection.OuterXml
                                                                //    '    Call logcontroller.appendInstallLog(cpCore, "Addon Collection for [" & Collectionname & "] contained an unknown node [" & CDefSection.Name & "]. This node will be ignored.")
                                                                break;
                                                        }
                                                    }
                                                    //
                                                    // --- end of pass
                                                    //
                                                    //-------------------------------------------------------------------------------
                                                    // ----- Pass 6
                                                    //
                                                    // process include add-on node of add-on nodes
                                                    //-------------------------------------------------------------------------------
                                                    //
                                                    logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], pass 6");
                                                    foreach (XmlNode collectionNode in Doc.DocumentElement.ChildNodes) {
                                                        switch (collectionNode.Name.ToLower()) {
                                                            case "addon":
                                                            case "add-on":
                                                                //
                                                                // Add-on Node, do part 1, verify the addon in the table with name and guid
                                                                string addonName = GetXMLAttribute(cpCore, IsFound, collectionNode, "name", collectionNode.Name);
                                                                logController.appendLogDebug(cpCore, "install addon [" + collectionNode.Name.ToLower() + "]");
                                                                if (addonName.ToLower()=="_oninstall") {
                                                                    onInstallAddonGuid = GetXMLAttribute(cpCore, IsFound, collectionNode, "guid", collectionNode.Name);
                                                                }
                                                                InstallCollectionFromLocalRepo_addonNode_Phase2(cpCore, collectionNode, "ccguid", cpCore.siteProperties.dataBuildVersion, collection.id, ref result, ref return_ErrorMessage);
                                                                break;
                                                            case "interfaces":
                                                                //
                                                                // Legacy Interface Node
                                                                //
                                                                foreach (XmlNode CDefInterfaces in collectionNode.ChildNodes) {
                                                                    InstallCollectionFromLocalRepo_addonNode_Phase2(cpCore, CDefInterfaces, "ccguid", cpCore.siteProperties.dataBuildVersion, collection.id, ref result, ref return_ErrorMessage);
                                                                    if (!result) {
                                                                        //result = result;
                                                                    }
                                                                }
                                                                break;
                                                        }
                                                    }
                                                    //
                                                    //-------------------------------------------------------------------------------
                                                    // ----- Pass 4, process fields in data nodes
                                                    //-------------------------------------------------------------------------------
                                                    //
                                                    logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], pass 4");
                                                    foreach (XmlNode CDefSection in Doc.DocumentElement.ChildNodes) {
                                                        switch (genericController.vbLCase(CDefSection.Name)) {
                                                            case "data":
                                                                foreach (XmlNode ContentNode in CDefSection.ChildNodes) {
                                                                    if (ContentNode.Name.ToLower() == "record") {
                                                                        string ContentName = GetXMLAttribute(cpCore, IsFound, ContentNode, "content", "");
                                                                        if (string.IsNullOrEmpty(ContentName)) {
                                                                            logController.appendLogInstall(cpCore, "install collection file contains a data.record node with a blank content attribute.");
                                                                            result = false;
                                                                            return_ErrorMessage = return_ErrorMessage + "<P>Collection file contains a data.record node with a blank content attribute.</P>";
                                                                        } else {
                                                                            string ContentRecordGuid = GetXMLAttribute(cpCore, IsFound, ContentNode, "guid", "");
                                                                            string ContentRecordName = GetXMLAttribute(cpCore, IsFound, ContentNode, "name", "");
                                                                            if ((!string.IsNullOrEmpty(ContentRecordGuid)) | (!string.IsNullOrEmpty(ContentRecordName))) {
                                                                                Models.Complex.cdefModel CDef = Models.Complex.cdefModel.getCdef(cpCore, ContentName);
                                                                                int cs = -1;
                                                                                if (!string.IsNullOrEmpty(ContentRecordGuid)) {
                                                                                    cs = cpCore.db.csOpen(ContentName, "ccguid=" + cpCore.db.encodeSQLText(ContentRecordGuid));
                                                                                } else {
                                                                                    cs = cpCore.db.csOpen(ContentName, "name=" + cpCore.db.encodeSQLText(ContentRecordName));
                                                                                }
                                                                                if (cpCore.db.csOk(cs)) {
                                                                                    //
                                                                                    // Update the record
                                                                                    foreach (XmlNode FieldNode in ContentNode.ChildNodes) {
                                                                                        if (FieldNode.Name.ToLower() == "field") {
                                                                                            bool IsFieldFound = false;
                                                                                            string FieldName = GetXMLAttribute(cpCore, IsFound, FieldNode, "name", "").ToLower();
                                                                                            int fieldTypeId = -1;
                                                                                            int FieldLookupContentID = -1;
                                                                                            foreach (var keyValuePair in CDef.fields) {
                                                                                                Models.Complex.cdefFieldModel field = keyValuePair.Value;
                                                                                                if (genericController.vbLCase(field.nameLc) == FieldName) {
                                                                                                    fieldTypeId = field.fieldTypeId;
                                                                                                    FieldLookupContentID = field.lookupContentID;
                                                                                                    IsFieldFound = true;
                                                                                                    break;
                                                                                                }
                                                                                            }
                                                                                            if (IsFieldFound) {
                                                                                                string FieldValue = FieldNode.InnerText;
                                                                                                switch (fieldTypeId) {
                                                                                                    case FieldTypeIdAutoIdIncrement:
                                                                                                    case FieldTypeIdRedirect: {
                                                                                                            //
                                                                                                            // not supported
                                                                                                            break;
                                                                                                        }
                                                                                                    case FieldTypeIdLookup: {
                                                                                                            //
                                                                                                            // read in text value, if a guid, use it, otherwise assume name
                                                                                                            if (FieldLookupContentID != 0) {
                                                                                                                string FieldLookupContentName = Models.Complex.cdefModel.getContentNameByID(cpCore, FieldLookupContentID);
                                                                                                                if (!string.IsNullOrEmpty(FieldLookupContentName)) {
                                                                                                                    if ((FieldValue.Left(1) == "{") && (FieldValue.Substring(FieldValue.Length - 1) == "}") && Models.Complex.cdefModel.isContentFieldSupported(cpCore, FieldLookupContentName, "ccguid")) {
                                                                                                                        //
                                                                                                                        // Lookup by guid
                                                                                                                        int fieldLookupId = genericController.encodeInteger(cpCore.db.GetRecordIDByGuid(FieldLookupContentName, FieldValue));
                                                                                                                        if (fieldLookupId <= 0) {
                                                                                                                            return_ErrorMessage = return_ErrorMessage + "<P>Warning: There was a problem translating field [" + FieldName + "] in record [" + ContentName + "] because the record it refers to was not found in this site.</P>";
                                                                                                                        } else {
                                                                                                                            cpCore.db.csSet(cs, FieldName, fieldLookupId);
                                                                                                                        }
                                                                                                                    } else {
                                                                                                                        //
                                                                                                                        // lookup by name
                                                                                                                        if (!string.IsNullOrEmpty(FieldValue)) {
                                                                                                                            int fieldLookupId = cpCore.db.getRecordID(FieldLookupContentName, FieldValue);
                                                                                                                            if (fieldLookupId <= 0) {
                                                                                                                                return_ErrorMessage = return_ErrorMessage + "<P>Warning: There was a problem translating field [" + FieldName + "] in record [" + ContentName + "] because the record it refers to was not found in this site.</P>";
                                                                                                                            } else {
                                                                                                                                cpCore.db.csSet(cs, FieldName, fieldLookupId);
                                                                                                                            }
                                                                                                                        }
                                                                                                                    }
                                                                                                                }
                                                                                                            } else if (FieldValue.IsNumeric()) {
                                                                                                                //
                                                                                                                // must be lookup list
                                                                                                                cpCore.db.csSet(cs, FieldName, FieldValue);
                                                                                                            }
                                                                                                            break;
                                                                                                        }
                                                                                                    default: {
                                                                                                            cpCore.db.csSet(cs, FieldName, FieldValue);
                                                                                                            break;
                                                                                                        }
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                                cpCore.db.csClose(ref cs);
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                break;
                                                        }
                                                    }
                                                    //
                                                    // --- end of pass
                                                    //
                                                }
                                                collection.DataRecordList = DataRecordList;
                                                collection.save(cpCore);
                                                //
                                                // -- execute onInstall addon if found
                                                if (!string.IsNullOrEmpty( onInstallAddonGuid )) {
                                                    var addon = Models.Entity.addonModel.create(cpCore, onInstallAddonGuid);
                                                    if ( addon != null) {
                                                        var executeContext = new BaseClasses.CPUtilsBaseClass.addonExecuteContext() {
                                                            addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextSimple
                                                        };
                                                        cpCore.addon.execute(addon, executeContext);
                                                    }
                                                }
                                            }
                                            //
                                            logController.appendLogInstall(cpCore, "install collection [" + Collectionname + "], upgrade complete, flush cache");
                                            //
                                            // -- import complete, flush caches
                                            cpCore.cache.invalidateAll();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                //
                // Log error and exit with failure. This way any other upgrading will still continue
                cpCore.handleException(ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        //
        private static void UpdateConfig(coreClass cpCore, string Collectionname, string CollectionGuid, DateTime CollectionUpdatedDate, string CollectionVersionFolderName) {
            try {
                //
                bool loadOK = true;
                string LocalFilename = null;
                string LocalGuid = null;
                XmlDocument Doc = new XmlDocument();
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode CollectionNode = null;
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode LocalListNode = null;
                XmlNode NewCollectionNode = null;
                XmlNode NewAttrNode = null;
                bool CollectionFound = false;
                //
                loadOK = true;
                try {
                    Doc.LoadXml(getLocalCollectionStoreListXml(cpCore));
                } catch (Exception ex) {
                    logController.appendLogInstall(cpCore, "UpdateConfig, Error loading Collections.xml file.");
                }
                if (loadOK) {
                    if (genericController.vbLCase(Doc.DocumentElement.Name) != genericController.vbLCase(CollectionListRootNode)) {
                        logController.appendLogInstall(cpCore, "UpdateConfig, The Collections.xml file has an invalid root node, [" + Doc.DocumentElement.Name + "] was received and [" + CollectionListRootNode + "] was expected.");
                    } else {
                        if (genericController.vbLCase(Doc.DocumentElement.Name) == "collectionlist") {
                            CollectionFound = false;
                            foreach (XmlNode LocalListNode in Doc.DocumentElement.ChildNodes) {
                                switch (genericController.vbLCase(LocalListNode.Name)) {
                                    case "collection":
                                        LocalGuid = "";
                                        foreach (XmlNode CollectionNode in LocalListNode.ChildNodes) {
                                            switch (genericController.vbLCase(CollectionNode.Name)) {
                                                case "guid":
                                                    //
                                                    LocalGuid = genericController.vbLCase(CollectionNode.InnerText);
                                                    //todo  WARNING: Exit statements not matching the immediately enclosing block are converted using a 'goto' statement:
                                                    //ORIGINAL LINE: Exit For
                                                    goto ExitLabel1;
                                            }
                                        }
                                        ExitLabel1:
                                        if (genericController.vbLCase(LocalGuid) == genericController.vbLCase(CollectionGuid)) {
                                            CollectionFound = true;
                                            foreach (XmlNode CollectionNode in LocalListNode.ChildNodes) {
                                                switch (genericController.vbLCase(CollectionNode.Name)) {
                                                    case "name":
                                                        CollectionNode.InnerText = Collectionname;
                                                        break;
                                                    case "lastchangedate":
                                                        CollectionNode.InnerText = CollectionUpdatedDate.ToString();
                                                        break;
                                                    case "path":
                                                        CollectionNode.InnerText = CollectionVersionFolderName;
                                                        break;
                                                }
                                            }
                                            //todo  WARNING: Exit statements not matching the immediately enclosing block are converted using a 'goto' statement:
                                            //ORIGINAL LINE: Exit For
                                            goto ExitLabel2;
                                        }
                                        break;
                                }
                            }
                            ExitLabel2:
                            if (!CollectionFound) {
                                NewCollectionNode = Doc.CreateNode(XmlNodeType.Element, "collection", "");
                                //
                                NewAttrNode = Doc.CreateNode(XmlNodeType.Element, "name", "");
                                NewAttrNode.InnerText = Collectionname;
                                NewCollectionNode.AppendChild(NewAttrNode);
                                //
                                NewAttrNode = Doc.CreateNode(XmlNodeType.Element, "lastchangedate", "");
                                NewAttrNode.InnerText = CollectionUpdatedDate.ToString();
                                NewCollectionNode.AppendChild(NewAttrNode);
                                //
                                NewAttrNode = Doc.CreateNode(XmlNodeType.Element, "guid", "");
                                NewAttrNode.InnerText = CollectionGuid;
                                NewCollectionNode.AppendChild(NewAttrNode);
                                //
                                NewAttrNode = Doc.CreateNode(XmlNodeType.Element, "path", "");
                                NewAttrNode.InnerText = CollectionVersionFolderName;
                                NewCollectionNode.AppendChild(NewAttrNode);
                                //
                                Doc.DocumentElement.AppendChild(NewCollectionNode);
                            }
                            //
                            // Save the result
                            //
                            LocalFilename = cpCore.addon.getPrivateFilesAddonPath() + "Collections.xml";
                            //LocalFilename = GetProgramPath & "\Addons\Collections.xml"
                            Doc.Save(cpCore.privateFiles.rootLocalPath + LocalFilename);
                        }
                    }
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        private static string GetCollectionPath(coreClass cpCore, string CollectionGuid) {
            string result = "";
            try {
                DateTime LastChangeDate = default(DateTime);
                string Collectionname = "";
                GetCollectionConfig(cpCore, CollectionGuid, ref result, ref LastChangeDate, ref Collectionname);
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return the collection path, lastChangeDate, and collectionName given the guid
        /// </summary>
        public static void GetCollectionConfig(coreClass cpCore, string CollectionGuid, ref string return_CollectionPath, ref DateTime return_LastChagnedate, ref string return_CollectionName) {
            try {
                string LocalPath = null;
                string LocalGuid = "";
                XmlDocument Doc = new XmlDocument();
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode CollectionNode = null;
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode LocalListNode = null;
                bool CollectionFound = false;
                string CollectionPath = "";
                DateTime LastChangeDate = default(DateTime);
                string hint = "";
                bool MatchFound = false;
                string LocalName = null;
                bool loadOK = false;
                //
                MatchFound = false;
                return_CollectionPath = "";
                return_LastChagnedate = DateTime.MinValue;
                loadOK = true;
                try {
                    Doc.LoadXml(getLocalCollectionStoreListXml(cpCore));
                } catch (Exception ex) {
                    //hint = hint & ",parse error"
                    logController.appendLogInstall(cpCore, "GetCollectionConfig, Hint=[" + hint + "], Error loading Collections.xml file.");
                    loadOK = false;
                }
                if (loadOK) {
                    if (genericController.vbLCase(Doc.DocumentElement.Name) != genericController.vbLCase(CollectionListRootNode)) {
                        logController.appendLogInstall(cpCore, "Hint=[" + hint + "], The Collections.xml file has an invalid root node");
                    } else {
                        if (true) {
                            //If genericController.vbLCase(.name) <> "collectionlist" Then
                            //    Call AppendClassLogFile("Server", "GetCollectionConfig", "Collections.xml file error, root node was not collectionlist, [" & .name & "].")
                            //Else
                            CollectionFound = false;
                            //hint = hint & ",checking nodes [" & .ChildNodes.Count & "]"
                            foreach (XmlNode LocalListNode in Doc.DocumentElement.ChildNodes) {
                                LocalName = "no name found";
                                LocalPath = "";
                                switch (genericController.vbLCase(LocalListNode.Name)) {
                                    case "collection":
                                        LocalGuid = "";
                                        foreach (XmlNode CollectionNode in LocalListNode.ChildNodes) {
                                            switch (genericController.vbLCase(CollectionNode.Name)) {
                                                case "name":
                                                    //
                                                    LocalName = genericController.vbLCase(CollectionNode.InnerText);
                                                    break;
                                                case "guid":
                                                    //
                                                    LocalGuid = genericController.vbLCase(CollectionNode.InnerText);
                                                    break;
                                                case "path":
                                                    //
                                                    CollectionPath = genericController.vbLCase(CollectionNode.InnerText);
                                                    break;
                                                case "lastchangedate":
                                                    LastChangeDate = genericController.encodeDate(CollectionNode.InnerText);
                                                    break;
                                            }
                                        }
                                        break;
                                }
                                //hint = hint & ",checking node [" & LocalName & "]"
                                if (genericController.vbLCase(CollectionGuid) == LocalGuid) {
                                    return_CollectionPath = CollectionPath;
                                    return_LastChagnedate = LastChangeDate;
                                    return_CollectionName = LocalName;
                                    //Call AppendClassLogFile("Server", "GetCollectionConfigArg", "GetCollectionConfig, match found, CollectionName=" & LocalName & ", CollectionPath=" & CollectionPath & ", LastChangeDate=" & LastChangeDate)
                                    MatchFound = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //======================================================================================================
        /// <summary>
        /// Installs Addons in a source folder
        /// </summary>
        public static bool InstallCollectionsFromPrivateFolder(coreClass cpCore, string privateFolder, ref string return_ErrorMessage, ref List<string> return_CollectionGUIDList, bool IsNewBuild, ref List<string> nonCriticalErrorList) {
            bool returnSuccess = false;
            try {
                DateTime CollectionLastChangeDate;
                //
                CollectionLastChangeDate = DateTime.Now;
                returnSuccess = buildLocalCollectionReposFromFolder(cpCore, privateFolder, CollectionLastChangeDate, ref return_CollectionGUIDList, ref return_ErrorMessage, false);
                if (!returnSuccess) {
                    //
                    // BuildLocal failed, log it and do not upgrade
                    //
                    logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder returned false with Error Message [" + return_ErrorMessage + "], exiting without calling UpgradeAllAppsFromLocalCollection");
                } else {
                    foreach (string collectionGuid in return_CollectionGUIDList) {
                        if (!installCollectionFromLocalRepo(cpCore, collectionGuid, cpCore.siteProperties.dataBuildVersion, ref return_ErrorMessage, "", IsNewBuild, ref nonCriticalErrorList)) {
                            logController.appendLogInstall(cpCore, "UpgradeAllAppsFromLocalCollection returned false with Error Message [" + return_ErrorMessage + "].");
                            break;
                        }
                    }
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                returnSuccess = false;
                if (string.IsNullOrEmpty(return_ErrorMessage)) {
                    return_ErrorMessage = "There was an unexpected error installing the collection, details [" + ex.Message + "]";
                }
            }
            return returnSuccess;
        }
        //
        //======================================================================================================
        /// <summary>
        /// Installs Addons in a source file
        /// </summary>
        public static bool InstallCollectionsFromPrivateFile(coreClass cpCore, string pathFilename, ref string return_ErrorMessage, ref string return_CollectionGUID, bool IsNewBuild, ref List<string> nonCriticalErrorList) {
            bool returnSuccess = false;
            try {
                DateTime CollectionLastChangeDate;
                //
                CollectionLastChangeDate = DateTime.Now;
                returnSuccess = buildLocalCollectionRepoFromFile(cpCore, pathFilename, CollectionLastChangeDate, ref return_CollectionGUID, ref return_ErrorMessage, false);
                if (!returnSuccess) {
                    //
                    // BuildLocal failed, log it and do not upgrade
                    //
                    logController.appendLogInstall(cpCore, "BuildLocalCollectionFolder returned false with Error Message [" + return_ErrorMessage + "], exiting without calling UpgradeAllAppsFromLocalCollection");
                } else {
                    returnSuccess = installCollectionFromLocalRepo(cpCore, return_CollectionGUID, cpCore.siteProperties.dataBuildVersion, ref return_ErrorMessage, "", IsNewBuild, ref nonCriticalErrorList);
                    if (!returnSuccess) {
                        //
                        // Upgrade all apps failed
                        //
                        logController.appendLogInstall(cpCore, "UpgradeAllAppsFromLocalCollection returned false with Error Message [" + return_ErrorMessage + "].");
                    } else {
                        returnSuccess = true;
                    }
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                returnSuccess = false;
                if (string.IsNullOrEmpty(return_ErrorMessage)) {
                    return_ErrorMessage = "There was an unexpected error installing the collection, details [" + ex.Message + "]";
                }
            }
            return returnSuccess;
        }
        //
        //======================================================================================================
        //
        private static int GetNavIDByGuid(coreClass cpCore, string ccGuid) {
            int navId = 0;
            try {
                int CS;
                //
                CS = cpCore.db.csOpen(cnNavigatorEntries, "ccguid=" + cpCore.db.encodeSQLText(ccGuid), "ID",true,0,false,false, "ID");
                if (cpCore.db.csOk(CS)) {
                    navId = cpCore.db.csGetInteger(CS, "id");
                }
                cpCore.db.csClose(ref CS);
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return navId;
        }
        //
        //======================================================================================================
        /// <summary>
        /// copy resources from install folder to www folder
        /// </summary>
        private static void copyInstallPathToDstPath(coreClass cpCore, string SrcPath, string DstPath, string BlockFileList, string BlockFolderList) {
            try {
                FileInfo[] FileInfoArray = null;
                DirectoryInfo[] FolderInfoArray = null;
                string SrcFolder = null;
                string DstFolder = null;
                //
                SrcFolder = SrcPath;
                if (SrcFolder.Substring(SrcFolder.Length - 1) == "\\") {
                    SrcFolder = SrcFolder.Left( SrcFolder.Length - 1);
                }
                //
                DstFolder = DstPath;
                if (DstFolder.Substring(DstFolder.Length - 1) == "\\") {
                    DstFolder = DstFolder.Left( DstFolder.Length - 1);
                }
                //
                if (cpCore.privateFiles.pathExists(SrcFolder)) {
                    FileInfoArray = cpCore.privateFiles.getFileList(SrcFolder);
                    foreach (FileInfo file in FileInfoArray) {
                        if ((file.Extension == "dll") || (file.Extension == "exe") || (file.Extension == "zip")) {
                            //
                            // can not copy dll or exe
                            //
                            //Filename = Filename
                        } else if (("," + BlockFileList + ",").IndexOf("," + file.Name + ",", System.StringComparison.OrdinalIgnoreCase)  != -1) {
                            //
                            // can not copy the current collection file
                            //
                            //file.Name = file.Name
                        } else {
                            //
                            // copy this file to destination
                            //
                            cpCore.privateFiles.copyFile(SrcPath + file.Name, DstPath + file.Name, cpCore.appRootFiles);
                        }
                    }
                    //
                    // copy folders to dst
                    //
                    FolderInfoArray = cpCore.privateFiles.getFolderList(SrcFolder);
                    foreach (DirectoryInfo folder in FolderInfoArray) {
                        if (("," + BlockFolderList + ",").IndexOf("," + folder.Name + ",", System.StringComparison.OrdinalIgnoreCase)  == -1) {
                            copyInstallPathToDstPath(cpCore, SrcPath + folder.Name + "\\", DstPath + folder.Name + "\\", BlockFileList, "");
                        }
                    }
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //======================================================================================================
        //
        private static string GetCollectionFileList(coreClass cpCore, string SrcPath, string SubFolder, string ExcludeFileList) {
            string result = "";
            try {
                FileInfo[] FileInfoArray = null;
                DirectoryInfo[] FolderInfoArray = null;
                string SrcFolder;
                //
                SrcFolder = SrcPath + SubFolder;
                if (SrcFolder.Substring(SrcFolder.Length - 1) == "\\") {
                    SrcFolder = SrcFolder.Left( SrcFolder.Length - 1);
                }
                //
                if (cpCore.privateFiles.pathExists(SrcFolder)) {
                    FileInfoArray = cpCore.privateFiles.getFileList(SrcFolder);
                    foreach (FileInfo file in FileInfoArray) {
                        if (("," + ExcludeFileList + ",").IndexOf("," + file.Name + ",", System.StringComparison.OrdinalIgnoreCase)  != -1) {
                            //
                            // can not copy the current collection file
                            //
                            //Filename = Filename
                        } else {
                            //
                            // copy this file to destination
                            //
                            result = result + "\r\n" + SubFolder + file.Name;
                            //runAtServer.IPAddress = "127.0.0.1"
                            //runAtServer.Port = "4531"
                            //QS = "SrcFile=" & encodeRequestVariable(SrcPath & Filename) & "&DstFile=" & encodeRequestVariable(DstPath & Filename)
                            //Call runAtServer.ExecuteCmd("CopyFile", QS)
                            //Call cpCore.app.privateFiles.CopyFile(SrcPath & Filename, DstPath & Filename)
                        }
                    }
                    //
                    // copy folders to dst
                    //
                    FolderInfoArray = cpCore.privateFiles.getFolderList(SrcFolder);
                    foreach (DirectoryInfo folder in FolderInfoArray) {
                        result = result + GetCollectionFileList(cpCore, SrcPath, SubFolder + folder.Name + "\\", ExcludeFileList);
                    }
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return result;
        }
        //
        //======================================================================================================
        //
        private static void InstallCollectionFromLocalRepo_addonNode_Phase1(coreClass cpCore, XmlNode AddonNode, string AddonGuidFieldName, string ignore_BuildVersion, int CollectionID, ref bool return_UpgradeOK, ref string return_ErrorMessage) {
            try {
                //
                int fieldTypeID = 0;
                string fieldType = null;
                string test = null;
                int TriggerContentID = 0;
                string ContentNameorGuid = null;
                int navTypeId = 0;
                int scriptinglanguageid = 0;
                string ScriptingCode = null;
                string FieldName = null;
                string NodeName = null;
                string NewValue = null;
                string NavIconTypeString = null;
                string menuNameSpace = null;
                string FieldValue = "";
                int CS2 = 0;
                string ScriptingNameorGuid = "";
                //  Dim ScriptingModuleID As Integer
                string ScriptingEntryPoint = null;
                int ScriptingTimeout = 0;
                string ScriptingLanguage = null;
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode ScriptingNode = null;
                XmlNode PageInterface = null;
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode TriggerNode = null;
                bool NavDeveloperOnly = false;
                string StyleSheet = null;
                string ArgumentList = null;
                int CS = 0;
                string Criteria = null;
                bool IsFound = false;
                string addonName = null;
                string addonGuid = null;
                string navTypeName = null;
                int addonId = 0;
                string Basename;
                //
                Basename = genericController.vbLCase(AddonNode.Name);
                if ((Basename == "page") || (Basename == "process") || (Basename == "addon") || (Basename == "add-on")) {
                    addonName = GetXMLAttribute(cpCore, IsFound, AddonNode, "name", "No Name");
                    if (string.IsNullOrEmpty(addonName)) {
                        addonName = "No Name";
                    }
                    addonGuid = GetXMLAttribute(cpCore, IsFound, AddonNode, "guid", addonName);
                    if (string.IsNullOrEmpty(addonGuid)) {
                        addonGuid = addonName;
                    }
                    navTypeName = GetXMLAttribute(cpCore, IsFound, AddonNode, "type", "");
                    navTypeId = GetListIndex(navTypeName, navTypeIDList);
                    if (navTypeId == 0) {
                        navTypeId = NavTypeIDAddon;
                    }
                    Criteria = "(" + AddonGuidFieldName + "=" + cpCore.db.encodeSQLText(addonGuid) + ")";
                    CS = cpCore.db.csOpen(cnAddons, Criteria,"", false);
                    if (cpCore.db.csOk(CS)) {
                        //
                        // Update the Addon
                        //
                        logController.appendLogInstall(cpCore, "UpgradeAppFromLocalCollection, GUID match with existing Add-on, Updating Add-on [" + addonName + "], Guid [" + addonGuid + "]");
                    } else {
                        //
                        // not found by GUID - search name against name to update legacy Add-ons
                        //
                        cpCore.db.csClose(ref CS);
                        Criteria = "(name=" + cpCore.db.encodeSQLText(addonName) + ")and(" + AddonGuidFieldName + " is null)";
                        CS = cpCore.db.csOpen(cnAddons, Criteria,"", false);
                        if (cpCore.db.csOk(CS)) {
                            logController.appendLogInstall(cpCore, "UpgradeAppFromLocalCollection, Add-on name matched an existing Add-on that has no GUID, Updating legacy Aggregate Function to Add-on [" + addonName + "], Guid [" + addonGuid + "]");
                        }
                    }
                    if (!cpCore.db.csOk(CS)) {
                        //
                        // not found by GUID or by name, Insert a new addon
                        //
                        cpCore.db.csClose(ref CS);
                        CS = cpCore.db.csInsertRecord(cnAddons, 0);
                        if (cpCore.db.csOk(CS)) {
                            logController.appendLogInstall(cpCore, "UpgradeAppFromLocalCollection, Creating new Add-on [" + addonName + "], Guid [" + addonGuid + "]");
                        }
                    }
                    if (!cpCore.db.csOk(CS)) {
                        //
                        // Could not create new Add-on
                        //
                        logController.appendLogInstall(cpCore, "UpgradeAppFromLocalCollection, Add-on could not be created, skipping Add-on [" + addonName + "], Guid [" + addonGuid + "]");
                    } else {
                        addonId = cpCore.db.csGetInteger(CS, "ID");
                        //
                        // Initialize the add-on
                        // Delete any existing related records - so if this is an update with removed relationships, those are removed
                        //
                        //Call cpCore.db.deleteContentRecords("Shared Styles Add-on Rules", "addonid=" & addonId)
                        //Call cpCore.db.deleteContentRecords("Add-on Scripting Module Rules", "addonid=" & addonId)
                        cpCore.db.deleteContentRecords("Add-on Include Rules", "addonid=" + addonId);
                        cpCore.db.deleteContentRecords("Add-on Content Trigger Rules", "addonid=" + addonId);
                        //
                        cpCore.db.csSet(CS, "collectionid", CollectionID);
                        cpCore.db.csSet(CS, AddonGuidFieldName, addonGuid);
                        cpCore.db.csSet(CS, "name", addonName);
                        cpCore.db.csSet(CS, "navTypeId", navTypeId);
                        ArgumentList = "";
                        StyleSheet = "";
                        NavDeveloperOnly = true;
                        if (AddonNode.ChildNodes.Count > 0) {
                            foreach (XmlNode PageInterfaceWithinLoop in AddonNode.ChildNodes) {
                                PageInterface = PageInterfaceWithinLoop;
                                switch (genericController.vbLCase(PageInterfaceWithinLoop.Name)) {
                                    case "activexdll":
                                        //
                                        // This is handled in BuildLocalCollectionFolder
                                        //
                                        break;
                                    case "editors":
                                        //
                                        // list of editors
                                        //
                                        foreach (XmlNode TriggerNode in PageInterfaceWithinLoop.ChildNodes) {
                                            switch (genericController.vbLCase(TriggerNode.Name)) {
                                                case "type":
                                                    fieldType = TriggerNode.InnerText;
                                                    fieldTypeID = cpCore.db.getRecordID("Content Field Types", fieldType);
                                                    if (fieldTypeID > 0) {
                                                        Criteria = "(addonid=" + addonId + ")and(contentfieldTypeID=" + fieldTypeID + ")";
                                                        CS2 = cpCore.db.csOpen("Add-on Content Field Type Rules", Criteria);
                                                        if (!cpCore.db.csOk(CS2)) {
                                                            cpCore.db.csClose(ref CS2);
                                                            CS2 = cpCore.db.csInsertRecord("Add-on Content Field Type Rules", 0);
                                                        }
                                                        if (cpCore.db.csOk(CS2)) {
                                                            cpCore.db.csSet(CS2, "addonid", addonId);
                                                            cpCore.db.csSet(CS2, "contentfieldTypeID", fieldTypeID);
                                                        }
                                                        cpCore.db.csClose(ref CS2);
                                                    }
                                                    break;
                                            }
                                        }
                                        break;
                                    case "processtriggers":
                                        //
                                        // list of events that trigger a process run for this addon
                                        //
                                        foreach (XmlNode TriggerNode in PageInterfaceWithinLoop.ChildNodes) {
                                            switch (genericController.vbLCase(TriggerNode.Name)) {
                                                case "contentchange":
                                                    TriggerContentID = 0;
                                                    ContentNameorGuid = TriggerNode.InnerText;
                                                    if (string.IsNullOrEmpty(ContentNameorGuid)) {
                                                        ContentNameorGuid = GetXMLAttribute(cpCore, IsFound, TriggerNode, "guid", "");
                                                        if (string.IsNullOrEmpty(ContentNameorGuid)) {
                                                            ContentNameorGuid = GetXMLAttribute(cpCore, IsFound, TriggerNode, "name", "");
                                                        }
                                                    }
                                                    Criteria = "(ccguid=" + cpCore.db.encodeSQLText(ContentNameorGuid) + ")";
                                                    CS2 = cpCore.db.csOpen("Content", Criteria);
                                                    if (!cpCore.db.csOk(CS2)) {
                                                        cpCore.db.csClose(ref CS2);
                                                        Criteria = "(ccguid is null)and(name=" + cpCore.db.encodeSQLText(ContentNameorGuid) + ")";
                                                        CS2 = cpCore.db.csOpen("content", Criteria);
                                                    }
                                                    if (cpCore.db.csOk(CS2)) {
                                                        TriggerContentID = cpCore.db.csGetInteger(CS2, "ID");
                                                    }
                                                    cpCore.db.csClose(ref CS2);
                                                    //If TriggerContentID = 0 Then
                                                    //    CS2 = cpCore.db.cs_insertRecord("Scripting Modules", 0)
                                                    //    If cpCore.db.cs_ok(CS2) Then
                                                    //        Call cpCore.db.cs_set(CS2, "name", ScriptingNameorGuid)
                                                    //        Call cpCore.db.cs_set(CS2, "ccguid", ScriptingNameorGuid)
                                                    //        TriggerContentID = cpCore.db.cs_getInteger(CS2, "ID")
                                                    //    End If
                                                    //    Call cpCore.db.cs_Close(CS2)
                                                    //End If
                                                    if (TriggerContentID == 0) {
                                                        //
                                                        // could not find the content
                                                        //
                                                    } else {
                                                        Criteria = "(addonid=" + addonId + ")and(contentid=" + TriggerContentID + ")";
                                                        CS2 = cpCore.db.csOpen("Add-on Content Trigger Rules", Criteria);
                                                        if (!cpCore.db.csOk(CS2)) {
                                                            cpCore.db.csClose(ref CS2);
                                                            CS2 = cpCore.db.csInsertRecord("Add-on Content Trigger Rules", 0);
                                                            if (cpCore.db.csOk(CS2)) {
                                                                cpCore.db.csSet(CS2, "addonid", addonId);
                                                                cpCore.db.csSet(CS2, "contentid", TriggerContentID);
                                                            }
                                                        }
                                                        cpCore.db.csClose(ref CS2);
                                                    }
                                                    break;
                                            }
                                        }
                                        break;
                                    case "scripting":
                                        //
                                        // include add-ons - NOTE - import collections must be run before interfaces
                                        // when importing a collectin that will be used for an include
                                        //
                                        ScriptingLanguage = GetXMLAttribute(cpCore, IsFound, PageInterfaceWithinLoop, "language", "");
                                        scriptinglanguageid = cpCore.db.getRecordID("scripting languages", ScriptingLanguage);
                                        cpCore.db.csSet(CS, "scriptinglanguageid", scriptinglanguageid);
                                        ScriptingEntryPoint = GetXMLAttribute(cpCore, IsFound, PageInterfaceWithinLoop, "entrypoint", "");
                                        cpCore.db.csSet(CS, "ScriptingEntryPoint", ScriptingEntryPoint);
                                        ScriptingTimeout = genericController.encodeInteger(GetXMLAttribute(cpCore, IsFound, PageInterfaceWithinLoop, "timeout", "5000"));
                                        cpCore.db.csSet(CS, "ScriptingTimeout", ScriptingTimeout);
                                        ScriptingCode = "";
                                        //Call cpCore.app.csv_SetCS(CS, "ScriptingCode", ScriptingCode)
                                        foreach (XmlNode ScriptingNode in PageInterfaceWithinLoop.ChildNodes) {
                                            switch (genericController.vbLCase(ScriptingNode.Name)) {
                                                case "code":
                                                    ScriptingCode = ScriptingCode + ScriptingNode.InnerText;
                                                    //Case "includemodule"

                                                    //    ScriptingModuleID = 0
                                                    //    ScriptingNameorGuid = ScriptingNode.InnerText
                                                    //    If ScriptingNameorGuid = "" Then
                                                    //        ScriptingNameorGuid = GetXMLAttribute(cpcore,IsFound, ScriptingNode, "guid", "")
                                                    //        If ScriptingNameorGuid = "" Then
                                                    //            ScriptingNameorGuid = GetXMLAttribute(cpcore,IsFound, ScriptingNode, "name", "")
                                                    //        End If
                                                    //    End If
                                                    //    Criteria = "(ccguid=" & cpCore.db.encodeSQLText(ScriptingNameorGuid) & ")"
                                                    //    CS2 = cpCore.db.cs_open("Scripting Modules", Criteria)
                                                    //    If Not cpCore.db.cs_ok(CS2) Then
                                                    //        Call cpCore.db.cs_Close(CS2)
                                                    //        Criteria = "(ccguid is null)and(name=" & cpCore.db.encodeSQLText(ScriptingNameorGuid) & ")"
                                                    //        CS2 = cpCore.db.cs_open("Scripting Modules", Criteria)
                                                    //    End If
                                                    //    If cpCore.db.cs_ok(CS2) Then
                                                    //        ScriptingModuleID = cpCore.db.cs_getInteger(CS2, "ID")
                                                    //    End If
                                                    //    Call cpCore.db.cs_Close(CS2)
                                                    //    If ScriptingModuleID = 0 Then
                                                    //        CS2 = cpCore.db.cs_insertRecord("Scripting Modules", 0)
                                                    //        If cpCore.db.cs_ok(CS2) Then
                                                    //            Call cpCore.db.cs_set(CS2, "name", ScriptingNameorGuid)
                                                    //            Call cpCore.db.cs_set(CS2, "ccguid", ScriptingNameorGuid)
                                                    //            ScriptingModuleID = cpCore.db.cs_getInteger(CS2, "ID")
                                                    //        End If
                                                    //        Call cpCore.db.cs_Close(CS2)
                                                    //    End If
                                                    //    Criteria = "(addonid=" & addonId & ")and(scriptingmoduleid=" & ScriptingModuleID & ")"
                                                    //    CS2 = cpCore.db.cs_open("Add-on Scripting Module Rules", Criteria)
                                                    //    If Not cpCore.db.cs_ok(CS2) Then
                                                    //        Call cpCore.db.cs_Close(CS2)
                                                    //        CS2 = cpCore.db.cs_insertRecord("Add-on Scripting Module Rules", 0)
                                                    //        If cpCore.db.cs_ok(CS2) Then
                                                    //            Call cpCore.db.cs_set(CS2, "addonid", addonId)
                                                    //            Call cpCore.db.cs_set(CS2, "scriptingmoduleid", ScriptingModuleID)
                                                    //        End If
                                                    //    End If
                                                    //    Call cpCore.db.cs_Close(CS2)
                                                    break;
                                            }
                                        }
                                        cpCore.db.csSet(CS, "ScriptingCode", ScriptingCode);
                                        break;
                                    case "activexprogramid":
                                        //
                                        // save program id
                                        //
                                        FieldValue = PageInterfaceWithinLoop.InnerText;
                                        cpCore.db.csSet(CS, "ObjectProgramID", FieldValue);
                                        break;
                                    case "navigator":
                                        //
                                        // create a navigator entry with a parent set to this
                                        //
                                        cpCore.db.csSave2(CS);
                                        menuNameSpace = GetXMLAttribute(cpCore, IsFound, PageInterfaceWithinLoop, "NameSpace", "");
                                        if (!string.IsNullOrEmpty(menuNameSpace)) {
                                            NavIconTypeString = GetXMLAttribute(cpCore, IsFound, PageInterfaceWithinLoop, "type", "");
                                            if (string.IsNullOrEmpty(NavIconTypeString)) {
                                                NavIconTypeString = "Addon";
                                            }
                                            //Dim builder As New coreBuilderClass(cpCore)
                                            appBuilderController.verifyNavigatorEntry(cpCore, "", menuNameSpace, addonName, "", "", "", false, false, false, true, addonName, NavIconTypeString, addonName, CollectionID);
                                        }
                                        break;
                                    case "argument":
                                    case "argumentlist":
                                        //
                                        // multiple argumentlist elements are concatinated with crlf
                                        //
                                        NewValue = encodeText(PageInterfaceWithinLoop.InnerText).Trim(' ');
                                        if (!string.IsNullOrEmpty(NewValue)) {
                                            if (string.IsNullOrEmpty(ArgumentList)) {
                                                ArgumentList = NewValue;
                                            } else if (NewValue != FieldValue) {
                                                ArgumentList = ArgumentList + "\r\n" + NewValue;
                                            }
                                        }
                                        break;
                                    case "style":
                                        //
                                        // import exclusive style
                                        //
                                        NodeName = GetXMLAttribute(cpCore, IsFound, PageInterfaceWithinLoop, "name", "");
                                        NewValue = encodeText(PageInterfaceWithinLoop.InnerText).Trim(' ');
                                        if (NewValue.Left( 1) != "{") {
                                            NewValue = "{" + NewValue;
                                        }
                                        if (NewValue.Substring(NewValue.Length - 1) != "}") {
                                            NewValue = NewValue + "}";
                                        }
                                        StyleSheet = StyleSheet + "\r\n" + NodeName + " " + NewValue;
                                        //Case "includesharedstyle"
                                        //    '
                                        //    ' added 9/3/2012
                                        //    '
                                        //    sharedStyleId = 0
                                        //    nodeNameOrGuid = GetXMLAttribute(cpcore,IsFound, PageInterface, "guid", "")
                                        //    If nodeNameOrGuid = "" Then
                                        //        nodeNameOrGuid = GetXMLAttribute(cpcore,IsFound, PageInterface, "name", "")
                                        //    End If
                                        //    Criteria = "(ccguid=" & cpCore.db.encodeSQLText(nodeNameOrGuid) & ")"
                                        //    CS2 = cpCore.db.cs_open("shared styles", Criteria)
                                        //    If Not cpCore.db.cs_ok(CS2) Then
                                        //        Call cpCore.db.cs_Close(CS2)
                                        //        Criteria = "(ccguid is null)and(name=" & cpCore.db.encodeSQLText(nodeNameOrGuid) & ")"
                                        //        CS2 = cpCore.db.cs_open("shared styles", Criteria)
                                        //    End If
                                        //    If cpCore.db.cs_ok(CS2) Then
                                        //        sharedStyleId = cpCore.db.cs_getInteger(CS2, "ID")
                                        //    End If
                                        //    Call cpCore.db.cs_Close(CS2)
                                        //    If sharedStyleId = 0 Then
                                        //        CS2 = cpCore.db.cs_insertRecord("shared styles", 0)
                                        //        If cpCore.db.cs_ok(CS2) Then
                                        //            Call cpCore.db.cs_set(CS2, "name", nodeNameOrGuid)
                                        //            Call cpCore.db.cs_set(CS2, "ccguid", nodeNameOrGuid)
                                        //            sharedStyleId = cpCore.db.cs_getInteger(CS2, "ID")
                                        //        End If
                                        //        Call cpCore.db.cs_Close(CS2)
                                        //    End If
                                        //    Criteria = "(addonid=" & addonId & ")and(StyleId=" & sharedStyleId & ")"
                                        //    CS2 = cpCore.db.cs_open("Shared Styles Add-on Rules", Criteria)
                                        //    If Not cpCore.db.cs_ok(CS2) Then
                                        //        Call cpCore.db.cs_Close(CS2)
                                        //        CS2 = cpCore.db.cs_insertRecord("Shared Styles Add-on Rules", 0)
                                        //        If cpCore.db.cs_ok(CS2) Then
                                        //            Call cpCore.db.cs_set(CS2, "addonid", addonId)
                                        //            Call cpCore.db.cs_set(CS2, "StyleId", sharedStyleId)
                                        //        End If
                                        //    End If
                                        //    Call cpCore.db.cs_Close(CS2)
                                        break;
                                    case "stylesheet":
                                    case "styles":
                                        //
                                        // import exclusive stylesheet if more then whitespace
                                        //
                                        test = PageInterfaceWithinLoop.InnerText;
                                        test = genericController.vbReplace(test, " ", "");
                                        test = genericController.vbReplace(test, "\r", "");
                                        test = genericController.vbReplace(test, "\n", "");
                                        test = genericController.vbReplace(test, "\t", "");
                                        if (!string.IsNullOrEmpty(test)) {
                                            StyleSheet = StyleSheet + "\r\n" + PageInterfaceWithinLoop.InnerText;
                                        }
                                        break;
                                    case "template":
                                    case "content":
                                    case "admin":
                                        //
                                        // these add-ons will be "non-developer only" in navigation
                                        //
                                        FieldName = PageInterfaceWithinLoop.Name;
                                        FieldValue = PageInterfaceWithinLoop.InnerText;
                                        if (!cpCore.db.cs_isFieldSupported(CS, FieldName)) {
                                            //
                                            // Bad field name - need to report it somehow
                                            //
                                        } else {
                                            cpCore.db.csSet(CS, FieldName, FieldValue);
                                            if (genericController.encodeBoolean(PageInterfaceWithinLoop.InnerText)) {
                                                //
                                                // if template, admin or content - let non-developers have navigator entry
                                                //
                                                NavDeveloperOnly = false;
                                            }
                                        }
                                        break;
                                    case "icon":
                                        //
                                        // icon
                                        //
                                        FieldValue = GetXMLAttribute(cpCore, IsFound, PageInterfaceWithinLoop, "link", "");
                                        if (!string.IsNullOrEmpty(FieldValue)) {
                                            //
                                            // Icons can be either in the root of the website or in content files
                                            //
                                            FieldValue = genericController.vbReplace(FieldValue, "\\", "/"); // make it a link, not a file
                                            if (genericController.vbInstr(1, FieldValue, "://") != 0) {
                                                //
                                                // the link is an absolute URL, leave it link this
                                                //
                                            } else {
                                                if (FieldValue.Left( 1) != "/") {
                                                    //
                                                    // make sure it starts with a slash to be consistance
                                                    //
                                                    FieldValue = "/" + FieldValue;
                                                }
                                                if (FieldValue.Left( 17) == "/contensivefiles/") {
                                                    //
                                                    // in content files, start link without the slash
                                                    //
                                                    FieldValue = FieldValue.Substring(17);
                                                }
                                            }
                                            cpCore.db.csSet(CS, "IconFilename", FieldValue);
                                            if (true) {
                                                cpCore.db.csSet(CS, "IconWidth", genericController.encodeInteger(GetXMLAttribute(cpCore, IsFound, PageInterfaceWithinLoop, "width", "0")));
                                                cpCore.db.csSet(CS, "IconHeight", genericController.encodeInteger(GetXMLAttribute(cpCore, IsFound, PageInterfaceWithinLoop, "height", "0")));
                                                cpCore.db.csSet(CS, "IconSprites", genericController.encodeInteger(GetXMLAttribute(cpCore, IsFound, PageInterfaceWithinLoop, "sprites", "0")));
                                            }
                                        }
                                        break;
                                    case "includeaddon":
                                    case "includeadd-on":
                                    case "include addon":
                                    case "include add-on":
                                        //
                                        // processed in phase2 of this routine, after all the add-ons are installed
                                        //
                                        break;
                                    case "form":
                                        //
                                        // The value of this node is the xml instructions to create a form. Take then
                                        //   entire node, children and all, and save them in the formxml field.
                                        //   this replaces the settings add-on type, and soo to be report add-on types as well.
                                        //   this removes the ccsettingpages and settingcollectionrules, etc.
                                        //
                                        if (true) {
                                            NavDeveloperOnly = false;
                                            cpCore.db.csSet(CS, "formxml", PageInterfaceWithinLoop.InnerXml);
                                        }
                                        break;
                                    case "javascript":
                                    case "javascriptinhead":
                                        //
                                        // these all translate to JSFilename
                                        //
                                        FieldName = "jsfilename";
                                        cpCore.db.csSet(CS, FieldName, PageInterfaceWithinLoop.InnerText);

                                        break;
                                    case "iniframe":
                                        //
                                        // typo - field is inframe
                                        //
                                        FieldName = "inframe";
                                        cpCore.db.csSet(CS, FieldName, PageInterfaceWithinLoop.InnerText);
                                        break;
                                    default:
                                        //
                                        // All the other fields should match the Db fields
                                        //
                                        FieldName = PageInterfaceWithinLoop.Name;
                                        FieldValue = PageInterfaceWithinLoop.InnerText;
                                        if (!cpCore.db.cs_isFieldSupported(CS, FieldName)) {
                                            //
                                            // Bad field name - need to report it somehow
                                            //
                                            cpCore.handleException(new ApplicationException("bad field found [" + FieldName + "], in addon node [" + addonName + "], of collection [" + cpCore.db.getRecordName("add-on collections", CollectionID) + "]"));
                                        } else {
                                            cpCore.db.csSet(CS, FieldName, FieldValue);
                                        }
                                        break;
                                }
                            }
                        }
                        cpCore.db.csSet(CS, "ArgumentList", ArgumentList);
                        cpCore.db.csSet(CS, "StylesFilename", StyleSheet);
                        // these are dynamic now
                        //            '
                        //            ' Setup special setting/tool/report Navigator Entry
                        //            '
                        //            If navTypeId = NavTypeIDTool Then
                        //                AddonNavID = GetNonRootNavigatorID(asv, AOName, GetNavIDByGuid(asv, "{801F1F07-20E6-4A5D-AF26-71007CCB834F}"), addonid, 0, NavIconTypeTool, AOName & " Add-on", NavDeveloperOnly, 0, "", 0, 0, CollectionID, navAdminOnly)
                        //            End If
                        //            If navTypeId = NavTypeIDReport Then
                        //                AddonNavID = GetNonRootNavigatorID(asv, AOName, GetNavIDByGuid(asv, "{2ED078A2-6417-46CB-8572-A13F64C4BF18}"), addonid, 0, NavIconTypeReport, AOName & " Add-on", NavDeveloperOnly, 0, "", 0, 0, CollectionID, navAdminOnly)
                        //            End If
                        //            If navTypeId = NavTypeIDSetting Then
                        //                AddonNavID = GetNonRootNavigatorID(asv, AOName, GetNavIDByGuid(asv, "{5FDDC758-4A15-4F98-8333-9CE8B8BFABC4}"), addonid, 0, NavIconTypeSetting, AOName & " Add-on", NavDeveloperOnly, 0, "", 0, 0, CollectionID, navAdminOnly)
                        //            End If
                    }
                    cpCore.db.csClose(ref CS);
                    //
                    // -- if this is needed, the installation xml files are available in the addon install folder. - I do not believe this is important
                    //       as if a collection is missing a dependancy, there is an error and you would expect to have to reinstall.
                    //
                    // Addon is now fully installed
                    // Go through all collection files on this site and see if there are
                    // any Dependencies on this add-on that need to be attached
                    // src args are those for the addon that includes the current addon
                    //   - if this addon is the target of another add-on's  "includeaddon" node
                    //
                    //Doc = New XmlDocument
                    //CS = cpCore.db.cs_open("Add-on Collections")
                    //Do While cpCore.db.cs_ok(CS)
                    //    CollectionFile = cpCore.db.cs_get(CS, "InstallFile")
                    //    If CollectionFile <> "" Then
                    //        Try
                    //            Call Doc.LoadXml(CollectionFile)
                    //            If Doc.DocumentElement.HasChildNodes Then
                    //                For Each TestObject In Doc.DocumentElement.ChildNodes
                    //                    '
                    //                    ' 20161002 - maybe this should be testing for an xmlElemetn, not node
                    //                    '
                    //                    If (TypeOf (TestObject) Is XmlElement) Then
                    //                        SrcMainNode = DirectCast(TestObject, XmlElement)
                    //                        If genericController.vbLCase(SrcMainNode.Name) = "addon" Then
                    //                            SrcAddonGuid = SrcMainNode.GetAttribute("guid")
                    //                            SrcAddonName = SrcMainNode.GetAttribute("name")
                    //                            If SrcMainNode.HasChildNodes Then
                    //                                '//On Error //Resume Next
                    //                                For Each TestObject2 In SrcMainNode.ChildNodes
                    //                                    'For Each SrcAddonNode In SrcMainNode.childNodes
                    //                                    If TypeOf TestObject2 Is XmlNode Then
                    //                                        SrcAddonNode = DirectCast(TestObject2, XmlElement)
                    //                                        If True Then
                    //                                            'If Err.Number <> 0 Then
                    //                                            '    ' this is to catch nodes that are not elements
                    //                                            '    Err.Clear
                    //                                            'Else
                    //                                            'On Error GoTo ErrorTrap
                    //                                            If genericController.vbLCase(SrcAddonNode.Name) = "includeaddon" Then
                    //                                                TestGuid = SrcAddonNode.GetAttribute("guid")
                    //                                                TestName = SrcAddonNode.GetAttribute("name")
                    //                                                Criteria = ""
                    //                                                If TestGuid <> "" Then
                    //                                                    If TestGuid = addonGuid Then
                    //                                                        Criteria = "(" & AddonGuidFieldName & "=" & cpCore.db.encodeSQLText(SrcAddonGuid) & ")"
                    //                                                    End If
                    //                                                ElseIf TestName <> "" Then
                    //                                                    If TestName = addonName Then
                    //                                                        Criteria = "(name=" & cpCore.db.encodeSQLText(SrcAddonName) & ")"
                    //                                                    End If
                    //                                                End If
                    //                                                If Criteria <> "" Then
                    //                                                    '$$$$$ cache this
                    //                                                    CS2 = cpCore.db.cs_open(cnAddons, Criteria, "ID")
                    //                                                    If cpCore.db.cs_ok(CS2) Then
                    //                                                        SrcAddonID = cpCore.db.cs_getInteger(CS2, "ID")
                    //                                                    End If
                    //                                                    Call cpCore.db.cs_Close(CS2)
                    //                                                    AddRule = False
                    //                                                    If SrcAddonID = 0 Then
                    //                                                        UserError = "The add-on being installed is referenced by another add-on in collection [], but this add-on could not be found by the respoective criteria [" & Criteria & "]"
                    //                                                        Call logcontroller.appendInstallLog(cpCore,  "UpgradeAddFromLocalCollection_InstallAddonNode, UserError [" & UserError & "]")
                    //                                                    Else
                    //                                                        CS2 = cpCore.db.cs_openCsSql_rev("default", "select ID from ccAddonIncludeRules where Addonid=" & SrcAddonID & " and IncludedAddonID=" & addonId)
                    //                                                        AddRule = Not cpCore.db.cs_ok(CS2)
                    //                                                        Call cpCore.db.cs_Close(CS2)
                    //                                                    End If
                    //                                                    If AddRule Then
                    //                                                        CS2 = cpCore.db.cs_insertRecord("Add-on Include Rules", 0)
                    //                                                        If cpCore.db.cs_ok(CS2) Then
                    //                                                            Call cpCore.db.cs_set(CS2, "Addonid", SrcAddonID)
                    //                                                            Call cpCore.db.cs_set(CS2, "IncludedAddonID", addonId)
                    //                                                        End If
                    //                                                        Call cpCore.db.cs_Close(CS2)
                    //                                                    End If
                    //                                                End If
                    //                                            End If
                    //                                        End If
                    //                                    End If
                    //                                Next
                    //                            End If
                    //                        End If
                    //                    Else
                    //                        CS = CS
                    //                    End If
                    //                Next
                    //            End If
                    //        Catch ex As Exception
                    //            cpCore.handleExceptionAndContinue(ex) : Throw
                    //        End Try
                    //    End If
                    //    Call cpCore.db.cs_goNext(CS)
                    //Loop
                    //Call cpCore.db.cs_Close(CS)
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //======================================================================================================
        /// <summary>
        /// process the include add-on node of the add-on nodes. 
        /// this is the second pass, so all add-ons should be added
        /// no errors for missing addones, except the include add-on case
        /// </summary>
        private static string InstallCollectionFromLocalRepo_addonNode_Phase2(coreClass cpCore, XmlNode AddonNode, string AddonGuidFieldName, string ignore_BuildVersion, int CollectionID, ref bool ReturnUpgradeOK, ref string ReturnErrorMessage) {
            string result = "";
            try {
                bool AddRule = false;
                string IncludeAddonName = null;
                string IncludeAddonGuid = null;
                int IncludeAddonID = 0;
                string UserError = null;
                int CS2 = 0;
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode PageInterface = null;
                bool NavDeveloperOnly = false;
                string StyleSheet = null;
                string ArgumentList = null;
                int CS = 0;
                string Criteria = null;
                bool IsFound = false;
                string AOName = null;
                string AOGuid = null;
                string AddOnType = null;
                int addonId = 0;
                string Basename;
                //
                Basename = genericController.vbLCase(AddonNode.Name);
                if ((Basename == "page") || (Basename == "process") || (Basename == "addon") || (Basename == "add-on")) {
                    AOName = GetXMLAttribute(cpCore, IsFound, AddonNode, "name", "No Name");
                    if (string.IsNullOrEmpty(AOName)) {
                        AOName = "No Name";
                    }
                    AOGuid = GetXMLAttribute(cpCore, IsFound, AddonNode, "guid", AOName);
                    if (string.IsNullOrEmpty(AOGuid)) {
                        AOGuid = AOName;
                    }
                    AddOnType = GetXMLAttribute(cpCore, IsFound, AddonNode, "type", "");
                    Criteria = "(" + AddonGuidFieldName + "=" + cpCore.db.encodeSQLText(AOGuid) + ")";
                    CS = cpCore.db.csOpen(cnAddons, Criteria,"", false);
                    if (cpCore.db.csOk(CS)) {
                        //
                        // Update the Addon
                        //
                        logController.appendLogInstall(cpCore, "UpgradeAppFromLocalCollection, GUID match with existing Add-on, Updating Add-on [" + AOName + "], Guid [" + AOGuid + "]");
                    } else {
                        //
                        // not found by GUID - search name against name to update legacy Add-ons
                        //
                        cpCore.db.csClose(ref CS);
                        Criteria = "(name=" + cpCore.db.encodeSQLText(AOName) + ")and(" + AddonGuidFieldName + " is null)";
                        CS = cpCore.db.csOpen(cnAddons, Criteria,"", false);
                    }
                    if (!cpCore.db.csOk(CS)) {
                        //
                        // Could not find add-on
                        //
                        logController.appendLogInstall(cpCore, "UpgradeAppFromLocalCollection, Add-on could not be created, skipping Add-on [" + AOName + "], Guid [" + AOGuid + "]");
                    } else {
                        addonId = cpCore.db.csGetInteger(CS, "ID");
                        ArgumentList = "";
                        StyleSheet = "";
                        NavDeveloperOnly = true;
                        if (AddonNode.ChildNodes.Count > 0) {
                            foreach (XmlNode PageInterface in AddonNode.ChildNodes) {
                                switch (genericController.vbLCase(PageInterface.Name)) {
                                    case "includeaddon":
                                    case "includeadd-on":
                                    case "include addon":
                                    case "include add-on":
                                        //
                                        // include add-ons - NOTE - import collections must be run before interfaces
                                        // when importing a collectin that will be used for an include
                                        //
                                        if (true) {
                                            IncludeAddonName = GetXMLAttribute(cpCore, IsFound, PageInterface, "name", "");
                                            IncludeAddonGuid = GetXMLAttribute(cpCore, IsFound, PageInterface, "guid", IncludeAddonName);
                                            IncludeAddonID = 0;
                                            Criteria = "";
                                            if (!string.IsNullOrEmpty(IncludeAddonGuid)) {
                                                Criteria = AddonGuidFieldName + "=" + cpCore.db.encodeSQLText(IncludeAddonGuid);
                                                if (string.IsNullOrEmpty(IncludeAddonName)) {
                                                    IncludeAddonName = "Add-on " + IncludeAddonGuid;
                                                }
                                            } else if (!string.IsNullOrEmpty(IncludeAddonName)) {
                                                Criteria = "(name=" + cpCore.db.encodeSQLText(IncludeAddonName) + ")";
                                            }
                                            if (!string.IsNullOrEmpty(Criteria)) {
                                                CS2 = cpCore.db.csOpen(cnAddons, Criteria);
                                                if (cpCore.db.csOk(CS2)) {
                                                    IncludeAddonID = cpCore.db.csGetInteger(CS2, "ID");
                                                }
                                                cpCore.db.csClose(ref CS2);
                                                AddRule = false;
                                                if (IncludeAddonID == 0) {
                                                    UserError = "The include add-on [" + IncludeAddonName + "] could not be added because it was not found. If it is in the collection being installed, it must appear before any add-ons that include it.";
                                                    logController.appendLogInstall(cpCore, "UpgradeAddFromLocalCollection_InstallAddonNode, UserError [" + UserError + "]");
                                                    ReturnUpgradeOK = false;
                                                    ReturnErrorMessage = ReturnErrorMessage + "<P>The collection was not installed because the add-on [" + AOName + "] requires an included add-on [" + IncludeAddonName + "] which could not be found. If it is in the collection being installed, it must appear before any add-ons that include it.</P>";
                                                } else {
                                                    CS2 = cpCore.db.csOpenSql_rev("default", "select ID from ccAddonIncludeRules where Addonid=" + addonId + " and IncludedAddonID=" + IncludeAddonID);
                                                    AddRule = !cpCore.db.csOk(CS2);
                                                    cpCore.db.csClose(ref CS2);
                                                }
                                                if (AddRule) {
                                                    CS2 = cpCore.db.csInsertRecord("Add-on Include Rules", 0);
                                                    if (cpCore.db.csOk(CS2)) {
                                                        cpCore.db.csSet(CS2, "Addonid", addonId);
                                                        cpCore.db.csSet(CS2, "IncludedAddonID", IncludeAddonID);
                                                    }
                                                    cpCore.db.csClose(ref CS2);
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    cpCore.db.csClose(ref CS);
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return result;
            //
        }
        //
        //======================================================================================================
        /// <summary>
        /// Import CDef on top of current configuration and the base configuration
        /// </summary>
        public static void installBaseCollection(coreClass cpCore, bool isNewBuild, ref List<string> nonCriticalErrorList) {
            try {
                //
                // -- new build
                // 20171029 -- upgrading should restore base collection fields as a fix to deleted required fields
                const string baseCollectionFilename = "aoBase5.xml";
                string baseCollectionXml = cpCore.programFiles.readFile(baseCollectionFilename);
                if (string.IsNullOrEmpty(baseCollectionXml)) {
                    //
                    // -- base collection notfound
                    throw new ApplicationException("Cannot load aoBase5.xml [" + cpCore.programFiles.rootLocalPath + "aoBase5.xml]");
                } else {
                    logController.appendLogInstall(cpCore, "Verify base collection -- new build");
                    miniCollectionModel baseCollection = installCollection_LoadXmlToMiniCollection(cpCore, baseCollectionXml, true, true, isNewBuild, new miniCollectionModel());
                    installCollection_BuildDbFromMiniCollection(cpCore, baseCollection, cpCore.siteProperties.dataBuildVersion, isNewBuild, ref nonCriticalErrorList);
                    //
                    // now treat as a regular collection and install - to pickup everything else 
                    string tmpFolderPath = "installBaseCollection" + genericController.GetRandomInteger(cpCore).ToString() + "\\";
                    cpCore.privateFiles.createPath(tmpFolderPath);
                    cpCore.programFiles.copyFile(baseCollectionFilename, tmpFolderPath + baseCollectionFilename, cpCore.privateFiles);
                    List<string> ignoreList = new List<string>();
                    string returnErrorMessage = "";
                    if (!InstallCollectionsFromPrivateFolder(cpCore, tmpFolderPath, ref returnErrorMessage, ref ignoreList, isNewBuild, ref nonCriticalErrorList)) {
                        throw new ApplicationException(returnErrorMessage);
                    }
                    cpCore.privateFiles.deleteFolder(tmpFolderPath);
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //======================================================================================================
        //
        public static void installCollectionFromLocalRepo_BuildDbFromXmlData(coreClass cpCore, string XMLText, bool isNewBuild, bool isBaseCollection, ref List<string> nonCriticalErrorList) {
            try {
                //
                logController.appendLogInstall(cpCore, "Application: " + cpCore.serverConfig.appConfig.name);
                //
                // ----- Import any CDef files, allowing for changes
                miniCollectionModel miniCollectionToAdd = new miniCollectionModel();
                miniCollectionModel miniCollectionWorking = installCollection_GetApplicationMiniCollection(cpCore, isNewBuild);
                miniCollectionToAdd = installCollection_LoadXmlToMiniCollection(cpCore, XMLText, isBaseCollection, false, isNewBuild, miniCollectionWorking);
                addMiniCollectionSrcToDst(cpCore, ref miniCollectionWorking, miniCollectionToAdd);
                installCollection_BuildDbFromMiniCollection(cpCore, miniCollectionWorking, cpCore.siteProperties.dataBuildVersion, isNewBuild, ref nonCriticalErrorList);
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //======================================================================================================
        /// <summary>
        /// create a collection class from a collection xml file, cdef are added to the cdefs in the application collection
        /// </summary>
        private static miniCollectionModel installCollection_LoadXmlToMiniCollection(coreClass cpcore, string srcCollecionXml, bool IsccBaseFile, bool setAllDataChanged, bool IsNewBuild, miniCollectionModel defaultCollection) {
            miniCollectionModel result = null;
            try {
                Models.Complex.cdefModel DefaultCDef = null;
                Models.Complex.cdefFieldModel DefaultCDefField = null;
                string contentNameLc = null;
                collectionXmlController XMLTools = new collectionXmlController(cpcore);
                //Dim AddonClass As New addonInstallClass(cpCore)
                string status = null;
                string CollectionGuid = null;
                string Collectionname = null;
                string ContentTableName = null;
                bool IsNavigator = false;
                string ActiveText = null;
                string Name = "";
                string MenuName = null;
                string IndexName = null;
                string TableName = null;
                int Ptr = 0;
                string FieldName = null;
                string ContentName = null;
                bool Found = false;
                string menuNameSpace = null;
                string MenuGuid = null;
               
                XmlNode CDef_Node = null;
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode CDefChildNode = null;
                string DataSourceName = null;
                XmlDocument srcXmlDom = new XmlDocument();
                string NodeName = null;
                //todo  NOTE: Commented this declaration since looping variables in 'foreach' loops are declared in the 'foreach' header in C#:
                //				XmlNode FieldChildNode = null;
                //
                logController.appendLogInstall(cpcore, "Application: " + cpcore.serverConfig.appConfig.name + ", UpgradeCDef_LoadDataToCollection");
                //
                result = new miniCollectionModel();
                //
                if (string.IsNullOrEmpty(srcCollecionXml)) {
                    //
                    // -- empty collection is an error
                    throw (new ApplicationException("UpgradeCDef_LoadDataToCollection, srcCollectionXml is blank or null"));
                } else {
                    try {
                        srcXmlDom.LoadXml(srcCollecionXml);
                    } catch (Exception ex) {
                        //
                        // -- xml load error
                        logController.appendLog(cpcore, "UpgradeCDef_LoadDataToCollection Error reading xml archive, ex=[" + ex.ToString() + "]");
                        throw new Exception("Error in UpgradeCDef_LoadDataToCollection, during doc.loadXml()", ex);
                    }
                    if ((srcXmlDom.DocumentElement.Name.ToLower() != CollectionFileRootNode) & (srcXmlDom.DocumentElement.Name.ToLower() != "contensivecdef")) {
                        //
                        // -- root node must be collection (or legacy contensivecdef)
                        cpcore.handleException(new ApplicationException("the archive file has a syntax error. Application name must be the first node."));
                    } else {
                        result.isBaseCollection = IsccBaseFile;
                        //
                        // Get Collection Name for logs
                        //
                        //hint = "get collection name"
                        Collectionname = GetXMLAttribute(cpcore, Found, srcXmlDom.DocumentElement, "name", "");
                        if (string.IsNullOrEmpty(Collectionname)) {
                            logController.appendLogInstall(cpcore, "UpgradeCDef_LoadDataToCollection, Application: " + cpcore.serverConfig.appConfig.name + ", Collection has no name");
                        } else {
                            //Call AppendClassLogFile(cpcore.app.config.name,"UpgradeCDef_LoadDataToCollection", "UpgradeCDef_LoadDataToCollection, Application: " & cpcore.app.appEnvironment.name & ", Collection: " & Collectionname)
                        }
                        result.name = Collectionname;
                        //
                        // Load possible DefaultSortMethods
                        //
                        //hint = "preload sort methods"
                        //SortMethodList = vbTab & "By Name" & vbTab & "By Alpha Sort Order Field" & vbTab & "By Date" & vbTab & "By Date Reverse"
                        //If cpCore.app.csv_IsContentFieldSupported("Sort Methods", "ID") Then
                        //    CS = cpCore.app.OpenCSContent("Sort Methods", , , , , , , "Name")
                        //    Do While cpCore.app.IsCSOK(CS)
                        //        SortMethodList = SortMethodList & vbTab & cpCore.app.cs_getText(CS, "name")
                        //        cpCore.app.nextCSRecord(CS)
                        //    Loop
                        //    Call cpCore.app.closeCS(CS)
                        //End If
                        //SortMethodList = SortMethodList & vbTab
                        //
                        foreach (XmlNode CDef_NodeWithinLoop in srcXmlDom.DocumentElement.ChildNodes) {
                            CDef_Node = CDef_NodeWithinLoop;
                            //isCdefTarget = False
                            NodeName = genericController.vbLCase(CDef_NodeWithinLoop.Name);
                            //hint = "read node " & NodeName
                            switch (NodeName) {
                                case "cdef":
                                    //
                                    // Content Definitions
                                    //
                                    ContentName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "name", "");
                                    contentNameLc = genericController.vbLCase(ContentName);
                                    if (string.IsNullOrEmpty(ContentName)) {
                                        throw (new ApplicationException("Unexpected exception")); //cpCore.handleLegacyError3(cpCore.serverConfig.appConfig.name, "collection file contains a CDEF node with no name attribute. This is not allowed.", "dll", "builderClass", "UpgradeCDef_LoadDataToCollection", 0, "", "", False, True, "")
                                    } else {
                                        //
                                        // setup a cdef from the application collection to use as a default for missing attributes (for inherited cdef)
                                        //
                                        if (defaultCollection.cdef.ContainsKey(contentNameLc)) {
                                            DefaultCDef = defaultCollection.cdef[contentNameLc];
                                        } else {
                                            DefaultCDef = new Models.Complex.cdefModel();
                                        }
                                        //
                                        ContentTableName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "ContentTableName", DefaultCDef.ContentTableName);
                                        if (!string.IsNullOrEmpty(ContentTableName)) {
                                            //
                                            // These two fields are needed to import the row
                                            //
                                            DataSourceName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "dataSource", DefaultCDef.ContentDataSourceName);
                                            if (string.IsNullOrEmpty(DataSourceName)) {
                                                DataSourceName = "Default";
                                            }
                                            //
                                            // ----- Add CDef if not already there
                                            //
                                            if (!result.cdef.ContainsKey(ContentName.ToLower())) {
                                                result.cdef.Add(ContentName.ToLower(), new Models.Complex.cdefModel());
                                            }
                                            //
                                            // Get CDef attributes
                                            //
                                            Models.Complex.cdefModel tempVar = result.cdef[ContentName.ToLower()];
                                            string activeDefaultText = "1";
                                            if (!(DefaultCDef.Active)) {
                                                activeDefaultText = "0";
                                            }
                                            ActiveText = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "Active", activeDefaultText);
                                            if (string.IsNullOrEmpty(ActiveText)) {
                                                ActiveText = "1";
                                            }
                                            tempVar.Active = genericController.encodeBoolean(ActiveText);
                                            tempVar.ActiveOnly = true;
                                            //.adminColumns = ?
                                            tempVar.AdminOnly = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "AdminOnly", DefaultCDef.AdminOnly);
                                            tempVar.AliasID = "id";
                                            tempVar.AliasName = "name";
                                            tempVar.AllowAdd = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "AllowAdd", DefaultCDef.AllowAdd);
                                            tempVar.AllowCalendarEvents = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "AllowCalendarEvents", DefaultCDef.AllowCalendarEvents);
                                            tempVar.AllowContentChildTool = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "AllowContentChildTool", DefaultCDef.AllowContentChildTool);
                                            tempVar.AllowContentTracking = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "AllowContentTracking", DefaultCDef.AllowContentTracking);
                                            tempVar.AllowDelete = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "AllowDelete", DefaultCDef.AllowDelete);
                                            tempVar.AllowTopicRules = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "AllowTopicRules", DefaultCDef.AllowTopicRules);
                                            tempVar.guid = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "guid", DefaultCDef.guid);
                                            tempVar.dataChanged = setAllDataChanged;
                                            tempVar.set_childIdList(cpcore, new List<int>());
                                            tempVar.ContentControlCriteria = "";
                                            tempVar.ContentDataSourceName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "ContentDataSourceName", DefaultCDef.ContentDataSourceName);
                                            tempVar.ContentTableName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "ContentTableName", DefaultCDef.ContentTableName);
                                            tempVar.dataSourceId = 0;
                                            tempVar.DefaultSortMethod = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "DefaultSortMethod", DefaultCDef.DefaultSortMethod);
                                            if ((tempVar.DefaultSortMethod == "") || (tempVar.DefaultSortMethod.ToLower() == "name")) {
                                                tempVar.DefaultSortMethod = "By Name";
                                            } else if (genericController.vbLCase(tempVar.DefaultSortMethod) == "sortorder") {
                                                tempVar.DefaultSortMethod = "By Alpha Sort Order Field";
                                            } else if (genericController.vbLCase(tempVar.DefaultSortMethod) == "date") {
                                                tempVar.DefaultSortMethod = "By Date";
                                            }
                                            tempVar.DeveloperOnly = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "DeveloperOnly", DefaultCDef.DeveloperOnly);
                                            tempVar.DropDownFieldList = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "DropDownFieldList", DefaultCDef.DropDownFieldList);
                                            tempVar.EditorGroupName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "EditorGroupName", DefaultCDef.EditorGroupName);
                                            tempVar.fields = new Dictionary<string, Models.Complex.cdefFieldModel>();
                                            tempVar.IconLink = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "IconLink", DefaultCDef.IconLink);
                                            tempVar.IconHeight = GetXMLAttributeInteger(cpcore, Found, CDef_NodeWithinLoop, "IconHeight", DefaultCDef.IconHeight);
                                            tempVar.IconWidth = GetXMLAttributeInteger(cpcore, Found, CDef_NodeWithinLoop, "IconWidth", DefaultCDef.IconWidth);
                                            tempVar.IconSprites = GetXMLAttributeInteger(cpcore, Found, CDef_NodeWithinLoop, "IconSprites", DefaultCDef.IconSprites);
                                            tempVar.IgnoreContentControl = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "IgnoreContentControl", DefaultCDef.IgnoreContentControl);
                                            tempVar.includesAFieldChange = false;
                                            tempVar.installedByCollectionGuid = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "installedByCollection", DefaultCDef.installedByCollectionGuid);
                                            tempVar.IsBaseContent = IsccBaseFile || GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "IsBaseContent", false);
                                            tempVar.IsModifiedSinceInstalled = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "IsModified", DefaultCDef.IsModifiedSinceInstalled);
                                            tempVar.Name = ContentName;
                                            tempVar.parentName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "Parent", DefaultCDef.parentName);
                                            tempVar.WhereClause = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "WhereClause", DefaultCDef.WhereClause);
                                            //
                                            // Get CDef field nodes
                                            //
                                            foreach (XmlNode CDefChildNode in CDef_NodeWithinLoop.ChildNodes) {
                                                //
                                                // ----- process CDef Field
                                                //
                                                if (TextMatch(CDefChildNode.Name, "field")) {
                                                    FieldName = GetXMLAttribute(cpcore, Found, CDefChildNode, "Name", "");
                                                    if (FieldName.ToLower() == "middlename") {
                                                        //FieldName = FieldName;
                                                    }
                                                    //
                                                    // try to find field in the defaultcdef
                                                    //
                                                    if (DefaultCDef.fields.ContainsKey(FieldName)) {
                                                        DefaultCDefField = DefaultCDef.fields[FieldName];
                                                    } else {
                                                        DefaultCDefField = new Models.Complex.cdefFieldModel();
                                                    }
                                                    //
                                                    if (!(result.cdef[ContentName.ToLower()].fields.ContainsKey(FieldName.ToLower()))) {
                                                        result.cdef[ContentName.ToLower()].fields.Add(FieldName.ToLower(), new Models.Complex.cdefFieldModel());
                                                    }
                                                    var cdefField = result.cdef[ContentName.ToLower()].fields[FieldName.ToLower()];
                                                    cdefField.nameLc = FieldName.ToLower();
                                                    ActiveText = "0";
                                                    if (DefaultCDefField.active) {
                                                        ActiveText = "1";
                                                    }
                                                    ActiveText = GetXMLAttribute(cpcore, Found, CDefChildNode, "Active", ActiveText);
                                                    if (string.IsNullOrEmpty(ActiveText)) {
                                                        ActiveText = "1";
                                                    }
                                                    cdefField.active = genericController.encodeBoolean(ActiveText);
                                                    //
                                                    // Convert Field Descriptor (text) to field type (integer)
                                                    //
                                                    string defaultFieldTypeName = cpcore.db.getFieldTypeNameFromFieldTypeId(DefaultCDefField.fieldTypeId);
                                                    string fieldTypeName = GetXMLAttribute(cpcore, Found, CDefChildNode, "FieldType", defaultFieldTypeName);
                                                    cdefField.fieldTypeId = cpcore.db.getFieldTypeIdFromFieldTypeName(fieldTypeName);
                                                    //FieldTypeDescriptor = GetXMLAttribute(cpcore,Found, CDefChildNode, "FieldType", DefaultCDefField.fieldType)
                                                    //If genericController.vbIsNumeric(FieldTypeDescriptor) Then
                                                    //    .fieldType = genericController.EncodeInteger(FieldTypeDescriptor)
                                                    //Else
                                                    //    .fieldType = cpCore.app.csv_GetFieldTypeByDescriptor(FieldTypeDescriptor)
                                                    //End If
                                                    //If .fieldType = 0 Then
                                                    //    .fieldType = FieldTypeText
                                                    //End If
                                                    cdefField.editSortPriority = GetXMLAttributeInteger(cpcore, Found, CDefChildNode, "EditSortPriority", DefaultCDefField.editSortPriority);
                                                    cdefField.authorable = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "Authorable", DefaultCDefField.authorable);
                                                    cdefField.caption = GetXMLAttribute(cpcore, Found, CDefChildNode, "Caption", DefaultCDefField.caption);
                                                    cdefField.defaultValue = GetXMLAttribute(cpcore, Found, CDefChildNode, "DefaultValue", DefaultCDefField.defaultValue);
                                                    cdefField.notEditable = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "NotEditable", DefaultCDefField.notEditable);
                                                    cdefField.indexColumn = GetXMLAttributeInteger(cpcore, Found, CDefChildNode, "IndexColumn", DefaultCDefField.indexColumn);
                                                    cdefField.indexWidth = GetXMLAttribute(cpcore, Found, CDefChildNode, "IndexWidth", DefaultCDefField.indexWidth);
                                                    cdefField.indexSortOrder = GetXMLAttributeInteger(cpcore, Found, CDefChildNode, "IndexSortOrder", DefaultCDefField.indexSortOrder);
                                                    cdefField.redirectID = GetXMLAttribute(cpcore, Found, CDefChildNode, "RedirectID", DefaultCDefField.redirectID);
                                                    cdefField.redirectPath = GetXMLAttribute(cpcore, Found, CDefChildNode, "RedirectPath", DefaultCDefField.redirectPath);
                                                    cdefField.htmlContent = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "HTMLContent", DefaultCDefField.htmlContent);
                                                    cdefField.uniqueName = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "UniqueName", DefaultCDefField.uniqueName);
                                                    cdefField.password = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "Password", DefaultCDefField.password);
                                                    cdefField.adminOnly = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "AdminOnly", DefaultCDefField.adminOnly);
                                                    cdefField.developerOnly = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "DeveloperOnly", DefaultCDefField.developerOnly);
                                                    cdefField.readOnly = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "ReadOnly", DefaultCDefField.readOnly);
                                                    cdefField.required = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "Required", DefaultCDefField.required);
                                                    cdefField.RSSTitleField = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "RSSTitle", DefaultCDefField.RSSTitleField);
                                                    cdefField.RSSDescriptionField = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "RSSDescriptionField", DefaultCDefField.RSSDescriptionField);
                                                    cdefField.memberSelectGroupName_set(cpcore, GetXMLAttribute(cpcore, Found, CDefChildNode, "MemberSelectGroup", ""));
                                                    cdefField.editTabName = GetXMLAttribute(cpcore, Found, CDefChildNode, "EditTab", DefaultCDefField.editTabName);
                                                    cdefField.Scramble = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "Scramble", DefaultCDefField.Scramble);
                                                    cdefField.lookupList = GetXMLAttribute(cpcore, Found, CDefChildNode, "LookupList", DefaultCDefField.lookupList);
                                                    cdefField.ManyToManyRulePrimaryField = GetXMLAttribute(cpcore, Found, CDefChildNode, "ManyToManyRulePrimaryField", DefaultCDefField.ManyToManyRulePrimaryField);
                                                    cdefField.ManyToManyRuleSecondaryField = GetXMLAttribute(cpcore, Found, CDefChildNode, "ManyToManyRuleSecondaryField", DefaultCDefField.ManyToManyRuleSecondaryField);
                                                    cdefField.set_lookupContentName(cpcore,GetXMLAttribute(cpcore, Found, CDefChildNode, "LookupContent", DefaultCDefField.get_lookupContentName(cpcore)));
                                                    // isbase should be set if the base file is loading, regardless of the state of any isBaseField attribute -- which will be removed later
                                                    // case 1 - when the application collection is loaded from the exported xml file, isbasefield must follow the export file although the data is not the base collection
                                                    // case 2 - when the base file is loaded, all fields must include the attribute
                                                    //Return_Collection.CDefExt(CDefPtr).Fields(FieldPtr).IsBaseField = IsccBaseFile
                                                    cdefField.isBaseField = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "IsBaseField", false) || IsccBaseFile;
                                                    cdefField.set_RedirectContentName(cpcore, GetXMLAttribute(cpcore, Found, CDefChildNode, "RedirectContent", DefaultCDefField.get_RedirectContentName(cpcore)));
                                                    cdefField.set_ManyToManyContentName(cpcore, GetXMLAttribute(cpcore, Found, CDefChildNode, "ManyToManyContent", DefaultCDefField.get_ManyToManyContentName(cpcore)));
                                                    cdefField.set_ManyToManyRuleContentName(cpcore, GetXMLAttribute(cpcore, Found, CDefChildNode, "ManyToManyRuleContent", DefaultCDefField.get_ManyToManyRuleContentName(cpcore)));
                                                    cdefField.isModifiedSinceInstalled = GetXMLAttributeBoolean(cpcore, Found, CDefChildNode, "IsModified", DefaultCDefField.isModifiedSinceInstalled);
                                                    cdefField.installedByCollectionGuid = GetXMLAttribute(cpcore, Found, CDefChildNode, "installedByCollectionId", DefaultCDefField.installedByCollectionGuid);
                                                    cdefField.dataChanged = setAllDataChanged;
                                                    //
                                                    // ----- handle child nodes (help node)
                                                    //
                                                    cdefField.HelpCustom = "";
                                                    cdefField.HelpDefault = "";
                                                    foreach (XmlNode FieldChildNode in CDefChildNode.ChildNodes) {
                                                        //
                                                        // ----- process CDef Field
                                                        //
                                                        if (TextMatch(FieldChildNode.Name, "HelpDefault")) {
                                                            cdefField.HelpDefault = FieldChildNode.InnerText;
                                                        }
                                                        if (TextMatch(FieldChildNode.Name, "HelpCustom")) {
                                                            cdefField.HelpCustom = FieldChildNode.InnerText;
                                                        }
                                                        cdefField.HelpChanged = setAllDataChanged;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case "sqlindex":
                                    //
                                    // SQL Indexes
                                    //
                                    IndexName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "indexname", "");
                                    TableName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "tableName", "");
                                    DataSourceName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "DataSourceName", "");
                                    if (string.IsNullOrEmpty(DataSourceName)) {
                                        DataSourceName = "default";
                                    }
                                    bool removeDup = false;
                                    miniCollectionModel.collectionSQLIndexModel dupToRemove = new miniCollectionModel.collectionSQLIndexModel();
                                    foreach (miniCollectionModel.collectionSQLIndexModel index in result.sqlIndexes) {
                                        if (TextMatch(index.IndexName, IndexName) & TextMatch(index.TableName, TableName) & TextMatch(index.DataSourceName, DataSourceName)) {
                                            dupToRemove = index;
                                            removeDup = true;
                                            break;
                                        }
                                    }
                                    if (removeDup) {
                                        result.sqlIndexes.Remove(dupToRemove);
                                    }
                                    miniCollectionModel.collectionSQLIndexModel newIndex = new miniCollectionModel.collectionSQLIndexModel();
                                    newIndex.IndexName = IndexName;
                                    newIndex.TableName = TableName;
                                    newIndex.DataSourceName = DataSourceName;
                                    newIndex.FieldNameList = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "FieldNameList", "");
                                    result.sqlIndexes.Add(newIndex);
                                    break;
                                case "adminmenu":
                                case "menuentry":
                                case "navigatorentry":
                                    //
                                    // Admin Menus / Navigator Entries
                                    MenuName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "Name", "");
                                    menuNameSpace = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "NameSpace", "");
                                    MenuGuid = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "guid", "");
                                    IsNavigator = (NodeName == "navigatorentry");
                                    string MenuKey = null;
                                    if (!IsNavigator) {
                                        MenuKey = genericController.vbLCase(MenuName);
                                    } else {
                                        MenuKey = MenuGuid;
                                    }
                                    if ( !result.menus.ContainsKey(MenuKey)) {
                                        ActiveText = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "Active", "1");
                                        if (string.IsNullOrEmpty(ActiveText)) {
                                            ActiveText = "1";
                                        }
                                        result.menus.Add(MenuKey, new miniCollectionModel.collectionMenuModel() {
                                            dataChanged = setAllDataChanged,
                                            Name = MenuName,
                                            Guid = MenuGuid,
                                            Key = MenuKey,
                                            Active = genericController.encodeBoolean(ActiveText),
                                            menuNameSpace = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "NameSpace", ""),
                                            ParentName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "ParentName", ""),
                                            ContentName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "ContentName", ""),
                                            LinkPage = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "LinkPage", ""),
                                            SortOrder = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "SortOrder", ""),
                                            AdminOnly = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "AdminOnly", false),
                                            DeveloperOnly = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "DeveloperOnly", false),
                                            NewWindow = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "NewWindow", false),
                                            AddonName = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "AddonName", ""),
                                            NavIconType = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "NavIconType", ""),
                                            NavIconTitle = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "NavIconTitle", ""),
                                            IsNavigator = IsNavigator
                                        });
                                    }
                                    break;
                                case "aggregatefunction":
                                case "addon":
                                    //
                                    // Aggregate Objects (just make them -- there are not too many
                                    //
                                    Name = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "Name", "");
                                    miniCollectionModel.collectionAddOnModel addon;
                                    if (result.addOns.ContainsKey(Name.ToLower())) {
                                        addon = result.addOns[Name.ToLower()];
                                    } else {
                                        addon = new miniCollectionModel.collectionAddOnModel();
                                        result.addOns.Add(Name.ToLower(), addon);
                                    }
                                    addon.dataChanged = setAllDataChanged;
                                    addon.Link = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "Link", "");
                                    addon.ObjectProgramID = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "ObjectProgramID", "");
                                    addon.ArgumentList = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "ArgumentList", "");
                                    addon.SortOrder = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "SortOrder", "");
                                    addon.Copy = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "copy", "");
                                    break;
                                case "style":
                                    //
                                    // style sheet entries
                                    //
                                    Name = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "Name", "");
                                    if (result.styleCnt > 0) {
                                        for (Ptr = 0; Ptr < result.styleCnt; Ptr++) {
                                            if (TextMatch(result.styles[Ptr].Name, Name)) {
                                                break;
                                            }
                                        }
                                    }
                                    if (Ptr >= result.styleCnt) {
                                        Ptr = result.styleCnt;
                                        result.styleCnt = result.styleCnt + 1;
                                        Array.Resize(ref result.styles, Ptr);
                                        result.styles[Ptr].Name = Name;
                                    }
                                    var tempVar5 = result.styles[Ptr];
                                    tempVar5.dataChanged = setAllDataChanged;
                                    tempVar5.Overwrite = GetXMLAttributeBoolean(cpcore, Found, CDef_NodeWithinLoop, "Overwrite", false);
                                    tempVar5.Copy = CDef_NodeWithinLoop.InnerText;
                                    break;
                                case "stylesheet":
                                    //
                                    // style sheet in one entry
                                    //
                                    result.styleSheet = CDef_NodeWithinLoop.InnerText;
                                    break;
                                case "getcollection":
                                case "importcollection":
                                    if (true) {
                                        //If Not UpgradeDbOnly Then
                                        //
                                        // Import collections are blocked from the BuildDatabase upgrade b/c the resulting Db must be portable
                                        //
                                        Collectionname = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "name", "");
                                        CollectionGuid = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "guid", "");
                                        if (string.IsNullOrEmpty(CollectionGuid)) {
                                            CollectionGuid = CDef_NodeWithinLoop.InnerText;
                                        }
                                        if (string.IsNullOrEmpty(CollectionGuid)) {
                                            status = "The collection you selected [" + Collectionname + "] can not be downloaded because it does not include a valid GUID.";
                                            //cpCore.AppendLog("builderClass.UpgradeCDef_LoadDataToCollection, UserError [" & status & "], The error was [" & Doc.ParseError.reason & "]")
                                        } else {
                                            result.collectionImports.Add(new miniCollectionModel.ImportCollectionType() {
                                                Name = Collectionname,
                                                Guid = CollectionGuid
                                            });
                                        }
                                    }
                                    break;
                                case "pagetemplate":
                                    //
                                    //-------------------------------------------------------------------------------------------------
                                    // Page Templates
                                    //-------------------------------------------------------------------------------------------------
                                    // *********************************************************************************
                                    // Page Template - started, but Return_Collection and LoadDataToCDef are all that is done do far
                                    //
                                    if (result.pageTemplateCnt > 0) {
                                        for (Ptr = 0; Ptr < result.pageTemplateCnt; Ptr++) {
                                            if (TextMatch(result.pageTemplates[Ptr].Name, Name)) {
                                                break;
                                            }
                                        }
                                    }
                                    if (Ptr >= result.pageTemplateCnt) {
                                        Ptr = result.pageTemplateCnt;
                                        result.pageTemplateCnt = result.pageTemplateCnt + 1;
                                        Array.Resize(ref result.pageTemplates, Ptr);
                                        result.pageTemplates[Ptr].Name = Name;
                                    }
                                    var tempVar6 = result.pageTemplates[Ptr];
                                    tempVar6.Copy = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "Copy", "");
                                    tempVar6.Guid = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "guid", "");
                                    tempVar6.Style = GetXMLAttribute(cpcore, Found, CDef_NodeWithinLoop, "style", "");
                                    //Case "sitesection"
                                    //    '
                                    //    '-------------------------------------------------------------------------------------------------
                                    //    ' Site Sections
                                    //    '-------------------------------------------------------------------------------------------------
                                    //    '
                                    //Case "dynamicmenu"
                                    //    '
                                    //    '-------------------------------------------------------------------------------------------------
                                    //    ' Dynamic Menus
                                    //    '-------------------------------------------------------------------------------------------------
                                    //    '
                                    break;
                                case "pagecontent":
                                    //
                                    //-------------------------------------------------------------------------------------------------
                                    // Page Content
                                    //-------------------------------------------------------------------------------------------------
                                    //
                                    break;
                                case "copycontent":
                                    //
                                    //-------------------------------------------------------------------------------------------------
                                    // Copy Content
                                    //-------------------------------------------------------------------------------------------------
                                    //
                                    break;
                            }
                        }
                        //hint = "nodes done"
                        //
                        // Convert Menus.ParentName to Menu.menuNameSpace
                        //
                        foreach ( var kvp in result.menus) {
                            miniCollectionModel.collectionMenuModel menu = kvp.Value;
                            if ( !string.IsNullOrEmpty( menu.ParentName )) {
                                menu.menuNameSpace = GetMenuNameSpace(cpcore, result.menus, menu, "");
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                cpcore.handleException(ex);
                throw;
            }
            return result;
        }
        //
        //======================================================================================================
        /// <summary>
        /// Verify ccContent and ccFields records from the cdef nodes of a a collection file. This is the last step of loading teh cdef nodes of a collection file. ParentId field is set based on ParentName node.
        /// </summary>
        private static void installCollection_BuildDbFromMiniCollection(coreClass cpCore, miniCollectionModel Collection, string BuildVersion, bool isNewBuild, ref List<string> nonCriticalErrorList) {
            try {
                //
                int FieldHelpID = 0;
                int FieldHelpCID = 0;
                int fieldId = 0;
                string FieldName = null;
                //Dim AddonClass As addonInstallClass
                string StyleSheetAdd = "";
                string NewStyleValue = null;
                string SiteStyles = null;
                int PosNameLineEnd = 0;
                int PosNameLineStart = 0;
                int SiteStylePtr = 0;
                string StyleLine = null;
                string[] SiteStyleSplit = { };
                int SiteStyleCnt = 0;
                string NewStyleName = null;
                string TestStyleName = null;
                string SQL = null;
                DataTable rs = null;
                string Copy = null;
                string ContentName = null;
                int NodeCount = 0;
                string TableName = null;
                bool RequireReload = false;
                bool Found = false;
                                                   //
                logController.appendLogInstall(cpCore, "Application: " + cpCore.serverConfig.appConfig.name + ", UpgradeCDef_BuildDbFromCollection");
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, stage 1: verify core sql tables");
                //----------------------------------------------------------------------------------------------------------------------
                //
                appBuilderController.VerifyBasicTables(cpCore);
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, stage 2: create SQL tables in default datasource");
                //----------------------------------------------------------------------------------------------------------------------
                //
                if (true) {
                    string UsedTables = "";
                    foreach (var keypairvalue in Collection.cdef) {
                        Models.Complex.cdefModel workingCdef = keypairvalue.Value;
                        ContentName = workingCdef.Name;
                        if (workingCdef.dataChanged) {
                            logController.appendLogInstall(cpCore, "creating sql table [" + workingCdef.ContentTableName + "], datasource [" + workingCdef.ContentDataSourceName + "]");
                            if (genericController.vbLCase(workingCdef.ContentDataSourceName) == "default" || workingCdef.ContentDataSourceName == "") {
                                TableName = workingCdef.ContentTableName;
                                if (genericController.vbInstr(1, "," + UsedTables + ",", "," + TableName + ",", 1) != 0) {
                                    //TableName = TableName;
                                } else {
                                    UsedTables = UsedTables + "," + TableName;
                                    cpCore.db.createSQLTable(workingCdef.ContentDataSourceName, TableName);
                                }
                            }
                        }
                    }
                    cpCore.doc.clearMetaData();
                    cpCore.cache.invalidateAll();
                }
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, stage 3: Verify all CDef names in ccContent so GetContentID calls will succeed");
                //----------------------------------------------------------------------------------------------------------------------
                //
                NodeCount = 0;
                List<string> installedContentList = new List<string>();
                rs = cpCore.db.executeQuery("SELECT Name from ccContent where (active<>0)");
                if (isDataTableOk(rs)) {
                    installedContentList = new List<string>(convertDataTableColumntoItemList(rs));
                }
                rs.Dispose();
                //
                foreach (var keypairvalue in Collection.cdef) {
                    if (keypairvalue.Value.dataChanged) {
                        logController.appendLogInstall(cpCore, "adding cdef name [" + keypairvalue.Value.Name + "]");
                        if (!installedContentList.Contains(keypairvalue.Value.Name.ToLower())) {
                            SQL = "Insert into ccContent (name,ccguid,active,createkey)values(" + cpCore.db.encodeSQLText(keypairvalue.Value.Name) + "," + cpCore.db.encodeSQLText(keypairvalue.Value.guid) + ",1,0);";
                            cpCore.db.executeQuery(SQL);
                            installedContentList.Add(keypairvalue.Value.Name.ToLower());
                            RequireReload = true;
                        }
                    }
                }
                cpCore.doc.clearMetaData();
                cpCore.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, stage 4: Verify content records required for Content Server");
                //----------------------------------------------------------------------------------------------------------------------
                //
                VerifySortMethods(cpCore);
                VerifyContentFieldTypes(cpCore);
                cpCore.doc.clearMetaData();
                cpCore.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, stage 5: verify 'Content' content definition");
                //----------------------------------------------------------------------------------------------------------------------
                //
                foreach (var keypairvalue in Collection.cdef) {
                    if (keypairvalue.Value.Name.ToLower() == "content") {
                        installCollection_BuildDbFromCollection_AddCDefToDb(cpCore, keypairvalue.Value, BuildVersion);
                        RequireReload = true;
                        break;
                    }
                }
                cpCore.doc.clearMetaData();
                cpCore.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, stage 6: Verify all definitions and fields");
                //----------------------------------------------------------------------------------------------------------------------
                //
                RequireReload = false;
                foreach (var keypairvalue in Collection.cdef) {
                    //
                    // todo tmp fix, changes to field caption in base.xml do not set fieldChange
                    if (true) // If .dataChanged Or .includesAFieldChange Then
                    {
                        if (keypairvalue.Value.Name.ToLower() != "content") {
                            installCollection_BuildDbFromCollection_AddCDefToDb(cpCore, keypairvalue.Value, BuildVersion);
                            RequireReload = true;
                        }
                    }
                }
                cpCore.doc.clearMetaData();
                cpCore.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, stage 7: Verify all field help");
                //----------------------------------------------------------------------------------------------------------------------
                //
                FieldHelpCID = cpCore.db.getRecordID("content", "Content Field Help");
                foreach (var keypairvalue in Collection.cdef) {
                    Models.Complex.cdefModel workingCdef = keypairvalue.Value;
                    ContentName = workingCdef.Name;
                    foreach (var fieldKeyValuePair in workingCdef.fields) {
                        Models.Complex.cdefFieldModel field = fieldKeyValuePair.Value;
                        FieldName = field.nameLc;
                        var tempVar = Collection.cdef[ContentName.ToLower()].fields[FieldName.ToLower()];
                        if (tempVar.HelpChanged) {
                            fieldId = 0;
                            SQL = "select f.id from ccfields f left join cccontent c on c.id=f.contentid where (f.name=" + cpCore.db.encodeSQLText(FieldName) + ")and(c.name=" + cpCore.db.encodeSQLText(ContentName) + ") order by f.id";
                            rs = cpCore.db.executeQuery(SQL);
                            if (isDataTableOk(rs)) {
                                fieldId = genericController.encodeInteger(cpCore.db.getDataRowColumnName(rs.Rows[0], "id"));
                            }
                            rs.Dispose();
                            if (fieldId == 0) {
                                throw (new ApplicationException("Unexpected exception")); //cpCore.handleLegacyError3(cpCore.serverConfig.appConfig.name, "Can not update help field for content [" & ContentName & "], field [" & FieldName & "] because the field was not found in the Db.", "dll", "builderClass", "UpgradeCDef_BuildDbFromCollection", 0, "", "", False, True, "")
                            } else {
                                SQL = "select id from ccfieldhelp where fieldid=" + fieldId + " order by id";
                                rs = cpCore.db.executeQuery(SQL);
                                if (isDataTableOk(rs)) {
                                    FieldHelpID = genericController.encodeInteger(rs.Rows[0]["id"]);
                                } else {
                                    FieldHelpID = cpCore.db.insertTableRecordGetId("default", "ccfieldhelp", 0);
                                }
                                rs.Dispose();
                                if (FieldHelpID != 0) {
                                    Copy = tempVar.HelpCustom;
                                    if (string.IsNullOrEmpty(Copy)) {
                                        Copy = tempVar.HelpDefault;
                                        if (!string.IsNullOrEmpty(Copy)) {
                                            //Copy = Copy;
                                        }
                                    }
                                    SQL = "update ccfieldhelp set active=1,contentcontrolid=" + FieldHelpCID + ",fieldid=" + fieldId + ",helpdefault=" + cpCore.db.encodeSQLText(Copy) + " where id=" + FieldHelpID;
                                    cpCore.db.executeQuery(SQL);
                                }
                            }
                        }
                    }
                }
                cpCore.doc.clearMetaData();
                cpCore.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, stage 8: create SQL indexes");
                //----------------------------------------------------------------------------------------------------------------------
                //
                foreach (miniCollectionModel.collectionSQLIndexModel index in Collection.sqlIndexes) {
                    if (index.dataChanged) {
                        logController.appendLogInstall(cpCore, "creating index [" + index.IndexName + "], fields [" + index.FieldNameList + "], on table [" + index.TableName + "]");
                        cpCore.db.createSQLIndex(index.DataSourceName, index.TableName, index.IndexName, index.FieldNameList);
                    }
                }
                cpCore.doc.clearMetaData();
                cpCore.cache.invalidateAll();
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, stage 9: Verify All Menu Names, then all Menus");
                //----------------------------------------------------------------------------------------------------------------------
                //
                foreach (var kvp in Collection.menus) {
                    var menu = kvp.Value;
                    if (menu.dataChanged) {
                        logController.appendLogInstall(cpCore, "creating navigator entry [" + menu.Name + "], namespace [" + menu.menuNameSpace + "], guid [" + menu.Guid + "]");
                        appBuilderController.verifyNavigatorEntry(cpCore, menu.Guid, menu.menuNameSpace, menu.Name, menu.ContentName, menu.LinkPage, menu.SortOrder, menu.AdminOnly, menu.DeveloperOnly, menu.NewWindow, menu.Active, menu.AddonName, menu.NavIconType, menu.NavIconTitle, 0);
                    }
                }
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, Upgrade collections added during upgrade process");
                //----------------------------------------------------------------------------------------------------------------------
                //
                logController.appendLogInstall(cpCore, "Installing Add-on Collections gathered during upgrade");
                foreach( var import in Collection.collectionImports) {
                    string CollectionPath = "";
                    DateTime lastChangeDate = new DateTime();
                    string emptyString = "";
                    GetCollectionConfig(cpCore, import.Guid , ref CollectionPath, ref lastChangeDate, ref emptyString);
                    string Guid = "";
                    string errorMessage = "";
                    if (!string.IsNullOrEmpty(CollectionPath)) {
                        //
                        // This collection is installed locally, install from local collections
                        //
                        installCollectionFromLocalRepo(cpCore, Guid, cpCore.codeVersion(), ref errorMessage, "", isNewBuild, ref nonCriticalErrorList);
                    } else {
                        //
                        // This is a new collection, install to the server and force it on this site
                        //
                        bool addonInstallOk = installCollectionFromRemoteRepo(cpCore, Guid, ref errorMessage, "", isNewBuild, ref nonCriticalErrorList);
                        if (!addonInstallOk) {
                            throw (new ApplicationException("Failure to install addon collection from remote repository. Collection [" + Guid + "] was referenced in collection [" + Collection.name + "]")); //cpCore.handleLegacyError3(cpCore.serverConfig.appConfig.name, "Error upgrading Addon Collection [" & Guid & "], " & errorMessage, "dll", "builderClass", "Upgrade2", 0, "", "", False, True, "")
                        }
                    }
                }
                //
                //----------------------------------------------------------------------------------------------------------------------
                logController.appendLogInstall(cpCore, "CDef Load, stage 9: Verify Styles");
                //----------------------------------------------------------------------------------------------------------------------
                //
                NodeCount = 0;
                if (Collection.styleCnt > 0) {
                    SiteStyles = cpCore.cdnFiles.readFile("templates/styles.css");
                    if (!string.IsNullOrEmpty(SiteStyles.Trim(' '))) {
                        //
                        // Split with an extra character at the end to guarantee there is an extra split at the end
                        //
                        SiteStyleSplit = (SiteStyles + " ").Split('}');
                        SiteStyleCnt = SiteStyleSplit.GetUpperBound(0) + 1;
                    }
                    for (var Ptr = 0; Ptr < Collection.styleCnt; Ptr++) {
                        Found = false;
                        var tempVar4 = Collection.styles[Ptr];
                        if (tempVar4.dataChanged) {
                            NewStyleName = tempVar4.Name;
                            NewStyleValue = tempVar4.Copy;
                            NewStyleValue = genericController.vbReplace(NewStyleValue, "}", "");
                            NewStyleValue = genericController.vbReplace(NewStyleValue, "{", "");
                            if (SiteStyleCnt > 0) {
                                for (SiteStylePtr = 0; SiteStylePtr < SiteStyleCnt; SiteStylePtr++) {
                                    StyleLine = SiteStyleSplit[SiteStylePtr];
                                    PosNameLineEnd = StyleLine.LastIndexOf("{") + 1;
                                    if (PosNameLineEnd > 0) {
                                        PosNameLineStart = StyleLine.LastIndexOf("\r\n", PosNameLineEnd - 1) + 1;
                                        if (PosNameLineStart > 0) {
                                            //
                                            // Check this site style for a match with the NewStyleName
                                            //
                                            PosNameLineStart = PosNameLineStart + 2;
                                            TestStyleName = (StyleLine.Substring(PosNameLineStart - 1, PosNameLineEnd - PosNameLineStart)).Trim(' ');
                                            if (genericController.vbLCase(TestStyleName) == genericController.vbLCase(NewStyleName)) {
                                                Found = true;
                                                if (tempVar4.Overwrite) {
                                                    //
                                                    // Found - Update style
                                                    //
                                                    SiteStyleSplit[SiteStylePtr] = "\r\n" + tempVar4.Name + " {" + NewStyleValue;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            //
                            // Add or update the stylesheet
                            //
                            if (!Found) {
                                StyleSheetAdd = StyleSheetAdd + "\r\n" + NewStyleName + " {" + NewStyleValue + "}";
                            }
                        }
                    }
                    SiteStyles = string.Join("}", SiteStyleSplit);
                    if (!string.IsNullOrEmpty(StyleSheetAdd)) {
                        SiteStyles = SiteStyles 
                            + "\r\n\r\n/*"
                            + "\r\nStyles added " + DateTime.Now + "\r\n*/"
                            + "\r\n" + StyleSheetAdd;
                    }
                    cpCore.appRootFiles.saveFile("templates/styles.css", SiteStyles);
                    //
                    // -- Update stylesheet cache
                    cpCore.siteProperties.setProperty("StylesheetSerialNumber", "-1");
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Update a table from a collection cdef node
        /// </summary>
        private static void installCollection_BuildDbFromCollection_AddCDefToDb(coreClass cpCore, Models.Complex.cdefModel cdef, string BuildVersion) {
            try {
                //
                logController.appendLogInstall(cpCore, "Update db cdef [" + cdef.Name + "]");
                //
                int ContentID = 0;
                bool ContentIsBaseContent = false;
                int FieldHelpCID = Models.Complex.cdefModel.getContentId(cpCore, "Content Field Help");
                var tmpList = new List<string> { };
                Contensive.Core.Models.Entity.dataSourceModel datasource = dataSourceModel.createByName(cpCore, cdef.ContentDataSourceName, ref tmpList);
                {
                    //
                    // -- get contentid and protect content with IsBaseContent true
                    string SQL = cpCore.db.GetSQLSelect("default", "ccContent", "ID,IsBaseContent", "name=" + cpCore.db.encodeSQLText(cdef.Name), "ID", "", 1);
                    DataTable dt = cpCore.db.executeQuery(SQL);
                    if (isDataTableOk(dt)) {
                        if (dt.Rows.Count > 0) {
                            ContentID = genericController.encodeInteger(cpCore.db.getDataRowColumnName(dt.Rows[0], "ID"));
                            ContentIsBaseContent = genericController.encodeBoolean(cpCore.db.getDataRowColumnName(dt.Rows[0], "IsBaseContent"));
                        }
                    }
                    dt.Dispose();
                }
                //
                // -- Update Content Record
                if (cdef.dataChanged) {
                    //
                    // -- Content needs to be updated
                    if (ContentIsBaseContent && !cdef.IsBaseContent) {
                        //
                        // -- Can not update a base content with a non-base content
                        cpCore.handleException(new ApplicationException("Warning: An attempt was made to update Content Definition [" + cdef.Name + "] from base to non-base. This should only happen when a base cdef is removed from the base collection. The update was ignored."));
                        cdef.IsBaseContent = ContentIsBaseContent;
                    }
                    //
                    // -- update definition (use SingleRecord as an update flag)
                    Models.Complex.cdefModel.addContent(cpCore, true, datasource, cdef.ContentTableName, cdef.Name, cdef.AdminOnly, cdef.DeveloperOnly, cdef.AllowAdd, cdef.AllowDelete, cdef.parentName, cdef.DefaultSortMethod, cdef.DropDownFieldList, false, cdef.AllowCalendarEvents, cdef.AllowContentTracking, cdef.AllowTopicRules, cdef.AllowContentChildTool, false, cdef.IconLink, cdef.IconWidth, cdef.IconHeight, cdef.IconSprites, cdef.guid, cdef.IsBaseContent, cdef.installedByCollectionGuid);
                    if (ContentID == 0) {
                        logController.appendLogInstall(cpCore, "Could not determine contentid after createcontent3 for [" + cdef.Name + "], upgrade for this cdef aborted.");
                    } else {
                        //
                        // -- Other fields not in the csv call
                        int EditorGroupID = 0;
                        if (cdef.EditorGroupName != "") {
                            DataTable dt = cpCore.db.executeQuery("select ID from ccGroups where name=" + cpCore.db.encodeSQLText(cdef.EditorGroupName));
                            if (isDataTableOk(dt)) {
                                if (dt.Rows.Count > 0) {
                                    EditorGroupID = genericController.encodeInteger(cpCore.db.getDataRowColumnName(dt.Rows[0], "ID"));
                                }
                            }
                            dt.Dispose();
                        }
                        string SQL = "update ccContent"
                            + " set EditorGroupID=" + EditorGroupID + ",isbasecontent=" + cpCore.db.encodeSQLBoolean(cdef.IsBaseContent) + " where id=" + ContentID + "";
                        cpCore.db.executeQuery(SQL);
                    }
                }
                //
                // -- update Content Field Records and Content Field Help records
                if (ContentID == 0 && (cdef.fields.Count > 0)) {
                    //
                    // -- cannot add fields if there is no content record
                    throw (new ApplicationException("Unexpected exception"));
                } else {
                    foreach (var nameValuePair in cdef.fields) {
                        Models.Complex.cdefFieldModel field = nameValuePair.Value;
                        int fieldId = 0;
                        if (field.dataChanged) {
                            fieldId = Models.Complex.cdefModel.verifyCDefField_ReturnID(cpCore, cdef.Name, field);
                        }
                        //
                        // -- update content field help records
                        if (field.HelpChanged) {
                            int FieldHelpID = 0;
                            DataTable dt = cpCore.db.executeQuery("select ID from ccFieldHelp where fieldid=" + fieldId);
                            if (isDataTableOk(dt)) {
                                if (dt.Rows.Count > 0) {
                                    FieldHelpID = genericController.encodeInteger(cpCore.db.getDataRowColumnName(dt.Rows[0], "ID"));
                                }
                            }
                            dt.Dispose();
                            //
                            if (FieldHelpID == 0) {
                                FieldHelpID = cpCore.db.insertTableRecordGetId("default", "ccFieldHelp", 0);
                            }
                            if (FieldHelpID != 0) {
                                string SQL = "update ccfieldhelp"
                                    + " set fieldid=" + fieldId + ",active=1"
                                    + ",contentcontrolid=" + FieldHelpCID + ",helpdefault=" + cpCore.db.encodeSQLText(field.HelpDefault) + ",helpcustom=" + cpCore.db.encodeSQLText(field.HelpCustom) + " where id=" + FieldHelpID;
                                cpCore.db.executeQuery(SQL);
                            }
                        }
                    }
                    //
                    // clear the cdef cache and list
                    cpCore.doc.clearMetaData();
                    cpCore.cache.invalidateAll();
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return the collectionList file stored in the root of the addon folder.
        /// </summary>
        /// <returns></returns>
        public static string getLocalCollectionStoreListXml(coreClass cpCore) {
            string returnXml = "";
            try {
                string LastChangeDate = "";
                DirectoryInfo SubFolder = null;
                DirectoryInfo[] SubFolderList = null;
                string FolderName = null;
                string collectionFilePathFilename = null;
                string CollectionGuid = null;
                string Collectionname = null;
                int Pos = 0;
                DirectoryInfo[] FolderList = null;
                //
                collectionFilePathFilename = cpCore.addon.getPrivateFilesAddonPath() + "Collections.xml";
                returnXml = cpCore.privateFiles.readFile(collectionFilePathFilename);
                if (string.IsNullOrEmpty(returnXml)) {
                    FolderList = cpCore.privateFiles.getFolderList(cpCore.addon.getPrivateFilesAddonPath());
                    if (FolderList.Length > 0) {
                        foreach (DirectoryInfo folder in FolderList) {
                            FolderName = folder.Name;
                            Pos = genericController.vbInstr(1, FolderName, "\t");
                            if (Pos > 1) {
                                //hint = hint & ",800"
                                FolderName = FolderName.Left(Pos - 1);
                                if (FolderName.Length > 34) {
                                    if (genericController.vbLCase(FolderName.Left(4)) != "temp") {
                                        CollectionGuid = FolderName.Substring(FolderName.Length - 32);
                                        Collectionname = FolderName.Left(FolderName.Length - CollectionGuid.Length - 1);
                                        CollectionGuid = CollectionGuid.Left(8) + "-" + CollectionGuid.Substring(8, 4) + "-" + CollectionGuid.Substring(12, 4) + "-" + CollectionGuid.Substring(16, 4) + "-" + CollectionGuid.Substring(20);
                                        CollectionGuid = "{" + CollectionGuid + "}";
                                        SubFolderList = cpCore.privateFiles.getFolderList(cpCore.addon.getPrivateFilesAddonPath() + "\\" + FolderName);
                                        if (SubFolderList.Length > 0) {
                                            SubFolder = SubFolderList[SubFolderList.Length - 1];
                                            FolderName = FolderName + "\\" + SubFolder.Name;
                                            LastChangeDate = SubFolder.Name.Substring(4, 2) + "/" + SubFolder.Name.Substring(6, 2) + "/" + SubFolder.Name.Left(4);
                                            if (!dateController.IsDate(LastChangeDate)) {
                                                LastChangeDate = "";
                                            }
                                        }
                                        returnXml = returnXml + "\r\n\t<Collection>";
                                        returnXml = returnXml + "\r\n\t\t<name>" + Collectionname + "</name>";
                                        returnXml = returnXml + "\r\n\t\t<guid>" + CollectionGuid + "</guid>";
                                        returnXml = returnXml + "\r\n\t\t<lastchangedate>" + LastChangeDate + "</lastchangedate>";
                                        returnXml += "\r\n\t\t<path>" + FolderName + "</path>";
                                        returnXml = returnXml + "\r\n\t</Collection>";
                                    }
                                }
                            }
                        }
                    }
                    returnXml = "<CollectionList>" + returnXml + "\r\n</CollectionList>";
                    cpCore.privateFiles.saveFile(collectionFilePathFilename, returnXml);
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return returnXml;
        }
        //
        //====================================================================================================
        /// <summary>
        /// get a list of collections available on the server
        /// </summary>
        public static bool getLocalCollectionStoreList(coreClass cpCore, ref List<collectionStoreClass> localCollectionStoreList, ref string return_ErrorMessage) {
            bool returnOk = true;
            try {
                //
                //-----------------------------------------------------------------------------------------------
                //   Load LocalCollections from the Collections.xml file
                //-----------------------------------------------------------------------------------------------
                //
                string localCollectionStoreListXml = getLocalCollectionStoreListXml(cpCore);
                if (!string.IsNullOrEmpty(localCollectionStoreListXml)) {
                    XmlDocument LocalCollections = new XmlDocument();
                    try {
                        LocalCollections.LoadXml(localCollectionStoreListXml);
                    } catch (Exception) {
                        string Copy = "Error loading privateFiles\\addons\\Collections.xml";
                        logController.appendLogInstall(cpCore, Copy);
                        return_ErrorMessage = return_ErrorMessage + "<P>" + Copy + "</P>";
                        returnOk = false;
                    }
                    if (returnOk) {
                        if (genericController.vbLCase(LocalCollections.DocumentElement.Name) != genericController.vbLCase(CollectionListRootNode)) {
                            string Copy = "The addons\\Collections.xml has an invalid root node, [" + LocalCollections.DocumentElement.Name + "] was received and [" + CollectionListRootNode + "] was expected.";
                            logController.appendLogInstall(cpCore, Copy);
                            return_ErrorMessage = return_ErrorMessage + "<P>" + Copy + "</P>";
                            returnOk = false;
                        } else {
                            //
                            // Get a list of the collection guids on this server
                            //
                            if (genericController.vbLCase(LocalCollections.DocumentElement.Name) == "collectionlist") {
                                foreach (XmlNode LocalListNode in LocalCollections.DocumentElement.ChildNodes) {
                                    switch (genericController.vbLCase(LocalListNode.Name)) {
                                        case "collection":
                                            var collection = new collectionStoreClass();
                                            localCollectionStoreList.Add(collection);
                                            foreach (XmlNode CollectionNode in LocalListNode.ChildNodes) {
                                                if (CollectionNode.Name.ToLower() == "name") {
                                                    collection.name = CollectionNode.InnerText;
                                                } else if (CollectionNode.Name.ToLower() == "guid") {
                                                    collection.guid = CollectionNode.InnerText;
                                                } else if (CollectionNode.Name.ToLower() == "path") {
                                                    collection.path = CollectionNode.InnerText;
                                                } else if (CollectionNode.Name.ToLower() == "lastchangedate") {
                                                    collection.lastChangeDate = genericController.encodeDate( CollectionNode.InnerText );
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return returnOk;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a list of collections on the 
        /// </summary>
        public static bool getRemoteCollectionStoreList(coreClass cpCore, ref List<collectionStoreClass> remoteCollectionStoreList) {
            bool result = false;
            try {
                var LibCollections = new XmlDocument();
                bool parseError = false;
                try {
                    LibCollections.Load("http://support.contensive.com/GetCollectionList?iv=" + cpCore.codeVersion());
                } catch (Exception ex) {
                    string UserError = "There was an error reading the Collection Library. The site may be unavailable.";
                    logController.appendLogInstall(cpCore, UserError);
                    errorController.addUserError(cpCore, UserError);
                    parseError = true;
                }
                if (!parseError) {
                    if (genericController.vbLCase(LibCollections.DocumentElement.Name) != genericController.vbLCase(CollectionListRootNode)) {
                        string UserError = "There was an error reading the Collection Library file. The '" + CollectionListRootNode + "' element was not found.";
                        logController.appendLogInstall(cpCore, UserError);
                        errorController.addUserError(cpCore, UserError);
                    } else {
                        foreach (XmlNode CDef_Node in LibCollections.DocumentElement.ChildNodes) {
                            var collection = new collectionStoreClass();
                            remoteCollectionStoreList.Add(collection);
                            switch (genericController.vbLCase(CDef_Node.Name)) {
                                case "collection":
                                    //
                                    // Read the collection
                                    //
                                    foreach (XmlNode CollectionNode in CDef_Node.ChildNodes) {
                                        switch (genericController.vbLCase(CollectionNode.Name)) {
                                            case "name":
                                                collection.name = CollectionNode.InnerText;
                                                break;
                                            case "guid":
                                                collection.guid = CollectionNode.InnerText;
                                                break;
                                            case "version":
                                                collection.version = CollectionNode.InnerText;
                                                break;
                                            case "description":
                                                collection.description = CollectionNode.InnerText;
                                                break;
                                            case "contensiveversion":
                                                collection.contensiveVersion = CollectionNode.InnerText;
                                                break;
                                            case "lastchangedate":
                                                collection.lastChangeDate = genericController.encodeDate(CollectionNode.InnerText);
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            } catch (Exception) {
                throw;
            }
            return result;
        }
        //
        //======================================================================================================
        /// <summary>
        /// data from local collection repository
        /// </summary>
        public class collectionStoreClass {
            public string name;
            public string guid;
            public string path;
            public DateTime lastChangeDate;
            public string version;
            public string description;
            public string contensiveVersion;
        }
    }
}
