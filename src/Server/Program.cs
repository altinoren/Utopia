using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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


builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly)
    .WithResourcesFromAssembly(typeof(Program).Assembly)
    .WithPromptsFromAssembly(typeof(Program).Assembly);


ResourceBuilder resource = ResourceBuilder.CreateDefault().AddService("utopia");
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("*").AddHttpClientInstrumentation().SetResourceBuilder(resource))
    .WithMetrics(b => b.AddMeter("*").AddHttpClientInstrumentation().SetResourceBuilder(resource))
    .WithLogging(b => b.SetResourceBuilder(resource))
    .UseOtlpExporter();


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