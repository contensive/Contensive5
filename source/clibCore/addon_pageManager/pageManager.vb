﻿
Option Explicit On
Option Strict On

Imports Contensive.BaseClasses
Imports Contensive.Core
Imports Contensive.Core.Models.Entity
Imports Contensive.Core.Controllers
Imports Contensive.Core.Controllers.genericController
'
Namespace Contensive.Addons.PageManager
    '
    Public Class pageManagerClass
        Inherits Contensive.BaseClasses.AddonBaseClass
        '
        '====================================================================================================
        ''' <summary>
        ''' pageManager addon interface
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <returns></returns>
        Public Overrides Function execute(cp As Contensive.BaseClasses.CPBaseClass) As Object
            Dim returnHtml As String = ""
            Try
                Dim processor As CPClass = DirectCast(cp, CPClass)
                Dim cpCore As coreClass = processor.core
                returnHtml = getDoc(cpCore)
            Catch ex As Exception
                cp.Site.ErrorReport(ex)
            End Try
            Return returnHtml
        End Function
        '
        '========================================================================
        '   Returns the HTML body
        '
        '   This code is based on the GoMethod site script
        '========================================================================
        '
        Public Function getBody(cpCore As coreClass) As String
            Dim returnBody As String = ""
            Try
                '
                Dim AddonReturn As String
                Dim Ptr As Integer
                Dim Cnt As Integer
                Dim layoutError As String
                Dim FilterStatusOK As Boolean
                Dim BlockFormatting As Boolean
                Dim IndentCnt As Integer
                Dim Result As New stringBuilderLegacyController
                Dim Content As String
                Dim ContentIndent As String
                Dim ContentCnt As Integer
                Dim PageContent As String
                Dim Stream As New stringBuilderLegacyController
                Dim LocalTemplateID As Integer
                Dim LocalTemplateName As String
                Dim LocalTemplateBody As String
                Dim Parse As htmlParserController
                Dim blockSiteWithLogin As Boolean
                Dim addonCachePtr As Integer
                Dim addonId As Integer
                Dim AddonName As String
                '
                Call cpCore.addonLegacyCache.load()
                returnBody = ""
                '
                ' ----- OnBodyStart add-ons
                '
                FilterStatusOK = False
                Cnt = UBound(cpCore.addonLegacyCache.addonCache.onBodyStartPtrs) + 1
                For Ptr = 0 To Cnt - 1
                    addonCachePtr = cpCore.addonLegacyCache.addonCache.onBodyStartPtrs(Ptr)
                    If addonCachePtr > -1 Then
                        addonId = cpCore.addonLegacyCache.addonCache.addonList(addonCachePtr.ToString).id
                        'hint = hint & ",addonId=" & addonId
                        If addonId > 0 Then
                            AddonName = cpCore.addonLegacyCache.addonCache.addonList(addonCachePtr.ToString).name
                            'hint = hint & ",AddonName=" & AddonName
                            returnBody = returnBody & cpCore.addon.execute_legacy2(addonId, "", "", CPUtilsBaseClass.addonContext.ContextOnBodyStart, "", 0, "", "", False, 0, "", FilterStatusOK, Nothing)
                            If Not FilterStatusOK Then
                                Throw New ApplicationException("Unexpected exception") ' Call cpcore.handleLegacyError12("pageManager_GetHtmlBody", "There was an error processing OnAfterBody [" & cpcore.addonCache.addonCache.addonList(addonCachePtr.ToString).name & "]. Filtering was aborted.")
                                Exit For
                            End If
                        End If
                    End If
                Next
                '
                ' ----- main_Get Content (Already Encoded)
                '
                blockSiteWithLogin = False
                PageContent = getBodyContent(cpCore, True, True, False, blockSiteWithLogin)
                If blockSiteWithLogin Then
                    '
                    ' section blocked, just return the login box in the page content
                    '
                    returnBody = "" _
                        & cr & "<div class=""ccLoginPageCon"">" _
                        & genericController.kmaIndent(PageContent) _
                        & cr & "</div>" _
                        & ""
                ElseIf Not cpCore.continueProcessing Then
                    '
                    ' exit if stream closed during main_GetSectionpage
                    '
                    returnBody = ""
                Else
                    '
                    ' -- no section block, continue
                    LocalTemplateID = cpCore.pages.template.ID
                    LocalTemplateBody = cpCore.pages.template.BodyHTML
                    If LocalTemplateBody = "" Then
                        LocalTemplateBody = TemplateDefaultBody
                    End If
                    LocalTemplateName = cpCore.pages.template.Name
                    If LocalTemplateName = "" Then
                        LocalTemplateName = "Template " & LocalTemplateID
                    End If
                    '
                    ' ----- Encode Template
                    '
                    If Not cpCore.htmlDoc.pageManager_printVersion Then
                        LocalTemplateBody = cpCore.htmlDoc.html_executeContentCommands(Nothing, LocalTemplateBody, CPUtilsBaseClass.addonContext.ContextTemplate, cpCore.authContext.user.id, cpCore.authContext.isAuthenticated, layoutError)
                        returnBody = returnBody & cpCore.htmlDoc.html_encodeContent9(LocalTemplateBody, cpCore.authContext.user.id, "Page Templates", LocalTemplateID, 0, False, False, True, True, False, True, "", cpCore.webServer.webServerIO_requestProtocol & cpCore.webServer.requestDomain, False, cpCore.siteProperties.defaultWrapperID, PageContent, CPUtilsBaseClass.addonContext.ContextTemplate)
                        'returnHtmlBody = returnHtmlBody & EncodeContent8(LocalTemplateBody, memberID, "Page Templates", LocalTemplateID, 0, False, False, True, True, False, True, "", main_ServerProtocol, False, app.SiteProperty_DefaultWrapperID, PageContent, ContextTemplate)
                    End If
                    '
                    ' If Content was not found, add it to the end
                    '
                    If (InStr(1, returnBody, fpoContentBox) <> 0) Then
                        returnBody = genericController.vbReplace(returnBody, fpoContentBox, PageContent)
                    Else
                        returnBody = returnBody & PageContent
                    End If
                    '
                    ' ----- Add tools Panel
                    '
                    If Not cpCore.authContext.isAuthenticated() Then
                        '
                        ' not logged in
                        '
                    Else
                        '
                        ' Add template editing
                        '
                        If cpCore.visitProperty.getBoolean("AllowAdvancedEditor") And cpCore.authContext.isEditing(cpCore, "Page Templates") Then
                            returnBody = cpCore.htmlDoc.main_GetEditWrapper("Page Template [" & LocalTemplateName & "]", cpCore.htmlDoc.main_GetRecordEditLink2("Page Templates", LocalTemplateID, False, LocalTemplateName, cpCore.authContext.isEditing(cpCore, "Page Templates")) & returnBody)
                        End If
                    End If
                    '
                    ' ----- OnBodyEnd add-ons
                    '
                    'hint = hint & ",onBodyEnd"
                    FilterStatusOK = False
                    Cnt = UBound(cpCore.addonLegacyCache.addonCache.onBodyEndPtrs) + 1
                    'hint = hint & ",cnt=" & Cnt
                    For Ptr = 0 To Cnt - 1
                        addonCachePtr = cpCore.addonLegacyCache.addonCache.onBodyEndPtrs(Ptr)
                        'hint = hint & ",ptr=" & Ptr & ",addonCachePtr=" & addonCachePtr
                        If addonCachePtr > -1 Then
                            addonId = cpCore.addonLegacyCache.addonCache.addonList(addonCachePtr.ToString).id
                            'hint = hint & ",addonId=" & addonId
                            If addonId > 0 Then
                                AddonName = cpCore.addonLegacyCache.addonCache.addonList(addonCachePtr.ToString).name
                                'hint = hint & ",AddonName=" & AddonName
                                cpCore.htmlDoc.html_DocBodyFilter = returnBody
                                AddonReturn = cpCore.addon.execute_legacy2(addonId, "", "", CPUtilsBaseClass.addonContext.ContextFilter, "", 0, "", "", False, 0, "", FilterStatusOK, Nothing)
                                returnBody = cpCore.htmlDoc.html_DocBodyFilter & AddonReturn
                                If Not FilterStatusOK Then
                                    Throw New ApplicationException("Unexpected exception") ' Call cpcore.handleLegacyError12("pageManager_GetHtmlBody", "There was an error processing OnBodyEnd for [" & AddonName & "]. Filtering was aborted.")
                                    Exit For
                                End If
                            End If
                        End If
                    Next
                    '
                    ' Make it pretty for those who care
                    '
                    returnBody = htmlReflowController.reflow(cpCore, returnBody)
                End If
                '
            Catch ex As Exception
                cpCore.handleExceptionAndContinue(ex) : Throw
            End Try
            Return returnBody
        End Function
        '
        '========================================================================
        '   Returns the entire HTML page based on the bid/sid stream values
        '
        '   This code is based on the GoMethod site script
        '========================================================================
        '
        Public Function getDoc(cpCore As coreClass) As String
            Dim returnHtml As String = ""
            Try
                Dim downloadId As Integer
                Dim Pos As Integer
                Dim htmlBody As String
                Dim htmlHead As String
                Dim bodyTag As String
                Dim bodyAddonId As Integer
                Dim bodyAddonStatusOK As Boolean
                Dim Clip As String
                Dim ClipParentRecordID As Integer
                Dim ClipParentContentID As Integer
                Dim ClipParentContentName As String
                Dim ClipChildContentID As Integer
                Dim ClipChildContentName As String
                Dim ClipChildRecordID As Integer
                Dim ClipChildRecordName As String
                Dim CSClip As Integer
                Dim ClipBoardArray() As String
                Dim ClipBoard As String
                Dim ClipParentFieldList As String
                Dim Fields As String()
                Dim FieldCount As Integer
                Dim NameValues() As String
                Dim RedirectLink As String = ""
                Dim RedirectReason As String = ""
                Dim PageNotFoundReason As String = ""
                Dim PageNotFoundSource As String = ""
                Dim IsPageNotFound As Boolean = False
                '
                If cpCore.continueProcessing Then
                    cpCore.htmlDoc.main_AdminWarning = cpCore.docProperties.getText("main_AdminWarningMsg")
                    cpCore.htmlDoc.main_AdminWarningPageID = cpCore.docProperties.getInteger("main_AdminWarningPageID")
                    '
                    ' todo move cookie test to htmlDoc controller
                    ' -- Add cookie test
                    Dim AllowCookieTest As Boolean
                    AllowCookieTest = cpCore.siteProperties.allowVisitTracking And (cpCore.authContext.visit.PageVisits = 1)
                    If AllowCookieTest Then
                        Call cpCore.htmlDoc.main_AddOnLoadJavascript2("if (document.cookie && document.cookie != null){cj.ajax.qs('f92vo2a8d=" & cpCore.security.encodeToken(cpCore.authContext.visit.id, cpCore.app_startTime) & "')};", "Cookie Test")
                    End If
                    '
                    '--------------------------------------------------------------------------
                    '   User form processing
                    '       if a form is created in the editor, process it by emailing and saving to the User Form Response content
                    '--------------------------------------------------------------------------
                    '
                    If cpCore.docProperties.getInteger("ContensiveUserForm") = 1 Then
                        Dim FromAddress As String = cpCore.siteProperties.getText("EmailFromAddress", "info@" & cpCore.webServer.webServerIO_requestDomain)
                        Call cpCore.email.sendForm(cpCore.siteProperties.emailAdmin, FromAddress, "Form Submitted on " & cpCore.webServer.webServerIO_requestReferer)
                        Dim cs As Integer = cpCore.db.cs_insertRecord("User Form Response")
                        If cpCore.db.cs_ok(cs) Then
                            Call cpCore.db.cs_set(cs, "name", "Form " & cpCore.webServer.requestReferrer)
                            Dim Copy As String = ""

                            For Each key As String In cpCore.docProperties.getKeyList()
                                Dim docProperty As docPropertiesClass = cpCore.docProperties.getProperty(key)
                                If (key.ToLower() <> "contensiveuserform") Then
                                    Copy &= docProperty.Name & "=" & docProperty.Value & vbCrLf
                                End If
                            Next
                            Call cpCore.db.cs_set(cs, "copy", Copy)
                            Call cpCore.db.cs_set(cs, "VisitId", cpCore.authContext.visit.id)
                        End If
                        Call cpCore.db.cs_Close(cs)
                    End If
                    '
                    '--------------------------------------------------------------------------
                    '   Contensive Form Page Processing
                    '--------------------------------------------------------------------------
                    '
                    If cpCore.docProperties.getInteger("ContensiveFormPageID") <> 0 Then
                        Call processForm(cpCore, cpCore.docProperties.getInteger("ContensiveFormPageID"))
                    End If
                    '
                    '--------------------------------------------------------------------------
                    ' ----- Automatic Redirect to a full URL
                    '   If the link field of the record is an absolution address
                    '       rc = redirect contentID
                    '       ri = redirect content recordid
                    '--------------------------------------------------------------------------
                    '
                    cpCore.htmlDoc.pageManager_RedirectContentID = (cpCore.docProperties.getInteger(rnRedirectContentId))
                    If (cpCore.htmlDoc.pageManager_RedirectContentID <> 0) Then
                        cpCore.htmlDoc.pageManager_RedirectRecordID = (cpCore.docProperties.getInteger(rnRedirectRecordId))
                        If (cpCore.htmlDoc.pageManager_RedirectRecordID <> 0) Then
                            Dim contentName As String = cpCore.metaData.getContentNameByID(cpCore.htmlDoc.pageManager_RedirectContentID)
                            If contentName <> "" Then
                                If iisController.main_RedirectByRecord_ReturnStatus(cpCore, contentName, cpCore.htmlDoc.pageManager_RedirectRecordID) Then
                                    '
                                    'Call AppendLog("main_init(), 3210 - exit for rc/ri redirect ")
                                    '
                                    cpCore.continueProcessing = False '--- should be disposed by caller --- Call dispose
                                    Return cpCore.htmlDoc.docBuffer
                                Else
                                    cpCore.htmlDoc.main_AdminWarning = "<p>The site attempted to automatically jump to another page, but there was a problem with the page that included the link.<p>"
                                    cpCore.htmlDoc.main_AdminWarningPageID = cpCore.htmlDoc.pageManager_RedirectRecordID
                                End If
                            End If
                        End If
                    End If
                    '
                    '--------------------------------------------------------------------------
                    ' ----- Active Download hook
                    Dim RecordEID As String = cpCore.docProperties.getText(RequestNameLibraryFileID)
                    If (RecordEID <> "") Then
                        Dim tokenDate As Date
                        Call cpCore.security.decodeToken(RecordEID, downloadId, tokenDate)
                        If downloadId <> 0 Then
                            '
                            ' -- lookup record and set clicks
                            Dim file As Models.Entity.libraryFilesModel = Models.Entity.libraryFilesModel.create(cpCore, downloadId)
                            If (file IsNot Nothing) Then
                                file.Clicks += 1
                                file.save(cpCore)
                                If file.Filename <> "" Then
                                    '
                                    ' -- create log entry
                                    Dim log As Models.Entity.libraryFileLogModel = Models.Entity.libraryFileLogModel.add(cpCore)
                                    If (log IsNot Nothing) Then
                                        log.FileID = file.id
                                        log.VisitID = cpCore.authContext.visit.id
                                        log.MemberID = cpCore.authContext.user.id
                                    End If
                                    '
                                    ' -- and go
                                    Dim link As String = cpCore.webServer.webServerIO_requestProtocol & cpCore.webServer.requestDomain & genericController.getCdnFileLink(cpCore, link)
                                    Call cpCore.webServer.redirect(link, "Redirecting because the active download request variable is set to a valid Library Files record. Library File Log has been appended.", False)
                                End If
                            End If
                            '
                        End If
                    End If
                    '
                    '--------------------------------------------------------------------------
                    '   Process clipboard cut/paste
                    '--------------------------------------------------------------------------
                    '
                    Clip = cpCore.docProperties.getText(RequestNameCut)
                    If (Clip <> "") Then
                        '
                        ' if a cut, load the clipboard
                        '
                        Call cpCore.visitProperty.setProperty("Clipboard", Clip)
                        Call genericController.modifyLinkQuery(cpCore.htmlDoc.refreshQueryString, RequestNameCut, "")
                    End If
                    ClipParentContentID = cpCore.docProperties.getInteger(RequestNamePasteParentContentID)
                    ClipParentRecordID = cpCore.docProperties.getInteger(RequestNamePasteParentRecordID)
                    ClipParentFieldList = cpCore.docProperties.getText(RequestNamePasteFieldList)
                    If (ClipParentContentID <> 0) And (ClipParentRecordID <> 0) Then
                        '
                        ' Request for a paste, clear the cliboard
                        '
                        ClipBoard = cpCore.visitProperty.getText("Clipboard", "")
                        Call cpCore.visitProperty.setProperty("Clipboard", "")
                        Call genericController.ModifyQueryString(cpCore.htmlDoc.refreshQueryString, RequestNamePasteParentContentID, "")
                        Call genericController.ModifyQueryString(cpCore.htmlDoc.refreshQueryString, RequestNamePasteParentRecordID, "")
                        ClipParentContentName = cpCore.metaData.getContentNameByID(ClipParentContentID)
                        If (ClipParentContentName = "") Then
                            ' state not working...
                        ElseIf (ClipBoard = "") Then
                            ' state not working...
                        Else
                            If Not cpCore.authContext.isAuthenticatedContentManager(cpCore, ClipParentContentName) Then
                                Call errorController.error_AddUserError(cpCore, "The paste operation failed because you are not a content manager of the Clip Parent")
                            Else
                                '
                                ' Current identity is a content manager for this content
                                '
                                Dim Position As Integer = genericController.vbInstr(1, ClipBoard, ".")
                                If Position = 0 Then
                                    Call errorController.error_AddUserError(cpCore, "The paste operation failed because the clipboard data is configured incorrectly.")
                                Else
                                    ClipBoardArray = Split(ClipBoard, ".")
                                    If UBound(ClipBoardArray) = 0 Then
                                        Call errorController.error_AddUserError(cpCore, "The paste operation failed because the clipboard data is configured incorrectly.")
                                    Else
                                        ClipChildContentID = genericController.EncodeInteger(ClipBoardArray(0))
                                        ClipChildRecordID = genericController.EncodeInteger(ClipBoardArray(1))
                                        If Not cpCore.metaData.isWithinContent(ClipChildContentID, ClipParentContentID) Then
                                            Call errorController.error_AddUserError(cpCore, "The paste operation failed because the destination location is not compatible with the clipboard data.")
                                        Else
                                            '
                                            ' the content definition relationship is OK between the child and parent record
                                            '
                                            ClipChildContentName = cpCore.metaData.getContentNameByID(ClipChildContentID)
                                            If Not ClipChildContentName <> "" Then
                                                Call errorController.error_AddUserError(cpCore, "The paste operation failed because the clipboard data content is undefined.")
                                            Else
                                                If (ClipParentRecordID = 0) Then
                                                    Call errorController.error_AddUserError(cpCore, "The paste operation failed because the clipboard data record is undefined.")
                                                ElseIf cpCore.pages.main_IsChildRecord(ClipChildContentName, ClipParentRecordID, ClipChildRecordID) Then
                                                    Call errorController.error_AddUserError(cpCore, "The paste operation failed because the destination location is a child of the clipboard data record.")
                                                Else
                                                    '
                                                    ' the parent record is not a child of the child record (circular check)
                                                    '
                                                    ClipChildRecordName = "record " & ClipChildRecordID
                                                    CSClip = cpCore.db.cs_open2(ClipChildContentName, ClipChildRecordID, True, True)
                                                    If Not cpCore.db.cs_ok(CSClip) Then
                                                        Call errorController.error_AddUserError(cpCore, "The paste operation failed because the data record referenced by the clipboard could not found.")
                                                    Else
                                                        '
                                                        ' Paste the edit record record
                                                        '
                                                        ClipChildRecordName = cpCore.db.cs_getText(CSClip, "name")
                                                        If ClipParentFieldList = "" Then
                                                            '
                                                            ' Legacy paste - go right to the parent id
                                                            '
                                                            If Not cpCore.db.cs_isFieldSupported(CSClip, "ParentID") Then
                                                                Call errorController.error_AddUserError(cpCore, "The paste operation failed because the record you are pasting does not   support the necessary parenting feature.")
                                                            Else
                                                                Call cpCore.db.cs_set(CSClip, "ParentID", ClipParentRecordID)
                                                            End If
                                                        Else
                                                            '
                                                            ' Fill in the Field List name values
                                                            '
                                                            Fields = Split(ClipParentFieldList, ",")
                                                            FieldCount = UBound(Fields) + 1
                                                            For FieldPointer = 0 To FieldCount - 1
                                                                Dim Pair As String
                                                                Pair = Fields(FieldPointer)
                                                                If Mid(Pair, 1, 1) = "(" And Mid(Pair, Len(Pair), 1) = ")" Then
                                                                    Pair = Mid(Pair, 2, Len(Pair) - 2)
                                                                End If
                                                                NameValues = Split(Pair, "=")
                                                                If UBound(NameValues) = 0 Then
                                                                    Call errorController.error_AddUserError(cpCore, "The paste operation failed because the clipboard data Field List is not configured correctly.")
                                                                Else
                                                                    If Not cpCore.db.cs_isFieldSupported(CSClip, CStr(NameValues(0))) Then
                                                                        Call errorController.error_AddUserError(cpCore, "The paste operation failed because the clipboard data Field [" & CStr(NameValues(0)) & "] is not supported by the location data.")
                                                                    Else
                                                                        Call cpCore.db.cs_set(CSClip, CStr(NameValues(0)), CStr(NameValues(1)))
                                                                    End If
                                                                End If
                                                            Next
                                                        End If
                                                        ''
                                                        '' Fixup Content Watch
                                                        ''
                                                        'ShortLink = main_ServerPathPage
                                                        'ShortLink = ConvertLinkToShortLink(ShortLink, main_ServerHost, main_ServerVirtualPath)
                                                        'ShortLink = genericController.modifyLinkQuery(ShortLink, "bid", CStr(ClipChildRecordID), True)
                                                        'Call main_TrackContentSet(CSClip, ShortLink)
                                                    End If
                                                    Call cpCore.db.cs_Close(CSClip)
                                                    '
                                                    ' Set Child Pages Found and clear caches
                                                    '
                                                    CSClip = cpCore.db.csOpenRecord(ClipParentContentName, ClipParentRecordID, , , "ChildPagesFound")
                                                    If cpCore.db.cs_ok(CSClip) Then
                                                        Call cpCore.db.cs_set(CSClip, "ChildPagesFound", True.ToString)
                                                    End If
                                                    Call cpCore.db.cs_Close(CSClip)
                                                    'Call cpCore.pages.cache_pageContent_clear()
                                                    If (cpCore.siteProperties.allowWorkflowAuthoring And cpCore.workflow.isWorkflowAuthoringCompatible(ClipChildContentName)) Then
                                                        '
                                                        ' Workflow editing
                                                        '
                                                    Else
                                                        '
                                                        ' Live Editing
                                                        '
                                                        Call cpCore.cache.invalidateContent(ClipChildContentName)
                                                        Call cpCore.cache.invalidateContent(ClipParentContentName)
                                                        'Call cpCore.pages.cache_pageContent_clear()
                                                    End If
                                                End If
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If
                    Clip = cpCore.docProperties.getText(RequestNameCutClear)
                    If (Clip <> "") Then
                        '
                        ' if a cut clear, clear the clipboard
                        '
                        Call cpCore.visitProperty.setProperty("Clipboard", "")
                        Clip = cpCore.visitProperty.getText("Clipboard", "")
                        Call genericController.modifyLinkQuery(cpCore.htmlDoc.refreshQueryString, RequestNameCutClear, "")
                    End If
                    '
                    ' link alias and link forward
                    '
                    'Dim Custom404SourcePathPage As String = main_ServerPathPage ' refactor all of this
                    'Dim Custom404SourceNoQueryString As String = main_ServerPathPage
                    ' Dim Custom404SourceQueryString As String = main_ServerQueryString
                    If True Then
                        If True Then
                            Dim LinkAliasCriteria As String = ""
                            Dim linkAliasTest1 As String = ""
                            Dim linkAliasTest2 As String
                            Dim LinkNoProtocol As String = ""
                            Dim linkDomain As String = ""
                            Dim LinkFullPath As String = ""
                            Dim LinkFullPathNoSlash As String = ""
                            Dim isLinkForward As Boolean = False
                            Dim LinkForwardCriteria As String = ""
                            Dim Sql As String = ""
                            Dim CSPointer As Integer = -1
                            Dim IsInLinkForwardTable As Boolean = False
                            Dim Viewings As Integer = 0
                            Dim LinkSplit As String()
                            Dim IsLinkAlias As Boolean = False
                            '
                            '--------------------------------------------------------------------------
                            ' try link alias
                            '--------------------------------------------------------------------------
                            '
                            LinkAliasCriteria = ""
                            linkAliasTest1 = cpCore.webServer.requestPathPage
                            If (linkAliasTest1.Substring(0, 1) = "/") Then
                                linkAliasTest1 = linkAliasTest1.Substring(1)
                            End If
                            If linkAliasTest1.Length > 0 Then
                                If (linkAliasTest1.Substring(linkAliasTest1.Length - 1, 1) = "/") Then
                                    linkAliasTest1 = linkAliasTest1.Substring(0, linkAliasTest1.Length - 1)
                                End If
                            End If

                            linkAliasTest2 = linkAliasTest1 & "/"
                            If (Not IsPageNotFound) And (cpCore.webServer.requestPathPage <> "") Then
                                '
                                ' build link variations needed later
                                '
                                '
                                Pos = genericController.vbInstr(1, cpCore.webServer.requestPathPage, "://", vbTextCompare)
                                If Pos <> 0 Then
                                    LinkNoProtocol = Mid(cpCore.webServer.requestPathPage, Pos + 3)
                                    Pos = genericController.vbInstr(Pos + 3, cpCore.webServer.requestPathPage, "/", vbBinaryCompare)
                                    If Pos <> 0 Then
                                        linkDomain = Mid(cpCore.webServer.requestPathPage, 1, Pos - 1)
                                        LinkFullPath = Mid(cpCore.webServer.requestPathPage, Pos)
                                        '
                                        ' strip off leading or trailing slashes, and return only the string between the leading and secton slash
                                        '
                                        If genericController.vbInstr(1, LinkFullPath, "/") <> 0 Then
                                            LinkSplit = Split(LinkFullPath, "/")
                                            LinkFullPathNoSlash = LinkSplit(0)
                                            If LinkFullPathNoSlash = "" Then
                                                If UBound(LinkSplit) > 0 Then
                                                    LinkFullPathNoSlash = LinkSplit(1)
                                                End If
                                            End If
                                        End If
                                        linkAliasTest1 = LinkFullPath
                                        linkAliasTest2 = LinkFullPathNoSlash
                                    End If
                                End If
                                '
                                '   if this has not already been recognized as a pagenot found, and the custom404source is present, try all these
                                '   Build LinkForwardCritia and LinkAliasCriteria
                                '   sample: http://www.a.com/kb/test
                                '   LinkForwardCriteria = (Sourcelink='http://www.a.com/kb/test')or(Sourcelink='http://www.a.com/kb/test/')
                                '
                                LinkForwardCriteria = "(active<>0)" _
                                    & "and(" _
                                    & "(SourceLink=" & cpCore.db.encodeSQLText(cpCore.webServer.requestPathPage) & ")" _
                                    & "or(SourceLink=" & cpCore.db.encodeSQLText(LinkNoProtocol) & ")" _
                                    & "or(SourceLink=" & cpCore.db.encodeSQLText(LinkFullPath) & ")" _
                                    & "or(SourceLink=" & cpCore.db.encodeSQLText(LinkFullPathNoSlash) & ")" _
                                    & ")"
                                isLinkForward = False
                                Sql = cpCore.db.GetSQLSelect("", "ccLinkForwards", "ID,DestinationLink,Viewings,GroupID", LinkForwardCriteria, "ID", , 1)
                                CSPointer = cpCore.db.cs_openSql(Sql)
                                If cpCore.db.cs_ok(CSPointer) Then
                                    '
                                    ' Link Forward found - update count
                                    '
                                    Dim tmpLink As String
                                    Dim GroupID As Integer
                                    Dim groupName As String
                                    '
                                    IsInLinkForwardTable = True
                                    Viewings = cpCore.db.cs_getInteger(CSPointer, "Viewings") + 1
                                    Sql = "update ccLinkForwards set Viewings=" & Viewings & " where ID=" & cpCore.db.cs_getInteger(CSPointer, "ID")
                                    Call cpCore.db.executeSql(Sql)
                                    tmpLink = cpCore.db.cs_getText(CSPointer, "DestinationLink")
                                    If tmpLink <> "" Then
                                        '
                                        ' Valid Link Forward (without link it is just a record created by the autocreate function
                                        '
                                        isLinkForward = True
                                        tmpLink = cpCore.db.cs_getText(CSPointer, "DestinationLink")
                                        GroupID = cpCore.db.cs_getInteger(CSPointer, "GroupID")
                                        If GroupID <> 0 Then
                                            groupName = groupController.group_GetGroupName(cpCore, GroupID)
                                            If groupName <> "" Then
                                                Call groupController.group_AddGroupMember(cpCore, groupName)
                                            End If
                                        End If
                                        If tmpLink <> "" Then
                                            RedirectLink = tmpLink
                                            RedirectReason = "Redirecting because the URL Is a valid Link Forward entry."
                                        End If
                                    End If
                                End If
                                Call cpCore.db.cs_Close(CSPointer)
                                '
                                If (RedirectLink = "") And Not isLinkForward Then
                                    '
                                    ' Test for Link Alias
                                    '
                                    If (linkAliasTest1 & linkAliasTest2 <> "") Then
                                        Dim sqlCriteria As String = "(link=" & cpCore.db.encodeSQLText(linkAliasTest1) & ")or(link=" & cpCore.db.encodeSQLText(linkAliasTest2) & ")"
                                        Dim linkAliasList As List(Of Models.Entity.linkAliasModel) = Models.Entity.linkAliasModel.createList(cpCore, sqlCriteria, "id desc")
                                        If (linkAliasList.Count > 0) Then
                                            Dim linkAlias As Models.Entity.linkAliasModel = linkAliasList.First
                                            Dim LinkQueryString As String = "bid=" & linkAlias.PageID & "&" & linkAlias.QueryStringSuffix
                                            cpCore.docProperties.setProperty("bid", linkAlias.PageID.ToString(), False)
                                            Dim nameValuePairs As String() = Split(linkAlias.QueryStringSuffix, "&")
                                            'Dim nameValuePairs As String() = Split(cpCore.cache_linkAlias(linkAliasCache_queryStringSuffix, Ptr), "&")
                                            For Each nameValuePair As String In nameValuePairs
                                                Dim nameValueThing As String() = Split(nameValuePair, "=")
                                                If (nameValueThing.GetUpperBound(0) = 0) Then
                                                    cpCore.docProperties.setProperty(nameValueThing(0), "", False)
                                                Else
                                                    cpCore.docProperties.setProperty(nameValueThing(0), nameValueThing(1), False)
                                                End If
                                            Next
                                        End If
                                    End If
                                    '
                                    If Not IsLinkAlias Then
                                        '
                                        ' Test for favicon.ico
                                        '
                                        If (LCase(cpCore.webServer.requestPathPage) = "/favicon.ico") Then
                                            '
                                            ' Handle Favicon.ico when the client did not recognize the meta tag
                                            '
                                            Dim Filename As String = cpCore.siteProperties.getText("FaviconFilename", "")
                                            If Filename = "" Then
                                                '
                                                ' no favicon, 404 the call
                                                '
                                                Call cpCore.webServer.setResponseStatus("404 Not Found")
                                                Call cpCore.webServer.setResponseContentType("image/gif")
                                                cpCore.continueProcessing = False '--- should be disposed by caller --- Call dispose
                                                Return cpCore.htmlDoc.docBuffer
                                            Else
                                                Call cpCore.webServer.redirect(genericController.getCdnFileLink(cpCore, Filename), "favicon request", False)
                                                cpCore.continueProcessing = False '--- should be disposed by caller --- Call dispose
                                                Return cpCore.htmlDoc.docBuffer
                                            End If
                                        End If
                                        '
                                        ' Test for robots.txt
                                        '
                                        If (LCase(LinkFullPathNoSlash) = "robots.txt") Or (LCase(LinkFullPathNoSlash) = "robots_txt") Then
                                            '
                                            ' Handle Robots.txt file
                                            '
                                            Dim Filename As String = "config/RobotsTxtBase.txt"
                                            ' set this way because the preferences page needs a filename in a site property (enhance later)
                                            Call cpCore.siteProperties.setProperty("RobotsTxtFilename", Filename)
                                            Dim Content As String = cpCore.cdnFiles.readFile(Filename)
                                            If Content = "" Then
                                                '
                                                ' save default robots.txt
                                                '
                                                Content = "User-agent: *" & vbCrLf & "Disallow: /admin/" & vbCrLf & "Disallow: /images/"
                                                Call cpCore.appRootFiles.saveFile(Filename, Content)
                                            End If
                                            Content = Content & cpCore.addonLegacyCache.addonCache.robotsTxt
                                            Call cpCore.webServer.setResponseContentType("text/plain")
                                            Call cpCore.htmlDoc.writeAltBuffer(Content)
                                            cpCore.continueProcessing = False '--- should be disposed by caller --- Call dispose
                                            Return cpCore.htmlDoc.docBuffer
                                        End If
                                        '
                                        ' No Link Forward, no Link Alias, no RemoteMethodFromPage, not Robots.txt
                                        '
                                        If (cpCore.app_errorCount = 0) And cpCore.siteProperties.getBoolean("LinkForwardAutoInsert") And (Not IsInLinkForwardTable) Then
                                            '
                                            ' Add a new Link Forward entry
                                            '
                                            CSPointer = cpCore.db.cs_insertRecord("Link Forwards")
                                            If cpCore.db.cs_ok(CSPointer) Then
                                                Call cpCore.db.cs_set(CSPointer, "Name", cpCore.webServer.requestPathPage)
                                                Call cpCore.db.cs_set(CSPointer, "sourcelink", cpCore.webServer.requestPathPage)
                                                Call cpCore.db.cs_set(CSPointer, "Viewings", 1)
                                            End If
                                            Call cpCore.db.cs_Close(CSPointer)
                                        End If
                                        '
                                        ' real 404
                                        '
                                        IsPageNotFound = True
                                        PageNotFoundSource = cpCore.webServer.requestPathPage
                                        PageNotFoundReason = "The page could Not be displayed because the URL Is Not a valid page, Link Forward, Link Alias Or RemoteMethod."
                                    End If
                                End If
                            End If
                        End If
                    End If
                    '
                    ' ----- do anonymous access blocking
                    '
                    If Not cpCore.authContext.isAuthenticated() Then
                        If (cpCore.webServer.webServerIO_requestPath <> "/") And genericController.vbInstr(1, cpCore.siteProperties.adminURL, cpCore.webServer.webServerIO_requestPath, vbTextCompare) <> 0 Then
                            '
                            ' admin page is excluded from custom blocking
                            '
                        Else
                            Dim AnonymousUserResponseID As Integer = genericController.EncodeInteger(cpCore.siteProperties.getText("AnonymousUserResponseID", "0"))
                            Select Case AnonymousUserResponseID
                                Case 1
                                    '
                                    ' block with login
                                    '
                                    '
                                    'Call AppendLog("main_init(), 3410 - exit for login block")
                                    '
                                    Call cpCore.htmlDoc.main_SetMetaContent(0, 0)
                                    Call cpCore.htmlDoc.writeAltBuffer(cpCore.htmlDoc.getLoginPage(False) & cpCore.htmlDoc.getHtmlDoc_beforeEndOfBodyHtml(False, False, False, False))
                                    cpCore.continueProcessing = False '--- should be disposed by caller --- Call dispose
                                    Return cpCore.htmlDoc.docBuffer
                                Case 2
                                    '
                                    ' block with custom content
                                    '
                                    '
                                    'Call AppendLog("main_init(), 3420 - exit for custom content block")
                                    '
                                    Call cpCore.htmlDoc.main_SetMetaContent(0, 0)
                                    Call cpCore.htmlDoc.main_AddOnLoadJavascript2("document.body.style.overflow='scroll'", "Anonymous User Block")
                                    Dim Copy As String = cr & cpCore.htmlDoc.html_GetContentCopy("AnonymousUserResponseCopy", "<p style=""width:250px;margin:100px auto auto auto;"">The site is currently not available for anonymous access.</p>", cpCore.authContext.user.id, True, cpCore.authContext.isAuthenticated)
                                    ' -- already encoded
                                    'Copy = EncodeContentForWeb(Copy, "copy content", 0, "", 0)
                                    Copy = "" _
                                            & cpCore.siteProperties.docTypeDeclaration() _
                                            & vbCrLf & "<html>" _
                                            & cr & "<head>" _
                                            & genericController.kmaIndent(cpCore.htmlDoc.getHTMLInternalHead(False)) _
                                            & cr & "</head>" _
                                            & cr & TemplateDefaultBodyTag _
                                            & genericController.kmaIndent(Copy) _
                                            & cr2 & "<div>" _
                                            & cr3 & cpCore.htmlDoc.getHtmlDoc_beforeEndOfBodyHtml(True, True, False, False) _
                                            & cr2 & "</div>" _
                                            & cr & "</body>" _
                                            & vbCrLf & "</html>"
                                    '& "<body class=""ccBodyAdmin ccCon"" style=""overflow:scroll"">"
                                    Call cpCore.htmlDoc.writeAltBuffer(Copy)
                                    cpCore.continueProcessing = False '--- should be disposed by caller --- Call dispose
                                    Return cpCore.htmlDoc.docBuffer
                            End Select
                        End If
                    End If
                    '-------------------------------------------
                    '
                    ' run the appropriate body addon
                    '
                    bodyAddonId = genericController.EncodeInteger(cpCore.siteProperties.getText("Html Body AddonId", "0"))
                    If bodyAddonId <> 0 Then
                        htmlBody = cpCore.addon.execute(bodyAddonId, "", "", CPUtilsBaseClass.addonContext.ContextPage, "", 0, "", "", False, 0, "", bodyAddonStatusOK, Nothing, "", Nothing, "", cpCore.authContext.user.id, cpCore.authContext.isAuthenticated)
                    Else
                        htmlBody = getBody(cpCore)
                    End If
                    If cpCore.continueProcessing Then
                        '
                        ' Build Body Tag
                        '
                        htmlHead = cpCore.htmlDoc.getHTMLInternalHead(False)
                        If cpCore.pages.template.BodyTag <> "" Then
                            bodyTag = cpCore.pages.template.BodyTag
                        Else
                            bodyTag = TemplateDefaultBodyTag
                        End If
                        '
                        ' Add tools panel to body
                        '
                        htmlBody = htmlBody & cr & "<div>" & genericController.kmaIndent(cpCore.htmlDoc.getHtmlDoc_beforeEndOfBodyHtml(True, True, False, False)) & cr & "</div>"
                        '
                        ' build doc
                        '
                        returnHtml = cpCore.htmlDoc.main_assembleHtmlDoc(cpCore.siteProperties.docTypeDeclaration(), htmlHead, bodyTag, cpCore.htmlDoc.docBuffer & htmlBody)
                    End If
                End If
                '
                ' all other routes should be handled here.
                '   - this code is in initApp right now but should be migrated here.
                '   - if all other routes fail, use the defaultRoute (pagemanager at first)
                '
                If True Then
                    ' --- not reall sure what to do with this - was in appInit() and I am just sure it does not go there.
                    '
                    '--------------------------------------------------------------------------
                    ' ----- check if the custom404pathpage matches the defaultdoc
                    '       in this case, the 404 hit is a direct result of a 404 I justreturned to IIS
                    '       currently, I am redirecting to the page-not-found page with a 404 - wrong
                    '       I should realize here that this is a 404 caused by the page in the 404 custom string
                    '           and display the 404 page. Even if all I can say is "the page was not found"
                    '
                    '--------------------------------------------------------------------------
                    '
                    If genericController.vbLCase(cpCore.webServer.requestPathPage) = genericController.vbLCase(requestAppRootPath & cpCore.siteProperties.serverPageDefault) Then
                        '
                        ' This is a 404 caused by Contensive returning a 404
                        '   possibly because the pageid was not found or was inactive.
                        '   contensive returned a 404 error, and the IIS custom error handler is hitting now
                        '   what we returned as an error cause is lost
                        '   ( because the Custom404Source page is the default page )
                        '   send it to the 404 page
                        '
                        cpCore.webServer.requestPathPage = cpCore.webServer.requestPathPage
                        IsPageNotFound = True
                        PageNotFoundReason = "The page could not be displayed. The record may have been deleted, marked inactive. The page's parent pages or section may be invalid."
                    End If
                End If
                If False Then
                    '
                    'todo consider if we will keep this. It is not straightforward, and and more straightforward method may exist
                    '
                    ' Determine where to go next
                    '   If the current page is not the referring page, redirect to the referring page
                    '   Because...
                    '   - the page with the form (the referrer) was a link alias page. You can not post to a link alias, so internally we post to the default page, and redirect back.
                    '   - This only acts on internal Contensive forms, so developer pages are not effected
                    '   - This way, if the form post comes from a main_GetJSPage Remote Method, it posts to the Content Server,
                    '       then redirects back to the static site (with the new changed content)
                    '
                    If cpCore.webServer.requestReferrer <> "" Then
                        Dim main_ServerReferrerURL As String
                        Dim main_ServerReferrerQs As String
                        Dim Position As Integer
                        main_ServerReferrerURL = cpCore.webServer.requestReferrer
                        main_ServerReferrerQs = ""
                        Position = genericController.vbInstr(1, main_ServerReferrerURL, "?")
                        If Position <> 0 Then
                            main_ServerReferrerQs = Mid(main_ServerReferrerURL, Position + 1)
                            main_ServerReferrerURL = Mid(main_ServerReferrerURL, 1, Position - 1)
                        End If
                        If Right(main_ServerReferrerURL, 1) = "/" Then
                            '
                            ' Referer had no page, figure out what it should have been
                            '
                            If cpCore.webServer.webServerIO_requestPage <> "" Then
                                '
                                ' If the referer had no page, and there is one here now, it must have been from an IIS redirect, use the current page as the default page
                                '
                                main_ServerReferrerURL = main_ServerReferrerURL & cpCore.webServer.webServerIO_requestPage
                            Else
                                main_ServerReferrerURL = main_ServerReferrerURL & cpCore.siteProperties.serverPageDefault
                            End If
                        End If
                        Dim linkDst As String
                        'main_ServerPage = main_ServerPage
                        If main_ServerReferrerURL <> cpCore.webServer.webServerIO_ServerFormActionURL Then
                            '
                            ' remove any methods from referrer
                            '
                            Dim Copy As String
                            Copy = "Redirecting because a Contensive Form was detected, source URL [" & main_ServerReferrerURL & "] does not equal the current URL [" & cpCore.webServer.webServerIO_ServerFormActionURL & "]. This may be from a Contensive Add-on that now needs to redirect back to the host page."
                            linkDst = cpCore.webServer.webServerIO_requestReferer
                            If main_ServerReferrerQs <> "" Then
                                linkDst = main_ServerReferrerURL
                                main_ServerReferrerQs = genericController.ModifyQueryString(main_ServerReferrerQs, "method", "")
                                If main_ServerReferrerQs <> "" Then
                                    linkDst = linkDst & "?" & main_ServerReferrerQs
                                End If
                            End If
                            Call cpCore.webServer.redirect(linkDst, Copy, False)
                            cpCore.continueProcessing = False '--- should be disposed by caller --- Call dispose
                        End If
                    End If
                End If
                If True Then
                    ' - same here, this was in appInit() to prcess the pagenotfounds - maybe here (at the end, maybe in pageManager)
                    '--------------------------------------------------------------------------
                    ' ----- Process Early page-not-found
                    '--------------------------------------------------------------------------
                    '
                    If IsPageNotFound Then
                        If True Then
                            '
                            ' new way -- if a (real) 404 page is received, just convert this hit to the page-not-found page, do not redirect to it
                            '
                            Call logController.log_appendLogPageNotFound(cpCore, cpCore.webServer.requestUrlSource)
                            Call cpCore.webServer.setResponseStatus("404 Not Found")
                            cpCore.docProperties.setProperty("bid", cpCore.pages.main_GetPageNotFoundPageId())
                            'Call main_mergeInStream("bid=" & main_GetPageNotFoundPageId())
                            If cpCore.authContext.isAuthenticatedAdmin(cpCore) Then
                                cpCore.htmlDoc.main_AdminWarning = PageNotFoundReason
                                cpCore.htmlDoc.main_AdminWarningPageID = 0
                            End If
                        Else
                            '
                            ' old way -- if a (real) 404 page is received, redirect to it to the 404 page with content
                            '
                            RedirectReason = PageNotFoundReason
                            RedirectLink = cpCore.pages.main_ProcessPageNotFound_GetLink(PageNotFoundReason, , PageNotFoundSource)
                        End If
                    End If
                End If
                '
                ' add exception list header
                '
                returnHtml = errorController.getDocExceptionHtmlList(cpCore) & returnHtml
                '
            Catch ex As Exception
                Throw New ApplicationException("Unexpected exception") ' Call cpcore.handleLegacyError18("main_GetHTMLDoc2")
            End Try
            Return returnHtml
        End Function
        '
        '
        '
        Private Sub processForm(cpcore As coreClass, FormPageID As Integer)
            Try
                '
                Dim CS As Integer
                Dim SQL As String
                Dim Formhtml As String
                Dim FormInstructions As String
                Dim f As pagesController.main_FormPagetype
                Dim Ptr As Integer
                Dim CSPeople As Integer
                Dim IsInGroup As Boolean
                Dim WasInGroup As Boolean
                Dim FormValue As String
                Dim Success As Boolean
                Dim PeopleFirstName As String
                Dim PeopleLastName As String
                Dim PeopleUsername As String
                Dim PeoplePassword As String
                Dim PeopleName As String
                Dim PeopleEmail As String
                Dim Groups() As String
                Dim GroupName As String
                Dim GroupIDToJoinOnSuccess As Integer
                '
                ' main_Get the instructions from the record
                '
                CS = cpcore.db.csOpenRecord("Form Pages", FormPageID)
                If cpcore.db.cs_ok(CS) Then
                    Formhtml = cpcore.db.cs_getText(CS, "Body")
                    FormInstructions = cpcore.db.cs_getText(CS, "Instructions")
                End If
                Call cpcore.db.cs_Close(CS)
                If FormInstructions <> "" Then
                    '
                    ' Load the instructions
                    '
                    f = cpcore.pages.pageManager_LoadFormPageInstructions(FormInstructions, Formhtml)
                    If f.AuthenticateOnFormProcess And Not cpcore.authContext.isAuthenticated() And cpcore.authContext.isRecognized(cpcore) Then
                        '
                        ' If this form will authenticate when done, and their is a current, non-authenticated account -- logout first
                        '
                        Call cpcore.authContext.logout(cpcore)
                    End If
                    CSPeople = -1
                    Success = True
                    For Ptr = 0 To UBound(f.Inst)
                        With f.Inst(Ptr)
                            Select Case .Type
                                Case 1
                                    '
                                    ' People Record
                                    '
                                    FormValue = cpcore.docProperties.getText(.PeopleField)
                                    If (FormValue <> "") And genericController.EncodeBoolean(cpcore.metaData.GetContentFieldProperty("people", .PeopleField, "uniquename")) Then
                                        SQL = "select count(*) from ccMembers where " & .PeopleField & "=" & cpcore.db.encodeSQLText(FormValue)
                                        CS = cpcore.db.cs_openSql(SQL)
                                        If cpcore.db.cs_ok(CS) Then
                                            Success = cpcore.db.cs_getInteger(CS, "cnt") = 0
                                        End If
                                        Call cpcore.db.cs_Close(CS)
                                        If Not Success Then
                                            errorController.error_AddUserError(cpcore, "The field [" & .Caption & "] must be unique, and the value [" & cpcore.htmlDoc.html_EncodeHTML(FormValue) & "] has already been used.")
                                        End If
                                    End If
                                    If (.REquired Or genericController.EncodeBoolean(cpcore.metaData.GetContentFieldProperty("people", .PeopleField, "required"))) And FormValue = "" Then
                                        Success = False
                                        errorController.error_AddUserError(cpcore, "The field [" & cpcore.htmlDoc.html_EncodeHTML(.Caption) & "] is required.")
                                    Else
                                        If Not cpcore.db.cs_ok(CSPeople) Then
                                            CSPeople = cpcore.db.csOpenRecord("people", cpcore.authContext.user.id)
                                        End If
                                        If cpcore.db.cs_ok(CSPeople) Then
                                            Select Case genericController.vbUCase(.PeopleField)
                                                Case "NAME"
                                                    PeopleName = FormValue
                                                    Call cpcore.db.cs_set(CSPeople, .PeopleField, FormValue)
                                                Case "FIRSTNAME"
                                                    PeopleFirstName = FormValue
                                                    Call cpcore.db.cs_set(CSPeople, .PeopleField, FormValue)
                                                Case "LASTNAME"
                                                    PeopleLastName = FormValue
                                                    Call cpcore.db.cs_set(CSPeople, .PeopleField, FormValue)
                                                Case "EMAIL"
                                                    PeopleEmail = FormValue
                                                    Call cpcore.db.cs_set(CSPeople, .PeopleField, FormValue)
                                                Case "USERNAME"
                                                    PeopleUsername = FormValue
                                                    Call cpcore.db.cs_set(CSPeople, .PeopleField, FormValue)
                                                Case "PASSWORD"
                                                    PeoplePassword = FormValue
                                                    Call cpcore.db.cs_set(CSPeople, .PeopleField, FormValue)
                                                Case Else
                                                    Call cpcore.db.cs_set(CSPeople, .PeopleField, FormValue)
                                            End Select
                                        End If
                                    End If
                                Case 2
                                    '
                                    ' Group main_MemberShip
                                    '
                                    IsInGroup = cpcore.docProperties.getBoolean("Group" & .GroupName)
                                    WasInGroup = cpcore.authContext.IsMemberOfGroup2(cpcore, .GroupName)
                                    If WasInGroup And Not IsInGroup Then
                                        groupController.group_DeleteGroupMember(cpcore, .GroupName)
                                    ElseIf IsInGroup And Not WasInGroup Then
                                        groupController.group_AddGroupMember(cpcore, .GroupName)
                                    End If
                            End Select
                        End With
                    Next
                    '
                    ' Create People Name
                    '
                    If PeopleName = "" And PeopleFirstName <> "" And PeopleLastName <> "" Then
                        If cpcore.db.cs_ok(CSPeople) Then
                            Call cpcore.db.cs_set(CSPeople, "name", PeopleFirstName & " " & PeopleLastName)
                        End If
                    End If
                    Call cpcore.db.cs_Close(CSPeople)
                    '
                    ' AuthenticationOnFormProcess requires Username/Password and must be valid
                    '
                    If Success Then
                        '
                        ' Authenticate
                        '
                        If f.AuthenticateOnFormProcess Then
                            Call cpcore.authContext.authenticateById(cpcore, cpcore.authContext.user.id, cpcore.authContext)
                        End If
                        '
                        ' Join Group requested by page that created form
                        '
                        Dim tokenDate As Date
                        Call cpcore.security.decodeToken(cpcore.docProperties.getText("SuccessID"), GroupIDToJoinOnSuccess, tokenDate)
                        'GroupIDToJoinOnSuccess = main_DecodeKeyNumber(main_GetStreamText2("SuccessID"))
                        If GroupIDToJoinOnSuccess <> 0 Then
                            Call groupController.group_AddGroupMember(cpcore, groupController.group_GetGroupName(cpcore, GroupIDToJoinOnSuccess))
                        End If
                        '
                        ' Join Groups requested by pageform
                        '
                        If f.AddGroupNameList <> "" Then
                            Groups = Split(Trim(f.AddGroupNameList), ",")
                            For Ptr = 0 To UBound(Groups)
                                GroupName = Trim(Groups(Ptr))
                                If GroupName <> "" Then
                                    Call groupController.group_AddGroupMember(cpcore, GroupName)
                                End If
                            Next
                        End If
                    End If
                End If
            Catch ex As Exception
                cpcore.handleExceptionAndContinue(ex)
                Throw
            End Try
        End Sub
        '
        '=============================================================================
        '   main_Get Section
        '       Two modes
        '           pre 3.3.613 - SectionName = RootPageName
        '           else - (IsSectionRootPageIDMode) SectionRecord has a RootPageID field
        '=============================================================================
        '
        Public Function getBodyContent(cpCore As coreClass, AllowChildPageList As Boolean, AllowReturnLink As Boolean, AllowEditWrapper As Boolean, ByRef return_blockSiteWithLogin As Boolean) As String
            Dim returnHtml As String = ""
            Try
                Dim allowPageWithoutSectionDislay As Boolean
                Dim domainIds() As String
                Dim setdomainId As Integer
                Dim linkDomain As String
                Dim templatedomainIdList As String
                Dim FieldRows As Integer
                Dim templateId As Integer
                Dim RootPageContentName As String
                Dim Ptr As Integer
                Dim PageID As Integer
                Dim UseContentWatchLink As Boolean = cpCore.siteProperties.useContentWatchLink
                '
                RootPageContentName = "Page Content"
                '
                ' -- get domain
                cpCore.pages.domain = Models.Entity.domainModel.createByName(cpCore, cpCore.webServer.requestDomain, New List(Of String))
                If (cpCore.pages.domain Is Nothing) Then
                    '
                    ' -- domain not listed, this is now an error
                    Call logController.log_appendLogPageNotFound(cpCore, cpCore.webServer.requestUrlSource)
                    Return "<div style=""width:300px; margin: 100px auto auto auto;text-align:center;"">The domain name is not configured for this site.</div>"
                End If
                '
                ' -- get pageid
                PageID = cpCore.docProperties.getInteger("bid")
                If (PageID = 0) Then
                    '
                    ' -- Nothing specified, use the Landing Page
                    PageID = cpCore.pages.main_GetLandingPageID()
                    If PageID = 0 Then
                        '
                        ' -- landing page is not valid -- display error
                        Call logController.log_appendLogPageNotFound(cpCore, cpCore.webServer.requestUrlSource)
                        cpCore.pages.pageManager_RedirectBecausePageNotFound = True
                        cpCore.pages.pageManager_RedirectReason = "Redirecting because the page selected could not be found."
                        cpCore.pages.redirectLink = cpCore.pages.main_ProcessPageNotFound_GetLink(cpCore.pages.pageManager_RedirectReason, , , PageID, 0)

                        cpCore.handleExceptionAndContinue(New ApplicationException("Page could not be determined. Error message displayed."))
                        Return "<div style=""width:300px; margin: 100px auto auto auto;text-align:center;"">This page is not valid.</div>"
                    End If
                End If
                Call cpCore.htmlDoc.webServerIO_addRefreshQueryString("bid", CStr(PageID))
                cpCore.pages.templateReason = "The reason this template was selected could not be determined."
                '
                ' -- build parentpageList (first = current page, last = root)
                ' -- add a 0, then repeat until another 0 is found, or there is a repeat
                cpCore.pages.pageToRootList = New List(Of Models.Entity.pageContentModel)()
                Dim usedPageIdList As New List(Of Integer)()
                Dim targetPageId = PageID
                usedPageIdList.Add(0)
                Do While (Not usedPageIdList.Contains(targetPageId))
                    usedPageIdList.Add(targetPageId)
                    Dim targetpage As Models.Entity.pageContentModel = Models.Entity.pageContentModel.create(cpCore, targetPageId, New List(Of String))
                    If (targetpage Is Nothing) Then
                        Exit Do
                    Else
                        cpCore.pages.pageToRootList.Add(targetpage)
                        targetPageId = targetpage.ID
                    End If
                Loop
                If (cpCore.pages.pageToRootList.Count = 0) Then
                    '
                    ' -- page is not valid -- display error
                    cpCore.handleExceptionAndContinue(New ApplicationException("Page could not be determined. Error message displayed."))
                    Return "<div style=""width:300px; margin: 100px auto auto auto;text-align:center;"">This page is not valid.</div>"
                End If
                cpCore.pages.page = cpCore.pages.pageToRootList.First
                '
                ' -- get contentBox
                Dim contentBoxHtml As String = cpCore.pages.getContentBox("", AllowChildPageList, AllowReturnLink, False, 0, UseContentWatchLink, allowPageWithoutSectionDislay)
                '
                ' -- get template from pages
                Dim template As Models.Entity.pageTemplateModel = Nothing
                For Each page As Models.Entity.pageContentModel In cpCore.pages.pageToRootList
                    If page.TemplateID > 0 Then
                        template = Models.Entity.pageTemplateModel.create(cpCore, page.TemplateID, New List(Of String))
                        If (template IsNot Nothing) Then
                            If (page Is cpCore.pages.page) Then
                                cpCore.pages.templateReason = "This template was used because it is selected by the current page."
                            Else
                                cpCore.pages.templateReason = "This template was used because it is selected one of this page's parents [" & page.Name & "]."
                            End If
                            Exit For
                        End If
                    End If
                Next
                '
                If (template Is Nothing) Then
                    '
                    ' -- get template from domain
                    If (cpCore.pages.domain IsNot Nothing) Then
                        template = Models.Entity.pageTemplateModel.create(cpCore, cpCore.pages.domain.DefaultTemplateId, New List(Of String))
                    End If
                    If (template Is Nothing) Then
                        '
                        ' -- get template named Default
                        template = Models.Entity.pageTemplateModel.createByName(cpCore, "default", New List(Of String))
                    End If
                End If
                '
                ' -- get contentbox
                returnHtml = cpCore.pages.getContentBox("", AllowChildPageList, AllowReturnLink, False, 0, UseContentWatchLink, allowPageWithoutSectionDislay)
                '
                ' -- add template details to document
                Call cpCore.htmlDoc.main_AddOnLoadJavascript2(cpCore.pages.template.JSOnLoad, "template")
                Call cpCore.htmlDoc.main_AddHeadScriptCode(cpCore.pages.template.JSHead, "template")
                Call cpCore.htmlDoc.main_AddEndOfBodyJavascript2(cpCore.pages.template.JSEndBody, "template")
                Call cpCore.htmlDoc.main_AddHeadTag2(cpCore.pages.template.OtherHeadTags, "template")
                If cpCore.pages.template.StylesFilename <> "" Then
                    cpCore.htmlDoc.main_MetaContent_TemplateStyleSheetTag = cr & "<link rel=""stylesheet"" type=""text/css"" href=""" & cpCore.webServer.webServerIO_requestProtocol & cpCore.webServer.requestDomain & genericController.getCdnFileLink(cpCore, cpCore.pages.template.StylesFilename) & """ >"
                End If
                '
                ' -- add shared styles
                Dim sql As String = "(templateId=" & template.ID & ")"
                Dim styleList As List(Of Models.Entity.SharedStylesTemplateRuleModel) = Models.Entity.SharedStylesTemplateRuleModel.createList(cpCore, sql, "sortOrder,id")
                For Each rule As SharedStylesTemplateRuleModel In styleList
                    Call cpCore.htmlDoc.main_AddSharedStyleID2(rule.StyleID, "template")
                Next
                '
                ' -- check secure certificate required
                Dim SecureLink_Template_Required As Boolean = template.IsSecure
                Dim SecureLink_Page_Required As Boolean = False
                For Each page As Models.Entity.pageContentModel In cpCore.pages.pageToRootList
                    If page.IsSecure Then
                        SecureLink_Page_Required = True
                        Exit For
                    End If
                Next
                Dim SecureLink_Required As Boolean = SecureLink_Template_Required Or SecureLink_Page_Required
                Dim SecureLink_CurrentURL As Boolean = (Left(LCase(cpCore.webServer.requestUrl), 8) = "https://")
                If (SecureLink_CurrentURL And (Not SecureLink_Required)) Then
                    '
                    ' -- redirect to non-secure
                    cpCore.pages.redirectLink = genericController.vbReplace(cpCore.webServer.requestUrl, "https://", "http://")
                    cpCore.pages.pageManager_RedirectReason = "Redirecting because neither the page or the template requires a secure link."
                    Return ""
                ElseIf ((Not SecureLink_CurrentURL) And SecureLink_Required) Then
                    '
                    ' -- redirect to secure
                    cpCore.pages.redirectLink = genericController.vbReplace(cpCore.webServer.requestUrl, "http://", "https://")
                    If SecureLink_Page_Required Then
                        cpCore.pages.pageManager_RedirectReason = "Redirecting because this page [" & cpCore.pages.pageToRootList(0).Name & "] requires a secure link."
                    Else
                        cpCore.pages.pageManager_RedirectReason = "Redirecting because this template [" & cpCore.pages.template.Name & "] requires a secure link."
                    End If
                    Return ""
                End If
                '
                ' -- check that this template exists on this domain
                ' -- if endpoint is just domain -> the template is automatically compatible by default (domain determined the landing page)
                ' -- if endpoint is domain + route (link alias), the route determines the page, which may determine the template. If this template is not allowed for this domain, redirect to the domain's landing page.
                '
                sql = "(domainId=" & cpCore.pages.domain.ID & ")"
                Dim allowTemplateRuleList As List(Of Models.Entity.TemplateDomainRuleModel) = Models.Entity.TemplateDomainRuleModel.createList(cpCore, sql)
                If (allowTemplateRuleList.Count = 0) Then
                    '
                    ' -- current template has no domain preference, use current
                Else
                    Dim allowTemplate As Boolean = False
                    For Each rule As TemplateDomainRuleModel In allowTemplateRuleList
                        If (rule.templateId = cpCore.pages.template.ID) Then
                            allowTemplate = True
                            Exit For
                        End If
                    Next
                    If (Not allowTemplate) Then
                        '
                        ' -- must redirect to a domain's landing page
                        cpCore.pages.redirectLink = cpCore.webServer.webServerIO_requestProtocol & cpCore.pages.domain.Name
                        cpCore.pages.pageManager_RedirectBecausePageNotFound = False
                        cpCore.pages.pageManager_RedirectReason = "Redirecting because this domain has template requiements set, and this template is not configured [" & cpCore.pages.template.Name & "]."
                        Return ""
                    End If
                End If
                '
                ' -- if fpo_QuickEdit it there, replace it out
                Dim Editor As String
                Dim styleOptionList As String
                Dim addonListJSON As String
                If cpCore.pages.redirectLink = "" And (InStr(1, returnHtml, html_quickEdit_fpo) <> 0) Then
                    FieldRows = genericController.EncodeInteger(cpCore.userProperty.getText("Page Content.copyFilename.PixelHeight", "500"))
                    If FieldRows < 50 Then
                        FieldRows = 50
                        Call cpCore.userProperty.setProperty("Page Content.copyFilename.PixelHeight", 50)
                    End If
                    Dim stylesheetCommaList As String = cpCore.htmlDoc.main_GetStyleSheet2(csv_contentTypeEnum.contentTypeWeb, templateId, 0)
                    addonListJSON = cpCore.htmlDoc.main_GetEditorAddonListJSON(csv_contentTypeEnum.contentTypeWeb)
                    Editor = cpCore.htmlDoc.html_GetFormInputHTML3("copyFilename", cpCore.htmlDoc.html_quickEdit_copy, CStr(FieldRows), "100%", False, True, addonListJSON, stylesheetCommaList, styleOptionList)
                    returnHtml = genericController.vbReplace(returnHtml, html_quickEdit_fpo, Editor)
                End If
                '
                ' -- Add admin warning to the top of the content
                If cpCore.authContext.isAuthenticatedAdmin(cpCore) And cpCore.htmlDoc.main_AdminWarning <> "" Then
                    '
                    ' Display Admin Warnings with Edits for record errors
                    '
                    If cpCore.htmlDoc.main_AdminWarningPageID <> 0 Then
                        cpCore.htmlDoc.main_AdminWarning = cpCore.htmlDoc.main_AdminWarning & "</p>" & cpCore.htmlDoc.main_GetRecordEditLink2("Page Content", cpCore.htmlDoc.main_AdminWarningPageID, True, "Page " & cpCore.htmlDoc.main_AdminWarningPageID, cpCore.authContext.isAuthenticatedAdmin(cpCore)) & "&nbsp;Edit the page<p>"
                        cpCore.htmlDoc.main_AdminWarningPageID = 0
                    End If
                    '
                    returnHtml = "" _
                    & cpCore.htmlDoc.html_GetAdminHintWrapper(cpCore.htmlDoc.main_AdminWarning) _
                    & returnHtml _
                    & ""
                    cpCore.htmlDoc.main_AdminWarning = ""
                End If
                '
                ' -- handle redirect and edit wrapper
                '------------------------------------------------------------------------------------
                '
                If cpCore.pages.redirectLink <> "" Then
                    Call cpCore.webServer.redirect(cpCore.pages.redirectLink, cpCore.pages.pageManager_RedirectReason, cpCore.pages.pageManager_RedirectBecausePageNotFound)
                ElseIf AllowEditWrapper Then
                    returnHtml = cpCore.htmlDoc.main_GetEditWrapper("Page Content", returnHtml)
                End If
                '
            Catch ex As Exception
                cpCore.handleExceptionAndContinue(ex)
            End Try
            Return returnHtml
        End Function
    End Class
End Namespace
