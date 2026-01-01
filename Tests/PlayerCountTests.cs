using System.Text.RegularExpressions;
using Pelican_Keeper.Models;
using Pelican_Keeper.Query;
using Pelican_Keeper.Utilities;

namespace Pelican_Keeper.Tests;

/// <summary>
/// Tests for player count response parsing and validation.
/// </summary>
[Category("Integration")]
public class PlayerCountResponseTesting
{
    [SetUp]
    public void Setup()
    {
        Logger.SuppressExitForTests = true;
    }

    /// <summary>
    /// Tests player count extraction from standard format responses.
    /// </summary>
    [Test]
    public void TestStandardPlayerCountFormat()
    {
        var count = PlayerCountHelper.ExtractPlayerCount("5/20");
        Assert.That(count, Is.EqualTo(5), "Should extract player count from standard format.");
    }

    /// <summary>
    /// Tests player count formatting.
    /// </summary>
    [Test]
    public void TestPlayerCountFormatting()
    {
        var formatted = PlayerCountHelper.FormatPlayerCount("5", 20);
        Assert.That(formatted, Is.EqualTo("5/20"), "Should format player count correctly.");
    }

    /// <summary>
    /// Tests player count extraction with null response.
    /// </summary>
    [Test]
    public void TestNullResponse()
    {
        var count = PlayerCountHelper.ExtractPlayerCount(null);
        Assert.That(count, Is.EqualTo(0), "Should return 0 for null response.");
    }

    /// <summary>
    /// Tests player count extraction with empty response.
    /// </summary>
    [Test]
    public void TestEmptyResponse()
    {
        var count = PlayerCountHelper.ExtractPlayerCount("");
        Assert.That(count, Is.EqualTo(0), "Should return 0 for empty response.");
    }
}
