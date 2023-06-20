namespace IntegracionPowTest;

public  class Configuraciones {

    public string SucursalesCsv { get; set; } = string.Empty;

    /// <summary>
    /// Lista de sucursales Id, cuyos stock vamos a informar
    /// </summary>
    public HashSet<int> SucursalesHabilitadas { get; set; } = new HashSet<int>();

    /// <summary>
    /// Lista de precios Id, cuyos precios vamos a informar
    /// </summary>
    public int ListaDePreciosId { get; set; }
    /// <summary>
    /// Obtiene el email asociado a la cuenta API de POW
    /// </summary>
    public string PowEmail { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene el password asociado a la cuenta API de POW
    /// </summary>
    public string PowPassword { get; set; } = string.Empty;
    /// <summary>
    /// El número máximo de elementos a enviar por vez. 
    /// </summary>
    public int NumeroDeObjetosPorLote { get; set; }
    /// <summary>
    /// Número de reintentos máximo al enviar un lote de novedades
    /// </summary>
    public int ReintentosMaximos { get; set; } 
    /// <summary>
    /// Espera máxima entre reintentos en milisegundos
    /// </summary>
    public int EsperaMaximaEntreReintentosMs { get; set; }

}

