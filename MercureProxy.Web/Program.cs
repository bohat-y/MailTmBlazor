using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using MercureProxy.Web.Realtime;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "HH:mm:ss ";
    options.SingleLine = true;
});

var spaOrigins = builder.Configuration.GetSection("Spa:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Spa", policy =>
        policy.WithOrigins(spaOrigins)
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

var mailTmSection = builder.Configuration.GetSection("MailTm");
var mercureBase = mailTmSection["MercureBaseUrl"] ?? throw new InvalidOperationException("MailTm:MercureBaseUrl not configured");
var mailApiBase = mailTmSection["ApiBaseUrl"] ?? throw new InvalidOperationException("MailTm:ApiBaseUrl not configured");

builder.Services.AddHttpClient("mercure", client =>
{
    client.BaseAddress = new Uri(mercureBase);
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddHttpClient("mailtm-api", client =>
{
    client.BaseAddress = new Uri(mailApiBase);
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddSingleton<MercureSubscriptionManager>();

var app = builder.Build();

app.UseCors("Spa");

app.MapHub<MailHub>("/hubs/mail");

app.MapMethods("/api/negotiate", new[] { "OPTIONS" }, () => Results.NoContent());

app.MapPost("/api/negotiate", async (
    NegotiateRequest request,
    MercureSubscriptionManager subscriptions,
    HubService hubService,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.AccountId) || string.IsNullOrWhiteSpace(request.Token))
    {
        logger.LogWarning("Negotiate request missing accountId or token");
        return Results.BadRequest("accountId and token are required");
    }

    try
    {
        // Validate the token against mail.tm so bogus requests fail fast.
        var apiClient = httpClientFactory.CreateClient("mailtm-api");
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.Token);
        var validationResponse = await apiClient.GetAsync("me");
        if (!validationResponse.IsSuccessStatusCode)
        {
            var body = await validationResponse.Content.ReadAsStringAsync();
            logger.LogWarning(
                "Token validation failed for {AccountId} with {Status}: {Body}",
                request.AccountId,
                (int)validationResponse.StatusCode,
                body);
            return Results.StatusCode((int)validationResponse.StatusCode);
        }

        subscriptions.SetToken(request.AccountId, request.Token);

        var negotiate = await hubService.NegotiateAsync(new NegotiationOptions
        {
            Claims = new List<Claim> { new("accountId", request.AccountId) }
        });

        return Results.Ok(new NegotiateResponse(negotiate.Url ?? string.Empty, negotiate.AccessToken ?? string.Empty));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to negotiate SignalR connection for {AccountId}", request.AccountId);
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

await app.RunAsync();
