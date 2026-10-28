using Dottor.WPC2026.Web.Components;
using Dottor.WPC2026.Web.Services;
using Dottor.WPC2026.Web.Services.Ai;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

// Demo 1: lascia il tempo ai circuiti di essere messi in pausa e persistiti prima dello stop.
builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

// Demo 1: pausa dei circuiti allo shutdown (graceful shutdown) + info sull'istanza.
builder.Services.AddSingleton(AppInstanceInfo.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton<CircuitShutdownService>();
builder.Services.AddScoped<ShutdownCircuitHandler>();
builder.Services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<ShutdownCircuitHandler>());

// Servizi in memoria usati dalle demo.
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<TicketService>();

// Demo 6: IChatClient (simulato di default, reale se configurato nella sezione "AI").
builder.Services.AddDocumentAssistant(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.MapStaticAssets();

app.MapGet("/health", (AppInstanceInfo info, CircuitShutdownService circuits) => Results.Ok(new
{
    status = "ok",
    version = info.Version,
    processId = info.ProcessId,
    startedAt = info.StartedAt,
    connectedCircuits = circuits.ConnectedCircuits
}));

// Endpoint usato da scripts/deploy-demo.ps1 per simulare il rilascio: solo in Development e solo da localhost.
app.MapPost("/demo/shutdown", (HttpContext context, IHostApplicationLifetime lifetime, ILogger<Program> logger) =>
{
    var remote = context.Connection.RemoteIpAddress;
    if (!app.Environment.IsDevelopment() || remote is null || !System.Net.IPAddress.IsLoopback(remote))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    logger.LogWarning("Richiesto arresto dell'applicazione da {Remote} (simulazione deploy)", remote);
    // StopApplication esegue i callback ApplicationStopping in modo sincrono: lo lanciamo fuori dalla richiesta.
    _ = Task.Run(lifetime.StopApplication);
    return Results.Accepted();
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(options =>
    {
        // Novità .NET 11: accesso diretto alle opzioni della connessione SignalR.
        options.ConfigureConnection = dispatcher =>
        {
            dispatcher.AllowStatefulReconnects = true;
        };
    })
    .WithBrowserOptions(options =>
    {
        // Novità .NET 11: configurazione di Blazor.start dal server, in C#.
        options.InteractiveServer.ReconnectionMaxRetries = 30;
        options.InteractiveServer.ReconnectionRetryInterval = TimeSpan.FromSeconds(2);
    });

app.Run();
