using IntegracionPowTest.Enumeradores;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace IntegracionPowTest.Validadores;

public static class ValidadorDeDocumentos {
    public static (bool esValido, string error) DeboProcesar(ILogger log, JsonNode doc, string tipoDeDocumento, Configuraciones configuraciones) {
        log.LogTrace($"{nameof(DeboProcesar)} comenzada");

        if (string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoStock) != 0 && 
            string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoPrecios) != 0) {
            return (false, $"El documento {doc["Id"]!.GetValue<string>()} no es de un tipo invalido.");
        }

        if (string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoStock) == 0 &&
            configuraciones.SucursalesHabilitadas.Contains(doc["sucursal"]!["Id"]!.GetValue<int>()) == false) {
            return (false, $"El documento {doc["Id"]!.GetValue<string>()}. No es de una sucursal habilitada. Sucursal: {doc["sucursal"]!["descripcion"]!.GetValue<string>()}");
        }

        if (string.CompareOrdinal(tipoDeDocumento, FabricaDeEnumeradoresDeSkus.TipoDeDocumentoPrecios) == 0 && 
            configuraciones.ListaDePreciosId != doc["listaDePreciosId"]!.GetValue<int>()) {
            return (false, $"El documento {doc["Id"]!.GetValue<string>()}. No es de una lista de precios habilitada. Lista: {doc["listaDePrecios"]!.GetValue<string>()}");
        }

        log.LogTrace($"{nameof(DeboProcesar)} terminada");
        return (true, "");
    }
}

