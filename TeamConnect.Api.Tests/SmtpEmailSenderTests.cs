using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TeamConnect.Api.Shared.Services;
using Xunit;

namespace TeamConnect.Api.Tests;

public class SmtpEmailSenderTests
{
    [Fact]
    public async Task SendAsync_WhenDisabled_ReturnsFalseWithoutThrowing()
    {
        var sender = new SmtpEmailSender(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Email:Enabled"] = "false"
        }), NullLogger<SmtpEmailSender>.Instance);

        var result = await sender.SendAsync("user@example.com", "Subject", "<p>Hello</p>", "Hello");

        Assert.False(result);
    }

    [Fact]
    public async Task SendAsync_WhenHostIsMissing_ThrowsInvalidOperationException()
    {
        var sender = new SmtpEmailSender(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Email:Enabled"] = "true",
            ["Email:FromAddress"] = "no-reply@example.com"
        }), NullLogger<SmtpEmailSender>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync("user@example.com", "Subject", "<p>Hello</p>", "Hello"));

        Assert.Contains("SMTP host", exception.Message);
    }

    [Fact]
    public async Task SendAsync_WhenFromAddressIsMissing_ThrowsInvalidOperationException()
    {
        var sender = new SmtpEmailSender(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Email:Enabled"] = "true",
            ["Email:Smtp:Host"] = "smtp.example.com"
        }), NullLogger<SmtpEmailSender>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync("user@example.com", "Subject", "<p>Hello</p>", "Hello"));

        Assert.Contains("from address", exception.Message);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}