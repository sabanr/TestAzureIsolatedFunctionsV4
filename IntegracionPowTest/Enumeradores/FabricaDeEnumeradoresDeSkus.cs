namespace IntegracionPowTest.Enumeradores;

public static class FabricaDeEnumeradoresDeSkus {

    public const string TipoDeDocumentoPrecios = "PreciosDeProducto";
    public const string TipoDeDocumentoStock = "StockDeProducto";
    public static IEnumeradorDeSkus ObtenerEnumeradorDeSkus(string tipoDeDocumento) => tipoDeDocumento switch
    {
        TipoDeDocumentoStock => new EnumeradorDeSkusDeStock(),
        TipoDeDocumentoPrecios => new EnumeradorDeSkusDePrecios(),
        _ => throw new ArgumentException(tipoDeDocumento),
    };

}

