using Utopia.Finance;
using Xunit;

namespace Utopia.Finance.Tests;

public class ACMEBankTests
{
    public ACMEBankTests()
    {
        // Reset balances
        foreach (var acc in ACMEBank.Accounts.Values)
            acc.Balance = 1000;
        ACMEBank.CreditCard.Balance = 0;
    }

    [Fact]
    public async Task GetAccountInfo_ValidAccount_ReturnsInfo()
    {
        var info = await ACMEBank.GetAccountInfo("Main_GBP");
        Assert.Contains("Account Main_GBP (GBP)", info);
        Assert.Contains("1000", info);
    }

    [Fact]
    public async Task GetAccountInfo_InvalidAccount_ReturnsNotFound()
    {
        var info = await ACMEBank.GetAccountInfo("Nonexistent");
        Assert.Equal("Account not found", info);
    }

    [Fact]
    public async Task GetCreditCardInfo_ReturnsInfo()
    {
        var info = await ACMEBank.GetCreditCardInfo();
        Assert.Contains("Credit Card", info);
        Assert.Contains("1234-5678-9012-3456", info);
        Assert.Contains("Limit = 2000", info);
    }

    [Fact]
    public async Task SendMoney_BetweenOwnAccounts_Succeeds()
    {
        var result = await ACMEBank.SendMoney("Main_GBP", "Main_GBP", 100);
        Assert.Contains("Transferred 100.00 GBP", result);
        Assert.Equal(1000, ACMEBank.Accounts["Main_GBP"].Balance);
    }

    [Fact]
    public async Task SendMoney_BetweenDifferentCurrency_Fails()
    {
        var result = await ACMEBank.SendMoney("Main_GBP", "Main_USD", 100);
        Assert.Equal("Currency mismatch", result);
    }

    [Fact]
    public async Task SendMoney_InsufficientFunds_Fails()
    {
        var result = await ACMEBank.SendMoney("Main_GBP", "Main_GBP", 2000);
        Assert.Equal("Insufficient funds", result);
    }

    [Fact]
    public async Task SendMoney_NegativeAmount_Fails()
    {
        var result = await ACMEBank.SendMoney("Main_GBP", "Main_GBP", -10);
        Assert.Equal("Amount must be positive", result);
    }

    [Fact]
    public async Task SendMoney_InvalidAccount_Fails()
    {
        var result = await ACMEBank.SendMoney("Main_GBP", "Nonexistent", 10);
        Assert.Equal("Destination account not found", result);
    }
}
