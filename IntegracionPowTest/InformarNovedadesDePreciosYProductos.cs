using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml;

using IntegracionPowTest.Entidades;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegracionPowTest;
public class InformarNovedadesDePreciosYProductos
{
    private readonly ILogger _log;
    private readonly HttpClient _clienteHttp;
    private readonly Configuraciones _configuraciones;

    private const string TipoDeDocumentoPrecios = "PreciosDeProducto";
    private const string TipoDeDocumentoStock = "StockDeProducto";

    private readonly JsonSerializerOptions OpcionesDeSerializacionPredeterminada = new JsonSerializerOptions {
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

        // TODO: LogDebug las configuraciones ?

        try {

            if (_configuraciones.SucursalesHabilitadas.Any() == false) {
                _log.LogWarning("No hay sucursales habilitadas, nada para procesar");
                return;
            }

            _log.LogDebug($"Leyendo documentos cambiados");

            IEnumerable<JsonObject[]> cambiosAgrupados = cambios.Chunk(_configuraciones.NumeroDeObjetosPorLote);

            foreach (JsonObject[] documentos in cambiosAgrupados) {

                var datos = new Dictionary<int, NovedadPow>();

                foreach (JsonObject doc in documentos) {
                    _log.LogDebug($"Procesando documento {doc["Id"]}");

                    var tipoDeDocumento = doc["TipoDeDocumento"]!.GetValue<string>();

                    if (EsDocValido(doc,  tipoDeDocumento) == false) 
                        continue;

                    ProcesarDocumento(doc, tipoDeDocumento, datos);

                }
                
                _log.LogDebug("Documento creado y listo para ser enviado");

                await EnviarNovedadesConReintentosAsync(datos, _configuraciones.ReintentosMaximos, _configuraciones.EsperaMaximaEntreReintentosMs);
                
                _log.LogInformation($"Cambios de precios y stock enviados correctamente");
            }
         
        } catch (Exception ex) {
            _log.LogCritical(ex.Message);

        } finally {
            _log.LogTrace($"{nameof(InformarNovedadesDePreciosYProductos)} terminada");
        }
        
    }

    private bool EsDocValido(JsonObject doc, string tipoDeDocumento) {
        _log.LogTrace($"{nameof(EsDocValido)} comenzada");

        if (string.CompareOrdinal(tipoDeDocumento, TipoDeDocumentoStock) != 0 && 
            string.CompareOrdinal(tipoDeDocumento, TipoDeDocumentoPrecios) != 0) {
            
            _log.LogTrace($"El documento {doc["Id"]!.GetValue<string>()} no es de un tipo invalido.");
            return false;
        }

        if (string.CompareOrdinal(tipoDeDocumento, TipoDeDocumentoStock) == 0 &&
            _configuraciones.SucursalesHabilitadas.Contains(doc["sucursal"]!["Id"]!.GetValue<int>()) == false) {

            _log.LogTrace($"El documento {doc["Id"]!.GetValue<string>()}. No es de una sucursal habilitada. Sucursal: {doc["sucursal"]!["descripcion"]!.GetValue<string>()}");
            return false;
        }

        if (string.CompareOrdinal(tipoDeDocumento, TipoDeDocumentoPrecios) == 0 && 
            _configuraciones.ListaDePreciosId != doc["listaDePreciosId"]!.GetValue<int>()) {

            _log.LogTrace($"El documento {doc["Id"]!.GetValue<string>()}. No es de una lista de precios habilitada. Lista: {doc["listaDePrecios"]!.GetValue<string>()}");
            return false;
        }

        _log.LogTrace($"{nameof(EsDocValido)} terminada");
        return true;
    }

