﻿
using System;
using System.IO;
using System.Linq;
using Contensive.Processor;
using System.Security.AccessControl;
using System.Security.Principal;
using Contensive.Processor.Models.Domain;
using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;
using System.Collections.Generic;
using Contensive.CLI.Controllers;
using NLog;
using System.Text.RegularExpressions;
using System.Net.Mail;

namespace Contensive.CLI {
    static class NewAppCmd {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--newapp (-n) [appName] [primary domain name]"
            + Environment.NewLine + "    Create a new application (website) on this server. If appName is omitted this command acts as a wizard and prompts for all options. If primary domain name is omitted, the domain www.appname.com will be used.";
        //
        // ====================================================================================================
        /// <summary>
        /// create a new app. If appname is provided, create the app with defaults. if not appname, prompt for defaults
        /// </summary>
        /// <param name="appName"></param>
        public static async System.Threading.Tasks.Task executeAsync(string appName, string domainName) {
            try {
                const string iisDefaultDoc = "default.aspx";
                //
                string allowableNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                if (!appName.All(x => allowableNameCharacters.Contains(x))) {
                    Console.Write("\nThis application name is not valid because it must contain only letters and numbers.");
                    return;
                }
                string defaultEmailAddress = "";
                //
                using (CPClass cp = new CPClass()) {
                    defaultEmailAddress = cp.ServerConfig.defaultEmailContact;
                    //
                    if (!cp.serverOk) {
                        Console.WriteLine("The Server Group does not appear to be configured correctly. Please run --configure");
                        return;
                    }
                    //
                    // -- verify defaultaspxsite.zip
                    if (!cp.core.programFiles.fileExists("defaultaspxsite.zip")) {
                        Console.WriteLine($"To build a new site, the DefaultAspxSite.zip must be downloaded from contensive.io/downloads to the program files folder, {cp.core.programFiles.localAbsRootPath}");
                        Console.ReadLine();
                        return;
                    }
                    //
                    // -- make sure this app does not already exist
                    if (cp.GetAppNameList().Contains(appName, StringComparer.OrdinalIgnoreCase)) {
                        Console.WriteLine("The application name you seleted is in use [" + appName + "]");
                        return;
                    }
                    //
                    // -- verify program files folder
                    string currentPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    if (!cp.core.serverConfig.programFilesPath.Equals(currentPath)) {
                        cp.core.serverConfig.programFilesPath = currentPath;
                        cp.core.serverConfig.save(cp.core);
                    }
                    //
                    // -- create the new app
                    Console.Write("\n\nCreate an application within the server group [" + cp.core.serverConfig.name + "]. The application name can contain upper and lower case alpha numeric characters with no spaces or special characters. This name will be used to create and identity resources and should not be changed later.");
                    AppConfigModel appConfig = new AppConfigModel {
                        //
                        // -- enable it
                        enabled = true,
                        //
                        // -- private key
                        privateKey = Processor.Controllers.GenericController.getGUIDNaked(),
                        //
                        // -- allow site monitor
                        allowSiteMonitor = false
                    };
                    //
                    // -- if you get a cluster object from cp with a key, and the key gives you access, you have a cluster object to create an app
                    bool promptForArguments = string.IsNullOrWhiteSpace(appName);
                    //
                    // -- create app
                    if (promptForArguments) {
                        //
                        // -- app name
                        while (true) {
                            Type myType = typeof(Processor.Controllers.CoreController);
                            Assembly myAssembly = Assembly.GetAssembly(myType);
                            AssemblyName myAssemblyname = myAssembly.GetName();
                            Version myVersion = myAssemblyname.Version;
                            DateTime rightNow = DateTime.Now;
                            string appNameDefault = "app" + Contensive.Processor.Controllers.GenericController.getDateTimeNumberString(rightNow);
                            Console.Write("\n\nEnter your application name. It must start with a letter and contain only letters and numbers.");
                            appName = GenericController.promptForReply("\nApplication Name:", appNameDefault).ToLowerInvariant();
                            if (string.IsNullOrWhiteSpace(appName)) {
                                Console.Write("\nThis application name is not valid because it cannot be blank.");
                                continue;
                            }
                            if (!char.IsLetter(appName, 0)) {
                                Console.Write("\nThis application name is not valid because it must start with a letter.");
                                continue;
                            }
                            if (!appName.All(x => allowableNameCharacters.Contains(x))) {
                                Console.Write("\nThis application name is not valid because it must contain only letters and numbers.");
                                continue;
                            }
                            if (cp.core.serverConfig.apps.ContainsKey(appName.ToLowerInvariant())) {
                                Console.Write("\nThere is already an application with this name. To get the current server configuration, use cc -s");
                                continue;
                            }
                            break;
                        }
                    }
                    //
                    // -- verify current app not already in server group
                    if (!string.IsNullOrEmpty(appName) && cp.core.serverConfig.apps.ContainsKey(appName)) {
                        Console.WriteLine("The application [" + appName + "] aleady exists. To upgrade this app, use --upgrade. To remove this site and create a new site, use --delete first.");
                        return;
                    }
                    appConfig.name = appName;
                    //
                    // -- admin route
                    appConfig.adminRoute = "admin";
                    if (promptForArguments) {
                        bool routeOk = false;
                        do {
                            appConfig.adminRoute = GenericController.promptForReply("Admin Route (non-blank, no leading or trailing slash)", appConfig.adminRoute);
                            appConfig.adminRoute = Contensive.Processor.Controllers.FileController.convertToUnixSlash(appConfig.adminRoute);
                            if (!string.IsNullOrEmpty(appConfig.adminRoute)) {
                                if (!appConfig.adminRoute.Substring(0, 1).Equals("/")) {
                                    if (!appConfig.adminRoute.Substring(appConfig.adminRoute.Length - 1, 1).Equals("/")) {
                                        routeOk = true;
                                    }
                                }
                            }
                        } while (!routeOk);
                    }
                    //
                    // -- domain
                    domainName = "www." + appConfig.name + ".com";
                    while (promptForArguments) {
                        domainName = GenericController.promptForReply("Primary Domain Name", domainName);
                        domainName = normalizeDomain(domainName);
                        if (IsValidDomain(domainName)) {
                            break;
                        } else {
                            Console.WriteLine("The primary domain name is not valid. It must be a valid domain name, such as www.example.com.");
                        }
                    }
                    appConfig.domainList.Clear();
                    appConfig.domainList.Add(domainName);
                    //
                    // -- admin email address / default fron-address, save to site properties after build
                    // create email address for admin@ the domain name of the website
                    if (string.IsNullOrEmpty(defaultEmailAddress)) {
                        defaultEmailAddress = "admin@" + getNakedDomain(domainName);
                    }
                    if (promptForArguments) {
                        defaultEmailAddress = GenericController.promptForReply("Admin email address (required for email from-address)", defaultEmailAddress);
                    }
                    if (string.IsNullOrEmpty(cp.ServerConfig.defaultEmailContact)) {
                        cp.ServerConfig.defaultEmailContact = defaultEmailAddress;
                    }
                    //
                    // -- file architectur
                    if (!promptForArguments) {
                        if (cp.core.serverConfig.isLocalFileSystem) {
                            //
                            // -- no prompts, local file system
                            appConfig.localWwwPath = cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\www\\";
                            appConfig.localFilesPath = cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\files\\";
                            appConfig.localPrivatePath = cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\private\\";
                            appConfig.localTempPath = cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\temp\\";
                            appConfig.remoteWwwPath = "";
                            appConfig.remoteFilePath = "";
                            appConfig.remotePrivatePath = "";
                            appConfig.cdnFileUrl = "/" + appConfig.name + "/files/";
                        } else {
                            //
                            // -- no prompts, remote file system
                            appConfig.localWwwPath = cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\www\\";
                            appConfig.localFilesPath = cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\files\\";
                            appConfig.localPrivatePath = cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\private\\";
                            appConfig.localTempPath = cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\temp\\";
                            appConfig.remoteWwwPath = "/" + appConfig.name + "/www/";
                            appConfig.remoteFilePath = "/" + appConfig.name + "/files/";
                            appConfig.remotePrivatePath = "/" + appConfig.name + "/private/";
                            appConfig.cdnFileUrl = "https://s3.amazonaws.com/" + cp.core.serverConfig.awsBucketName + "/" + appConfig.name + "/files/";
                        }
                    } else {
                        if (cp.core.serverConfig.isLocalFileSystem) {
                            //
                            // Server is local file Mode, compatible with v4.1, cdn in appRoot folder as /" + appConfig.name + "/files/
                            //
                            Console.Write("\nThe Server Group is configured for a local filesystem (Local Mode, scale-up architecture. Files are stored and accessed on the local server.)");
                            appConfig.localWwwPath = GenericController.promptForReply("\napp files", cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\www\\");
                            appConfig.localFilesPath = GenericController.promptForReply("cdn files", cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\files\\");
                            appConfig.localPrivatePath = GenericController.promptForReply("private files", cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\private\\");
                            appConfig.localTempPath = GenericController.promptForReply("temp files (ephemeral storage)", cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\temp\\");
                            appConfig.remoteWwwPath = "";
                            appConfig.remoteFilePath = "";
                            appConfig.remotePrivatePath = "";
                            appConfig.cdnFileUrl = GenericController.promptForReply("files Url (typically a virtual path on the application website)", "/" + appConfig.name + "/files/");
                        } else {
                            //
                            // Server is remote file mode
                            //
                            Console.Write("\nThe Server Group is configured for a remote filesystem.");
                            appConfig.localWwwPath = GenericController.promptForReply("\napp files (local mirror)", cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\www\\");
                            appConfig.localFilesPath = GenericController.promptForReply("cdn files (local mirror)", cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\files\\");
                            appConfig.localPrivatePath = GenericController.promptForReply("private files (local mirror)", cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\private\\");
                            appConfig.localTempPath = GenericController.promptForReply("temp files (local only storage)", cp.core.serverConfig.localDataDriveLetter + ":\\inetpub\\" + appConfig.name + "\\temp\\");
                            appConfig.remoteWwwPath = GenericController.promptForReply("AWS S3 folder for app www storage", "/" + appConfig.name + "/www/");
                            appConfig.remoteFilePath = GenericController.promptForReply("AWS S3 folder for cdn file storage", "/" + appConfig.name + "/files/");
                            appConfig.remotePrivatePath = GenericController.promptForReply("AWS S3 folder for private file storage", "/" + appConfig.name + "/private/");
                            appConfig.cdnFileUrl = GenericController.promptForReply("files Url (typically a public folder in CDN website)", "https://s3.amazonaws.com/" + cp.core.serverConfig.awsBucketName + "/" + appConfig.name + "/files/");
                        }
                    }
                    //
                    // -- configure local folders
                    logger.Info($"{cp.core.logCommonMessage},Create local folders.");
                    setupDirectory(appConfig.localWwwPath);
                    setupDirectory(appConfig.localFilesPath);
                    setupDirectory(appConfig.localPrivatePath);
                    setupDirectory(appConfig.localTempPath);
                    //
                    // -- configure remote folders
                    AmazonS3Client s3client = null;
                    if (!cp.core.serverConfig.isLocalFileSystem) {
                        //
                        // -- update the server's bucket policy to make the remoteFilesPath public
                        string policyString = "";
                        try {
                            Amazon.RegionEndpoint region = Amazon.RegionEndpoint.GetBySystemName(cp.core.serverConfig.awsRegionName);
                            s3client = new(cp.core.secrets.awsAccessKey, cp.core.secrets.awsSecretAccessKey, region);
                        } catch (Exception ex) {
                            cp.Log.Error(ex, "NewAppCmd, error getting S3 client.");
                            //
                            Console.WriteLine($"\nError getting S3 client, {ex.Message}");
                            Console.ReadLine();
                        }
                        try {
                            //
                            // -- get current policy for this bucket
                            GetBucketPolicyRequest getRequest = new() {
                                BucketName = cp.core.serverConfig.awsBucketName
                            };
                            GetBucketPolicyResponse getResponse = await s3client.GetBucketPolicyAsync(getRequest);
                            policyString = getResponse.Policy;
                        } catch (Exception ex) {
                            cp.Log.Error(ex, "NewAppCmd, error getting S3 policy.");
                            //
                            Console.WriteLine($"\nError creating AWS S3 Policy, {ex.Message}");
                            Console.ReadLine();
                        }
                        try {

                            AwsBucketPolicy policy;
                            if (string.IsNullOrEmpty(policyString)) {
                                //
                                // -- no current policy, create base policy
                                policy = new() {
                                    Version = "2012-10-17"
                                };
                            } else {
                                //
                                // -- add to current policy
                                policy = cp.JSON.Deserialize<AwsBucketPolicy>(policyString);
                            }
                            policy.Statement.Add(new AwsBucketPolicyStatement {
                                Action = "s3:GetObject",
                                Effect = "Allow",
                                Principal = "*",
                                Resource = "arn:aws:s3:::" + cp.core.serverConfig.awsBucketName + appConfig.remoteFilePath + "*",
                                Sid = "AllowPublicRead"
                            });
                            string policyJson = cp.JSON.Serialize(policy);
                            PutBucketPolicyRequest putRequest = new() {
                                BucketName = cp.core.serverConfig.awsBucketName,
                                Policy = policyJson
                            };
                            PutBucketPolicyResponse putResponse = await s3client.PutBucketPolicyAsync(putRequest);
                            Console.Write("\nAWS S3 Policy Applied:" + policyJson);
                        } catch (Exception ex) {
                            cp.Log.Error(ex, "NewAppCmd, error creating S3 policy, continue.");
                            //
                            Console.WriteLine($"\nError creating AWS S3 Policy, {ex.Message}");
                            Console.ReadLine();
                        }
                    }
                    //
                    // -- save the app configuration and reload the server using this app
                    logger.Info($"{cp.core.logCommonMessage},Save app configuration.");
                    appConfig.appStatus = AppConfigModel.AppStatusEnum.maintenance;
                    cp.core.serverConfig.apps.Add(appConfig.name, appConfig);
                    cp.core.serverConfig.save(cp.core);
                    cp.core.serverConfig = ServerConfigModel.create(cp.core);
                    cp.core.appConfig = AppConfigModel.getObject(cp.core, cp.core.serverConfig, appConfig.name);
                    // 
                    // update local host file
                    //
                    try {
                        logger.Info($"{cp.core.logCommonMessage},Update host file to add domain [127.0.0.1 " + appConfig.name + "].");
                        File.AppendAllText("c:\\windows\\system32\\drivers\\etc\\hosts", System.Environment.NewLine + "127.0.0.1\t" + appConfig.name);
                    } catch (Exception ex) {
                        Console.Write("Error attempting to update local host file:" + ex);
                        Console.Write("Please manually add the following line to your host file (c:\\windows\\system32\\drivers\\etc\\hosts):" + "127.0.0.1\t" + appConfig.name);
                    }
                    //
                    // create the database on the server
                    //
                    logger.Info($"{cp.core.logCommonMessage},Create database.");
                    cp.core.dbServer.createCatalog(appConfig.name);
                    //
                    logger.Info($"{cp.core.logCommonMessage},When app creation is complete, use IIS Import Application to install either you web application, or the Contensive DefaultAspxSite.zip application.");
                    //// copy in the pattern files 
                    ////  - the only pattern is aspx
                    ////  - this is cc running, so they are setting up new application which may or may not have a webrole here.
                    ////  - setup a basic webrole just in case this will include one -- maybe later make it an option
                    ////
                    // - logger.Info($"{cp.core.logCommonMessage},Copy default site to www folder.");
                    // - cp.core.programFiles.copyFolder("resources\\DefaultAspxSite\\", "\\", cp.core.appRootFiles);
                }
                //
                // initialize the new app, use the save authentication that was used to authorize this object
                //
                using (CPClass cp = new(appName)) {
                    logger.Info($"{cp.core.logCommonMessage},Verify website.");
                    cp.core.webServer.verifySite(appName, domainName, cp.core.appConfig.localWwwPath);
                    //
                    bool DefaultAspxSiteInstalled = false;
                    logger.Info($"{cp.core.logCommonMessage},Install DefaultAspxSite.");
                    if (!cp.core.programFiles.fileExists(@"\defaultaspxsite.zip")) {
                        //
                        // -- message to install defaultsite manually
                        Console.WriteLine($"File [defaultaspxsite.zip] was not found in the folder [{cp.core.programFiles.localAbsRootPath}\\Contensive]. To setup an IIS website, import this file using IIS Manager from the deployment folder. To automatically install during this process, copy the file into the program files folder.");
                    } else {
                        //
                        // -- install defaultaspxsite
                        DefaultAspxSiteInstalled = true;
                        cp.core.programFiles.copyFile(@"\defaultaspxsite.zip", @"\defaultaspxsite.zip", cp.core.tempFiles);
                        cp.TempFiles.UnzipFile(@"\defaultaspxsite.zip");
                        string srcPath = getZipSrcTempPath(cp, "Content", "Web.config");
                        if (string.IsNullOrWhiteSpace(srcPath)) {
                            Console.WriteLine("The installation on this server does not include a valid DefaultAspxSite.zip file. Replace this file with a valid DefaultAspxSite.zip file or delete the invalid file and retry.");
                            return;
                        }
                        cp.TempFiles.CopyPath(srcPath, @"", cp.WwwFiles);
                        cp.TempFiles.DeleteFile(@"\defaultaspxsite.zip");
                        cp.TempFiles.DeleteFolder(@"content");
                    }
                    // -- 
                    // -- if WebAppSettings.config does not exist, copy WebAppSettings-Sample.config
                    if (!cp.WwwFiles.FileExists("WebAppSettings.config")) {
                        cp.WwwFiles.Copy("WebAppSettings-Sample.config", "WebAppSettings.config");
                    }
                    //
                    // -- if WebRewrite.config does not exist, copy WebRewrite-Sample.config
                    if (!cp.WwwFiles.FileExists("WebRewrite.config")) {
                        cp.WwwFiles.Copy("WebRewrite-Sample.config", "WebRewrite.config");
                    }
                    //
                    logger.Info($"{cp.core.logCommonMessage},Run db upgrade.");
                    Processor.Controllers.Build.BuildController.upgrade(cp.core, true, true);
                    //
                    // -- set the application back to normal mode
                    cp.core.serverConfig.save(cp.core);
                    cp.core.siteProperties.setProperty(Constants.sitePropertyName_ServerPageDefault, iisDefaultDoc);
                    //
                    cp.core.siteProperties.setProperty(Constants.sitePropertyName_EmailAdmin, defaultEmailAddress);
                    cp.core.siteProperties.setProperty(Constants.sitePropertyName_EmailFromAddress, defaultEmailAddress);
                    //
                    logger.Info($"{cp.core.logCommonMessage},Upgrade complete.");
                    if (DefaultAspxSiteInstalled) {
                        logger.Info($"{cp.core.logCommonMessage},A default website was imported into an iis website with this applicaiton name.");
                    } else {
                        logger.Info($"{cp.core.logCommonMessage},The Contensive website was not imported because the file DefaultAspxSite.zip was not found in path [" + cp.core.programFiles.localAbsRootPath + "].");
                    }
                }
                //
            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// create a folder and make it full access for everyone
        /// </summary>
        /// <param name="folderPathPage"></param>
        public static void setupDirectory(string folderPathPage) {
            Directory.CreateDirectory(folderPathPage);
            DirectoryInfo dInfo = new DirectoryInfo(folderPathPage);
            DirectorySecurity dSecurity = dInfo.GetAccessControl();
            dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
            dInfo.SetAccessControl(dSecurity);
        }
        //
        //====================================================================================================
        /// <summary>
        /// recursively search a folder for a target file. If found return its path, else return empty
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="startPath"></param>
        /// <param name="targetFilename"></param>
        /// <returns></returns>
        public static string getZipSrcTempPath(CPClass cp, string startPath, string targetFilename) {
            if (!string.IsNullOrWhiteSpace(startPath)) {
                startPath += (startPath.right(1).Equals(@"/")) ? "" : "/";
            }
            foreach (var file in cp.TempFiles.FileList(startPath)) {
                if (file.Name.Equals(targetFilename, StringComparison.InvariantCultureIgnoreCase)) {
                    //
                    // -- target file found, return this path
                    return startPath;
                }
            }
            foreach (var folder in cp.TempFiles.FolderList(startPath)) {
                string targetPath = getZipSrcTempPath(cp, startPath + folder.Name + @"/", targetFilename);
                if (!string.IsNullOrEmpty(targetPath)) {
                    //
                    // -- target file found in this folder, return this path
                    return targetPath;
                }
            }
            return string.Empty;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Claude
        /// given a domain entered by the user, process it to a standard format.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static string normalizeDomain(string input) {
            // Remove common prefixes if present
            string domain = input.ToLower();

            if (domain.StartsWith("http://"))
                domain = domain.Substring(7);
            else if (domain.StartsWith("https://"))
                domain = domain.Substring(8);

            // Remove trailing slash if present
            if (domain.EndsWith("/"))
                domain = domain.TrimEnd('/');

            // Remove path components (keep only the domain part)
            int slashIndex = domain.IndexOf('/');
            if (slashIndex != -1)
                domain = domain.Substring(0, slashIndex);

            return domain;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Claude
        /// given a domain entered by the user, process it to a standard format.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static string getNakedDomain(string input) {
            // Remove common prefixes if present
            string domain = normalizeDomain(input);
            if (domain.StartsWith("www."))
                domain = domain.Substring(4);
            return domain;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Validate the domain name format.
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        static bool IsValidDomain(string domain) {
            if (string.IsNullOrEmpty(domain))
                return false;

            // Basic domain validation regex
            // Allows letters, numbers, hyphens, and dots
            // Must have at least one dot and valid TLD structure
            string pattern = @"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*\.[a-zA-Z]{2,}$";

            return Regex.IsMatch(domain, pattern) &&
                   domain.Length <= 253 &&
                   !domain.StartsWith("-") &&
                   !domain.EndsWith("-") &&
                   !domain.Contains("..");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Policy used to assign public access to S3 bucket folder
        /// </summary>
        public class AwsBucketPolicy {
            public string Version { get; set; }
            public List<AwsBucketPolicyStatement> Statement { get; set; }
        }
        /// <summary>
        /// Policy used to assign public access to S3 bucket folder
        /// </summary>
        public class AwsBucketPolicyStatement {
            public string Sid { get; set; }
            public string Effect { get; set; }
            public object Principal { get; set; }
            public string Action { get; set; }
            public string Resource { get; set; }
        }
        /// <summary>
        /// Principal, object with AWS property
        /// </summary>
        public class AwsBucketPolicyStatementPrincipalObjString {
            public string AWS { get; set; }
        }
        public class AwsBucketPolicyStatementPrincipalObjListString {
            public List<string> AWS { get; set; }
        }

    }
}
