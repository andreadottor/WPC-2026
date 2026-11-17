using Dottor.WPC2026.Web.Components;
using Dottor.WPC2026.Web.Endpoints;
using Dottor.WPC2026.Web.Services;
using Dottor.WPC2026.Web.Services.Ai;
using Dottor.WPC2026.Web.Shutdown;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Demo 1: pausa dei circuiti allo shutdown (graceful shutdown) + info sull'istanza.
builder.AddCircuitShutdown();
builder.Services.AddSingleton<DemoInstance>();

// Demo 1: pausa automatica dei circuiti quando la scheda resta nascosta (pacchetto opzionale).
builder.Services.AddOptions<AutoPauseDemoOptions>()
                .Bind(builder.Configuration.GetSection("AutoPause"))
                .Validate(options => options.HiddenDelay > TimeSpan.Zero, "AutoPause:HiddenDelay deve essere maggiore di zero.")
                .ValidateOnStart();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    })
    .AddInteractiveWebAssemblyComponents();

// Demo 3: [SupplyParameterFromSession] richiede la sessione HTTP.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Servizi in memoria usati dalle demo.
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<TicketService>();
builder.Services.AddSingleton<CategoryReportService>();

// Demo 9 e 10: IChatClient (Microsoft.Extensions.AI) su Microsoft Foundry: sezione "AI" + chiave nei User Secrets.
builder.Services.AddFoundryChatClient(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    // In Development lo script di deploy usa l'URL http: niente redirect verso https.
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseCircuitShutdown();
app.UseSession();

app.MapDemoEndpoints();
app.MapStaticAssets();

var autoPause = app.Services.GetRequiredService<IOptions<AutoPauseDemoOptions>>().Value;
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(options =>
    {
        // Novità .NET 11: accesso diretto alle opzioni della connessione SignalR.
        options.ConfigureConnection = dispatcher =>
        {
            dispatcher.AllowStatefulReconnects = true;
        };
    })
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Dottor.WPC2026.Web.Client._Imports).Assembly)
    .WithBrowserOptions(options =>
    {
        // Novità .NET 11: configurazione di Blazor.start dal server, in C#.
        options.InteractiveServer.ReconnectionMaxRetries = 30;
        options.InteractiveServer.ReconnectionRetryInterval = TimeSpan.FromSeconds(2);

        // Pacchetto Microsoft.AspNetCore.Components.Server.AutoPause: vale per ogni pagina Interactive Server.
        options.AddAutoPause(pause =>
        {
            pause.Enabled = autoPause.Enabled;
            pause.HiddenDelay = autoPause.HiddenDelay;
        });
    });

app.Run();
