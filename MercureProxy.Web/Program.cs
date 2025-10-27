using System.Collections.Generic;
using System.Threading;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using MercureProxy.Web.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Spa", policy =>
        policy.WithOrigins(
                "http://localhost:5004",
                "https://mailtmblazor-abghg7bggth2fkbg.switzerlandnorth-01.azurewebsites.net")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddSignalR().AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]);
builder.Services.AddSingleton<ServiceManager>(_ =>
{
    var connectionString = builder.Configuration["Azure:SignalR:ConnectionString"]
        ?? throw new InvalidOperationException("Azure:SignalR:ConnectionString is not configured.");
    return new ServiceManagerBuilder()
        .WithOptions(o => o.ConnectionString = connectionString)
        .BuildServiceManager();
});
builder.Services.AddSingleton<HubService>(sp =>
{
    var manager = sp.GetRequiredService<ServiceManager>();
    var context = manager.CreateHubContextAsync(nameof(MailHub), CancellationToken.None).GetAwaiter().GetResult();
    return new HubService(context);
});

builder.Services.AddHttpClient("mercure", client =>
{
    client.BaseAddress = new Uri("https://mercure.mail.tm/");
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddSingleton<MercureSubscriptionManager>();

var app = builder.Build();

app.UseCors("Spa");

app.MapHub<MailHub>("/hubs/mail");

app.MapMethods("/api/negotiate", new[] { "OPTIONS" }, () => Results.NoContent());

app.MapPost("/api/negotiate", async (
    NegotiateRequest request,
    MercureSubscriptionManager subscriptions,
    HubService hubService) =>
{
    if (string.IsNullOrWhiteSpace(request.AccountId) || string.IsNullOrWhiteSpace(request.Token))
    {
        return Results.BadRequest("accountId and token are required");
    }

    subscriptions.SetToken(request.AccountId, request.Token);

    var negotiate = await hubService.NegotiateAsync(new NegotiationOptions
    {
        Claims = new List<Claim> { new("accountId", request.AccountId) }
    });

    return Results.Ok(new NegotiateResponse(negotiate.Url, negotiate.AccessToken));
});

app.Run();
