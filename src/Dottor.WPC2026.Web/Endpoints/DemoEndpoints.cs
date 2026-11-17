using System.Net;
using Dottor.WPC2026.Web.Client.Models;
using Dottor.WPC2026.Web.Models;
using Dottor.WPC2026.Web.Services;

namespace Dottor.WPC2026.Web.Endpoints;

public static class DemoEndpoints
{
    public static void MapDemoEndpoints(this WebApplication app)
    {
        app.MapGet("/health", (DemoInstance instance) => Results.Ok(new
        {
            status = "Healthy", instance.Version, instance.ProcessId
        }));

        // Demo 1: usato da scripts/deploy-demo.ps1 per simulare il rilascio.
        if (app.Environment.IsDevelopment())
        {
            app.MapPost("/demo/shutdown", (HttpContext context, IHostApplicationLifetime lifetime) =>
            {
                var address = context.Connection.RemoteIpAddress;
                if (address is null || !IPAddress.IsLoopback(address) ||
                    context.Request.Headers["X-Demo-Deployment"] != "local")
                {
                    return Results.Problem("Shutdown consentito solo da loopback con header X-Demo-Deployment: local.", statusCode: 403);
                }

                context.Response.OnCompleted(() =>
                {
                    ThreadPool.QueueUserWorkItem(_ => lifetime.StopApplication());
                    return Task.CompletedTask;
                });
                return Results.Accepted(value: new { message = "Shutdown controllato richiesto" });
            });
        }

        // Demo 6: interrogato periodicamente dall'IHostedService che gira nel browser (WebAssembly).
        app.MapGet("/api/tickets/stats", (TicketService tickets, DemoInstance instance) =>
        {
            var all = tickets.GetAll().ToList();
            return new TicketStats(
                Total: all.Count,
                Open: all.Count(t => t.Status == TicketStatus.Open),
                Critical: all.Count(t => t.Priority == TicketPriority.Critical && t.Status != TicketStatus.Closed),
                LastTicketTitle: all.MaxBy(t => t.Id)?.Title,
                ServerVersion: instance.Version,
                GeneratedAt: DateTimeOffset.Now);
        });
    }
}
