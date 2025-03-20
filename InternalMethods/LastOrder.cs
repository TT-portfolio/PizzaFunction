using Microsoft.Azure.Cosmos;

namespace PizzaFunction.InternalMethods
{
    public class LastOrder
    {

        public static async Task<int> GetLastOrderNo(Container container)
        {
            var query = new QueryDefinition("SELECT TOP 1 c.OrderNo FROM c ORDER BY c.OrderNo DESC");
            using FeedIterator<dynamic> resultSet = container.GetItemQueryIterator<dynamic>(query);
            if (resultSet.HasMoreResults)
            {
                var response = await resultSet.ReadNextAsync();
                if (response.Count > 0)
                {
                    return response.First().OrderNo;
                }
            }
            return 0;

        }
    }
}
