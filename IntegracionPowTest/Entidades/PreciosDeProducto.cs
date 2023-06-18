using System.Text.Json.Serialization;

namespace IntegracionPowTest.Entidades;

public record PreciosDeProducto {
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("tipoDeDocumento")]
    public string TipoDeDocumento { get; set; } = string.Empty;
    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;
    [JsonPropertyName("listaDePreciosId")]
    public int ListaDePreciosId { get; set; }
    [JsonPropertyName("listaDePrecios")]
    public string ListaDePrecios { get; set; } = string.Empty;
    [JsonPropertyName("precios")]
    public List<Precios> Precios { get; set; } = new List<Precios>();
}