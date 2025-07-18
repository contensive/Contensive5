﻿using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Contensive.Processor.Controllers.Build {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class BuildDataMigrationController : IDisposable {
        //
        //====================================================================================================
        /// <summary>
        /// when breaking changes are required for data, update them here
        /// </summary>
        /// <param name="core"></param>
        /// <param name="DataBuildVersion"></param>
        public static void migrateData(CoreController core, string DataBuildVersion, string logPrefix) {
            try {
                CPClass cp = core.cpParent;
                //
                // -- Roll the style sheet cache if it is setup
                core.siteProperties.setProperty("StylesheetSerialNumber", (-1).ToString());
                //
                // -- verify ID is primary key on all tables with an id
                foreach (TableModel table in DbBaseModel.createList<TableModel>(cp)) {
                    //
                    // -- hack, ccformset, ccforms, ccformfields were converted in designblocks but not removed
                    // -- should be removed by that collection, just avoid the error here
                    if (table.name.Equals("ccformsets", StringComparison.InvariantCultureIgnoreCase)) { continue; }
                    if (table.name.Equals("ccforms", StringComparison.InvariantCultureIgnoreCase)) { continue; }
                    if (table.name.Equals("ccformfields", StringComparison.InvariantCultureIgnoreCase)) { continue; }
                    if (table.name.Equals("ccuserformresponse", StringComparison.InvariantCultureIgnoreCase)) { continue; }
                    //
                    if (!string.IsNullOrWhiteSpace(table.name)) {
                        bool tableHasId = false;
                        {
                            //
                            // -- verify table as an id field
                            string sql = "SELECT name FROM sys.columns WHERE Name = N'ID' AND Object_ID = Object_ID(N'ccmembers')";
                            DataTable dt = cp.Db.ExecuteQuery(sql);
                            if (dt != null) {
                                tableHasId = !dt.Rows.Equals(0);
                            }
                        }
                        if (tableHasId) {
                            //
                            // -- table has id field, make sure it is primary key
                            string sql = ""
                                + " select Col.Column_Name"
                                + " from INFORMATION_SCHEMA.TABLE_CONSTRAINTS Tab, INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Col"
                                + " where (Col.Constraint_Name = Tab.Constraint_Name) AND (Col.Table_Name = Tab.Table_Name) AND (Constraint_Type = 'PRIMARY KEY') AND (Col.Table_Name = '" + table.name + "')";
                            bool idPrimaryKeyFound = false;
                            foreach (DataRow dr in core.db.executeQuery(sql).Rows) {
                                if (GenericController.encodeText(dr["Column_Name"]).ToLower().Equals("id")) {
                                    idPrimaryKeyFound = true;
                                    break;
                                }
                            }
                            if (!idPrimaryKeyFound) {
                                try {
                                    core.db.executeNonQuery("alter table " + table.name + " add primary key (ID)");
                                } catch (Exception ex) {
                                    logger.Error($"{core.logCommonMessage}", ex, "Content Table [" + table.name + "] does not include column ID. Exception happened while adding column and setting it primarykey.");
                                }
                            }
                        }
                    }
                }
                //
                // -- continue only if a previous build exists
                if (!string.IsNullOrEmpty(DataBuildVersion)) {
                    //
                    // -- 4.1 to 5 conversions
                    if (GenericController.versionIsOlder(DataBuildVersion, "4.1")) {
                        //
                        // -- create Data Migration Assets collection
                        var migrationCollection = DbBaseModel.createByUniqueName<AddonCollectionModel>(cp, "Data Migration Assets");
                        if (migrationCollection == null) {
                            migrationCollection = DbBaseModel.addDefault<AddonCollectionModel>(cp);
                            migrationCollection.name = "Data Migration Assets";
                        }
                        //
                        // -- remove all addon content fieldtype rules
                        DbBaseModel.deleteRows<AddonContentFieldTypeRulesModel>(cp, "(1=1)");
                        //
                        // -- delete /admin www subfolder
                        core.wwwFiles.deleteFolder("admin");
                        //
                        // -- delete .asp and .php files
                        foreach (CPFileSystemBaseClass.FileDetail file in core.wwwFiles.getFileList("")) {
                            if (file == null) { continue; }
                            if (string.IsNullOrWhiteSpace(file.Name)) { continue; }
                            if (file.Name.Length < 4) { continue; }
                            string extension = System.IO.Path.GetExtension(file.Name).ToLower(CultureInfo.InvariantCulture);
                            if (extension == ".php" || extension == ".asp") {
                                core.wwwFiles.deleteFile(file.Name);
                            }
                        }
                        //
                        // -- create www /cclib folder and copy in legacy resources
                        core.programFiles.copyFile("cclib.zip", "cclib.zip", core.wwwFiles);
                        core.wwwFiles.unzipFile("cclib.zip");
                        //
                        // -- remove all the old menu entries and leave the navigation entries
                        var navContent = DbBaseModel.createByUniqueName<ContentModel>(cp, NavigatorEntryModel.tableMetadata.contentName);
                        if (navContent != null) {
                            core.db.executeNonQuery("delete from ccMenuEntries where ((contentcontrolid<>0)and(contentcontrolid<>" + navContent.id + ")and(contentcontrolid is not null))");
                        }
                        //
                        // -- reinstall newest font-awesome collection
                        ErrorReturnModel returnErrorMessage = new();
                        var context = new Stack<string>();
                        var nonCritialErrorList = new List<string>();
                        var collectionsInstalledList = new List<string>();
                        bool skipCdefInstall = false;
                        CollectionLibraryController.installCollectionFromLibrary(core, false, context, Constants.fontAwesomeCollectionGuid, ref returnErrorMessage, false, true, ref nonCritialErrorList, logPrefix, ref collectionsInstalledList, skipCdefInstall);
                        //
                        // -- reinstall newest redactor collection
                        context = new Stack<string>();
                        nonCritialErrorList = new List<string>();
                        collectionsInstalledList = new List<string>();
                        CollectionLibraryController.installCollectionFromLibrary(core, false, context, Constants.redactorCollectionGuid, ref returnErrorMessage, false, true, ref nonCritialErrorList, logPrefix, ref collectionsInstalledList, skipCdefInstall);
                        //
                        string sql = "";
                        //
                        // -- create page menus from section menus
                        using (var cs = new CsModel(core)) {
                            sql = "select m.name as menuName, m.id as menuId, p.name as pageName, p.id as pageId, s.name as sectionName, m.*"
                                + " from ccDynamicMenus m"
                                + " left join ccDynamicMenuSectionRules r on r.DynamicMenuId = m.id"
                                + " left join ccSections s on s.id = r.SectionID"
                                + " left join ccPageContent p on p.id = s.RootPageID"
                                + " where (p.id is not null)and(s.active>0)"
                                + " order by m.id, s.sortorder,s.id";
                            if (cs.openSql(sql)) {
                                int sortOrder = 0;
                                do {
                                    string menuName = cs.getText("menuName");
                                    if (!string.IsNullOrWhiteSpace(menuName)) {
                                        var menu = DbBaseModel.createByUniqueName<MenuModel>(cp, menuName);
                                        if (menu == null) {
                                            menu = DbBaseModel.addEmpty<MenuModel>(cp);
                                            menu.name = menuName;
                                            try {
                                                menu.classItemActive = cs.getText("classItemActive");
                                                menu.classItemFirst = cs.getText("classItemFirst");
                                                menu.classItemHover = cs.getText("classItemHover");
                                                menu.classItemLast = cs.getText("classItemLast");
                                                menu.classTierAnchor = cs.getText("classTierItem");
                                                menu.classTierItem = cs.getText("classTierItem");
                                                menu.classTierList = cs.getText("classTierList");
                                                menu.classTopAnchor = cs.getText("classTopItem");
                                                menu.classTopItem = cs.getText("classTopItem");
                                                menu.classTopList = cs.getText("classTopList");
                                                menu.classTopWrapper = cs.getText("classTopWrapper");
                                            } catch (Exception ex) {
                                                logger.Error($"{core.logCommonMessage}", ex, "migrateData error populating menu from dynamic menu.");
                                            }
                                            menu.save(cp);
                                        }
                                        //
                                        // -- set the root page's menuHeadline to the section name
                                        var page = DbBaseModel.create<PageContentModel>(cp, cs.getInteger("pageId"));
                                        if (page != null) {
                                            page.menuHeadline = cs.getText("sectionName");
                                            page.save(cp);
                                            //
                                            // -- create a menu-page rule to attach this page to the menu in the current order
                                            var menuPageRule = DbBaseModel.addEmpty<MenuPageRuleModel>(cp);
                                            if (menuPageRule != null) {
                                                menuPageRule.name = "Created from v4.1 menu sections " + core.dateTimeNowMockable.ToString();
                                                menuPageRule.pageId = page.id;
                                                menuPageRule.menuId = menu.id;
                                                menuPageRule.active = true;
                                                menuPageRule.sortOrder = sortOrder.ToString().PadLeft(4, '0');
                                                menuPageRule.save(cp);
                                                sortOrder += 10;
                                            }
                                        }
                                    }
                                    cs.goNext();
                                } while (cs.ok());
                            }
                        }
                        //
                        // -- create a theme addon for each template for styles and meta content
                        using (var csTemplate = cp.CSNew()) {
                            if (csTemplate.Open("page templates")) {
                                do {
                                    int templateId = csTemplate.GetInteger("id");
                                    string templateStylePrepend = "";
                                    string templateStyles = csTemplate.GetText("StylesFilename");
                                    //
                                    // -- add shared styles to the template stylesheet
                                    using (var csStyleRule = cp.CSNew()) {
                                        if (csStyleRule.Open("shared styles template rules", "(TemplateID=" + templateId + ")")) {
                                            do {
                                                int sharedStyleId = csStyleRule.GetInteger("styleid");
                                                using (var csStyle = cp.CSNew()) {
                                                    if (csStyleRule.Open("shared styles", "(id=" + sharedStyleId + ")")) {
                                                        //
                                                        // -- prepend lines beginning with @ t
                                                        string styles = csStyleRule.GetText("StyleFilename");
                                                        if (!string.IsNullOrWhiteSpace(styles)) {
                                                            //
                                                            // -- trim off leading spaces, newlines, comments
                                                            styles = styles.Trim();
                                                            while (!string.IsNullOrWhiteSpace(styles) && styles.Substring(0, 1).Equals("@")) {
                                                                if (styles.IndexOf(Environment.NewLine) >= 0) {
                                                                    templateStylePrepend += styles.Substring(0, styles.IndexOf(Environment.NewLine));
                                                                    styles = styles.Substring(styles.IndexOf(Environment.NewLine) + 1).Trim();
                                                                } else {
                                                                    templateStylePrepend += styles;
                                                                    styles = string.Empty;
                                                                }
                                                            };
                                                            templateStyles += Environment.NewLine + styles;
                                                        }
                                                    }
                                                }
                                                csStyleRule.GoNext();
                                            } while (csStyleRule.OK());
                                        }
                                    }
                                    // 
                                    // -- create an addon
                                    var themeAddon = DbBaseModel.addDefault<AddonModel>(cp);
                                    themeAddon.name = "Theme assets for template " + csTemplate.GetText("name");
                                    themeAddon.otherHeadTags = csTemplate.GetText("otherheadtags");
                                    themeAddon.javaScriptBodyEnd = csTemplate.GetText("jsendbody");
                                    themeAddon.stylesFilename.content = templateStylePrepend + Environment.NewLine + templateStyles;
                                    themeAddon.collectionId = migrationCollection.id;
                                    themeAddon.save(cp);
                                    // 
                                    // -- create an addon template rule to set dependency
                                    var rule = DbBaseModel.addEmpty<AddonTemplateRuleModel>(cp);
                                    rule.addonId = themeAddon.id;
                                    rule.templateId = templateId;
                                    rule.save(cp);
                                    //
                                    csTemplate.GoNext();
                                } while (csTemplate.OK());

                            }
                        }
                        //
                        // -- reset the html minify so it is easier to resolve other issues
                        core.siteProperties.setProperty("ALLOW HTML MINIFY", false);
                        //
                        // -- remove contentcategoryid from all edit page
                        cp.Db.ExecuteNonQuery("update ccfields set Authorable=0 where name='contentcategoryid'");
                        cp.Db.ExecuteNonQuery("update ccfields set Authorable=0 where name='editsourceid'");
                        cp.Db.ExecuteNonQuery("update ccfields set Authorable=0 where name='editarchive'");
                        cp.Db.ExecuteNonQuery("update ccfields set Authorable=0 where name='editblank'");
                        //
                        // -- remove legacy workflow fields
                        UpgradeController.dropLegacyWorkflowField(core, "editsourceid");
                        cp.Db.ExecuteNonQuery("delete from ccfields where name='editsourceid'");
                        //
                        UpgradeController.dropLegacyWorkflowField(core, "editblank");
                        cp.Db.ExecuteNonQuery("delete from ccfields where name='editblank'");
                        //
                        UpgradeController.dropLegacyWorkflowField(core, "editarchive");
                        cp.Db.ExecuteNonQuery("delete from ccfields where name='editarchive'");
                        //
                        UpgradeController.dropLegacyWorkflowField(core, "contentcategoryid");
                        cp.Db.ExecuteNonQuery("delete from ccfields where name='contentcategoryid'");
                        //
                        //
                        // -- end of 4.1 to 5 conversion
                    }
                    //
                    // -- 5.19.1223 conversion -- render AddonList no copyFilename
                    if (GenericController.versionIsOlder(DataBuildVersion, "5.19.1223")) {
                        //
                        // -- verify design block installation
                        string returnUserError = "";
                        if (!cp.Db.IsTable("dbtext")) {
                            if (!cp.Addon.InstallCollectionFromLibrary(Constants.designBlockCollectionGuid, ref returnUserError)) { throw new Exception("Error installing Design Blocks, required for data upgrade. " + returnUserError); }
                        }
                        //
                        // -- add a text block and childPageList to every page without an addonlist
                        foreach (var page in DbBaseModel.createList<PageContentModel>(cp, "(addonList is null)")) {
                            convertPageContentToAddonList(core, page);
                        }
                        core.siteProperties.setProperty("PageController Render Legacy Copy", false);
                    }
                    //
                    // -- 5.2005.9.4 conversion -- collections incorrectly marked not-updateable - mark all except themes (templates)
                    if (GenericController.versionIsOlder(DataBuildVersion, "5.2005.9.4")) {
                        //
                        // -- 
                        cp.Db.ExecuteNonQuery("update ccaddoncollections set updatable=1 where name not like '%theme%'");
                    }
                    //
                    // -- 5.2005.19.1 conversion -- rename site property EmailUrlRootRelativePrefix to LocalFileModeProtocolDomain
                    if (GenericController.versionIsOlder(DataBuildVersion, "5.2005.19.1")) {
                        //
                        // -- 
                        if (string.IsNullOrWhiteSpace(cp.Site.GetText("webAddressProtocolDomain"))) {
                            cp.Site.SetProperty("webAddressProtocolDomain", cp.Site.GetText("EmailUrlRootRelativePrefix"));
                        }
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "22.3.15.4")) {
                        //
                        // -- delete legacy corehelp collection. Created with fields that have only field name, legacy install layed collections over the application collection
                        //    new install loads fields directly from collection, which coreHelp then marks all fields inactive.
                        core.db.delete("ccaddoncollections", "{6e905db1-d3f0-40af-aac4-4bd78e680fae}");
                        //
                        // -- 21.12.29.0 - delete Edit Settigns (af=43)
                        core.db.delete("ccmenuentries", "{5F714C38-A5EB-4BFC-86BB-9DAA2C5F113E}");
                        //
                        // -- 22.3.13.22 -- sort orders were installed incorrectly
                        core.db.executeNonQuery("delete from ccSortMethods where name is null");
                        core.db.executeNonQuery("update ccSortMethods set ccguid='{97128516-AEDF-4B6C-BC56-F6EAA4C3AA78}' where name='By Name'");
                        core.db.executeNonQuery("update ccSortMethods set ccguid='{61ACDEA0-6E26-478F-9050-20D8C1F9D7B4}' where name='By Alpha Sort Order Field'");
                        core.db.executeNonQuery("update ccSortMethods set ccguid='{25B0724D-CB5C-45C5-98C5-FCA2EC941132}' where name='By Date'");
                        core.db.executeNonQuery("update ccSortMethods set ccguid='{FC3889B7-8624-437A-A6E5-A628D93D73CA}' where name='By Date Reverse'");
                        core.db.executeNonQuery("update ccSortMethods set ccguid='{73B4559E-0995-4E6F-8981-B116FA0E0A5F}' where name='By Alpha Sort Order Then Oldest First'");
                        //
                        // -- 22.3.14.1 -- convert activity log message from text to longtext
                        core.db.executeNonQuery("update ccfields set type=" + (int)CPContentBaseClass.FieldTypeIdEnum.LongText + " where name='Message' and contentid=" + cp.Content.GetID("activity log"));
                        //
                        // -- 22.3.15.4 -- addon category 'containers' has incorrect guid
                        core.db.executeNonQuery("update ccAddonCategories set ccguid='{bc311ad8-fcae-4228-800d-e432733fdf3e}' where name='Containers'");
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "22.4.14.1")) {
                        //
                        // -- early data migration created duplicate indexes with this format - ccEmailDrops$ccEmailDropsContentControlID
                        foreach (TableModel table in DbBaseModel.createList<TableModel>(cp)) {
                            string tableName = table.name;
                            if (string.IsNullOrEmpty(tableName)) { continue; }
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "ID] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "Name] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "Active] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "ccGuid] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "ContentControlID] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "CreateKey] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "SortOrder] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "DateAdded] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "ModifiedDate] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "CreatedBy] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "ModifiedBy] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "ContentCategoryID] ON [" + tableName + "]");
                            cp.Db.ExecuteNonQuery("DROP INDEX IF EXISTS [" + tableName + "$" + tableName + "EditsourceID] ON [" + tableName + "]");
                        }
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "22.8.12.1")) {
                        //
                        // -- import old smtp bounce list and update emailBounceList table
                        string blockList = core.privateFiles.readFileText("Config\\SMTPBlockList.txt");
                        blockList += core.cdnFiles.readFileText("Config\\SMTPBlockList_" + core.appConfig.name + ".txt");
                        foreach (var row in blockList.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) {
                            string[] rowParts = row.Split('\t');
                            var record = DbBaseModel.addDefault<EmailBounceListModel>(cp);
                            record.name = rowParts[0];
                            record.email = EmailController.getSimpleEmailFromFriendlyEmail(cp, record.name);
                            record.details = rowParts[1] + ", user requested to be unsubscribed.";
                            record.save(cp);
                        }
                        core.privateFiles.renameFile("Config\\SMTPBlockList.txt", "Legacy_SMTPBlockList.txt");
                        core.cdnFiles.renameFile("Config\\SMTPBlockList_" + core.appConfig.name + ".txt", "Legacy_SMTPBlockList_" + core.appConfig.name + ".txt");
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "22.11.27.5")) {
                        //
                        // -- minify addon styles and javascript
                        foreach (var addon in DbBaseModel.createList<AddonModel>(cp)) {
                            try {
                                MinifyController.minifyAddon(core, addon);
                            } catch (Exception ex) {
                                logger.Warn(ex, $"{core.logCommonMessage},Warning during upgrade, data migration, build addon css and js minification, addon [" + addon.id + ", " + addon.name + "]");
                            }
                        }
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "23.2.25.1")) {
                        //
                        // -- default content populated 
                        core.db.executeNonQuery("update ccPageContent set customblockmessage=null where customblockmessage like '%<%'");
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "23.7.21.1")) {
                        //
                        // -- remove all addons with active-x components
                        core.db.executeNonQuery("delete from ccaggregatefunctions where (objectprogramid is not null)or(objectprogramid='')");
                        // -- no, cannot remove field because it fails old collection installs (and it must be removed from model also)
                        //core.db.executeNonQuery($"delete from ccfields where name='objectprogramid' and contentcontrolid={cp.Content.GetID("Content fields")}");
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "23.7.28.6")) {
                        //
                        // -- remove page edit tag
                        core.siteProperties.setProperty("allow page settings edit", false);
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "23.11.26.1")) {
                        //
                        // -- change sitewarnings field from text to varchar
                        core.db.executeNonQuery("ALTER TABLE ccsitewarnings ALTER COLUMN description VARCHAR(4000)");
                        //
                        // -- convert all text to varchar(nmax)
                        foreach (var table in DbBaseModel.createList<TableModel>(cp, "(active>0)and(name is not null)and(name<>'')")) {
                            DataTable dt = core.db.executeQuery($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table.name}' and data_type='text'");
                            if (dt?.Rows == null && dt.Rows.Count == 0) { continue; }
                            foreach (DataRow dr in dt.Rows) {
                                core.db.executeNonQuery($"ALTER TABLE {table.name} ALTER COLUMN {cp.Utils.EncodeText(dr[0])} VARCHAR(max)");
                            }
                        }
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "24.1.1.1")) {
                        //
                        // -- old sites, turn off all password requirements
                        core.siteProperties.passwordBlockUsedPasswordPeriod = 0;
                        core.siteProperties.passwordMinLength = 0;
                        core.siteProperties.passwordRequiresLowercase = false;
                        core.siteProperties.passwordRequiresNumber = false;
                        core.siteProperties.passwordRequiresSpecialCharacter = false;
                        core.siteProperties.passwordRequiresUppercase = false;
                        core.siteProperties.clearAdminPasswordOnHash = false;
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "24.7.6.1")) {
                        //
                        // -- remove deprecated fields
                        cp.Db.ExecuteNonQuery("delete from ccfields where name='boaddedupuserid'");
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "24.7.31.1")) {
                        //
                        // -- deprecate wrappers
                        cp.Db.ExecuteNonQuery("delete from ccMenuEntries where ccguid='{131E0319-D516-4AB2-BBDA-D7EA63A8AD9E}'");
                        cp.Db.ExecuteNonQuery("update cccontent set isbasecontent=0 where name='Wrappers'");
                        // leave table and content metadata in case an addon accesses the data
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "24.7.31.1")) {
                        //
                        // -- delete old hardcoded menu entries
                        cp.Db.ExecuteNonQuery("delete from ccMenuEntries where ccguid='{131E0319-D516-4AB2-BBDA-D7EA63A8AD9E}'");
                        cp.Db.ExecuteNonQuery("update cccontent set isbasecontent=0 where name='Wrappers'");
                        // leave table and content metadata in case an addon accesses the data
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "24.12.19.0")) {
                        //
                        // -- update redactor, needed because edit modal needs redactor js to load the redactor library
                        string returnErrorMessage = "";
                        cp.Addon.InstallCollectionFromLibrary(Constants.redactorCollectionGuid, ref returnErrorMessage);
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "24.12.28.0")) {
                        //
                        // -- Sort Methods created without name caused duplicates
                        // -- merging will cause issues, but they have a blank name anyway
                        cp.Db.ExecuteNonQuery("delete from ccSortMethods where (name is null)or(name='')or(orderbyclause is null)or(orderbyclause='')");
                        BuildController.verifySortMethod(core, "By Name", "name");
                        BuildController.verifySortMethod(core, "By Alpha Sort Order Field", "sortorder");
                        BuildController.verifySortMethod(core, "By Date", "dateadded");
                        BuildController.verifySortMethod(core, "By Date Reverse", "dateadded desc");
                        BuildController.verifySortMethod(core, "By Alpha Sort Order Then Oldest First", "sortorder,id");
                        //
                        // -- staff and site manager groups have duplicates because xml installed records by guid after build created them programmatically
                        mergeGroupFixCase(cp, "Staff");
                        mergeGroupFixCase(cp, "Site Managers");
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "24.12.28.3")) {
                        //
                        // -- append breadcrumb widget to all pages with feature set
                        foreach (var page in DbBaseModel.createList<PageContentModel>(cp, "allowReturnLinkDisplay>0")) {
                            List<AddonListItemModel> addonList = cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                            if (addonList is null) { addonList = []; }
                            if (addonList.Count == 0 || addonList[0].designBlockTypeGuid != Constants.addonGuidBreadcrumbWidget) {
                                addonList.Insert(0, new AddonListItemModel {
                                    designBlockTypeGuid = Constants.addonGuidBreadcrumbWidget,
                                    designBlockTypeName = Constants.addonNameBreadcrumbWidget,
                                    instanceGuid = GenericController.getGUID(),
                                    renderedHtml = "",
                                    renderedAssets = new Models.AddonAssetsModel {
                                        headStyles = [],
                                        headStylesheetLinks = [],
                                        headJs = [],
                                        headJsLinks = [],
                                        bodyJs = [],
                                        bodyJsLinks = []
                                    },
                                    columns = [],
                                    settingsEditUrl = "",
                                    addonEditUrl = ""
                                });
                            }
                            page.allowReturnLinkDisplay = false;
                            page.addonList = cp.JSON.Serialize(addonList);
                            page.save(cp);
                        }

                        mergeGroupFixCase(cp, "Staff");
                        mergeGroupFixCase(cp, "Site Managers");
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "25.3.2.2")) {
                        //
                        // -- update breadcrumb widget in addonlist to populate null entries
                        foreach (var page in DbBaseModel.createList<PageContentModel>(cp)) {
                            bool updated = false;
                            List<AddonListItemModel> addonList = cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                            if (addonList is null) { continue; }
                            if (addonList.Count == 0) { continue; }
                            foreach (var addon in addonList) {
                                if (addon.designBlockTypeGuid == Constants.addonGuidBreadcrumbWidget) {
                                    if (addon.renderedAssets == null) {
                                        updated = true;
                                        addon.renderedAssets = new Models.AddonAssetsModel {
                                            headStyles = [],
                                            headStylesheetLinks = [],
                                            headJs = [],
                                            headJsLinks = [],
                                            bodyJs = [],
                                            bodyJsLinks = []
                                        };
                                        if (addon.columns == null) {
                                            updated = true;
                                            addon.columns = new List<Models.AddonListColumnItemModel>();
                                        }
                                    }
                                }
                            }
                            if (updated) {
                                page.addonList = cp.JSON.Serialize(addonList);
                                page.save(cp);
                            }
                        }
                    }
                    if (GenericController.versionIsOlder(DataBuildVersion, "25.5.28.21")) {
                        //
                        // -- delete Editor Config Tool (dup of ConfigureEditClass)
                        core.db.executeNonQuery("delete from ccaggregatefunctions where ccguid='{C2100D71-8993-4491-8D75-79FCF6B329E9}'");
                        //
                        // -- delete Editor Config Tool (dup of ConfigureListClass)
                        core.db.executeNonQuery("delete from ccaggregatefunctions where ccguid='{91373E4C-4562-42DD-A121-C5E09DAF1CFE}'");
                    }
                    //
                    // -- Reload
                    core.cache.invalidateAll();
                    core.cacheRuntime.clear();
                }
            } catch (Exception ex) {
                logger.Error($"{core.logCommonMessage}", ex, "Warning during upgrade, data migration");
            }
        }
        //
        // ====================================================================================================
        //
        private static void mergeGroupFixCase(CPClass cp, string groupName) {
            List<GroupModel> groups = DbBaseModel.createList<GroupModel>(cp, $"name='{groupName}'");
            foreach (GroupModel group in groups.Skip(1)) {
                using (DataTable dt = cp.Db.ExecuteQuery("select t.name from ccfields f left join cccontent c on c.id=f.ContentID left join cctables t on t.id=c.ContentTableID where f.name='groupid'")) {
                    if (dt?.Rows != null) {
                        foreach (DataRow row in dt.Rows) {
                            string tableName = cp.Utils.EncodeText(row["name"]);
                            cp.Db.ExecuteNonQuery($"update {tableName} set groupid={groups.First().id} where groupId={group.id}");
                        }
                    }
                    cp.Db.Delete("ccgroups", group.id);
                }
                cp.Db.ExecuteNonQuery($"update ccgroups set name={cp.Db.EncodeSQLText(groupName)} where name={cp.Db.EncodeSQLText(groupName)}");
            };
        }
        //
        // ====================================================================================================
        //
        public static void convertPageContentToAddonList(CoreController core, PageContentModel page) {
            // 
            // -- save copyFilename copy to new Text Block record
            string textBlockInstanceGuid = GenericController.getGUID();
            core.cpParent.Db.ExecuteNonQuery("insert into dbText (active,name,text,ccguid) values (1,'Text Block'," + core.cpParent.Db.EncodeSQLText(page.copyfilename.content) + "," + core.cpParent.Db.EncodeSQLText(textBlockInstanceGuid) + ")");
            // 
            // -- assign all child pages without a childpageListname to this new childpageList addon
            string childListInstanceGuid = GenericController.getGUID();
            core.cpParent.Db.ExecuteNonQuery("update ccpagecontent set parentListName=" + core.cpParent.Db.EncodeSQLText(childListInstanceGuid) + " where (parentId=" + page.id + ")and((parentListName='')or(parentListName is null))");
            //
            // -- set defaultAddonList.json into page.addonList
            string addonList = Resources.defaultAddonListJson.replace("{textBlockInstanceGuid}", textBlockInstanceGuid, StringComparison.InvariantCulture).replace("{childListInstanceGuid}", childListInstanceGuid, StringComparison.InvariantCulture);
            core.cpParent.Db.ExecuteNonQuery("update ccpagecontent set addonList=" + core.cpParent.Db.EncodeSQLText(addonList) + " where (id=" + page.id + ")");
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed;
        //
        public void Dispose() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~BuildDataMigrationController() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);


        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                disposed = true;
                if (disposing) {
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }
    //
}