using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MailTmBlazor;
using MailTmBlazor.Application.Abstractions;
using MailTmBlazor.Infrastructure.Auth;
using MailTmBlazor.Infrastructure.MailTm;
using MailTmBlazor.Infrastructure.Realtime;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

using var configHttpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

await using (var stream = await configHttpClient.GetStreamAsync("appsettings.json"))
{
    builder.Configuration.AddJsonStream(stream);
}

var envConfigPath = $"appsettings.{builder.HostEnvironment.Environment}.json";
try
{
    await using var envStream = await configHttpClient.GetStreamAsync(envConfigPath);
    builder.Configuration.AddJsonStream(envStream);
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    // ignore missing environment-specific config
}

var mailTmSection = builder.Configuration.GetSection("MailTm");
var apiBase = mailTmSection["ApiBaseUrl"] ?? throw new InvalidOperationException("MailTm:ApiBaseUrl not configured");
var proxyBase = mailTmSection["ProxyBaseUrl"] ?? throw new InvalidOperationException("MailTm:ProxyBaseUrl not configured");
builder.Services.AddSingleton(new MailTmEndpoints(apiBase, proxyBase));

// HttpClient for mail.tm
builder.Services.AddHttpClient();
builder.Services.AddHttpClient(nameof(InboxRealtimeClient));
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBase) });

builder.Services.AddScoped<IAccountSession, LocalStorageAccountSession>();
builder.Services.AddScoped<InboxRealtimeClient>();
builder.Services.AddScoped<IAuthService, MailTmHttpClient>();
builder.Services.AddScoped<IMailboxService, MailTmHttpClient>();

await builder.Build().RunAsync();
