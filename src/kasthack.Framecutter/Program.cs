namespace kasthack.Framecutter;

using System;
using System.Threading.Tasks;

using kasthack.Framecutter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

#pragma warning disable RCS1102
internal class Program
{
#pragma warning restore RCS1102

    /// <summary>
    /// Whitewashing bot.
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
#pragma warning disable SA1009, SA1111
#if DEBUG
                    .AddUserSecrets<Program>()
#endif
            )
#pragma warning restore SA1009, SA111
            .ConfigureServices((ctx, services) =>
            {
                var keySection = ctx.Configuration.GetSection("Framecutter");
                services
                    .AddOptions()
                        .Configure<Configuration.ConfigurationOptions>(keySection);

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