﻿
Option Explicit On
Option Strict On

Namespace Contensive.Core
    '
    '====================================================================================================
    ''' <summary>
    ''' classSummary
    ''' </summary>
    Public Class coreUserClass
        '
        Private cpCore As cpCoreClass
        '
        '====================================================================================================
        ''' <summary>
        ''' id for the user. When this property is set, all public user. properteries are updated for this selected id
        ''' </summary>
        ''' <returns></returns>
        Public Property id As Integer
            Get
                Return _id
            End Get
            Set(value As Integer)
                _id = initializeUser(value)
            End Set
        End Property
        Private _id As Integer = 0
        '
        ' simple shared properties, derived from the userId when .id set (through initilizeUser method)
        '
        Friend name As String = ""                 '
        Friend isAdmin As Boolean = False              '
        Friend isDeveloper As Boolean = False          '
        Friend organizationId As Integer = 0         ' The members Organization
        Friend languageId As Integer = 0             '
        Friend language As String = ""             '
        Friend isNew As Boolean = False                ' stored in visit record - Is this the first visit for this member
        Friend email As String = ""               '
        '
        Friend allowBulkEmail As Boolean = False      ' Allow bulk mail
        Friend allowToolsPanel As Boolean = False    '
        Friend autoLogin As Boolean = False         ' if true, and setup AllowAutoLogin then use cookie to login
        Friend adminMenuModeID As Integer = 0     '
        '
        Friend userAdded As Boolean = False              ' depricated - true only during the page that the join was completed - use for redirections and GroupAdds
        Friend username As String = ""
        Friend password As String = ""
        Friend contentControlID As Integer = 0
        '
        Friend styleFilename As String = ""          ' if not empty, add to head
        Friend excludeFromAnalytics As Boolean = False   ' if true, future visits will be marked exclude from analytics
        '
        Private property_user_isAuthenticated As Boolean = False
        Private property_user_isAuthenticated_isLoaded As Boolean = False
        '
        Public main_IsEditingContentList As String = ""
        Public main_IsNotEditingContentList As String = ""
        '
        '-----------------------------------------------------------------------
        ' ----- Member Private
        '-----------------------------------------------------------------------
        '
        Public active As Boolean = False           '
        Public visits As Integer = 0                '
        Public lastVisit As Date = Date.MinValue             ' The last visit by the Member (the beginning of this visit
        '
        Public company As String = ""
        Public user_Title As String = ""
        Public main_MemberAddress As String = ""
        Public main_MemberCity As String = ""
        Public main_MemberState As String = ""
        Public main_MemberZip As String = ""
        Public main_MemberCountry As String = ""
        '
        Public main_MemberPhone As String = ""
        Public main_MemberFax As String = ""
        '
        '-----------------------------------------------------------------------
        ' ----- Member Commerce properties
        '-----------------------------------------------------------------------
        '
        Public user_billEmail As String = ""          ' Billing Address for purchases
        Public user_billPhone As String = ""          '
        Public user_billFax As String = ""            '
        Public main_MemberBillCompany As String = ""        '
        Public main_MemberBillAddress As String = ""        '
        Public main_MemberBillCity As String = ""           '
        Public main_MemberBillState As String = ""         '
        Public main_MemberBillZip As String = ""            '
        Public main_MemberBillCountry As String = ""       '
        '
        Public main_MemberShipName As String = ""          ' Mailing Address
        Public main_MemberShipCompany As String = ""           '
        Public main_MemberShipAddress As String = ""          '
        Public main_MemberShipCity As String = ""          '
        Public main_MemberShipState As String = ""        '
        Public main_MemberShipZip As String = ""           '
        Public main_MemberShipCountry As String = ""         '
        Public main_MemberShipPhone As String = ""        '
        '
        '----------------------------------------------------------------------------------------------------
        '
        Public loginForm_Username As String = ""       ' Values entered with the login form
        Public loginForm_Password As String = ""       '   =
        Public loginForm_Email As String = ""          '   =
        Public loginForm_AutoLogin As Boolean = False    '   =
        '
        '
        Public Const main_maxVisitLoginAttempts = 20
        Public main_loginFormDefaultProcessed As Boolean = False       ' prevent main_ProcessLoginFormDefault from running twice (multiple user messages, popups, etc.)
        '
        '------------------------------------------------------------------------
        ' ----- local cache to speed up user.main_IsContentManager
        '------------------------------------------------------------------------
        '
        Private main_GetContentAccessRights_NotList As String = ""                  ' If ContentId in this list, they are not a content manager
        Private main_GetContentAccessRights_List As String = ""                     ' If ContentId in this list, they are a content manager
        Private main_GetContentAccessRights_AllowAddList As String = ""             ' If in _List, test this for allowAdd
        Private main_GetContentAccessRights_AllowDeleteList As String = ""          ' If in _List, test this for allowDelete
        '
        '========================================================================
        ''' <summary>
        ''' is Guest
        ''' </summary>
        ''' <returns></returns>
        Public Function user_IsGuest() As Boolean
            Return Not user_isAuthenticatedMember()
        End Function
        '
        '========================================================================
        ''' <summary>
        ''' Is Recognized (not new and not authenticted)
        ''' </summary>
        ''' <returns></returns>
        Public Function user_isRecognized() As Boolean
            Return Not isNew
        End Function
        '
        '========================================================================
        ''' <summary>
        ''' authenticated
        ''' </summary>
        ''' <returns></returns>
        Public Function isAuthenticated() As Boolean
            Return cpCore.visit_isAuthenticated
        End Function
        '
        '========================================================================
        ''' <summary>
        ''' true if editing any content
        ''' </summary>
        ''' <returns></returns>
        Public Function user_isEditingAnything() As Boolean
            Return user_isEditing("")
        End Function
        '
        '========================================================================
        ''' <summary>
        ''' True if editing a specific content
        ''' </summary>
        ''' <param name="ContentNameOrId"></param>
        ''' <returns></returns>
        Public Function user_isEditing(ByVal ContentNameOrId As String) As Boolean
            Dim returnResult As Boolean = False
            Try
                If True Then
                    Dim localContentNameOrId As String
                    Dim cacheTestName As String
                    '
                    If Not cpCore.visit_initialized Then
                        Call cpCore.testPoint("...visit not initialized")
                    Else
                        '
                        ' always false until visit loaded
                        '
                        localContentNameOrId = EncodeText(ContentNameOrId)
                        cacheTestName = localContentNameOrId
                        If cacheTestName = "" Then
                            cacheTestName = "iseditingall"
                        End If
                        cacheTestName = LCase(cacheTestName)
                        If IsInDelimitedString(main_IsEditingContentList, cacheTestName, ",") Then
                            Call cpCore.testPoint("...is in main_IsEditingContentList")
                            returnResult = True
                        ElseIf IsInDelimitedString(main_IsNotEditingContentList, cacheTestName, ",") Then
                            Call cpCore.testPoint("...is in main_IsNotEditingContentList")
                        Else
                            If isAuthenticated() Then
                                If Not cpCore.main_ServerPagePrintVersion Then
                                    If (cpCore.visitProperty.getBoolean("AllowEditing") Or cpCore.visitProperty.getBoolean("AllowAdvancedEditor")) Then
                                        If localContentNameOrId <> "" Then
                                            If IsNumeric(localContentNameOrId) Then
                                                localContentNameOrId = cpCore.metaData.getContentNameByID(EncodeInteger(localContentNameOrId))
                                            End If
                                        End If
                                        returnResult = isAuthenticatedContentManager(localContentNameOrId)
                                    End If
                                End If
                            End If
                            If returnResult Then
                                main_IsEditingContentList = main_IsEditingContentList & "," & cacheTestName
                            Else
                                main_IsNotEditingContentList = main_IsNotEditingContentList & "," & cacheTestName
                            End If
                        End If
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnResult
        End Function
        '
        '========================================================================
        ''' <summary>
        ''' true if editing with the quick editor
        ''' </summary>
        ''' <param name="ContentName"></param>
        ''' <returns></returns>
        Public Function user_isQuickEditing(ByVal ContentName As String) As Boolean
            Dim returnResult As Boolean = False
            Try
                If (Not cpCore.main_ServerPagePrintVersion) Then
                    If isAuthenticatedContentManager(EncodeText(ContentName)) Then
                        returnResult = cpCore.visitProperty.getBoolean("AllowQuickEditor")
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnResult
        End Function
        '
        '========================================================================
        ' main_IsAdvancedEditing( ContentName )
        ''' <summary>
        ''' true if advanded editing
        ''' </summary>
        ''' <param name="ContentName"></param>
        ''' <returns></returns>
        Public Function user_IsAdvancedEditing(ByVal ContentName As String) As Boolean
            Dim returnResult As Boolean = False
            Try
                If (Not cpCore.main_ServerPagePrintVersion) Then
                    If isAuthenticatedContentManager(EncodeText(ContentName)) Then
                        returnResult = cpCore.visitProperty.getBoolean("AllowAdvancedEditor")
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnResult
        End Function
        '
        '========================================================================
        '   main_IsAdmin
        '   true if:
        '       Is Authenticated
        '       Is Member
        '       Member has admin or developer status
        '========================================================================
        '
        Public Function isAuthenticatedAdmin() As Boolean
            Dim returnIs As Boolean = False
            Try
                If (Not isAuthenticatedAdmin_cache_isLoaded) And cpCore.visit_initialized Then
                    isAuthenticatedAdmin_cache = isAuthenticated() And (isAdmin Or isDeveloper)
                    isAuthenticatedAdmin_cache_isLoaded = True
                End If
                returnIs = isAuthenticatedAdmin_cache
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnIs
        End Function
        Private isAuthenticatedAdmin_cache As Boolean = False               ' true if member is administrator
        Private isAuthenticatedAdmin_cache_isLoaded As Boolean = False              ' true if main_IsAdminCache is initialized
        '
        '========================================================================
        '   main_IsDeveloper
        '========================================================================
        '
        Public Function isAuthenticatedDeveloper() As Boolean
            Dim returnIs As Boolean = False
            Try
                If (Not isAuthenticatedDeveloper_cache_isLoaded) And cpCore.visit_initialized Then
                    isAuthenticatedDeveloper_cache = (isAuthenticated() And isDeveloper)
                    isAuthenticatedDeveloper_cache_isLoaded = True
                End If
                returnIs = isAuthenticatedDeveloper_cache
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnIs
        End Function
        '
        Private isAuthenticatedDeveloper_cache As Boolean = False
        Private isAuthenticatedDeveloper_cache_isLoaded As Boolean = False
        '
        '=============================================================================
        '   main_SaveMember()
        '       Saves the member properties that are loaded during main_OpenMember
        '=============================================================================
        '
        Public Sub user_SaveMember()
            Try
                Dim SQL As String
                '
                If cpCore.visit_initialized Then
                    If (id > 0) Then
                        SQL = "UPDATE ccMembers SET " _
                        & " Name=" & cpCore.db.encodeSQLText(name) _
                        & ",username=" & cpCore.db.encodeSQLText(username) _
                        & ",email=" & cpCore.db.encodeSQLText(email) _
                        & ",password=" & cpCore.db.encodeSQLText(password) _
                        & ",OrganizationID=" & cpCore.db.encodeSQLNumber(organizationId) _
                        & ",LanguageID=" & cpCore.db.encodeSQLNumber(languageId) _
                        & ",Active=" & cpCore.db.encodeSQLBoolean(active) _
                        & ",Company=" & cpCore.db.encodeSQLText(company) _
                        & ",Visits=" & cpCore.db.encodeSQLNumber(visits) _
                        & ",LastVisit=" & cpCore.db.encodeSQLDate(lastVisit) _
                        & ",AllowBulkEmail=" & cpCore.db.encodeSQLBoolean(allowBulkEmail) _
                        & ",AdminMenuModeID=" & cpCore.db.encodeSQLNumber(adminMenuModeID) _
                        & ",AutoLogin=" & cpCore.db.encodeSQLBoolean(autoLogin)
                        ' 6/18/2009 - removed notes from base
                        '           & ",SendNotes=" & encodeSQLBoolean(MemberSendNotes)
                        SQL &= "" _
                        & ",BillEmail=" & cpCore.db.encodeSQLText(user_billEmail) _
                        & ",BillPhone=" & cpCore.db.encodeSQLText(user_billPhone) _
                        & ",BillFax=" & cpCore.db.encodeSQLText(user_billFax) _
                        & ",BillCompany=" & cpCore.db.encodeSQLText(main_MemberBillCompany) _
                        & ",BillAddress=" & cpCore.db.encodeSQLText(main_MemberBillAddress) _
                        & ",BillCity=" & cpCore.db.encodeSQLText(main_MemberBillCity) _
                        & ",BillState=" & cpCore.db.encodeSQLText(main_MemberBillState) _
                        & ",BillZip=" & cpCore.db.encodeSQLText(main_MemberBillZip) _
                        & ",BillCountry=" & cpCore.db.encodeSQLText(main_MemberBillCountry)
                        SQL &= "" _
                        & ",ShipName=" & cpCore.db.encodeSQLText(main_MemberShipName) _
                        & ",ShipCompany=" & cpCore.db.encodeSQLText(main_MemberShipCompany) _
                        & ",ShipAddress=" & cpCore.db.encodeSQLText(main_MemberShipAddress) _
                        & ",ShipCity=" & cpCore.db.encodeSQLText(main_MemberShipCity) _
                        & ",ShipState=" & cpCore.db.encodeSQLText(main_MemberShipState) _
                        & ",ShipZip=" & cpCore.db.encodeSQLText(main_MemberShipZip) _
                        & ",ShipCountry=" & cpCore.db.encodeSQLText(main_MemberShipCountry) _
                        & ",ShipPhone=" & cpCore.db.encodeSQLText(main_MemberShipPhone)
                        If True Then
                            SQL &= ",ExcludeFromAnalytics=" & cpCore.db.encodeSQLBoolean(excludeFromAnalytics)
                        End If
                        SQL &= " WHERE ID=" & id & ";"
                        Call cpCore.db.executeSql(SQL)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '=============================================================================
        ''' <summary>
        ''' 
        ''' </summary>
        ''' <returns></returns>
        Public Function user_GetLoginForm() As String
            Dim returnHtml As String = ""
            Try
                '
                Dim loginAddonID As Integer
                Dim isAddonOk As Boolean
                Dim QS As String
                '
                loginAddonID = cpCore.siteProperties.getinteger("Login Page AddonID")
                If loginAddonID <> 0 Then
                    '
                    ' Custom Login
                    '
                    returnHtml = cpCore.executeAddon_legacy2(loginAddonID, "", "", cpCoreClass.addonContextEnum.ContextPage, "", 0, "", "", False, 0, "", isAddonOk, Nothing)
                    If Not isAddonOk Then
                        loginAddonID = 0
                    ElseIf (returnHtml = "") And (isAddonOk) Then
                        '
                        ' login successful, redirect back to this page (without a method)
                        '
                        QS = cpCore.web_RefreshQueryString
                        QS = ModifyQueryString(QS, "method", "")
                        QS = ModifyQueryString(QS, "RequestBinary", "")
                        '
                        Call cpCore.web_Redirect2("?" & QS, "Login form success", False)
                    End If
                End If
                If loginAddonID = 0 Then
                    '
                    ' ----- When page loads, set focus on login username
                    '
                    returnHtml = user_GetLoginForm_Default()
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnHtml
        End Function
        '
        '=============================================================================
        ''' <summary>
        ''' a simple email password form
        ''' </summary>
        ''' <returns></returns>
        Public Function user_GetSendPasswordForm() As String
            Dim returnResult As String = ""
            Try
                Dim QueryString As String
                '
                If cpCore.siteProperties.getBoolean("allowPasswordEmail", True) Then
                    returnResult = "" _
                    & cr & "<table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">" _
                    & cr2 & "<tr>" _
                    & cr3 & "<td style=""text-align:right;vertical-align:middle;width:30%;padding:4px"" align=""right"" width=""30%"">" & SpanClassAdminNormal & "Email</span></td>" _
                    & cr3 & "<td style=""text-align:left;vertical-align:middle;width:70%;padding:4px"" align=""left""  width=""70%""><input NAME=""" & "email"" VALUE=""" & html_EncodeHTML(loginForm_Email) & """ SIZE=""20"" MAXLENGTH=""50""></td>" _
                    & cr2 & "</tr>" _
                    & cr2 & "<tr>" _
                    & cr3 & "<td colspan=""2"">&nbsp;</td>" _
                    & cr2 & "</tr>" _
                    & cr2 & "<tr>" _
                    & cr3 & "<td colspan=""2"">" _
                    & kmaIndent(kmaIndent(cpCore.main_GetPanelButtons(ButtonSendPassword, "Button"))) _
                    & cr3 & "</td>" _
                    & cr2 & "</tr>" _
                    & cr & "</table>" _
                    & ""
                    '
                    ' write out all of the form input (except state) to hidden fields so they can be read after login
                    '
                    '
                    returnResult = "" _
                    & returnResult _
                    & cpCore.html_GetFormInputHidden("Type", FormTypeSendPassword) _
                    & ""
                    For Each kvp As KeyValuePair(Of String, docPropertiesClass) In cpCore.docProperties.docPropertiesDict
                        With kvp.Value
                            If .IsForm Then
                                Select Case UCase(.Name)
                                    Case "S", "MA", "MB", "USERNAME", "PASSWORD", "EMAIL"
                                    Case Else
                                        returnResult = returnResult & cpCore.html_GetFormInputHidden(.Name, .Value)
                                End Select
                            End If
                        End With
                    Next
                    '
                    QueryString = cpCore.web_RefreshQueryString
                    QueryString = ModifyQueryString(QueryString, "S", "")
                    QueryString = ModifyQueryString(QueryString, "ccIPage", "")
                    returnResult = "" _
                    & cpCore.html_GetFormStart(QueryString) _
                    & kmaIndent(returnResult) _
                    & cr & "</form>" _
                    & ""
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnResult
        End Function
        '
        '===============================================================================================================================
        '   Is Group Member of a GroupIDList
        '   admins are always returned true
        '===============================================================================================================================
        '
        Public Function user_isMemberOfGroupIdList(ByVal MemberID As Integer, ByVal isAuthenticated As Boolean, ByVal GroupIDList As String) As Boolean
            user_isMemberOfGroupIdList = user_isMemberOfGroupIdList2(MemberID, isAuthenticated, GroupIDList, True)
        End Function
        '
        '===============================================================================================================================
        '   Is Group Member of a GroupIDList
        '===============================================================================================================================
        '
        Public Function user_isMemberOfGroupIdList2(ByVal MemberID As Integer, ByVal isAuthenticated As Boolean, ByVal GroupIDList As String, ByVal adminReturnsTrue As Boolean) As Boolean
            Dim returnREsult As Boolean = False
            Try
                '
                Dim CS As Integer
                Dim SQL As String
                Dim Criteria As String
                Dim WorkingIDList As String
                '
                returnREsult = False
                If isAuthenticated Then
                    WorkingIDList = GroupIDList
                    WorkingIDList = Replace(WorkingIDList, " ", "")
                    Do While InStr(1, WorkingIDList, ",,") <> 0
                        WorkingIDList = Replace(WorkingIDList, ",,", ",")
                    Loop
                    If (WorkingIDList <> "") Then
                        If Left(WorkingIDList, 1) = "," Then
                            If Len(WorkingIDList) <= 1 Then
                                WorkingIDList = ""
                            Else
                                WorkingIDList = Mid(WorkingIDList, 2)
                            End If
                        End If
                    End If
                    If (WorkingIDList <> "") Then
                        If Right(WorkingIDList, 1) = "," Then
                            If Len(WorkingIDList) <= 1 Then
                                WorkingIDList = ""
                            Else
                                WorkingIDList = Mid(WorkingIDList, 1, Len(WorkingIDList) - 1)
                            End If
                        End If
                    End If
                    If (WorkingIDList = "") Then
                        If adminReturnsTrue Then
                            '
                            ' check if memberid is admin
                            '
                            SQL = "select top 1 m.id" _
                            & " from ccmembers m" _
                            & " where" _
                            & " (m.id=" & MemberID & ")" _
                            & " and(m.active<>0)" _
                            & " and(" _
                                & " (m.admin<>0)" _
                                & " or(m.developer<>0)" _
                            & " )" _
                            & " "
                            CS = cpCore.db.db_openCsSql_rev("default", SQL)
                            returnREsult = cpCore.db.cs_Ok(CS)
                            Call cpCore.db.cs_Close(CS)
                        End If
                    Else
                        '
                        ' check if they are admin or in the group list
                        '
                        If InStr(1, WorkingIDList, ",") <> 0 Then
                            Criteria = "r.GroupID in (" & WorkingIDList & ")"
                        Else
                            Criteria = "r.GroupID=" & WorkingIDList
                        End If
                        Criteria = "" _
                        & "(" & Criteria & ")" _
                        & " and(r.id is not null)" _
                        & " and((r.DateExpires is null)or(r.DateExpires>" & cpCore.db.encodeSQLDate(Now) & "))" _
                        & " "
                        If adminReturnsTrue Then
                            Criteria = "(" & Criteria & ")or(m.admin<>0)or(m.developer<>0)"
                        End If
                        Criteria = "" _
                        & "(" & Criteria & ")" _
                        & " and(m.active<>0)" _
                        & " and(m.id=" & MemberID & ")" _
                        '
                        SQL = "select top 1 m.id" _
                        & " from ccmembers m" _
                        & " left join ccMemberRules r on r.Memberid=m.id" _
                        & " where" & Criteria
                        CS = cpCore.db.db_openCsSql_rev("default", SQL)
                        returnREsult = cpCore.db.cs_Ok(CS)
                        Call cpCore.db.cs_Close(CS)
                    End If
                End If

            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function
        '
        '========================================================================
        ' Member Open
        '   Attempts to open the Member record based on the iRecordID
        '   If successful, MemberID is set to the iRecordID
        '========================================================================
        '
        Public Function initializeUser(recordId As Integer) As Integer
            Dim returnRecordId As Integer = 0
            Try
                Dim CS As Integer
                '
                If recordId <> 0 Then
                    '
                    ' attempt to read in Member record if logged on
                    ' dont just do main_CheckMember() -- in case a pretty login is needed
                    '
                    CS = cpCore.db_csOpenRecord("People", recordId)
                    If cpCore.db.cs_Ok(CS) Then
                        name = (cpCore.db.cs_getText(CS, "Name"))
                        isDeveloper = (cpCore.db.cs_getBoolean(CS, "Developer"))
                        isAdmin = (cpCore.db.cs_getBoolean(CS, "Admin"))
                        contentControlID = (cpCore.db.cs_getInteger(CS, "ContentControlID"))
                        organizationId = (cpCore.db.cs_getInteger(CS, "OrganizationID"))
                        languageId = (cpCore.db.cs_getInteger(CS, "LanguageID"))
                        language = (cpCore.main_GetCSEncodedField(CS, "LanguageID"))
                        '
                        main_MemberShipName = (cpCore.db.cs_getText(CS, "ShipName"))
                        main_MemberShipCompany = (cpCore.db.cs_getText(CS, "ShipCompany"))
                        main_MemberShipAddress = (cpCore.db.cs_getText(CS, "ShipAddress"))
                        main_MemberShipCity = (cpCore.db.cs_getText(CS, "ShipCity"))
                        main_MemberShipState = (cpCore.db.cs_getText(CS, "ShipState"))
                        main_MemberShipZip = (cpCore.db.cs_getText(CS, "ShipZip"))
                        main_MemberShipCountry = (cpCore.db.cs_getText(CS, "ShipCountry"))
                        main_MemberShipPhone = (cpCore.db.cs_getText(CS, "ShipPhone"))
                        '
                        main_MemberBillCompany = (cpCore.db.cs_getText(CS, "BillCompany"))
                        main_MemberBillAddress = (cpCore.db.cs_getText(CS, "BillAddress"))
                        main_MemberBillCity = (cpCore.db.cs_getText(CS, "BillCity"))
                        main_MemberBillState = (cpCore.db.cs_getText(CS, "BillState"))
                        main_MemberBillZip = (cpCore.db.cs_getText(CS, "BillZip"))
                        main_MemberBillCountry = (cpCore.db.cs_getText(CS, "BillCountry"))
                        user_billEmail = (cpCore.db.cs_getText(CS, "BillEmail"))
                        user_billPhone = (cpCore.db.cs_getText(CS, "BillPhone"))
                        user_billFax = (cpCore.db.cs_getText(CS, "BillFax"))
                        '
                        allowBulkEmail = (cpCore.db.cs_getBoolean(CS, "AllowBulkEmail"))
                        allowToolsPanel = (cpCore.db.cs_getBoolean(CS, "AllowToolsPanel"))
                        adminMenuModeID = (cpCore.db.cs_getInteger(CS, "AdminMenuModeID"))
                        autoLogin = (cpCore.db.cs_getBoolean(CS, "AutoLogin"))
                        '
                        styleFilename = cpCore.db.cs_getText(CS, "StyleFilename")
                        If styleFilename <> "" Then
                            Call cpCore.main_AddStylesheetLink(cpCore.web_requestProtocol & cpCore.webServer.requestDomain & cpCore.csv_getVirtualFileLink(cpCore.appConfig.cdnFilesNetprefix, styleFilename))
                        End If
                        excludeFromAnalytics = cpCore.db.cs_getBoolean(CS, "ExcludeFromAnalytics")
                        returnRecordId = recordId
                    End If
                    Call cpCore.db.cs_Close(CS)
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnRecordId
        End Function
        '
        '========================================================================
        ' ----- Returns true if the visitor is an admin, or authenticated and in the group named
        '========================================================================
        '
        Public Function user_IsGroupMember(ByVal GroupName As String, Optional ByVal checkMemberID As Integer = 0) As Boolean
            Dim returnREsult As Boolean = False
            Try
                Dim iMemberID As Integer
                iMemberID = EncodeInteger(checkMemberID)
                If iMemberID = 0 Then
                    iMemberID = id
                End If
                returnREsult = user_IsGroupListMember2("," & cpCore.group_GetGroupID(EncodeText(GroupName)), iMemberID, True)
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function
        '
        '========================================================================
        ' ----- Returns true if the visitor is a member, and in the group named
        '========================================================================
        '
        Public Function user_IsGroupMember2(ByVal GroupName As String, Optional ByVal checkMemberID As Integer = 0, Optional ByVal adminReturnsTrue As Boolean = False) As Boolean
            Dim returnREsult As Boolean = False
            Try
                Dim iMemberID As Integer
                iMemberID = checkMemberID
                If iMemberID = 0 Then
                    iMemberID = id
                End If
                returnREsult = user_IsGroupListMember2("," & cpCore.group_GetGroupID(EncodeText(GroupName)), iMemberID, adminReturnsTrue)
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function

        '
        '========================================================================
        ' ----- Returns true if the visitor is an admin, or authenticated and in the group list
        '========================================================================
        '
        Public Function user_IsGroupListMember2(ByVal GroupIDList As String, Optional ByVal checkMemberID As Integer = 0, Optional ByVal adminReturnsTrue As Boolean = False) As Boolean
            Dim returnREsult As Boolean = False
            Try
                If checkMemberID = 0 Then
                    checkMemberID = id
                End If
                returnREsult = user_isMemberOfGroupIdList2(checkMemberID, isAuthenticated(), GroupIDList, adminReturnsTrue)
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function
        '
        '========================================================================
        '   IsMember
        '   true if the user is authenticated and is a trusted people (member content)
        '========================================================================
        '
        Public Function user_isAuthenticatedMember() As Boolean
            Dim returnREsult As Boolean = False
            Try
                If (Not property_user_isMember_isLoaded) And (cpCore.visit_initialized) Then
                    property_user_isMember = isAuthenticated() And cpCore.db_IsWithinContent(contentControlID, cpCore.main_GetContentID("members"))
                    property_user_isMember_isLoaded = True
                End If
                returnREsult = property_user_isMember
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function
        Private property_user_isMember As Boolean = False
        Private property_user_isMember_isLoaded As Boolean = False
        '
        '========================================================================
        ' ----- Process the login form
        '========================================================================
        '
        Friend Function user_ProcessLoginFormDefault() As Boolean
            Dim returnREsult As Boolean = False
            Try
                Dim LocalMemberID As Integer
                returnREsult = False
                '
                If Not main_loginFormDefaultProcessed Then
                    '
                    ' Processing can happen
                    '   1) early in init() -- legacy
                    '   2) as well as at the front of main_GetLoginForm - to support addon Login forms
                    ' This flag prevents the default form from processing twice
                    '
                    main_loginFormDefaultProcessed = True
                    loginForm_Username = cpCore.docProperties.getText("username")
                    loginForm_Password = cpCore.docProperties.getText("password")
                    loginForm_AutoLogin = cpCore.main_GetStreamBoolean2("autologin")
                    '
                    If (cpCore.visit_loginAttempts < main_maxVisitLoginAttempts) And (cpCore.visit_cookieSupport) Then
                        LocalMemberID = user_getLoginUserID(loginForm_Username, loginForm_Password)
                        If LocalMemberID = 0 Then
                            cpCore.visit_loginAttempts = cpCore.visit_loginAttempts + 1
                            Call cpCore.visit_save()
                        Else
                            returnREsult = authenticateByID(LocalMemberID, loginForm_AutoLogin)
                            If returnREsult Then
                                Call cpCore.main_LogActivity2("successful username/password login", id, organizationId)
                            Else
                                Call cpCore.main_LogActivity2("bad username/password login", id, organizationId)
                            End If
                        End If
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function
        '
        '========================================================================
        ' ----- Process the send password form
        '========================================================================
        '
        Public Sub user_ProcessFormSendPassword()
            Try
                loginForm_Email = cpCore.docProperties.getText("email")
                Call security_SendMemberPassword(loginForm_Email)
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '=============================================================================
        ' Send the Member his username and password
        '=============================================================================
        '
        Public Function security_SendMemberPassword(ByVal Email As Object) As Boolean
            Dim returnREsult As Boolean = False
            Try
                Dim sqlCriteria As String
                Dim Message As String
                Dim CS As Integer
                Dim MethodName As String
                Dim workingEmail As String
                Dim FromAddress As String
                Dim subject As String
                Dim allowEmailLogin As Boolean
                Dim Password As String
                Dim Username As String
                Dim updateUser As Boolean
                Dim atPtr As Integer
                Dim Cnt As Integer
                Dim Index As Integer
                Dim EMailName As String
                Dim usernameOK As Boolean
                Dim recordCnt As Integer
                Dim hint As String
                Dim Ptr As Integer
                '
                Const passwordChrs = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ012345678999999"
                Const passwordChrsLength = 62
                '
                'hint = "100"
                workingEmail = EncodeText(Email)
                '
                returnREsult = False
                If workingEmail = "" Then
                    'hint = "110"
                    cpCore.error_AddUserError("Please enter your email address before requesting your username and password.")
                Else
                    'hint = "120"
                    atPtr = InStr(1, workingEmail, "@")
                    If atPtr < 2 Then
                        '
                        ' email not valid
                        '
                        'hint = "130"
                        cpCore.error_AddUserError("Please enter a valid email address before requesting your username and password.")
                    Else
                        'hint = "140"
                        EMailName = Mid(workingEmail, 1, atPtr - 1)
                        '
                        Call cpCore.main_LogActivity2("password request for email " & workingEmail, id, organizationId)
                        '
                        allowEmailLogin = cpCore.siteProperties.getBoolean("allowEmailLogin", False)
                        recordCnt = 0
                        sqlCriteria = "(email=" & cpCore.db.encodeSQLText(workingEmail) & ")"
                        If True Then
                            sqlCriteria = sqlCriteria & "and((dateExpires is null)or(dateExpires>" & cpCore.db.encodeSQLDate(Now) & "))"
                        End If
                        CS = cpCore.db.csOpen("People", sqlCriteria, "ID", , , ,, "username,password", 1)
                        If Not cpCore.db.cs_Ok(CS) Then
                            '
                            ' valid login account for this email not found
                            '
                            If (LCase(Mid(workingEmail, atPtr + 1)) = "contensive.com") Then
                                '
                                ' look for expired account to renew
                                '
                                Call cpCore.db.cs_Close(CS)
                                CS = cpCore.db.csOpen("People", "((email=" & cpCore.db.encodeSQLText(workingEmail) & "))", "ID", , , , , , 1)
                                If cpCore.db.cs_Ok(CS) Then
                                    '
                                    ' renew this old record
                                    '
                                    'hint = "150"
                                    Call cpCore.db.db_SetCSField(CS, "developer", "1")
                                    Call cpCore.db.db_SetCSField(CS, "admin", "1")
                                    Call cpCore.db.db_SetCSField(CS, "dateExpires", Now.AddDays(7).Date.ToString)
                                Else
                                    '
                                    ' inject support record
                                    '
                                    'hint = "150"
                                    Call cpCore.db.cs_Close(CS)
                                    CS = cpCore.db.cs_insertRecord("people")
                                    Call cpCore.db.db_SetCSField(CS, "name", "Contensive Support")
                                    Call cpCore.db.db_SetCSField(CS, "email", workingEmail)
                                    Call cpCore.db.db_SetCSField(CS, "developer", "1")
                                    Call cpCore.db.db_SetCSField(CS, "admin", "1")
                                    Call cpCore.db.db_SetCSField(CS, "dateExpires", Now.AddDays(7).Date.ToString)
                                End If
                                Call cpCore.db.db_SaveCSRecord(CS)
                            Else
                                'hint = "155"
                                cpCore.error_AddUserError("No current user was found matching this email address. Please try again. ")
                            End If
                        End If
                        If cpCore.db.cs_Ok(CS) Then
                            'hint = "160"
                            FromAddress = cpCore.siteProperties.getText("EmailFromAddress", "info@" & cpCore.main_ServerDomain)
                            subject = "Password Request at " & cpCore.main_ServerDomain
                            Message = ""
                            Do While cpCore.db.cs_Ok(CS)
                                'hint = "170"
                                updateUser = False
                                If Message = "" Then
                                    'hint = "180"
                                    Message = "This email was sent in reply to a request at " & cpCore.main_ServerDomain & " for the username and password associated with this email address. "
                                    Message = Message & "If this request was made by you, please return to the login screen and use the following:" & vbCrLf
                                    Message = Message & vbCrLf
                                Else
                                    'hint = "190"
                                    Message = Message & vbCrLf
                                    Message = Message & "Additional user accounts with the same email address: " & vbCrLf
                                End If
                                '
                                ' username
                                '
                                'hint = "200"
                                Username = cpCore.db.cs_getText(CS, "Username")
                                usernameOK = True
                                If Not allowEmailLogin Then
                                    'hint = "210"
                                    If Username <> Trim(Username) Then
                                        'hint = "220"
                                        Username = Trim(Username)
                                        updateUser = True
                                    End If
                                    If Username = "" Then
                                        'hint = "230"
                                        'username = emailName & Int(Rnd() * 9999)
                                        usernameOK = False
                                        Ptr = 0
                                        Do While Not usernameOK And (Ptr < 100)
                                            'hint = "240"
                                            Username = EMailName & Int(Rnd() * 9999)
                                            usernameOK = Not cpCore.main_IsLoginOK(Username, "test")
                                            Ptr = Ptr + 1
                                        Loop
                                        'hint = "250"
                                        If usernameOK Then
                                            updateUser = True
                                        End If
                                    End If
                                    'hint = "260"
                                    Message = Message & " username: " & Username & vbCrLf
                                End If
                                'hint = "270"
                                If usernameOK Then
                                    '
                                    ' password
                                    '
                                    'hint = "280"
                                    Password = cpCore.db.cs_getText(CS, "Password")
                                    If Trim(Password) <> Password Then
                                        'hint = "290"
                                        Password = Trim(Password)
                                        updateUser = True
                                    End If
                                    'hint = "300"
                                    If Password = "" Then
                                        'hint = "310"
                                        For Ptr = 0 To 8
                                            'hint = "320"
                                            Index = CInt(Rnd() * passwordChrsLength)
                                            Password = Password & Mid(passwordChrs, Index, 1)
                                        Next
                                        'hint = "330"
                                        updateUser = True
                                    End If
                                    'hint = "340"
                                    Message = Message & " password: " & Password & vbCrLf
                                    returnREsult = True
                                    If updateUser Then
                                        'hint = "350"
                                        Call cpCore.db.cs_set(CS, "username", Username)
                                        Call cpCore.db.cs_set(CS, "password", Password)
                                    End If
                                    recordCnt = recordCnt + 1
                                End If
                                cpCore.db.db_csGoNext(CS)
                            Loop
                        End If
                    End If
                End If
                'hint = "360"
                If returnREsult Then
                    Call cpCore.main_SendEmail(workingEmail, FromAddress, subject, Message, , True, False)
                    '    main_ClosePageHTML = main_ClosePageHTML & main_GetPopupMessage(app.publicFiles.ReadFile("ccLib\Popup\PasswordSent.htm"), 300, 300, "no")
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function

        Public Sub New(cpCore As cpCoreClass)
            MyBase.New()
            Me.cpCore = cpCore
        End Sub
        '
        '========================================================================
        ' main_IsContentManager2
        '   If ContentName is missing, returns true if this is an authenticated member with
        '       content management over anything
        '   If ContentName is given, it only tests this content
        '========================================================================
        '
        Public Function isAuthenticatedContentManager(Optional ByVal ContentName As String = "") As Boolean
            Dim returnIsContentManager = False
            Try
                Dim SQL As String
                Dim CS As Integer
                Dim notImplemented_allowAdd As Boolean
                Dim notImplemented_allowDelete As Boolean
                '
                ' REFACTOR -- add a private dictionary with contentname=>result, plus a authenticationChange flag that makes properties like this invalid
                '
                returnIsContentManager = False
                If String.IsNullOrEmpty(ContentName) Then
                    If isAuthenticated() Then
                        If isAuthenticatedAdmin() Then
                            returnIsContentManager = True
                        Else
                            '
                            ' Is a CM for any content def
                            '
                            If (Not _isAuthenticatedContentManagerAnything_loaded) Or (_isAuthenticatedContentManagerAnything_userId <> id) Then
                                SQL = "SELECT ccGroupRules.ContentID" _
                                    & " FROM ccGroupRules RIGHT JOIN ccMemberRules ON ccGroupRules.GroupID = ccMemberRules.GroupID" _
                                    & " WHERE (" _
                                        & "(ccMemberRules.MemberID=" & cpCore.db.encodeSQLNumber(id) & ")" _
                                        & " AND(ccMemberRules.active<>0)" _
                                        & " AND(ccGroupRules.active<>0)" _
                                        & " AND(ccGroupRules.ContentID Is not Null)" _
                                        & " AND((ccMemberRules.DateExpires is null)OR(ccMemberRules.DateExpires>" & cpCore.db.encodeSQLDate(cpCore.main_PageStartTime) & "))" _
                                        & ")"
                                CS = cpCore.db.cs_openSql(SQL)
                                _isAuthenticatedContentManagerAnything = cpCore.db.cs_Ok(CS)
                                cpCore.db.cs_Close(CS)
                                '
                                _isAuthenticatedContentManagerAnything_userId = id
                                _isAuthenticatedContentManagerAnything_loaded = True
                            End If
                            returnIsContentManager = _isAuthenticatedContentManagerAnything
                        End If
                    End If
                Else
                    '
                    ' Specific Content called out
                    '
                    Call cpCore.user.getContentAccessRights(ContentName, returnIsContentManager, notImplemented_allowAdd, notImplemented_allowDelete)
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnIsContentManager
        End Function
        Private _isAuthenticatedContentManagerAnything_loaded As Boolean = False
        Private _isAuthenticatedContentManagerAnything_userId As Integer
        Private _isAuthenticatedContentManagerAnything As Boolean
        '
        '========================================================================
        ' Member Login (by username and password)
        '
        '   See main_GetLoginMemberID and main_LoginMemberByID
        '========================================================================
        '
        Public Function authenticate(ByVal loginFieldValue As String, ByVal password As String, Optional ByVal AllowAutoLogin As Boolean = False) As Boolean
            Dim returnREsult As Boolean = False
            Try
                Dim LocalMemberID As Integer
                '
                returnREsult = False
                LocalMemberID = user_getLoginUserID(loginFieldValue, password)
                If LocalMemberID <> 0 Then
                    returnREsult = authenticateByID(LocalMemberID, AllowAutoLogin)
                    If returnREsult Then
                        Call cpCore.main_LogActivity2("successful password login", id, organizationId)
                        isAuthenticatedAdmin_cache_isLoaded = False
                        property_user_isMember_isLoaded = False
                        property_user_isAuthenticated_isLoaded = False
                    Else
                        Call cpCore.main_LogActivity2("unsuccessful login (loginField:" & loginFieldValue & "/password:" & password & ")", id, organizationId)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function
        '
        '========================================================================
        '   Member Login By ID
        '
        '========================================================================
        '
        Public Function authenticateByID(ByVal irecordID As Integer, Optional ByVal AllowAutoLogin As Boolean = False) As Boolean
            Dim returnREsult As Boolean = False
            Try
                Dim CS As Integer
                '
                returnREsult = recognizeByID(irecordID)
                If returnREsult Then
                    '
                    ' Log them in
                    '
                    cpCore.visit_isAuthenticated = True
                    Call cpCore.visit_save()
                    isAuthenticatedAdmin_cache_isLoaded = False
                    property_user_isMember_isLoaded = False
                    property_user_isAuthenticated_isLoaded = False
                    isAuthenticatedDeveloper_cache_isLoaded = False
                    '
                    ' Write Cookies in case Visit Tracking is off
                    '
                    If cpCore.visit_startTime = Date.MinValue Then
                        cpCore.visit_startTime = cpCore.main_PageStartTime
                    End If
                    If Not cpCore.siteProperties.getBoolean("allowVisitTracking", True) Then
                        Call cpCore.visit_init(True)
                    End If
                    '
                    ' Change autologin if included, selected, and allowed
                    '
                    If AllowAutoLogin Xor autoLogin Then
                        If EncodeBoolean(cpCore.siteProperties.getBoolean("AllowAutoLogin", False)) Then
                            CS = cpCore.db_csOpenRecord("people", irecordID)
                            If cpCore.db.cs_Ok(CS) Then
                                Call cpCore.db.cs_set(CS, "AutoLogin", AllowAutoLogin)
                            End If
                            Call cpCore.db.cs_Close(CS)
                        End If
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function
        ''
        ''========================================================================
        ''   main_SetMemberIdentity
        ''
        ''   See RecognizeMember for details
        ''========================================================================
        ''
        'Public Function user_SetMemberIdentity(Optional ByVal Criteria As String = "") As Boolean
        '    Dim returnREsult As Boolean = False
        '    Try
        '        Dim CS As Integer
        '        '
        '        returnREsult = False
        '        CS = cpCore.db.csOpen("people", Criteria, , , , , , "ID")
        '        If cpCore.db.cs_Ok(CS) Then
        '            returnREsult = user_RecognizeMemberByID(cpCore.db.cs_getInteger(CS, "ID"))
        '        End If
        '    Catch ex As Exception
        '        cpCore.handleExceptionAndRethrow(ex)
        '    End Try
        '    Return returnREsult
        'End Function
        '
        '========================================================================
        '   RecognizeMember
        '
        '   the current member to be non-authenticated, but recognized
        '========================================================================
        '
        Public Function recognizeByID(ByVal RecordID As Integer) As Boolean
            Dim returnREsult As Boolean = False
            Try
                Dim CS As Integer
                Dim SQL As String
                '
                SQL = "select" _
                    & " ccMembers.*" _
                    & " ,ccLanguages.name as LanguageName" _
                    & " from" _
                    & " ccMembers" _
                    & " left join ccLanguages on ccMembers.LanguageID=ccLanguages.ID" _
                    & " where" _
                    & " (ccMembers.active<>" & SQLFalse & ")" _
                    & " and(ccMembers.ID=" & RecordID & ")"
                SQL &= "" _
                    & " and((ccMembers.dateExpires is null)or(ccMembers.dateExpires>" & cpCore.db.encodeSQLDate(Now) & "))" _
                    & ""
                CS = cpCore.db.cs_openSql(SQL)
                If cpCore.db.cs_Ok(CS) Then
                    If cpCore.visit_Id = 0 Then
                        '
                        ' Visit was blocked during init, init the visit now
                        '
                        Call cpCore.visit_init(True)
                    End If
                    '
                    ' ----- Member was recognized
                    '   REFACTOR -- when the id is set, the user object is populated, so the rest of this can be removed (verify these are all set in the load
                    '
                    id = (cpCore.db.cs_getInteger(CS, "ID"))
                    name = (cpCore.db.cs_getText(CS, "Name"))
                    username = (cpCore.db.cs_getText(CS, "username"))
                    email = (cpCore.db.cs_getText(CS, "Email"))
                    password = (cpCore.db.cs_getText(CS, "Password"))
                    organizationId = (cpCore.db.cs_getInteger(CS, "OrganizationID"))
                    languageId = (cpCore.db.cs_getInteger(CS, "LanguageID"))
                    active = (cpCore.db.cs_getBoolean(CS, "Active"))
                    company = (cpCore.db.cs_getText(CS, "Company"))
                    visits = (cpCore.db.cs_getInteger(CS, "Visits"))
                    lastVisit = (cpCore.db.db_GetCSDate(CS, "LastVisit"))
                    allowBulkEmail = (cpCore.db.cs_getBoolean(CS, "AllowBulkEmail"))
                    allowToolsPanel = (cpCore.db.cs_getBoolean(CS, "AllowToolsPanel"))
                    adminMenuModeID = (cpCore.db.cs_getInteger(CS, "AdminMenuModeID"))
                    autoLogin = (cpCore.db.cs_getBoolean(CS, "AutoLogin"))
                    isDeveloper = (cpCore.db.cs_getBoolean(CS, "Developer"))
                    isAdmin = (cpCore.db.cs_getBoolean(CS, "Admin"))
                    contentControlID = (cpCore.db.cs_getInteger(CS, "ContentControlID"))
                    languageId = (cpCore.db.cs_getInteger(CS, "LanguageID"))
                    language = (cpCore.db.cs_getText(CS, "LanguageName"))
                    styleFilename = cpCore.db.cs_getText(CS, "StyleFilename")
                    If styleFilename <> "" Then
                        Call cpCore.main_AddStylesheetLink(cpCore.web_requestProtocol & cpCore.webServer.requestDomain & cpCore.csv_getVirtualFileLink(cpCore.appConfig.cdnFilesNetprefix, styleFilename))
                    End If
                    excludeFromAnalytics = cpCore.db.cs_getBoolean(CS, "ExcludeFromAnalytics")
                    '
                    visits = visits + 1
                    If visits = 1 Then
                        isNew = True
                    Else
                        isNew = False
                    End If
                    lastVisit = cpCore.main_PageStartTime
                    'cpCore.main_VisitMemberID = id
                    cpCore.visit_loginAttempts = 0
                    cpCore.visitor_memberID = id
                    cpCore.visit_excludeFromAnalytics = cpCore.visit_excludeFromAnalytics Or cpCore.visit_isBot Or excludeFromAnalytics Or isAdmin Or isDeveloper
                    Call cpCore.visit_save()
                    Call cpCore.main_SaveVisitor()
                    Call user_SaveMemberBase()
                    returnREsult = True
                End If
                Call cpCore.db.cs_Close(CS)
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function
        '
        '========================================================================
        ' ----- Create a new default user and save it
        '       If failure, MemberID is 0
        '       If successful, main_VisitMemberID and main_VisitorMemberID must be set to MemberID
        '========================================================================
        '
        Public Sub user_CreateUser()
            Try
                Dim CSMember As Integer
                Dim CSlanguage As Integer
                '
                Call user_CreateUserDefaults(cpCore.visit_name)
                '
                id = 0
                CSMember = cpCore.db.cs_insertRecord("people")
                If Not cpCore.db.cs_Ok(CSMember) Then
                    Call cpCore.handleExceptionAndRethrow(New ApplicationException("main_CreateUser, Error inserting new people record, could not main_CreateUser"))
                Else
                    id = cpCore.db.cs_getInteger(CSMember, "id")
                    Call cpCore.db.cs_set(CSMember, "CreatedByVisit", True)
                    '
                    active = True
                    Call cpCore.db.cs_set(CSMember, "active", active)
                    '
                    visits = 1
                    Call cpCore.db.cs_set(CSMember, "Visits", visits)
                    '
                    lastVisit = cpCore.main_PageStartTime
                    Call cpCore.db.cs_set(CSMember, "LastVisit", lastVisit)
                    '
                    '
                    CSlanguage = cpCore.db_csOpenRecord("Languages", cpCore.web_GetBrowserLanguageID, , , "Name")
                    If cpCore.db.cs_Ok(CSlanguage) Then
                        languageId = cpCore.db.cs_getInteger(CSlanguage, "ID")
                        language = cpCore.db.cs_getText(CSlanguage, "Name")
                        Call cpCore.db.cs_set(CSMember, "LanguageID", languageId)
                    End If
                    Call cpCore.db.cs_Close(CSlanguage)
                    '
                    userAdded = True
                    isNew = True
                    styleFilename = ""
                    excludeFromAnalytics = False
                    '
                    Call cpCore.db.cs_Close(CSMember)
                    '
                    'cpCore.main_VisitMemberID = id
                    cpCore.visitor_memberID = id
                    cpCore.visit_isAuthenticated = False
                    Call cpCore.visit_save()
                    Call cpCore.main_SaveVisitor()
                    '
                    isAuthenticatedAdmin_cache_isLoaded = False
                    property_user_isMember_isLoaded = False
                    property_user_isAuthenticated_isLoaded = False
                    isAuthenticatedDeveloper_cache_isLoaded = False
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '========================================================================
        '   Creates the internal records for the user, but does not create
        '   a people record to save them
        '========================================================================
        '
        Friend Sub user_CreateUserDefaults(ByVal DefaultName As String)
            Try
                Dim CSlanguage As Integer
                '
                id = 0
                name = DefaultName
                isAdmin = False
                isDeveloper = False
                organizationId = 0
                languageId = 0
                language = ""
                isNew = False
                email = ""
                allowBulkEmail = False
                allowToolsPanel = False
                autoLogin = False
                adminMenuModeID = 0
                loginForm_Username = ""
                loginForm_Password = ""
                loginForm_Email = ""
                loginForm_AutoLogin = False
                userAdded = False
                username = ""
                password = ""
                contentControlID = 0
                active = False
                visits = 0
                lastVisit = cpCore.visit_startTime
                company = ""
                user_Title = ""
                main_MemberAddress = ""
                main_MemberCity = ""
                main_MemberState = ""
                main_MemberZip = ""
                main_MemberCountry = ""
                main_MemberPhone = ""
                main_MemberFax = ""
                '
                active = True
                '
                visits = 1
                '
                lastVisit = cpCore.main_PageStartTime
                '
                '
                CSlanguage = cpCore.db_csOpenRecord("Languages", cpCore.web_GetBrowserLanguageID, , , "Name")
                If cpCore.db.cs_Ok(CSlanguage) Then
                    languageId = cpCore.db.cs_getInteger(CSlanguage, "ID")
                    language = cpCore.db.cs_getText(CSlanguage, "Name")
                End If
                Call cpCore.db.cs_Close(CSlanguage)
                '
                userAdded = True
                isNew = True
                styleFilename = ""
                excludeFromAnalytics = False
                '
                isAuthenticatedAdmin_cache_isLoaded = False
                property_user_isMember_isLoaded = False
                property_user_isAuthenticated_isLoaded = False
                isAuthenticatedDeveloper_cache_isLoaded = False
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '=============================================================================
        '   main_SaveMemberBase()
        '       Saves the current Member record to the database
        '=============================================================================
        '
        Public Sub user_SaveMemberBase()
            Try
                Dim SQL As String
                '
                If cpCore.visit_initialized Then
                    If (id > 0) Then
                        SQL = "UPDATE ccMembers SET " _
                        & " Name=" & cpCore.db.encodeSQLText(name) _
                        & ",username=" & cpCore.db.encodeSQLText(username) _
                        & ",email=" & cpCore.db.encodeSQLText(email) _
                        & ",password=" & cpCore.db.encodeSQLText(password) _
                        & ",OrganizationID=" & cpCore.db.encodeSQLNumber(organizationId) _
                        & ",LanguageID=" & cpCore.db.encodeSQLNumber(languageId) _
                        & ",Active=" & cpCore.db.encodeSQLBoolean(active) _
                        & ",Company=" & cpCore.db.encodeSQLText(company) _
                        & ",Visits=" & cpCore.db.encodeSQLNumber(visits) _
                        & ",LastVisit=" & cpCore.db.encodeSQLDate(lastVisit) _
                        & ",AllowBulkEmail=" & cpCore.db.encodeSQLBoolean(allowBulkEmail) _
                        & ",AllowToolsPanel=" & cpCore.db.encodeSQLBoolean(allowToolsPanel) _
                        & ",AdminMenuModeID=" & cpCore.db.encodeSQLNumber(adminMenuModeID) _
                        & ",AutoLogin=" & cpCore.db.encodeSQLBoolean(autoLogin)
                        SQL &= ",ExcludeFromAnalytics=" & cpCore.db.encodeSQLBoolean(excludeFromAnalytics)
                        SQL &= " WHERE ID=" & id & ";"
                        Call cpCore.db.executeSql(SQL)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '========================================================================
        ' Member Logout
        '   Create and assign a guest Member identity
        '========================================================================
        '
        Public Sub security_LogoutMember()
            Try
                Dim CS As Integer
                '
                Call cpCore.main_LogActivity2("logout", id, organizationId)
                '
                ' Clear MemberID for this page
                '
                Call user_CreateUser()
                '
                ' Clear cached permissions
                '
                isAuthenticatedAdmin_cache_isLoaded = False              ' true if main_IsAdminCache is initialized
                property_user_isMember_isLoaded = False
                property_user_isAuthenticated_isLoaded = False
                isAuthenticatedDeveloper_cache_isLoaded = False
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '========================================================================
        ' ----- Process the send password form
        '========================================================================
        '
        Public Sub user_ProcessFormJoin()
            Try
                Dim ErrorMessage As String = ""
                Dim CS As Integer
                Dim FirstName As String
                Dim LastName As String
                Dim FullName As String
                Dim Email As String
                '
                loginForm_Username = cpCore.docProperties.getText("username")
                loginForm_Password = cpCore.docProperties.getText("password")
                '
                If Not EncodeBoolean(cpCore.siteProperties.getBoolean("AllowMemberJoin", False)) Then
                    cpCore.error_AddUserError("This site does not accept public main_MemberShip.")
                Else
                    If Not main_IsNewLoginOK(loginForm_Username, loginForm_Password, ErrorMessage) Then
                        Call cpCore.error_AddUserError(ErrorMessage)
                    Else
                        If Not cpCore.error_IsUserError() Then
                            CS = cpCore.db.csOpen("people", "ID=" & cpCore.db.encodeSQLNumber(cpCore.user.id))
                            If Not cpCore.db.cs_Ok(CS) Then
                                cpCore.handleExceptionAndRethrow(New Exception("Could not open the current members account to set the username and password."))
                            Else
                                If (cpCore.db.cs_getText(CS, "username") <> "") Or (cpCore.db.cs_getText(CS, "password") <> "") Or (cpCore.db.cs_getBoolean(CS, "admin")) Or (cpCore.db.cs_getBoolean(CS, "developer")) Then
                                    '
                                    ' if the current account can be logged into, you can not join 'into' it
                                    '
                                    Call security_LogoutMember()
                                End If
                                FirstName = cpCore.docProperties.getText("firstname")
                                LastName = cpCore.docProperties.getText("firstname")
                                FullName = FirstName & " " & LastName
                                Email = cpCore.docProperties.getText("email")
                                Call cpCore.db.cs_set(CS, "FirstName", FirstName)
                                Call cpCore.db.cs_set(CS, "LastName", LastName)
                                Call cpCore.db.cs_set(CS, "Name", FullName)
                                Call cpCore.db.cs_set(CS, "username", loginForm_Username)
                                Call cpCore.db.cs_set(CS, "password", loginForm_Password)
                                Call cpCore.user.authenticateByID(cpCore.user.id)
                            End If
                            Call cpCore.db.cs_Close(CS)
                        End If
                    End If
                End If
                Call cpCore.cache.invalidateTagCommaList("People")
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '========================================================================
        '   Print the login form in an intercept page
        '========================================================================
        '
        Public Function user_GetLoginPage2(forceDefaultLogin As Boolean) As String
            Dim returnREsult As String = ""
            Try
                Dim Body As String
                Dim head As String
                Dim bodyTag As String
                '
                ' ----- Default Login
                '
                If forceDefaultLogin Then
                    Body = user_GetLoginForm_Default()
                Else
                    Body = user_GetLoginForm()
                End If
                Body = "" _
                    & cr & "<p class=""ccAdminNormal"">You are attempting to enter an access controlled area. Continue only if you have authority to enter this area. Information about your visit will be recorded for security purposes.</p>" _
                    & Body _
                    & ""
                '
                Body = "" _
                    & cpCore.main_GetPanel(Body, "ccPanel", "ccPanelHilite", "ccPanelShadow", "400", 15) _
                    & cr & "<p>&nbsp;</p>" _
                    & cr & "<p>&nbsp;</p>" _
                    & cr & "<p style=""text-align:center""><a href=""http://www.Contensive.com"" target=""_blank""><img src=""/ccLib/images/ccLibLogin.GIF"" width=""80"" height=""33"" border=""0"" alt=""Contensive Content Control"" ></A></p>" _
                    & cr & "<p style=""text-align:center"" class=""ccAdminSmall"">The content on this web site is managed and delivered by the Contensive Site Management Server. If you do not have member access, please use your back button to return to the public area.</p>" _
                    & ""
                '
                ' --- create an outer table to hold the form
                '
                Body = "" _
                    & cr & "<div class=""ccCon"" style=""width:400px;margin:100px auto 0 auto;"">" _
                    & kmaIndent(cpCore.main_GetPanelHeader("Login")) _
                    & kmaIndent(Body) _
                    & "</div>"
                '
                Call cpCore.main_SetMetaContent(0, 0)
                Call cpCore.main_AddPagetitle2("Login", "loginPage")
                head = cpCore.main_GetHTMLInternalHead(False)
                If cpCore.pageManager_TemplateBodyTag <> "" Then
                    bodyTag = cpCore.pageManager_TemplateBodyTag
                Else
                    bodyTag = TemplateDefaultBodyTag
                End If
                'Call AppendLog("call main_getEndOfBody, from main_getLoginPage2 ")
                returnREsult = cpCore.main_assembleHtmlDoc(cpCore.main_docType, head, bodyTag, Body & cpCore.main_GetEndOfBody(False, False, False, False))
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function
        '
        '========================================================================
        '   default login form
        '========================================================================
        '
        Friend Function user_GetLoginForm_Default() As String
            Dim returnHtml As String = ""
            Try
                Dim Panel As String
                Dim usernameMsg As String
                Dim QueryString As String
                Dim loginForm As String
                Dim Caption As String
                Dim formType As String
                Dim needLoginForm As Boolean
                '
                ' ----- process the previous form, if login OK, return blank (signal for page refresh)
                '
                needLoginForm = True
                formType = cpCore.docProperties.getText("type")
                If formType = FormTypeLogin Then
                    If user_ProcessLoginFormDefault() Then
                        returnHtml = ""
                        needLoginForm = False
                    End If
                End If
                If needLoginForm Then
                    '
                    ' ----- When page loads, set focus on login username
                    '
                    Call cpCore.web_addRefreshQueryString("method", "")
                    loginForm = ""
                    Call cpCore.main_AddOnLoadJavascript2("document.getElementById('LoginUsernameInput').focus()", "login")
                    '
                    ' ----- Error Messages
                    '
                    If EncodeBoolean(cpCore.siteProperties.getBoolean("allowEmailLogin", False)) Then
                        usernameMsg = "<b>To login, enter your username or email address with your password.</b></p>"
                    Else
                        usernameMsg = "<b>To login, enter your username and password.</b></p>"
                    End If
                    '
                    QueryString = cpCore.webServer.requestQueryString
                    QueryString = ModifyQueryString(QueryString, RequestNameHardCodedPage, "", False)
                    QueryString = ModifyQueryString(QueryString, "requestbinary", "", False)
                    '
                    ' ----- Username
                    '
                    If EncodeBoolean(cpCore.siteProperties.getBoolean("allowEmailLogin", False)) Then
                        Caption = "Username&nbsp;or&nbsp;Email"
                    Else
                        Caption = "Username"
                    End If
                    '
                    loginForm = loginForm _
                    & cr & "<tr>" _
                    & cr2 & "<td style=""text-align:right;vertical-align:middle;width:30%;padding:4px"" align=""right"" width=""30%"">" & SpanClassAdminNormal & Caption & "&nbsp;</span></td>" _
                    & cr2 & "<td style=""text-align:left;vertical-align:middle;width:70%;padding:4px"" align=""left""  width=""70%""><input ID=""LoginUsernameInput"" NAME=""" & "username"" VALUE=""" & html_EncodeHTML(loginForm_Username) & """ SIZE=""20"" MAXLENGTH=""50"" ></td>" _
                    & cr & "</tr>"
                    '
                    ' ----- Password
                    '
                    If EncodeBoolean(cpCore.siteProperties.getBoolean("allowNoPasswordLogin", False)) Then
                        Caption = "Password&nbsp;(optional)"
                    Else
                        Caption = "Password"
                    End If
                    loginForm = loginForm _
                    & cr & "<tr>" _
                    & cr2 & "<td style=""text-align:right;vertical-align:middle;width:30%;padding:4px"" align=""right"">" & SpanClassAdminNormal & Caption & "&nbsp;</span></td>" _
                    & cr2 & "<td style=""text-align:left;vertical-align:middle;width:70%;padding:4px"" align=""left"" ><input NAME=""" & "password"" VALUE="""" SIZE=""20"" MAXLENGTH=""50"" type=""password""></td>" _
                    & cr & "</tr>" _
                    & ""
                    '
                    ' ----- autologin support
                    '
                    If EncodeBoolean(cpCore.siteProperties.getBoolean("AllowAutoLogin", False)) Then
                        loginForm = loginForm _
                        & cr & "<tr>" _
                        & cr2 & "<td align=""right"">&nbsp;</td>" _
                        & cr2 & "<td align=""left"" >" _
                        & cr3 & "<table border=""0"" cellpadding=""5"" cellspacing=""0"" width=""100%"">" _
                        & cr4 & "<tr>" _
                        & cr5 & "<td valign=""top"" width=""20""><input type=""checkbox"" name=""" & "autologin"" value=""ON"" checked></td>" _
                        & cr5 & "<td valign=""top"" width=""100%"">" & SpanClassAdminNormal & "Login automatically from this computer</span></td>" _
                        & cr4 & "</tr>" _
                        & cr3 & "</table>" _
                        & cr2 & "</td>" _
                        & cr & "</tr>"
                    End If
                    loginForm = loginForm _
                        & cr & "<tr>" _
                        & cr2 & "<td colspan=""2"">&nbsp;</td>" _
                        & cr & "</tr>" _
                        & ""
                    loginForm = "" _
                        & cr & "<table border=""0"" cellpadding=""5"" cellspacing=""0"" width=""100%"">" _
                        & kmaIndent(loginForm) _
                        & cr & "</table>" _
                        & ""
                    loginForm = loginForm _
                        & cpCore.html_GetFormInputHidden("Type", FormTypeLogin) _
                        & cpCore.html_GetFormInputHidden("email", loginForm_Email) _
                        & cpCore.main_GetPanelButtons(ButtonLogin, "Button") _
                        & ""
                    loginForm = "" _
                        & cpCore.html_GetFormStart(QueryString) _
                        & kmaIndent(loginForm) _
                        & cr & "</form>" _
                        & ""

                    '-------

                    Panel = "" _
                        & cpCore.error_GetUserError() _
                        & cr & "<p class=""ccAdminNormal"">" & usernameMsg _
                        & loginForm _
                        & ""
                    '
                    ' ----- Password Form
                    '
                    If EncodeBoolean(cpCore.siteProperties.getBoolean("allowPasswordEmail", True)) Then
                        Panel = "" _
                            & Panel _
                            & cr & "<p class=""ccAdminNormal""><b>Forget your password?</b></p>" _
                            & cr & "<p class=""ccAdminNormal"">If you are a member of the system and can not remember your password, enter your email address below and we will email your matching username and password.</p>" _
                            & user_GetSendPasswordForm() _
                            & ""
                    End If
                    '
                    returnHtml = "" _
                        & cr & "<div class=""ccLoginFormCon"">" _
                        & kmaIndent(Panel) _
                        & cr & "</div>" _
                        & ""
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnHtml
        End Function
        '
        '========================================================================
        '   same as main_GetLoginForm
        '========================================================================
        '
        Public Function getLoginPanel() As String
            Return user_GetLoginForm()
        End Function
        '
        '========================================================================
        '   Member Check
        '       Check for visit authentication.
        '       If the visit is not authenticated (logged in with username/password),
        '       block the page with the login form (not a loginpage so there is no <html><body>
        '========================================================================
        '
        Public Sub user_CheckMember()
            Try
                If Not isAuthenticated() Then
                    Call cpCore.writeAltBuffer(user_GetLoginPage2(False))
                    Call cpCore.main_CloseStream()
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '========================================================================
        '   Create Member
        '       creates a new member account
        '       iUsername is required and must be unique
        '       Returns the new member ID if OK.
        '       If failure, returns 0 and adds a user error
        '       iUsername must be included and must be unique
        '       does NOT log the visitor into the new account
        '========================================================================
        '
        Public Function user_CreateMember(ByVal username As String, Optional ByVal password As String = "", Optional ByVal email As String = "") As Integer
            Dim returnREsult As Integer = 0
            Try
                Dim CS As Integer
                Dim CSPointer As Integer
                '
                returnREsult = 0
                If username = "" Then
                    cpCore.error_AddUserError("A Username is required to create a new site member.")
                Else
                    '
                    ' ----- check if the iUsername is in use (con not use main_OpenContent because active tested)
                    '
                    CS = cpCore.db.csOpen("People", "(Username=" & cpCore.db.encodeSQLText(username) & ")", , False)
                    If cpCore.db.cs_Ok(CS) Then
                        '
                        ' ----- iUsername is taken
                        '
                        Call cpCore.db.cs_Close(CS)
                        cpCore.error_AddUserError("This Username is currently in use.")
                    Else
                        Call cpCore.db.cs_Close(CS)
                        '
                        ' ----- Create the new people with whatever you got
                        '
                        Call user_CreateUser()
                        '
                        CSPointer = cpCore.db.csOpen("people", "ID=" & cpCore.db.encodeSQLNumber(id))
                        If cpCore.db.cs_Ok(CSPointer) Then
                            Call cpCore.db.cs_set(CSPointer, "Username", username)
                            Call cpCore.db.cs_set(CSPointer, "password", password)
                            Call cpCore.db.cs_set(CSPointer, "email", email)
                            Call cpCore.db.cs_set(CSPointer, "ContentControlID", cpCore.main_GetContentID("Members"))
                        End If
                        Call cpCore.db.cs_Close(CSPointer)
                        returnREsult = id
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnREsult
        End Function
        '
        '===================================================================================================
        '   Returns the ID of a member given their Username and Password
        '
        '   If the Id can not be found, user errors are added with main_AddUserError and 0 is returned (false)
        '===================================================================================================
        '
        Public Function user_getLoginUserID(ByVal loginFieldValue As String, ByVal Password As String) As Integer
            Dim returnUserId As Integer = 0
            Try
                Const badLoginUserError = "Your login was not successful. Please try again."
                '
                Dim SQL As String
                Dim recordIsAdmin As Boolean
                Dim recordIsDeveloper As Boolean
                Dim Criteria As String
                Dim CS As Integer
                Dim iPassword As String
                Dim iReturnErrorMessage As Boolean
                Dim iReturnErrorCode As Boolean
                Dim iErrorMessage As String
                Dim iErrorCode As Integer
                Dim RecordPassword As String
                Dim RecordID As Integer
                Dim allowEmailLogin As Boolean
                Dim allowNoPasswordLogin As Boolean
                Dim PeopleCID As Integer
                Dim Ptr As Integer
                Dim CDef As coreMetaDataClass.CDefClass
                Dim iLoginFieldValue As String
                '
                iLoginFieldValue = EncodeText(loginFieldValue)
                iPassword = EncodeText(Password)
                '
                returnUserId = 0
                allowEmailLogin = cpCore.siteProperties.getBoolean("allowEmailLogin")
                allowNoPasswordLogin = cpCore.siteProperties.getBoolean("allowNoPasswordLogin")
                If iLoginFieldValue = "" Then
                    '
                    ' ----- loginFieldValue blank, stop here
                    '
                    If allowEmailLogin Then
                        Call cpCore.error_AddUserError("A valid login requires a non-blank username or email.")
                    Else
                        Call cpCore.error_AddUserError("A valid login requires a non-blank username.")
                    End If
                ElseIf (Not allowNoPasswordLogin) And (iPassword = "") Then
                    '
                    ' ----- password blank, stop here
                    '
                    Call cpCore.error_AddUserError("A valid login requires a non-blank password.")
                ElseIf (cpCore.visit_loginAttempts >= main_maxVisitLoginAttempts) Then
                    '
                    ' ----- already tried 5 times
                    '
                    Call cpCore.error_AddUserError(badLoginUserError)
                Else
                    If allowEmailLogin Then
                        '
                        ' login by username or email
                        '
                        Criteria = "((username=" & cpCore.db.encodeSQLText(iLoginFieldValue) & ")or(email=" & cpCore.db.encodeSQLText(iLoginFieldValue) & "))"
                    Else
                        '
                        ' login by username only
                        '
                        Criteria = "(username=" & cpCore.db.encodeSQLText(iLoginFieldValue) & ")"
                    End If
                    If True Then
                        Criteria = Criteria & "and((dateExpires is null)or(dateExpires>" & cpCore.db.encodeSQLDate(Now()) & "))"
                    End If
                    CS = cpCore.db.csOpen("People", Criteria, "id", , , , , "ID ,password,admin,developer", 2)
                    If Not cpCore.db.cs_Ok(CS) Then
                        '
                        ' ----- loginFieldValue not found, stop here
                        '
                        Call cpCore.error_AddUserError(badLoginUserError)
                    ElseIf (Not EncodeBoolean(cpCore.siteProperties.getBoolean("AllowDuplicateUsernames", False))) And (cpCore.db.db_GetCSRowCount(CS) > 1) Then
                        '
                        ' ----- AllowDuplicates is false, and there are more then one record
                        '
                        Call cpCore.error_AddUserError("This user account can not be used because the username is not unique on this website. Please contact the site administrator.")
                    Else
                        '
                        ' ----- search all found records for the correct password
                        '
                        Do While cpCore.db.cs_Ok(CS)
                            returnUserId = 0
                            '
                            ' main_Get Id if password good
                            '
                            If (iPassword = "") Then
                                '
                                ' no-password-login -- allowNoPassword + no password given + account has no password + account not admin/dev/cm
                                '
                                recordIsAdmin = cpCore.db.cs_getBoolean(CS, "admin")
                                recordIsDeveloper = Not cpCore.db.cs_getBoolean(CS, "admin")
                                If allowNoPasswordLogin And (cpCore.db.cs_getText(CS, "password") = "") And (Not recordIsAdmin) And (recordIsDeveloper) Then
                                    returnUserId = cpCore.db.cs_getInteger(CS, "ID")
                                    '
                                    ' verify they are in no content manager groups
                                    '
                                    SQL = "SELECT ccGroupRules.ContentID" _
                                    & " FROM ccGroupRules RIGHT JOIN ccMemberRules ON ccGroupRules.GroupID = ccMemberRules.GroupID" _
                                    & " WHERE (" _
                                        & "(ccMemberRules.MemberID=" & cpCore.db.encodeSQLNumber(returnUserId) & ")" _
                                        & " AND(ccMemberRules.active<>0)" _
                                        & " AND(ccGroupRules.active<>0)" _
                                        & " AND(ccGroupRules.ContentID Is not Null)" _
                                        & " AND((ccMemberRules.DateExpires is null)OR(ccMemberRules.DateExpires>" & cpCore.db.encodeSQLDate(cpCore.main_PageStartTime) & "))" _
                                        & ");"
                                    CS = cpCore.db.cs_openSql(SQL)
                                    If cpCore.db.cs_Ok(CS) Then
                                        returnUserId = 0
                                    End If
                                    Call cpCore.db.cs_Close(CS)
                                End If
                            Else
                                '
                                ' password login
                                '
                                If LCase(cpCore.db.cs_getText(CS, "password")) = LCase(iPassword) Then
                                    returnUserId = cpCore.db.cs_getInteger(CS, "ID")
                                End If
                            End If
                            If returnUserId <> 0 Then
                                Exit Do
                            End If
                            Call cpCore.db.db_csGoNext(CS)
                        Loop
                        If returnUserId = 0 Then
                            Call cpCore.error_AddUserError(badLoginUserError)
                        End If
                    End If
                    Call cpCore.db.cs_Close(CS)
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnUserId
        End Function
        '
        '====================================================================================================
        '   Checks the username and password for a new login
        '       returns true if this can be used
        '       returns false, and a User Error response if it can not be used
        '
        Public Function main_IsNewLoginOK(ByVal Username As String, ByVal Password As String, Optional ByRef ErrorMessage As String = "", Optional ByVal ErrorCode As Integer = 0) As Boolean
            Dim returnOk As Boolean = False
            Try
                Dim CSPointer As Integer
                '
                returnOk = False
                If Username = "" Then
                    '
                    ' ----- username blank, stop here
                    '
                    ErrorCode = 1
                    ErrorMessage = "A valid login requires a non-blank username."
                ElseIf Password = "" Then
                    '
                    ' ----- password blank, stop here
                    '
                    ErrorCode = 4
                    ErrorMessage = "A valid login requires a non-blank password."
                    '    ElseIf Not main_VisitCookieSupport Then
                    '        '
                    '        ' No Cookie Support, can not log in
                    '        '
                    '        errorCode = 2
                    '        errorMessage = "You currently have cookie support disabled in your browser. Without cookies, your browser can not support the level of security required to login."
                Else

                    CSPointer = cpCore.db.csOpen("People", "username=" & cpCore.db.encodeSQLText(Username), , False, , , , "ID", 2)
                    If cpCore.db.cs_Ok(CSPointer) Then
                        '
                        ' ----- username was found, stop here
                        '
                        ErrorCode = 3
                        ErrorMessage = "The username you supplied is currently in use."
                    Else
                        returnOk = True
                    End If
                    Call cpCore.db.cs_Close(CSPointer)
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
            Return returnOk
        End Function
        '
        '====================================================================================================
        ' main_GetContentAccessRights( ContentIdOrName, returnAllowEdit, returnAllowAdd, returnAllowDelete )
        '
        Friend Sub getContentAccessRights(ByVal ContentName As String, ByRef returnAllowEdit As Boolean, ByRef returnAllowAdd As Boolean, ByRef returnAllowDelete As Boolean)
            Try
                Dim ContentID As Integer
                Dim CDef As coreMetaDataClass.CDefClass
                '
                returnAllowEdit = False
                returnAllowAdd = False
                returnAllowDelete = False
                If True Then
                    If Not isAuthenticated() Then
                        '
                        ' no authenticated, you are not a conent manager
                        '
                    ElseIf String.IsNullOrEmpty(ContentName) Then
                        '
                        ' no content given, do not handle the general case -- use user.main_IsContentManager2()
                        '
                    ElseIf isAuthenticatedDeveloper() Then
                        '
                        ' developers are always content managers
                        '
                        returnAllowEdit = True
                        returnAllowAdd = True
                        returnAllowDelete = True
                    ElseIf isAuthenticatedAdmin() Then
                        '
                        ' admin is content manager if the CDef is not developer only
                        '
                        CDef = cpCore.metaData.getCdef(ContentName)
                        If CDef.Id <> 0 Then
                            If Not CDef.DeveloperOnly Then
                                returnAllowEdit = True
                                returnAllowAdd = True
                                returnAllowDelete = True
                            End If
                        End If
                    Else
                        '
                        ' Authenticated and not admin or developer
                        '
                        ContentID = cpCore.main_GetContentID(ContentName)
                        Call getContentAccessRights_NonAdminByContentId(ContentID, returnAllowEdit, returnAllowAdd, returnAllowDelete, "")
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
        '
        '====================================================================================================
        ' main_GetContentAccessRights_NonAdminByContentId
        '   Checks if the member is a content manager for the specific content,
        '   Which includes transversing up the tree to find the next rule that applies'
        '   Member must be checked for authenticated and main_IsAdmin already
        '========================================================================
        '
        Private Sub getContentAccessRights_NonAdminByContentId(ByVal ContentID As Integer, ByRef returnAllowEdit As Boolean, ByRef returnAllowAdd As Boolean, ByRef returnAllowDelete As Boolean, ByVal usedContentIdList As String)
            Try
                Dim SQL As String
                Dim CSPointer As Integer
                Dim ParentID As Integer
                Dim ContentName As String
                Dim CDef As coreMetaDataClass.CDefClass
                '
                returnAllowEdit = False
                returnAllowAdd = False
                returnAllowDelete = False
                If IsInDelimitedString(usedContentIdList, CStr(ContentID), ",") Then
                    '
                    ' failed usedContentIdList test, this content id was in the child path
                    '
                    Call Err.Raise(KmaErrorInternal, "dll", "ContentID [" & ContentID & "] was found to be in it's own parentid path.")
                ElseIf ContentID < 1 Then
                    '
                    ' ----- not a valid contentname
                    '
                ElseIf IsInDelimitedString(main_GetContentAccessRights_NotList, CStr(ContentID), ",") Then
                    '
                    ' ----- was previously found to not be a Content Manager
                    '
                ElseIf IsInDelimitedString(main_GetContentAccessRights_List, CStr(ContentID), ",") Then
                    '
                    ' ----- was previously found to be a Content Manager
                    '
                    returnAllowEdit = True
                    returnAllowAdd = IsInDelimitedString(main_GetContentAccessRights_AllowAddList, CStr(ContentID), ",")
                    returnAllowDelete = IsInDelimitedString(main_GetContentAccessRights_AllowDeleteList, CStr(ContentID), ",")
                Else
                    '
                    ' ----- Must test it
                    '
                    SQL = "SELECT ccGroupRules.ContentID,allowAdd,allowDelete" _
                    & " FROM ccGroupRules RIGHT JOIN ccMemberRules ON ccGroupRules.GroupID = ccMemberRules.GroupID" _
                    & " WHERE (" _
                        & " (ccMemberRules.MemberID=" & cpCore.db.encodeSQLNumber(id) & ")" _
                        & " AND(ccMemberRules.active<>0)" _
                        & " AND(ccGroupRules.active<>0)" _
                        & " AND(ccGroupRules.ContentID=" & ContentID & ")" _
                        & " AND((ccMemberRules.DateExpires is null)OR(ccMemberRules.DateExpires>" & cpCore.db.encodeSQLDate(cpCore.main_PageStartTime) & "))" _
                        & ");"
                    CSPointer = cpCore.db.cs_openSql(SQL)
                    If cpCore.db.cs_Ok(CSPointer) Then
                        returnAllowEdit = True
                        returnAllowAdd = cpCore.db.cs_getBoolean(CSPointer, "allowAdd")
                        returnAllowDelete = cpCore.db.cs_getBoolean(CSPointer, "allowDelete")
                    End If
                    cpCore.db.cs_Close(CSPointer)
                    '
                    If Not returnAllowEdit Then
                        '
                        ' ----- Not a content manager for this one, check the parent
                        '
                        ContentName = cpCore.metaData.getContentNameByID(ContentID)
                        If ContentName <> "" Then
                            CDef = cpCore.metaData.getCdef(ContentName)
                            ParentID = CDef.parentID
                            If ParentID > 0 Then
                                Call getContentAccessRights_NonAdminByContentId(ParentID, returnAllowEdit, returnAllowAdd, returnAllowDelete, usedContentIdList & "," & CStr(ContentID))
                            End If
                        End If
                    End If
                    If returnAllowEdit Then
                        '
                        ' ----- Was found to be true
                        '
                        main_GetContentAccessRights_List = main_GetContentAccessRights_List & "," & CStr(ContentID)
                        If returnAllowAdd Then
                            main_GetContentAccessRights_AllowAddList = main_GetContentAccessRights_AllowAddList & "," & CStr(ContentID)
                        End If
                        If returnAllowDelete Then
                            main_GetContentAccessRights_AllowDeleteList = main_GetContentAccessRights_AllowDeleteList & "," & CStr(ContentID)
                        End If
                    Else
                        '
                        ' ----- Was found to be false
                        '
                        main_GetContentAccessRights_NotList = main_GetContentAccessRights_NotList & "," & CStr(ContentID)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
            End Try
        End Sub
    End Class
End Namespace