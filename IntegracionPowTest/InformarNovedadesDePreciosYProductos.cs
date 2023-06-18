using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using IntegracionPowTest.EntidadesPow;
using IntegracionPowTest.Enumeradores;
using IntegracionPowTest.Validadores;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegracionPowTest;

public class InformarNovedadesDePreciosYProductos {
    private readonly ILogger _log;
    private readonly HttpClient _clienteHttp;
    private readonly Configuraciones _configuraciones;

    private readonly JsonSerializerOptions _opcionesDeSerializacionPredeterminada = new JsonSerializerOptions {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public InformarNovedadesDePreciosYProductos(ILoggerFactory creadorDeLogs, IHttpClientFactory creadorDeClienteHttp, IOptions<Configuraciones> opciones) {
        _log = creadorDeLogs.CreateLogger<InformarNovedadesDePreciosYProductos>();
        _clienteHttp = creadorDeClienteHttp.CreateClient();
        _configuraciones = opciones.Value;
    }

    [Function("InformarNovedadesDePreciosYProductos")]
    public async Task Run([CosmosDBTrigger(
        databaseName: "SweetDb",
        containerName: "Productos",
        Connection = "cn",
        LeaseContainerName = "Leases",
        LeaseContainerPrefix = "T3")] IEnumerable<JsonObject> cambios) {
        
        _log.LogTrace($"{nameof(InformarNovedadesDePreciosYProductos)} comenzada");

        try {

            if (_configuraciones.SucursalesHabilitadas.Any() == false) {
                _log.LogWarning("No hay sucursales habilitadas configuradas, nada para procesar");
                return;
            }

            _log.LogDebug($"Dividiendo documentos cambiados en lotes de {_configuraciones.NumeroDeObjetosPorLote}");

            IEnumerable<JsonObject[]> cambiosAgrupados = cambios.Chunk(_configuraciones.NumeroDeObjetosPorLote);

            var numeroDeLote = 0;
            foreach (JsonObject[] documentos in cambiosAgrupados) {

                numeroDeLote++;
                var datos = new Dictionary<int, NovedadPow>();

                _log.LogDebug($"Procesando lote {numeroDeLote}");

                foreach (JsonObject doc in documentos) {
                    _log.LogTrace($"Procesando documento {doc["Id"]}");

                    var tipoDeDocumento = doc["TipoDeDocumento"]!.GetValue<string>();
                    
                    (bool esValido, string mensajeDeError) = ValidadorDeDocumentos.DeboProcesar(_log, doc, tipoDeDocumento, _configuraciones)
                    if (esValido == false) {
                        _log.LogTrace(mensajeDeError);
                        continue;
                    }

                    ProcesarDocumento(doc, tipoDeDocumento, datos);

                }

                int numeroDeVariantes = datos.Values.Sum(n => n.Variantes.Count);

                _log.LogDebug($"Lote {numeroDeLote} procesado. {numeroDeVariantes} variantes encontradas");

                await EnviarNovedadesConReintentosAsync(datos, _configuraciones.ReintentosMaximos, _configuraciones.EsperaMaximaEntreReintentosMs);
                
            }
         
        } catch (Exception ex) {
            _log.LogCritical(ex.Message);

        } finally {
            _log.LogTrace($"{nameof(InformarNovedadesDePreciosYProductos)} terminada");
        }
    }

    private void ProcesarDocumento(JsonNode doc, string tipoDeDocumento, IDictionary<int, NovedadPow> datos) {
        _log.LogTrace($"{nameof(ProcesarDocumento)} comenzada");

        try {
            int sucursalId = doc["sucursal"]?["Id"]?.GetValue<int>() ?? -1;

            if (datos.TryGetValue(sucursalId, out NovedadPow? novedad) == false) {
                novedad = new NovedadPow
                {
                    Email = _configuraciones.PowEmail,
                    Password = _configuraciones.PowPassword,
                    SucursalId = sucursalId,
                    Variantes = new SortedSet<VariantePow>()
                };
                datos.Add(sucursalId, novedad);
            }

            IEnumeradorDeSkus enumeradorDeSkus = FabricaDeEnumeradoresDeSkus.ObtenerEnumeradorDeSkus(_log, tipoDeDocumento, doc);
            foreach (KeyValuePair<string, double> sku in enumeradorDeSkus.Skus(_log, doc)) {

                if (novedad.Variantes.TryGetValue(new VariantePow { Codigo = sku.Key }, out VariantePow? variante) == false) {
                    variante = new VariantePow
                    {
                        Codigo = sku.Key
                    };
                    novedad.Variantes.Add(variante);
                }

                switch (tipoDeDocumento) {
                    case FabricaDeEnumeradoresDeSkus.TipoDeDocumentoStock:
                        variante.Cantidad = sku.Value;

                        break;

                    case FabricaDeEnumeradoresDeSkus.TipoDeDocumentoPrecios:
                        variante.Precio = sku.Value;

                        break;
                }
            }

        } finally {
            _log.LogTrace($"{nameof(ProcesarDocumento)} terminada");
        }
    }

    private async Task EnviarNovedadesConReintentosAsync(Dictionary<int, NovedadPow> datos, int reintentosMaximos, int esperaMaximaEntreReintentosMs) {
        _log.LogTrace($"{nameof(EnviarNovedadesConReintentosAsync)} comenzada");

        try {
            foreach (NovedadPow novedadPow in datos.Values) {
                _log.LogDebug($"{nameof(EnviarNovedadesConReintentosAsync)} Serializando las novedades a JSON");
                string json = JsonSerializer.Serialize(novedadPow, _opcionesDeSerializacionPredeterminada);

                for (var reintentos = 0; reintentos < reintentosMaximos; reintentos++) {
                    try {

                        _log.LogDebug($"{nameof(EnviarNovedadesConReintentosAsync)} enviando novedades a Pow");
                        HttpResponseMessage respuesta = await _clienteHttp.PostAsync("http://staging.sweet.com.ar/json_stock_update", new StringContent(json));

                        respuesta.EnsureSuccessStatusCode();

                        _log.LogDebug($"{nameof(EnviarNovedadesConReintentosAsync)} Novedades recibidas por POW exitosamente");
                        return;

                    } catch (Exception error) {
                        _log.LogError($"No se pudieron enviar las novedades a Pow. Error: {error.Message}. Se reintenta");

                        if (reintentos == reintentosMaximos) {
                            _log.LogError($"Se llego al maximo numero de reintentos. No se reintetara mas");
                            throw;
                        }
                    }

                    // Espero antes del proximo reintento
                    var rand = new Random();
                    int tiempoDeEsperaMs = rand.Next(50, esperaMaximaEntreReintentosMs + 1);

                    _log.LogWarning($"Esperado {tiempoDeEsperaMs}ms para reintentar...");
                    await Task.Delay(tiempoDeEsperaMs);
                }
            }

        } finally {
            _log.LogTrace($"{nameof(EnviarNovedadesConReintentosAsync)} finalizada");
        }
    }
}
