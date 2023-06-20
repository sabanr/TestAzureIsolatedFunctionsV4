using System.Text.Json.Serialization;

namespace IntegracionPowTest.Pow.EntidadesPow;

public record RespuestaDeActualizacionDeStock {
    [JsonPropertyName("json_stock_updater")]
    public IReadOnlyList<List<string>> JsonStockUpdater { get; set; }
}

