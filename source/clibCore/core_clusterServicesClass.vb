﻿
Option Explicit On
Option Strict On

Imports System.Data.SqlClient
Imports System.Configuration
Imports Contensive.Core.ccCommonModule
Imports System.Runtime.InteropServices
Imports Enyim.Caching
Imports Amazon.ElastiCacheCluster
Imports System.Text.RegularExpressions

Namespace Contensive.Core
    '
    '====================================================================================================
    ''' <summary>
    ''' application configuration class
    ''' </summary>
    Public Class appConfigClass
        Public name As String
        Public enabled As Boolean
        Public privateKey As String                     ' rename hashKey
        Public defaultConnectionString As String
        Public appRootPath As String                    ' path relative to clusterPhysicalPath
        Public cdnFilesPath As String                   ' path relative to clusterPhysicalPath
        Public privateFilesPath As String               ' path relative to clusterPhysicalPath
        Public cdnFilesNetprefix As String              ' in some cases (like legacy), cdnFiles are iis virtual folder mapped to appRoot (/files/). Some cases this is a URL (http:\\cdn.domain.com pointing to s3)
        Public allowSiteMonitor As Boolean
        Public domainList As New List(Of String)        ' primary domain is the first item in the list
        Public enableCache As Boolean
        Public adminRoute As String                                          ' The url pathpath that executes the addon site
    End Class    '
    '
    '====================================================================================================
    ''' <summary>
    ''' cluster configuration class - deserialized configration file
    ''' </summary>
    ''' <remarks></remarks>
    Public Class clusterConfigClass
        Public isLocal As Boolean = True
        Public name As String = ""
        '
        ' local caching using dotnet framework, flushes on appPool
        '
        Public isLocalCache As Boolean = False
        '
        ' AWS dotnet elaticcache client wraps enyim, and provides node autodiscovery through the configuration object.
        ' this is the srver:port to the config file it uses.
        '
        Public awsElastiCacheConfigurationEndpoint As String
        '
        ' datasource for the cluster
        '
        Public defaultDataSourceType As dataSourceTypeEnum
        '
        ' odbc
        '
        Public defaultDataSourceODBCConnectionString As String
        '
        ' native
        '
        Public defaultDataSourceAddress As String = ""
        '
        ' user for creating new databases, and creating the new user for the database during site create, and saved to appconfig
        '
        Public defaultDataSourceUsername As String = ""
        Public defaultDataSourcePassword As String = ""
        '
        ' endpoint for cluster files (not sure how it works, maybe this will be an object taht includes permissions, for now an fpo)
        '
        Public clusterFilesEndpoint As String
        '
        ' configuration of async command listener on render machines (not sure if used still)
        '
        Public serverListenerPort As Integer = Port_ContentServerControlDefault
        Public maxCmdInstances As Integer = 5
        ' ayncCmd server authentication -- change this to a key later
        Public username As String = ""
        Public password As String = ""
        '
        ' This is the root path to the localCluster files, typically getLocalDataFolder (d:\cfw)
        '   if isLocal, the cluster runs from these files
        '   if not, this is the local mirror of the cluster files
        '
        Public clusterPhysicalPath As String
        '
        'Public domainRoutes As Dictionary(Of String, String)
        '
        Public appPattern As String
        '
        '
        '
        Public apps As New Dictionary(Of String, appConfigClass)
    End Class
    '====================================================================================================
    ''' <summary>
    ''' cluster srervices - properties and methods to maintain the cluster. Applications do not have access to this. 
    ''' </summary>
    ''' <remarks></remarks>
    Public Class clusterServicesClass
        Implements IDisposable
        '
        ' the cp parent for this object
        '
        Private cpCore As cpCoreClass
        '
        Public config As clusterConfigClass
        Public files As fileSystemClass
        Private mc As Enyim.Caching.MemcachedClient
        '
        '========================================================================
        ''' <summary>
        ''' Constructor builds data. read from cache and deserialize, if not in cache, build it from scratch, eventually, setup public properties as indivisual lazyCache
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <remarks></remarks>
        Friend Sub New(cpCore As cpCoreClass)
            '
            ' called during core constructor - so cp.core is not valid
            '
            MyBase.New()
            Me.cpCore = cpCore
            Try
                Dim json_serializer As New System.Web.Script.Serialization.JavaScriptSerializer
                Dim JSONTemp As String
                Dim cacheConfig As Amazon.ElastiCacheCluster.ElastiCacheClusterConfig
                Dim serverPortSplit As String()
                Dim port As Integer = 11211
                '
                ' setup programData\cfw to bootstrap clusterConfig file
                '
                config = New clusterConfigClass()
                config.isLocal = True
                config.clusterPhysicalPath = getAppDataFolder() & "\"
                _ok = False
                files = New fileSystemClass(cpCore, config, fileSystemClass.fileSyncModeEnum.activeSync, getAppDataFolder())
                If False Then
                    '
                    ' dotnet app config
                    '
                    'config.appPattern = ConfigurationManager.AppSettings("appPattern")
                    'config.awsElastiCacheConfigurationEndpoint = ConfigurationManager.AppSettings("awsElastiCacheConfigurationEndpoint")
                    'config.clusterFilesEndpoint = ConfigurationManager.AppSettings("clusterFilesEndpoint")
                    'config.defaultDataSourceAddress = ConfigurationManager.AppSettings("defaultDataSourceAddress")
                    'config.defaultDataSourceODBCConnectionString = ConfigurationManager.AppSettings("defaultDataSourceODBCConnectionString")
                    'config.defaultDataSourcePassword = ConfigurationManager.AppSettings("defaultDataSourcePassword")
                    'config.defaultDataSourceType = ConfigurationManager.AppSettings("defaultDataSourceType")
                    'config.defaultDataSourceUsername = ConfigurationManager.AppSettings("defaultDataSourceUsername")
                    ''config.domainRoutes = ConfigurationManager.AppSettings("domainRoutes").Split(",")
                    'config.isLocal = ConfigurationManager.AppSettings("isLocal")
                    'config.isLocalCache = ConfigurationManager.AppSettings("isLocalCache")
                    'config.clusterPhysicalPath = ConfigurationManager.AppSettings("localDataPath")
                    'config.maxCmdInstances = ConfigurationManager.AppSettings("maxCmdInstances")
                    'config.name = ConfigurationManager.AppSettings("clusterName")
                    'config.password = ConfigurationManager.AppSettings("password")
                    'config.serverListenerPort = ConfigurationManager.AppSettings("serverListenerPort")
                    'config.username = ConfigurationManager.AppSettings("username")
                Else
                    '
                    ' generic json file
                    '
                    JSONTemp = files.ReadFile("clusterConfig.json")
                    If JSONTemp = "" Then
                        '
                        ' for now it fails, maybe later let it autobuild a local cluster
                        '
                        'config = New clusterConfigClass
                        ''config.isLocal = True
                        'JSONTemp = json_serializer.Serialize(config)
                        'Call files.SaveFile("clusterConfig.json", JSONTemp)
                    Else
                        config = json_serializer.Deserialize(Of clusterConfigClass)(JSONTemp)
                        _ok = True
                    End If
                End If
                '
                ' backfill with default in case it was set blank
                '
                If config.clusterPhysicalPath = "" Then
                    config.clusterPhysicalPath = ccCommonModule.getAppDataFolder & "\"
                End If
                '
                ' init file system
                '
                If _ok Then
                    If Not config.isLocal Then
                        files = New fileSystemClass(cpCore, config, fileSystemClass.fileSyncModeEnum.noSync, localDataPath, "")
                    Else
                        files = New fileSystemClass(cpCore, config, fileSystemClass.fileSyncModeEnum.activeSync, localDataPath, config.clusterFilesEndpoint)
                    End If
                End If
                '
                ' setup cache
                '
                If config.isLocalCache Then
                    Throw New NotImplementedException("local cache not implemented yet")
                Else
                    If Not String.IsNullOrEmpty(config.awsElastiCacheConfigurationEndpoint) Then
                        serverPortSplit = config.awsElastiCacheConfigurationEndpoint.Split(":"c)
                        If serverPortSplit.Count > 1 Then
                            port = EncodeInteger(serverPortSplit(1))
                        End If
                        cacheConfig = New Amazon.ElastiCacheCluster.ElastiCacheClusterConfig(serverPortSplit(0), port)
                        cacheConfig.Protocol = Enyim.Caching.Memcached.MemcachedProtocol.Binary
                        mc = New Enyim.Caching.MemcachedClient(cacheConfig)
                        mc.Store(Memcached.StoreMode.Set, "testing", "123", Now.AddMinutes(10))
                    End If
                End If
            Catch ex As Exception
                cpCore.handleException(ex)
            End Try
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' ok - means the class has initialized and methods can be used to maintain the cluser
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property ok As Boolean
            Get
                Return _ok
            End Get
        End Property
        Private _ok As Boolean = False
        '
        '====================================================================================================
        ''' <summary>
        ''' physical path to the head of the local data storage
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property localDataPath As String
            Get
                If (config Is Nothing) Then
                    Return ""
                Else
                    Return config.clusterPhysicalPath
                End If
            End Get
        End Property
        '
        Public ReadOnly Property localAppsPath As String
            Get
                If (config Is Nothing) Then
                    Return ""
                Else
                    Return config.clusterPhysicalPath & "apps\"
                End If
            End Get
        End Property
        '
        '
        '
        Public Sub cache_saveRaw(ByVal Key As String, ByVal data As Object, Optional invalidationDate As Date = #12:00:00 AM#)
            Try
                Dim testValue As Object
                If invalidationDate <= Date.MinValue Then
                    invalidationDate = Now.AddDays(7 + Rnd())
                End If
                If (Key = "") Then
                    cpCore.handleException(New ApplicationException("key cannot be empty"))
                ElseIf (invalidationDate <= Now()) Then
                    cpCore.handleException(New ApplicationException("invalidationDate must be > current date/time"))
                Else
                    Dim allowSave As Boolean
                    allowSave = False
                    If data Is Nothing Then
                        allowSave = True
                    ElseIf Not data.GetType.IsSerializable Then
                        cpCore.handleException(New ApplicationException("data object must be serializable"))
                    Else
                        allowSave = True
                    End If
                    If allowSave Then
                        If config.isLocalCache Then
                            Throw New NotImplementedException("local cache not implemented yet")
                        Else
                            Call mc.Store(Enyim.Caching.Memcached.StoreMode.Set, encodeCacheKey(Key), data, invalidationDate)
                            ' !!!!! remove me when tested
                            'cpCore.appendLog("cache_saveRow, remove readback test")
                            'testValue = mc.Get(encodeCacheKey(Key))
                        End If
                    End If
                End If
            Catch ex As Exception
                cpCore.handleException(ex)
            End Try
        End Sub
        '
        '
        '====================================================================================================
        ''' <summary>
        ''' Read from the cluster
        ''' </summary>
        ''' <param name="Key"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function cache_readRaw(ByVal Key As String) As Object
            Dim returnValue As Object = Nothing
            Try
                If config.isLocalCache Then
                    Throw New NotImplementedException("local cache not implemented yet")
                Else
                    returnValue = mc.Get(encodeCacheKey(Key))
                End If
            Catch ex As Exception
                cpCore.handleException(ex)
            End Try
            Return returnValue
        End Function
        '
        '=======================================================================
        ''' <summary>
        ''' Encode a string to be memCacheD compatible, removing 0x00-0x20 and space
        ''' </summary>
        ''' <param name="key"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function encodeCacheKey(key As String) As String
            Dim returnKey As String
            returnKey = Regex.Replace(key, "0x[a-fA-F\d]{2}", "_")
            returnKey = returnKey.Replace(" ", "_")
            Return returnKey
        End Function
        '
        ' return as list all appNames with applications on this cluster
        '
        Public Function getAppNameList() As List(Of String)
            Dim appList As List(Of String) = New List(Of String)
            Try
                Throw New NotImplementedException()
            Catch ex As Exception
                cpCore.handleException(ex)
            End Try
            Return appList
        End Function
        '
        ' execute sql on default connection and return datatable
        '
        Public Function executeMasterSql(ByVal sql As String, Optional ByVal startRecord As Integer = 0, Optional ByVal maxRecords As Integer = 9999) As DataTable
            Dim returnData As New DataTable
            Try
                Dim connString As String
                '
                connString = "" _
                    & "data source=" & config.defaultDataSourceAddress & ";" _
                    & "UID=" & config.defaultDataSourceUsername & ";" _
                    & "PWD=" & config.defaultDataSourcePassword & ";" _
                    & ""
                Using connSQL As New SqlConnection(connString)
                    connSQL.Open()
                    Using cmdSQL As New SqlCommand()
                        cmdSQL.CommandType = Data.CommandType.Text
                        cmdSQL.CommandText = sql
                        cmdSQL.Connection = connSQL
                        Using adptSQL = New SqlClient.SqlDataAdapter(cmdSQL)
                            adptSQL.Fill(startRecord, maxRecords, returnData)
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                cpCore.handleException(ex)
            End Try
            Return returnData
        End Function
        '
        ' verify database exists
        '
        Public Function checkDatabaseExists(databaseName As String) As Boolean
            Dim returnOk As Boolean = False
            Try
                Dim sql As String
                Dim databaseId As Integer = 0
                Dim dt As DataTable
                '
                sql = String.Format("SELECT database_id FROM sys.databases WHERE Name = '{0}'", databaseName)
                dt = executeMasterSql(sql)
                returnOk = (dt.Rows.Count > 0)
                dt.Dispose()
            Catch ex As Exception
                cpCore.handleException(ex)
            End Try
            Return returnOk
        End Function
        '
        '====================================================================================================
        ''' <summary>
        ''' save config changes to the clusterConfig.json file
        ''' </summary>
        Public Sub saveConfig()
            Dim json As New System.Web.Script.Serialization.JavaScriptSerializer
            Dim jsonTemp As String = json.Serialize(config)
            files.SaveFile("clusterConfig.json", jsonTemp)
        End Sub
        '
        '====================================================================================================
        ''' <summary>
        ''' handle exceptions in this class
        ''' </summary>
        ''' <param name="ex"></param>
        ''' <param name="methodName"></param>
        ''' <param name="Cause"></param>
        ''' <remarks></remarks>
        Private Sub handleClassException(ByVal ex As Exception, ByVal methodName As String, ByVal Cause As String)
            cpCore.handleException(ex, "Unexpected exception in clusterServicesClass." & methodName & ", cause=[" & Cause & "]")
        End Sub
        '
        '====================================================================================================
        ' dispose
        '====================================================================================================
        '
#Region " IDisposable Support "
        Protected disposed As Boolean = False
        Protected Overridable Overloads Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposed Then
                If disposing Then
                    '
                    ' call .dispose for managed objects
                    '
                    'CP = Nothing

                    '
                    ' ----- Close all open csv_ContentSets, and make sure the RS is killed
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
