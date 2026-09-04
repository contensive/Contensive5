
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using HtmlAgilityPack;
using NLog;
using System;
using System.Collections.Generic;
using System.Xml;
using static Contensive.BaseClasses.CPLayoutBaseClass;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Handle resource file installation during collection install.
    /// </summary>
    public static class CollectionInstallResourceController {
        //
        //====================================================================================================
        /// <summary>
        /// Process a single resource node from the collection XML. Copies files to the appropriate
        /// file system (www, private, cdn, helpfiles) and tracks them in the resource manifest.
        /// </summary>
        internal static void installResourceNode(CoreController core, XmlNode metaDataSection, string collectionName, string collectionGuid, string collectionVersionFolder, ResourceManifestModel resourceManifest, HashSet<string> trackedFolders, List<string> assembliesInZip, ref string wwwFileList, ref string contentFileList, ref string execFileList, ref string layoutFileList) {
            //
            // set wwwfilelist, contentfilelist, execfilelist
            //
            string resourceType = XmlController.getXMLAttribute(core, metaDataSection, "type", "");
            string resourcePath = XmlController.getXMLAttribute(core, metaDataSection, "path", "");
            string filename = XmlController.getXMLAttribute(core, metaDataSection, "name", "");
            //
            logger.Info($"{core.logCommonMessage}, installCollectionFromAddonCollectionFolder [{collectionName}], resource found, name [{filename}], type [{resourceType}], path [{resourcePath}]");
            //
            filename = FileController.convertToDosSlash(filename);
            string SrcPath = "";
            string dstPath = resourcePath;
            int Pos = GenericController.strInstr(1, filename, "\\");
            if (Pos != 0) {
                //
                // Source path is in filename
                //
                SrcPath = filename.left(Pos - 1);
                filename = filename.Substring(Pos);
                if (string.IsNullOrEmpty(resourcePath)) {
                    //
                    // -- No Resource Path give, use the same folder structure from source
                    dstPath = SrcPath;
                } else {
                    //
                    // -- Copy file to resource path
                    dstPath = resourcePath;
                }
            }
            //
            // -- if the filename in the collection file is the wrong case, correct it now
            filename = core.privateFiles.correctFilenameCase(collectionVersionFolder + SrcPath + filename);
            //
            // -- verify the source file exists before attempting to copy
            string srcPathFilename = collectionVersionFolder + SrcPath + filename;
            if (!core.privateFiles.fileExists(srcPathFilename)) {
                logger.Error($"{core.logCommonMessage}, installCollectionFromAddonCollectionFolder [{collectionName}], resource file missing from installation, file [{srcPathFilename}], type [{resourceType}], path [{resourcePath}]. The installation will continue without this file.");
                return;
            }
            //
            // == normalize dst
            string dstDosPath = FileController.normalizeDosPath(dstPath);
            //
            // --
            switch (resourceType.ToLowerInvariant()) {
                case "wwwfiles":
                case "wwwfile":
                case "wwwroot":
                case "www": {
                        wwwFileList += Environment.NewLine + dstDosPath + filename;
                        logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying file to wwwFiles, src [{collectionVersionFolder}{SrcPath}], dst [{core.appConfig.localWwwPath}{dstDosPath}].");
                        core.privateFiles.copyFile(collectionVersionFolder + SrcPath + filename, dstDosPath + filename, core.wwwFiles);
                        if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                            logger.Info($"{core.logCommonMessage}, installCollectionFromAddonCollectionFolder [{collectionName}], GUID [{collectionGuid}], pass 1, unzipping www file [{core.appConfig.localWwwPath}{dstDosPath}{filename}].");
                            if (!string.IsNullOrEmpty(dstDosPath)) {
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "www", folderPath = dstDosPath });
                            }
                            ResourceManifestModel.unzipToTempThenCopy(core, core.wwwFiles, dstDosPath, dstDosPath + filename, "www", resourceManifest);
                            core.wwwFiles.deleteFile(dstDosPath + filename);
                        } else {
                            resourceManifest.resources.Add(new ResourceManifestModel.ResourceManifestEntry { type = "www", destinationPath = dstDosPath + filename });
                            if (!string.IsNullOrEmpty(dstDosPath) && !trackedFolders.Contains($"www::{dstDosPath}")) {
                                trackedFolders.Add($"www::{dstDosPath}");
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "www", folderPath = dstDosPath });
                            }
                        }
                        break;
                    }
                case "privatefiles":
                case "privatefile":
                case "private": {
                        contentFileList += Environment.NewLine + dstDosPath + filename;
                        logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying file to privateFiles, src [{collectionVersionFolder}{SrcPath}], dst [{dstDosPath}].");
                        core.privateFiles.copyFile(collectionVersionFolder + SrcPath + filename, dstDosPath + filename);
                        if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, unzipping privateFiles file [{dstDosPath}{filename}].");
                            if (!string.IsNullOrEmpty(dstDosPath)) {
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "private", folderPath = dstDosPath });
                                trackedFolders.Add($"private::{dstDosPath}");
                            }
                            ResourceManifestModel.unzipToTempThenCopy(core, core.privateFiles, dstDosPath, dstDosPath + filename, "private", resourceManifest);
                            core.privateFiles.deleteFile(dstDosPath + filename);
                        } else {
                            resourceManifest.resources.Add(new ResourceManifestModel.ResourceManifestEntry { type = "private", destinationPath = dstDosPath + filename });
                            if (!string.IsNullOrEmpty(dstDosPath) && !trackedFolders.Contains($"private::{dstDosPath}")) {
                                trackedFolders.Add($"private::{dstDosPath}");
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "private", folderPath = dstDosPath });
                            }
                        }
                        break;
                    }
                case "cdn":
                case "cdnfile":
                case "cdnfiles":
                case "file":
                case "files":
                case "content": {
                        contentFileList += Environment.NewLine + dstDosPath + filename;
                        logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying file to cdnFiles, src [{collectionVersionFolder}{SrcPath}], dst [{dstDosPath}].");
                        core.privateFiles.copyFile(collectionVersionFolder + SrcPath + filename, dstDosPath + filename, core.cdnFiles);
                        if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, unzipping cdnFiles [{dstDosPath}{filename}].");
                            if (!string.IsNullOrEmpty(dstDosPath)) {
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "cdn", folderPath = dstDosPath });
                                trackedFolders.Add($"cdn::{dstDosPath}");
                            }
                            ResourceManifestModel.unzipToTempThenCopy(core, core.cdnFiles, dstDosPath, dstDosPath + filename, "cdn", resourceManifest);
                            core.cdnFiles.deleteFile(dstDosPath + filename);
                        } else {
                            resourceManifest.resources.Add(new ResourceManifestModel.ResourceManifestEntry { type = "cdn", destinationPath = dstDosPath + filename });
                            if (!string.IsNullOrEmpty(dstDosPath) && !trackedFolders.Contains($"cdn::{dstDosPath}")) {
                                trackedFolders.Add($"cdn::{dstDosPath}");
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "cdn", folderPath = dstDosPath });
                            }
                        }
                        break;
                    }
                case "helpfiles":
                case "helpcenter":
                case "helpcenterfile":
                case "helpcenterfiles":
                case "helpfile":
                case "help": {
                        //
                        // -- ignore the resource path for helpfiles, always install to helpfiles\
                        string helpFilesDstPath = "helpfiles\\";
                        //
                        // -- prefix filename with collection name (Base5 uses "Contensive" prefix)
                        string originalFilename = filename;
                        string helpPrefix = getHelpFilePrefix(collectionName);
                        filename = $"{helpPrefix}.{filename}";
                        //
                        logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying file to privateFiles helpFiles, src [{collectionVersionFolder}{SrcPath}], dst [{helpFilesDstPath}].");
                        core.privateFiles.copyFile(collectionVersionFolder + SrcPath + originalFilename, helpFilesDstPath + filename);
                        if (GenericController.toLCase(filename.Substring(filename.Length - 4)) == ".zip") {
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, unzipping helpFiles file [{helpFilesDstPath}{filename}].");
                            resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "helpfiles", folderPath = helpFilesDstPath });
                            trackedFolders.Add($"helpfiles::{helpFilesDstPath}");
                            unzipHelpFilesToTempThenCopy(core, helpFilesDstPath, helpFilesDstPath + filename, helpPrefix, resourceManifest);
                            core.privateFiles.deleteFile(helpFilesDstPath + filename);
                        } else {
                            resourceManifest.resources.Add(new ResourceManifestModel.ResourceManifestEntry { type = "helpfiles", destinationPath = helpFilesDstPath + filename });
                            if (!string.IsNullOrEmpty(helpFilesDstPath) && !trackedFolders.Contains($"helpfiles::{helpFilesDstPath}")) {
                                trackedFolders.Add($"helpfiles::{helpFilesDstPath}");
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "helpfiles", folderPath = helpFilesDstPath });
                            }
                        }
                        break;
                    }
                case "layoutfiles":
                case "layoutfile":
                case "layout": {
                        string layoutFilesDstPath = "layoutFiles\\";
                        string ext = System.IO.Path.GetExtension(filename).ToLowerInvariant();
                        if (ext == ".zip") {
                            //
                            // -- zip file: copy to wwwFiles first, then extract with split logic
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying layout zip to wwwFiles for extraction, src [{collectionVersionFolder}{SrcPath}], dst [{dstDosPath}].");
                            core.privateFiles.copyFile(collectionVersionFolder + SrcPath + filename, dstDosPath + filename, core.wwwFiles);
                            if (!string.IsNullOrEmpty(dstDosPath)) {
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "layout-www", folderPath = dstDosPath });
                            }
                            resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "layout-private", folderPath = layoutFilesDstPath });
                            var htmlFilesCopied = ResourceManifestModel.unzipLayoutToTempThenCopy(core, dstDosPath, dstDosPath + filename, resourceManifest);
                            core.wwwFiles.deleteFile(dstDosPath + filename);
                            //
                            // -- import HTML files by meta tags into database records
                            importLayoutFilesByMetaTags(core, collectionName, htmlFilesCopied);
                        } else if (ext == ".htm" || ext == ".html") {
                            //
                            // -- HTML file: copy to layoutFiles\ in privateFiles
                            layoutFileList += Environment.NewLine + layoutFilesDstPath + filename;
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying layout HTML to privateFiles, src [{collectionVersionFolder}{SrcPath}], dst [{layoutFilesDstPath}].");
                            core.privateFiles.copyFile(collectionVersionFolder + SrcPath + filename, layoutFilesDstPath + filename);
                            resourceManifest.resources.Add(new ResourceManifestModel.ResourceManifestEntry { type = "layout-private", destinationPath = layoutFilesDstPath + filename });
                            if (!trackedFolders.Contains($"layout-private::{layoutFilesDstPath}")) {
                                trackedFolders.Add($"layout-private::{layoutFilesDstPath}");
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "layout-private", folderPath = layoutFilesDstPath });
                            }
                            //
                            // -- import HTML file by meta tags into database records
                            importLayoutFilesByMetaTags(core, collectionName, new List<string> { filename });
                        } else {
                            //
                            // -- non-HTML file: copy to dstDosPath in wwwFiles
                            layoutFileList += Environment.NewLine + dstDosPath + filename;
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], pass 1, copying layout file to wwwFiles, src [{collectionVersionFolder}{SrcPath}], dst [{dstDosPath}].");
                            core.privateFiles.copyFile(collectionVersionFolder + SrcPath + filename, dstDosPath + filename, core.wwwFiles);
                            resourceManifest.resources.Add(new ResourceManifestModel.ResourceManifestEntry { type = "layout-www", destinationPath = dstDosPath + filename });
                            if (!string.IsNullOrEmpty(dstDosPath) && !trackedFolders.Contains($"layout-www::{dstDosPath}")) {
                                trackedFolders.Add($"layout-www::{dstDosPath}");
                                resourceManifest.folders.Add(new ResourceManifestModel.ResourceManifestFolderEntry { type = "layout-www", folderPath = dstDosPath });
                            }
                        }
                        break;
                    }
                default: {
                        if (assembliesInZip.Contains(filename)) {
                            assembliesInZip.Remove(filename);
                        }
                        execFileList = execFileList + Environment.NewLine + filename;
                        break;
                    }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Read each HTML file from privateFiles\layoutFiles\, parse meta tags, and create/update
        /// the corresponding database records (layout, page template, email template, email).
        /// This mirrors the meta-tag import logic in ImportController.processImportFile.
        /// Errors are logged but do not fail the collection install.
        /// </summary>
        internal static void importLayoutFilesByMetaTags(CoreController core, string collectionName, List<string> htmlFilenames) {
            if (htmlFilenames == null || htmlFilenames.Count == 0) { return; }
            foreach (string htmlFilename in htmlFilenames) {
                try {
                    //
                    // -- read the HTML file from privateFiles\layoutFiles\
                    string htmlContent = core.privateFiles.readFileText($"layoutFiles\\{htmlFilename}");
                    if (string.IsNullOrWhiteSpace(htmlContent)) { continue; }
                    //
                    // -- parse HTML and scan for meta tags
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(HtmlController.wrapMustacheAttributes(htmlContent));
                    string layoutRecordName = string.Empty;
                    string layoutRecordGuid = string.Empty;
                    string pageTemplateRecordName = string.Empty;
                    string pageTemplateRecordGuid = string.Empty;
                    string emailTemplateRecordName = string.Empty;
                    string emailTemplateRecordGuid = string.Empty;
                    string emailRecordName = string.Empty;
                    string emailRecordGuid = string.Empty;
                    var metadataList = htmlDoc.DocumentNode.SelectNodes("//meta");
                    if (metadataList != null) {
                        foreach (var metadataNode in metadataList) {
                            switch (metadataNode.GetAttributeValue("name", string.Empty).ToLowerInvariant()) {
                                case "layout": {
                                        layoutRecordName = metadataNode.GetAttributeValue("content", string.Empty);
                                        break;
                                    }
                                case "layout-guid": {
                                        layoutRecordGuid = metadataNode.GetAttributeValue("content", string.Empty);
                                        break;
                                    }
                                case "template":
                                case "pagetemplate": {
                                        pageTemplateRecordName = metadataNode.GetAttributeValue("content", string.Empty);
                                        break;
                                    }
                                case "template-guid":
                                case "pagetemplate-guid": {
                                        pageTemplateRecordGuid = metadataNode.GetAttributeValue("content", string.Empty);
                                        break;
                                    }
                                case "emailtemplate": {
                                        emailTemplateRecordName = metadataNode.GetAttributeValue("content", string.Empty);
                                        break;
                                    }
                                case "emailtemplate-guid": {
                                        emailTemplateRecordGuid = metadataNode.GetAttributeValue("content", string.Empty);
                                        break;
                                    }
                                case "email": {
                                        emailRecordName = metadataNode.GetAttributeValue("content", string.Empty);
                                        break;
                                    }
                                case "email-guid": {
                                        emailRecordGuid = metadataNode.GetAttributeValue("content", string.Empty);
                                        break;
                                    }
                            }
                        }
                    }
                    //
                    // -- if no recognized meta tags found, skip this file
                    if (string.IsNullOrWhiteSpace(layoutRecordName)
                        && string.IsNullOrWhiteSpace(pageTemplateRecordName)
                        && string.IsNullOrWhiteSpace(emailTemplateRecordName)
                        && string.IsNullOrWhiteSpace(emailRecordName)) {
                        continue;
                    }
                    //
                    // -- determine layout framework version for platform-specific layout content
                    int layoutFrameworkId = core.siteProperties.htmlPlatformVersion;
                    //
                    // -- save layout record
                    if (!string.IsNullOrWhiteSpace(layoutRecordName)) {
                        var ignoreErrors = new List<string>();
                        string processedHtml = ImportController.processHtml(core.cpParent, htmlContent, ImporttypeEnum.LayoutForAddon, ref ignoreErrors, layoutRecordName);
                        if (!string.IsNullOrEmpty(processedHtml)) {
                            LayoutModel layout;
                            if (!string.IsNullOrWhiteSpace(layoutRecordGuid)) {
                                //
                                // -- guid provided, get-or-create by guid (ensures stable identity across installs)
                                layout = DbBaseModel.verify<LayoutModel>(core.cpParent, layoutRecordGuid);
                                layout.name = layoutRecordName;
                            } else {
                                //
                                // -- no guid, fall back to name-based lookup
                                layout = DbBaseModel.createByUniqueName<LayoutModel>(core.cpParent, layoutRecordName);
                                if (layout == null) {
                                    layout = DbBaseModel.addDefault<LayoutModel>(core.cpParent);
                                    layout.name = layoutRecordName;
                                }
                            }
                            if (layoutFrameworkId == 5) {
                                layout.layoutPlatform5.content = processedHtml;
                            } else {
                                layout.layout.content = processedHtml;
                            }
                            layout.modifiedDate = DateTime.Now;
                            layout.save(core.cpParent);
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], imported layout [{layoutRecordName}] from layout file [{htmlFilename}]");
                        }
                    }
                    //
                    // -- save page template record
                    if (!string.IsNullOrWhiteSpace(pageTemplateRecordName)) {
                        var ignoreErrors = new List<string>();
                        string processedHtml = ImportController.processHtml(core.cpParent, htmlContent, ImporttypeEnum.PageTemplate, ref ignoreErrors, pageTemplateRecordName);
                        if (!string.IsNullOrEmpty(processedHtml)) {
                            PageTemplateModel pageTemplate;
                            if (!string.IsNullOrWhiteSpace(pageTemplateRecordGuid)) {
                                //
                                // -- guid provided, get-or-create by guid
                                pageTemplate = DbBaseModel.verify<PageTemplateModel>(core.cpParent, pageTemplateRecordGuid);
                                pageTemplate.name = pageTemplateRecordName;
                            } else {
                                //
                                // -- no guid, fall back to name-based lookup
                                pageTemplate = DbBaseModel.createByUniqueName<PageTemplateModel>(core.cpParent, pageTemplateRecordName);
                                if (pageTemplate == null) {
                                    pageTemplate = DbBaseModel.addDefault<PageTemplateModel>(core.cpParent);
                                    pageTemplate.name = pageTemplateRecordName;
                                }
                            }
                            pageTemplate.bodyHTML = processedHtml;
                            pageTemplate.modifiedDate = DateTime.Now;
                            pageTemplate.save(core.cpParent);
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], imported page template [{pageTemplateRecordName}] from layout file [{htmlFilename}]");
                        }
                    }
                    //
                    // -- save email template record
                    if (!string.IsNullOrWhiteSpace(emailTemplateRecordName)) {
                        var ignoreErrors = new List<string>();
                        string processedHtml = ImportController.processHtml(core.cpParent, htmlContent, ImporttypeEnum.EmailTemplate, ref ignoreErrors, emailTemplateRecordName);
                        if (!string.IsNullOrEmpty(processedHtml)) {
                            EmailTemplateModel emailTemplate;
                            if (!string.IsNullOrWhiteSpace(emailTemplateRecordGuid)) {
                                //
                                // -- guid provided, get-or-create by guid
                                emailTemplate = DbBaseModel.verify<EmailTemplateModel>(core.cpParent, emailTemplateRecordGuid);
                                emailTemplate.name = emailTemplateRecordName;
                            } else {
                                //
                                // -- no guid, fall back to name-based lookup
                                emailTemplate = DbBaseModel.createByUniqueName<EmailTemplateModel>(core.cpParent, emailTemplateRecordName);
                                if (emailTemplate == null) {
                                    emailTemplate = DbBaseModel.addDefault<EmailTemplateModel>(core.cpParent);
                                    emailTemplate.name = emailTemplateRecordName;
                                }
                            }
                            emailTemplate.bodyHTML = processedHtml;
                            emailTemplate.modifiedDate = DateTime.Now;
                            emailTemplate.save(core.cpParent);
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], imported email template [{emailTemplateRecordName}] from layout file [{htmlFilename}]");
                        }
                    }
                    //
                    // -- save email record
                    if (!string.IsNullOrWhiteSpace(emailRecordName)) {
                        var ignoreErrors = new List<string>();
                        string processedHtml = ImportController.processHtml(core.cpParent, htmlContent, ImporttypeEnum.Eamil, ref ignoreErrors, emailRecordName);
                        if (!string.IsNullOrEmpty(processedHtml)) {
                            EmailModel email;
                            if (!string.IsNullOrWhiteSpace(emailRecordGuid)) {
                                //
                                // -- guid provided, get-or-create by guid
                                email = DbBaseModel.verify<EmailModel>(core.cpParent, emailRecordGuid);
                                email.name = emailRecordName;
                            } else {
                                //
                                // -- no guid, fall back to name-based lookup
                                email = DbBaseModel.createByUniqueName<EmailModel>(core.cpParent, emailRecordName);
                                if (email == null) {
                                    email = DbBaseModel.addDefault<EmailModel>(core.cpParent);
                                    email.name = emailRecordName;
                                }
                            }
                            email.copyFilename.content = processedHtml;
                            email.modifiedDate = DateTime.Now;
                            email.save(core.cpParent);
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], imported email [{emailRecordName}] from layout file [{htmlFilename}]");
                        }
                    }
                } catch (Exception ex) {
                    //
                    // -- log but do not fail the collection install
                    logger.Error(ex, $"{core.logCommonMessage}, CollectionName [{collectionName}], error importing layout file [{htmlFilename}] by meta tags. The installation will continue.");
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Save the resource manifest and clean up orphaned files from a previous version.
        /// Loads the old manifest, saves the new one, then deletes any files and empty folders
        /// that were in the old manifest but not in the new one.
        /// </summary>
        internal static void saveManifestAndCleanupOrphans(CoreController core, string collectionName, string collectionGuid, string collectionVersionFolder, ResourceManifestModel resourceManifest) {
            //
            // -- load old manifest before saving new one
            var oldManifest = ResourceManifestModel.load(core, collectionVersionFolder);
            //
            // -- save the new manifest
            ResourceManifestModel.save(core, collectionVersionFolder, resourceManifest);
            //
            // -- delete orphaned files from the previous version
            if (oldManifest != null && oldManifest.resources != null) {
                var newPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in resourceManifest.resources) {
                    newPaths.Add($"{entry.type}::{entry.destinationPath}");
                }
                foreach (var oldEntry in oldManifest.resources) {
                    if (!newPaths.Contains($"{oldEntry.type}::{oldEntry.destinationPath}")) {
                        switch (oldEntry.type.ToLowerInvariant()) {
                            case "www":
                                core.wwwFiles.deleteFile(oldEntry.destinationPath);
                                break;
                            case "private":
                                core.privateFiles.deleteFile(oldEntry.destinationPath);
                                break;
                            case "cdn":
                                core.cdnFiles.deleteFile(oldEntry.destinationPath);
                                break;
                            case "helpfiles":
                            case "layout-private":
                                core.privateFiles.deleteFile(oldEntry.destinationPath);
                                break;
                            case "layout-www":
                                core.wwwFiles.deleteFile(oldEntry.destinationPath);
                                break;
                        }
                        logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], deleted orphaned resource [{oldEntry.type}::{oldEntry.destinationPath}]");
                    }
                }
            }
            //
            // -- delete orphaned folders from the previous version (only if empty)
            if (oldManifest != null && oldManifest.folders != null) {
                var newFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in resourceManifest.folders) {
                    newFolders.Add($"{entry.type}::{entry.folderPath}");
                }
                // -- process in reverse so deepest subfolders are checked first
                for (int i = oldManifest.folders.Count - 1; i >= 0; i--) {
                    var oldFolder = oldManifest.folders[i];
                    if (!newFolders.Contains($"{oldFolder.type}::{oldFolder.folderPath}")) {
                        FileController fileSystem = oldFolder.type.ToLowerInvariant() switch {
                            "www" => core.wwwFiles,
                            "private" => core.privateFiles,
                            "cdn" => core.cdnFiles,
                            "helpfiles" => core.privateFiles,
                            "layout-private" => core.privateFiles,
                            "layout-www" => core.wwwFiles,
                            _ => null
                        };
                        if (fileSystem != null && fileSystem.getFileList(oldFolder.folderPath).Count == 0 && fileSystem.getFolderList(oldFolder.folderPath).Count == 0) {
                            fileSystem.deleteFolder(oldFolder.folderPath);
                            logger.Info($"{core.logCommonMessage}, CollectionName [{collectionName}], GUID [{collectionGuid}], deleted orphaned empty folder [{oldFolder.type}::{oldFolder.folderPath}]");
                        }
                    }
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Unzip a help file zip into a temp folder, prefix extracted files with the help prefix,
        /// then copy each file to the destination. New files are added to the manifest.
        /// </summary>
        internal static void unzipHelpFilesToTempThenCopy(CoreController core, string dstPath, string zipPathFilename, string helpPrefix, ResourceManifestModel resourceManifest ) {
            string tempPath = $"installHelpZip{GenericController.getRandomInteger()}\\";
            try {
                core.tempFiles.createPath(tempPath);
                //
                // -- copy the zip to temp and extract there
                string zipFilename = System.IO.Path.GetFileName(zipPathFilename);
                core.privateFiles.copyFile(zipPathFilename, tempPath + zipFilename, core.tempFiles);
                core.tempFiles.unzipFile(tempPath + zipFilename);
                core.tempFiles.deleteFile(tempPath + zipFilename);
                //
                // -- prefix all extracted files in temp with the help prefix
                prefixTempHelpFiles(core, tempPath, helpPrefix);
                //
                // -- copy from temp to destination, tracking new files in manifest
                ResourceManifestModel.copyTempToDestRecursively(core, core.privateFiles, tempPath, dstPath, "helpfiles", resourceManifest, alwaysAddToManifest: true);
            } finally {
                core.tempFiles.deleteFolder(tempPath);
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Recursively prefix all files in a temp helpFiles folder with the help prefix.
        /// Since all files in temp are newly extracted, no existing-files check is needed.
        /// </summary>
        internal static void prefixTempHelpFiles(CoreController core, string folderPath, string helpPrefix) {
            string prefix = $"{helpPrefix}.";
            foreach (var extractedFile in core.tempFiles.getFileList(folderPath)) {
                if (extractedFile.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { continue; }
                string prefixedName = $"{prefix}{extractedFile.Name}";
                core.tempFiles.copyFile(folderPath + extractedFile.Name, folderPath + prefixedName);
                core.tempFiles.deleteFile(folderPath + extractedFile.Name);
            }
            foreach (var subFolder in core.tempFiles.getFolderList(folderPath)) {
                prefixTempHelpFiles(core, $"{folderPath}{subFolder.Name}\\", helpPrefix);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Return the prefix used for helpfile names. The Base5 collection uses "Contensive"
        /// instead of the collection name so help files are branded consistently.
        /// </summary>
        private static string getHelpFilePrefix(string collectionName) {
            if (collectionName.Equals("Base5", StringComparison.OrdinalIgnoreCase)) {
                return "Contensive";
            }
            return collectionName;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
