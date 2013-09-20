using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace com.exapark.tools.cloud
{
    public static class Logger
    {
        /// <summary>
        /// Log message to event log
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="type">Entry type</param>
        public static void Log(string message, EventLogEntryType type)
        {
            try
            {
                EventLog log = new EventLog(LogName);
                log.Source = ServiceName;
                log.WriteEntry(message, type);
            }
            catch (Exception)
            {
                //обработка исключений журнализации не выполняется
            }
        }

        private const string LogName = "Application";
        private const string ServiceName = "Exapark Server Discoverer Service";
    }
}
