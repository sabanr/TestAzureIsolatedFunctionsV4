using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using IntegracionPowTest.Enumeradores;
namespace IntegracionPowTest.Validadores;

public class ValidadorDeDocumentos {

    private readonly ILogger<ValidadorDeDocumentos> _log;
    private readonly Configuraciones _configuraciones;

    public ValidadorDeDocumentos(ILoggerFactory creadorDeLoggers, IOptions<Configuraciones> opciones) {
        _log = creadorDeLoggers.CreateLogger<ValidadorDeDocumentos>();
        _configuraciones = opciones.Value;
    }

    public (bool esValido, string error) DeboProcesarDocumento(JsonNode doc, string tipoDeDocumento) {
        _log.LogTrace("{0} comenzada", nameof(DeboProcesarDocumento));

        try {

            var docId = doc["id"]?.GetValue<string>();

            if (string.IsNullOrEmpty(tipoDeDocumento)) {
                return (false, $"El documento {docId} no tiene tipo de documento!");
            }

            if (string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoStock) != 0 &&
                string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoPrecios) != 0) {
                return (false, $"El documento {docId} de tipo {tipoDeDocumento} no es de un tipo valido.");
            }

            if (string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoStock) == 0) {
                var sucursalId = doc["sucursal"]!["id"]!.GetValue<int>();

                return _configuraciones.SucursalesHabilitadas.Contains(sucursalId) == false ? (false, $"El documento {docId} no es de una sucursal habilitada. Sucursal: {doc["sucursal"]!["descripcion"]!.GetValue<string>()}") : (true, "");
            }

            if (string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoPrecios) == 0) {
                var listaDePrecioId = doc["listaDePreciosId"]!.GetValue<int>();

                if (_configuraciones.ListaDePreciosId != listaDePrecioId) {
                    return (false, $"El documento {docId}. No es de una lista de precios habilitada. Lista: {doc["listaDePrecios"]!.GetValue<string>()}");
                }
            }

            return (true, "");

        } catch (Exception errorEnValidacion) {
            _log.LogError(errorEnValidacion,"No se pudo validar el documento. {0}", errorEnValidacion.Message);

            throw;

        } finally {
            _log.LogTrace("{0} terminada", nameof(DeboProcesarDocumento));
        }
    }
}

