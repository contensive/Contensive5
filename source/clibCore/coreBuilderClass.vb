﻿
Option Explicit On
Option Strict On

Imports System.Xml
Imports Microsoft.Web.Administration
'
Namespace Contensive.Core
    '
    '====================================================================================================
    ''' <summary>
    ''' code to built and upgrade apps
    ''' not IDisposable - not contained classes that need to be disposed
    ''' </summary>
    Public Class coreBuilderClass
        '
        '====================================================================================================
        '
        Private Structure fieldTypePrivate
            Dim Name As String
            Dim fieldTypePrivate As Integer
        End Structure
        '
        ' ----- objects passed in constructor
        '
        Private cpCore As coreClass
        '
        ' ----- constants
        '
        Private Const UpgradeErrorTheshold As Integer = 100
        '
        ' ----- shared globals
        '
        Friend classLogFolder As String                    ' the folder for logging errors. default="Upgrade", AddonInstall can change it
        '
        ' ----- private globals
        '
        Private ApplicationCollectionLoaded As Boolean
        Private UpgradeErrorCount As Integer
        Private StepCount As Integer
        Private AddonInstallNeeded As Boolean
        '
        '====================================================================================================
        ''' <summary>
        ''' constructor
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <remarks></remarks>
        Public Sub New(cpCore As coreClass)
            MyBase.New()
            Me.cpCore = cpCore
            classLogFolder = "Upgrade"
            ApplicationCollectionLoaded = False
            UpgradeErrorCount = 0
            StepCount = 0
            AddonInstallNeeded = False
        End Sub
        '
        Private Function web_existsSite(appName As String) As Boolean
            Dim returnExists As Boolean = False
            Dim serverManager As New ServerManager
            Dim siteCollection As SiteCollection = serverManager.Sites
            For Each Site As Site In siteCollection
                If appName = Site.Name Then
                    returnExists = True
                    Exit For
                End If
            Next
        End Function
        '
        ' -------------------------------------------------------------------
        ' Create a site and add two bindings, the domain and a 127.0.0.1 for the appName
        ' -------------------------------------------------------------------
        '
        Public Sub web_addSite(ByVal appName As String, ByVal DomainName As String, ByVal rootPublicFilesPath As String, ByVal defaultDocOrBlank As String)
            Try
                Dim mySite As Site
                Dim newPool As ApplicationPool
                Dim bindinginformation As String
                '
                If Not web_existsSite(appName) Then
                    '
                    Using iisManager As ServerManager = New ServerManager()
                        cpCore.appRootFiles.saveFile("deleteMe.txt", "Temp document to create path")
                        cpCore.appRootFiles.deleteFile("deleteMe.txt")
                        bindinginformation = "*:80:" & DomainName
                        mySite = iisManager.Sites.Add(appName, "http", bindinginformation, cpCore.serverconfig.appConfig.appRootFilesPath)
                        'mySite = iisManager.Sites.Add(appName, "http", bindinginformation, cpCore.serverConfig.clusterPath & cpCore.serverconfig.appConfig.appRootFilesPath)
                        'iisManager.Sites.Item(0).)
                        bindinginformation = "*:80:" & appName
                        mySite.Bindings.Add(bindinginformation, "http")
                        mySite.ServerAutoStart = True
                        mySite.ApplicationDefaults.ApplicationPoolName = appName
                        mySite.TraceFailedRequestsLogging.Enabled = True
                        mySite.TraceFailedRequestsLogging.Directory = "C:\\inetpub\\"
                        '
                        newPool = iisManager.ApplicationPools.Add(appName)
                        newPool.ManagedRuntimeVersion = "v4.0"
                        newPool.Enable32BitAppOnWin64 = True
                        '
                        iisManager.CommitChanges()
                        'If False Then
                        '    '
                        '    ' not sure why this fails, but the is already started.
                        '    '
                        '    iisSite = iisManager.Sites(siteName)
                        '    iisSite.Start()
                        'End If
                    End Using
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '        '
        '        '=======================================================================================
        '        '   Register a dotnet assembly (only with interop perhaps)
        '        '
        '        '   Must be called from a process running as admin
        '        '   This can be done using the command queue, which kicks off the ccCmd process from the Server
        '        '=======================================================================================
        '        '
        '        Public Sub RegisterDotNet(ByVal FilePathFileName As String)
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim RegAsmFound As Boolean
        '            Dim Cmd As String
        '            Dim Filename As String
        '            Dim FilePathFileNameNoExt As String
        '            Dim Ptr As Integer
        '            Dim LogFilename As String
        '            Dim MonthNumber As Integer
        '            Dim DayNumber As Integer
        '            Dim Filenamec As String
        '            Dim DotNetInstallPath As String
        '            'Dim fs As New fileSystemClass
        '            Dim RegAsmFilename As String

        '            Const userRoot As String = "HKEY_LOCAL_MACHINE"
        '            Const subkey As String = "\SOFTWARE\Microsoft\.NETFramework"
        '            Const keyName As String = userRoot & "\" & subkey
        '            '
        '            DotNetInstallPath = Microsoft.Win32.Registry.GetValue(keyName, "InstallRoot", "")
        '            If DotNetInstallPath = "" Then
        '                DotNetInstallPath = "C:\WINNT\Microsoft.NET\Framework\"
        '                If Not cpCore.app.publicFiles.checkPath(DotNetInstallPath) Then
        '                    DotNetInstallPath = "C:\Windows\Microsoft.NET\Framework\"
        '                    If Not cpCore.app.publicFiles.checkPath(DotNetInstallPath) Then
        '                        DotNetInstallPath = ""
        '                    End If
        '                End If
        '            End If
        '            If DotNetInstallPath = "" Then
        '                Call handleLegacyClassError2("", "RegisterDotNet", "Could not detect dotnet installation path. Dot Net may not be installed.")
        '            Else
        '                RegAsmFound = True
        '                RegAsmFilename = DotNetInstallPath & "v2.0.50727\regasm.exe"
        '                If Not cpCore.app.publicFiles.checkFile(RegAsmFilename) Then
        '                    RegAsmFilename = DotNetInstallPath & "v3.0\regasm.exe"
        '                    If Not cpCore.app.publicFiles.checkFile(RegAsmFilename) Then
        '                        RegAsmFilename = DotNetInstallPath & "v3.5\regasm.exe"
        '                        If Not cpCore.app.publicFiles.checkFile(RegAsmFilename) Then
        '                            RegAsmFound = False
        '                        End If
        '                    End If
        '                End If
        '                If Not RegAsmFound Then
        '                    Call handleLegacyClassError2("", "RegisterDotNet", "Could not find regasm.exe in subfolders v2.0.50727, v3.0, or v3.5 of dotnet installation path [" & RegAsmFilename & "]. Dot Net may not be installed.")
        '                Else
        '                    Ptr = InStrRev(FilePathFileName, ".")
        '                    If Ptr > 0 Then
        '                        FilePathFileNameNoExt = Mid(FilePathFileName, 1, Ptr - 1)
        '                        DayNumber = Day(Now)
        '                        MonthNumber = Month(Now)
        '                        LogFilename = Year(Now)
        '                        If MonthNumber < 10 Then
        '                            LogFilename = LogFilename & "0"
        '                        End If
        '                        LogFilename = LogFilename & MonthNumber
        '                        If DayNumber < 10 Then
        '                            LogFilename = LogFilename & "0"
        '                        End If
        '                        LogFilename = getProgramFilesPath() & "\Logs\addoninstall\" & LogFilename & DayNumber & ".log"
        '                        Cmd = """" & RegAsmFilename & """ """ & FilePathFileName & """ /codebase"
        '                        Call runProcess(cpCore, Cmd, , True)
        '                    End If
        '                End If
        '            End If

        '            'cpCore.AppendLog("dll" & ".SiteBuilderClass.RegisterDotnet called Regsvr32, called but the output could not be captured")
        '            '
        '            Exit Sub
        'ErrorTrap:
        '            Call handleLegacyClassError1("unknown", "RegisterDotnet")
        '        End Sub
        ''
        ''=============================================================================================================
        ''   Main
        ''       Returns nothing if all OK, else returns an error message
        ''=============================================================================================================
        ''

        'Public Function createApp(ByVal appName As String, ByVal domainName As String) As Boolean
        '    Dim returnOk As Boolean = False
        '    Try
        '        Dim builder As builderClass
        '        Dim useIIS As Boolean
        '        Dim iisDefaultDoc As String
        '        Dim cpNewApp As CPClass
        '        '
        '        useIIS = False
        '        iisDefaultDoc = ""
        '        Select Case cpCore.cluster.config.appPattern.ToLower
        '            Case "php"
        '                useIIS = True
        '                iisDefaultDoc = "index.php"
        '            Case "aspnet"
        '                useIIS = True
        '                iisDefaultDoc = "default.aspx"
        '            Case "iismodule"
        '                useIIS = True
        '                iisDefaultDoc = "default.aspx"
        '            Case "nodejs"
        '        End Select
        '        '
        '        ' setup iis
        '        '
        '        If useIIS Then
        '            Call web_addSite(appName, domainName, "\", iisDefaultDoc)
        '        End If
        '        '
        '        ' initialize the new app, use the save authentication that was used to authorize this object
        '        '
        '        cpNewApp = New CPClass(appName)
        '        builder = New builderClass(cpNewApp.core)
        '        Call builder.upgrade(True)
        '        Call cpNewApp.core.app.siteProperty_set(siteproperty_serverPageDefault_name, iisDefaultDoc)
        '        cpNewApp.Dispose()
        '    Catch ex As Exception
        '        cpCore.handleException(ex)
        '    End Try
        '    Return returnOk
        'End Function
        '
        '=============================================================================================================
        '   Main
        '       Returns nothing if all OK, else returns an error message
        '=============================================================================================================
        '
        Public Function importApp(ByVal siteName As String, ByVal IPAddress As String, ByVal DomainName As String, ByVal ODBCConnectionString As String, ByVal ContentFilesPath As String, ByVal WWWRootPath As String, ByVal defaultDoc As String, ByVal SMTPServer As String, ByVal AdminEmail As String) As String
            Dim returnMessage As String = ""
            Try
                '
                Dim Ready As Boolean
                Dim WWWDestinationFolder As String
                Dim Copy As String
                Dim DSNFilename As String
                Dim ProgramFilesPath As String
                Dim WWWSourceFilename As String
                '
                If siteName = "" Then
                    importApp = "The application name was blank. It is required."
                Else
                    '
                    If defaultDoc = "" Then
                        '
                        ' it was required, this is the best guess
                        '
                        defaultDoc = siteproperty_serverPageDefault_defaultValue
                    End If
                    '
                    If IPAddress = "" Then
                        IPAddress = "127.0.0.1"
                    End If
                    '
                    If DomainName = "" Then
                        DomainName = IPAddress
                    End If
                    '
                    If ContentFilesPath = "" Then
                        ContentFilesPath = "c:\inetpub\apps\" & siteName & "\cdnFiles"
                    End If
                    If Right(ContentFilesPath, 1) <> "\" Then
                        ContentFilesPath = ContentFilesPath & "\"
                    End If
                    '
                    If ODBCConnectionString = "" Then
                        ODBCConnectionString = siteName
                    End If
                    '
                    If WWWRootPath = "" Then
                        WWWRootPath = "c:\inetpub\apps\" & siteName & "\appRoot"
                    End If
                    If Right(WWWRootPath, 1) <> "\" Then
                        WWWRootPath = WWWRootPath & "\"
                    End If
                    '
                    If SMTPServer = "" Then
                        SMTPServer = "127.0.0.1"
                    End If
                    '
                    If AdminEmail = "" Then
                        AdminEmail = "admin@" & DomainName
                    End If
                    '
                    ' Configure Contensive
                    '
                    'Call VerifyApp2(siteName, DomainName, ODBCConnectionString, ContentFilesPath, "/", WWWRootPath)
                    '
                    ' Rebuild IIS Server
                    '
                    Call web_addSite(siteName, DomainName, "\", defaultDoc)
                    '
                    ' Now wait here for site to start with upgrade
                    '

                    Dim builder As New coreBuilderClass(cpCore)
                    Call builder.upgrade(False)
                    importApp = ""
                End If
            Catch ex As Exception

            End Try
        End Function

        ''
        ''========================================================================
        ''   Init()
        ''========================================================================
        ''
        'Public Sub Init(appservicesObj As cpCoreClass)
        '    appservices = appservicesObj
        '    On Error GoTo ErrorTrap
        '    '
        '    Dim MethodName As String
        '    '
        '    MethodName = "Init"
        '    '
        '    ApplicationNameLocal = cpcore.app.appEnvironment.name
        '    ClassInitialized = True
        '    '
        '    Exit Sub
        '    '
        'ErrorTrap:
        '    dim ex as new exception("todo"): Call HandleClassError(ex,cpcore.app.appEnvironment.name,Err.Number, Err.Source, Err.Description, "Init", True, False)
        'End Sub
        ''
        ''=========================================================================
        ''
        ''=========================================================================
        ''
        'Public Sub Upgrade()
        '    '
        '    ' deprecated call
        '    '
        '    Call Upgrade2( "index.php")
        'End Sub
        'Public Sub upgrade(ByVal appName As String, ByVal clusterServices As clusterServicesClass, isNewSite As Boolean)
        '    Dim appservices As New appServicesClass(appName, clusterServices)
        '    If Not cpcore.app.config.enabled Then
        '        Call Upgrade2( isNewSite)
        '    Else
        '        cpCore.AppendLog("Cannot upgrade until the site is disabled")
        '    End If
        'End Sub
        '
        '=========================================================================
        ' upgrade
        '=========================================================================
        '
        Public Sub upgrade(isNewBuild As Boolean)
            Try
                Dim addonInstallOk As Boolean
                Dim UpgradeOK As Boolean
                Dim saveLogFolder As String
                Dim CollectionPath As String
                Dim LastChangeDate As Date
                Dim LocalLastChangeDate As Date
                Dim upgradeCollection As Boolean
                Dim LocalListNode As XmlNode
                Dim CollectionNode As XmlNode
                Dim LocalGuid As String
                Dim Guids() As String
                Dim Guid As String
                Dim Ptr As Integer
                Dim IISResetRequired As Boolean
                'Dim RegisterList As String
                Dim ErrorMessage As String
                Dim XMLTools As New coreXmlToolsClass(cpCore)
                'Dim SiteBuilder As New builderClass(cpCore)
                Dim addonInstall As New coreAddonInstallClass(cpCore)
                Dim StyleSN As Integer
                Dim Doc As XmlDocument
                Dim DataBuildVersion As String
                Dim SQL As String
                Dim dt As DataTable
                Dim Copy As String
                Dim MethodName As String = "Upgrade2"
                '
                '---------------------------------------------------------------------
                ' ----- verify upgrade is not already in progress
                '---------------------------------------------------------------------
                '
                If cpCore.upgradeInProgress Then
                    Call Err.Raise(ignoreInteger, "ccCSv", ignoreString)
                Else
                    cpCore.upgradeInProgress = True
                    DataBuildVersion = cpCore.siteProperties.dataBuildVersion
                    Call appendUpgradeLog("Upgrade, isNewBuild=[" & isNewBuild & "], data buildVersion=[" & DataBuildVersion & "], code buildVersion=[" & cpCore.common_version & "]")
                    '
                    '---------------------------------------------------------------------
                    '   Verify core table fields (DataSources, Content Tables, Content, Content Fields, Setup, Sort Methods)
                    '   This must be done before CDef  restore
                    '---------------------------------------------------------------------
                    '
                    Call appendUpgradeLog("VerifyCoreTables...")
                    Call VerifyCoreTables()
                    '
                    '---------------------------------------------------------------------
                    ' ----- build/verify Content Definitions
                    '---------------------------------------------------------------------
                    '
                    ' Update the Db Content from CDef Files
                    '
                    Call appendUpgradeLog("UpgradeCDef...")
                    Call addonInstall.installBaseCollection(isNewBuild)
                    '
                    '---------------------------------------------------------------------
                    ' ----- Convert Database fields for new Db
                    '---------------------------------------------------------------------
                    '
                    If isNewBuild Then
                        cpCore.siteProperties.setProperty("publicFileContentPathPrefix", "/contentFiles/")
                        '
                        ' add the root developer
                        '
                        Dim cid As Integer
                        cid = cpCore.db.getContentId("people")
                        dt = cpCore.db.executeSql("select id from ccmembers where (Developer<>0)")
                        If dt.Rows.Count = 0 Then
                            SQL = "" _
                                & "insert into ccmembers" _
                                & " (active,contentcontrolid,name,firstName,username,password,developer,admin,AllowToolsPanel,AllowBulkEmail,AutoLogin)" _
                                & " values (1," & cid & ",'root','root','root','contensive',1,1,1,1,1)" _
                                & "" _
                                & "" _
                                & ""
                            cpCore.db.executeSql(SQL)
                        End If
                        '
                        ' Copy default styles into Template Styles
                        '
                        Call cpCore.appRootFiles.copyFile("ccLib\Config\Styles.css", "Templates\Styles.css")
                        '
                        ' set build version so a scratch build will not go through data conversion
                        '
                        DataBuildVersion = cpCore.common_version()
                        Call cpCore.siteProperties.setProperty("BuildVersion", cpCore.common_version)
                        cpCore.siteProperties._dataBuildVersion_Loaded = False
                    End If
                    '
                    '---------------------------------------------------------------------
                    ' ----- Upgrade Database fields if not new
                    '---------------------------------------------------------------------
                    '
                    If DataBuildVersion < cpCore.common_version() Then
                        '
                        Call appendUpgradeLog("Calling database conversion, DataBuildVersion [" & DataBuildVersion & "], software version [" & cpCore.common_version() & "]")
                        '
                        Call Upgrade_Conversion(DataBuildVersion)
                    End If
                    '
                    '---------------------------------------------------------------------
                    ' ----- Verify content
                    '---------------------------------------------------------------------
                    '
                    If True Then
                        Call appendUpgradeLog("Verify records required")
                        '
                        ' ##### menus are created in ccBase.xml, this just checks for dups
                        Call VerifyAdminMenus(DataBuildVersion)
                        Call VerifyLanguageRecords()
                        Call VerifyCountries()
                        Call VerifyStates()
                        Call VerifyLibraryFolders()
                        Call VerifyLibraryFileTypes()
                        'Call VerifyContentWatchLists()
                        Call VerifyDefaultGroups()
                        Call VerifyScriptingRecords()
                        ' yoo nych rewrite
                        'Call cpcore.app.csv_VerifyDynamicMenu("Default")
                    End If
                    '
                    '---------------------------------------------------------------------
                    ' ----- Set Default SitePropertyDefaults
                    '       must be after upgrade_conversion
                    '---------------------------------------------------------------------
                    '
                    If True Then
                        Call appendUpgradeLog("Verify Site Properties")
                        '
                        ' 20151204 - no - on new builds, set the site property after the builder.
                        '' 4.1.575 - 8/28 - fixes problem where new sites have no defaultDoc ( and therefor no AdminUrl)
                        ''if ServerPageDefault is set, get it. else set it to defaultDoc
                        '' if empty, leave blank at this point (for base Db build).
                        ''
                        'ServerPageDefault = cpCore.app.siteProperty_getText(siteproperty_serverPageDefault_name, "")
                        'If (ServerPageDefault = "") And (defaultDoc = "") Then
                        '    '
                        '    ' This is the base build, just leave it blank
                        '    '
                        'ElseIf (ServerPageDefault <> "") Then
                        '    '
                        '    ' is has a non-blank value already stored, use it
                        '    '
                        '    defaultDoc = ServerPageDefault
                        'Else
                        '    '
                        '    ' property is blank but defaultDoc is not, set it to the new defaultDoc
                        '    '
                        '    Call cpCore.app.siteProperty_set(siteproperty_serverPageDefault_name, defaultDoc)
                        'End If
                        '
                        Copy = cpCore.siteProperties.getText("AllowAutoHomeSectionOnce", EncodeText(isNewBuild))
                        Copy = cpCore.siteProperties.getText("AllowAutoLogin", "False")
                        Copy = cpCore.siteProperties.getText("AllowBake", "True")
                        Copy = cpCore.siteProperties.getText("AllowChildMenuHeadline", "True")
                        Copy = cpCore.siteProperties.getText("AllowContentAutoLoad", "True")
                        Copy = cpCore.siteProperties.getText("AllowContentSpider", "False")
                        Copy = cpCore.siteProperties.getText("AllowContentWatchLinkUpdate", "True")
                        Copy = cpCore.siteProperties.getText("AllowDuplicateUsernames", "False")
                        Copy = cpCore.siteProperties.getText("ConvertContentText2HTML", "False")
                        Copy = cpCore.siteProperties.getText("AllowMemberJoin", "False")
                        Copy = cpCore.siteProperties.getText("AllowPasswordEmail", "True")
                        Copy = cpCore.siteProperties.getText("AllowPathBlocking", "True")
                        Copy = cpCore.siteProperties.getText("AllowPopupErrors", "True")
                        Copy = cpCore.siteProperties.getText("AllowTestPointLogging", "False")
                        Copy = cpCore.siteProperties.getText("AllowTestPointPrinting", "False")
                        Copy = cpCore.siteProperties.getText("AllowTransactionLog", "False")
                        Copy = cpCore.siteProperties.getText("AllowTrapEmail", "True")
                        Copy = cpCore.siteProperties.getText("AllowTrapLog", "True")
                        Copy = cpCore.siteProperties.getText("AllowWorkflowAuthoring", "False")
                        Copy = cpCore.siteProperties.getText("ArchiveAllowFileClean", "False")
                        Copy = cpCore.siteProperties.getText("ArchiveRecordAgeDays", "90")
                        Copy = cpCore.siteProperties.getText("ArchiveTimeOfDay", "2:00:00 AM")
                        Copy = cpCore.siteProperties.getText("BreadCrumbDelimiter", "&nbsp;&gt;&nbsp;")
                        Copy = cpCore.siteProperties.getText("CalendarYearLimit", "1")
                        Copy = cpCore.siteProperties.getText("ContentPageCompatibility21", "false")
                        Copy = cpCore.siteProperties.getText("DefaultFormInputHTMLHeight", "500")
                        Copy = cpCore.siteProperties.getText("DefaultFormInputTextHeight", "1")
                        Copy = cpCore.siteProperties.getText("DefaultFormInputWidth", "60")
                        Copy = cpCore.siteProperties.getText("EditLockTimeout", "5")
                        Copy = cpCore.siteProperties.getText("EmailAdmin", "webmaster@" & cpCore.serverconfig.appConfig.domainList(0))
                        Copy = cpCore.siteProperties.getText("EmailFromAddress", "webmaster@" & cpCore.serverconfig.appConfig.domainList(0))
                        Copy = cpCore.siteProperties.getText("EmailPublishSubmitFrom", "webmaster@" & cpCore.serverconfig.appConfig.domainList(0))
                        Copy = cpCore.siteProperties.getText("Language", "English")
                        Copy = cpCore.siteProperties.getText("PageContentMessageFooter", "Copyright " & cpCore.serverconfig.appConfig.domainList(0))
                        Copy = cpCore.siteProperties.getText("SelectFieldLimit", "4000")
                        Copy = cpCore.siteProperties.getText("SelectFieldWidthLimit", "100")
                        Copy = cpCore.siteProperties.getText("SMTPServer", "127.0.0.1")
                        Copy = cpCore.siteProperties.getText("TextSearchEndTag", "<!-- TextSearchEnd -->")
                        Copy = cpCore.siteProperties.getText("TextSearchStartTag", "<!-- TextSearchStart -->")
                        Copy = cpCore.siteProperties.getText("TrapEmail", "")
                        Copy = cpCore.siteProperties.getText("TrapErrors", "0")
                    End If
                    '
                    '---------------------------------------------------------------------
                    ' ----- Changes that effect the web server or content files, not the Database
                    '---------------------------------------------------------------------

                    '
                    StyleSN = (cpCore.siteProperties.getinteger("StylesheetSerialNumber"))
                    If StyleSN > 0 Then
                        StyleSN = StyleSN + 1
                        Call cpCore.siteProperties.setProperty("StylesheetSerialNumber", CStr(StyleSN))
                        ' too lazy
                        'Call cpcore.app.publicFiles.SaveFile(cpcore.app.csv_getPhysicalFilename("templates\Public" & StyleSN & ".css"), cpcore.app.csv_getStyleSheetProcessed)
                        'Call cpcore.app.publicFiles.SaveFile(cpcore.app.csv_getPhysicalFilename("templates\Admin" & StyleSN & ".css", cpcore.app.csv_getStyleSheetDefault)
                    End If
                    '
                    ' clear all cache
                    '
                    Call cpCore.cache.invalidateAll()
                    '
                    '---------------------------------------------------------------------
                    ' ----- internal upgrade complete
                    '---------------------------------------------------------------------
                    '
                    If True Then
                        Call appendUpgradeLog("Internal upgrade complete, set Buildversion to " & cpCore.common_version)
                        Call cpCore.siteProperties.setProperty("BuildVersion", cpCore.common_version)
                        cpCore.siteProperties._dataBuildVersion_Loaded = False
                        '
                        '---------------------------------------------------------------------
                        ' ----- Upgrade local collections
                        '       This would happen if this is the first site to upgrade after a new build is installed
                        '       (can not be in startup because new addons might fail with DbVersions)
                        '       This means a dataupgrade is required with a new build - You can expect errors
                        '---------------------------------------------------------------------
                        '
                        If True Then
                            'If Not UpgradeDbOnly Then
                            '
                            ' 4.1.575 - 8/28 - put this code behind the DbOnly check, makes DbOnly beuild MUCH faster
                            '
                            ErrorMessage = ""
                            'RegisterList = ""
                            Call appendUpgradeLog("Upgrading All Local Collections to new server build.")
                            saveLogFolder = classLogFolder
                            UpgradeOK = addonInstall.UpgradeLocalCollectionRepoFromRemoteCollectionRepo(ErrorMessage, "", IISResetRequired, isNewBuild)
                            classLogFolder = saveLogFolder
                            If ErrorMessage <> "" Then
                                cpCore.handleLegacyError3(cpCore.serverconfig.appConfig.name, "During UpgradeAllLocalCollectionsFromLib3 call, " & ErrorMessage, "dll", "builderClass", "Upgrade2", 0, "", "", False, True, "")
                            ElseIf Not UpgradeOK Then
                                cpCore.handleLegacyError3(cpCore.serverconfig.appConfig.name, "During UpgradeAllLocalCollectionsFromLib3 call, NotOK was returned without an error message", "dll", "builderClass", "Upgrade2", 0, "", "", False, True, "")
                            End If
                            ''
                            ''---------------------------------------------------------------------
                            '' ----- Upgrade collections added during upgrade process
                            ''---------------------------------------------------------------------
                            ''
                            'Call appendUpgradeLog("Installing Add-on Collections gathered during upgrade")
                            'If InstallCollectionList = "" Then
                            '    Call appendUpgradeLog(cpCore.app.config.name, MethodName, "No Add-on collections added during upgrade")
                            'Else
                            '    ErrorMessage = ""
                            '    Guids = Split(InstallCollectionList, ",")
                            '    For Ptr = 0 To UBound(Guids)
                            '        ErrorMessage = ""
                            '        Guid = Guids(Ptr)
                            '        If Guid <> "" Then
                            '            saveLogFolder = classLogFolder
                            '            Call addonInstall.GetCollectionConfig(Guid, CollectionPath, LastChangeDate, "")
                            '            classLogFolder = saveLogFolder
                            '            If CollectionPath <> "" Then
                            '                '
                            '                ' This collection is installed locally, install from local collections
                            '                '
                            '                saveLogFolder = classLogFolder
                            '                Call addonInstall.installCollectionFromLocalRepo(Me, IISResetRequired, Guid, cpCore.version, ErrorMessage, "", "", isNewBuild)
                            '                classLogFolder = saveLogFolder
                            '                'Call AddonInstall.UpgradeAppFromLocalCollection( Me, ParentNavigatorID, IISResetRequired, Guid, getcodeversion, ErrorMessage, RegisterList, "")
                            '            Else
                            '                '
                            '                ' This is a new collection, install to the server and force it on this site
                            '                '
                            '                saveLogFolder = classLogFolder
                            '                addonInstallOk = addonInstall.installCollectionFromRemoteRepo(Guid, DataBuildVersion, IISResetRequired, "", ErrorMessage, "", isNewBuild)
                            '                classLogFolder = saveLogFolder
                            '                If Not addonInstallOk Then
                            '                    cpCore.handleLegacyError3(cpCore.app.config.name, "Error upgrading Addon Collection [" & Guid & "], " & ErrorMessage, "dll", "builderClass", "Upgrade2", 0, "", "", False, True, "")
                            '                End If

                            '            End If
                            '        End If
                            '    Next
                            'End If
                            '
                            '---------------------------------------------------------------------
                            ' ----- Upgrade all collection for this app (in case collections were installed before the upgrade
                            '---------------------------------------------------------------------
                            '
                            Dim Collectionname As String
                            Dim CollectionGuid As String
                            Dim localCollectionFound As Boolean
                            Call appendUpgradeLog("Checking all installed collections for upgrades from Collection Library")
                            Call appendUpgradeLog("...Open collectons.xml")
                            Try
                                Doc = New XmlDocument
                                Call Doc.LoadXml(addonInstall.getCollectionListFile)
                                If True Then
                                    If vbLCase(Doc.DocumentElement.Name) <> vbLCase(CollectionListRootNode) Then
                                        cpCore.handleLegacyError3(cpCore.serverconfig.appConfig.name, "Error loading Collection config file. The Collections.xml file has an invalid root node, [" & Doc.DocumentElement.Name & "] was received and [" & CollectionListRootNode & "] was expected.", "dll", "builderClass", "Upgrade", 0, "", "", False, True, "")
                                    Else
                                        With Doc.DocumentElement
                                            If vbLCase(.Name) = "collectionlist" Then
                                                '
                                                ' now go through each collection in this app and check the last updated agains the one here
                                                '
                                                Call appendUpgradeLog("...Open site collectons, iterate through all collections")
                                                'Dim dt As DataTable
                                                dt = cpCore.db.executeSql("select * from ccaddoncollections where (ccguid is not null)and(updatable<>0)")
                                                If dt.Rows.Count > 0 Then
                                                    Dim rowptr As Integer
                                                    For rowptr = 0 To dt.Rows.Count - 1

                                                        ErrorMessage = ""
                                                        CollectionGuid = vbLCase(dt.Rows(rowptr).Item("ccguid").ToString)
                                                        Collectionname = dt.Rows(rowptr).Item("name").ToString
                                                        Call appendUpgradeLog("...checking collection [" & Collectionname & "], guid [" & CollectionGuid & "]")
                                                        If CollectionGuid <> "{7c6601a7-9d52-40a3-9570-774d0d43d758}" Then
                                                            '
                                                            ' upgrade all except base collection from the local collections
                                                            '
                                                            localCollectionFound = False
                                                            upgradeCollection = False
                                                            LastChangeDate = EncodeDate(dt.Rows(rowptr).Item("LastChangeDate"))
                                                            If LastChangeDate = Date.MinValue Then
                                                                '
                                                                ' app version has no lastchangedate
                                                                '
                                                                upgradeCollection = True
                                                                Call appendUpgradeLog(cpCore.serverconfig.appConfig.name, MethodName, "Upgrading collection " & dt.Rows(rowptr).Item("name").ToString & " because the collection installed in the application has no LastChangeDate. It may have been installed manually.")
                                                            Else
                                                                '
                                                                ' compare to last change date in collection config file
                                                                '
                                                                LocalGuid = ""
                                                                LocalLastChangeDate = Date.MinValue
                                                                For Each LocalListNode In .ChildNodes
                                                                    Select Case vbLCase(LocalListNode.Name)
                                                                        Case "collection"
                                                                            For Each CollectionNode In LocalListNode.ChildNodes
                                                                                Select Case vbLCase(CollectionNode.Name)
                                                                                    Case "guid"
                                                                                        '
                                                                                        LocalGuid = vbLCase(CollectionNode.InnerText)
                                                                                    Case "lastchangedate"
                                                                                        '
                                                                                        LocalLastChangeDate = EncodeDate(CollectionNode.InnerText)
                                                                                End Select
                                                                            Next
                                                                    End Select
                                                                    If CollectionGuid = vbLCase(LocalGuid) Then
                                                                        localCollectionFound = True
                                                                        Call appendUpgradeLog("...local collection found")
                                                                        If LocalLastChangeDate <> Date.MinValue Then
                                                                            If LocalLastChangeDate > LastChangeDate Then
                                                                                Call appendUpgradeLog(cpCore.serverconfig.appConfig.name, MethodName, "Upgrading collection " & dt.Rows(rowptr).Item("name").ToString() & " because the collection in the local server store has a newer LastChangeDate than the collection installed on this application.")
                                                                                upgradeCollection = True
                                                                            End If
                                                                        End If
                                                                        Exit For
                                                                    End If
                                                                Next
                                                            End If
                                                            ErrorMessage = ""
                                                            If Not localCollectionFound Then
                                                                Call appendUpgradeLog("...site collection [" & Collectionname & "] not found in local collection, call UpgradeAllAppsFromLibCollection2 to install it.")
                                                                addonInstallOk = addonInstall.installCollectionFromRemoteRepo(CollectionGuid, ErrorMessage, "", isNewBuild)
                                                                classLogFolder = saveLogFolder
                                                                If Not addonInstallOk Then
                                                                    '
                                                                    ' this may be OK so log, but do not call it an error
                                                                    '
                                                                    Call appendUpgradeLog("...site collection [" & Collectionname & "] not found in collection Library. It may be a custom collection just for this site. Collection guid [" & CollectionGuid & "]")
                                                                End If
                                                            Else
                                                                If upgradeCollection Then
                                                                    Call appendUpgradeLog("...upgrading collection")
                                                                    saveLogFolder = classLogFolder
                                                                    Call addonInstall.installCollectionFromLocalRepo(CollectionGuid, cpCore.common_version, ErrorMessage, "", isNewBuild)
                                                                    classLogFolder = saveLogFolder
                                                                End If
                                                            End If
                                                        End If
                                                    Next
                                                End If
                                            End If
                                        End With
                                    End If
                                End If

                            Catch ex9 As Exception
                                Call handleClassException(ex9, cpCore.serverconfig.appConfig.name, MethodName) ' "upgrade2")
                            End Try
                            '
                            ' done
                            '
                        End If
                    End If
                    '
                    '---------------------------------------------------------------------
                    ' ----- Explain, put up a link and exit without continuing
                    '---------------------------------------------------------------------
                    '
                    appendUpgradeLog("Upgrade Complete")
                    cpCore.upgradeInProgress = False
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '        '
        '        ' ----- Rename a content definition to a new name
        '        '
        '        Private Sub RenameContentDefinition(ByVal OldName As String, ByVal NewName As String)

        '            On Error GoTo ErrorTrap
        '            '
        '            Dim ErrorDescription As String
        '            Dim MethodName As String
        '            Dim SQL As String
        '            '
        '            MethodName = "RenameContentDefinition"
        '            '
        '            SQL = "UPDATE ccContent SET ccContent.name = '" & NewName & "' WHERE (((ccContent.name)='" & OldName & "'));"
        '            Call cpCore.app.executeSql(SQL)
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call handleClassException(ex, cpCore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "RenameContentDefinition", True, False)
        '        End Sub

        '        '
        '        '
        '        '
        '        Private Sub UpgradeSortOrder( ByVal DataSourceName As String, ByVal TableName As String)
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim ErrorDescription As String
        '            Dim CSPointer As Integer
        '            Dim methodName As String = "UpgradeSortOrder"
        '            '
        '            Call cpcore.app.csv_CreateSQLTableField(DataSourceName, TableName, "TempField", FieldTypeText)
        '            CSPointer = cpcore.app.csv_OpenCSSQL(DataSourceName, "SELECT ID, Sortorder from " & TableName & " Order By Sortorder;")
        '            If Not cpcore.app.csv_IsCSOK(CSPointer) Then
        '                Dim ex2 As New Exception("todo") : Call HandleClassError(ex2, cpcore.app.config.name, methodName) ' ignoreInteger, "dll", "Could not upgrade SortOrder", "UpgradeSortOrder", False, True)
        '            Else
        '                Do While cpcore.app.csv_IsCSOK(CSPointer)
        '                    Call cpcore.app.ExecuteSQL(DataSourceName, "UPDATE " & TableName & " SET TempField=" & encodeSQLText(Format(cpcore.app.csv_cs_getInteger(CSPointer, "sortorder"), "00000000")) & " WHERE ID=" & encodeSQLNumber(cpcore.app.csv_cs_getInteger(CSPointer, "ID")) & ";")
        '                    cpcore.app.csv_NextCSRecord(CSPointer)
        '                Loop
        '                Call cpcore.app.csv_CloseCS(CSPointer)
        '                Call cpcore.app.csv_DeleteTableIndex(DataSourceName, TableName, "SORTORDER")
        '                Call cpcore.app.csv_DeleteTableIndex(DataSourceName, TableName, TableName & "SORTORDER")
        '                Call cpcore.app.csv_DeleteTableField(DataSourceName, TableName, "SortOrder")
        '                Call cpcore.app.csv_CreateSQLTableField(DataSourceName, TableName, "SortOrder", FieldTypeText)
        '                Call cpcore.app.ExecuteSQL(DataSourceName, "UPDATE " & TableName & " SET SortOrder=TempField;")
        '                Call cpcore.app.csv_CreateSQLIndex(DataSourceName, TableName, TableName & "SORTORDER", "SortOrder")
        '            End If
        '            Call cpcore.app.csv_DeleteTableField(DataSourceName, TableName, "TempField")
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "UpgradeSortOrder", True, False)
        '        End Sub
        '        '
        '        '=============================================================================
        '        ' ----- Returns true if the content field exists
        '        '=============================================================================
        '        '
        '        Private Function ExistsSQLTableField(ByVal TableName As String, ByVal FieldName As String) As Boolean
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim ErrorDescription As String
        '            ' converted array to dictionary - Dim FieldPointer As Integer
        '            Dim dt As DataTable
        '            Dim UcaseTableFieldName
        '            Dim UcaseFieldName As String
        '            Dim MethodName As String
        '            '
        '            MethodName = "ExistsSQLTableField"
        '            '
        '            ExistsSQLTableField = False
        '            '
        '            dt = cpCore.app.executeSql("SELECT * FROM " & TableName & ";")
        '            If dt.Rows.Count = 0 Then
        '                Call cpCore.app.executeSql("INSERT INTO " & TableName & " (Name)VALUES('no name');")
        '                dt = cpCore.app.executeSql("SELECT * FROM " & TableName & ";")
        '            End If
        '            If dt.Rows.Count > 0 Then

        '                For FieldPointer = 1 To dt.Rows.Count - 1
        '                    If UcaseFieldName = vbUCase(dt.Rows(FieldPointer).Item("name")) Then
        '                        ExistsSQLTableField = True
        '                        Exit For
        '                    End If
        '                Next
        '            End If
        '            '
        '            Exit Function
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call handleClassException(ex, cpCore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "ExistsSQLTableField", True, False)
        '        End Function
        '        '
        '        '
        '        '
        '        Private Sub CreatePage( ByVal ContentName As String, ByVal PageName As String, ByVal PageCopy As String)
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim ErrorDescription As String
        '            Dim CSPointer As Integer
        '            Dim RecordID As Integer
        '            Dim Filename As String
        '            '
        '            CSPointer = cpcore.app.csv_InsertCSRecord(ContentName)
        '            If cpcore.app.csv_IsCSOK(CSPointer) Then
        '                RecordID = (cpcore.app.csv_cs_getInteger(CSPointer, "ID"))
        '                Filename = cpcore.app.csv_cs_getFilename(CSPointer, "Name", "")
        '                'Filename = cpcore.app.csv_GetVirtualFilename(ContentName, "Name", RecordID)
        '                Call cpcore.app.csv_SetCSField(CSPointer, "name", PageName)
        '                Call cpcore.app.csv_SetCSField(CSPointer, "copyfilename", Filename)
        '                Call cpcore.app.publicFiles.SaveFile(Filename, PageCopy)
        '            End If
        '            cpcore.app.csv_CloseCS(CSPointer)
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "createPage") ' Err.Number, Err.Source, Err.Description, "CreatePage", True, False)
        '        End Sub
        '        '
        '        '==========================================================================================
        '        '   Add the ccTable for version 3.0.300
        '        '==========================================================================================
        '        '
        '        Private Sub PopulateTableTable()
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim ErrorDescription As String
        '            Dim SQL As String
        '            dim dt as datatable
        '            Dim RSTables as datatable
        '            Dim RSNewTable as datatable
        '            Dim DataSourceID As Integer
        '            Dim ContentCID As Integer
        '            Dim TablesCID As Integer
        '            Dim MethodName As String
        '            Dim RunUpgrade As Boolean
        '            Dim SQLNow As String
        '            Dim TableName As String
        '            Dim TableID As Integer
        '            Dim ContentID As Integer
        '            '
        '            MethodName = "PopulateTableTable"
        '            '
        '            If False Then
        '                Exit Sub
        '            End If
        '            '
        '            ' ----- Make sure content definition exists
        '            '
        '            Call cpcore.app.csv_CreateContentFromSQLTable("Default", "ccTables", "Tables")
        '            TablesCID = cpcore.app.csv_GetContentID("Tables")
        '            If True Then
        '                '
        '                ' ----- Create the ccTables TableName entry in the ccContent Table
        '                '
        '                SQL = "Update ccContent set sqlTable='ccTables' where name='tables';"
        '                Call cpcore.app.executeSql(SQL)
        '                '
        '                ' ----- Append tables from ccContent
        '                '
        '                SQL = "Select ID, sqlTable,DataSourceID From ccContent where active<>0;"
        '                RS = cpcore.app.executeSql(SQL)
        '                If (isDataTableOk(rs)) Then
        '                    '
        '                    ' if no error, field exists, and it is OK to continue
        '                    '
        '                    Do While Not rs.rows.count=0
        '                        TableName = EncodeText(cpcore.app.getDataRowColumnName(RS.rows(0), "sqlTable"))
        '                        TableID = 0
        '                        DataSourceID = EncodeInteger(cpcore.app.getDataRowColumnName(RS.rows(0), "DataSourceID"))
        '                        ContentID = EncodeInteger(cpcore.app.getDataRowColumnName(RS.rows(0), "ID"))
        '                        If TableName <> "" Then
        '                            '
        '                            ' ----- Get TableID from TableName
        '                            '
        '                            SQL = "SELECT ID FROM ccTables where name=" & EncodeSQLText(TableName) & ";"
        '                            RSTables = cpcore.app.executeSql(SQL)
        '                            If Not RSTables.EOF Then
        '                                '
        '                                ' ----- Table entry found
        '                                '
        '                                TableID = cpcore.app.getDataRowColumnName(RSTables, "ID")
        '                            Else
        '                                '
        '                                ' ----- Table entry not found in ccTables, Create it
        '                                '
        '                                RSNewTable = cpcore.app.csv_InsertTableRecordGetDataTable("Default", "ccTables")
        '                                If Not (RSNewTable Is Nothing) Then
        '                                    TableID = cpcore.app.getDataRowColumnName(RSNewTable, "ID")
        '                                    Call RSNewTable.Close()
        '                                    If DataSourceID = 0 Then
        '                                        '
        '                                        ' ----- New entry has null datasource
        '                                        '
        '                                        SQL = "Update ccTables set active=" & SQLTrue & ", ContentControlID=" & EncodeSQLNumber(TablesCID) & ", Name=" & EncodeSQLText(TableName) & " where ID=" & TableID & ";"
        '                                    Else
        '                                        '
        '                                        ' ----- New entry has datasource
        '                                        '
        '                                        SQL = "Update ccTables set active=" & SQLTrue & ", ContentControlID=" & EncodeSQLNumber(TablesCID) & ", Name=" & EncodeSQLText(TableName) & ", DataSourceId=" & EncodeSQLNumber(DataSourceID) & " where ID=" & TableID & ";"
        '                                    End If
        '                                    Call cpcore.app.executeSql(SQL)
        '                                End If
        '                                RSNewTable = Nothing
        '                            End If
        '                            RSTables = Nothing
        '                        End If
        '                        If TableID <> 0 Then
        '                            SQL = "Update ccContent set ContentTableID=" & EncodeSQLNumber(TableID) & ", AuthoringTableID=" & EncodeSQLNumber(TableID) & " where ID=" & ContentID & ";"
        '                            Call cpcore.app.executeSql(SQL)
        '                        End If
        '                        Call RS.MoveNext()
        '                    Loop
        '                    If (isDataTableOk(rs)) Then
        '                        If false Then
        '                            'RS.Close()
        '                        End If
        '                        'RS = Nothing
        '                    End If
        '                End If
        '            End If
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            UpgradeErrorCount = UpgradeErrorCount + 1
        '            Dim ex As New exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "PopulateTableTable (error #" & UpgradeErrorCount & ")", True, True)
        '            If UpgradeErrorCount >= UpgradeErrorTheshold Then
        '                Dim ex3 As New Exception("todo") : Call HandleClassError(ex3, cpcore.app.config.name, MethodName) ' Err.Number, Err.Source, Err.Description, "PopulateTableTable (error #" & UpgradeErrorCount & ")", True, False)
        '            End If
        '            Resume Next
        '        End Sub

        '        '
        '        ' ----- Upgrade to version 4.1.xxx
        '        '
        '        Private Sub Upgrade_Conversion_to_41( ByVal DataBuildVersion As String)
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim ErrMessage As String
        '            Dim typeId As Integer
        '            Dim sf As Object
        '            Dim FilenameNoExt As String
        '            Dim Filename As String
        '            Dim Pos As Integer
        '            Dim FilenameExt As String
        '            Dim AltSizeList As String
        '            Dim Height As Integer
        '            Dim Width As Integer
        '            Dim SQL As String
        '            Dim RecordID As Integer
        '            Dim CS As Integer
        '            Dim CSSrc As Integer
        '            Dim CSDst As Integer
        '            Dim Guid As String
        '            Dim Name As String
        '            Dim addonId As Integer
        '            Dim runAtServer As runAtServerClass
        '            Dim addonInstall As New addonInstallClass
        '            Dim IISResetRequired As Boolean
        '            Dim RegisterList As String
        '            Dim MethodName As String
        '            Dim ContentID As Integer
        '            Dim TimeoutSave As Integer
        '            Dim CID As Integer
        '            ''
        '            'TimeoutSave = cpcore.app.csv_SQLCommandTimeout
        '            'cpcore.app.csv_SQLCommandTimeout = 1800
        '            '
        '            If False Then
        '                Exit Sub
        '            Else
        '                '
        '                MethodName = "Upgrade_Conversion_to_41"
        '                '
        '                If false Then
        '                    Call appendUpgradeLogAddStep(cpcore.app.config.name,MethodName, "4.1.279 upgrade")
        '                    '
        '                    ' added ccguid to all cdefs, but non-base did not upgrade automatically
        '                    '
        '                    CS = cpcore.app.csOpen("content")
        '                    Do While cpcore.app.csv_IsCSOK(CS)
        '                        Call metaData_VerifyCDefField_ReturnID(True, cpcore.app.csv_cs_getText(CS, "name"), "ccguid", FieldTypeText, , False, "Guid", , , , , , , , , , , , , , , , , True)
        '                        Call cpcore.app.csv_NextCSRecord(CS)
        '                    Loop
        '                    Call cpcore.app.csv_CloseCS(CS)
        '                End If
        '                If false Then
        '                    Call appendUpgradeLogAddStep(cpcore.app.config.name,MethodName, "4.1.288 upgrade")
        '                    '
        '                    ' added updatable again (was originally added in 275)
        '                    '
        '                    Call cpcore.app.ExecuteSQL("", "update ccAddonCollections set updatable=1")
        '                End If
        '                If false Then
        '                    Call appendUpgradeLogAddStep(cpcore.app.config.name,MethodName, "4.1.290 upgrade")
        '                    '
        '                    ' delete blank field help records, new method creates dups of inheritance cdef parent fields
        '                    '
        '                    Call cpcore.app.ExecuteSQL("", "delete from ccfieldhelp where (HelpDefault is null)and(HelpCustom is null)")
        '                End If
        '                If false Then
        '                    Call appendUpgradeLogAddStep(cpcore.app.config.name,MethodName, "4.1.294 upgrade")
        '                    '
        '                    ' convert fieldtypelongtext + htmlcontent to fieldtypehtml
        '                    '
        '                    Call cpcore.app.ExecuteSQL("", "update ccfields set type=" & FieldTypeHTML & " where type=" & FieldTypeLongText & " and (htmlcontent<>0)")
        '                    '
        '                    ' convert fieldtypetextfile + htmlcontent to fieldtypehtmlfile
        '                    '
        '                    Call cpcore.app.ExecuteSQL("", "update ccfields set type=" & FieldTypeHTMLFile & " where type=" & FieldTypeTextFile & " and (htmlcontent<>0)")
        '                End If
        '                If false Then
        '                    Call appendUpgradeLogAddStep(cpcore.app.config.name,MethodName, "4.1.352 upgrade")
        '                    '
        '                    ' try again - from 4.1.195
        '                    ' Some content used to be base, but now they are in add-ons
        '                    ' the isbasefield and isbasecontent needs to be cleard
        '                    '
        '                    Call RemoveIsBase( "items")
        '                    Call RemoveIsBase( "orders")
        '                    Call RemoveIsBase( "order details")
        '                    Call RemoveIsBase( "event images")
        '                    Call RemoveIsBase( "Promotions")
        '                    Call RemoveIsBase( "Page Types")
        '                    Call RemoveIsBase( "Forums")
        '                    Call RemoveIsBase( "Forum Messages")
        '                    Call RemoveIsBase( "Item Categories")
        '                    Call RemoveIsBase( "Survey Question Types")
        '                    Call RemoveIsBase( "Survey Questions")
        '                    Call RemoveIsBase( "Surveys")
        '                    Call RemoveIsBase( "Meeting Links")
        '                    Call RemoveIsBase( "news articles")
        '                    Call RemoveIsBase( "news issues")
        '                    Call RemoveIsBase( "Order Ship Methods")
        '                    Call RemoveIsBase( "Promotion Uses")
        '                    Call RemoveIsBase( "Spider Tasks")
        '                    Call RemoveIsBase( "Spider Cookies")
        '                    Call RemoveIsBase( "Spider Docs")
        '                    Call RemoveIsBase( "Spider Word Hits")
        '                    Call RemoveIsBase( "Spider Errors")
        '                    Call RemoveIsBase( "Calendar Events")
        '                    Call RemoveIsBase( "Calendars")
        '                    Call RemoveIsBase( "WhitePapers")
        '                    Call RemoveIsBase( "Survey Results")
        '                    Call RemoveIsBase( "Orders Completed")
        '                    Call RemoveIsBase( "Meetings")
        '                    Call RemoveIsBase( "Meeting Sessions")
        '                    Call RemoveIsBase( "Meeting Attendee Types")
        '                    Call RemoveIsBase( "Meeting Session Fees")
        '                    Call RemoveIsBase( "Meeting Attendees")
        '                    Call RemoveIsBase( "Meeting Attendee Sessions")
        '                    Call RemoveIsBase( "PAY METHODS")
        '                    Call RemoveIsBase( "Meeting Pay Methods")
        '                    Call RemoveIsBase( "Memberships")
        '                    Call RemoveIsBase( "Organization Address Types")
        '                End If
        '                If false Then
        '                    Call appendUpgradeLogAddStep(cpcore.app.config.name,MethodName, "4.1.374 upgrade")
        '                    '
        '                    ' repair the country table (abbreviation was set to an integer a long time ago)
        '                    '
        '                    SQL = "insert into cccountries (abbreviation)values('US')"
        '                    On Error Resume Next
        '                    Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                    ErrMessage = Err.Description
        '                    Err.Clear()
        '                    On Error GoTo ErrorTrap
        '                    SQL = "delete from cccountries where (name is null) or (name='')"
        '                    Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                    If ErrMessage <> "" Then
        '                        '
        '                        ' needs to be fixed
        '                        '
        '                        typeId = cpcore.app.csv_GetDataSourceType("default")
        '                        If typeId = DataSourceTypeODBCAccess Then
        '                            '
        '                            ' MS Access
        '                            '
        '                            SQL = "alter table cccountries add column abbr VarChar(255) NULL"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                            '
        '                            SQL = "update cccountries set abbr=abbreviation"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                            '
        '                            SQL = "alter table cccountries drop abbreviation"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                            '
        '                            SQL = "alter table cccountries add column abbreviation VarChar(255) NULL"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                            '
        '                            SQL = "update cccountries set abbreviation=abbr"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                            '
        '                            SQL = "alter table cccountries drop abbr"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                        ElseIf typeId = DataSourceTypeODBCSQLServer Then
        '                            '
        '                            ' MS SQL Server
        '                            '
        '                            SQL = "alter table cccountries add abbr VarChar(255) NULL"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                            '
        '                            SQL = "update cccountries set abbr=abbreviation"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                            '
        '                            SQL = "alter table cccountries drop column abbreviation"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                            '
        '                            SQL = "alter table cccountries add abbreviation VarChar(255) NULL"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                            '
        '                            SQL = "update cccountries set abbreviation=abbr"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                            '
        '                            SQL = "alter table cccountries drop column abbr"
        '                            Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                        End If
        '                    End If
        '                    '
        '                    ' remove all ccwebx3 and ccwebx4 addons
        '                    '
        '                    SQL = "delete from ccaggregatefunctions where ObjectProgramID like '%ccwebx3%'"
        '                    Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                End If
        '                If false Then
        '                    Call appendUpgradeLogAddStep(cpcore.app.config.name,MethodName, "4.1.508 upgrade")
        '                    '
        '                    ' repair the country table (abbreviation was set to an integer a long time ago)
        '                    '
        '                    Call cpcore.app.setSiteProperty("DefaultFormInputHTMLHeight", "500", 0)
        '                End If
        '                If false Then
        '                    Call appendUpgradeLogAddStep(cpcore.app.config.name,MethodName, "4.1.517 upgrade")
        '                    '
        '                    ' prepopulate docpcore.app.main_allowCrossLogin
        '                    '
        '                    SQL = "update ccDomains set allowCrossLogin=1 where typeid=1"
        '                    Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                End If
        '                If false Then
        '                    Call appendUpgradeLogAddStep(cpcore.app.config.name,MethodName, "4.1.588 upgrade")
        '                    '
        '                    ' ccfields.htmlContent redefined
        '                    '   means nothing for fields not set to html or htmlFile
        '                    '   for these, it means the initial editor should show the HTML (not wysiwyg) -- "real html"
        '                    '
        '                    SQL = "update ccFields set htmlcontent=null where (name<>'layout')or(contentid<>" & cpcore.app.csv_GetContentID("layouts") & ")"
        '                    Call cpcore.app.ExecuteSQL("", CStr(SQL))
        '                End If
        '                '
        '                ' return the normal timeout
        '                '
        '                cpcore.app.csv_SQLCommandTimeout = TimeoutSave
        '                '
        '                ' Regsiter and IISReset if needed
        '                '
        '                If RegisterList <> "" Then
        '                    Call addonInstall.RegisterActiveXFiles(RegisterList)
        '                    Call addonInstall.RegisterDotNet(RegisterList)
        '                    RegisterList = ""
        '                End If
        '                '
        '                ' IISReset if needed
        '                '
        '                If IISResetRequired Then
        '                    '
        '                    ' Restart IIS if stopped
        '                    '
        '                    runAtServer = New runAtServerClass
        '                    Call runAtServer.executeCmd("IISReset", "")
        '                End If
        '            End If

        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            UpgradeErrorCount = UpgradeErrorCount + 1
        '            Dim ex As New exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "Upgrade_Conversion_to_41 (error #" & UpgradeErrorCount & ")", True, True)
        '            If UpgradeErrorCount >= UpgradeErrorTheshold Then
        '                Dim ex4 As New Exception("todo") : Call HandleClassError(ex4, cpcore.app.config.name, MethodName) ' Err.Number, Err.Source, Err.Description, "Upgrade_Conversion_to_41 (error #" & UpgradeErrorCount & ")", True, False)
        '            End If
        '            Resume Next
        '        End Sub
        '        '
        '        ' ----- Delete unused fields from both the Content Definition and the Table
        '        '
        '        Private Sub DeleteField( ByVal DataSourceName As String, ByVal TableName As String, ByVal FieldName As String)
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim RSTables as datatable
        '            'Dim RSContent as datatable
        '            Dim TableID As Integer
        '            Dim ContentID As Integer
        '            '
        '            ' Delete any indexes found for this field
        '            '
        '            Call cpcore.app.csv_DeleteTableIndex(DataSourceName, TableName, TableName & FieldName)
        '            'Call cpcore.app.csv_DeleteTableIndex(DataSourceName, TableName, TableName & FieldName)
        '            '
        '            ' Delete all Content Definition Fields associated with this field
        '            '
        '            RSTables = cpcore.app.ExecuteSQL(DataSourceName, "SELECT ID from ccTables where name='" & TableName & "';")
        '            If Not (RSTables Is Nothing) Then
        '                Do While Not RSTables.EOF
        '                    TableID = EncodeInteger(RSTables("ID"))
        '                    RSContent = cpcore.app.ExecuteSQL(DataSourceName, "Select ID from ccContent where (ContentTableID=" & TableID & ")or(AuthoringTableID=" & TableID & ");")
        '                    If Not (RSContent Is Nothing) Then
        '                        Do While Not RSContent.EOF
        '                            ContentID = EncodeInteger(RSContent("ID"))
        '                            Call cpcore.app.ExecuteSQL(DataSourceName, "Delete From ccFields where (ContentID=" & ContentID & ")and(name=" & encodeSQLText(FieldName) & ");")
        '                            RSContent.MoveNext()
        '                        Loop
        '                        Call RSContent.Close()
        '                    End If
        '                    RSContent = Nothing
        '                    RSTables.MoveNext()
        '                Loop
        '                Call RSTables.Close()
        '            End If
        '            RSTables = Nothing
        '            '
        '            ' Drop the field from the table
        '            '
        '            Call cpcore.app.csv_DeleteTableField(DataSourceName, TableName, FieldName)
        '            '
        '            Exit Sub
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "deleteField") ' Err.Number, Err.Source, Err.Description, "DeleteField", True, False)
        '        End Sub
        '        '
        '        '   Returns TableID
        '        '       -1 if table not found
        '        '
        '        Private Function GetTableID( ByVal TableName As String) As Integer
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim SQL As String
        '            dim dt as datatable
        '            '
        '            GetTableID = -1
        '            RS = cpcore.app.ExecuteSQL( "Select ID from ccTables where name=" & encodeSQLText(TableName) & ";")
        '            If (isDataTableOk(rs)) Then
        '                If Not rs.rows.count=0 Then
        '                    GetTableID = cpcore.app.getDataRowColumnName(RS.rows(0), "ID")
        '                End If
        '            End If
        '            '
        '            Exit Function
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "GetTableID", True, False)
        '        End Function
        '        '
        '        '
        '        '
        '        Private Function core_group_add(ByVal GroupName As String) As Integer
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim dt As DataTable
        '            Dim sql As String
        '            Dim createkey As Integer = GetRandomInteger()
        '            Dim cid As Integer
        '            '
        '            core_group_add = 0
        '            dt = cpCore.app.executeSql("SELECT ID FROM CCGROUPS WHERE NAME='" & GroupName & "';")
        '            If dt.Rows.Count > 0 Then
        '                core_group_add = EncodeInteger(dt.Rows(0).Item("ID"))
        '            Else
        '                cid = GetContentID("groups")
        '                sql = "insert into ccgroups (contentcontrolid,active,createkey,name,caption) values (" & cid & ",1," & createkey & "," & EncodeSQLText(GroupName) & "," & EncodeSQLText(GroupName) & ")"
        '                Call cpCore.app.executeSql(sql)
        '                sql = "select id from ccgroups where createkey=" & createkey & " order by id desc"
        '                dt = cpCore.app.executeSql(sql)
        '                If dt.Rows.Count > 0 Then
        '                    core_group_add = EncodeInteger(dt.Rows(0).Item("id"))
        '                End If

        '            End If
        '            '
        '            Exit Function
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call handleClassException(ex, cpCore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "AddGroup", True, False)
        '        End Function
        '
        '
        '
        Private Sub VerifyAdminMenus(ByVal DataBuildVersion As String)
            Try
                '
                Dim MethodName As String
                Dim EntryName As String
                Dim HeaderName As String
                Dim EntryID As Object
                Dim HeaderID As Object
                Dim dt As DataTable
                Dim CSHeaderEntry As Integer
                Dim SQL As String
                Dim ReportQuery As String
                '
                MethodName = "VerifyAdminMenus"
                '
                If False Then
                    Exit Sub
                End If
                '
                ' ----- remove duplicate menus that may have been added during faulty upgrades
                '
                Dim FieldNew As String
                Dim FieldLast As String
                Dim FieldRecordID As Integer
                'Dim dt As DataTable
                dt = cpCore.db.executeSql("Select ID,Name,ParentID from ccMenuEntries where (active<>0) Order By ParentID,Name")
                If dt.Rows.Count > 0 Then
                    FieldLast = ""
                    For rowptr = 0 To dt.Rows.Count - 1
                        FieldNew = EncodeText(dt.Rows(rowptr).Item("name")) & "." & EncodeText(dt.Rows(rowptr).Item("parentid"))
                        If (FieldNew = FieldLast) Then
                            FieldRecordID = EncodeInteger(dt.Rows(rowptr).Item("ID"))
                            Call cpCore.db.executeSql("Update ccMenuEntries set active=0 where ID=" & FieldRecordID & ";")
                        End If
                        FieldLast = FieldNew
                    Next
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '        '
        '        ' Get the Menu for FormInputHTML
        '        '
        '        Private Sub VerifyEditorOptions()
        '            On Error GoTo ErrorTrap
        '            '
        '            If Not (False) Then
        '                Call VerifyEditorOptions_FontFace()
        '                Call VerifyEditorOptions_FontSize()
        '                Call VerifyEditorOptions_FontColor()
        '                Call VerifyEditorOptions_Paragraph()
        '                Call VerifyEditorOptions_Styles()
        '            End If
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "VerifyEditorOptions", True, True)
        '        End Sub
        '
        ' Get the Menu for FormInputHTML
        '
        Private Sub VerifyRecord(ByVal ContentName As String, ByVal Name As String, Optional ByVal CodeFieldName As String = "", Optional ByVal Code As String = "", Optional ByVal InActive As Boolean = False)
            Try
                '
                Dim CS As Integer
                Dim Active As Boolean
                Dim dt As DataTable
                'Dim tableName As String
                Dim sql1 As String
                Dim sql2 As String
                Dim sql3 As String
                '
                Active = Not InActive
                'Dim createkey As Integer = GetRandomInteger()
                Dim cdef As coreMetaDataClass.CDefClass = cpCore.metaData.getCdef(ContentName)
                Dim tableName As String = cdef.ContentTableName
                Dim cid As Integer = cdef.Id
                '
                dt = cpCore.db.executeSql("SELECT ID FROM " & tableName & " WHERE NAME=" & cpCore.db.encodeSQLText(Name) & ";")
                If dt.Rows.Count = 0 Then
                    ' cid = GetContentID(ContentName)
                    'tableName = cpcore.app.csv_GetContentTablename(ContentName)
                    sql1 = "insert into " & tableName & " (contentcontrolid,createkey,active,name"
                    sql2 = ") values (" & cid & ",0," & cpCore.db.encodeSQLBoolean(Active) & "," & cpCore.db.encodeSQLText(Name)
                    sql3 = ")"
                    If CodeFieldName <> "" Then
                        sql1 &= "," & CodeFieldName
                        sql2 &= "," & Code
                    End If
                    Call cpCore.db.executeSql(sql1 & sql2 & sql3)
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '        '
        '        ' Get the Menu for FormInputHTML
        '        '
        '        Private Sub VerifyEditorOptions_FontFace()
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim CS As Integer
        '            '
        '            If Not (False) Then
        '                CS = cpcore.app.csOpen("Editor Font Options", , "name", , , , , "ID")
        '                If Not cpcore.app.csv_IsCSOK(CS) Then
        '                    Call VerifyRecord( "Editor Font Options", "Arial", "Code", "Arial")
        '                    Call VerifyRecord( "Editor Font Options", "Helvetica", "Code", "Helvetica")
        '                    Call VerifyRecord( "Editor Font Options", "Verdana", "Code", "Verdana")
        '                    Call VerifyRecord( "Editor Font Options", "Geneva", "Code", "Geneva")
        '                End If
        '                Call cpcore.app.csv_CloseCS(CS)
        '            End If
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "VerifyEditorOptions_FontFace", True, True)
        '        End Sub
        '        '
        '        ' Get the Menu for FormInputHTML
        '        '
        '        Private Sub VerifyEditorOptions_FontSize()
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim CS As Integer
        '            '
        '            If Not (False) Then
        '                CS = cpcore.app.csOpen("Editor Font Size Options", , "name", , , , , "ID")
        '                If Not cpcore.app.csv_IsCSOK(CS) Then
        '                    Call VerifyRecord( "Editor Font Size Options", "Small (1)", "Code", "1")
        '                    Call VerifyRecord( "Editor Font Size Options", "Normal (3)", "Code", "3")
        '                    Call VerifyRecord( "Editor Font Size Options", "Large (5)", "Code", "5")
        '                End If
        '                Call cpcore.app.csv_CloseCS(CS)
        '            End If
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "VerifyEditorOptions_FontSize", True, True)
        '        End Sub
        '        '
        '        ' Get the Menu for FormInputHTML
        '        '
        '        Private Sub VerifyEditorOptions_FontColor()
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim CS As Integer
        '            Dim ContentName As String
        '            '
        '            If Not (False) Then
        '                ContentName = "Editor Font Color Options"
        '                CS = cpcore.app.csOpen(ContentName, , "name", , , , , "ID")
        '                If Not cpcore.app.csv_IsCSOK(CS) Then
        '                    Call VerifyRecord( ContentName, "red", "Code", "#FF0000")
        '                    Call VerifyRecord( ContentName, "green", "Code", "#00FF00")
        '                    Call VerifyRecord( ContentName, "blue", "Code", "#0000FF")
        '                End If
        '                Call cpcore.app.csv_CloseCS(CS)
        '            End If
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "VerifyEditorOptions_FontColor", True, True)
        '        End Sub
        '        '
        '        ' Get the Menu for FormInputHTML
        '        '
        '        Private Sub VerifyEditorOptions_Paragraph()
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim CS As Integer
        '            Dim ContentName As String
        '            '
        '            If Not (False) Then
        '                ContentName = "Editor Paragraph Options"
        '                CS = cpcore.app.csOpen(ContentName, , "name", , , , , "ID")
        '                If Not cpcore.app.csv_IsCSOK(CS) Then
        '                    Call VerifyRecord( ContentName, "Headline 1", "Code", "<H1>")
        '                    Call VerifyRecord( ContentName, "Headline 2", "Code", "<H2>")
        '                    Call VerifyRecord( ContentName, "Headline 3", "Code", "<H3>")
        '                    Call VerifyRecord( ContentName, "Headline 4", "Code", "<H4>")
        '                    Call VerifyRecord( ContentName, "Headline 5", "Code", "<H5>")
        '                    Call VerifyRecord( ContentName, "Headline 6", "Code", "<H6>")
        '                    Call VerifyRecord( ContentName, "Normal", "Code", "<P>")
        '                    Call VerifyRecord( ContentName, "Preformatted", "Code", "<PRE>")
        '                End If
        '                Call cpcore.app.csv_CloseCS(CS)
        '            End If
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "VerifyEditorOptions_Paragraph", True, True)
        'End Sub
        '        '
        '        ' Get the Menu for FormInputHTML
        '        '
        '        Private Sub VerifyEditorOptions_Styles()
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim CS As Integer
        '            '
        '            If Not (False) Then
        '                CS = cpcore.app.csOpen("Editor Style Options", , "name", , , , , "ID")
        '                If Not cpcore.app.csv_IsCSOK(CS) Then
        '                    Call VerifyRecord( "Editor Style Options", "Default", "Code", "default", True)
        '                    Call VerifyRecord( "Editor Style Options", "ccSmall", "Code", "ccSmall", True)
        '                    Call VerifyRecord( "Editor Style Options", "ccNormal", "Code", "ccNormal", True)
        '                    Call VerifyRecord( "Editor Style Options", "ccLarge", "Code", "ccLarge", True)
        '                    Call VerifyRecord( "Editor Style Options", "ccError", "Code", "ccError", True)
        '                    Call VerifyRecord( "Editor Style Options", "ccHeadline", "Code", "ccHeadline", True)
        '                End If
        '                Call cpcore.app.csv_CloseCS(CS)
        '            End If
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "VerifyEditorOptions_Styles", True, True)
        '        End Sub
        '        '
        '        '========================================================================
        '        '   Load content from an XML file
        '        '========================================================================
        '        '
        '        public Sub VerifyXMLContentNode( ByVal ContentNode As XmlNode)
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim FieldCount As Integer
        '            Dim FieldSize As Integer
        '            ' converted array to dictionary - Dim FieldPointer As Integer
        '            Dim Fields() As fieldTypePrivate
        '            '
        '            Dim XMLTools As New xmlToolsClass()
        '            Dim FieldNode As XmlNode
        '            Dim RecordNode As XmlNode
        '            Dim CDefFieldsNode As XmlNode
        '            Dim RowNode As XmlNode
        '            'Dim FieldNode As XmlNode
        '            '
        '            Dim ContentName As String
        '            Dim FieldName As String
        '            Dim DataSourceName As String
        '            Dim NodeAttribute As XmlAttribute
        '            Dim SQL As String
        '            Dim SQLDelimiter As String
        '            Dim DataSourcePointer As Object
        '            Dim DefaultValue As String
        '            Dim Found As Boolean
        '            '
        '            ContentName = GetXMLAttribute( Found, ContentNode, "name", "")
        '            '
        '            Call appendUpgradeLogAddStep(cpcore.app.config.name,"VerifyXMLContentNode", "Load Content Records [" & ContentName & "]")
        '            '
        '            DataSourceName = GetXMLAttribute( Found, ContentNode, "dataSource", "Default")
        '            'DataSourceName = GetXMLAttribute(Found, ContentNode, "dataSource")
        '            'If DataSourceName = "" Then
        '            '    DataSourceName = "Default"
        '            '    End If
        '            '
        '            ' ----- load the definition
        '            '
        '            FieldSize = 0
        '            FieldCount = 0
        '            For Each RowNode In ContentNode.childNodes
        '                '
        '                ' ----- process rows
        '                '
        '                If vbUCase(RowNode.Name) = "ROW" Then
        '                    For Each FieldNode In RowNode.ChildNodes
        '                        If vbUCase(RowNode.Name) = "FIELD" Then
        '                            Call VerifyRecord( ContentName, GetXMLAttribute( Found, RowNode, "name", ""), "CreateKey", "0")
        '                        End If
        '                    Next
        '                End If
        '            Next
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "VerifyXMLContentNode", True, True)
        '            Resume Next
        '        End Sub
        '
        '========================================================================
        ' ----- Upgrade Conversion
        '========================================================================
        '
        Private Sub Upgrade_Conversion(ByVal DataBuildVersion As String)
            Try
                Dim dt As DataTable
                Dim CID As Integer
                Dim SQL As String
                Dim rowPtr As Integer
                '
                '---------------------------------------------------------------------
                '   moved from right after core table creation
                '   If pre-3.0.300 core table changes only
                '   required so the rest of the system will operate during upgrade
                '   convert Setup to Site Properties, populate ccTables, populate ccFields.Authorable from .active, set .active true
                '---------------------------------------------------------------------
                '
                If False Then
                    Call appendUpgradeLogAddStep(cpCore.serverconfig.appConfig.name, "Upgrade_Conversion", "4.2.414, convert all ccaggregateFunctions to add-ons")
                    '
                    ' remove all non add-on contentdefs for ccaggregatefunctions
                    '
                    CID = cpCore.db.getContentId("Add-ons")
                    If CID <> 0 Then
                        Call cpCore.db.executeSql("update ccaggregatefunctions set contentcontrolid=" & CID)
                        Call cpCore.db.executeSql("delete from cccontent where id in (select c.id from cccontent c left join cctables t on t.id=c.contenttableid where t.name='ccAggregateFunctions' and c.id<>" & CID & ")")
                    End If
                End If
                '
                ' iis virtual path /appName/files/ becomes publicFileContentPathPrefix
                '
                'cpcore.app.config.contentFilePathPrefix = "/" & cpCore.app.config.name & "/files/"
                'Call cpCore.app.siteProperty_getText("publicFileContentPathPrefix", cpcore.app.config.contentFilePathPrefix)
                '
                '---------------------------------------------------------------------
                ' ----- Roll the style sheet cache if it is setup
                '---------------------------------------------------------------------
                '
                Call cpCore.siteProperties.setProperty("StylesheetSerialNumber", CStr(-1))
                '
                ' ----- Reload CSv
                '
                Call cpCore.cache.invalidateAll()
                Call cpCore.metaData.clear()
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '=========================================================================================
        '
        '=========================================================================================
        '
        Public Sub VerifyTableCoreFields()
            Try
                '
                Dim IDVariant As Integer
                Dim Active As Boolean
                Dim TableName As String
                Dim DataSourceName As String
                Dim MethodName As String
                Dim SQL As String
                Dim dt As DataTable
                Dim ptr As Integer
                '
                MethodName = "VerifyTableCoreFields"
                '
                Call appendUpgradeLogAddStep(cpCore.serverconfig.appConfig.name, MethodName, "Verify core fields in all tables registered in [Tables] content.")
                '
                SQL = "SELECT ccDataSources.Name as DataSourceName, ccDataSources.ID as DataSourceID, ccDataSources.Active as DataSourceActive, ccTables.Name as TableName" _
                & " FROM ccTables LEFT JOIN ccDataSources ON ccTables.DataSourceID = ccDataSources.ID" _
                & " Where (((ccTables.active) <> 0))" _
                & " ORDER BY ccDataSources.Name, ccTables.Name;"
                dt = cpCore.db.executeSql(SQL)
                ptr = 0
                Do While (ptr < dt.Rows.Count)
                    IDVariant = EncodeInteger(dt.Rows(ptr).Item("DataSourceID"))
                    If (IDVariant = 0) Then
                        Active = True
                        DataSourceName = "Default"
                    Else
                        Active = EncodeBoolean(dt.Rows(ptr).Item("DataSourceActive"))
                        DataSourceName = EncodeText(dt.Rows(ptr).Item("DataSourcename"))
                    End If
                    If Active Then
                        Call cpCore.db.createSQLTable(DataSourceName, EncodeText(dt.Rows(ptr).Item("Tablename")))
                    End If
                    ptr += 1
                Loop
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '=========================================================================================
        '
        '=========================================================================================
        '
        Public Sub VerifyScriptingRecords()
            Try
                '
                Call appendUpgradeLogAddStep(cpCore.serverconfig.appConfig.name, "VerifyScriptingRecords", "Verify Scripting Records.")
                '
                Call VerifyRecord("Scripting Languages", "VBScript", "", "")
                Call VerifyRecord("Scripting Languages", "JScript", "", "")
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '=========================================================================================
        '
        '=========================================================================================
        '
        Public Sub VerifyLanguageRecords()
            Try
                '
                Call appendUpgradeLogAddStep(cpCore.serverconfig.appConfig.name, "VerifyLanguageRecords", "Verify Language Records.")
                '
                Call VerifyRecord("Languages", "English", "HTTP_Accept_Language", "'en'")
                Call VerifyRecord("Languages", "Spanish", "HTTP_Accept_Language", "'es'")
                Call VerifyRecord("Languages", "French", "HTTP_Accept_Language", "'fr'")
                Call VerifyRecord("Languages", "Any", "HTTP_Accept_Language", "'any'")
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '   Verify Library Folder records
        '
        Private Sub VerifyLibraryFolders()
            Try
                '
                Dim CS As Integer
                Dim dt As DataTable
                '
                Call appendUpgradeLogAddStep(cpCore.serverconfig.appConfig.name, "VerifyLibraryFolders", "Verify Library Folders: Images and Downloads")
                '
                dt = cpCore.db.executeSql("select id from cclibraryfiles")
                If dt.Rows.Count = 0 Then
                    Call VerifyRecord("Library Folders", "Images")
                    Call VerifyRecord("Library Folders", "Downloads")
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '        '
        '        '   Verify ContentWatchLists
        '        '
        '        Private Sub VerifyContentWatchLists()
        '            On Error GoTo ErrorTrap
        '            '
        '            Dim CS As Integer
        '            Dim FieldName As String
        '            '
        '            Call appendUpgradeLogAddStep(cpcore.app.config.name,"VerifyContentWatchLists", "Verify Content Watch Lists: What's New and What's Related")
        '            '
        '            If Not (False) Then
        '                CS = cpcore.app.csOpen("Content Watch Lists", , "name", , , , , "ID")
        '                If Not cpcore.app.csv_IsCSOK(CS) Then
        '                    Call VerifyRecord( "Content Watch Lists", "What's New", "Active", "1", True)
        '                    Call VerifyRecord( "Content Watch Lists", "What's Related", "Active", "1", True)
        '                End If
        '                Call cpcore.app.csv_CloseCS(CS)
        '            End If
        '            '
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call HandleClassError(ex, cpcore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "VerifyContentWatchLists", True, True)
        ''        End Sub
        ''
        ''=============================================================================
        ''
        ''=============================================================================
        ''
        'Private Function VerifySurveyQuestionTypes() As Integer
        '    Dim returnType As Integer = 0
        '    Try
        '        '
        '        Dim rs As DataTable
        '        Dim RowsFound As Integer
        '        Dim CID As Integer
        '        Dim TableBad As Boolean
        '        Dim RowsNeeded As Integer
        '        '
        '        Call appendUpgradeLogAddStep(cpCore.app.config.name, "VerifySurveyQuestionTypes", "Verify Survey Question Types")
        '        '
        '        ' ----- make sure there are enough records
        '        '
        '        TableBad = False
        '        RowsFound = 0
        '        rs = cpCore.app.executeSql("Select ID from ccSurveyQuestionTypes order by id")
        '        If (Not isDataTableOk(rs)) Then
        '            '
        '            ' problem
        '            '
        '            TableBad = True
        '        Else
        '            '
        '            ' Verify the records that are there
        '            '
        '            RowsFound = 0
        '            For Each rsDr As DataRow In rs.Rows
        '                RowsFound = RowsFound + 1
        '                If RowsFound <> EncodeInteger(rsDr("ID")) Then
        '                    '
        '                    ' Bad Table
        '                    '
        '                    TableBad = True
        '                    Exit For
        '                End If
        '            Next
        '        End If
        '        rs.Dispose()
        '        '
        '        ' ----- Replace table if needed
        '        '
        '        If TableBad Then
        '            Call cpCore.app.csv_DeleteTable("Default", "ccSurveyQuestionTypes")
        '            'Call cpcore.app.ExecuteSQL( "Drop table ccSurveyQuestionTypes")
        '            Call cpCore.app.CreateSQLTable("Default", "ccSurveyQuestionTypes")
        '            RowsFound = 0
        '        End If
        '        '
        '        ' ----- Add the number of rows needed
        '        '
        '        RowsNeeded = 3 - RowsFound
        '        If RowsNeeded > 0 Then
        '            CID = cpCore.app.csv_GetContentID("Survey Question Types")
        '            If CID <= 0 Then
        '                '
        '                ' Problem
        '                '
        '                Call Err.Raise(ignoreInteger, "dll", "Survey Question Types content definition was not found")
        '            Else
        '                Do While RowsNeeded > 0
        '                    Call cpCore.app.executeSql("Insert into ccSurveyQuestionTypes (active,contentcontrolid)values(1," & CID & ")")
        '                    RowsNeeded = RowsNeeded - 1
        '                Loop
        '            End If
        '        End If
        '        '
        '        ' ----- Update the Names of each row
        '        '
        '        Call cpCore.app.executeSql("Update ccSurveyQuestionTypes Set Name='Text Field' where ID=1;")
        '        Call cpCore.app.executeSql("Update ccSurveyQuestionTypes Set Name='Select Dropdown' where ID=2;")
        '        Call cpCore.app.executeSql("Update ccSurveyQuestionTypes Set Name='Radio Buttons' where ID=3;")
        '    Catch ex As Exception
        '        cpCore.handleException(ex)
        '    End Try
        '    Return returnType
        'End Function
        '        '
        '        '=============================================================================
        '        '
        '        '=============================================================================
        '        '
        '        Private Function DeleteAdminMenu(ByVal MenuName As String, ByVal ParentMenuName As String) As Integer
        '            Dim returnAttr As String = ""
        '            Try

        '            Catch ex As Exception
        '                cpCore.handleException(ex)
        '            End Try
        '            Return returnAttr

        '            On Error GoTo ErrorTrap
        '            '
        '            Dim ParentID As Integer
        '            '
        '            If ParentMenuName <> "" Then
        '                ParentID = GetIDBYName("ccMenuEntries", ParentMenuName)
        '                Call cpCore.app.executeSql("Delete from ccMenuEntries where name=" & EncodeSQLText(MenuName) & " and ParentID=" & ParentID)
        '            Else
        '                Call cpCore.app.executeSql("Delete from ccMenuEntries where name=" & EncodeSQLText(MenuName))
        '            End If
        '            '
        '            Exit Function
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            Dim ex As New Exception("todo") : Call handleClassException(ex, cpCore.app.config.name, "methodNameFPO") ' Err.Number, Err.Source, Err.Description, "DeleteAdminMenu", True, False)
        '        End Function
        '
        '=============================================================================
        '
        '=============================================================================
        '
        Private Function GetIDBYName(ByVal TableName As String, ByVal RecordName As String) As Integer
            Dim returnid As Integer = 0
            Try
                '
                Dim rs As DataTable
                '
                rs = cpCore.db.executeSql("Select ID from " & TableName & " where name=" & cpCore.db.encodeSQLText(RecordName))
                If isDataTableOk(rs) Then
                    GetIDBYName = EncodeInteger(rs.Rows(0).Item("ID"))
                End If
                rs.Dispose()
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnid
        End Function
        '
        '   Verify Library Folder records
        '
        Private Sub VerifyLibraryFileTypes()
            Try
                '
                Dim CS As Integer
                Dim ContentID As Integer
                Dim VerifyRecords As Boolean
                '
                If Not (False) Then
                    '
                    ' Load basic records -- default images are handled in the REsource Library through the /ccLib/config/DefaultValues.txt GetDefaultValue(key) mechanism
                    '
                    If cpCore.db.getRecordID("Library File Types", "Image") = 0 Then
                        Call VerifyRecord("Library File Types", "Image", "ExtensionList", "'GIF,JPG,JPE,JPEG,BMP,PNG'", False)
                        Call VerifyRecord("Library File Types", "Image", "IsImage", "1", False)
                        Call VerifyRecord("Library File Types", "Image", "IsVideo", "0", False)
                        Call VerifyRecord("Library File Types", "Image", "IsDownload", "1", False)
                        Call VerifyRecord("Library File Types", "Image", "IsFlash", "0", False)
                    End If
                    '
                    If cpCore.db.getRecordID("Library File Types", "Video") = 0 Then
                        Call VerifyRecord("Library File Types", "Video", "ExtensionList", "'ASX,AVI,WMV,MOV,MPG,MPEG,MP4,QT,RM'", False)
                        Call VerifyRecord("Library File Types", "Video", "IsImage", "0", False)
                        Call VerifyRecord("Library File Types", "Video", "IsVideo", "1", False)
                        Call VerifyRecord("Library File Types", "Video", "IsDownload", "1", False)
                        Call VerifyRecord("Library File Types", "Video", "IsFlash", "0", False)
                    End If
                    '
                    If cpCore.db.getRecordID("Library File Types", "Audio") = 0 Then
                        Call VerifyRecord("Library File Types", "Audio", "ExtensionList", "'AIF,AIFF,ASF,CDA,M4A,M4P,MP2,MP3,MPA,WAV,WMA'", False)
                        Call VerifyRecord("Library File Types", "Audio", "IsImage", "0", False)
                        Call VerifyRecord("Library File Types", "Audio", "IsVideo", "0", False)
                        Call VerifyRecord("Library File Types", "Audio", "IsDownload", "1", False)
                        Call VerifyRecord("Library File Types", "Audio", "IsFlash", "0", False)
                    End If
                    '
                    If cpCore.db.getRecordID("Library File Types", "Word") = 0 Then
                        Call VerifyRecord("Library File Types", "Word", "ExtensionList", "'DOC'", False)
                        Call VerifyRecord("Library File Types", "Word", "IsImage", "0", False)
                        Call VerifyRecord("Library File Types", "Word", "IsVideo", "0", False)
                        Call VerifyRecord("Library File Types", "Word", "IsDownload", "1", False)
                        Call VerifyRecord("Library File Types", "Word", "IsFlash", "0", False)
                    End If
                    '
                    If cpCore.db.getRecordID("Library File Types", "Flash") = 0 Then
                        Call VerifyRecord("Library File Types", "Flash", "ExtensionList", "'SWF'", False)
                        Call VerifyRecord("Library File Types", "Flash", "IsImage", "0", False)
                        Call VerifyRecord("Library File Types", "Flash", "IsVideo", "0", False)
                        Call VerifyRecord("Library File Types", "Flash", "IsDownload", "1", False)
                        Call VerifyRecord("Library File Types", "Flash", "IsFlash", "1", False)
                    End If
                    '
                    If cpCore.db.getRecordID("Library File Types", "PDF") = 0 Then
                        Call VerifyRecord("Library File Types", "PDF", "ExtensionList", "'PDF'", False)
                        Call VerifyRecord("Library File Types", "PDF", "IsImage", "0", False)
                        Call VerifyRecord("Library File Types", "PDF", "IsVideo", "0", False)
                        Call VerifyRecord("Library File Types", "PDF", "IsDownload", "1", False)
                        Call VerifyRecord("Library File Types", "PDF", "IsFlash", "0", False)
                    End If
                    '
                    If cpCore.db.getRecordID("Library File Types", "XLS") = 0 Then
                        Call VerifyRecord("Library File Types", "Excel", "ExtensionList", "'XLS'", False)
                        Call VerifyRecord("Library File Types", "Excel", "IsImage", "0", False)
                        Call VerifyRecord("Library File Types", "Excel", "IsVideo", "0", False)
                        Call VerifyRecord("Library File Types", "Excel", "IsDownload", "1", False)
                        Call VerifyRecord("Library File Types", "Excel", "IsFlash", "0", False)
                    End If
                    '
                    If cpCore.db.getRecordID("Library File Types", "PPT") = 0 Then
                        Call VerifyRecord("Library File Types", "Power Point", "ExtensionList", "'PPT,PPS'", False)
                        Call VerifyRecord("Library File Types", "Power Point", "IsImage", "0", False)
                        Call VerifyRecord("Library File Types", "Power Point", "IsVideo", "0", False)
                        Call VerifyRecord("Library File Types", "Power Point", "IsDownload", "1", False)
                        Call VerifyRecord("Library File Types", "Power Point", "IsFlash", "0", False)
                    End If
                    '
                    If cpCore.db.getRecordID("Library File Types", "Default") = 0 Then
                        Call VerifyRecord("Library File Types", "Default", "ExtensionList", "''", False)
                        Call VerifyRecord("Library File Types", "Default", "IsImage", "0", False)
                        Call VerifyRecord("Library File Types", "Default", "IsVideo", "0", False)
                        Call VerifyRecord("Library File Types", "Default", "IsDownload", "1", False)
                        Call VerifyRecord("Library File Types", "Default", "IsFlash", "0", False)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '=========================================================================================
        '
        '=========================================================================================
        '
        Private Sub VerifyState(ByVal Name As String, ByVal Abbreviation As String, ByVal SaleTax As Double, ByVal CountryID As Integer, ByVal FIPSState As String)
            Try
                '
                Dim CS As Integer
                Const ContentName = "States"
                '
                CS = cpCore.db.cs_open(ContentName, "name=" & cpCore.db.encodeSQLText(Name), , False)
                If Not cpCore.db.cs_ok(CS) Then
                    '
                    ' create new record
                    '
                    Call cpCore.db.cs_Close(CS)
                    CS = cpCore.db.cs_insertRecord(ContentName, SystemMemberID)
                    Call cpCore.db.cs_setField(CS, "NAME", Name)
                    Call cpCore.db.cs_setField(CS, "ACTIVE", True)
                    Call cpCore.db.cs_setField(CS, "Abbreviation", Abbreviation)
                    Call cpCore.db.cs_setField(CS, "CountryID", CountryID)
                    Call cpCore.db.cs_setField(CS, "FIPSState", FIPSState)
                Else
                    '
                    ' verify only fields needed for contensive
                    '
                    Call cpCore.db.cs_setField(CS, "CountryID", CountryID)
                    Call cpCore.db.cs_setField(CS, "Abbreviation", Abbreviation)
                End If
                Call cpCore.db.cs_Close(CS)
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '=========================================================================================
        '
        '=========================================================================================
        '
        Public Sub VerifyStates()
            Try
                '
                Call appendUpgradeLogAddStep(cpCore.serverconfig.appConfig.name, "VerifyStates", "Verify States")
                '
                Dim CountryID As Integer
                '
                Call VerifyCountry("United States", "US")
                CountryID = cpCore.db.getRecordID("Countries", "United States")
                '
                Call VerifyState("Alaska", "AK", 0.0#, CountryID, "")
                Call VerifyState("Alabama", "AL", 0.0#, CountryID, "")
                Call VerifyState("Arizona", "AZ", 0.0#, CountryID, "")
                Call VerifyState("Arkansas", "AR", 0.0#, CountryID, "")
                Call VerifyState("California", "CA", 0.0#, CountryID, "")
                Call VerifyState("Connecticut", "CT", 0.0#, CountryID, "")
                Call VerifyState("Colorado", "CO", 0.0#, CountryID, "")
                Call VerifyState("Delaware", "DE", 0.0#, CountryID, "")
                Call VerifyState("District of Columbia", "DC", 0.0#, CountryID, "")
                Call VerifyState("Florida", "FL", 0.0#, CountryID, "")
                Call VerifyState("Georgia", "GA", 0.0#, CountryID, "")

                Call VerifyState("Hawaii", "HI", 0.0#, CountryID, "")
                Call VerifyState("Idaho", "ID", 0.0#, CountryID, "")
                Call VerifyState("Illinois", "IL", 0.0#, CountryID, "")
                Call VerifyState("Indiana", "IN", 0.0#, CountryID, "")
                Call VerifyState("Iowa", "IA", 0.0#, CountryID, "")
                Call VerifyState("Kansas", "KS", 0.0#, CountryID, "")
                Call VerifyState("Kentucky", "KY", 0.0#, CountryID, "")
                Call VerifyState("Louisiana", "LA", 0.0#, CountryID, "")
                Call VerifyState("Massachusetts", "MA", 0.0#, CountryID, "")
                Call VerifyState("Maine", "ME", 0.0#, CountryID, "")

                Call VerifyState("Maryland", "MD", 0.0#, CountryID, "")
                Call VerifyState("Michigan", "MI", 0.0#, CountryID, "")
                Call VerifyState("Minnesota", "MN", 0.0#, CountryID, "")
                Call VerifyState("Missouri", "MO", 0.0#, CountryID, "")
                Call VerifyState("Mississippi", "MS", 0.0#, CountryID, "")
                Call VerifyState("Montana", "MT", 0.0#, CountryID, "")
                Call VerifyState("North Carolina", "NC", 0.0#, CountryID, "")
                Call VerifyState("Nebraska", "NE", 0.0#, CountryID, "")
                Call VerifyState("New Hampshire", "NH", 0.0#, CountryID, "")
                Call VerifyState("New Mexico", "NM", 0.0#, CountryID, "")

                Call VerifyState("New Jersey", "NJ", 0.0#, CountryID, "")
                Call VerifyState("New York", "NY", 0.0#, CountryID, "")
                Call VerifyState("Nevada", "NV", 0.0#, CountryID, "")
                Call VerifyState("North Dakota", "ND", 0.0#, CountryID, "")
                Call VerifyState("Ohio", "OH", 0.0#, CountryID, "")
                Call VerifyState("Oklahoma", "OK", 0.0#, CountryID, "")
                Call VerifyState("Oregon", "OR", 0.0#, CountryID, "")
                Call VerifyState("Pennsylvania", "PA", 0.0#, CountryID, "")
                Call VerifyState("Rhode Island", "RI", 0.0#, CountryID, "")
                Call VerifyState("South Carolina", "SC", 0.0#, CountryID, "")

                Call VerifyState("South Dakota", "SD", 0.0#, CountryID, "")
                Call VerifyState("Tennessee", "TN", 0.0#, CountryID, "")
                Call VerifyState("Texas", "TX", 0.0#, CountryID, "")
                Call VerifyState("Utah", "UT", 0.0#, CountryID, "")
                Call VerifyState("Vermont", "VT", 0.0#, CountryID, "")
                Call VerifyState("Virginia", "VA", 0.045, CountryID, "")
                Call VerifyState("Washington", "WA", 0.0#, CountryID, "")
                Call VerifyState("Wisconsin", "WI", 0.0#, CountryID, "")
                Call VerifyState("West Virginia", "WV", 0.0#, CountryID, "")
                Call VerifyState("Wyoming", "WY", 0.0#, CountryID, "")
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        ' Get the Menu for FormInputHTML
        '
        Private Sub VerifyCountry(ByVal Name As String, ByVal Abbreviation As String)
            Try
                '
                Dim CS As Integer
                Dim Active As Boolean
                '
                CS = cpCore.db.cs_open("Countries", "name=" & cpCore.db.encodeSQLText(Name))
                If Not cpCore.db.cs_ok(CS) Then
                    Call cpCore.db.cs_Close(CS)
                    CS = cpCore.db.cs_insertRecord("Countries", SystemMemberID)
                    If cpCore.db.cs_ok(CS) Then
                        Call cpCore.db.cs_setField(CS, "ACTIVE", True)
                    End If
                End If
                If cpCore.db.cs_ok(CS) Then
                    Call cpCore.db.cs_setField(CS, "NAME", Name)
                    Call cpCore.db.cs_setField(CS, "Abbreviation", Abbreviation)
                    If vbLCase(Name) = "united states" Then
                        Call cpCore.db.cs_set(CS, "DomesticShipping", "1")
                    End If
                End If
                Call cpCore.db.cs_Close(CS)
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '=========================================================================================
        '
        '=========================================================================================
        '
        Public Sub VerifyCountries()
            Try
                '
                Dim list As String
                Dim Rows() As String
                Dim RowPtr As Integer
                Dim Row As String
                Dim Attr() As String
                Dim Name As String
                'Dim fs As New fileSystemClass
                '
                Call appendUpgradeLogAddStep(cpCore.serverconfig.appConfig.name, "VerifyCountries", "Verify Countries")
                '
                list = cpCore.appRootFiles.readFile("cclib\config\isoCountryList.txt")
                Rows = Split(list, vbCrLf)
                For RowPtr = 0 To UBound(Rows)
                    Row = Rows(RowPtr)
                    If Row <> "" Then
                        Attr = Split(Row, ";")
                        If UBound(Attr) >= 1 Then
                            Name = Attr(0)
                            Name = EncodeInitialCaps(Name)
                            Call VerifyCountry(Name, Attr(1))
                        End If
                    End If
                Next
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '=========================================================================================
        '
        '=========================================================================================
        '
        Public Sub VerifyDefaultGroups()
            Try
                '
                Dim GroupID As Integer
                Dim SQL As String
                '
                Call appendUpgradeLogAddStep(cpCore.serverconfig.appConfig.name, "VerifyDefaultGroups", "Verify Default Groups")
                '
                GroupID = cpCore.group_add("Content Editors")
                SQL = "Update ccContent Set EditorGroupID=" & cpCore.db.encodeSQLNumber(GroupID) & " where EditorGroupID is null;"
                Call cpCore.db.executeSql(SQL)
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        ''
        ''=========================================================================================
        ''
        ''=========================================================================================
        ''
        'Public Sub ImportCDefFolder()
        '    Try
        '        '
        '        Dim runAtServer As runAtServerClass
        '        Dim FilePath As String
        '        Dim CollectionWorking As CollectionClass
        '        Dim IISResetRequired As Boolean
        '        '
        '        ' Now Create / Modify Db based on all CDef records that are 'CDefChanged'
        '        '
        '        Call UpgradeCDef_BuildDbFromCollection(CollectionWorking, IISResetRequired, cpCore.app.DataBuildVersion_DontUseThis)
        '        '
        '        ' IISReset if needed
        '        '
        '        If IISResetRequired Then
        '            '
        '            ' Restart IIS if stopped
        '            '
        '            runAtServer = New runAtServerClass(cpCore)
        '            Call runAtServer.executeCmd("IISReset", "")
        '        End If
        '        '
        '        ' Clear the CDef folder
        '        '
        '        Call cpCore.app.publicFiles.DeleteFileFolder(FilePath)
        '        Call cpCore.app.publicFiles.createPath(FilePath)
        '    Catch ex As Exception
        '        cpCore.handleException(ex)
        '    End Try
        'End Sub
        ''
        ''
        ''
        'public Sub SetNavigatorEntry(EntryName As String, ParentName As String, AddonID As Integer)
        '    On Error GoTo ErrorTrap
        '    '
        '    Dim CS As Integer
        '    Dim ParentID As Integer
        '    '
        '    If ParentName <> "" Then
        '        CS = cpcore.app.csOpen("Navigator Entries", "name=" & encodeSQLText(ParentName), "ID", , , , , "ID")
        '        If Not cpcore.app.csv_IsCSOK(CS) Then
        '            Call cpcore.app.csv_CloseCS(CS)
        '            CS = cpcore.app.csv_InsertCSRecord("Navigator Entries")
        '        End If
        '        If cpcore.app.csv_IsCSOK(CS) Then
        '            ParentID = cpcore.app.csv_cs_getInteger(CS, "ID")
        '        End If
        '    End If
        '    CS = cpcore.app.csOpen("Navigator Entries", "name=" & encodeSQLText(EntryName), "ID", , , , , "ID,Name,ParentID,AddonID")
        '    If Not cpcore.app.csv_IsCSOK(CS) Then
        '        Call cpcore.app.csv_CloseCS(CS)
        '        CS = cpcore.app.csv_InsertCSRecord("Navigator Entries")
        '    End If
        '    If cpcore.app.csv_IsCSOK(CS) Then
        '        Call cpcore.app.csv_SetCS(CS, "Name", EntryName)
        '        Call cpcore.app.csv_SetCS(CS, "ParentID", ParentID)
        '        Call cpcore.app.csv_SetCS(CS, "AddonID", AddonID)
        '    End If
        '    Call cpcore.app.csv_CloseCS(CS)
        '    '
        '    Exit Sub
        'ErrorTrap:
        '    dim ex as new exception("todo"): Call HandleClassError(ex,cpcore.app.appEnvironment.name,Err.Number, Err.Source, Err.Description, "SetNavigatorEntry", True, False)
        'End Sub
        ''
        ''
        ''
        'public Sub SetNavigatorEntry2(EntryName As String, ParentGuid As String, AddonID As Integer, NavIconTypeID As Integer, NavIconTitle As String, DataBuildVersion As String)
        '    On Error GoTo ErrorTrap
        '    '
        '    Dim CS As Integer
        '    Dim ParentID As Integer
        '    '
        '    If ParentGuid <> "" Then
        '        CS = cpcore.app.csOpen("Navigator Entries", "NavGuid=" & encodeSQLText(ParentGuid), "ID", , , , , "ID")
        '        If cpcore.app.csv_IsCSOK(CS) Then
        '            ParentID = cpcore.app.csv_cs_getInteger(CS, "ID")
        '        End If
        '        Call cpcore.app.csv_CloseCS(CS)
        '    End If
        '    If ParentID > 0 Then
        '        CS = cpcore.app.csOpen("Navigator Entries", "(parentid=" & ParentID & ")and(name=" & encodeSQLText(EntryName) & ")", "ID", , , , , "ID,Name,ParentID,AddonID")
        '        If Not cpcore.app.csv_IsCSOK(CS) Then
        '            Call cpcore.app.csv_CloseCS(CS)
        '            CS = cpcore.app.csv_InsertCSRecord("Navigator Entries")
        '        End If
        '        If cpcore.app.csv_IsCSOK(CS) Then
        '            Call cpcore.app.csv_SetCS(CS, "Name", EntryName)
        '            Call cpcore.app.csv_SetCS(CS, "ParentID", ParentID)
        '            Call cpcore.app.csv_SetCS(CS, "AddonID", AddonID)
        '            If true Then
        '                Call cpcore.app.csv_SetCS(CS, "NavIconTypeID", NavIconTypeID)
        '                Call cpcore.app.csv_SetCS(CS, "NavIconTitle", NavIconTitle)
        '            End If
        '        End If
        '        Call cpcore.app.csv_CloseCS(CS)
        '    End If
        '    '
        '    Exit Sub
        'ErrorTrap:
        '    dim ex as new exception("todo"): Call HandleClassError(ex,cpcore.app.appEnvironment.name,Err.Number, Err.Source, Err.Description, "SetNavigatorEntry", True, False)
        'End Sub
        ''
        ''======================================================================================================
        ''   Installs Addons in the content folder install subfolder
        ''======================================================================================================
        '' REFACTOR - \INSTALL FOLDER IS NOT THE CURRENT PATTERN
        'Public Function InstallAddons(IsNewBuild As Boolean, buildVersion As String) As Boolean
        '    Dim returnOk As Boolean = True
        '    Try
        '        '
        '        Dim IISResetRequired As Boolean
        '        Dim runAtServer As runAtServerClass
        '        Dim addonInstall As New addonInstallClass(cpCore)
        '        Dim saveLogFolder As String
        '        '
        '        InstallAddons = False
        '        '
        '        saveLogFolder = classLogFolder
        '        InstallAddons = addonInstall.InstallCollectionFromPrivateFolder(Me, buildVersion, "Install\", IISResetRequired, cpCore.app.config.name, "", "", IsNewBuild)
        '        classLogFolder = saveLogFolder
        '        '
        '        ' IISReset if needed
        '        '
        '        If IISResetRequired Then
        '            runAtServer = New runAtServerClass(cpCore)
        '            Call runAtServer.executeCmd("IISReset", "")
        '        End If
        '    Catch ex As Exception
        '        cpCore.handleException(ex)
        '    End Try
        '    Return returnOk
        'End Function
        ''
        ''
        ''
        Public Sub ReplaceAddonWithCollection(ByVal AddonProgramID As String, ByVal CollectionGuid As String, ByRef return_IISResetRequired As Boolean, ByRef return_RegisterList As String)
            Dim ex As New Exception("todo") : Call handleClassException(ex, cpCore.serverconfig.appConfig.name, "methodNameFPO") ' ignoreInteger, "dll", "builderClass.ReplaceAddonWithCollection is deprecated", "ReplaceAddonWithCollection", True, True)
        End Sub
        '    On Error GoTo ErrorTrap
        '    '
        '    Dim CS As Integer
        '    Dim ErrorMessage As String
        '    Dim addonInstall As addonInstallClass
        '    '
        '    CS = cpcore.app.csOpen("Add-ons", "objectProgramID=" & encodeSQLText(AddonProgramID))
        '    If cpcore.app.csv_IsCSOK(CS) Then
        '        Call cpcore.app.csv_DeleteCSRecord(CS)
        '        InstallCollectionList = InstallCollectionList & "," & CollectionGuid
        '        'Set AddonInstall = New AddonInstallClass
        '        'If Not AddonInstall.UpgradeAllAppsFromLibCollection2(CollectionGuid, cpcore.app.appEnvironment.name, Return_IISResetRequired, Return_RegisterList, ErrorMessage) Then
        '        'End If
        '    End If
        '    Call cpcore.app.csv_CloseCS(CS)
        '    '
        '    Exit Sub
        '    '
        '    ' ----- Error Trap
        '    '
        'ErrorTrap:
        '    dim ex as new exception("todo"): Call HandleClassError(ex,cpcore.app.appEnvironment.name, Err.Number, Err.Source, Err.Description, "ReplaceAddonWithCollection", True, True)
        '    Err.Clear
        'End Sub
        '
        '===================================================================================================================
        '   Verify all the core tables
        '       Moved to Sub because if an Addon upgrade from another site on the server distributes the
        '       CDef changes, this site could have to update it's ccContent and ccField records, and
        '       it will fail if they are not up to date.
        '===================================================================================================================
        '
        Private Sub VerifyCoreTables()
            Try
                '
                If Not False Then
                    Call appendUpgradeLogAddStep(cpCore.serverconfig.appConfig.name, "VerifyCoreTables", "Verify Core SQL Tables")
                    '
                    Call cpCore.db.createSQLTable("Default", "ccDataSources")
                    Call cpCore.db.createSQLTableField("Default", "ccDataSources", "typeId", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccDataSources", "address", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccDataSources", "username", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccDataSources", "password", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccDataSources", "ConnString", FieldTypeIdText)
                    '
                    Call cpCore.db.createSQLTable("Default", "ccTables")
                    Call cpCore.db.createSQLTableField("Default", "ccTables", "DataSourceID", FieldTypeIdLookup)
                    '
                    Call cpCore.db.createSQLTable("Default", "ccContent")
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "ContentTableID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "AuthoringTableID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "AllowAdd", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "AllowDelete", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "AllowWorkflowAuthoring", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "DeveloperOnly", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "AdminOnly", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "ParentID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "DefaultSortMethodID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "DropDownFieldList", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "EditorGroupID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "AllowCalendarEvents", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "AllowContentTracking", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "AllowTopicRules", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "AllowContentChildTool", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "AllowMetaContent", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "IconLink", FieldTypeIdLink)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "IconHeight", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "IconWidth", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "IconSprites", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "installedByCollectionId", FieldTypeIdInteger)
                    'Call cpCore.app.csv_CreateSQLTableField("Default", "ccContent", "ccGuid", FieldTypeText)
                    Call cpCore.db.createSQLTableField("Default", "ccContent", "IsBaseContent", FieldTypeIdBoolean)
                    'Call cpcore.app.csv_CreateSQLTableField("Default", "ccContent", "WhereClause", FieldTypeText)
                    '
                    Call cpCore.db.createSQLTable("Default", "ccFields")
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "ContentID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "Type", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "Caption", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "ReadOnly", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "NotEditable", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "LookupContentID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "RedirectContentID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "RedirectPath", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "RedirectID", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "HelpMessage", FieldTypeIdLongText) ' deprecated but Im chicken to remove this
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "UniqueName", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "TextBuffered", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "Password", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "IndexColumn", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "IndexWidth", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "IndexSortPriority", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "IndexSortDirection", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "EditSortPriority", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "AdminOnly", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "DeveloperOnly", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "DefaultValue", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "Required", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "HTMLContent", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "Authorable", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "ManyToManyContentID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "ManyToManyRuleContentID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "ManyToManyRulePrimaryField", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "ManyToManyRuleSecondaryField", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "RSSTitleField", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "RSSDescriptionField", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "MemberSelectGroupID", FieldTypeIdInteger)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "EditTab", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "Scramble", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "LookupList", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "IsBaseField", FieldTypeIdBoolean)
                    Call cpCore.db.createSQLTableField("Default", "ccFields", "installedByCollectionId", FieldTypeIdInteger)
                    '
                    Call cpCore.db.createSQLTable("Default", "ccFieldHelp")
                    Call cpCore.db.createSQLTableField("Default", "ccFieldHelp", "FieldID", FieldTypeIdLookup)
                    Call cpCore.db.createSQLTableField("Default", "ccFieldHelp", "HelpDefault", FieldTypeIdLongText)
                    Call cpCore.db.createSQLTableField("Default", "ccFieldHelp", "HelpCustom", FieldTypeIdLongText)
                    '
                    Call cpCore.db.createSQLTable("Default", "ccSetup")
                    Call cpCore.db.createSQLTableField("Default", "ccSetup", "FieldValue", FieldTypeIdText)
                    Call cpCore.db.createSQLTableField("Default", "ccSetup", "DeveloperOnly", FieldTypeIdBoolean)
                    '
                    Call cpCore.db.createSQLTable("Default", "ccSortMethods")
                    Call cpCore.db.createSQLTableField("Default", "ccSortMethods", "OrderByClause", FieldTypeIdText)
                    '
                    Call cpCore.db.createSQLTable("Default", "ccFieldTypes")
                    '
                    Call cpCore.db.createSQLTable("Default", "ccContentCategories")
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub

        '
        '===========================================================================
        '   Error handler
        '===========================================================================
        '
        Private Sub handleClassException(ByVal ex As Exception, ByVal ApplicationName As String, ByVal MethodName As String)
            cpCore.log_appendLog("exception in builderClass." & MethodName & ", application [" & ApplicationName & "], ex [" & ex.ToString & "]")
        End Sub
        '
        '===========================================================================
        '   Append Log File
        '===========================================================================
        '
        Private Sub appendUpgradeLog(ByVal appName As String, ByVal Method As String, ByVal Message As String)
            appendUpgradeLog("app [" & appName & "], Method [" & Method & "], Message [" & Message & "]")
        End Sub
        '
        '=============================================================================
        '   Get a ContentID from the ContentName using just the tables
        '=============================================================================
        '
        Private Sub appendUpgradeLogAddStep(ByVal appName As String, ByVal Method As String, ByVal Message As String)
            StepCount = StepCount + 1
            Call appendUpgradeLog(appName, Method, "Step " & StepCount & " : " & Message)
        End Sub
        '
        '=====================================================================================================
        '   a value in a name/value pair
        '=====================================================================================================
        '
        Public Sub SetNameValueArrays(ByVal InputName As String, ByVal InputValue As String, ByRef SQLName() As String, ByRef SQLValue() As String, ByRef Index As Integer)
            ' ##### removed to catch err<>0 problem on error resume next
            '
            SQLName(Index) = InputName
            SQLValue(Index) = InputValue
            Index = Index + 1
            '
        End Sub
        '
        Private Sub appendUpgradeLog(ByVal message As String)
            Console.WriteLine("upgrade: " & message)
            cpCore.log_appendLog(message, "Upgrade")
        End Sub
        ''
        ''=============================================================================
        ''
        ''=============================================================================
        ''
        'Public Sub csv_VerifyAggregateFunction(ByVal Name As String, ByVal Link As String, ByVal ObjectProgramID As String, ByVal ArgumentList As String, ByVal SortOrder As String)
        '    Try
        '        '
        '        ' Determine Function or Object based on Link
        '        '
        '        If Link <> "" Then
        '            Call csv_VerifyAggregateScript(Name, Link, ArgumentList, SortOrder)
        '        ElseIf ObjectProgramID <> "" Then
        '            Call csv_VerifyAggregateObject(Name, ObjectProgramID, ArgumentList, SortOrder)
        '        Else
        '            Call csv_VerifyAggregateReplacement2(Name, "", ArgumentList, SortOrder)
        '        End If
        '    Catch ex As Exception
        '        cpCore.handleException(ex)
        '    End Try
        'End Sub '

        '
        '=============================================================================
        '   Verify an Admin Menu Entry
        '       Entries are unique by their name
        '=============================================================================
        '
        Public Sub admin_VerifyMenuEntry(ByVal ParentName As String, ByVal EntryName As String, ByVal ContentName As String, ByVal LinkPage As String, ByVal SortOrder As String, ByVal AdminOnly As Boolean, ByVal DeveloperOnly As Boolean, ByVal NewWindow As Boolean, ByVal Active As Boolean, ByVal MenuContentName As String, ByVal AddonName As String)
            Try
                '
                Const AddonContentName = "Aggregate Functions"
                '
                Dim SelectList As String
                Dim ErrorDescription As String
                Dim CSEntry As Integer
                Dim ContentID As Integer
                Dim ParentID As Integer
                Dim MethodName As String
                Dim addonId As Integer
                Dim CS As Integer
                Dim SupportAddonID As Boolean
                '
                MethodName = "csv_VerifyMenuEntry"
                '
                SelectList = "Name,ContentID,ParentID,LinkPage,SortOrder,AdminOnly,DeveloperOnly,NewWindow,Active"
                SupportAddonID = cpCore.metaData.isContentFieldSupported(MenuContentName, "AddonID")
                '
                ' Get AddonID from AddonName
                '
                addonId = 0
                If Not SupportAddonID Then
                    SelectList = SelectList & ",0 as AddonID"
                Else
                    SelectList = SelectList & ",AddonID"
                    If AddonName <> "" Then
                        CS = cpCore.db.cs_open(AddonContentName, "name=" & cpCore.db.encodeSQLText(AddonName), "ID", False, , , , "ID", 1)
                        If cpCore.db.cs_ok(CS) Then
                            addonId = (cpCore.db.cs_getInteger(CS, "ID"))
                        End If
                        Call cpCore.db.cs_Close(CS)
                    End If
                End If
                ''
                'If vbLCase(EntryName) = "property search log" Then
                '    EntryName = EntryName
                'End If
                '
                ' Get ParentID from ParentName
                '
                ParentID = 0
                If ParentName <> "" Then
                    CS = cpCore.db.cs_open(MenuContentName, "name=" & cpCore.db.encodeSQLText(ParentName), "ID", False, , , , "ID", 1)
                    If cpCore.db.cs_ok(CS) Then
                        ParentID = (cpCore.db.cs_getInteger(CS, "ID"))
                    End If
                    Call cpCore.db.cs_Close(CS)
                End If
                '
                ' Set ContentID from ContentName
                '
                ContentID = -1
                If ContentName <> "" Then
                    ContentID = cpCore.db.getContentId(ContentName)
                End If
                '
                ' Locate current entry
                '
                CSEntry = cpCore.db.cs_open(MenuContentName, "(name=" & cpCore.db.encodeSQLText(EntryName) & ")", "ID", False, , , , SelectList)
                '
                ' If no current entry, create one
                '
                If Not cpCore.db.cs_ok(CSEntry) Then
                    cpCore.db.cs_Close(CSEntry)
                    CSEntry = cpCore.db.cs_insertRecord(MenuContentName, SystemMemberID)
                    If cpCore.db.cs_ok(CSEntry) Then
                        Call cpCore.db.cs_setField(CSEntry, "name", EntryName)
                    End If
                End If
                If cpCore.db.cs_ok(CSEntry) Then
                    If ParentID = 0 Then
                        Call cpCore.db.cs_setField(CSEntry, "ParentID", Nothing)
                    Else
                        Call cpCore.db.cs_setField(CSEntry, "ParentID", ParentID)
                    End If
                    If (ContentID = -1) Then
                        Call cpCore.db.cs_setField(CSEntry, "ContentID", Nothing)
                    Else
                        Call cpCore.db.cs_setField(CSEntry, "ContentID", ContentID)
                    End If
                    Call cpCore.db.cs_setField(CSEntry, "LinkPage", LinkPage)
                    Call cpCore.db.cs_setField(CSEntry, "SortOrder", SortOrder)
                    Call cpCore.db.cs_setField(CSEntry, "AdminOnly", AdminOnly)
                    Call cpCore.db.cs_setField(CSEntry, "DeveloperOnly", DeveloperOnly)
                    Call cpCore.db.cs_setField(CSEntry, "NewWindow", NewWindow)
                    Call cpCore.db.cs_setField(CSEntry, "Active", Active)
                    If SupportAddonID Then
                        Call cpCore.db.cs_setField(CSEntry, "AddonID", addonId)
                    End If
                End If
                Call cpCore.db.cs_Close(CSEntry)
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
    End Class
End Namespace
