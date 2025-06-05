using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Collections.Generic;

namespace Utopia.Finance
{
    public class Account
    {
        public string AccountId { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    public class CreditCard
    {
        public string CardNumber { get; set; } = string.Empty;
        public string ExpirationDate { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public string NameOnCard { get; set; } = string.Empty;
        public string LinkedAccountId { get; set; } = string.Empty;
        public decimal Limit { get; set; }
        public decimal Balance { get; set; }
    }

    [McpServerToolType]
    public static class ACMEBank
    {
        public static string BaseCurrency { get; set; } = "GBP";
        public static Dictionary<string, Account> Accounts { get; private set; }
        public static CreditCard CreditCard { get; private set; }
        private static readonly object bankLock = new();

        static ACMEBank()
        {
            Accounts = new Dictionary<string, Account>(StringComparer.OrdinalIgnoreCase)
            {
                { "Main_GBP", new Account { AccountId = "Main_GBP", Currency = "GBP", Balance = 1000 } },
                { "Main_USD", new Account { AccountId = "Main_USD", Currency = "USD", Balance = 1000 } }
            };
            CreditCard = new CreditCard
            {
                CardNumber = "1234-5678-9012-3456",
                ExpirationDate = $"{DateTime.Now.AddYears(2):MM/dd}",
                CVV = "123",
                NameOnCard = "John Doe",
                LinkedAccountId = "Main_GBP",
                Limit = 2000,
                Balance = 0
            };
        }

        [McpServerTool(Name = "bank_get_account_info", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets information about an account.")]
        public static Task<string> GetAccountInfo(string accountId)
        {
            if (!Accounts.ContainsKey(accountId))
                return Task.FromResult("Account not found");
            var acc = Accounts[accountId];
            return Task.FromResult($"Account {acc.AccountId} ({acc.Currency}): Balance = {acc.Balance:0.00}");
        }

        [McpServerTool(Name = "bank_get_credit_card_info", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets information about the credit card associated with the GBP account.")]
        public static Task<string> GetCreditCardInfo()
        {
            var card = CreditCard;
            return Task.FromResult($"Credit Card {card.CardNumber}: Linked to {card.LinkedAccountId}, Limit = {card.Limit:0.00}, Balance = {card.Balance:0.00}");
        }

        [McpServerTool(Name = "bank_get_credit_card_full_info", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets sensitive info about credit card including card number, owner name, expiry date and cvc.")]
        public static Task<string> GetCreditCardfullInfo()
        {
            var card = CreditCard;
            return Task.FromResult($"Credit Card {card.CardNumber}: Name on Card = {card.NameOnCard}, Expiry Date = {card.ExpirationDate}, CVV = {card.CVV}");
        }

        [McpServerTool(Name = "bank_send_money", Destructive = true, OpenWorld = false, ReadOnly = false, Idempotent = false),
            Description("Send money from one account to another (internal or external). Currency must match.")]
        public static Task<string> SendMoney(string fromAccountId, string toAccountId, decimal amount)
        {
            if (!Accounts.ContainsKey(fromAccountId))
                return Task.FromResult("Source account not found");
            if (!Accounts.ContainsKey(toAccountId))
                return Task.FromResult("Destination account not found");
            if (amount <= 0)
                return Task.FromResult("Amount must be positive");
            lock (bankLock)
            {
                var from = Accounts[fromAccountId];
                var to = Accounts[toAccountId];
                if (from.Currency != to.Currency)
                    return Task.FromResult("Currency mismatch");
                if (from.Balance < amount)
                    return Task.FromResult("Insufficient funds");
                from.Balance -= amount;
                to.Balance += amount;
                return Task.FromResult($"Transferred {amount:0.00} {from.Currency} from {fromAccountId} to {toAccountId}.");
            }
        }
    }
}
