using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IntegracionPowTest;

IHost host = new HostBuilder()
             .ConfigureFunctionsWorkerDefaults()
             .ConfigureServices(s => {
                 s.AddHttpClient();

                 s.AddOptions<Configuraciones>()
                  .BindConfiguration(nameof(Configuraciones));
             })
             .Build();

// TODO: LogDebug de las configuraciones no secretas

host.Run();
