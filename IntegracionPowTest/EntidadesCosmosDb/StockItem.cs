﻿using System.Text.Json.Serialization;

namespace IntegracionPowTest.EntidadesCosmosDb;

public record StockItem {
    [JsonPropertyName("codigoDeTalle")]
    public string CodigoDeTalle { get; set; } = string.Empty;
    [JsonPropertyName("talle")]
    public string Talle { get; set; } = string.Empty;
    [JsonPropertyName("cantidad")]
    public double Cantidad { get; set; }
}

