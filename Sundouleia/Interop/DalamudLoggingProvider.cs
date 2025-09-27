using Serilog.Events;

namespace Sundouleia;

/// <summary>
///     A provider for Dalamud loggers, where we can construct our customized logger output message string
/// </summary>
[ProviderAlias("Dalamud")]
public sealed class DalamudLoggingProvider : ILoggerProvider
{
    // the concurrent dictionary of loggers that we have created
    private readonly ConcurrentDictionary<string, DalamudLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public DalamudLoggingProvider()
    {
        Svc.Logger.Information("DalamudLoggingProvider is initialized.");
        Svc.Logger.MinimumLogLevel = LogEventLevel.Verbose;
    }

    public ILogger CreateLogger(string categoryName)
    {
        // make the category name. Should be 15 characters or less long.
        // begin by splitting categoryName by periods (.), removes any empty entries,
        // then selects the last segment.
        // (This is a common pattern to extract the most specific part of a namespace
        // or class name, which often represents the actual class or component name.)
        var catName = categoryName.Split(".", StringSplitOptions.RemoveEmptyEntries).Last();
        // if the name is longer than 15 characters, take the first 6 characters, the last 6 characters, and add "..."
        if (catName.Length > 19)
        {
            catName = string.Join("", catName.Take(8)) + "..." + string.Join("", catName.TakeLast(8));
        }
        // otherwise replace any leftover empty space with spaces
        else
        {
            catName = string.Join("", Enumerable.Range(0, 19 - catName.Length).Select(_ => " ")) + catName;
        }
        // now that we have the name properly, get/add it to our logger for dalamud
        try
        {
            var newLogger = _loggers.GetOrAdd(catName, name => new DalamudLogger(name, Svc.Logger));
            //_pluginLog.Information($"Logger {catName} is created."); // <--- FOR DEBUGGING
            return newLogger;
        }
        catch (Bagagwa e)
        {
            Svc.Logger.Error($"Failed to create logger {catName}.");
            Svc.Logger.Error(e.ToString());
            throw;
        }
    }

    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
