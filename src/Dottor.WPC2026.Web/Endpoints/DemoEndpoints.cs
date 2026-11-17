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
        app.MapGet("/api/kitchen/stats", (OrderService orders, DemoInstance instance) =>
        {
            var all = orders.GetAll().ToList();
            var last = all.MaxBy(o => o.Id);
            return new KitchenStats(
                Total: all.Count,
                InOven: all.Count(o => o.Status == OrderStatus.InOven),
                Starving: all.Count(o => o.Hunger == HungerLevel.Starving && o.Status != OrderStatus.Delivered),
                LastOrder: last is null ? null : $"#{last.Id} · {last.Pizza}",
                ServerVersion: instance.Version,
                GeneratedAt: DateTimeOffset.Now);
        });
    }
}