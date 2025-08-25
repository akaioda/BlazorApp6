using BlazorApp6.Components;
using BlazorApp6.Hubs;
using BlazorApp6.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Serilog;
using Serilog.Events;

namespace BlazorApp6
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "logs");
            Directory.CreateDirectory(logDirectory);

            // Configuration de Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Debug)
                .MinimumLevel.Override("Microsoft.AspNetCore.Http.Connections", LogEventLevel.Debug)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(logDirectory, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    shared: true)
                .CreateLogger();

            builder.Host.UseSerilog();

            builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();

            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = options.DefaultPolicy;
            });

           
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<ChatService>();

           
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

           
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapHub<ChatHub>("/chathub");

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
