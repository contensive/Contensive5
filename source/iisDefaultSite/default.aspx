<script runat="server">
    Sub Page_Load()
        '
        ' -- DO NOT edit. This file is replaced during upgrade
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
                    For Each header As Contensive.Processor.Models.Domain.HttpContextResponseHeader In context.Response.headers
                        Response.Headers.Add(header.name, header.value)
                    Next
                    '
                    '
                    ' -- write Set-Cookie headers directly instead of using HttpCookie/AppendCookie.
                    '    ASP.NET's HttpCookie serializer emits a legacy hyphenated Expires format
                    '    (dd-MMM-yyyy) that some browsers will not persist reliably. Writing the
                    '    header directly lets us emit RFC 1123 dates and Max-Age, which RFC 6265
                    '    says takes precedence over Expires.
                    For Each cookie As KeyValuePair(Of String, Contensive.Processor.Models.Domain.HttpContextResponseCookie) In context.Response.cookies
                        Dim sb As New System.Text.StringBuilder()
                        sb.Append(cookie.Key)
                        sb.Append("=")
                        sb.Append(If(cookie.Value.value, ""))
                        If (Not String.IsNullOrEmpty(cookie.Value.path)) Then
                            sb.Append("; Path=")
                            sb.Append(cookie.Value.path)
                        Else
                            sb.Append("; Path=/")
                        End If
                        If (Not String.IsNullOrEmpty(cookie.Value.domain)) Then
                            sb.Append("; Domain=")
                            sb.Append(cookie.Value.domain)
                        End If
                        If (cookie.Value.expires <> DateTime.MinValue) Then
                            '
                            ' -- Max-Age (seconds) takes precedence per RFC 6265 and cannot be misparsed
                            Dim maxAgeSeconds As Long = CLng((cookie.Value.expires.ToUniversalTime() - DateTime.UtcNow).TotalSeconds)
                            If (maxAgeSeconds < 0) Then
                                maxAgeSeconds = 0
                            End If
                            sb.Append("; Max-Age=")
                            sb.Append(maxAgeSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture))
                            '
                            ' -- RFC 1123 Expires as a fallback for clients that do not honor Max-Age
                            sb.Append("; Expires=")
                            sb.Append(cookie.Value.expires.ToUniversalTime().ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                        End If
                        If (cookie.Value.httpOnly) Then
                            sb.Append("; HttpOnly")
                        End If
                        If (cookie.Value.secure) Then
                            sb.Append("; Secure")
                        End If
                        '
                        ' -- HttpContextResponseCookieSameSiteMode currently only defines Lax
                        sb.Append("; SameSite=Lax")
                        Response.Headers.Add("Set-Cookie", sb.ToString())
                    Next
                    '
                    Response.ContentType = context.Response.contentType
                    Response.CacheControl = context.Response.cacheControl
                    Response.Status = context.Response.status
                    Response.Expires = context.Response.expires
                    Response.Buffer = context.Response.buffer
                    '
                    ' -- transfer response to webserver
                    If (Not String.IsNullOrEmpty(context.Response.redirectUrl)) Then
                        '
                        ' -- perform redirect. Do not exit because reload required if route change
						if(context.Response.status.contains("301")) then
							Response.RedirectPermanent(context.Response.redirectUrl, False)
						else
							Response.Redirect(context.Response.redirectUrl, False)
						end if
                    Else
                        '
                        ' -- write content body to webserver
                        Response.Write(content)
                    End If
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
                Dim allowOrigin As String = ""
                Dim allowOriginList As String = ConfigurationManager.AppSettings("DefaultCORSAllowOrigins")
                If (Not String.IsNullOrEmpty(allowOriginList)) Then
                    Dim allowOrigins() As String = allowOriginList.Split(","c)
                    Dim origin As String = Request.Headers("Origin")
                    '
                    allowOrigin = If(allowOrigins.Contains(origin), origin, ConfigurationManager.AppSettings("DefaultCORSAllowOrigin"))
                End If
                allowOrigin = If(String.IsNullOrEmpty(allowOrigin), "*", allowOrigin)
                HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Origin", allowOrigin)
				
				Trace.Write("PageLoad, allowOrigin [" & allowOrigin & "]")
				' System.Diagnostics.Trace.WriteLine("Error in Widget 42");
				' Trace.Flush()
                '
                If allowOrigin <> "*" Then
                    HttpContext.Current.Response.Headers.Set("Access-Control-Allow-Credentials", "true")
                End If
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
