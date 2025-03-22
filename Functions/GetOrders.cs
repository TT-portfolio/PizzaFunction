using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PizzaFunction.Models;
using System.Security.Cryptography.Xml;

namespace PizzaFunction.Functions
{
    public class GetOrders(ILogger<GetOrders> logger)
    {
        private readonly ILogger<GetOrders> _logger = logger;
        private static readonly string KeyVaultName = Environment.GetEnvironmentVariable("KEYVAULT_NAME");
        private static readonly string KeyVaultUri = $"https://{KeyVaultName}.vault.azure.net/";

        [Function("GetOrders")]
        public async Task <IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            // Kolla om vi kör i testläge
            string test = req.Query["test"];
            string variant = req.Query["variant"];

            if (test == "true")
            {
                List<Order> mockOrders = variant switch
                {
                    "empty" => [],
                    "single" => [
                        new Order
                {
                    Id = "mock123",
                    OrderStatus = "Mottagen",
                    CustomerFirstName = "Testperson",
                    CustomerLastName = "Testsson",
                    OrderTime = "12:00",
                    Pizzas = new List<Pizza> {
                        new Pizza {
                            PizzaName = "Margarita",
                            Quantity="1",
                            Price="125"
                        }
                    }

                }
                    ],
                    _ => [] // default tom lista
                };
                return new OkObjectResult(mockOrders);
            }

                var client = new SecretClient(new Uri(KeyVaultUri), new DefaultAzureCredential());
            var secret = await client.GetSecretAsync("PizzaOrderCosmos");

            // _logger.LogInformation("Secret retrieved successfully: {SecretValue}", secret.Value.Value);

            string cosmosDbConnectionstring = (await client.GetSecretAsync("PizzaOrderCosmos")).Value.Value;

            using CosmosClient cosmosClient = new(cosmosDbConnectionstring);
            var database = cosmosClient.GetDatabase("Resturant");
            var container = database.GetContainer("Orders");
            var orders = new List<dynamic>();
            var query = "SELECT * FROM c";
            var iterator = container.GetItemQueryIterator<Order>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();

                //_logger.LogInformation("Raw Cosmos DB response: {json}", Newtonsoft.Json.JsonConvert.SerializeObject(response));

                orders.AddRange(response);
                foreach (var item in await iterator.ReadNextAsync())
                {
                    orders.Add(item);
                }
            }

            _logger.LogInformation("C# HTTP trigger function processed a request for GetOrders.");
            return new OkObjectResult(orders);
        }
    }
}
