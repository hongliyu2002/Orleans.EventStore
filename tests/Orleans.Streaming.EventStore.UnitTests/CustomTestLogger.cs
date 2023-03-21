using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;

public class TestOutputLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputLogger();
    }

    public void Dispose()
    {
    }

    private class TestOutputLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            TestContext.Progress.WriteLine($"[{logLevel}] {message}");
        }
    }
}
