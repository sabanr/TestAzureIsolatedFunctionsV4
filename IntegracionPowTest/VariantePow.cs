using System.Text.Json.Serialization;

namespace IntegracionPowTest;

public class VariantePow : IComparable<VariantePow>, IComparable<string> {

    [JsonRequired]
    [JsonPropertyOrder(1)]
    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;
    [JsonPropertyOrder(2)]
    [JsonPropertyName("cantidad")]
    public double? Cantidad { get; set; }
    [JsonPropertyOrder(3)]
    [JsonPropertyName("price")]
    public double? Precio { get; set; }
   

    public int CompareTo(VariantePow? other) {
        return other == null ? 1 : string.Compare(Codigo, other.Codigo, StringComparison.CurrentCultureIgnoreCase);
    }

    public int CompareTo(string? other) {
        return string.Compare(Codigo, other, StringComparison.CurrentCultureIgnoreCase);
    }

    public override string ToString() {
        return $"{Codigo} - cantidad: {Cantidad} precio: {Precio:N2}";
    }

}
