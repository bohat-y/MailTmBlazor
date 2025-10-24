using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MailTmBlazor;

using MailTmBlazor.Application.Abstractions;
using MailTmBlazor.Infrastructure.MailTm;

using MailTmBlazor.Application.Abstractions;
using MailTmBlazor.Infrastructure.Auth;
using MailTmBlazor.Infrastructure.MailTm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// HttpClient for mail.tm
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://api.mail.tm/") });

builder.Services.AddScoped<IAccountSession, LocalStorageAccountSession>();

builder.Services.AddScoped<IAuthService, MailTmHttpClient>();
builder.Services.AddScoped<IMailboxService, MailTmHttpClient>();

await builder.Build().RunAsync();