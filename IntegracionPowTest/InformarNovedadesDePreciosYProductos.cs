using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IntegracionPowTest
{
    public class InformarNovedadesDePreciosYProductos
    {
        private readonly ILogger _logger;

        public InformarNovedadesDePreciosYProductos(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<InformarNovedadesDePreciosYProductos>();
        }

        [Function("InformarNovedadesDePreciosYProductos")]
        public void Run([CosmosDBTrigger(
            databaseName: "SweetDb",
            collectionName: "Productos",
            ConnectionStringSetting = "cn",
            LeaseCollectionName = "Leases",
            LeaseCollectionPrefix = "T3")] IReadOnlyList<MyDocument> input) {
            
            if (input is not { Count: > 0 }) {
                return;
            }

            _logger.LogInformation("Documents modified: " + input.Count);

            foreach (var myDocument in input) {
                _logger.LogInformation(" - document changed Id: " + input[0].Id);
            }

            
        }
    }

    public class MyDocument
    {
        public string Id { get; set; }

        public string Text { get; set; }

        public int Number { get; set; }

        public bool Boolean { get; set; }
    }
}
