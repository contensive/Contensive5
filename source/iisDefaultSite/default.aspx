<script runat="server">
    Sub Page_Load()
        '
        ' -- DO NOT edit. This file is replaced duing upgrade
        '
        Contensive.Processor.Controllers.LogController.logShortLine("Page_Load", Contensive.BaseClasses.CPLogBaseClass.LogLevel.Trace)
        Try
            If (HttpContext.Current.Request.HttpMethod <> "OPTIONS") Then
                '
                ' -- not preflight options call, get content
                '
                ' -- initialize with contensive d:\contensive\serverConfig.json (use same settings as cli and services)
                Dim appName As String = ConfigurationClass.getAppName()
                Dim context = ConfigurationClass.buildContext(appName, HttpContext.Current)
                Using cp As New Contensive.Processor.CPClass(appName, context)
                    '
                    ' -- if disabled, exit
                    if not cp.appOk then
                        response.write("The application [" & appName & "] is currently disabled.")
                        Exit Sub
                    end if
                    '
                    ' -- execute code ------------------------------------------------
                    Dim content As String = cp.executeRoute()
                    ' -- /execute code ------------------------------------------------
                    '
                    ' -- exit now if response headers sent. This technique is used to write binary
                    If Response.HeadersWritten Then Return
                    '
                    ' -- delete uploaded temp files in request
                    For Each file In context.Request.Files
                        DefaultSite.WindowsTempFileController.deleteTmpFile(file.windowsTempfilename)
                    Next
                    '
                    ' -- transfer response to webserver
                    If (Not String.IsNullOrEmpty(context.Response.redirectUrl)) Then
                        Response.Redirect(context.Response.redirectUrl, False)
                        Exit Sub
                    End If
                    '
                    For Each header As Contensive.Processor.Models.Domain.HttpContextResponseHeader In context.Response.headers
                        Response.Headers.Add(header.name, header.value)
                    Next
                    '
                    For Each cookie As KeyValuePair(Of String, Contensive.Processor.Models.Domain.HttpContextResponseCookie) In context.Response.cookies
                        Dim ck As New HttpCookie(cookie.Key, cookie.Value.value)
                        ck.Domain = cookie.Value.domain
                        ck.Expires = cookie.Value.expires
                        ck.HttpOnly = cookie.Value.httpOnly
                        ck.Name = cookie.Key
                        ck.Path = cookie.Value.path
                        ck.SameSite = cookie.Value.sameSite
                        ck.Secure = cookie.Value.secure
                        Response.AppendCookie(ck)
                    Next
                    '
                    Response.ContentType = context.Response.contentType
                    Response.CacheControl = context.Response.cacheControl
                    Response.Status = context.Response.status
                    Response.Expires = context.Response.expires
                    Response.Buffer = context.Response.buffer
                    '
                    ' -- write content body to webserver
                    Response.Write(content)
                    '
                    ' -- if routeMap changed, unload app domain
                    If (ConfigurationClass.routeMapDateInvalid() OrElse (cp.routeMap.dateCreated <> CDate(HttpContext.Current.Application("RouteMapDateCreated")))) Then
                        HttpRuntime.UnloadAppDomain()
                    End If
                End Using
            End If
            '
            ' -- setup CORS if not present 
            ' -- option 1 
            '		- set CORS programmatically and allow this to handle option verb when code is not executed (preflight options for ex)
            '		- to set origin, use webconfig AppSettings section. Set DefaultCORSAllowOrigin (see web.config for details)
            ' -- option 2
            '		- add all CORS response to customweb.config.  (see web.config for details)
            If Not HttpContext.Current.Response.Headers.AllKeys.Contains("Access-Control-Allow-Origin") Then
                Dim allowOrigin As String = ConfigurationManager.AppSettings("DefaultCORSAllowOrigin")
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Origin", If(String.IsNullOrEmpty(allowOrigin), "*", allowOrigin))
                '
				if allowOrigin<>"*" then
					HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Credentials", "true")
				end if
                '
                Dim allowMethods As String = ConfigurationManager.AppSettings("DefaultCORSAllowMethods")
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Methods", If(String.IsNullOrEmpty(allowMethods), "GET,PUT,POST,DELETE,PATCH,OPTIONS", allowMethods))
                '
                Dim allowHeaders As String = ConfigurationManager.AppSettings("DefaultCORSAllowHeaders")
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Headers", If(String.IsNullOrEmpty(allowHeaders), "Origin,X-Requested-With,Content-Type,soapaction,Accept,Authorization", allowHeaders))
            End If
        Catch ex As Exception
        Finally
        End Try
    End Sub
</script>
