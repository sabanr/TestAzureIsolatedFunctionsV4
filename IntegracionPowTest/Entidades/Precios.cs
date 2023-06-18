using System.Text.Json.Serialization;

namespace IntegracionPowTest.Entidades;

public record Precios {

    [JsonPropertyName("codigoDeColor")]
    public string CodigoDeColor { get; set;} = string.Empty;
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
    [JsonPropertyName("preciosPorTalle")]
    public List<PreciosPorTalle> PreciosPorTalle { get; set; } = new List<PreciosPorTalle>()

}
