using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Utopia.HomeAutomation;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

HashSet<string> subscriptions = [];
var _minimumLoggingLevel = LoggingLevel.Debug;

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly)
    .WithTools<SmartLock>()
    .WithResourcesFromAssembly(typeof(Program).Assembly)
    .WithPromptsFromAssembly(typeof(Program).Assembly)
    //.WithSubscribeToResourcesHandler(async (ctx, ct) =>
    //{
    //     var uri = ctx.Params?.Uri;

    //     if (uri is not null)
    //     {
    //         subscriptions.Add(uri);

    //         await ctx.Server.RequestSamplingAsync([
    //             new ChatMessage(ChatRole.System, "You are a helpful test server"),
    //            new ChatMessage(ChatRole.User, $"Resource {uri}, context: A new subscription was started"),
    //        ],
    //         options: new ChatOptions
    //         {
    //             MaxOutputTokens = 100,
    //             Temperature = 0.7f,
    //         },
    //         cancellationToken: ct);
    //     }

    //     return new EmptyResult();
    //})
    //.WithUnsubscribeFromResourcesHandler(async (ctx, ct) =>
    //{
    //    var uri = ctx.Params?.Uri;
    //    if (uri is not null)
    //    {
    //        subscriptions.Remove(uri);
    //    }
    //    return new EmptyResult();
    //})
    ;


ResourceBuilder resource = ResourceBuilder.CreateDefault().AddService("utopia");
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("*").AddHttpClientInstrumentation().SetResourceBuilder(resource))
    .WithMetrics(b => b.AddMeter("*").AddHttpClientInstrumentation().SetResourceBuilder(resource))
    .WithLogging(b => b.SetResourceBuilder(resource))
    .UseOtlpExporter();

builder.Services.AddSingleton(subscriptions);
builder.Services.AddHostedService<SubscriptionMessageSender>();
builder.Services.AddHostedService<LoggingUpdateMessageSender>();

builder.Services.AddSingleton<Func<LoggingLevel>>(_ => () => _minimumLoggingLevel);

await builder.Build().RunAsync();

// Simulating IDisposable with the static tools.
try
{
    var toolTypes = typeof(Program).Assembly
        .GetTypes()
        .Where(t => t.IsClass && t.IsAbstract && t.IsSealed && t.GetCustomAttributes(typeof(McpServerToolTypeAttribute), inherit: false).Any());
    
    foreach (var type in toolTypes)
    {
        var disposeMethod = type.GetMethod("Dispose", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        disposeMethod?.Invoke(null, null);
    }
}
catch (Exception)
{
}