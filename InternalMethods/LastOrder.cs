using Microsoft.Azure.Cosmos;

namespace PizzaFunction.InternalMethods
{
    public class LastOrder
    {

        public static async Task<int> GetLastOrderNo(Container container)
        {
            using FeedIterator<dynamic> resultSet = container.GetItemQueryIterator<dynamic>();
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
