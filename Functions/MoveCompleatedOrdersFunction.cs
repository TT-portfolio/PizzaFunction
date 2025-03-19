using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PizzaFunction.Functions;

public class MoveCompleatedOrdersFunction
{
    private readonly ILogger _logger;
    private static readonly string KeyVaultName = Environment.GetEnvironmentVariable("KEYVAULT_NAME");
    private static readonly string KeyVaultUri = $"https://{KeyVaultName}.vault.azure.net/";

    public MoveCompleatedOrdersFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MoveCompleatedOrdersFunction>();
    }

    [Function("MoveCompleatedOrdersFunction")]
    public async Task Run([TimerTrigger("0 0/5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        
        var client = new SecretClient(new Uri(KeyVaultUri), new DefaultAzureCredential());
        string cosmosDbConnectionString = (await client.GetSecretAsync("PizzaOrderCosmos")).Value.Value;

        try
        {
            using (CosmosClient cosmosClient = new CosmosClient(cosmosDbConnectionString))
            {
                var database = cosmosClient.GetDatabase("Resturant");
                var activeOrdersContainer = database.GetContainer("Orders");
                var dailyOrdersContainer = database.GetContainer("DailyCompletedOrders");
                var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30).ToString("o");

                var query = new QueryDefinition(
                    "SELECT * FROM c WHERE c.OrderStatus = 'Avslutad' AND c.LastUpdateTime < @time"
                    ).WithParameter("@time", thirtyMinutesAgo);

                var ordersToMove = new List<dynamic>();

                using (var feedIterator = activeOrdersContainer.GetItemQueryIterator<dynamic>(query))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        var response = await feedIterator.ReadNextAsync();
                        ordersToMove.AddRange(response);
                    }

                }

                foreach (var order in ordersToMove)
                {
                    // move order to daylyCompleted
                    await dailyOrdersContainer.CreateItemAsync(order, new PartitionKey(order["OrderId"].ToString()));
                    // Ta bort ordern från ActiveOrders
                    await activeOrdersContainer.DeleteItemAsync<dynamic>(order["id"].ToString(), new PartitionKey(order["OrderId"].ToString()));
                    _logger.LogInformation($"Order {order["OrderNo"]} flyttad till DailyCompletedOrders.");

                    
                }
            }
        }
        catch (Exception ex) 
        {
            Console.Error.WriteLine(ex);
        }
        
    }
}
