using System.Text.Json.Serialization;

namespace IntegracionPowTest.Entidades;

public record PreciosPorTalle {

    [JsonPropertyName("codigoDeTalle")]
    public string CodigoDeTalle { get; set; } = string.Empty;

    [JsonPropertyName("talle")]
    public string Talle { get; set; } = string.Empty;
    [JsonPropertyName("precio")]
    public double Precio { get; set; }

}
