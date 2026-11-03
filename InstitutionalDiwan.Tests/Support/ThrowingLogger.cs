using Microsoft.Extensions.Logging;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>A logger that rethrows every logged exception, so a swallowed failure fails the test.</summary>
internal sealed class ThrowingLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (exception is not null)
            throw new InvalidOperationException("Logged exception: " + exception, exception);
    }
}
