using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PizzaFunction.Functions
{
    public class UpdateOrderStatus(ILogger<UpdateOrderStatus> logger)
    {
        private readonly ILogger<UpdateOrderStatus> _logger = logger;
        private static readonly string KeyVaultName = Environment.GetEnvironmentVariable("KEYVAULT_NAME");
        private static readonly string KeyVaultUri = $"https://{KeyVaultName}.vault.azure.net/";

        [Function("UpdateOrderStatus")]
        public async Task <IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            var client = new SecretClient(new Uri(KeyVaultUri), new DefaultAzureCredential());
            string cosmosDbConnectionString = (await client.GetSecretAsync("PizzaOrderCosmos")).Value.Value;

            try
            {
                using (CosmosClient cosmosClient = new CosmosClient(cosmosDbConnectionString))
                {
                    var database = cosmosClient.GetDatabase("Resturant");
                    var container = database.GetContainer("Orders");

                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic data = JsonConvert.DeserializeObject(requestBody);

                    if (data?.OrderId == null || data?.OrderStatus == null)
                    {
                        _logger.LogWarning("Missing orderId or status in request");
                        return new BadRequestObjectResult(new { message = "orderNo and status is required" });
                    };

                    string orderId = data.OrderId.ToString().Trim();
                    string newStatus = data.OrderStatus.ToString().Trim();

                    _logger.LogInformation($"{orderId} {newStatus}");

                    try
                    {
                        var response = await container.ReadItemAsync<dynamic>(orderId, new PartitionKey(orderId));
                        var order = response.Resource;

                        if (order == null)
                        {
                            _logger.LogWarning($"Order {orderId} not found");
                            return new BadRequestObjectResult(new { message = "Order not found" });
                        }

                        order.OrderStatus = newStatus;
                        order.LastUpdateTime = DateTime.Now;
                        await container.UpsertItemAsync(order, new PartitionKey(orderId));

                        return new OkObjectResult(new
                        {
                            message ="Order status updatetd succesfully",
                            orderId = orderId,
                            status = newStatus,
                        });


                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"{ex.Message}");
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError($"Error updating order: {ex.Message}");
                return new NotFoundObjectResult(new { message = "Order not found" });
            }
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
