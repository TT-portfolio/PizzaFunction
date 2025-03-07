using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PizzaFunction.Models;
using Newtonsoft.Json;

namespace PizzaFunction.Functions
{
    public class CreateOrder
    {
        private readonly ILogger<CreateOrder> _logger;
        private static readonly string KeyVaultName = Environment.GetEnvironmentVariable("KEYVAULT_NAME");
        private static readonly string KeyVaultUri = $"https://{KeyVaultName}.vault.azure.net/";

        public CreateOrder(ILogger<CreateOrder> logger)
        {
            _logger = logger;
        }

        [Function("CreateOrder")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            _logger.LogInformation("Processing new order submission");

            try
            {                
                var client = new SecretClient(new Uri(KeyVaultUri), new DefaultAzureCredential());
                string cosmosDbConnectionString = (await client.GetSecretAsync("PizzaOrderCosmos")).Value.Value;

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"Received order data: {requestBody}");
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                //Validate incoming data
                if (data?.customer == null || data?.items == null)
                {
                    _logger.LogWarning("Invalid order data: Missing customer or items");
                    return new BadRequestObjectResult(new { message = "Customer and items data required" });
                }

                // Create new order
                var orderNo = $"ORDER-{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                var orderId = Guid.NewGuid().ToString();

                // Create PizzaList
                var pizzas = new List<Pizza>();

                // Get pizzas from dataitems
                foreach (var item in data.items)
                {
                    pizzas.Add(new Pizza
                    {
                        PizzaName = item.name.ToString(),
                        Quantity = item.quantity.ToString(),
                        Price = item.price.ToString()
                    });
                }

                var order = new Order
                {
                    Id = orderId,
                    OrderId = orderId,
                    OrderNo = orderNo,
                    CustomerName = data.customer.name.ToString(),
                    OrderStatus = "Pending",
                    OrderTime = DateTime.UtcNow.ToString("o"),
                    Pizzas = pizzas
                };

                _logger.LogInformation($"Created order with ID: {orderId}");

                // Save order to Cosmos DB
                using (CosmosClient cosmosClient = new CosmosClient(cosmosDbConnectionString))
                {
                    var database = cosmosClient.GetDatabase("Resturant");
                    var container = database.GetContainer("Orders");
                    await container.CreateItemAsync(order, new PartitionKey(orderId));
                    _logger.LogInformation("Order saved to Cosmos DB successfully");
                }

                return new OkObjectResult(new
                {
                    message = "Order created successfully",
                    orderId = orderId,
                    orderNo = orderNo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}