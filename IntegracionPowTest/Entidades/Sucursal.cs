using System.Text.Json.Serialization;

namespace IntegracionPowTest.Entidades;

public record Sucursal {
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;
}

