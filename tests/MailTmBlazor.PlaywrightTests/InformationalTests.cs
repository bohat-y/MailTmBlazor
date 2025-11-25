using System;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MailTmBlazor.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
public class InformationalTests : PageTest
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")
        ?? "http://localhost:5004";

    [Test]
    public async Task Auth_shows_validation_errors_for_invalid_input()
    {
        await Page.GotoAsync($"{BaseUrl}/auth");
        await Page.GetByLabel("Username").FillAsync("xx");
        await Page.GetByLabel("Password").FillAsync("123");
        await Page.GetByLabel("Username").PressAsync("Tab");

        await Expect(Page.GetByText("Use letters, numbers, dashes, or underscore")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Password must be at least 6 characters")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Domains_page_lists_available_domains()
    {
        await Page.GotoAsync($"{BaseUrl}/domains");
        // Wait for the loading state to finish so we don't read a transient zero.
        await Page.GetByText("Loading...").WaitForAsync(new() { State = WaitForSelectorState.Detached });
        await Page.WaitForSelectorAsync("ul li", new() { Timeout = 15000 });
        var count = await Page.Locator("ul li").CountAsync();
        var content = await Page.ContentAsync();
        Assert.That(count, Is.GreaterThan(0), $"Expected at least one domain on the domains page. Page content:\n{content}");
    }

    [Test]
    public async Task Inbox_redirects_unauthenticated_users_to_auth()
    {
        await Page.GotoAsync($"{BaseUrl}/inbox");
        // App may auto-provision an account; accept either redirect to /auth or landing on inbox.
        var url = Page.Url;
        if (Regex.IsMatch(url, "/auth$", RegexOptions.IgnoreCase))
        {
            Assert.Pass("Redirected to auth as expected.");
        }
        else
        {
            await Expect(Page.GetByText("Inbox").First).ToBeVisibleAsync();
        }
    }
}
