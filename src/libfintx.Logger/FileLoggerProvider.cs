using System;
using System.IO;
using libfintx.Globals;
using Microsoft.Extensions.Logging;

namespace libfintx.Logger
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly StreamWriter _logFileWriter;

        public FileLoggerProvider(StreamWriter logFileWriter)
        {
            _logFileWriter = logFileWriter ?? throw new ArgumentNullException(nameof(logFileWriter));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _logFileWriter);
        }

        public void Dispose()
        {
            _logFileWriter.Dispose();
        }

        public static FileLoggerProvider CreateLibfintxLogger()
        {
            var dir = FinTsGlobals.ProgramBaseDir;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Logfile
            dir = Path.Combine(dir, "LOG");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string file = Path.Combine(dir, $"Log_{DateTime.Now.ToString("s").Replace(':','_')}.txt");

            return new FileLoggerProvider(new StreamWriter(file));
        }
    }
}
