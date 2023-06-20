using System.Text.Json;
using System.Text.Json.Nodes;

using IntegracionPowTest.EntidadesPow;
using IntegracionPowTest.Enumeradores;
using IntegracionPowTest.Validadores;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegracionPowTest;

public class InformarNovedadesDePreciosYProductos {

    private const string InformarStockYPrecios = "json_stock_update";

    private readonly ILogger<InformarNovedadesDePreciosYProductos> _log;
    private readonly Configuraciones _configuraciones;
    private readonly ValidadorDeDocumentos _validadorDeDocumentos;
    private readonly JsonSerializerOptions _opcionesDeSerializacionPredeterminada;
    private readonly HttpClient _clientePow;
    
    public InformarNovedadesDePreciosYProductos(ILoggerFactory creadorDeLoggers, IHttpClientFactory creadorDeClienteHttp, IOptions<Configuraciones> opciones, ValidadorDeDocumentos validadorDeDocumentos, JsonSerializerOptions opcionesDeSerializacionPredeterminada) {
        _log = creadorDeLoggers.CreateLogger<InformarNovedadesDePreciosYProductos>();
        _configuraciones = opciones.Value;
        _validadorDeDocumentos = validadorDeDocumentos;
        _opcionesDeSerializacionPredeterminada = opcionesDeSerializacionPredeterminada;
        _clientePow = creadorDeClienteHttp.CreateClient();
        _clientePow.BaseAddress = new Uri("http://staging.sweet.com.ar/");
    }

    [Function("InformarNovedadesDePreciosYProductos")]
    public async Task Run([CosmosDBTrigger(
        databaseName: "SweetDb",
        containerName: "Productos",
        Connection = "cn",
        LeaseContainerName = "Leases",
        LeaseContainerPrefix = "T3")] IEnumerable<JsonObject> cambiosEnDb) {
        
        _log.LogTrace($"{nameof(InformarNovedadesDePreciosYProductos)} comenzada");

        try {
            _log.LogDebug($"Dividiendo documentos cambiados en lotes de {_configuraciones.NumeroDeObjetosPorLote}");
            IEnumerable<JsonObject[]> cambiosEnDbAgrupados = cambiosEnDb.Chunk(_configuraciones.NumeroDeObjetosPorLote);

            var numeroDeLote = 0;
            foreach (JsonObject[] cambios in cambiosEnDbAgrupados) {

                numeroDeLote++;
                var datos = new Dictionary<int, NovedadPow>();

                _log.LogDebug($"Procesando lote {numeroDeLote}");

                foreach (JsonObject doc in cambios) {
                    _log.LogTrace($"Procesando documento {doc["id"]!.GetValue<string>()}");

                    var tipoDeDocumento = doc["TipoDeDocumento"]!.GetValue<string>();

                    (bool esValido, string mensajeDeError) = _validadorDeDocumentos.DeboProcesarDocumento(doc, tipoDeDocumento);
                    if (esValido == false) {
                        _log.LogTrace(mensajeDeError);
                        continue;
                    }

                    ProcesarDocumento(doc, tipoDeDocumento, datos);

                }

                int numeroDeVariantes = datos.Values.Sum(n => n.Variantes.Count);

                if (numeroDeVariantes == 0) {
                    _log.LogDebug($"Lote {numeroDeLote} fue procesado pero no tiene variantes para informar");
                    return;
                }

                _log.LogDebug($"Lote {numeroDeLote} procesado. {numeroDeVariantes} variantes encontradas");

                foreach (NovedadPow novedadPow in datos.Values) {
                    _log.LogInformation($"Enviando {novedadPow.Variantes.Count} novedades de precios y stock");
                    await InformarStockYPreciosAsync(novedadPow);
                }
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
            int sucursalId = doc["sucursal"]?["id"]?.GetValue<int>() ?? -1;

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

    public async Task InformarStockYPreciosAsync(NovedadPow novedadPow) {
        _log.LogTrace($"{nameof(InformarStockYPreciosAsync)} comenzada");

        try {
            
            _log.LogDebug("Serializando las novedades a JSON");
            string json = JsonSerializer.Serialize(novedadPow, _opcionesDeSerializacionPredeterminada);

            for (var reintentos = 0; reintentos < _configuraciones.ReintentosMaximos; reintentos++) {
                try {

                    _log.LogDebug("Enviando novedades a Pow");
                    HttpResponseMessage respuesta = await _clientePow.PostAsync(InformarStockYPrecios, new StringContent(json));

                    respuesta.EnsureSuccessStatusCode();

                    // TODO: LA API devuelve informacion sobre SKUs inexistentes. Se pueden devolver aqui

                    _log.LogDebug("Novedades recibidas exitósamente");
                    return;

                } catch (Exception error) {
                    _log.LogError(error, "No se pudieron enviar las novedades a Pow. Error: {0}. Se reintentará nuevamente", error.Message);

                    if (reintentos == _configuraciones.ReintentosMaximos) {
                        _log.LogError(error, "Se llego al máximo numero de reintentos. No se volverá a reintentar");
                        throw;
                    }
                }

                // Espero antes del proximo reintento
                var rand = new Random();
                int tiempoDeEsperaMs = rand.Next(50, _configuraciones.EsperaMaximaEntreReintentosMs + 1);

                _log.LogWarning($"Esperado {tiempoDeEsperaMs}ms para reintentar...");
                await Task.Delay(tiempoDeEsperaMs);
            }
            

        } finally {
            _log.LogTrace($"{nameof(InformarStockYPreciosAsync)} finalizada");
        }
    }
}
