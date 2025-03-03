using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PizzaFunction.Models
{
    public class Pizza
    {
        [JsonProperty(nameof(PizzaName))]
        public string PizzaName { get; set; } = null!;

        [JsonProperty(nameof(Quantity))]
        public string Quantity { get; set; } = null!;

        [JsonProperty(nameof(Price))]
        public string Price { get; set; } = null!;
    }
}
