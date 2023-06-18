using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using IntegracionPowTest.Enumeradores;

namespace IntegracionPowTest.Validadores;

public class ValidadorDeDocumentos {

    private readonly ILogger _log;
    private readonly Configuraciones _configuraciones;

    public ValidadorDeDocumentos(ILoggerFactory creadorDeLoggers, IOptions<Configuraciones> opciones) {
        _log = creadorDeLoggers.CreateLogger<ValidadorDeDocumentos>();
        _configuraciones = opciones.Value;
    }

    public (bool esValido, string error) DeboProcesarDocumento(JsonNode doc, string tipoDeDocumento) {
        _log.LogTrace($"{nameof(DeboProcesarDocumento)} comenzada");

        try {
            var docId = doc["Id"]!.GetValue<int>();
            if (string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoStock) != 0 &&
                string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoPrecios) != 0) {
                return (false, $"El documento {docId} de tipo {tipoDeDocumento} no es de un tipo valido.");
            }

            if ((string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoStock) == 0 &&
                _configuraciones.SucursalesHabilitadas.Contains(docId)) == false) {
                return (false, $"El documento {docId} no es de una sucursal habilitada. Sucursal: {doc["sucursal"]!["descripcion"]!.GetValue<string>()}");
            }

            if (string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoPrecios) == 0 &&
                _configuraciones.ListaDePreciosId != doc["listaDePreciosId"]!.GetValue<int>()) {
                return (false, $"El documento {docId}. No es de una lista de precios habilitada. Lista: {doc["listaDePrecios"]!.GetValue<string>()}");
            }

            return (true, "");

        } finally {
            _log.LogTrace($"{nameof(DeboProcesarDocumento)} terminada");
        }
    }
}

