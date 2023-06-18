using System.Text.Json;
using System.Text.Json.Nodes;

using IntegracionPowTest.Entidades;

using Microsoft.Extensions.Logging;

namespace IntegracionPowTest.Enumeradores;

public class EnumeradorDeSkusDeStock : IEnumeradorDeSkus {

    public IEnumerable<KeyValuePair<string, double>> Skus(ILogger log, JsonNode doc) {
        log.LogTrace($"{nameof(Skus)} comenzada");

        try {

            var stockDeProducto = doc.Deserialize<StockDeProducto>();

            foreach (StockItem stockItem in stockDeProducto!.Stock) {
                var sku = $"{stockDeProducto.Codigo}{stockDeProducto.CodigoDeColor}.{stockItem.CodigoDeTalle}";
                yield return new KeyValuePair<string, double>(sku, stockItem.Cantidad);
            }

        } finally {
            log.LogTrace($"{nameof(Skus)} terminada");
        }
    }

}
