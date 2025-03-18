using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PizzaFunction.Models
{
    public class SendModel
    {
        public string id { get; set; }
        public int OrderNo { get; set; }
        public string OrderStatus { get; set; } = null!;
        public DateTime LastUpdateTime { get; set; }
    }
}
