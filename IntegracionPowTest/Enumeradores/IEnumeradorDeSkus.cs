using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;

namespace IntegracionPowTest.Enumeradores;

public interface IEnumeradorDeSkus {

    IEnumerable<KeyValuePair<string, double>> Skus(ILogger log, JsonNode doc);

}
