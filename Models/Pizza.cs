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
        [JsonProperty("PizzaName")]
        public string PizzaName { get; set; }

        [JsonProperty("Quantity")]
        public int Quantity { get; set; }

        [JsonProperty("Price")]
        public decimal Price { get; set; }
    }
}
