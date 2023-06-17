namespace IntegracionPowTest;

public  class Configuraciones {
    public string SucursalesCsv { get; set; }
    /// <summary>
    /// Lista de sucursales id, cuyos stock y precios vamos a informar
    /// </summary>
    public IEnumerable<int> SucursalesHabilitadas {
        get {
            return string.IsNullOrWhiteSpace(SucursalesCsv) ? Array.Empty<int>() : 
                       SucursalesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => Convert.ToInt32(s));
        }
    }
    /// <summary>
    /// Obtiene el email asociado a la cuenta API de POW
    /// </summary>
    public string PowEmail { get; set; }
    /// <summary>
    /// Obtiene el password asociado a la cuenta API de POW
    /// </summary>
    public string PowPassword { get; set; }
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

