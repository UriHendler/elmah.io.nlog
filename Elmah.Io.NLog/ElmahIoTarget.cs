﻿using System;
using System.Collections.Generic;
using System.Linq;
using Elmah.Io.Client;
using NLog;
using NLog.Config;
using NLog.Targets;
using ILogger = Elmah.Io.Client.ILogger;
using Logger = Elmah.Io.Client.Logger;

namespace Elmah.Io.NLog
{
    [Target("elmah.io")]
    public class ElmahIoTarget : Target
    {
        private ILogger _logger;

        [RequiredParameter]
        public Guid LogId { get; set; }

        public ElmahIoTarget()
        {
        }

        public ElmahIoTarget(ILogger logger)
        {
            _logger = logger;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            if (_logger == null)
            {
                _logger = new Logger(LogId);
            }

            var message = new Message(logEvent.FormattedMessage)
            {
                Severity = LevelToSeverity(logEvent.Level),
                DateTime = logEvent.TimeStamp.ToUniversalTime(),
                Detail = logEvent.Exception != null ? logEvent.Exception.ToString() : null,
                Data = PropertiesToData(logEvent),
                Application = AppDomain.CurrentDomain.FriendlyName,
                Source = logEvent.LoggerName,
                Hostname = Environment.MachineName,
                Type = Type(logEvent)
            };

            _logger.Log(message);
        }

        private string Type(LogEventInfo logEvent)
        {
            if (logEvent.Exception == null) return null;
            return logEvent.Exception.GetType().FullName;
        }

        private List<Item> PropertiesToData(LogEventInfo logEvent)
        {
            var data = new List<Item>();
            if (logEvent.Exception != null)
            {
                data.AddRange(
                    logEvent.Exception.Data.Keys.Cast<object>()
                        .Select(key => new Item { Key = key.ToString(), Value = logEvent.Exception.Data[key].ToString() }));
            }

            data.AddRange(logEvent.Properties.Select(p => new Item { Key = p.Key.ToString(), Value = p.Value.ToString() }));
            return data;
        }

        private Severity? LevelToSeverity(LogLevel level)
        {
            if (level == LogLevel.Debug) return Severity.Debug;
            if (level == LogLevel.Error) return Severity.Error;
            if (level == LogLevel.Fatal) return Severity.Fatal;
            if (level == LogLevel.Trace) return Severity.Verbose;
            if (level == LogLevel.Warn) return Severity.Warning;
            return Severity.Information;
        }
    }
}
