using System.Text.RegularExpressions;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MailTmBlazor.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
public class HomePageTests : PageTest
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")
        ?? "http://localhost:5004";

    [Test]
    public async Task Home_page_loads()
    {
        await Page.GotoAsync($"{BaseUrl}/inbox");
        await Expect(Page).ToHaveURLAsync(new Regex("/inbox$", RegexOptions.IgnoreCase));
    }
}
