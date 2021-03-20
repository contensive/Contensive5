
Imports Contensive
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
            '
            Using cp As New Contensive.Processor.CPClass(ConfigurationClass.getAppName())
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
                Dim exMsg As String = exception.Message
                If (exMsg.Substring(0, 10).Equals("The file '") And exMsg.Substring(exMsg.Length - 17, 17).Equals("' does not exist.")) Then
                    '
                    ' -- File does not exist, thrown for every bot searching content
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

End Class
