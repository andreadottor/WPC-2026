using Dottor.WPC2026.Web.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Demo 6, novità .NET 11: IHostedService anche in Blazor WebAssembly.
// Il servizio parte con il runtime WebAssembly e vive finché la scheda resta aperta.
builder.Services.AddSingleton<TicketStatsMonitor>();
builder.Services.AddHostedService(services => services.GetRequiredService<TicketStatsMonitor>());

await builder.Build().RunAsync();