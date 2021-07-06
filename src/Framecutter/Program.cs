namespace Framecutter
{
    using System;
    using System.Threading.Tasks;

    using Framecutter.Services;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Serilog;

    internal class Program
    {
        /// <summary>
        /// Whitewashing bot
        /// </summary>
        private static async Task Main() => await
            Host
                .CreateDefaultBuilder()
                .ConfigureHostConfiguration(configHost => configHost.AddEnvironmentVariables())
                .ConfigureAppConfiguration((ctx, configuration) =>
                    configuration
                        .SetBasePath(Environment.CurrentDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                //#if DEBUG
                //        .AddUserSecrets<Program>()
                //#endif
                )
                .ConfigureServices((ctx, services) =>
                {
                    var keySection = ctx.Configuration.GetSection("Framecutter");
                    services
                        .AddOptions()
                            .Configure<Framecutter.Configuration.Configuration>(keySection);

                    services
                        .AddLogging(builder =>
                        {
                            Log.Logger = new LoggerConfiguration()
                                .ReadFrom.Configuration(ctx.Configuration)
                                .CreateLogger();
                            builder
                                .ClearProviders()
                                .AddSerilog(dispose: true);
                        })
                        .AddHostedService<BotService>()
                        .AddHttpClient();
                })
                .RunConsoleAsync()
                .ConfigureAwait(false);
    }
}