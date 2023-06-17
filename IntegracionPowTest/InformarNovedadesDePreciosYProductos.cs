using System.Text.Json.Nodes;

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

    /// <summary>
    /// Obtiene el email asociado a la cuenta API de POW
    /// </summary>
    private static string PowEmail {
        get { return Environment.GetEnvironmentVariable("powEmail", EnvironmentVariableTarget.Process) ?? string.Empty; }
    }
    /// <summary>
    /// Obtiene el password asociado a la cuenta API de POW
    /// </summary>
    private static string PowPassword {
        get { return Environment.GetEnvironmentVariable("powPassword", EnvironmentVariableTarget.Process) ?? string.Empty; }
    }

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
        LeaseContainerPrefix = "T3")] IEnumerable<JsonObject> input) {
        
        _log.LogTrace($"{nameof(InformarNovedadesDePreciosYProductos)} comenzada");

        try {

            if (_configuraciones.SucursalesHabilitadas.Any() == false) {
                _log.LogTrace("No hay sucursales habilitadas, nada para procesar");

                return;
            }

            _log.LogDebug($"Leyendo documentos cambiados");

            IEnumerable<JsonObject[]> documentosDivididos = input.Chunk(_configuraciones.NumeroDeObjetosPorLote);

            foreach (JsonObject[] documentos in documentosDivididos) {

                // generar json
                var pedidoJson = new JsonObject
                {
                    ["email"] = PowEmail,
                    ["password"] = PowPassword,
                    ["sucursalId"] = null
                };

                var variantes = new JsonArray();
                
                foreach (JsonObject documento in documentos) {
                    _log.LogDebug($"Procesando documento {documento["id"]}");

                    if (string.CompareOrdinal(documento["tipoDeDocumento"]!.GetValue<string>(), TipoDeDocumentoPrecios) != 0 && 
                        string.CompareOrdinal(documento["tipoDeDocumento"]!.GetValue<string>(), TipoDeDocumentoStock) != 0) {

                        _log.LogTrace($"Salteo documento {documento["id"]!.GetValue<string>()}. No es del tipo requerido. Tipo: {documento["tipoDeDocumento"]!.GetValue<string>()}");
                        continue;
                    }

                    if (_configuraciones.SucursalesHabilitadas.Contains(documento["sucursales"]!["id"]!.GetValue<int>()) == false) {

                        _log.LogTrace($"Salteo documento {documento["id"]!.GetValue<string>()}. No es de una sucursal habilitada. Sucursal: {documento["sucursal"]!["descripcion"]!.GetValue<string>()}");
                        continue;
                    }

                    AgregarDocumento(documento, pedidoJson);

                }
                
                _log.LogTrace("Documento listo para ser enviado");
                
                var pedidoString = pedidoJson.ToString();

                _log.LogDebug(pedidoString);

                await EnviarNovedadesConReintentosAsync(pedidoString, _configuraciones.ReintentosMaximos, _configuraciones.EsperaMaximaEntreReintentosMs);
                
                _log.LogInformation($"Cambios de precios y stock enviados correctamente");
            }
         
        } catch (Exception ex) {
            _log.LogCritical(ex.Message);

        } finally {
            _log.LogTrace($"{nameof(InformarNovedadesDePreciosYProductos)}ecios terminada");
        }
        
    }

    private static void AgregarDocumento(JsonObject documento, JsonObject pedidoJson) {
        // TODO: crear el objeto json

    }

    private async Task EnviarNovedadesConReintentosAsync(string novedades, int reintentosMaximos, int esperaMaximaEntreReintentosMs) {
        _log.LogTrace($"{nameof(EnviarNovedadesConReintentosAsync)} comenzada");

        try {
            for (var reintentos = 0; reintentos < reintentosMaximos; reintentos++) {
                try {
                    _log.LogTrace($"{nameof(EnviarNovedadesConReintentosAsync)} enviando novedad a Pow");
                    HttpResponseMessage respuesta = await _clienteHttp.PostAsync("", new StringContent(novedades));

                    // TODO: manejar el caso de SKUs inexistentes.

                    respuesta.EnsureSuccessStatusCode();

                    return;

                } catch (Exception error) {
                    _log.LogError($"No se pudo enviar el pedido a Pow. {error.Message}");
                    if (reintentos == reintentosMaximos) {
                        throw;
                    }
                }

                // Espero antes del proximo reintento
                var rand = new Random();
                int tiempoDeEsperaMs = rand.Next(50, esperaMaximaEntreReintentosMs + 1);

                _log.LogWarning($"Esperado {tiempoDeEsperaMs}ms para reintentar...");
                await Task.Delay(tiempoDeEsperaMs);
            }

        } finally {
            _log.LogTrace($"{nameof(EnviarNovedadesConReintentosAsync)} finalizada");
        }
    }

}

