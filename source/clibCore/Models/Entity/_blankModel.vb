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
    '
    '====================================================================================================
    ' entity model pattern
    '   factory pattern load because if a record is not found, must rturn nothing
    '   new() - empty constructor to allow deserialization
    '   saveObject() - saves instance properties (nonstatic method)
    '   create() - loads instance properties and returns a model 
    '   delete() - deletes the record that matches the argument
    '   getObjectList() - a pattern for creating model lists.
    '   invalidateFIELDNAMEcache() - method to invalide the model cache. One per cache
    '
    '	1) set the primary content name in const cnPrimaryContent. avoid constants Like cnAddons used outside model
    '	2) find-And-replace "_blankModel" with the name for this model
    '	3) when adding model fields, add in three places: the Public Property, the saveObject(), the loadObject()
    '	4) when adding create() methods to support other fields/combinations of fields, 
    '       - add a secondary cache For that new create method argument in loadObjec()
    '       - add it to the injected cachename list in loadObject()
    '       - add an invalidate
    '
    ' Model Caching
    '   caching applies to model objects only, not lists of models (for now)
    '       - this is because of the challenge of invalidating the list object when individual records are added or deleted
    '
    '   a model should have 1 primary cache object which stores the data and can have other secondary cacheObjects which do not hold data
    '    the cacheName of the 'primary' cacheObject for models and db records (cacheNamePrefix + ".id." + #id)
    '    'secondary' cacheName is (cacheNamePrefix + . + fieldName + . + #)
    '
    '   cacheobjects can be used to hold data (primary cacheobjects), or to hold only metadata (secondary cacheobjects)
    '       - primary cacheobjects are like 'personModel.id.99' that holds the model for id=99
    '           - it is primary because the .primaryobject is null
    '           - invalidationData. This cacheobject is invalid after this datetime
    '           - dependentobjectlist() - this object is invalid if any of those objects are invalid
    '       - secondary cachobjects are like 'person.ccguid.12345678'. It does not hold data, just a reference to the primary cacheobject
    '
    '   cacheNames spaces are replaced with underscores, so "addon collections" should be addon_collections
    '
    '   cacheNames that match content names are treated as caches of "any" record in the content, so invalidating "people" can be used to invalidate
    '       any non-specific cache in the people table, by including "people" as a dependant cachename. the "people" cachename should not clear
    '       specific people caches, like people.id.99, but can be used to clear lists of records like "staff_list_group"
    '       - this can be used as a fallback strategy to cache record lists: a remote method list can be cached with a dependancy on "add-ons".
    '       - models should always clear this content name cache entry on all cache clears
    '
    '   when a model is created, the code first attempts to read the model's cacheobject. if it fails, it builds it and saves the cache object and tags
    '       - when building the model, is writes object to the primary cacheobject, and writes all the secondaries to be used
    '       - when building the model, if a database record is opened, a dependantObject Tag is created for the tablename+'id'+id
    '       - when building the model, if another model is added, that model returns its cachenames in the cacheNameList to be added as dependentObjects
    '
    '
    Public Class _blankModel
        '
        '-- const
        Public Const primaryContentName As String = "" '<------ set content name
        Private Const primaryContentTableName As String = "" '<------ set to tablename for the primary content (used for cache names)
        Private Const primaryContentDataSource As String = "default" '<----- set to datasource if not default
        '
        ' -- instance properties
        Public id As Integer
        Public name As String
        Public ccguid As String
        '
        Public foreignKey1Id As Integer ' <-- DELETE - sample field for create/delete patterns
        Public foreignKey2Id As Integer ' <-- DELETE - sample field for create/delete patterns
        '
        ' -- publics not exposed to the UI (test/internal data)
        <JsonIgnore> Public createKey As Integer
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
        ''' <param name="callersCacheNameList"></param>
        ''' <returns></returns>
        Public Shared Function add(cpCore As coreClass, ByRef callersCacheNameList As List(Of String)) As _blankModel
            Dim result As _blankModel = Nothing
            Try
                result = create(cpCore, cpCore.db.metaData_InsertContentRecordGetID(primaryContentName, cpCore.authContext.user.ID), callersCacheNameList)
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
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
        ''' <param name="callersCacheNameList">Any cachenames effected by this record will be added to this list. If the method consumer creates a cache object, add these cachenames to its dependent cachename list.</param>
        Public Shared Function create(cpCore As coreClass, recordId As Integer, ByRef callersCacheNameList As List(Of String)) As _blankModel
            Dim result As _blankModel = Nothing
            Try
                If recordId > 0 Then
                    Dim cacheName As String = GetType(_blankModel).FullName & getCacheName("id", recordId.ToString())
                    result = cpCore.cache.getObject(Of _blankModel)(cacheName)
                    If (result Is Nothing) Then
                        result = loadObject(cpCore, "id=" & recordId.ToString(), callersCacheNameList)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
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
        Public Shared Function create(cpCore As coreClass, recordGuid As String, ByRef callersCacheNameList As List(Of String)) As _blankModel
            Dim result As _blankModel = Nothing
            Try
                If Not String.IsNullOrEmpty(recordGuid) Then
                    Dim cacheName As String = GetType(_blankModel).FullName & getCacheName("ccguid", recordGuid)
                    result = cpCore.cache.getObject(Of _blankModel)(cacheName)
                    If (result Is Nothing) Then
                        result = loadObject(cpCore, "ccGuid=" & cpCore.db.encodeSQLText(recordGuid), callersCacheNameList)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
                Throw
            End Try
            Return result
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' template for open an existing object with multiple keys (like a rule)
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="foreignKey1Id"></param>
        Public Shared Function create(cpCore As coreClass, foreignKey1Id As Integer, foreignKey2Id As Integer, ByRef callersCacheNameList As List(Of String)) As _blankModel
            Dim result As _blankModel = Nothing
            Try
                If ((foreignKey1Id > 0) And (foreignKey2Id > 0)) Then
                    result = cpCore.cache.getObject(Of _blankModel)(getCacheName("foreignKey1", foreignKey1Id.ToString(), "foreignKey2", foreignKey2Id.ToString()))
                    If (result Is Nothing) Then
                        result = loadObject(cpCore, "(foreignKey1=" & foreignKey1Id.ToString() & ")and(foreignKey1=" & foreignKey1Id.ToString() & ")", callersCacheNameList)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
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
        Private Shared Function loadObject(cpCore As coreClass, sqlCriteria As String, ByRef callersCacheNameList As List(Of String)) As _blankModel
            Dim result As _blankModel = Nothing
            Try
                Dim cs As New csController(cpCore)
                If cs.open(primaryContentName, sqlCriteria) Then
                    result = New _blankModel
                    With result
                        '
                        ' -- populate result model
                        .id = cs.getInteger("id")
                        .name = cs.getText("name")
                        .ccguid = cs.getText("ccGuid")
                        .createKey = cs.getInteger("createKey")
                    End With
                    If (result IsNot Nothing) Then
                        '
                        ' -- set primary cache to the object created
                        ' -- set secondary caches to the primary cache
                        ' -- add all cachenames to the injected cachenamelist
                        Dim cacheName0 As String = getCacheName("id", result.id.ToString())
                        callersCacheNameList.Add(cacheName0)
                        cpCore.cache.setObject(cacheName0, result)
                        '
                        Dim cacheName1 As String = getCacheName("ccguid", result.ccguid)
                        callersCacheNameList.Add(cacheName1)
                        cpCore.cache.setSecondaryObject(cacheName1, cacheName0)
                        '
                        Dim cacheName2 As String = getCacheName("foreignKey1", result.foreignKey1Id.ToString(), "foreignKey2", result.foreignKey2Id.ToString())
                        callersCacheNameList.Add(cacheName2)
                        cpCore.cache.setSecondaryObject(cacheName2, cacheName0)
                    End If
                End If
                Call cs.Close()
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
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
                If (id > 0) Then
                    If Not cs.open(primaryContentName, "id=" & id) Then
                        Dim message As String = "Unable to open record in content [" & primaryContentName & "], with id [" & id & "]"
                        cs.Close()
                        id = 0
                        Throw New ApplicationException(message)
                    End If
                Else
                    If Not cs.Insert(primaryContentName) Then
                        cs.Close()
                        id = 0
                        Throw New ApplicationException("Unable to insert record in content [" & primaryContentName & "]")
                    End If
                End If
                If cs.ok() Then
                    id = cs.getInteger("id")
                    If (String.IsNullOrEmpty(ccguid)) Then
                        ccguid = Controllers.genericController.getGUID()
                    End If
                    Call cs.setField("name", name)
                    Call cs.setField("ccGuid", ccguid)
                    Call cs.setField("createKey", createKey.ToString())
                End If
                Call cs.Close()
                '
                ' -- invalidate objects
                cpCore.cache.invalidateObject(getCacheName("id", id.ToString))
                cpCore.cache.invalidateObject(getCacheName("ccguid", ccguid))
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
                Throw
            End Try
            Return id
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' delete an existing database record by id
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="recordId"></param>
        Public Shared Sub delete(cpCore As coreClass, recordId As Integer)
            Try
                If (recordId > 0) Then
                    cpCore.db.deleteContentRecords(primaryContentName, "id=" & recordId.ToString)
                    cpCore.cache.invalidateObject(getCacheName("id", recordId.ToString))
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
                Throw
            End Try
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' delete an existing database record by guid
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="ccguid"></param>
        Public Shared Sub delete(cpCore As coreClass, ccguid As String)
            Try
                If (Not String.IsNullOrEmpty(ccguid)) Then
                    Dim instance As _blankModel = create(cpCore, ccguid, New List(Of String))
                    If (instance IsNot Nothing) Then
                        invalidatePrimaryCache(cpCore, instance.id)
                        cpCore.db.deleteContentRecords(primaryContentName, "(ccguid=" & cpCore.db.encodeSQLText(ccguid) & ")")
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
                Throw
            End Try
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' pattern to delete an existing object based on multiple criteria (like a rule record)
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="foreignKey1Id"></param>
        ''' <param name="foreignKey2Id"></param>
        Public Shared Sub delete(cpCore As coreClass, foreignKey1Id As Integer, foreignKey2Id As Integer)
            Try
                If (foreignKey2Id > 0) And (foreignKey1Id > 0) Then
                    Dim instance As _blankModel = create(cpCore, foreignKey1Id, foreignKey2Id, New List(Of String))
                    If (instance IsNot Nothing) Then
                        invalidatePrimaryCache(cpCore, instance.id)
                        cpCore.db.deleteTableRecord(primaryContentTableName, instance.id, primaryContentDataSource)
                    End If
                End If
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
                Throw
            End Try
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' pattern get a list of objects from this model
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="someCriteria"></param>
        ''' <returns></returns>
        Public Shared Function getObjectList(cpCore As coreClass, someCriteria As Integer, callersCacheNameList As List(Of String)) As List(Of _blankModel)
            Dim result As New List(Of _blankModel)
            Try
                Dim cs As New csController(cpCore)
                Dim ignoreCacheNames As New List(Of String)
                If (cs.open(primaryContentName, "(someCriteria=" & someCriteria & ")", "name", True, "id")) Then
                    Dim instance As _blankModel
                    Do
                        instance = _blankModel.create(cpCore, cs.getInteger("id"), callersCacheNameList)
                        If (instance IsNot Nothing) Then
                            result.Add(instance)
                        End If
                        cs.goNext()
                    Loop While cs.ok()
                End If
                cs.Close()
            Catch ex As Exception
                cpCore.handleExceptionAndRethrow(ex)
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
        Public Shared Sub invalidatePrimaryCache(cpCore As coreClass, recordId As Integer)
            cpCore.cache.invalidateObject(getCacheName("id", recordId.ToString))
            '
            ' -- the zero record cache means any record was updated. Can be used to invalidate arbitraty lists of records in the table
            cpCore.cache.invalidateObject(getCacheName("id", "0"))
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' produce a standard format cachename for this model
        ''' </summary>
        ''' <param name="fieldName"></param>
        ''' <param name="fieldValue"></param>
        ''' <returns></returns>
        Private Shared Function getCacheName(fieldName As String, fieldValue As String) As String
            Return (primaryContentTableName & "-" & fieldName & "." & fieldValue).ToLower().Replace(" ", "_")
        End Function
        '
        Private Shared Function getCacheName(field1Name As String, field1Value As String, field2Name As String, field2Value As String) As String
            Return (primaryContentTableName & "-" & field1Name & "." & field1Value & "-" & field2Name & "." & field2Value).ToLower().Replace(" ", "_")
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' get the name of the record by it's id
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="recordId"></param>record
        ''' <returns></returns>
        Public Shared Function getRecordName(cpcore As coreClass, recordId As Integer) As String
            Return _blankModel.create(cpcore, recordId, New List(Of String)).name
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' get the name of the record by it's guid 
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="ccGuid"></param>record
        ''' <returns></returns>
        Public Shared Function getRecordName(cpcore As coreClass, ccGuid As String) As String
            Return _blankModel.create(cpcore, ccGuid, New List(Of String)).name
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' get the id of the record by it's guid 
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <param name="ccGuid"></param>record
        ''' <returns></returns>
        Public Shared Function getRecordId(cpcore As coreClass, ccGuid As String) As Integer
            Return _blankModel.create(cpcore, ccGuid, New List(Of String)).id
        End Function
        '
        '====================================================================================================
        '
        Public Shared Function getDefault(cpcore As coreClass) As _blankModel
            Dim instance As New _blankModel
            Try
                Dim CDef As coreMetaDataClass.CDefClass = cpcore.metaData.getCdef(primaryContentName)
                If (CDef Is Nothing) Then
                    Throw New ApplicationException("content [" & primaryContentName & "] could Not be found.")
                ElseIf (CDef.Id <= 0) Then
                    Throw New ApplicationException("content [" & primaryContentName & "] could Not be found.")
                Else
                    With CDef
                        instance.ccguid = .fields("ccguid").defaultValue
                        instance.createKey = genericController.EncodeInteger(.fields("createKey").defaultValue)
                        instance.name = .fields("name").defaultValue
                    End With
                End If
            Catch ex As Exception
                cpcore.handleExceptionAndContinue(ex)
            End Try
            Return instance
        End Function
    End Class
End Namespace