    private void ProcesarDocumento(JsonObject doc, string tipoDeDocumento, Dictionary<int, NovedadPow> datos) {
        _log.LogTrace($"{nameof(ProcesarDocumento)} comenzada");

        try {
            int sucursalId = doc["sucursal"]?["Id"]?.GetValue<int>() ?? -1;

            if (datos.TryGetValue(sucursalId, out NovedadPow? novedad) == false)
            {
                novedad = new NovedadPow
                {
                    Email = _configuraciones.PowEmail,
                    Password = _configuraciones.PowPassword,
                    SucursalId = sucursalId,
                    Variantes = new SortedSet<VariantePow>()
                };
                datos.Add(sucursalId, novedad);
            }

            foreach (KeyValuePair<string, double> sku in ObtenerEnumeradorDeSkus(tipoDeDocumento, doc)) {

                if (novedad.Variantes.TryGetValue(new VariantePow { Codigo = sku.Key }, out VariantePow? variante) == false) {
                    variante = new VariantePow
                    {
                        Codigo = sku.Key
                    };
                    novedad.Variantes.Add(variante);
                }

                switch (tipoDeDocumento) {
                    case TipoDeDocumentoStock:
                        variante.Cantidad = sku.Value;

                        break;

                    case TipoDeDocumentoPrecios:
                        variante.Precio = sku.Value;

                        break;
                }
            }

        } finally {
            _log.LogTrace($"{nameof(ProcesarDocumento)} terminada");
        }
    }

    private IEnumerable<KeyValuePair<string, double>> ObtenerEnumeradorDeSkus(string tipoDeDocumento, JsonObject doc) => tipoDeDocumento switch
    {
        TipoDeDocumentoStock => ObtenerEnumeradorDeSkusDeStock(doc),
        TipoDeDocumentoPrecios => ObtenerEnumeradorDeSkusDePrecios(doc),
        _ => throw new ArgumentException(tipoDeDocumento),
    };

    private IEnumerable<KeyValuePair<string, double>> ObtenerEnumeradorDeSkusDeStock(JsonObject doc) {
        _log.LogTrace($"{nameof(ObtenerEnumeradorDeSkusDeStock)} comenzada");

        try {

            if (doc == null) 
                throw new ArgumentNullException(nameof(doc));

            var stockDeProducto = JsonSerializer.Deserialize<StockDeProducto>(doc);

            if (stockDeProducto == null) 
                throw new ArgumentNullException(nameof(stockDeProducto));

            foreach (StockItem stockItem in stockDeProducto!.Stock) {
                var sku = $"{stockDeProducto.Codigo}{stockDeProducto.CodigoDeColor}.{stockItem.CodigoDeTalle}";
                yield return new KeyValuePair<string, double>(sku, stockItem.Cantidad);
            }

        } finally {
            _log.LogTrace($"{nameof(ObtenerEnumeradorDeSkusDeStock)} terminada");
        }
    }
    private static IEnumerable<KeyValuePair<string, double>> ObtenerEnumeradorDeSkusDePrecios(JsonObject doc) { throw new NotImplementedException(); }

    private async Task EnviarNovedadesConReintentosAsync( Dictionary<int, NovedadPow> datos, int reintentosMaximos, int esperaMaximaEntreReintentosMs)
    {
        _log.LogTrace($"{nameof(EnviarNovedadesConReintentosAsync)} comenzada");

        try {
            foreach (KeyValuePair<int, NovedadPow> novedadPow in datos) {
                string json = JsonSerializer.Serialize<NovedadPow>(novedadPow.Value, OpcionesDeSerializacionPredeterminada);

                for (var reintentos = 0; reintentos < reintentosMaximos; reintentos++) {
                    try {

                        _log.LogDebug($"{nameof(EnviarNovedadesConReintentosAsync)} enviando novedades a Pow");
                        HttpResponseMessage respuesta = await _clienteHttp.PostAsync("http://staging.sweet.com.ar/json_stock_update", new StringContent(json));

                        // TODO: manejar el caso de SKUs inexistentes.

                        respuesta.EnsureSuccessStatusCode();

                        return;

                    } catch (Exception error) {
                        _log.LogError($"No se pudo enviar el pedido a Pow. {error.Message}");
                        if (reintentos == reintentosMaximos)
                            throw;
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
