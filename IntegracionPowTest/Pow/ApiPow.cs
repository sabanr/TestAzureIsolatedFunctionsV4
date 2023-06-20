using System.Net.Mime;
using System.Text;
using System.Text.Json;

using IntegracionPowTest.Pow.EntidadesPow;

using Microsoft.Extensions.Logging;

namespace IntegracionPowTest.Pow;

public class ApiPow : IApiPow {

    private const string InformarStockYPrecios = "json_stock_update";

    private readonly ILogger<ApiPow> _log;
    private readonly HttpClient _clienteHttp;
    private readonly JsonSerializerOptions _opcionesDeSerializacionPredeterminada;

    public ApiPow(ILoggerFactory creadorDeLogs, HttpClient clienteHttp, JsonSerializerOptions opcionesDeSerializacionPredeterminada) {
        _log = creadorDeLogs.CreateLogger<ApiPow>();
        _clienteHttp = clienteHttp;
        _opcionesDeSerializacionPredeterminada = opcionesDeSerializacionPredeterminada;
    }

    public async Task InformarStockYPreciosAsync(NovedadPow novedadPow) {
        _log.LogTrace("{nom} comenzada", nameof(InformarStockYPreciosAsync));

        try {
            
            _log.LogDebug("Serializando las novedades a JSON");
            string json = JsonSerializer.Serialize(novedadPow, _opcionesDeSerializacionPredeterminada);
            
            _log.LogDebug("Enviando novedades a Pow");
            HttpResponseMessage respuestaHttp = await _clienteHttp.PostAsync(InformarStockYPrecios, new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json))
                                                              .ConfigureAwait(false);

            respuestaHttp.EnsureSuccessStatusCode();
            _log.LogDebug("Novedades informadas exitósamente");
            
            //string contenidoDeRespuesta = await respuestaHttp.Content.ReadAsStringAsync()
            //                                         .ConfigureAwait(false);

            //try
            //{
            //    var respuesta = JsonSerializer.Deserialize<RespuestaDeActualizacionDeStock>(contenidoDeRespuesta);
            //    var numeroDeSkusNoExistentes = respuesta?.JsonStockUpdater?.Count(item => item is [_, "not found"]) ?? 0;

            //    _log.LogInformation("Pow informa {sku} SKUs no encontrados", numeroDeSkusNoExistentes);
            //}
            //catch (Exception)
            //{

            //    throw;
            //}

        } finally {
            _log.LogTrace("{nom} finalizada", nameof(InformarStockYPreciosAsync));
        }
    }
}