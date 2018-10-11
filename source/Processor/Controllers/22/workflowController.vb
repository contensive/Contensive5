﻿
Option Explicit On
Option Strict On

Imports Contensive.Core.Controllers
Imports Contensive.Core.Controllers.genericController
Imports Contensive.Core.Models.Entity

Namespace Contensive.Core.Controllers
    Public Class workflowController
        Implements IDisposable
        '
        '------------------------------------------------------------------------------------------------------------------------
        ' objects passed in that are not disposed
        '------------------------------------------------------------------------------------------------------------------------
        '
        Private cpCore As coreClass
        '
        '------------------------------------------------------------------------------------------------------------------------
        ' objects created within class to dispose in dispose
        '   typically used as app.meaData.method()
        '------------------------------------------------------------------------------------------------------------------------
        '
        '------------------------------------------------------------------------------------------------------------------------
        ' internal storage
        '   these objects is deserialized during constructor
        '   appConfig has static setup values like file system endpoints and Db connection string
        '   appProperties has dynamic values and is serialized and saved when changed
        '   include properties saved in appConfig file, settings not editable online.
        '------------------------------------------------------------------------------------------------------------------------
        '
        Private constructed As Boolean = False                                  ' set true when contructor is finished 
        Public Const csv_AllowAutocsv_ClearContentTimeStamp = True
        '
        '
        Private Structure EditLockType
            Public Key As String
            Public MemberID As Integer
            Public DateExpires As Date
        End Structure
        Private EditLockArray() As EditLockType
        Private EditLockCount As Integer
        '
        '-----------------------------------------------------------------------
        ' ----- Edit Lock info
        '-----------------------------------------------------------------------
        '
        Private main_EditLockContentRecordKey_Local As String = ""
        Private main_EditLockStatus_Local As Boolean = False
        Private main_EditLockMemberID_Local As Integer = 0
        Private main_EditLockMemberName_Local As String = ""
        Private main_EditLockDateExpires_Local As Date = Date.MinValue
        '
        '==========================================================================================
        ''' <summary>
        ''' constructor
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="cluster"></param>
        ''' <param name="appName"></param>
        ''' <remarks></remarks>
        Public Sub New(cpCore As coreClass)
            MyBase.New()
            Try
                '
                Me.cpCore = cpCore
                constructed = True
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
                Throw (ex)
            End Try
        End Sub
        '
        '==========================================================================================
        '
        '
        '========================================================================
        '   Returns true if the record is locked to the current member
        '========================================================================
        '
        Public Function GetEditLockStatus(ByVal ContentName As String, ByVal RecordID As Integer) As Boolean
            Try
                '
                'If Not (true) Then Exit Function
                '
                Dim ReturnMemberID As Integer
                Dim ReturnDateExpires As Date
                '
                main_EditLockContentRecordKey_Local = (ContentName & genericController.encodeText(RecordID))
                main_EditLockDateExpires_Local = Date.MinValue
                main_EditLockMemberID_Local = 0
                main_EditLockMemberName_Local = ""
                main_EditLockStatus_Local = False
                '
                main_EditLockStatus_Local = getEditLock(genericController.encodeText(ContentName), genericController.EncodeInteger(RecordID), ReturnMemberID, ReturnDateExpires)
                If main_EditLockStatus_Local And (ReturnMemberID <> cpCore.doc.authContext.user.id) Then
                    main_EditLockStatus_Local = True
                    main_EditLockDateExpires_Local = ReturnDateExpires
                    main_EditLockMemberID_Local = ReturnMemberID
                    main_EditLockMemberName_Local = ""
                End If
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return main_EditLockStatus_Local
        End Function
        '
        '========================================================================
        '   Edit Lock Handling
        '========================================================================
        '
        Public Function GetEditLockMemberName(ByVal ContentName As String, ByVal RecordID As Integer) As String
            Try
                Dim CS As Integer
                '
                If (main_EditLockContentRecordKey_Local <> (ContentName & genericController.encodeText(RecordID))) Then
                    Call GetEditLockStatus(ContentName, RecordID)
                End If
                If main_EditLockStatus_Local Then
                    If main_EditLockMemberName_Local = "" Then
                        If main_EditLockMemberID_Local <> 0 Then
                            CS = cpCore.db.cs_open2("people", main_EditLockMemberID_Local)
                            If cpCore.db.csOk(CS) Then
                                main_EditLockMemberName_Local = cpCore.db.csGetText(CS, "name")
                            End If
                            Call cpCore.db.csClose(CS)
                        End If
                        If main_EditLockMemberName_Local = "" Then
                            main_EditLockMemberName_Local = "unknown"
                        End If
                    End If
                    GetEditLockMemberName = main_EditLockMemberName_Local
                End If
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return main_EditLockMemberName_Local
        End Function
        '
        '========================================================================
        '   Edit Lock Handling
        '========================================================================
        '
        Public Function GetEditLockDateExpires(ByVal ContentName As String, ByVal RecordID As Integer) As Date
            Dim returnDate As Date = Date.MinValue
            Try
                If (main_EditLockContentRecordKey_Local <> (ContentName & genericController.encodeText(RecordID))) Then
                    Call GetEditLockStatus(ContentName, RecordID)
                End If
                If main_EditLockStatus_Local Then
                    returnDate = main_EditLockDateExpires_Local
                End If
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return returnDate
        End Function
        '
        '========================================================================
        '   Sets the edit lock for this record
        '========================================================================
        '
        Public Sub SetEditLock(ByVal ContentName As String, ByVal RecordID As Integer)
            Call setEditLock(genericController.encodeText(ContentName), genericController.EncodeInteger(RecordID), cpCore.doc.authContext.user.id)
        End Sub
        '
        '========================================================================
        '   Clears the edit lock for this record
        '========================================================================
        '
        Public Sub ClearEditLock(ByVal ContentName As String, ByVal RecordID As Integer)
            Call clearEditLock(genericController.encodeText(ContentName), genericController.EncodeInteger(RecordID), cpCore.doc.authContext.user.id)
        End Sub
        ''
        ''========================================================================
        ''   Aborts any edits for this record
        ''========================================================================
        ''
        'Public Sub publishEdit(ByVal ContentName As String, ByVal RecordID As Integer)
        '    Call publishEdit(genericController.encodeText(ContentName), genericController.EncodeInteger(RecordID), cpCore.doc.authContext.user.id)
        'End Sub
        '
        ''========================================================================
        ''   Approves any edits for this record
        ''========================================================================
        ''
        'Public Sub approveEdit(ByVal ContentName As String, ByVal RecordID As Integer)
        '    Call approveEdit(genericController.encodeText(ContentName), genericController.EncodeInteger(RecordID), cpCore.doc.authContext.user.id)
        'End Sub
        ''
        ''========================================================================
        ''   Submits any edits for this record
        ''========================================================================
        ''
        'Public Sub main_SubmitEdit(ByVal ContentName As String, ByVal RecordID As Integer)
        '    Call submitEdit2(genericController.encodeText(ContentName), genericController.EncodeInteger(RecordID), cpCore.doc.authContext.user.id)
        'End Sub
        ''
        ''=========================================================================================
        ''   Determine if this Content Definition is run on a table that supports
        ''   Workflow authoring. Only the ContentTable is checked, it is assumed that the
        ''   AuthoringTable is a copy of the ContentTable
        ''=========================================================================================
        ''
        'Public Function isWorkflowAuthoringCompatible(ByVal ContentName As String) As Boolean
        '    isWorkflowAuthoringCompatible = genericController.EncodeBoolean(cpCore.metaData.GetContentProperty(genericController.encodeText(ContentName), "ALLOWWORKFLOWAUTHORING"))
        'End Function
        ''
        ''==========================================================================================
        ''==========================================================================================
        ''
        ''
        ''=================================================================================
        ''
        ''=================================================================================
        ''
        'Public Sub publishEdit(ByVal ContentName As String, ByVal RecordID As Integer, ByVal MemberID As Integer)
        '    Try
        '        '
        '        Dim RSLive As DataTable
        '        Dim LiveRecordID As Integer
        '        Dim LiveDataSourceName As String
        '        Dim LiveTableName As String
        '        Dim LiveSQLValue As String
        '        Dim LiveRecordBlank As Boolean
        '        '
        '        Dim BlankSQLValue As String
        '        '
        '        Dim RSEdit As DataTable
        '        Dim EditDataSourceName As String
        '        Dim EditTableName As String
        '        Dim EditRecordID As Integer
        '        Dim EditSQLValue As String
        '        Dim EditFilename As String
        '        Dim EditRecordCID As Integer
        '        Dim EditRecordBlank As Boolean
        '        '
        '        Dim NewEditRecordID As Integer
        '        Dim NewEditFilename As String
        '        '
        '        Dim ContentID As Integer
        '        '
        '        Dim FieldNameArray() As String
        '        Dim ArchiveSqlFieldList As New sqlFieldListClass
        '        Dim NewEditSqlFieldList As New sqlFieldListClass
        '        Dim PublishFieldNameArray() As String
        '        Dim PublishSqlFieldList As New sqlFieldListClass
        '        Dim BlankSqlFieldList As New sqlFieldListClass
        '        '
        '        Dim FieldPointer As Integer
        '        Dim FieldCount As Integer
        '        Dim FieldName As String
        '        Dim fieldTypeId As Integer
        '        Dim SQL As String
        '        Dim MethodName As String
        '        Dim FieldArraySize As Integer
        '        Dim PublishingDelete As Boolean
        '        Dim PublishingInactive As Boolean
        '        Dim CDef As Models.Complex.cdefModel
        '        Dim FieldList As String
        '        '
        '        MethodName = "csv_PublishEdit"
        '        '
        '        CDef = models.complex.cdefmodel.getcdef(cpcore,ContentName)
        '        If CDef.Id > 0 Then
        '            If false And cpCore.siteProperties.allowWorkflowAuthoring Then
        '                With CDef
        '                    FieldList = .SelectCommaList
        '                    LiveDataSourceName = .ContentDataSourceName
        '                    LiveTableName = .ContentTableName
        '                    EditDataSourceName = .AuthoringDataSourceName
        '                    EditTableName = .AuthoringTableName
        '                    FieldCount = .fields.Count
        '                    ContentID = .Id
        '                    ContentName = .Name
        '                End With
        '                FieldArraySize = FieldCount + 6
        '                ReDim FieldNameArray(FieldArraySize)
        '                'ReDim ArchiveSqlFieldList(FieldArraySize)
        '                'ReDim NewEditSqlFieldList(FieldArraySize)
        '                'ReDim BlankSqlFieldList(FieldArraySize)
        '                'ReDim PublishSqlFieldList(FieldArraySize)
        '                ReDim PublishFieldNameArray(FieldArraySize)
        '                LiveRecordID = RecordID
        '                '
        '                ' ----- Open the live record
        '                '
        '                RSLive = cpCore.db.executeSql("SELECT " & FieldList & " FROM " & LiveTableName & " WHERE ID=" & cpCore.db.encodeSQLNumber(LiveRecordID) & ";", LiveDataSourceName)
        '                'RSLive = appservices.cpcore.db.executeSql(LiveDataSourceName, "SELECT " & FieldList & " FROM " & LiveTableName & " WHERE ID=" & encodeSQLNumber(LiveRecordID) & ";")
        '                If RSLive.Rows.Count <= 0 Then
        '                    '
        '                    Throw (New ApplicationException("During record publishing, there was an error opening the live record, [ID=" & LiveRecordID & "] in table [" & LiveTableName & "] on datasource [" & LiveDataSourceName & " ]"))
        '                Else
        '                    If True Then
        '                        LiveRecordID = genericController.EncodeInteger(cpCore.db.getDataRowColumnName(RSLive.Rows(0), "ID"))
        '                        LiveRecordBlank = genericController.EncodeBoolean(cpCore.db.getDataRowColumnName(RSLive.Rows(0), "EditBlank"))
        '                        '
        '                        ' ----- Open the edit record
        '                        '
        '                        RSEdit = cpCore.db.executeSql("SELECT " & FieldList & " FROM " & EditTableName & " WHERE (EditSourceID=" & cpCore.db.encodeSQLNumber(LiveRecordID) & ")and(EditArchive=0) Order By ID DESC;", EditDataSourceName)
        '                        'RSEdit = appservices.cpcore.db.executeSql(EditDataSourceName, "SELECT " & FieldList & " FROM " & EditTableName & " WHERE (EditSourceID=" & encodeSQLNumber(LiveRecordID) & ")and(EditArchive=0) Order By ID DESC;")
        '                        If RSEdit.Rows.Count <= 0 Then
        '                            '
        '                            Throw (New ApplicationException("During record publishing, there was an error opening the edit record [EditSourceID=" & LiveRecordID & "] in table [" & EditTableName & "] on datasource [" & EditDataSourceName & " ]"))
        '                        Else
        '                            If True Then
        '                                EditRecordID = genericController.EncodeInteger(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), "ID"))
        '                                EditRecordCID = genericController.EncodeInteger(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), "ContentControlID"))
        '                                EditRecordBlank = genericController.EncodeBoolean(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), "EditBlank"))
        '                                PublishingDelete = EditRecordBlank
        '                                PublishingInactive = Not genericController.EncodeBoolean(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), "active"))
        '                                '
        '                                ' ----- Create new Edit record
        '                                '
        '                                If Not PublishingDelete Then
        '                                    NewEditRecordID = cpCore.db.insertTableRecordGetId(EditDataSourceName, EditTableName, SystemMemberID)
        '                                    If NewEditRecordID < 1 Then
        '                                        '
        '                                        Throw (New ApplicationException("During record publishing, a new edit record could not be create, table [" & EditTableName & "] on datasource [" & EditDataSourceName & " ]"))
        '                                    End If
        '                                End If
        '                                If True Then
        '                                    '
        '                                    ' ----- create update arrays
        '                                    '
        '                                    FieldPointer = 0
        '                                    For Each keyValuePair As KeyValuePair(Of String, Models.Complex.CDefFieldModel) In CDef.fields
        '                                        Dim field As Models.Complex.CDefFieldModel = keyValuePair.Value
        '                                        With field
        '                                            FieldName = .nameLc
        '                                            fieldTypeId = .fieldTypeId
        '                                            Select Case fieldTypeId
        '                                                Case FieldTypeIdManyToMany, FieldTypeIdRedirect
        '                                                    '
        '                                                    ' These content fields have no Db Field
        '                                                    '
        '                                                Case Else
        '                                                    '
        '                                                    ' Process These field types
        '                                                    '
        '                                                    Select Case genericController.vbUCase(FieldName)
        '                                                        Case "ID", "CONTENTCONTROLID"  
        '                                                        '
        '                                                        ' ----- control fields that should not be in any dataset
        '                                                        '
        '                                                        Case "MODIFIEDDATE", "MODIFIEDBY"
        '                                                            '
        '                                                            ' ----- non-content fields that need to be in all datasets
        '                                                            '       add manually later, in case they are not in ContentDefinition
        '                                                            '
        '                                                        Case Else
        '                                                            '
        '                                                            ' ----- Content related field
        '                                                            '
        '                                                            LiveSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSLive.Rows(0), FieldName), fieldTypeId)
        '                                                            EditSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), FieldName), fieldTypeId)
        '                                                            BlankSQLValue = cpCore.db.EncodeSQL(Nothing, fieldTypeId)
        '                                                            FieldNameArray(FieldPointer) = FieldName
        '                                                            '
        '                                                            ' ----- New Edit Record value
        '                                                            '
        '                                                            If Not PublishingDelete Then
        '                                                                Select Case fieldTypeId
        '                                                                    Case FieldTypeIdFileCSS, FieldTypeIdFileXML, FieldTypeIdFileJavascript
        '                                                                        '
        '                                                                        ' ----- cdn files - create copy of File for neweditrecord
        '                                                                        '
        '                                                                        EditFilename = genericController.encodeText(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), FieldName))
        '                                                                        If EditFilename = "" Then
        '                                                                            NewEditSqlFieldList.add(FieldName, cpCore.db.encodeSQLText(""))
        '                                                                        Else
        '                                                                            NewEditFilename = fileController.getVirtualRecordPathFilename(EditTableName, FieldName, NewEditRecordID, fieldTypeId)
        '                                                                            'NewEditFilename = csv_GetVirtualFilename(ContentName, FieldName, NewEditRecordID)
        '                                                                            Call cpCore.cdnFiles.copyFile(EditFilename, NewEditFilename)
        '                                                                            NewEditSqlFieldList.add(FieldName, cpCore.db.encodeSQLText(NewEditFilename))
        '                                                                        End If
        '                                                                    Case FieldTypeIdFileText, FieldTypeIdFileHTML
        '                                                                        '
        '                                                                        ' ----- private files - create copy of File for neweditrecord
        '                                                                        '
        '                                                                        EditFilename = genericController.encodeText(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), FieldName))
        '                                                                        If EditFilename = "" Then
        '                                                                            NewEditSqlFieldList.add(FieldName, cpCore.db.encodeSQLText(""))
        '                                                                        Else
        '                                                                            NewEditFilename = fileController.getVirtualRecordPathFilename(EditTableName, FieldName, NewEditRecordID, fieldTypeId)
        '                                                                            'NewEditFilename = csv_GetVirtualFilename(ContentName, FieldName, NewEditRecordID)
        '                                                                            Call cpCore.cdnFiles.copyFile(EditFilename, NewEditFilename)
        '                                                                            NewEditSqlFieldList.add(FieldName, cpCore.db.encodeSQLText(NewEditFilename))
        '                                                                        End If
        '                                                                    Case Else
        '                                                                        '
        '                                                                        ' ----- put edit value in new edit record
        '                                                                        '
        '                                                                        NewEditSqlFieldList.add(FieldName, EditSQLValue)
        '                                                                End Select
        '                                                            End If
        '                                                            '
        '                                                            ' ----- set archive value
        '                                                            '
        '                                                            ArchiveSqlFieldList.add(FieldName, LiveSQLValue)
        '                                                            '
        '                                                            ' ----- set live record value (and name)
        '                                                            '
        '                                                            If PublishingDelete Then
        '                                                                '
        '                                                                ' ----- Record delete - fill the live record with null
        '                                                                '
        '                                                                PublishFieldNameArray(FieldPointer) = FieldName
        '                                                                PublishSqlFieldList.add(FieldName, BlankSQLValue)
        '                                                            Else
        '                                                                PublishFieldNameArray(FieldPointer) = FieldName
        '                                                                PublishSqlFieldList.add(FieldName, EditSQLValue)
        '                                                            End If
        '                                                    End Select
        '                                            End Select
        '                                        End With
        '                                        FieldPointer += 1
        '                                    Next
        '                                    '
        '                                    ' ----- create non-content control field entries
        '                                    '
        '                                    FieldName = "MODIFIEDDATE"
        '                                    fieldTypeId = FieldTypeIdDate
        '                                    FieldNameArray(FieldPointer) = FieldName
        '                                    LiveSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSLive.Rows(0), FieldName), fieldTypeId)
        '                                    EditSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), FieldName), fieldTypeId)
        '                                    ArchiveSqlFieldList.add(FieldName, LiveSQLValue)
        '                                    NewEditSqlFieldList.add(FieldName, EditSQLValue)
        '                                    PublishFieldNameArray(FieldPointer) = FieldName
        '                                    PublishSqlFieldList.add(FieldName, EditSQLValue)
        '                                    FieldPointer = FieldPointer + 1
        '                                    '
        '                                    FieldName = "MODIFIEDBY"
        '                                    fieldTypeId = FieldTypeIdLookup
        '                                    FieldNameArray(FieldPointer) = FieldName
        '                                    LiveSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSLive.Rows(0), FieldName), fieldTypeId)
        '                                    EditSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), FieldName), fieldTypeId)
        '                                    ArchiveSqlFieldList.add(FieldName, LiveSQLValue)
        '                                    NewEditSqlFieldList.add(FieldName, EditSQLValue)
        '                                    PublishFieldNameArray(FieldPointer) = FieldName
        '                                    PublishSqlFieldList.add(FieldName, EditSQLValue)
        '                                    FieldPointer = FieldPointer + 1
        '                                    '
        '                                    FieldName = "CONTENTCONTROLID"
        '                                    fieldTypeId = FieldTypeIdLookup
        '                                    FieldNameArray(FieldPointer) = FieldName
        '                                    LiveSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSLive.Rows(0), FieldName), fieldTypeId)
        '                                    EditSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), FieldName), fieldTypeId)
        '                                    ArchiveSqlFieldList.add(FieldName, LiveSQLValue)
        '                                    NewEditSqlFieldList.add(FieldName, EditSQLValue)
        '                                    PublishFieldNameArray(FieldPointer) = FieldName
        '                                    If PublishingDelete Then
        '                                        PublishSqlFieldList.add(FieldName, cpCore.db.encodeSQLNumber(0))
        '                                    Else
        '                                        PublishSqlFieldList.add(FieldName, EditSQLValue)
        '                                    End If
        '                                    FieldPointer = FieldPointer + 1
        '                                    '
        '                                    FieldName = "EDITBLANK"
        '                                    fieldTypeId = FieldTypeIdBoolean
        '                                    FieldNameArray(FieldPointer) = FieldName
        '                                    LiveSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSLive.Rows(0), FieldName), fieldTypeId)
        '                                    EditSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), FieldName), fieldTypeId)
        '                                    ArchiveSqlFieldList.add(FieldName, LiveSQLValue)
        '                                    NewEditSqlFieldList.add(FieldName, EditSQLValue)
        '                                    PublishFieldNameArray(FieldPointer) = FieldName
        '                                    If PublishingDelete Then
        '                                        PublishSqlFieldList.add(FieldName, SQLFalse)
        '                                    Else
        '                                        PublishSqlFieldList.add(FieldName, EditSQLValue)
        '                                    End If

        '                                    '
        '                                    ' ----- copy edit record to live record
        '                                    '
        '                                    Call cpCore.db.updateTableRecord(LiveDataSourceName, LiveTableName, "ID=" & LiveRecordID, PublishSqlFieldList)
        '                                    '
        '                                    ' ----- copy live record to archive record and the edit to the new edit
        '                                    '
        '                                    Call cpCore.db.updateTableRecord(EditDataSourceName, EditTableName, "ID=" & EditRecordID, ArchiveSqlFieldList)
        '                                    If Not PublishingDelete Then
        '                                        Call cpCore.db.updateTableRecord(EditDataSourceName, EditTableName, "ID=" & NewEditRecordID, NewEditSqlFieldList)
        '                                    End If
        '                                    '
        '                                    ' ----- Content Watch effects
        '                                    '
        '                                    If PublishingDelete Then
        '                                        '
        '                                        ' Record was deleted, delete contentwatch records also
        '                                        '
        '                                        Call cpCore.db.deleteContentRules(ContentID, RecordID)
        '                                    End If
        '                                    '
        '                                    ' ----- mark the SpiderDocs record not up-to-date
        '                                    '
        '                                    If (LCase(EditTableName) = "ccpagecontent") And (LiveRecordID <> 0) Then
        '                                        If cpCore.db.isSQLTableField("default", "ccSpiderDocs", "PageID") Then
        '                                            SQL = "UPDATE ccspiderdocs SET UpToDate = 0 WHERE PageID=" & LiveRecordID
        '                                            Call cpCore.db.executeSql(SQL)
        '                                        End If
        '                                    End If
        '                                    '
        '                                    ' ----- Clear Time Stamp because a record changed
        '                                    '
        '                                    If csv_AllowAutocsv_ClearContentTimeStamp Then
        '                                        Call cpCore.cache.invalidateObject(ContentName)
        '                                    End If
        '                                End If
        '                            End If
        '                            'RSEdit.Close()
        '                        End If
        '                        'RSEdit = Nothing
        '                    End If
        '                    'RSLive.Close()
        '                End If
        '                RSLive.Dispose()
        '                '
        '                ' ----- Clear all Authoring Controls
        '                '
        '                Call clearAuthoringControl(ContentName, LiveRecordID, AuthoringControlsEditing, MemberID)
        '                Call clearAuthoringControl(ContentName, LiveRecordID, AuthoringControlsModified, MemberID)
        '                Call clearAuthoringControl(ContentName, LiveRecordID, AuthoringControlsApproved, MemberID)
        '                Call clearAuthoringControl(ContentName, LiveRecordID, AuthoringControlsSubmitted, MemberID)
        '                '
        '                '
        '                '
        '                If PublishingDelete Or PublishingInactive Then
        '                    Call cpCore.db.deleteContentRules(ContentID, LiveRecordID)
        '                End If
        '            End If
        '        End If
        '    Catch ex As Exception
        '        cpCore.handleException(ex) : Throw
        '    End Try
        'End Sub
        ''
        ''=================================================================================
        ''
        ''=================================================================================
        ''
        'Public Sub abortEdit2(ByVal ContentName As String, ByVal RecordID As Integer, ByVal MemberID As Integer)
        '    Try
        '        '
        '        Dim RSLive As DataTable
        '        Dim LiveRecordID As Integer
        '        Dim LiveDataSourceName As String
        '        Dim LiveTableName As String
        '        Dim LiveSQLValue As String
        '        Dim LiveFilename As String
        '        '
        '        Dim RSEdit As DataTable
        '        Dim EditDataSourceName As String
        '        Dim EditTableName As String
        '        Dim EditRecordID As Integer
        '        Dim EditFilename As String
        '        Dim ContentID As Integer
        '        Dim FieldPointer As Integer
        '        Dim FieldCount As Integer
        '        Dim FieldName As String
        '        Dim fieldTypeId As Integer
        '        Dim CDef As Models.Complex.cdefModel
        '        Dim sqlFieldList As New sqlFieldListClass
        '        '
        '        CDef = models.complex.cdefmodel.getcdef(cpcore,ContentName)
        '        If CDef.Id > 0 Then
        '            If false And cpCore.siteProperties.allowWorkflowAuthoring Then
        '                With CDef
        '                    LiveDataSourceName = .ContentDataSourceName
        '                    LiveTableName = .ContentTableName
        '                    EditDataSourceName = .AuthoringDataSourceName
        '                    EditTableName = .AuthoringTableName
        '                    FieldCount = .fields.Count
        '                    ContentID = .Id
        '                    ContentName = .Name
        '                End With
        '                'ReDim sqlFieldList(FieldCount + 2)
        '                'ReDim PublishFieldNameArray(FieldCount + 2)
        '                LiveRecordID = RecordID
        '                ' LiveRecordID = appservices.csv_cs_getField(CSPointer, "ID")
        '                '
        '                ' Open the live record
        '                '
        '                RSLive = cpCore.db.executeSql("SELECT * FROM " & LiveTableName & " WHERE ID=" & cpCore.db.encodeSQLNumber(LiveRecordID) & ";", LiveDataSourceName)
        '                If (RSLive Is Nothing) Then
        '                    '
        '                    Throw (New ApplicationException("During record publishing, there was an error opening the live record, [ID=" & LiveRecordID & "] in table [" & LiveTableName & "] on datasource [" & LiveDataSourceName & " ]"))
        '                Else
        '                    If RSLive.Rows.Count <= 0 Then
        '                        '
        '                        Throw (New ApplicationException("During record publishing, the live record could not be found, [ID=" & LiveRecordID & "] in table [" & LiveTableName & "] on datasource [" & LiveDataSourceName & " ]"))
        '                    Else
        '                        LiveRecordID = genericController.EncodeInteger(cpCore.db.getDataRowColumnName(RSLive.Rows(0), "ID"))
        '                        '
        '                        ' Open the edit record
        '                        '
        '                        RSEdit = cpCore.db.executeSql("SELECT * FROM " & EditTableName & " WHERE (EditSourceID=" & cpCore.db.encodeSQLNumber(LiveRecordID) & ")and(EditArchive=0) Order By ID DESC;", EditDataSourceName)
        '                        If (RSEdit Is Nothing) Then
        '                            '
        '                            Throw (New ApplicationException("During record publishing, there was an error opening the edit record [EditSourceID=" & LiveRecordID & "] in table [" & EditTableName & "] on datasource [" & EditDataSourceName & " ]"))
        '                        Else
        '                            If RSEdit.Rows.Count <= 0 Then
        '                                '
        '                                Throw (New ApplicationException("During record publishing, the edit record could not be found, [EditSourceID=" & LiveRecordID & "] in table [" & EditTableName & "] on datasource [" & EditDataSourceName & " ]"))
        '                            Else
        '                                EditRecordID = genericController.EncodeInteger(cpCore.db.getDataRowColumnName(RSEdit.Rows(0), "ID"))
        '                                '
        '                                ' create update arrays
        '                                '
        '                                FieldPointer = 0
        '                                For Each keyValuePair As KeyValuePair(Of String, Models.Complex.CDefFieldModel) In CDef.fields
        '                                    Dim field As Models.Complex.CDefFieldModel = keyValuePair.Value
        '                                    With field
        '                                        FieldName = .nameLc
        '                                        If cpCore.db.isSQLTableField(EditDataSourceName, EditTableName, FieldName) Then
        '                                            fieldTypeId = .fieldTypeId
        '                                            LiveSQLValue = cpCore.db.EncodeSQL(cpCore.db.getDataRowColumnName(RSLive.Rows(0), FieldName), fieldTypeId)
        '                                            Select Case genericController.vbUCase(FieldName)
        '                                                Case "ID"
        '                                                    '
        '                                                    '   block from dataset
        '                                                    '
        '                                                Case Else
        '                                                    '
        '                                                    ' allow only authorable fields
        '                                                    '
        '                                                    If (fieldTypeId = FieldTypeIdFileCSS) Or (fieldTypeId = FieldTypeIdFileJavascript) Or (fieldTypeId = FieldTypeIdFileXML) Then
        '                                                        '
        '                                                        '   cdnfiles - create copy of Live TextFile for Edit record
        '                                                        '
        '                                                        LiveFilename = genericController.encodeText(cpCore.db.getDataRowColumnName(RSLive.Rows(0), FieldName))
        '                                                        If LiveFilename <> "" Then
        '                                                            EditFilename = fileController.getVirtualRecordPathFilename(EditTableName, FieldName, EditRecordID, fieldTypeId)
        '                                                            Call cpCore.cdnFiles.copyFile(LiveFilename, EditFilename)
        '                                                            LiveSQLValue = cpCore.db.encodeSQLText(EditFilename)
        '                                                        End If
        '                                                    End If
        '                                                    If (fieldTypeId = FieldTypeIdFileText) Or (fieldTypeId = FieldTypeIdFileHTML) Then
        '                                                        '
        '                                                        '   pivatefiles - create copy of Live TextFile for Edit record
        '                                                        '
        '                                                        LiveFilename = genericController.encodeText(cpCore.db.getDataRowColumnName(RSLive.Rows(0), FieldName))
        '                                                        If LiveFilename <> "" Then
        '                                                            EditFilename = fileController.getVirtualRecordPathFilename(EditTableName, FieldName, EditRecordID, fieldTypeId)
        '                                                            Call cpCore.cdnFiles.copyFile(LiveFilename, EditFilename)
        '                                                            LiveSQLValue = cpCore.db.encodeSQLText(EditFilename)
        '                                                        End If
        '                                                    End If
        '                                                    '
        '                                                    sqlFieldList.add(FieldName, LiveSQLValue)
        '                                            End Select
        '                                        End If
        '                                    End With
        '                                    FieldPointer += 1
        '                                Next
        '                            End If
        '                            Call RSEdit.Dispose()
        '                        End If
        '                        RSEdit = Nothing
        '                    End If
        '                    RSLive.Dispose()
        '                End If
        '                RSLive = Nothing
        '                '
        '                ' ----- copy live record to editrecord
        '                '
        '                Call cpCore.db.updateTableRecord(EditDataSourceName, EditTableName, "ID=" & EditRecordID, sqlFieldList)
        '                '
        '                ' ----- Clear all authoring controls
        '                '
        '                Call clearAuthoringControl(ContentName, RecordID, AuthoringControlsModified, MemberID)
        '                Call clearAuthoringControl(ContentName, RecordID, AuthoringControlsSubmitted, MemberID)
        '                Call clearAuthoringControl(ContentName, RecordID, AuthoringControlsApproved, MemberID)
        '            End If
        '        End If
        '    Catch ex As Exception
        '        cpCore.handleException(ex) : Throw
        '    End Try
        'End Sub
        ''
        ''=================================================================================
        ''
        ''=================================================================================
        ''
        'Public Sub approveEdit(ByVal ContentName As String, ByVal RecordID As Integer, ByVal MemberID As Integer)
        '    Try
        '        '
        '        Dim CDef As Models.Complex.cdefModel
        '        '
        '        CDef = models.complex.cdefmodel.getcdef(cpcore,ContentName)
        '        If CDef.Id > 0 Then
        '            If false And cpCore.siteProperties.allowWorkflowAuthoring Then
        '                Call setAuthoringControl(ContentName, RecordID, AuthoringControlsApproved, MemberID)
        '            End If
        '        End If
        '    Catch ex As Exception
        '        cpCore.handleException(ex) : Throw
        '    End Try
        'End Sub
        ''
        ''=================================================================================
        ''
        ''=================================================================================
        ''
        'Public Sub submitEdit2(ByVal ContentName As String, ByVal RecordID As Integer, ByVal MemberID As Integer)
        '    Try
        '        '
        '        Dim CDef As Models.Complex.cdefModel
        '        '
        '        CDef = models.complex.cdefmodel.getcdef(cpcore,ContentName)
        '        If CDef.Id > 0 Then
        '            If false And cpCore.siteProperties.allowWorkflowAuthoring Then
        '                Call setAuthoringControl(ContentName, RecordID, AuthoringControlsSubmitted, MemberID)
        '            End If
        '        End If
        '    Catch ex As Exception
        '        cpCore.handleException(ex) : Throw
        '    End Try
        'End Sub
        '
        '=====================================================================================================
        '   returns true if another user has the record locked
        '=====================================================================================================
        '
        Public Function isRecordLocked(ByVal ContentName As String, ByVal RecordID As Integer, ByVal MemberID As Integer) As Boolean
            Dim result As Boolean = False
            Try
                Dim Criteria As String
                Dim CS As Integer
                '
                Criteria = getAuthoringControlCriteria(ContentName, RecordID) & "and(CreatedBy<>" & cpCore.db.encodeSQLNumber(MemberID) & ")"
                CS = cpCore.db.csOpen("Authoring Controls", Criteria, , , MemberID)
                result = cpCore.db.csOk(CS)
                Call cpCore.db.csClose(CS)
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return result
        End Function
        '
        '=====================================================================================================
        '   returns true if another user has the record locked
        '=====================================================================================================
        '
        Private Function getAuthoringControlCriteria(ByVal ContentName As String, ByVal RecordID As Integer) As String
            Dim result As String = ""
            Try
                Dim ContentCnt As Integer
                Dim CS As Integer
                Dim Criteria As String = ""
                Dim TableID As Integer
                '
                TableID = cpCore.db.GetContentTableID(ContentName)
                '
                ' Authoring Control records are referenced by ContentID
                '
                ContentCnt = 0
                CS = cpCore.db.csOpen("Content", "(contenttableid=" & TableID & ")")
                Do While cpCore.db.csOk(CS)
                    Criteria = Criteria & "," & cpCore.db.csGetInteger(CS, "ID")
                    ContentCnt = ContentCnt + 1
                    Call cpCore.db.csGoNext(CS)
                Loop
                Call cpCore.db.csClose(CS)
                If ContentCnt < 1 Then
                    '
                    ' No references to this table
                    '
                    Throw (New ApplicationException("TableID [" & TableID & "] could not be found in any ccContent.ContentTableID"))
                    result = "(1=0)"
                ElseIf ContentCnt = 1 Then
                    '
                    ' One content record
                    '
                    result = "(ContentID=" & cpCore.db.encodeSQLNumber(EncodeInteger(Mid(Criteria, 2))) & ")And(RecordID=" & cpCore.db.encodeSQLNumber(RecordID) & ")And((DateExpires>" & cpCore.db.encodeSQLDate(Now) & ")Or(DateExpires Is null))"
                Else
                    '
                    ' Multiple content records
                    '
                    result = "(ContentID In (" & Mid(Criteria, 2) & "))And(RecordID=" & cpCore.db.encodeSQLNumber(RecordID) & ")And((DateExpires>" & cpCore.db.encodeSQLDate(Now) & ")Or(DateExpires Is null))"
                End If
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return result
        End Function
        '
        '=====================================================================================================
        '   Clear the Approved Authoring Control
        '=====================================================================================================
        '
        Public Sub clearAuthoringControl(ByVal ContentName As String, ByVal RecordID As Integer, ByVal AuthoringControl As Integer, ByVal MemberID As Integer)
            Try
                Dim Criteria As String
                '
                Criteria = getAuthoringControlCriteria(ContentName, RecordID) & "And(ControlType=" & AuthoringControl & ")"
                Select Case AuthoringControl
                    Case AuthoringControlsEditing
                        Call cpCore.db.deleteContentRecords("Authoring Controls", Criteria & "And(CreatedBy=" & cpCore.db.encodeSQLNumber(MemberID) & ")", MemberID)
                    Case AuthoringControlsSubmitted, AuthoringControlsApproved, AuthoringControlsModified
                        Call cpCore.db.deleteContentRecords("Authoring Controls", Criteria, MemberID)
                End Select
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
        End Sub
        '
        '=====================================================================================================
        '   the Approved Authoring Control
        '=====================================================================================================
        '
        Public Sub setRecordLocking(ByVal ContentName As String, ByVal RecordID As Integer, ByVal AuthoringControl As Integer, ByVal MemberID As Integer)
            Try
                '
                Dim MethodName As String
                Dim ContentID As Integer
                'Dim TableID as integer
                Dim AuthoringCriteria As String
                Dim CSNewLock As Integer
                Dim CSCurrentLock As Integer
                Dim sqlCriteria As String
                Dim EditLockTimeoutDays As Double
                Dim EditLockTimeoutMinutes As Double
                Dim CDef As Models.Complex.cdefModel
                '
                MethodName = "csv_SetAuthoringControl"
                '
                CDef = Models.Complex.cdefModel.getCdef(cpCore, ContentName)
                ContentID = CDef.Id
                If ContentID <> 0 Then
                    'TableID = csv_GetContentTableID(ContentName)
                    AuthoringCriteria = getAuthoringControlCriteria(ContentName, RecordID)
                    Select Case AuthoringControl
                        Case AuthoringControlsEditing
                            EditLockTimeoutMinutes = EncodeNumber(cpCore.siteProperties.getText("EditLockTimeout", "5"))
                            If (EditLockTimeoutMinutes <> 0) Then
                                sqlCriteria = AuthoringCriteria & "And(ControlType=" & AuthoringControlsEditing & ")"
                                EditLockTimeoutDays = (EditLockTimeoutMinutes / 60 / 24)
                                '
                                ' Delete expired locks
                                '
                                Call cpCore.db.deleteContentRecords("Authoring Controls", sqlCriteria & "And(DATEEXPIRES<" & cpCore.db.encodeSQLDate(Now) & ")")
                                '
                                ' Select any lock left, only the newest counts
                                '
                                CSCurrentLock = cpCore.db.csOpen("Authoring Controls", sqlCriteria, "ID DESC", , MemberID, False, False)
                                If Not cpCore.db.csOk(CSCurrentLock) Then
                                    '
                                    ' No lock, create one
                                    '
                                    CSNewLock = cpCore.db.csInsertRecord("Authoring Controls", MemberID)
                                    If cpCore.db.csOk(CSNewLock) Then
                                        Call cpCore.db.csSet(CSNewLock, "RecordID", RecordID)
                                        Call cpCore.db.csSet(CSNewLock, "DateExpires", (Now.AddDays(EditLockTimeoutDays)))
                                        Call cpCore.db.csSet(CSNewLock, "ControlType", AuthoringControlsEditing)
                                        Call cpCore.db.csSet(CSNewLock, "ContentRecordKey", genericController.encodeText(ContentID & "." & RecordID))
                                        Call cpCore.db.csSet(CSNewLock, "ContentID", ContentID)
                                    End If
                                    Call cpCore.db.csClose(CSNewLock)
                                Else
                                    If (cpCore.db.csGetInteger(CSCurrentLock, "CreatedBy") = MemberID) Then
                                        '
                                        ' Record Locked by Member, update DateExpire
                                        '
                                        Call cpCore.db.csSet(CSCurrentLock, "DateExpires", (Now.AddDays(EditLockTimeoutDays)))
                                    End If
                                End If
                                Call cpCore.db.csClose(CSCurrentLock)
                            End If
                            'Case AuthoringControlsSubmitted, AuthoringControlsApproved, AuthoringControlsModified
                            '    If False And cpCore.siteProperties.allowWorkflowAuthoring Then
                            '        sqlCriteria = AuthoringCriteria & "And(ControlType=" & AuthoringControl & ")"
                            '        CSCurrentLock = cpCore.db.cs_open("Authoring Controls", sqlCriteria, "ID DESC", , MemberID, False, False)
                            '        If Not cpCore.db.cs_ok(CSCurrentLock) Then
                            '            '
                            '            ' Create new lock
                            '            '
                            '            CSNewLock = cpCore.db.cs_insertRecord("Authoring Controls", MemberID)
                            '            If cpCore.db.cs_ok(CSNewLock) Then
                            '                Call cpCore.db.cs_set(CSNewLock, "RecordID", RecordID)
                            '                Call cpCore.db.cs_set(CSNewLock, "ControlType", AuthoringControl)
                            '                Call cpCore.db.cs_set(CSNewLock, "ContentRecordKey", genericController.encodeText(ContentID & "." & RecordID))
                            '                Call cpCore.db.cs_set(CSNewLock, "ContentID", ContentID)
                            '            End If
                            '            Call cpCore.db.cs_Close(CSNewLock)
                            '        Else
                            '            '
                            '            ' Update current lock
                            '            '
                            '            'Call csv_SetCSField(CSCurrentLock, "ContentID", ContentID)
                            '            'Call csv_SetCSField(CSCurrentLock, "RecordID", RecordID)
                            '            'Call csv_SetCSField(CSCurrentLock, "ControlType", AuthoringControl)
                            '            'Call csv_SetCSField(CSCurrentLock, "ContentRecordKey", genericController.encodeText(ContentID & "." & RecordID))
                            '        End If
                            '        Call cpCore.db.cs_Close(CSCurrentLock)
                            '    End If
                    End Select
                End If
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
        End Sub
        '
        '=====================================================================================================
        '   the Approved Authoring Control
        '=====================================================================================================
        '
        Public Sub getAuthoringStatus(ByVal ContentName As String, ByVal RecordID As Integer, ByRef IsSubmitted As Boolean, ByRef IsApproved As Boolean, ByRef SubmittedName As String, ByRef ApprovedName As String, ByRef IsInserted As Boolean, ByRef IsDeleted As Boolean, ByRef IsModified As Boolean, ByRef ModifiedName As String, ByRef ModifiedDate As Date, ByRef SubmittedDate As Date, ByRef ApprovedDate As Date)
            Try
                Dim ContentID As Integer
                Dim CDef As Models.Complex.cdefModel
                '
                IsModified = False
                ModifiedName = ""
                ModifiedDate = Date.MinValue
                IsSubmitted = False
                SubmittedName = ""
                SubmittedDate = Date.MinValue
                IsApproved = False
                ApprovedName = ""
                ApprovedDate = Date.MinValue
                IsInserted = False
                IsDeleted = False
                If RecordID > 0 Then
                    '
                    ' Get Workflow Locks
                    '
                    CDef = Models.Complex.cdefModel.getCdef(cpCore, ContentName)
                    ContentID = CDef.Id
                    If ContentID > 0 Then
                        'If false And cpCore.siteProperties.allowWorkflowAuthoring Then
                        '    '
                        '    ' Check Authoring Controls record for Locks
                        '    '
                        '    'TableID = csv_GetContentTableID(ContentName)
                        '    Criteria = getAuthoringControlCriteria(ContentName, RecordID)
                        '    CSLocks = cpCore.db.cs_open("Authoring Controls", Criteria, "DateAdded Desc", , , , , "DateAdded,ControlType,CreatedBy,ID,DateExpires")
                        '    Do While cpCore.db.cs_ok(CSLocks)
                        '        ControlType = cpCore.db.cs_getInteger(CSLocks, "ControlType")
                        '        Select Case ControlType
                        '            Case AuthoringControlsModified
                        '                If Not IsModified Then
                        '                    ModifiedDate = cpCore.db.cs_getDate(CSLocks, "DateAdded")
                        '                    ModifiedName = cpCore.db.cs_get(CSLocks, "CreatedBy")
                        '                    IsModified = True
                        '                End If
                        '            Case AuthoringControlsSubmitted
                        '                If Not IsSubmitted Then
                        '                    SubmittedDate = cpCore.db.cs_getDate(CSLocks, "DateAdded")
                        '                    SubmittedName = cpCore.db.cs_get(CSLocks, "CreatedBy")
                        '                    IsSubmitted = True
                        '                End If
                        '            Case AuthoringControlsApproved
                        '                If Not IsApproved Then
                        '                    ApprovedDate = cpCore.db.cs_getDate(CSLocks, "DateAdded")
                        '                    ApprovedName = cpCore.db.cs_get(CSLocks, "CreatedBy")
                        '                    IsApproved = True
                        '                End If
                        '        End Select
                        '        cpCore.db.cs_goNext(CSLocks)
                        '    Loop
                        '    Call cpCore.db.cs_Close(CSLocks)
                        '    '
                        '    ContentTableName = CDef.ContentTableName
                        '    AuthoringTableName = CDef.AuthoringTableName
                        '    DataSourceName = CDef.ContentDataSourceName
                        '    SQL = "Select ContentTableName.ID As LiveRecordID, AuthoringTableName.ID As EditRecordID, AuthoringTableName.EditBlank As IsDeleted, ContentTableName.EditBlank As IsInserted, AuthoringTableName.ModifiedDate As EditRecordModifiedDate, ContentTableName.ModifiedDate As LiveRecordModifiedDate" _
                        '    & " FROM " & AuthoringTableName & " As AuthoringTableName RIGHT JOIN " & ContentTableName & " As ContentTableName On AuthoringTableName.EditSourceID = ContentTableName.ID" _
                        '    & " Where (((ContentTableName.ID) = " & RecordID & "))" _
                        '    & " ORDER BY AuthoringTableName.ID DESC;"
                        '    rs = cpCore.db.executeSql(SQL, DataSourceName)
                        '    If isDataTableOk(rs) Then
                        '        IsInserted = genericController.EncodeBoolean(cpCore.db.getDataRowColumnName(rs.Rows(0), "IsInserted"))
                        '        IsDeleted = genericController.EncodeBoolean(cpCore.db.getDataRowColumnName(rs.Rows(0), "IsDeleted"))
                        '        'IsModified = (getDataRowColumnName(RS.rows(0), "LiveRecordModifiedDate") <> getDataRowColumnName(RS.rows(0), "EditRecordModifiedDate"))
                        '    End If
                        '    Call closeDataTable(rs)
                        'End If
                    End If
                End If
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
        End Sub
        '
        '=================================================================================
        '   Depricate this
        '   Instead, use csv_IsRecordLocked2, which uses the Db
        '=================================================================================
        '
        Public Sub setEditLock(ByVal ContentName As String, ByVal RecordID As Integer, ByVal MemberID As Integer)
            Try
                Call setEditLock2(ContentName, RecordID, MemberID, False)
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
        End Sub
        '
        '=================================================================================
        '   Depricate this
        '   Instead, use csv_IsRecordLocked, which uses the Db
        '=================================================================================
        '
        Public Sub clearEditLock(ByVal ContentName As String, ByVal RecordID As Integer, ByVal MemberID As Integer)
            Try
                Call setEditLock2(ContentName, RecordID, MemberID, True)
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
        End Sub
        '
        '=================================================================================
        '   Depricate this
        '   Instead, use csv_IsRecordLocked, which uses the Db
        '=================================================================================
        '
        Public Function getEditLock(ByVal ContentName As String, ByVal RecordID As Integer, ByRef ReturnMemberID As Integer, ByRef ReturnDateExpires As Date) As Boolean
            Return getEditLock2(ContentName, RecordID, ReturnMemberID, ReturnDateExpires)
        End Function
        '
        '=================================================================================
        '
        '=================================================================================
        '
        Public Sub setEditLock2(ByVal ContentName As String, ByVal RecordID As Integer, ByVal MemberID As Integer, Optional ByVal ClearLock As Boolean = False)
            Try
                Dim SourcePointer As Integer
                Dim EditLockTimeoutMinutes As Double
                Dim LockFound As Boolean
                Dim EditLockDateExpires As Date
                Dim SourceKey As String
                Dim SourceDateExpires As Date
                Dim DestinationPointer As Integer
                Dim StringBuffer As String
                Dim EditLockKey2 As String
                '
                If (ContentName <> "") And (RecordID <> 0) Then
                    EditLockKey2 = genericController.vbUCase(ContentName & "," & CStr(RecordID))
                    StringBuffer = cpCore.siteProperties.getText("EditLockTimeout", "5")
                    EditLockTimeoutMinutes = EncodeNumber(StringBuffer)
                    EditLockDateExpires = DateTime.Now.AddMinutes(EditLockTimeoutMinutes)
                    If EditLockCount > 0 Then
                        For SourcePointer = 0 To EditLockCount - 1
                            If False Then
                                Exit For
                            End If
                            SourceKey = EditLockArray(SourcePointer).Key
                            SourceDateExpires = EditLockArray(SourcePointer).DateExpires
                            If SourceKey = EditLockKey2 Then
                                '
                                ' This edit lock was found
                                '
                                LockFound = True
                                If EditLockArray(SourcePointer).MemberID <> MemberID Then
                                    '
                                    ' This member did not create the lock, he can not change it either
                                    '
                                    If (SourcePointer <> DestinationPointer) Then
                                        EditLockArray(DestinationPointer).Key = SourceKey
                                        EditLockArray(DestinationPointer).MemberID = MemberID
                                        EditLockArray(DestinationPointer).DateExpires = SourceDateExpires
                                    End If
                                    DestinationPointer = DestinationPointer + 1
                                ElseIf Not ClearLock Then
                                    '
                                    ' This lock created by this member, he can change it
                                    '
                                    EditLockArray(DestinationPointer).Key = SourceKey
                                    EditLockArray(DestinationPointer).MemberID = MemberID
                                    EditLockArray(DestinationPointer).DateExpires = EditLockDateExpires
                                    DestinationPointer = DestinationPointer + 1
                                End If
                            ElseIf (SourceDateExpires >= EditLockDateExpires) Then
                                '
                                ' Lock not expired, move it if needed
                                '
                                If (SourcePointer <> DestinationPointer) Then
                                    EditLockArray(DestinationPointer).Key = SourceKey
                                    EditLockArray(DestinationPointer).MemberID = MemberID
                                    EditLockArray(DestinationPointer).DateExpires = SourceDateExpires
                                End If
                                DestinationPointer = DestinationPointer + 1
                            End If
                            '''DoEvents()
                        Next
                    End If
                    If (Not LockFound) And (Not ClearLock) Then
                        '
                        ' Lock not found, add it
                        '
                        If EditLockCount > 0 Then
                            If (DestinationPointer > UBound(EditLockArray)) Then
                                ReDim Preserve EditLockArray(DestinationPointer + 10)
                            End If
                        Else
                            ReDim Preserve EditLockArray(10)
                        End If
                        EditLockArray(DestinationPointer).Key = EditLockKey2
                        EditLockArray(DestinationPointer).MemberID = MemberID
                        EditLockArray(DestinationPointer).DateExpires = EditLockDateExpires
                        DestinationPointer = DestinationPointer + 1
                    End If
                    EditLockCount = DestinationPointer
                End If
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
        End Sub
        '
        '=================================================================================
        '   Returns true if this content/record is locked
        '       if true, the ReturnMemberID is the member that locked it
        '           and ReturnDteExpires is the date when it will be released
        '=================================================================================
        '
        Public Function getEditLock2(ByVal ContentName As String, ByVal RecordID As Integer, ByRef ReturnMemberID As Integer, ByRef ReturnDateExpires As Date) As Boolean
            Dim EditLock2 As Boolean = False
            Try
                Dim SourcePointer As Integer
                Dim EditLockKey2 As String
                Dim DateNow As Date
                '
                If (ContentName <> "") And (RecordID <> 0) And (EditLockCount > 0) Then
                    EditLockKey2 = genericController.vbUCase(ContentName & "," & CStr(RecordID))
                    DateNow = DateTime.Now
                    For SourcePointer = 0 To EditLockCount - 1
                        If (EditLockArray(SourcePointer).Key = EditLockKey2) Then
                            ReturnMemberID = EditLockArray(SourcePointer).MemberID
                            ReturnDateExpires = EditLockArray(SourcePointer).DateExpires
                            If ReturnDateExpires > Now() Then
                                EditLock2 = True
                            End If
                            Exit For
                        End If
                    Next
                End If
            Catch ex As Exception
                cpCore.handleException(ex) : Throw
            End Try
            Return EditLock2
        End Function

        '
        '==========================================================================================
#Region " IDisposable Support "
        Protected disposed As Boolean = False
        '
        '==========================================================================================
        ''' <summary>
        ''' dispose
        ''' </summary>
        ''' <param name="disposing"></param>
        ''' <remarks></remarks>
        Protected Overridable Overloads Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposed Then
                If disposing Then
                    '
                    ' ----- call .dispose for managed objects
                    '
                End If
                '
                ' Add code here to release the unmanaged resource.
                '
            End If
            Me.disposed = True
        End Sub
        ' Do not change or add Overridable to these methods.
        ' Put cleanup code in Dispose(ByVal disposing As Boolean).
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
        Protected Overrides Sub Finalize()
            Dispose(False)
            MyBase.Finalize()
        End Sub
#End Region
    End Class
End Namespace
