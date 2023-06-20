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
        _log.LogTrace($"{nameof(InformarStockYPreciosAsync)} comenzada");

        try {
            
            _log.LogDebug("Serializando las novedades a JSON");
            string json = JsonSerializer.Serialize(novedadPow, _opcionesDeSerializacionPredeterminada);
            
            _log.LogDebug("Enviando novedades a Pow");
            HttpResponseMessage respuesta = await _clienteHttp.PostAsync(InformarStockYPrecios, new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json))
                                                              .ConfigureAwait(false);

            respuesta.EnsureSuccessStatusCode();

            // TODO: LA API devuelve informacion sobre SKUs inexistentes. Se pueden devolver aqui

            _log.LogDebug("Novedades recibidas exitósamente");

        } finally {
            _log.LogTrace($"{nameof(InformarStockYPreciosAsync)} finalizada");
        }
    }
}