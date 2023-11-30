
using System.ServiceProcess;

namespace Contensive.Services {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main() {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new taskService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
