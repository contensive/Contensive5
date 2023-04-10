
using Contensive.BaseClasses;
using Contensive.Models.Db;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Contensive.BaseClasses.CPLayoutBaseClass;

namespace Contensive.Processor {
    namespace Controllers {
        public static class ImportController {
            //
            //====================================================================================================
            /// <summary>
            /// Import a file and process the html. Save the result.
            /// </summary>
            /// <param name="cp"></param>
            /// <param name="htmlSourceTempPathFilename"></param>
            /// <param name="importTypeId"></param>
            /// <param name="layoutId">If not 0, the imported html will be saved to the record in this table.</param>
            /// <param name="pageTemplateId">If not 0, the imported html will be saved to the record in this table.</param>
            /// <param name="emailTemplateId">If not 0, the imported html will be saved to the record in this table.</param>
            /// <param name="emailId">If not 0, the imported html will be saved to the record in this table.</param>
            /// <param name="userMessageList">If there were any processing errors caused by the data, return them here. These can be presented to the user.</param>
            /// <returns></returns>
            public static bool processImportFile(CPBaseClass cp, string htmlSourceTempPathFilename, CPLayoutBaseClass.ImporttypeEnum importTypeId, int layoutId, int pageTemplateId, int emailTemplateId, int emailId, ref List<string> userMessageList) {
                try {
                    if (System.IO.Path.GetExtension(htmlSourceTempPathFilename).Equals(".zip")) {
                        //
                        // -- upload file is a zip and unzip to temp and copy assets to wwwroot
                        cp.TempFiles.UnzipFile(htmlSourceTempPathFilename);
                        cp.TempFiles.DeleteFile(htmlSourceTempPathFilename);
                        //
                        // -- copy non-html files to wwwroot
                        copyNonHtmlFilesToWWW(cp, cp.TempFiles.GetPath(htmlSourceTempPathFilename), "");
                    }
                    //
                    // -- import all html files in the root folder
                    string tempPath = cp.TempFiles.GetPath(htmlSourceTempPathFilename);
                    bool htmlFileFound = false;
                    foreach (var file in cp.TempFiles.FileList(tempPath)) {
                        if (file.Extension.ToLowerInvariant().Equals(".html")) {
                            //
                            // -- process each html file one at a time
                            userMessageList.Add("Processing file " + cp.CdnFiles.GetFilename(file.Name));
                            string newRecordName = Path.GetFileNameWithoutExtension(file.Name);
                            htmlFileFound = true;
                            HtmlDocument htmlDoc = new HtmlDocument();
                            htmlDoc.Load(cp.TempFiles.PhysicalFilePath + tempPath + file.Name, Encoding.UTF8);
                            if (htmlDoc == null) {
                                //
                                // -- body tag not found, import the whole document
                                userMessageList.Add("The file is empty.");
                                return false;
                            }
                            //
                            // -- get record names for each import type
                            string layoutRecordName = string.Empty;
                            string pageTemplateRecordName = string.Empty;
                            string emailTemplateRecordName = string.Empty;
                            string emailRecordName = string.Empty;
                            //
                            // -- search for meta name=template|layout content=recordaname
                            var metadataList = htmlDoc.DocumentNode.SelectNodes("//meta");
                            if (metadataList != null) {
                                foreach (var metadataNode in metadataList) {
                                    switch (metadataNode.GetAttributeValue("name", String.Empty).ToLowerInvariant()) {
                                        case "layout": {
                                                layoutRecordName = metadataNode.GetAttributeValue("content", String.Empty);
                                                break;
                                            }
                                        case "template":
                                        case "pagetemplate": {
                                                pageTemplateRecordName = metadataNode.GetAttributeValue("content", String.Empty);
                                                break;
                                            }
                                        case "emailtemplate": {
                                                emailTemplateRecordName = metadataNode.GetAttributeValue("content", String.Empty);
                                                break;
                                            }
                                        case "email": {
                                                emailRecordName = metadataNode.GetAttributeValue("content", String.Empty);
                                                break;
                                            }
                                        default: {
                                                break;
                                            }
                                    }
                                }
                            }
                            //
                            // -- process the document html
                            if (!processHtmlDoc(cp, htmlDoc, importTypeId, ref userMessageList)) {
                                return false;
                            }
                            //
                            // -- save manual layout
                            LayoutModel layout = null;
                            {
                                if (importTypeId.Equals(ImporttypeEnum.LayoutForAddon) && layoutId.Equals(0) & string.IsNullOrWhiteSpace(layoutRecordName)) {
                                    //
                                    // -- layout type but no layout selected, and no layout imported, use filename
                                    layoutRecordName = newRecordName;
                                }
                                if (importTypeId.Equals(ImporttypeEnum.LayoutForAddon) && !layoutId.Equals(0)) {
                                    layout = DbBaseModel.create<LayoutModel>(cp, layoutId);
                                    if (layout == null) {
                                        userMessageList.Add("The layout selected could not be found.");
                                        return false;
                                    }
                                    if (cp.Site.GetInteger("html platform version") == 5) {
                                        layout.layoutPlatform5.content = htmlDoc.DocumentNode.OuterHtml;
                                    } else {
                                        layout.layout.content = htmlDoc.DocumentNode.OuterHtml;
                                    }
                                    layout.save(cp);
                                }
                                //
                                // -- save meta layout
                                if ((layout == null) && !string.IsNullOrWhiteSpace(layoutRecordName)) {
                                    layout = DbBaseModel.createByUniqueName<LayoutModel>(cp, layoutRecordName);
                                    if (layout == null) {
                                        layout = DbBaseModel.addDefault<LayoutModel>(cp);
                                        layout.name = layoutRecordName;
                                    }
                                    //
                                    // -- check site property, not cp.site.htmlPlatformVersion because this tool runs in the admin site, which may overwrite the public property
                                    if (cp.Site.GetInteger("html platform version") == 5) {
                                        layout.layoutPlatform5.content = htmlDoc.DocumentNode.OuterHtml;
                                    } else {
                                        layout.layout.content = htmlDoc.DocumentNode.OuterHtml;
                                    }
                                    layout.save(cp);
                                    userMessageList.Add("Saved Layout '" + layoutRecordName + "'.");
                                }
                            }
                            //
                            // -- save page template
                            PageTemplateModel pageTemplate = null;
                            {
                                if (importTypeId.Equals(ImporttypeEnum.PageTemplate) && pageTemplateId.Equals(0) & string.IsNullOrWhiteSpace(pageTemplateRecordName)) {
                                    //
                                    // -- layout type but no layout selected, and no layout imported, use filename
                                    pageTemplateRecordName = newRecordName;
                                }
                                if (importTypeId.Equals(ImporttypeEnum.PageTemplate) && !pageTemplateId.Equals(0)) {
                                    pageTemplate = DbBaseModel.create<PageTemplateModel>(cp, pageTemplateId);
                                    if (pageTemplate == null) {
                                        userMessageList.Add("The template selected could not be found.");
                                        return false;
                                    }
                                    pageTemplate.bodyHTML = htmlDoc.DocumentNode.OuterHtml;
                                    pageTemplate.save(cp);
                                    userMessageList.Add("Saved Page Template '" + pageTemplateRecordName + "'.");
                                }
                                //
                                // -- save meta template
                                if ((pageTemplate == null) && !string.IsNullOrWhiteSpace(pageTemplateRecordName)) {
                                    pageTemplate = DbBaseModel.createByUniqueName<PageTemplateModel>(cp, pageTemplateRecordName);
                                    if (pageTemplate == null) {
                                        pageTemplate = DbBaseModel.addDefault<PageTemplateModel>(cp);
                                        pageTemplate.name = pageTemplateRecordName;
                                    }
                                    //
                                    // -- try to resolve the various relative urls possible into the primary url, then to a reoot relative url
                                    //string urlProtocolDomainSlash = "https://" + cp.Site.DomainPrimary + "/";
                                    string bodyhtml = htmlDoc.DocumentNode.OuterHtml;
                                    //
                                    pageTemplate.bodyHTML = bodyhtml;
                                    pageTemplate.save(cp);
                                    userMessageList.Add("Saved Page Template '" + pageTemplateRecordName + "'.");
                                }
                            }
                            //
                            // -- save email template
                            EmailTemplateModel emailTemplate = null;
                            {
                                if (importTypeId.Equals(ImporttypeEnum.EmailTemplate) && emailTemplateId.Equals(0) & string.IsNullOrWhiteSpace(emailTemplateRecordName)) {
                                    //
                                    // --  type but no layout selected, and no layout imported, use filename
                                    emailTemplateRecordName = newRecordName;
                                }
                                if (importTypeId.Equals(ImporttypeEnum.EmailTemplate) && !emailTemplateId.Equals(0)) {
                                    emailTemplate = DbBaseModel.create<EmailTemplateModel>(cp, emailTemplateId);
                                    if (emailTemplate == null) {
                                        userMessageList.Add("The template selected could not be found.");
                                        return false;
                                    }
                                    emailTemplate.bodyHTML = htmlDoc.DocumentNode.OuterHtml;
                                    emailTemplate.save(cp);
                                }
                                //
                                // -- save meta template
                                if ((emailTemplate == null) && !string.IsNullOrWhiteSpace(emailTemplateRecordName)) {
                                    emailTemplate = DbBaseModel.createByUniqueName<EmailTemplateModel>(cp, emailTemplateRecordName);
                                    if (emailTemplate == null) {
                                        emailTemplate = DbBaseModel.addDefault<EmailTemplateModel>(cp);
                                        emailTemplate.name = emailTemplateRecordName;
                                    }
                                    emailTemplate.bodyHTML = htmlDoc.DocumentNode.OuterHtml;
                                    emailTemplate.save(cp);
                                    userMessageList.Add("Saved Email Template '" + emailTemplateRecordName + "'.");
                                }
                            }
                            //
                            // -- save email
                            EmailModel email = null;
                            {
                                if (importTypeId.Equals(5) && emailId.Equals(0) & string.IsNullOrWhiteSpace(emailRecordName)) {
                                    //
                                    // -- layout type but no layout selected, and no layout imported, use filename
                                    emailRecordName = newRecordName;
                                }
                                if (!emailId.Equals(0)) {
                                    email = DbBaseModel.create<EmailModel>(cp, emailId);
                                    if (email == null) {
                                        userMessageList.Add("The email selected could not be found.");
                                        return false;
                                    }
                                    email.copyFilename.content = htmlDoc.DocumentNode.OuterHtml;
                                    email.save(cp);
                                    userMessageList.Add("Saved Email '" + emailRecordName + "'.");
                                }
                                //
                                // -- save meta template
                                if ((email == null) && !string.IsNullOrWhiteSpace(emailRecordName)) {
                                    email = DbBaseModel.createByUniqueName<EmailModel>(cp, emailRecordName);
                                    if (email == null) {
                                        email = DbBaseModel.addDefault<EmailModel>(cp);
                                        email.name = emailRecordName;
                                    }
                                    email.copyFilename.content = htmlDoc.DocumentNode.OuterHtml;
                                    email.save(cp);
                                    userMessageList.Add("Saved Email '" + emailRecordName + "'.");
                                }
                            }
                        }
                    }
                    if (!htmlFileFound) {
                        userMessageList.Add("No files were found with .HTML extension. Only files with .HTML extensions are imported.");
                        return false;
                    }
                    return true;
                } catch (Exception ex) {
                    cp.Site.ErrorReport(ex);
                    throw;
                }
            }
            //
            //====================================================================================================
            /// <summary>
            /// Process and return an html string. Any processing errors are returned in userMessageList. If UserMessageList.count is 0, there wre no errors.
            /// </summary>
            /// <param name="cp"></param>
            /// <param name="html"></param>
            /// <param name="importTypeId"></param>
            /// <param name="userMessageList"></param>
            /// <returns></returns>
            public static string  processHtml(CPBaseClass cp, string html, ImporttypeEnum importTypeId, ref List<string> userMessageList) {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml( html );
                if (htmlDoc == null) {
                    //
                    // -- body tag not found, import the whole document
                    userMessageList.Add("The file is empty.");
                    return "";
                }
                processHtmlDoc(cp, htmlDoc, importTypeId, ref userMessageList);
                return htmlDoc.DocumentNode.OuterHtml;
            }
            //
            //====================================================================================================
            /// <summary>
            /// Process and return an htmlagility object. Any processing errors are returned in userMessageList. If UserMessageList.count is 0, there wre no errors.
            /// </summary>
            /// <param name="cp"></param>
            /// <param name="htmlDoc"></param>
            /// <param name="importTypeId"></param>
            /// <param name="userMessageList"></param>
            /// <returns></returns>
            private static bool processHtmlDoc(CPBaseClass cp, HtmlDocument htmlDoc, ImporttypeEnum importTypeId, ref List<string> userMessageList) {
                //
                // -- get body (except email template because it uses the full html document
                if (importTypeId != ImporttypeEnum.EmailTemplate) {
                    //
                    // -- find the data-body or body tag
                    {
                        string xPath = "//*[@data-body]";
                        HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                        if (nodeList != null) {
                            //
                            // -- import data-body
                            userMessageList.Add("Body set by data-body attribute.");
                            htmlDoc.LoadHtml(nodeList.First().InnerHtml);
                        } else {
                            HtmlNode bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
                            if (bodyNode == null) {
                                //
                                // -- no body found, use entire document
                                userMessageList.Add("No Body found, entire document imported.");
                            } else {
                                //
                                // -- use body
                                string body = bodyNode.InnerHtml;
                                if (string.IsNullOrWhiteSpace(body)) {
                                    //
                                    // -- body tag not found, import the whole document
                                    userMessageList.Add("The content does not include a data-body attribute and the body tag is empty.");
                                    return false;
                                }
                                //
                                // -- body found, set the htmlDoc to the body
                                userMessageList.Add("Html Body imported.");
                                htmlDoc.LoadHtml(body);
                            }
                        }
                    }
                }
                //
                // -- process data-layout nodes. Within each node found, run all other controllers inddividually then save
                DataLayoutController.process(cp, htmlDoc, ref userMessageList);
                //
                // -- process the body
                MustacheSectionController.process(htmlDoc);
                MustacheInvertedSectionController.process(htmlDoc);
                MustacheTruthyController.process(htmlDoc);
                //
                DataDeleteController.process(htmlDoc);
                DataAddonController.process(cp, htmlDoc);
                DataHrefController.process(htmlDoc);
                DataSrcController.process(htmlDoc);
                DataAltController.process(htmlDoc);
                DataValueController.process(htmlDoc);
                DataInnerTextController.process(htmlDoc);
                //
                // -- legacy, not advertized
                MustacheValueController.process(htmlDoc);
                MustacheVariableController.process(htmlDoc);
                //
                return true;

            }
            //
            // ====================================================================================================
            /// <summary>
            /// copy all files from tempPath to wwwPath, except html files in the root.
            /// </summary>
            /// <param name="cp"></param>
            /// <param name="tempPath"></param>
            /// <param name="wwwPath"></param>
            private static void copyNonHtmlFilesToWWW(CPBaseClass cp, string tempPath, string wwwPath) {
                try {
                    foreach (var folder in cp.TempFiles.FolderList(tempPath)) {
                        string folderPath = folder.Name + "\\";
                        cp.WwwFiles.CreateFolder(wwwPath + folderPath);
                        copyNonHtmlFilesToWWW(cp, tempPath + folderPath, wwwPath + folderPath);
                    }
                    foreach (var file in cp.TempFiles.FileList(tempPath)) {
                        if (!string.IsNullOrEmpty(wwwPath) || !file.Extension.ToLowerInvariant().Equals(".html")) {
                            cp.TempFiles.Copy(tempPath + file.Name, wwwPath + file.Name, cp.WwwFiles);
                        }
                    }
                } catch (Exception ex) {
                    cp.Site.ErrorReport(ex);
                    throw;
                }
            }
        }
    }
}
