using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PizzaFunction.Models
{
    public class Order
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty(nameof(OrderNo))]
        public int OrderNo { get; set; } 

        [JsonProperty(nameof(OrderId))]
        public string OrderId { get; set; } = null!;

        [JsonProperty(nameof(OrderStatus))]
        public string OrderStatus { get; set; } = null!;

        [JsonProperty(nameof(CustomerFirstName))]
        public string CustomerFirstName { get; set; } = null!;

        [JsonProperty(nameof(CustomerLastName))]
        public string CustomerLastName { get; set; } = null!;

        [JsonProperty(nameof(CustomerPhoneNumber))]
        public string CustomerPhoneNumber { get; set; } = null!;

        [JsonProperty(nameof(CustomerEmail))]
        public string CustomerEmail { get; set; } = null!;

        [JsonProperty(nameof(OrderTime))]
        public string OrderTime { get; set; } = null!;

        [JsonProperty(nameof(Pizzas))]
        public List<Pizza> Pizzas { get; set; } = null!;
    }
}
