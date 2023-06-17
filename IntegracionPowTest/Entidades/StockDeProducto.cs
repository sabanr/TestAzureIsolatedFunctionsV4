using System.Text.Json.Serialization;

namespace IntegracionPowTest.Entidades;

public class StockDeProducto {
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("tipoDeDocumento")]
    public string TipoDeDocumento { get; set; } = string.Empty;

    [JsonPropertyName("sucursal")]
    public Sucursal Sucursal { get; set; } = new Sucursal();

    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;
    [JsonPropertyName("codigoDeColor")]
    public string CodigoDeColor { get; set; } = string.Empty;
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
    [JsonPropertyName("stock")]
    public List<StockItem> Stock { get; set; } = new List<StockItem>();
}

