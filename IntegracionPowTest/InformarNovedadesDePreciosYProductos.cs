#nullable enable
using System.Text.Json.Nodes;

using IntegracionPowTest.Enumeradores;
using IntegracionPowTest.Pow;
using IntegracionPowTest.Pow.EntidadesPow;
using IntegracionPowTest.Validadores;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegracionPowTest;

public class InformarNovedadesDePreciosYProductos {

    private readonly ILogger<InformarNovedadesDePreciosYProductos> _log;
    private readonly Configuraciones _configuraciones;
    private readonly ValidadorDeDocumentos _validadorDeDocumentos;
    private readonly IApiPow _apiPow;
    
    public InformarNovedadesDePreciosYProductos(ILoggerFactory creadorDeLoggers, IApiPow apiPow, IOptions<Configuraciones> opciones, ValidadorDeDocumentos validadorDeDocumentos) {
        _log = creadorDeLoggers.CreateLogger<InformarNovedadesDePreciosYProductos>();
        _configuraciones = opciones.Value;
        _validadorDeDocumentos = validadorDeDocumentos;
        _apiPow = apiPow;
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
            _log.LogDebug("Dividiendo documentos cambiados en lotes de {num}", _configuraciones.NumeroDeObjetosPorLote);
            IEnumerable<JsonObject[]> cambiosEnDbAgrupados = cambiosEnDb.Chunk(_configuraciones.NumeroDeObjetosPorLote);

            var numeroDeLote = 0;
            foreach (JsonObject[] cambios in cambiosEnDbAgrupados) {

                numeroDeLote++;
                var datos = new Dictionary<int, NovedadPow>();

                _log.LogDebug("Procesando lote {num}...", numeroDeLote);

                foreach (JsonObject doc in cambios) {
                    _log.LogTrace("Procesando documento {id}...", doc["id"]!.GetValue<string>());

                    var tipoDeDocumento = doc["TipoDeDocumento"]!.GetValue<string>();

                    (bool esValido, string mensajeDeError) = _validadorDeDocumentos.DeboProcesarDocumento(doc, tipoDeDocumento);
                    if (esValido == false) {
                        _log.LogTrace("{msg}", mensajeDeError);
                        continue;
                    }

                    ProcesarDocumento(doc, tipoDeDocumento, datos);

                }

                int numeroDeVariantes = datos.Values.Sum(n => n.Variantes.Count);

                if (numeroDeVariantes == 0) {
                    _log.LogDebug("Lote {lote} fue procesado pero no tiene variantes para informar", numeroDeLote);
                    return;
                }

                _log.LogDebug("Lote {lote} procesado. {variantes} variantes encontradas", numeroDeLote, numeroDeVariantes);

                foreach (NovedadPow novedadPow in datos.Values) {
                    _log.LogInformation("Enviando {variantes} novedades de precios y stock", novedadPow.Variantes.Count);
                    await _apiPow.InformarStockYPreciosAsync(novedadPow);
                }
            }
         
        } catch (Exception ex) {
            _log.LogCritical(ex, "Error critico encontrado. {e}", ex.Message);

            throw;

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

            IEnumeradorDeSkus enumeradorDeSkus = FabricaDeEnumeradoresDeSkus.ObtenerEnumeradorDeSkus(tipoDeDocumento);
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
}
