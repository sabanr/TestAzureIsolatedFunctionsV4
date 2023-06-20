using System.Text.Json;
using System.Text.Json.Nodes;

using IntegracionPowTest.EntidadesCosmosDb;

using Microsoft.Extensions.Logging;

namespace IntegracionPowTest.Enumeradores;

public class EnumeradorDeSkusDePrecios : IEnumeradorDeSkus {

    public IEnumerable<KeyValuePair<string, double>> Skus(ILogger log, JsonNode doc) {
        log.LogTrace($"{nameof(Skus)} comenzada");

        try {

            var preciosDeProducto = doc.Deserialize<PreciosDeProducto>();

            foreach (Precios precios in preciosDeProducto!.Precios) {
                foreach (PreciosPorTalle preciosPorTalle in precios.PreciosPorTalle) {
                    var sku = $"{preciosDeProducto.Codigo}{precios.CodigoDeColor}.{preciosPorTalle.CodigoDeTalle}";
                    yield return new KeyValuePair<string, double>(sku, preciosPorTalle.Precio);
                }
            }

        } finally {
            log.LogTrace($"{nameof(Skus)} terminada");
        }
    }

}
