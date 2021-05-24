
Imports System.Web.Routing
Imports Contensive
Imports Contensive.Processor
Imports Contensive.Processor.Controllers
Imports Contensive.Processor.Models.Domain

Public Class ConfigurationClass
    '
    '====================================================================================================
    ''' <summary>
    ''' if true, the route map is not loaded or invalid and needs to be loaded
    ''' </summary>
    ''' <returns></returns>
    Public Shared Function routeMapDateInvalid() As Boolean
        If (HttpContext.Current.Application("RouteMapDateCreated") Is Nothing) Then Return True
        Dim dateResult As Date
        Return (Not Date.TryParse(HttpContext.Current.Application("RouteMapDateCreated").ToString(), dateResult))
    End Function
    '
    '====================================================================================================
    ''' <summary>
    ''' determine the Contensive application name from the webconfig or iis sitename
    ''' </summary>
    ''' <returns></returns>
    Public Shared Function getAppName() As String
        '
        ' -- app name matches iis site name unless overridden by aspx app setting "ContensiveAppName"
        Dim appName As String = ConfigurationManager.AppSettings("ContensiveAppName")
        If (String.IsNullOrEmpty(appName)) Then
            appName = System.Web.Hosting.HostingEnvironment.ApplicationHost.GetSiteName()
        End If
        Return appName
    End Function
    '
    ' ====================================================================================================
    ''' <summary>
    ''' verify the routemap is not stale. This was the legacy reload process that reloads without an application load.
    ''' </summary>
    ''' <param name="cp"></param>
    Public Shared Sub verifyRouteMap(cp As CPClass)
        '
        ' -- if application var does not equal routemap.datecreated rebuild
        If (routeMapDateInvalid() OrElse (cp.routeMap.dateCreated <> CDate(HttpContext.Current.Application("RouteMapDateCreated")))) Then
            If routeMapDateInvalid() Then
                LogController.logShortLine("configurationClass, loadRouteMap, [" + cp.Site.Name + "], rebuild because HttpContext.Current.Application(RouteMapDateCreated) is not valid", BaseClasses.CPLogBaseClass.LogLevel.Info)
            Else
                LogController.logShortLine("configurationClass, loadRouteMap, [" + cp.Site.Name + "], rebuild because not equal, cp.routeMap.dateCreated [" + cp.routeMap.dateCreated.ToString() + "], HttpContext.Current.Application(RouteMapDateCreated) [" + HttpContext.Current.Application("RouteMapDateCreated").ToString() + "]", BaseClasses.CPLogBaseClass.LogLevel.Info)
            End If
            loadRouteMap(cp)
        End If
    End Sub
    '
    ' ====================================================================================================
    ''' <summary>
    ''' load the routemap
    ''' </summary>
    ''' <param name="cp"></param>
    Public Shared Sub loadRouteMap(cp As CPClass)
        SyncLock RouteTable.Routes
            '
            LogController.logShortLine("configurationClass, loadRouteMap enter, [" + cp.Site.Name + "]", BaseClasses.CPLogBaseClass.LogLevel.Trace)
            '
            HttpContext.Current.Application("routeMapDateCreated") = cp.routeMap.dateCreated
            '
            RouteTable.Routes.Clear()
            For Each newRouteKeyValuePair In cp.routeMap.routeDictionary
                Try
                    RouteTable.Routes.Remove(RouteTable.Routes(newRouteKeyValuePair.Key))
                    RouteTable.Routes.MapPageRoute(newRouteKeyValuePair.Value.virtualRoute, newRouteKeyValuePair.Value.virtualRoute, newRouteKeyValuePair.Value.physicalRoute)
                Catch ex As Exception
                    cp.Site.ErrorReport(ex, "Unexpected exception adding virtualRoute, key [" & newRouteKeyValuePair.Key & "], route [" & newRouteKeyValuePair.Value.virtualRoute & "]")
                End Try
            Next
        End SyncLock
        '
        LogController.logShortLine("configurationClass, loadRouteMap exit, [" + cp.Site.Name + "]", BaseClasses.CPLogBaseClass.LogLevel.Info)
        '
    End Sub
    '
    ' ====================================================================================================
    ''' <summary>
    ''' build the http context from the iis httpContext object
    ''' </summary>
    ''' <param name="appName"></param>
    ''' <param name="iisContext"></param>
    ''' <returns></returns>
    Public Shared Function buildContext(appName As String, ByVal iisContext As HttpContext) As Contensive.Processor.Models.Domain.HttpContextModel
        Try
            Dim context As New HttpContextModel

            If (iisContext Is Nothing) OrElse (iisContext.Request Is Nothing) OrElse (iisContext.Response Is Nothing) Then
                LogController.logShortLine("ConfigurationClass.buildContext - Attempt to initialize webContext but iisContext or one of its objects is null., [" + appName + "]", BaseClasses.CPLogBaseClass.LogLevel.Fatal)
                Throw New ApplicationException("ConfigurationClass.buildContext - Attempt to initialize webContext but iisContext or one of its objects is null., [" + appName + "]")
            End If
            '
            ' -- set default response
            context.Response.cacheControl = "no-cache"
            context.Response.expires = -1
            context.Response.buffer = True
            '
            ' -- setup request
            iisContext.Request.InputStream.Position = 0
            context.Request.requestBody = (New System.IO.StreamReader(iisContext.Request.InputStream)).ReadToEnd()
            context.Request.ContentType = iisContext.Request.ContentType
            context.Request.Url = New HttpContentRequestUrl() With {
                .AbsoluteUri = iisContext.Request.Url.Scheme & "://" & iisContext.Request.Url.Host & iisContext.Request.RawUrl,
                .Port = iisContext.Request.Url.Port
            }
            context.Request.UrlReferrer = iisContext.Request.UrlReferrer
            '
            If True Then
                '
                ' -- server variables
                Dim nameValues As NameValueCollection = iisContext.Request.ServerVariables
                For i As Integer = 0 To nameValues.Count - 1
                    Dim key As String = nameValues.GetKey(i)
                    If String.IsNullOrWhiteSpace(key) Then
                        Continue For
                    End If
                    If context.Request.ServerVariables.ContainsKey(key) Then
                        context.Request.ServerVariables.Remove(key)
                    End If
                    context.Request.ServerVariables.Add(key, nameValues.Get(i))
                Next
            End If
            '
            If True Then
                '
                ' -- request headers
                Dim nameValues As NameValueCollection = iisContext.Request.Headers
                For i As Integer = 0 To nameValues.Count - 1
                    Dim key As String = nameValues.GetKey(i)
                    If String.IsNullOrWhiteSpace(key) Then
                        Continue For
                    End If
                    If context.Request.Headers.ContainsKey(key) Then
                        context.Request.Headers.Remove(key)
                    End If
                    context.Request.Headers.Add(key, nameValues.Get(i))
                Next
            End If
            '
            If True Then
                '
                ' -- request querystring
                Dim nameValues As NameValueCollection = iisContext.Request.QueryString
                For i As Integer = 0 To nameValues.Count - 1
                    Dim key As String = nameValues.GetKey(i)
                    If String.IsNullOrWhiteSpace(key) Then
                        Continue For
                    End If
                    If context.Request.Headers.ContainsKey(key) Then
                        context.Request.Headers.Remove(key)
                    End If
                    context.Request.QueryString.Add(key, nameValues.Get(i))
                Next
            End If
            '
            If True Then
                '
                ' -- request form
                Dim nameValues As NameValueCollection = iisContext.Request.Form
                For i As Integer = 0 To nameValues.Count - 1
                    Dim key As String = nameValues.GetKey(i)
                    If String.IsNullOrWhiteSpace(key) Then
                        Continue For
                    End If
                    If context.Request.Headers.ContainsKey(key) Then
                        context.Request.Headers.Remove(key)
                    End If
                    context.Request.Form.Add(key, nameValues.Get(i))
                Next
            End If

            If True Then
                '
                ' -- transfer upload files
                Dim filePtr As Integer = 0
                Dim instanceId As String = GenericController.getGUIDNaked()
                For Each key As String In iisContext.Request.Files.AllKeys
                    If String.IsNullOrWhiteSpace(key) Then
                        Continue For
                    End If
                    Dim file As HttpPostedFile = iisContext.Request.Files(key)
                    If file IsNot Nothing Then
                        Dim normalizedFilename As String = FileController.normalizeDosFilename(file.FileName)
                        If (file.ContentLength > 0) AndAlso (Not String.IsNullOrWhiteSpace(normalizedFilename)) Then
                            Dim windowsTempFile As String = DefaultSite.WindowsTempFileController.createTmpFile()
                            file.SaveAs(windowsTempFile)
                            context.Request.Files.Add(New DocPropertyModel With {
                                .name = key,
                                .value = normalizedFilename,
                                .nameValue = System.Uri.EscapeDataString(key) & "=" + System.Uri.EscapeDataString(normalizedFilename),
                                .windowsTempfilename = windowsTempFile,
                                .propertyType = DocPropertyModel.DocPropertyTypesEnum.file
                            })
                            filePtr += 1
                        End If
                    End If
                Next
            End If

            If True Then
                '
                ' -- transfer cookies
                LogController.logShortLine("ConfigurationClass.buildContext transfer cookies", BaseClasses.CPLogBaseClass.LogLevel.Trace)
                For Each cookieKey As String In iisContext.Request.Cookies.Keys
                    LogController.logShortLine("ConfigurationClass.buildContext transfer cookies, cookieKey [" & cookieKey & "], cookie value [" & iisContext.Request.Cookies(cookieKey).Value & "]", BaseClasses.CPLogBaseClass.LogLevel.Trace)
                    If String.IsNullOrWhiteSpace(cookieKey) Then Continue For
                    If (context.Request.Cookies.ContainsKey(cookieKey)) Then
                        context.Request.Cookies.Remove(cookieKey)
                    End If
                    context.Request.Cookies.Add(cookieKey, New HttpContextRequestCookie() With {
                        .Name = cookieKey,
                        .Value = iisContext.Request.Cookies(cookieKey).Value
                    })
                Next
                LogController.logShortLine("ConfigurationClass.buildContext exit, context.Request.Cookies.Count [" & context.Request.Cookies.Count & "]", BaseClasses.CPLogBaseClass.LogLevel.Trace)
            End If
            Return context
        Catch ex As Exception
            LogController.logShortLine("ConfigurationClass.buildContext exception, [" + ex.ToString() + "]", BaseClasses.CPLogBaseClass.LogLevel.Fatal)
            Throw
        End Try
    End Function
End Class

