
using Contensive.Services;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => {
        options.ServiceName = "Contensive Task Service";
    })
    .ConfigureServices(services => {
        services.AddHostedService<ContensiveWorker>();
    })
    .Build()
    .Run();
