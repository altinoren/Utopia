using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Utopia.Shopping
{
    [McpServerToolType]
    public static class ACMEGrocery
    {
        private static string ExampleOrdersJson = @"
[
  {
    ""order_id"": 1,
    ""items"": [
      { ""item"": ""6 eggs"", ""quantity"": 1 },
      { ""item"": ""semi-skimmed milk"", ""quantity"": ""2 pints"" },
      { ""item"": ""Greek yogurt"", ""quantity"": ""500g"" }
    ]
  },
  {
    ""order_id"": 2,
    ""items"": [
      { ""item"": ""Branston Pickle"", ""quantity"": ""1 jar"" },
      { ""item"": ""cheddar cheese"", ""quantity"": ""250g"" },
      { ""item"": ""wholemeal bread loaf"", ""quantity"": 1 }
    ]
  },
  {
    ""order_id"": 3,
    ""items"": [
      { ""item"": ""orange juice"", ""quantity"": ""1L"" },
      { ""item"": ""green apples"", ""quantity"": ""1kg"" },
      { ""item"": ""carrots"", ""quantity"": ""500g"" }
    ]
  },
  {
    ""order_id"": 4,
    ""items"": [
      { ""item"": ""red bell peppers"", ""quantity"": 3 },
      { ""item"": ""mayonnaise"", ""quantity"": ""1 bottle"" },
      { ""item"": ""semi-skimmed milk"", ""quantity"": ""2 pints"" }
    ]
  },
  {
    ""order_id"": 5,
    ""items"": [
      { ""item"": ""broccoli"", ""quantity"": ""1 head"" },
      { ""item"": ""butter"", ""quantity"": ""250g"" },
      { ""item"": ""yogurt"", ""quantity"": ""500g"" }
    ]
  },
  {
    ""order_id"": 6,
    ""items"": [
      { ""item"": ""ketchup"", ""quantity"": ""1 bottle"" },
      { ""item"": ""apple juice"", ""quantity"": ""1L"" },
      { ""item"": ""cucumber"", ""quantity"": 2 }
    ]
  }
]";

        [McpServerTool(Name = "grocery_place_order", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = false),
            Description("Places a grocery order. Accepts a list of items to buy, credit card info and delivery address. Returns a confirmation and delivery time.")]
        public static Task<string> PlaceOrder(List<GroceryOrderItem> items, CreditCardInfo creditCard, string deliveryAddress)
        {
            if (items == null || items.Count == 0)
                return Task.FromResult("No items provided for the order.");
            if(string.IsNullOrWhiteSpace(deliveryAddress))
                return Task.FromResult("Delivery address is required.");

            // Parse the current orders from ExampleOrdersJson
            List<GroceryOrder> orders;
            try
            {
                orders = JsonSerializer.Deserialize<List<GroceryOrder>>(ExampleOrdersJson) ?? new List<GroceryOrder>();
            }
            catch
            {
                orders = new List<GroceryOrder>();
            }

            // Determine the next order_id
            int nextOrderId = orders.Count > 0 ? orders.Max(o => o.order_id) + 1 : 1;

            // Create the new order
            var newOrder = new GroceryOrder(nextOrderId, items);

            // Add the new order to the list
            orders.Add(newOrder);

            // Update ExampleOrdersJson
            ExampleOrdersJson = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });

            var confirmation = new
            {
                confirmation = "Order placed successfully!",
                deliveryTime = DateTime.Now.AddDays(1)
            };
            return Task.FromResult(JsonSerializer.Serialize(confirmation));
        }

        [McpServerTool(Name = "grocery_get_past_orders", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Returns past grocery order details as JSON.")]
        public static Task<string> GetPastOrders()
        {
            return Task.FromResult(ExampleOrdersJson);
        }

        public record GroceryOrderItem(string item, object quantity);
        public record CreditCardInfo(string number, string expiry, string cvv);
        public record GroceryOrder(int order_id, List<GroceryOrderItem> items);
    }
}
