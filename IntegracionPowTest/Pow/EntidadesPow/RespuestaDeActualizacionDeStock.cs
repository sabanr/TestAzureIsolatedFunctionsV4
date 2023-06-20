using System.Text.Json.Serialization;

namespace IntegracionPowTest.Pow.EntidadesPow;

public class RespuestaDeActualizacionDeStock {
    [JsonPropertyName("json_stock_updater")]
    public List<List<string>> JsonStockUpdater { get; set; }
}

