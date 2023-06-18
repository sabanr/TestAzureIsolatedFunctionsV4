using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;

namespace IntegracionPowTest.Enumeradores;

public static class FabricaDeEnumeradoresDeSkus {

    public const string TipoDeDocumentoPrecios = "PreciosDeProducto";
    public const string TipoDeDocumentoStock = "StockDeProducto";
    public static IEnumeradorDeSkus ObtenerEnumeradorDeSkus(ILogger log, string tipoDeDocumento, JsonNode doc) => tipoDeDocumento switch
    {
        TipoDeDocumentoStock => new EnumeradorDeSkusDeStock(),
        TipoDeDocumentoPrecios => new EnumeradorDeSkusDePrecios(),
        _ => throw new ArgumentException(tipoDeDocumento),
    };

}

