using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IntegracionPowTest;

using System.Text.Json;
using System.Text.Json.Serialization;

using IntegracionPowTest.Validadores;

IHost host = new HostBuilder()
             .ConfigureFunctionsWorkerDefaults()
             .ConfigureServices(s => {
                 s.AddHttpClient();

                 s.AddOptions<Configuraciones>()
                  .BindConfiguration(nameof(Configuraciones));

                 s.PostConfigure<Configuraciones>(c => {
                     if (string.IsNullOrWhiteSpace(c.SucursalesCsv)) 
                         return;

                     foreach (int sucursalId in c.SucursalesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                           .Select(id => Convert.ToInt32(id))) {

                         c.SucursalesHabilitadas.Add(sucursalId);
                     }
                 });

                 s.AddSingleton(new JsonSerializerOptions {
                     DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                     DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                 });

                 s.AddSingleton<ValidadorDeDocumentos>();
             })
             .Build();

// TODO: LogDebug de las configuraciones no secretas

host.Run();
