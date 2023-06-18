using System.Text.Json.Serialization;

namespace IntegracionPowTest.EntidadesPow;
public class NovedadPow
{
    [JsonRequired]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    [JsonRequired]
    [JsonPropertyName("sucursalid")]
    public int SucursalId { get; set; }

    [JsonRequired]
    [JsonPropertyName("variants")]
    public SortedSet<VariantePow> Variantes { get; set; } = new SortedSet<VariantePow>();

}