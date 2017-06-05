﻿
Option Explicit On
Option Strict On

Imports System
Imports System.Collections.Generic
Imports System.Text
Imports Contensive.BaseClasses
Imports Contensive.Core.Controllers
Imports Newtonsoft.Json

Namespace Contensive.Core.Models.Entity
    Public Class visitorModel
        '
        '-- const
        Public Const primaryContentName As String = "visitors" '<------ set content name
        Private Const primaryContentTableName As String = "ccvisitors" '<------ set to tablename for the primary content (used for cache names)
        Private Const primaryContentDataSource As String = "default" '<----- set to datasource if not default
        '
        ' -- instance properties
        Public ID As Integer
        Public Active As Boolean
        Public ccGuid As String
        Public ContentCategoryID As Integer
        Public ContentControlID As Integer
        Public CreatedBy As Integer
        Public CreateKey As Integer
        Public DateAdded As Date
        Public EditArchive As Boolean
        Public EditBlank As Boolean
        Public EditSourceID As Integer
        Public ForceBrowserMobile As Integer
        Public MemberID As Integer
        Public ModifiedBy As Integer
        Public ModifiedDate As Date
        Public Name As String
        Public OrderID As Integer
        Public SortOrder As String
        'Public id As Integer
        'Public name As String
        'Public guid As String
        ''
        'Public memberID As Integer = 0              ' the last member account this visitor used (memberid=0 means untracked guest)
        'Public orderID As Integer = 0               ' the current shopping cart (non-complete order)
        'Public newVisitor As Boolean = False               ' stored in visit record - Is this the first visit for this visitor
        'Public forceBrowserMobile As Integer = 0           ' 0 = not set -- use Browser detect each time, 1 = Force Mobile, 2 = Force not Mobile
        ''
        '' -- publics not exposed to the UI (test/internal data)
        '<JsonIgnore> Public createKey As Integer
        '
        '====================================================================================================
        ''' <summary>
        ''' Create an empty object. needed for deserialization
        ''' </summary>
        Public Sub New()
            '
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' add a new recod to the db and open it. Starting a new model with this method will use the default
        ''' values in Contensive metadata (active, contentcontrolid, etc)
        ''' </summary>
        ''' <param name="cpCore"></param>
        ''' <param name="cacheNameList"></param>
        ''' <returns></returns>
        Public Shared Function add(cpCore As coreClass, ByRef cacheNameList As List(Of String)) As visitorModel
            Dim result As visitorModel = Nothing
            Try
                result = create(cpCore, cpCore.db.metaData_InsertContentRecordGetID(primaryContentName, 0), cacheNameList)
            Catch ex As Exception
                cpCore.handleExceptionAndContinue(ex) : Throw
                Throw
            End Try
            Return result
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' return a new model with the data selected. All cacheNames related to the object will be added to the cacheNameList.
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="recordId">The id of the record to be read into the new object</param>
        ''' <param name="cacheNameList">Any cachenames effected by this record will be added to this list. If the method consumer creates a cache object, add these cachenames to its dependent cachename list.</param>
        Public Shared Function create(cpCore As coreClass, recordId As Integer, ByRef cacheNameList As List(Of String)) As visitorModel
            Dim result As visitorModel = Nothing
            Try
                If recordId > 0 Then
                    Dim cacheName As String = Controllers.cacheController.getDbRecordCacheName(primaryContentTableName, "id", recordId.ToString())
                    result = cpCore.cache.getObject(Of visitorModel)(cacheName)
                    If (result Is Nothing) Then
                        result = loadObject(cpCore, "id=" & recordId.ToString(), cacheNameList)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndContinue(ex) : Throw
                Throw
            End Try
            Return result
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' open an existing object
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="recordGuid"></param>
        Public Shared Function create(cpCore As coreClass, recordGuid As String, ByRef cacheNameList As List(Of String)) As visitorModel
            Dim result As visitorModel = Nothing
            Try
                If Not String.IsNullOrEmpty(recordGuid) Then
                    Dim cacheName As String = Controllers.cacheController.getDbRecordCacheName(primaryContentTableName, "ccguid", recordGuid)
                    result = cpCore.cache.getObject(Of visitorModel)(cacheName)
                    If (result Is Nothing) Then
                        result = loadObject(cpCore, "ccGuid=" & cpCore.db.encodeSQLText(recordGuid), cacheNameList)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndContinue(ex) : Throw
                Throw
            End Try
            Return result
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' open an existing object
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="sqlCriteria"></param>
        Private Shared Function loadObject(cpCore As coreClass, sqlCriteria As String, ByRef callersCacheNameList As List(Of String)) As visitorModel
            Dim result As visitorModel = Nothing
            Try
                Dim cs As New csController(cpCore)
                If cs.open(primaryContentName, sqlCriteria) Then
                    result = New visitorModel
                    With result
                        '
                        ' -- populate result model
                        .ID = cs.getInteger("ID")
                        .Active = cs.getBoolean("Active")
                        .ccGuid = cs.getText("ccGuid")
                        .ContentCategoryID = cs.getInteger("ContentCategoryID")
                        .ContentControlID = cs.getInteger("ContentControlID")
                        .CreatedBy = cs.getInteger("CreatedBy")
                        .CreateKey = cs.getInteger("CreateKey")
                        .DateAdded = cs.getDate("DateAdded")
                        .EditArchive = cs.getBoolean("EditArchive")
                        .EditBlank = cs.getBoolean("EditBlank")
                        .EditSourceID = cs.getInteger("EditSourceID")
                        .ForceBrowserMobile = cs.getInteger("ForceBrowserMobile")
                        .MemberID = cs.getInteger("MemberID")
                        .ModifiedBy = cs.getInteger("ModifiedBy")
                        .ModifiedDate = cs.getDate("ModifiedDate")
                        .Name = cs.getText("Name")
                        .OrderID = cs.getInteger("OrderID")
                        .SortOrder = cs.getText("SortOrder")
                        If (String.IsNullOrEmpty(.ccGuid)) Then .ccGuid = Controllers.genericController.getGUID()
                    End With
                    If (result IsNot Nothing) Then
                        '
                        ' -- set primary and secondary caches
                        ' -- add all cachenames to the injected cachenamelist
                        Dim cacheName0 As String = Controllers.cacheController.getDbRecordCacheName(primaryContentTableName, "id", result.ID.ToString())
                        callersCacheNameList.Add(cacheName0)
                        cpCore.cache.setObject(cacheName0, result)
                        '
                        Dim cacheName1 As String = Controllers.cacheController.getDbRecordCacheName(primaryContentTableName, "ccguid", result.ccGuid)
                        callersCacheNameList.Add(cacheName1)
                        cpCore.cache.setSecondaryObject(cacheName1, cacheName0)
                    End If
                End If
                Call cs.Close()
            Catch ex As Exception
                cpCore.handleExceptionAndContinue(ex) : Throw
                Throw
            End Try
            Return result
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' save the instance properties to a record with matching id. If id is not provided, a new record is created.
        ''' </summary>
        ''' <param name="cpCore"></param>
        ''' <returns></returns>
        Public Function saveObject(cpCore As coreClass) As Integer
            Try
                Dim cs As New csController(cpCore)
                If (ID > 0) Then
                    If Not cs.open(primaryContentName, "id=" & ID) Then
                        Dim message As String = "Unable to open record in content [" & primaryContentName & "], with id [" & ID & "]"
                        cs.Close()
                        ID = 0
                        Throw New ApplicationException(message)
                    End If
                Else
                    If Not cs.Insert(primaryContentName) Then
                        cs.Close()
                        ID = 0
                        Throw New ApplicationException("Unable to insert record in content [" & primaryContentName & "]")
                    End If
                End If
                If cs.ok() Then
                    ID = cs.getInteger("id")
                    If (String.IsNullOrEmpty(ccGuid)) Then
                        ccGuid = Controllers.genericController.getGUID()
                    End If
                    If (String.IsNullOrEmpty(Name)) Then
                        Name = "Visitor " & ID.ToString()
                    End If
                    cs.setField("Active", Active.ToString())
                    cs.setField("ccGuid", ccGuid)
                    cs.setField("ContentCategoryID", ContentCategoryID.ToString())
                    cs.setField("ContentControlID", ContentControlID.ToString())
                    cs.setField("CreatedBy", CreatedBy.ToString())
                    cs.setField("CreateKey", CreateKey.ToString())
                    cs.setField("DateAdded", DateAdded.ToString())
                    cs.setField("EditArchive", EditArchive.ToString())
                    cs.setField("EditBlank", EditBlank.ToString())
                    cs.setField("EditSourceID", EditSourceID.ToString())
                    cs.setField("ForceBrowserMobile", ForceBrowserMobile.ToString())
                    cs.setField("MemberID", MemberID.ToString())
                    cs.setField("ModifiedBy", ModifiedBy.ToString())
                    cs.setField("ModifiedDate", ModifiedDate.ToString())
                    cs.setField("Name", Name)
                    cs.setField("OrderID", OrderID.ToString())
                    cs.setField("SortOrder", SortOrder)
                End If
                Call cs.Close()
                '
                ' -- invalidate objects
                ' -- no, the primary is invalidated by the cs.save()
                'cpCore.cache.invalidateObject(controllers.cacheController.getModelCacheName(primaryContentTablename,"id", id.ToString))
                ' -- no, the secondary points to the pirmary, which is invalidated. Dont waste resources invalidating
                'cpCore.cache.invalidateObject(controllers.cacheController.getModelCacheName(primaryContentTablename,"ccguid", ccguid))
                '
                ' -- object is here, but the cache was invalidated, setting
                cpCore.cache.setObject(Controllers.cacheController.getDbRecordCacheName(primaryContentTableName, "id", Me.ID.ToString()), Me)
            Catch ex As Exception
                cpCore.handleExceptionAndContinue(ex) : Throw
                Throw
            End Try
            Return ID
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' delete an existing database record
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="recordId"></param>
        Public Shared Sub delete(cpCore As coreClass, recordId As Integer)
            Try
                If (recordId > 0) Then
                    cpCore.db.deleteContentRecords(primaryContentName, "id=" & recordId.ToString)
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndContinue(ex) : Throw
                Throw
            End Try
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' delete an existing database record
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="recordId"></param>
        Public Shared Sub delete(cpCore As coreClass, guid As String)
            Try
                If (Not String.IsNullOrEmpty(guid)) Then
                    cpCore.db.deleteContentRecords(primaryContentName, "(ccguid=" & cpCore.db.encodeSQLText(guid) & ")")
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndContinue(ex) : Throw
                Throw
            End Try
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' get a list of objects from this model
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="someCriteria"></param>
        ''' <returns></returns>
        Public Shared Function getObjectList(cpCore As coreClass, someCriteria As Integer) As List(Of visitorModel)
            Dim result As New List(Of visitorModel)
            Try
                Dim cs As New csController(cpCore)
                Dim ignoreCacheNames As New List(Of String)
                If (cs.open(primaryContentName, "(someCriteria=" & someCriteria & ")", "name", True, "id")) Then
                    Dim instance As visitorModel
                    Do
                        instance = visitorModel.create(cpCore, cs.getInteger("id"), ignoreCacheNames)
                        If (instance IsNot Nothing) Then
                            result.Add(instance)
                        End If
                        cs.goNext()
                    Loop While cs.ok()
                End If
                cs.Close()
            Catch ex As Exception
                cpCore.handleExceptionAndContinue(ex) : Throw
            End Try
            Return result
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' invalidate the primary key (which depends on all secondary keys)
        ''' </summary>
        ''' <param name="cpCore"></param>
        ''' <param name="recordId"></param>
        Public Shared Sub invalidateIdCache(cpCore As coreClass, recordId As Integer)
            cpCore.cache.invalidateObject(Controllers.cacheController.getDbRecordCacheName(primaryContentTableName, "id", recordId.ToString))
            '
            ' -- always clear the cache with the content name
            '?? cpCore.cache.invalidateObject(primaryContentName)
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' invalidate a secondary key (ccGuid field).
        ''' </summary>
        ''' <param name="cpCore"></param>
        ''' <param name="guid"></param>
        Public Shared Sub invalidateGuidCache(cpCore As coreClass, guid As String)
            cpCore.cache.invalidateObject(Controllers.cacheController.getDbRecordCacheName(primaryContentTableName, "ccguid", guid))
            '
            ' -- always clear the cache with the content name
            '?? cpCore.cache.invalidateObject(primaryContentName)
        End Sub
        ''
        ''====================================================================================================
        '''' <summary>
        '''' produce a standard format cachename for this model
        '''' </summary>
        '''' <param name="fieldName"></param>
        '''' <param name="fieldValue"></param>
        '''' <returns></returns>
        'Private Shared Function Controllers.cacheController.getModelCacheName( primaryContentTableName,fieldName As String, fieldValue As String) As String
        '    Return (primaryContentTableName & "." & fieldName & "." & fieldValue).ToLower().Replace(" ", "_")
        'End Function
        '
        '====================================================================================================
        '
        Public Shared Function getDefault(cpcore As coreClass) As visitorModel
            Dim instance As New visitorModel
            Try
                Dim CDef As cdefModel = cpcore.metaData.getCdef(primaryContentName)
                If (CDef Is Nothing) Then
                    Throw New ApplicationException("content [" & primaryContentName & "] could Not be found.")
                ElseIf (CDef.Id <= 0) Then
                    Throw New ApplicationException("content [" & primaryContentName & "] could Not be found.")
                Else
                    With CDef
                        instance.Active = genericController.EncodeBoolean(.fields("Active").defaultValue)
                        instance.ccGuid = genericController.encodeText(.fields("ccGuid").defaultValue)
                        instance.ContentCategoryID = CDef.Id
                        instance.ContentControlID = genericController.EncodeInteger(.fields("ContentControlID").defaultValue)
                        instance.CreatedBy = genericController.EncodeInteger(.fields("CreatedBy").defaultValue)
                        instance.CreateKey = genericController.EncodeInteger(.fields("CreateKey").defaultValue)
                        instance.DateAdded = genericController.EncodeDate(.fields("DateAdded").defaultValue)
                        instance.EditArchive = genericController.EncodeBoolean(.fields("EditArchive").defaultValue)
                        instance.EditBlank = genericController.EncodeBoolean(.fields("EditBlank").defaultValue)
                        instance.EditSourceID = genericController.EncodeInteger(.fields("EditSourceID").defaultValue)
                        instance.ForceBrowserMobile = genericController.EncodeInteger(.fields("ForceBrowserMobile").defaultValue)
                        instance.MemberID = genericController.EncodeInteger(.fields("MemberID").defaultValue)
                        instance.ModifiedBy = genericController.EncodeInteger(.fields("ModifiedBy").defaultValue)
                        instance.ModifiedDate = genericController.EncodeDate(.fields("ModifiedDate").defaultValue)
                        instance.Name = genericController.encodeText(.fields("Name").defaultValue)
                        instance.OrderID = genericController.EncodeInteger(.fields("OrderID").defaultValue)
                        instance.SortOrder = genericController.encodeText(.fields("SortOrder").defaultValue)
                    End With
                End If
            Catch ex As Exception
                cpcore.handleExceptionAndContinue(ex)
            End Try
            Return instance
        End Function
        '        '
        '        ' LEGACY CODE =============================================================================
        '        '   Save Visitor
        '        '
        '        '   Saves changes to the visitor record back to the database. Should be called
        '        '   before exit of anypage if anything here changes
        '        '=============================================================================
        '        '
        '        Public Sub saveObject(cpcore As coreClass)
        '            On Error GoTo ErrorTrap ''Dim th as integer : th = profileLogMethodEnter("SaveVisitor")
        '            '
        '            'If Not (true) Then Exit Sub
        '            '
        '            Dim SQL As String
        '            Dim MethodName As String
        '            '
        '            MethodName = "main_SaveVisitor"
        '            '
        '            If cpcore.visit.visit_initialized Then
        '                If True Then
        '                    SQL = "UPDATE ccVisitors SET " _
        '                        & " Name = " & cpcore.db.encodeSQLText(name) _
        '                        & ",MemberID = " & cpcore.db.encodeSQLNumber(memberID) _
        '                        & ",OrderID = " & cpcore.db.encodeSQLNumber(orderID) _
        '                        & ",ForceBrowserMobile = " & cpcore.db.encodeSQLNumber(forceBrowserMobile) _
        '                        & " WHERE ID=" & id & ";"
        '                Else
        '                    SQL = "UPDATE ccVisitors SET " _
        '                        & " Name = " & cpcore.db.encodeSQLText(name) _
        '                        & ",MemberID = " & cpcore.db.encodeSQLNumber(memberID) _
        '                        & ",OrderID = " & cpcore.db.encodeSQLNumber(orderID) _
        '                        & " WHERE ID=" & id & ";"
        '                End If
        '                Call cpcore.db.executeSql(SQL)
        '            End If
        '            Exit Sub
        '            '
        '            ' ----- Error Trap
        '            '
        'ErrorTrap:
        '            throw new applicationException("Unexpected exception") ' Call cpcore.handleLegacyError18(MethodName)
        '            '
        '        End Sub
    End Class
End Namespace
