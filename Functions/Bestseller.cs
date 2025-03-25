using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PizzaFunction.Functions
{
    public class Bestseller(ILogger<Bestseller> logger)
    {
        private readonly ILogger<Bestseller> _logger = logger;

        private static readonly string KeyVaultName = Environment.GetEnvironmentVariable("KEYVAULT_NAME");
        private static readonly string KeyVaultUri = $"https://{KeyVaultName}.vault.azure.net/";

        [Function("Bestseller")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            var client = new SecretClient(new Uri(KeyVaultUri), new DefaultAzureCredential());
            //var secret = await client.GetSecretAsync("PizzaOrderCosmos");
            string cosmosDbConnection = (await client.GetSecretAsync("PizzaOrderCosmos")).Value.Value;

            using CosmosClient cosmosClient = new(cosmosDbConnection);
            var database = cosmosClient.GetDatabase("Resturant");
            var container = database.GetContainer("DailyCompletedOrders");
            var query = "SELECT * FROM c";
            var iterator = container.GetItemQueryIterator<dynamic>(query);

            var pizzaCounter = new Dictionary<string, int>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var order in response)
                {
                    var pizzas = order.Pizzas;
                    foreach (var pizza in pizzas)
                    {
                        string name = pizza.PizzaName;
                        int quantity = (int)pizza.Quantity;

                        if (pizzaCounter.ContainsKey(name))
                            pizzaCounter[name] += quantity;
                        else
                            pizzaCounter[name] = quantity;
                    }
                }
            }

            var topPizzas = pizzaCounter
                .OrderByDescending(p => p.Value)
                .Take(3)
                .Select(p => new
                {
                    PizzaName = p.Key,
                    TotalSold = p.Value
                })
                .ToList();

            if (topPizzas.Count == null)
            {
                return new NotFoundObjectResult("Inga pizzor fanns bland ordrana");
            }

            return new OkObjectResult(topPizzas);
        }
    }
}
