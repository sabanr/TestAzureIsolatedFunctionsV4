using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IntegracionPowTest
{
    public class InformarNovedadesDePreciosYProductos
    {
        private readonly ILogger _logger;

        public InformarNovedadesDePreciosYProductos(ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger<InformarNovedadesDePreciosYProductos>();
        }

        [Function("InformarNovedadesDePreciosYProductos")]
        public void Run([CosmosDBTrigger(
            databaseName: "SweetDb",
            containerName: "Productos",
            Connection = "cn",
            LeaseContainerName = "Leases",
            LeaseContainerPrefix = "T3")] IReadOnlyList<JsonObject> input) {
            
            if (input is not { Count: > 0 }) {
                return;
            }
            _logger.LogInformation("Documents modified: " + input.Count);

            foreach (JsonObject jsonObject in input) {
                _logger.LogInformation(" - document changed Id: " + jsonObject["id"]
                                           ?.GetValue<string>() ?? string.Empty);
            }

            
        }
    }

}
