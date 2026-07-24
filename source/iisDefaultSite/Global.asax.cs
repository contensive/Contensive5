using System;
using System.Text;
using System.Web;
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using Microsoft.VisualBasic;
//
/// <summary>
/// 
/// </summary>
public class Global_asax : HttpApplication {
    /// <summary>
    /// </summary>  
    public Guid appId = Guid.NewGuid();
    // 
    // ====================================================================================================
    /// <summary>
    /// application load -- build routing
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void Application_Start(object sender, EventArgs e) {
        try {
            // 
            LogController.logShortLine("Global.asax, Application_Start [" + ConfigurationClass.getAppName() + "]", CPLogBaseClass.LogLevel.Info);
            using var cp = new Contensive.Processor.CPClass(ConfigurationClass.getAppName());
            // 
            // -- validate new fields. Upgrade handles this process, but if the upgrade is not run, this will catch it
            cp.core.db.createSQLTableField("ccAggregateFunctions", "dashboardWidget", CPContentBaseClass.FieldTypeIdEnum.Boolean);
            // 
            // -- load the route map
            ConfigurationClass.loadRouteMap(cp);
        } catch (Exception ex) {
            LogController.logShortLine("Global.asax, Application_Start exception [" + ConfigurationClass.getAppName() + "]" + getAppDescription("Application_Start ERROR exit") + ", ex [" + ex.ToString() + "]", CPLogBaseClass.LogLevel.Fatal);
        }
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// Fires when the session is started
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void Session_Start(object sender, EventArgs e) {
        // 
        LogController.logShortLine("Global.asax, Session_Start [" + e.ToString() + "]", CPLogBaseClass.LogLevel.Info);
        // 
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// Fires at the beginning of each request
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void Application_BeginRequest(object sender, EventArgs e) {
        // 
        LogController.logShortLine("Global.asax, Application_BeginRequest [" + e.ToString() + "]", CPLogBaseClass.LogLevel.Info);
        // 
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// Fires when iis attempts to authenticate the use
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void Application_AuthenticateRequest(object sender, EventArgs e) {
        // 
        LogController.logShortLine("Global.asax, Application_AuthenticateRequest [" + e.ToString() + "]", CPLogBaseClass.LogLevel.Info);
        // 
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// Fires when an error occurs
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void Application_Error(object sender, EventArgs e) {
        if (sender is not null) {
            var exception = Server.GetLastError();
            if (exception is not null) {
                // 
                // -- dont log [The file '...' does not exist.]
                // -- check both outer and inner exception because ASP.NET may wrap HttpException in HttpUnhandledException
                if (isFileNotExistsException(exception) || exception.InnerException is not null && isFileNotExistsException(exception.InnerException)) {
                    return;
                }
                // 
                // -- dont log viewstate MAC validation failures (bot/scanner probes submitting forged viewstate)
                if (isViewStateMacException(exception) || exception.InnerException is not null && isViewStateMacException(exception.InnerException)) {
                    return;
                }
                // 
                // -- dont log TraceHandler exceptions (bots/scanners requesting /trace.axd)
                if (isTraceHandlerException(exception) || exception.InnerException is not null && isTraceHandlerException(exception.InnerException)) {
                    return;
                }
                LogController.logShortLine("Global.asax, Application_Error, exception message [" + exception.Message + "], toString [" + exception.ToString() + "]", CPLogBaseClass.LogLevel.Error);
                var innerException = exception.InnerException;
                if (innerException is not null) {
                    LogController.logShortLine("Global.asax, Application_Error, inner exception message [" + innerException.Message + "], toString [" + innerException.ToString() + "]", CPLogBaseClass.LogLevel.Error);
                }
            }
        }

    }
    // 
    // ====================================================================================================
    /// <summary>
    /// Fires when the session ends
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void Session_End(object sender, EventArgs e) {
        // 
        LogController.logShortLine("Global.asax, Session_End [" + e.ToString() + "]", CPLogBaseClass.LogLevel.Info);
        // 
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// Fires when the application ends
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void Application_End(object sender, EventArgs e) {
        // 
        LogController.logShortLine("Global.asax, Application_End [" + e.ToString() + "," + getShutdownDetail() + "]", CPLogBaseClass.LogLevel.Info);
        // 
    }
    // 
    // ====================================================================================================
    private string getAppDescription(string eventName) {
        var builder = new StringBuilder();
        // 
        builder.AppendFormat("Event: {0}", eventName);
        builder.AppendFormat(", Guid: {0}", appId);
        builder.AppendFormat(", Thread Id: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
        builder.AppendFormat(", Appdomain: {0}", AppDomain.CurrentDomain.FriendlyName);
        builder.Append(Interaction.IIf(System.Threading.Thread.CurrentThread.IsThreadPoolThread, ", Pool Thread", ", No Thread").ToString());
        return builder.ToString();
    }
    // 
    private string getShutdownDetail() {
        var shutdownReason = System.Web.Hosting.HostingEnvironment.ShutdownReason;
        string shutdownDetail;

        switch (shutdownReason) {
            case ApplicationShutdownReason.BinDirChangeOrDirectoryRename: {
                    shutdownDetail = "A change was made to the bin directory or the directory was renamed";
                    break;
                }
            case ApplicationShutdownReason.BrowsersDirChangeOrDirectoryRename: {
                    shutdownDetail = "A change was made to the App_browsers folder or the files contained in it";
                    break;
                }
            case ApplicationShutdownReason.ChangeInGlobalAsax: {
                    shutdownDetail = "A change was made in the global.asax file";
                    break;
                }
            case ApplicationShutdownReason.ChangeInSecurityPolicyFile: {
                    shutdownDetail = "A change was made in the code access security policy file";
                    break;
                }
            case ApplicationShutdownReason.CodeDirChangeOrDirectoryRename: {
                    shutdownDetail = "A change was made in the App_Code folder or the files contained in it";
                    break;
                }
            case ApplicationShutdownReason.ConfigurationChange: {
                    shutdownDetail = "A change was made to the application level configuration";
                    break;
                }
            case ApplicationShutdownReason.HostingEnvironment: {
                    shutdownDetail = "The hosting environment shut down the application";
                    break;
                }
            case ApplicationShutdownReason.HttpRuntimeClose: {
                    shutdownDetail = "A call to Close() was requested";
                    break;
                }
            case ApplicationShutdownReason.IdleTimeout: {
                    shutdownDetail = "The idle time limit was reached";
                    break;
                }
            case ApplicationShutdownReason.InitializationError: {
                    shutdownDetail = "An error in the initialization of the AppDomain";
                    break;
                }
            case ApplicationShutdownReason.MaxRecompilationsReached: {
                    shutdownDetail = "The maximum number of dynamic recompiles of a resource limit was reached";
                    break;
                }
            case ApplicationShutdownReason.PhysicalApplicationPathChanged: {
                    shutdownDetail = "A change was made to the physical path to the application";
                    break;
                }
            case ApplicationShutdownReason.ResourcesDirChangeOrDirectoryRename: {
                    shutdownDetail = "A change was made to the App_GlobalResources foldr or the files contained within it";
                    break;
                }
            case ApplicationShutdownReason.UnloadAppDomainCalled: {
                    shutdownDetail = "A call to UnloadAppDomain() was completed";
                    break;
                }

            default: {
                    shutdownDetail = "Unknown shutdown reason";
                    break;
                }
        }
        return shutdownDetail;
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// Returns true if the exception message matches the pattern "The file '...' does not exist."
    /// </summary>
    private bool isFileNotExistsException(Exception ex) {
        if (ex is null)
            return false;
        string msg = ex.Message;
        if (string.IsNullOrEmpty(msg) || msg.Length < 27)
            return false;
        return msg[..10].Equals("The file '") && msg.Substring(msg.Length - 17, 17).Equals("' does not exist.");
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// Returns true if the exception is a viewstate MAC validation failure (typically from bots submitting forged viewstate)
    /// </summary>
    private bool isViewStateMacException(Exception ex) {
        if (ex is null)
            return false;
        string msg = ex.Message;
        if (string.IsNullOrEmpty(msg))
            return false;
        return msg.Contains("Validation of viewstate MAC failed");
    }
    // 
    // ====================================================================================================
    /// <summary>
    /// Returns true if the exception originates from ASP.NET TraceHandler (bots/scanners requesting /trace.axd)
    /// </summary>
    private bool isTraceHandlerException(Exception ex) {
        if (ex is null)
            return false;
        string stackTrace = ex.StackTrace;
        if (string.IsNullOrEmpty(stackTrace))
            return false;
        return stackTrace.Contains("System.Web.Handlers.TraceHandler");
    }

}