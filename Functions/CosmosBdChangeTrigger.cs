using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PizzaFunction.Models;
using System.Text.Json;

namespace PizzaFunction.Functions
{
    public class CosmosBdChangeTrigger
    {
        private readonly ILogger _logger;

        public CosmosBdChangeTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CosmosBdChangeTrigger>();
        }

        [Function("CosmosBdChangeTrigger")]
        [SignalROutput(HubName = "orders", ConnectionStringSetting = "AzureSignalRConnectionString")]
        public SignalRMessageAction Run(
            [CosmosDBTrigger(
            databaseName: "Resturant",
            containerName: "Orders",
            Connection = "CosmosDBConnection",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<SendModel> input)
        {

            if (input != null && input.Count > 0)
            {
                _logger.LogInformation($"Documents modified: {input.Count}");
                // _logger.LogInformation($"First document Id: {input[0].id}");
                _logger.LogInformation($"First document Id: {input[0].OrderNo}");
                _logger.LogInformation($"Skickar uppdatering till SignalR: {JsonSerializer.Serialize(input[0])}");

                return new SignalRMessageAction("orderUpdated")
                {
                    Arguments = new[] { input[0] }
                };
            }
            return null;
        }
    }
}
