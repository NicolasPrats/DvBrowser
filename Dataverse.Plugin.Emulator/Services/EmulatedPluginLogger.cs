using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dataverse.Plugin.Emulator.ExecutionTree;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;

namespace Dataverse.Plugin.Emulator.Services
{
    internal class EmulatedPluginLogger
        : ILogger
    {

        private class DummyDisposable : IDisposable
        {
            public void Dispose()
            {

            }
            public static readonly DummyDisposable Instance = new DummyDisposable();
        }

        public EmulatedPluginLogger(ExecutionTreeNode currentExecutionTreeNode)
        {
            this.CurrentExecutionTreeNode = currentExecutionTreeNode;

        }

        public ExecutionTreeNode CurrentExecutionTreeNode { get; }

        public void AddCustomProperty(string propertyName, string propertyValue)
        {

        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return DummyDisposable.Instance;
        }

        public IDisposable BeginScope(string messageFormat, params object[] args)
        {
            return DummyDisposable.Instance;
        }

        public void Execute(string activityName, Action action, IEnumerable<KeyValuePair<string, string>> additionalCustomProperties = null)
        {

        }

        public Task ExecuteAsync(string activityName, Func<Task> action, IEnumerable<KeyValuePair<string, string>> additionalCustomProperties = null)
        {
            return Task.CompletedTask;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {

        }

        public void Log(LogLevel logLevel, EventId eventId, Exception exception, string message, params object[] args)
        {

        }

        public void Log(LogLevel logLevel, EventId eventId, string message, params object[] args)
        {

        }

        public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
        {

        }

        public void Log(LogLevel logLevel, string message, params object[] args)
        {

        }

        public void LogCritical(EventId eventId, Exception exception, string message, params object[] args)
        {

        }

        public void LogCritical(EventId eventId, string message, params object[] args)
        {

        }

        public void LogCritical(Exception exception, string message, params object[] args)
        {

        }

        public void LogCritical(string message, params object[] args)
        {

        }

        public void LogDebug(EventId eventId, Exception exception, string message, params object[] args)
        {

        }

        public void LogDebug(EventId eventId, string message, params object[] args)
        {

        }

        public void LogDebug(Exception exception, string message, params object[] args)
        {

        }

        public void LogDebug(string message, params object[] args)
        {

        }

        public void LogError(EventId eventId, Exception exception, string message, params object[] args)
        {

        }

        public void LogError(EventId eventId, string message, params object[] args)
        {

        }

        public void LogError(Exception exception, string message, params object[] args)
        {

        }

        public void LogError(string message, params object[] args)
        {

        }

        public void LogInformation(EventId eventId, Exception exception, string message, params object[] args)
        {

        }

        public void LogInformation(EventId eventId, string message, params object[] args)
        {

        }

        public void LogInformation(Exception exception, string message, params object[] args)
        {

        }

        public void LogInformation(string message, params object[] args)
        {

        }

        public void LogMetric(string metricName, long value)
        {

        }

        public void LogMetric(string metricName, IDictionary<string, string> metricDimensions, long value)
        {

        }

        public void LogTrace(EventId eventId, Exception exception, string message, params object[] args)
        {

        }

        public void LogTrace(EventId eventId, string message, params object[] args)
        {

        }

        public void LogTrace(Exception exception, string message, params object[] args)
        {

        }

        public void LogTrace(string message, params object[] args)
        {

        }

        public void LogWarning(EventId eventId, Exception exception, string message, params object[] args)
        {

        }

        public void LogWarning(EventId eventId, string message, params object[] args)
        {

        }

        public void LogWarning(Exception exception, string message, params object[] args)
        {

        }

        public void LogWarning(string message, params object[] args)
        {

        }
    }
}