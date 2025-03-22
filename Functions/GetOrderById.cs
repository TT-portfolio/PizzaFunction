using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PizzaFunction.Models;
using System.Text.Json;

namespace PizzaFunction.Functions
{
    public class GetOrderById
    {
        private readonly ILogger<GetOrderById> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly SecretClient _secretClient;
        private readonly string _databaseName = "Resturant";
        private readonly string _containerName = "Orders";


        public GetOrderById(ILogger<GetOrderById> logger)
        {
            _logger = logger;
            string keyVaultName = Environment.GetEnvironmentVariable("KEYVAULT_NAME");
            string keyVaultUri = $"https://{keyVaultName}.vault.azure.net/";
            _secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

            var cosmosDbConnectionString = _secretClient.GetSecret("PizzaOrderCosmos").Value.Value;
            _cosmosClient = new CosmosClient(cosmosDbConnectionString, new CosmosClientOptions { SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } });

        }

        [Function("GetOrderById")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            string orderId;

            if (req.Method == "GET")
            {
                orderId = req.Query["id"];
            }else
            {
                using var reader = new StreamReader(req.Body);
                var requestBody = await reader.ReadToEndAsync();
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);
                orderId = data?["id"];
            }

            if (string.IsNullOrEmpty(orderId))
            {
                _logger.LogWarning("No Id provided in the request");
                return new BadRequestObjectResult("Please provide id");
            }

            try
            {
                var container = _cosmosClient.GetContainer(_databaseName, _containerName);
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", orderId);

                using FeedIterator<Order> iterator = container.GetItemQueryIterator<Order>(query);
                List<Order> orders = new List<Order>();

                while (iterator.HasMoreResults)
                {
                    FeedResponse<Order> response = await iterator.ReadNextAsync();
                    orders.AddRange(response);
                }

                if (!orders.Any())
                {
                    _logger.LogWarning($"Order with ID {orderId} not found.");
                    return new NotFoundObjectResult($"Order with ID {orderId} not found.");
                }
                _logger.LogInformation($"Order with ID {orderId} retrieved successfully.");

                var jsonResponse = JsonSerializer.Serialize(orders.First(), new JsonSerializerOptions { WriteIndented = true });
                return new OkObjectResult(jsonResponse);
            }
            catch (CosmosException cosmosEx)
            {
                _logger.LogError($"CosmosDB error: {cosmosEx.StatusCode} - {cosmosEx.Message}");
                return new StatusCodeResult((int)cosmosEx.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retriving order: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }
    }
}

