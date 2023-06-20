using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IntegracionPowTest;

using System.Text.Json;
using System.Text.Json.Serialization;

using IntegracionPowTest.Pow;
using IntegracionPowTest.Validadores;

using Microsoft.Extensions.Configuration;

using Polly;
using Polly.Extensions.Http;

IHost host = new HostBuilder()
             .ConfigureFunctionsWorkerDefaults()
             .ConfigureServices((hostContext, s) => {

                 IConfiguration configuracion = hostContext.Configuration;
                 var reintentosMaximos = Convert.ToInt32(configuracion["Configuraciones:ReintentosMaximos"]);
                 var tiempoEntreReintentosMs = Convert.ToInt64(configuracion["Configuraciones:EsperaMaximaEntreReintentosMs"]);
                 string apiPowEndPoint = configuracion["Configuraciones:PowEndpoint"] ?? string.Empty;

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

                 IAsyncPolicy<HttpResponseMessage> politicaDeReintentos = HttpPolicyExtensions
                                                                            .HandleTransientHttpError()
                                                                            .WaitAndRetryAsync(
                                                                                               reintentosMaximos,
                                                                                               d => TimeSpan.FromMilliseconds(d * tiempoEntreReintentosMs));

                 s.AddHttpClient<IApiPow, ApiPow>(cli => {
                      cli.BaseAddress = new Uri(apiPowEndPoint);
                      cli.DefaultRequestHeaders.Add("Accept", "application/json");
                  })
                  .SetHandlerLifetime(TimeSpan.FromMinutes(15.0))
                  .AddPolicyHandler(politicaDeReintentos);


             })
             .Build();

// TODO: LogDebug de las configuraciones no secretas

host.Run();
