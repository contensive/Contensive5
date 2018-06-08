

<script runat="server">

    Sub Page_Load()
        Contensive.Processor.Controllers.logController.forceNLog("Page_Load", Contensive.Processor.Controllers.logController.logLevel.Trace)
        '
        ' -- two possible initialization methods 
        Dim sw As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()
        Dim useWebConfig As Boolean = (ConfigurationManager.AppSettings("ContensiveUseWebConfig").ToLower = "true")
        Try
            If useWebConfig Then
                '
                ' -- initialize with web.config
                Dim serverConfig As Contensive.Processor.Models.Context.serverConfigModel = DefaultSite.configurationClass.getServerConfig()
                Using cp As New Contensive.Processor.CPClass(serverConfig.apps(0).name, serverConfig, HttpContext.Current)
                    Response.Write(cp.executeRoute())
                    If (cp.routeDictionaryChanges) Then DefaultSite.configurationClass.loadRouteMap(cp)
                End Using
            Else
                '
                ' -- initialize with contensive c:\programdata\contensive\serverConfig.json setup during installation
                Using cp As New Contensive.Processor.CPClass(DefaultSite.configurationClass.getAppName(), HttpContext.Current)
                    Response.Write(cp.executeRoute())
                    If (cp.routeDictionaryChanges) Then DefaultSite.configurationClass.loadRouteMap(cp)
                End Using
            End If
        Catch ex As Exception
        Finally
        End Try
    End Sub
</script>