using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MailTmBlazor.PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
public class HomePageTests : PageTest
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")
        ?? "https://mailtmblazor-abghg7bggth2fkbg.switzerlandnorth-01.azurewebsites.net";

    [Test]
    public async Task Home_page_loads()
    {
        await Page.GotoAsync(BaseUrl);
        await Expect(Page).ToHaveTitleAsync("MailTmBlazor");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }
}
