
Imports Contensive
Imports Contensive.BaseClasses
Imports Contensive.Processor.Controllers

Public Class Global_asax
    Inherits System.Web.HttpApplication
    '
    Public AppId As Guid = Guid.NewGuid()
    '
    '====================================================================================================
    ''' <summary>
    ''' application load -- build routing
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
        Try
            '
            LogController.logShortLine("Global.asax, Application_Start [" & ConfigurationClass.getAppName() & "]", BaseClasses.CPLogBaseClass.LogLevel.Info)
            Using cp As New Contensive.Processor.CPClass(ConfigurationClass.getAppName())
                '
                ' -- validate new fields. Upgrade handles this process, but if the upgrade is not run, this will catch it
                cp.core.db.createSQLTableField("ccAggregateFunctions", "dashboardWidget", CPContentBaseClass.FieldTypeIdEnum.Boolean)
                '
                ' -- load the route map
                ConfigurationClass.loadRouteMap(cp)
            End Using
        Catch ex As Exception
            LogController.logShortLine("Global.asax, Application_Start exception [" & ConfigurationClass.getAppName() & "]" & getAppDescription("Application_Start ERROR exit") + ", ex [" & ex.ToString() & "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Fatal)
        End Try
    End Sub
    '
    '====================================================================================================
    ''' <summary>
    ''' Fires when the session is started
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Sub Session_Start(ByVal sender As Object, ByVal e As EventArgs)
        '
        LogController.logShortLine("Global.asax, Session_Start [" + e.ToString() + "]", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Info)
        '
    End Sub
    '
    '====================================================================================================
    ''' <summary>
    ''' Fires at the beginning of each request
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Sub Application_BeginRequest(ByVal sender As Object, ByVal e As EventArgs)
        '
        LogController.logShortLine("Global.asax, Application_BeginRequest [" + e.ToString() + "]", BaseClasses.CPLogBaseClass.LogLevel.Info)
        '
    End Sub
    '
    '====================================================================================================
    ''' <summary>
    ''' Fires when iis attempts to authenticate the use
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Sub Application_AuthenticateRequest(ByVal sender As Object, ByVal e As EventArgs)
        '
        LogController.logShortLine("Global.asax, Application_AuthenticateRequest [" + e.ToString() + "]", BaseClasses.CPLogBaseClass.LogLevel.Info)
        '
    End Sub
    '
    '====================================================================================================
    ''' <summary>
    ''' Fires when an error occurs
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
        If (sender IsNot Nothing) Then
            Dim exception As Exception = Server.GetLastError()
            If (exception IsNot Nothing) Then
                '
                ' -- dont log [The file '...' does not exist.]
                ' -- check both outer and inner exception because ASP.NET may wrap HttpException in HttpUnhandledException
                If (isFileNotExistsException(exception) OrElse (exception.InnerException IsNot Nothing AndAlso isFileNotExistsException(exception.InnerException))) Then
                    Return
                End If
                '
                ' -- dont log viewstate MAC validation failures (bot/scanner probes submitting forged viewstate)
                If (isViewStateMacException(exception) OrElse (exception.InnerException IsNot Nothing AndAlso isViewStateMacException(exception.InnerException))) Then
                    Return
                End If
                '
                ' -- dont log TraceHandler exceptions (bots/scanners requesting /trace.axd)
                If (isTraceHandlerException(exception) OrElse (exception.InnerException IsNot Nothing AndAlso isTraceHandlerException(exception.InnerException))) Then
                    Return
                End If
                LogController.logShortLine("Global.asax, Application_Error, exception message [" + exception.Message + "], toString [" + exception.ToString() + "]", BaseClasses.CPLogBaseClass.LogLevel.Error)
                Dim innerException As Exception = exception.InnerException
                If (innerException IsNot Nothing) Then
                    LogController.logShortLine("Global.asax, Application_Error, inner exception message [" + innerException.Message + "], toString [" + innerException.ToString() + "]", BaseClasses.CPLogBaseClass.LogLevel.Error)
                End If
            End If
        End If

    End Sub
    '
    '====================================================================================================
    ''' <summary>
    ''' Fires when the session ends
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Sub Session_End(ByVal sender As Object, ByVal e As EventArgs)
        '
        LogController.logShortLine("Global.asax, Session_End [" + e.ToString() + "]", BaseClasses.CPLogBaseClass.LogLevel.Info)
        '
    End Sub
    '
    '====================================================================================================
    ''' <summary>
    ''' Fires when the application ends
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Sub Application_End(ByVal sender As Object, ByVal e As EventArgs)
        '
        LogController.logShortLine("Global.asax, Application_End [" + e.ToString() + "," + getShutdownDetail() + "]", BaseClasses.CPLogBaseClass.LogLevel.Info)
        '
    End Sub
    '
    '====================================================================================================
    Private Function getAppDescription(eventName As String) As String
        Dim builder As New StringBuilder
        '
        builder.AppendFormat("Event: {0}", eventName)
        builder.AppendFormat(", Guid: {0}", AppId)
        builder.AppendFormat(", Thread Id: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId)
        builder.AppendFormat(", Appdomain: {0}", AppDomain.CurrentDomain.FriendlyName)
        builder.Append(IIf(System.Threading.Thread.CurrentThread.IsThreadPoolThread, ", Pool Thread", ", No Thread").ToString())
        Return builder.ToString()
    End Function
    '
    Private Function getShutdownDetail() As String
        Dim shutdownReason As System.Web.ApplicationShutdownReason = System.Web.Hosting.HostingEnvironment.ShutdownReason
        Dim shutdownDetail As String

        Select Case shutdownReason
            Case ApplicationShutdownReason.BinDirChangeOrDirectoryRename
                shutdownDetail = "A change was made to the bin directory or the directory was renamed"
            Case ApplicationShutdownReason.BrowsersDirChangeOrDirectoryRename
                shutdownDetail = "A change was made to the App_browsers folder or the files contained in it"
            Case ApplicationShutdownReason.ChangeInGlobalAsax
                shutdownDetail = "A change was made in the global.asax file"
            Case ApplicationShutdownReason.ChangeInSecurityPolicyFile
                shutdownDetail = "A change was made in the code access security policy file"
            Case ApplicationShutdownReason.CodeDirChangeOrDirectoryRename
                shutdownDetail = "A change was made in the App_Code folder or the files contained in it"
            Case ApplicationShutdownReason.ConfigurationChange
                shutdownDetail = "A change was made to the application level configuration"
            Case ApplicationShutdownReason.HostingEnvironment
                shutdownDetail = "The hosting environment shut down the application"
            Case ApplicationShutdownReason.HttpRuntimeClose
                shutdownDetail = "A call to Close() was requested"
            Case ApplicationShutdownReason.IdleTimeout
                shutdownDetail = "The idle time limit was reached"
            Case ApplicationShutdownReason.InitializationError
                shutdownDetail = "An error in the initialization of the AppDomain"
            Case ApplicationShutdownReason.MaxRecompilationsReached
                shutdownDetail = "The maximum number of dynamic recompiles of a resource limit was reached"
            Case ApplicationShutdownReason.PhysicalApplicationPathChanged
                shutdownDetail = "A change was made to the physical path to the application"
            Case ApplicationShutdownReason.ResourcesDirChangeOrDirectoryRename
                shutdownDetail = "A change was made to the App_GlobalResources foldr or the files contained within it"
            Case ApplicationShutdownReason.UnloadAppDomainCalled
                shutdownDetail = "A call to UnloadAppDomain() was completed"
            Case Else
                shutdownDetail = "Unknown shutdown reason"
        End Select
        Return shutdownDetail
    End Function
    '
    '====================================================================================================
    ''' <summary>
    ''' Returns true if the exception message matches the pattern "The file '...' does not exist."
    ''' </summary>
    Private Function isFileNotExistsException(ex As Exception) As Boolean
        If (ex Is Nothing) Then Return False
        Dim msg As String = ex.Message
        If (String.IsNullOrEmpty(msg) OrElse msg.Length < 27) Then Return False
        Return (msg.Substring(0, 10).Equals("The file '") AndAlso msg.Substring(msg.Length - 17, 17).Equals("' does not exist."))
    End Function
    '
    '====================================================================================================
    ''' <summary>
    ''' Returns true if the exception is a viewstate MAC validation failure (typically from bots submitting forged viewstate)
    ''' </summary>
    Private Function isViewStateMacException(ex As Exception) As Boolean
        If (ex Is Nothing) Then Return False
        Dim msg As String = ex.Message
        If (String.IsNullOrEmpty(msg)) Then Return False
        Return msg.Contains("Validation of viewstate MAC failed")
    End Function
    '
    '====================================================================================================
    ''' <summary>
    ''' Returns true if the exception originates from ASP.NET TraceHandler (bots/scanners requesting /trace.axd)
    ''' </summary>
    Private Function isTraceHandlerException(ex As Exception) As Boolean
        If (ex Is Nothing) Then Return False
        Dim stackTrace As String = ex.StackTrace
        If (String.IsNullOrEmpty(stackTrace)) Then Return False
        Return stackTrace.Contains("System.Web.Handlers.TraceHandler")
    End Function

End Class
