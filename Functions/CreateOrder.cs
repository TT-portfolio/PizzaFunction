using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PizzaFunction.Models;

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

                using (CosmosClient cosmosClient = new CosmosClient(cosmosDbConnectionString))
                {
                    var database = cosmosClient.GetDatabase("Resturant");
                    var container = database.GetContainer("Orders");

                    var query = new QueryDefinition("SELECT TOP 1 c.OrderNo FROM c ORDER BY c.OrderNo DESC");
                    using FeedIterator<dynamic> resultSet = container.GetItemQueryIterator<dynamic>(query);

                    int latestOrderNo = 0;
                    if (resultSet.HasMoreResults)
                    {
                        var response = await resultSet.ReadNextAsync();
                        if (response.Count > 0)
                        {
                            latestOrderNo = response.First().OrderNo;
                        }
                    }
                    // Create new order
                    int orderNo = latestOrderNo + 1;                   
                    var orderId = Guid.NewGuid().ToString(); //orderId for Db

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
                        CustomerFirstName = data.customer.firstName.ToString(),
                        CustomerLastName = data.customer.lastName.ToString(),
                        CustomerPhoneNumber = data.customer.phoneNumber.ToString(),
                        CustomerEmail = data.customer.email.ToString(),
                        OrderStatus = "Mottagen",
                        OrderTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
                        LastUpdateTime = DateTime.Now,
                        Pizzas = pizzas
                    };

                    _logger.LogInformation($"Created order with ID: {orderId}");

                    // Save order to Cosmos DB
                    await container.CreateItemAsync(order, new PartitionKey(orderId));
                    _logger.LogInformation("Order saved to Cosmos DB successfully");

                    return new OkObjectResult(new
                    {
                        message = "Order created successfully",
                        orderId = orderId,
                        orderNo = orderNo
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}