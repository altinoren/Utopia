using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utopia.Shopping;
using Xunit;

namespace UtopiaTest.Shopping
{
    public class ACMEGroceryTests
    {
        [Fact]
        public async Task PlaceOrder_AddsOrderAndReturnsConfirmation()
        {
            var items = new List<ACMEGrocery.GroceryOrderItem>
            {
                new("test item", 2),
                new("milk", "1L")
            };
            var card = new ACMEGrocery.CreditCardInfo("1234567890123456", "12/30", "123");
            var address = "123 Test St, Testville";
            var resultJson = await ACMEGrocery.PlaceOrder(items, card, address);
            Assert.Contains("Order placed successfully", resultJson);
            Assert.Contains("deliveryTime", resultJson);
            // Should be present in past orders
            var ordersJson = await ACMEGrocery.GetPastOrders();
            Assert.Contains("test item", ordersJson);
            Assert.Contains("milk", ordersJson);
        }

        [Fact]
        public async Task PlaceOrder_EmptyItems_ReturnsError()
        {
            var card = new ACMEGrocery.CreditCardInfo("1234567890123456", "12/30", "123");
            var address = "123 Test St, Testville";
            var result = await ACMEGrocery.PlaceOrder(new List<ACMEGrocery.GroceryOrderItem>(), card, address);
            Assert.Equal("No items provided for the order.", result);
        }

        [Fact]
        public async Task PlaceOrder_MissingAddress_ReturnsError()
        {
            var items = new List<ACMEGrocery.GroceryOrderItem>
            {
                new("test item", 1)
            };
            var card = new ACMEGrocery.CreditCardInfo("1234567890123456", "12/30", "123");
            var result = await ACMEGrocery.PlaceOrder(items, card, "");
            Assert.Equal("Delivery address is required.", result);
        }

        [Fact]
        public async Task GetPastOrders_ReturnsJson()
        {
            var json = await ACMEGrocery.GetPastOrders();
            Assert.Contains("order_id", json);
            Assert.Contains("items", json);
        }
    }
}
