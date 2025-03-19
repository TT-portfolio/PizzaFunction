using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PizzaFunction.Models;
using System.Text.Json;

namespace PizzaFunction.Functions
{
    public class RemoveActiveOrderChangeTrigger
    {
        private readonly ILogger _logger;

        public RemoveActiveOrderChangeTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RemoveActiveOrderChangeTrigger>();
        }

        [Function("RemoveActiveOrderChangeTrigger")]
        [SignalROutput(HubName = "orders", ConnectionStringSetting = "AzureSignalRConnectionString")]
        public SignalRMessageAction Run(
            [CosmosDBTrigger(
            databaseName: "Resturant",
            containerName: "DailyCompletedOrders",
            Connection = "CosmosDBConnection",
            LeaseContainerName = "leaseCompletedDaily",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<SendModel> input)
        {

            if (input != null && input.Count > 0)
            {

                _logger.LogInformation($"Documents modified: {input.Count}");
                _logger.LogInformation($"First document Id: {input[0].id}");
                _logger.LogInformation($"Skickar uppdatering till SignalR: {JsonSerializer.Serialize(input[0])}");
                _logger.LogInformation($"Försöker skicka SingalRMess");
                _logger.LogInformation($"Dokumentdata: {JsonSerializer.Serialize(input[0])}");

                return new SignalRMessageAction("orderUpdated")
                {

                    Arguments = new[] { input[0] }
                };
            }
            return null;
        }
    }
}
