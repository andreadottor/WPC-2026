using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Dottor.WPC2026.Web.Shutdown;

public static class CircuitShutdownExtensions
{
    public static WebApplicationBuilder AddCircuitShutdown(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<ShutdownCircuitOptions>()
                        .BindConfiguration("CircuitShutdown")
                        .Validate(options => options.ShutdownTimeout > TimeSpan.Zero && options.ShutdownTimeout < TimeSpan.FromSeconds(50),
                            "CircuitShutdown:ShutdownTimeout deve essere positivo e inferiore a 50 secondi.")
                        .ValidateOnStart();
        builder.Host.ConfigureHostOptions(options => options.ShutdownTimeout = TimeSpan.FromSeconds(60));

        builder.Services.AddSingleton<CircuitShutdownService>();
        builder.Services.AddScoped<ShutdownCircuitHandler>();
        builder.Services.AddScoped<CircuitHandler>(services => services.GetRequiredService<ShutdownCircuitHandler>());
        
        return builder;
    }

    public static WebApplication UseCircuitShutdown(this WebApplication app)
    {
        app.Lifetime.ApplicationStopped.Register(() => app.Logger.LogInformation("Shutdown host completato"));
        app.Use(async (context, next) =>
        {
            // Resolved on the first request, after the server starts: ApplicationStopping callbacks run in
            // reverse registration order, so the drain starts before Kestrel closes the circuit connections.
            var shutdown = context.RequestServices.GetRequiredService<CircuitShutdownService>();
            if (shutdown.IsStopping && (context.Request.Path == "/_blazor/negotiate" || context.Request.Path == "/health"))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Deployment in corso. Riprovare tra qualche secondo.");
                return;
            }
            await next(context);
        });
        return app;
    }
}
