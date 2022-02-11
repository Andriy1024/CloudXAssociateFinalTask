using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveOrderToCosmosDB
{
    public class Order
    {
        public int Id { get; set; }
        public string BuyerId { get; set; }
        public decimal Amount { get; set; }
        public List<Item> Items { get; set; }

        public class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(BuyerId)}: {BuyerId}, {nameof(Amount)}: {Amount}";
        }
    }
}
