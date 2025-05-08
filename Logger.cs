using System;
using System.IO;
using System.Text;

namespace app_launcher
{
    /// <summary>
    /// Provides static methods for logging messages to a file and to the console with colored output.
    /// </summary>
    public static class FileLogger
    {
        private static string _logFilePath = string.Empty;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes the logger. Creates a log directory if it doesn't exist and sets up the log file path.
        /// The log file name is based on the current timestamp (yyMMdd_HHmmss).
        /// Logs initialization status to both console (with color) and file.
        /// </summary>
        /// <param name="logDirectory">The name of the subdirectory within the application's base directory where logs will be stored. Defaults to "logs".</param>
        public static void Initialize(string logDirectory = "logs")
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string logsSubDir = Path.Combine(baseDirectory, logDirectory);
                Directory.CreateDirectory(logsSubDir); // Ensure directory exists
                string timestamp = DateTime.Now.ToString("yyMMdd_HHmmss");
                _logFilePath = Path.Combine(logsSubDir, $"{timestamp}.log");

                LogInfo("Logger initialized.");
            }
            catch (Exception ex)
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INIT_FAIL] Failed to initialize logger: {ex.Message}");
                Console.ForegroundColor = originalColor;
                _logFilePath = string.Empty; // Ensure log path is empty so file logging is skipped
            }
        }

        /// <summary>
        /// Logs an informational message to console (Green) and to file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        /// <summary>
        /// Logs a warning message to console (Yellow) and to file.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public static void LogWarning(string message)
        {
            Log("WARN", message);
        }

        /// <summary>
        /// Logs an error message to console (Red) and to file, optionally including details from an exception.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="ex">The exception associated with the error, if any. Its type, message, and stack trace will be logged.</param>
        public static void LogError(string message, Exception? ex = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(message);
            if (ex != null)
            {
                sb.AppendLine($" Exception: {ex.GetType().Name} - {ex.Message}");
                sb.AppendLine($" StackTrace: {ex.StackTrace}");
            }
            Log("ERROR", sb.ToString().TrimEnd()); // TrimEnd to remove trailing newline if any from StringBuilder
        }

        /// <summary>
        /// Private helper method to write a log entry to the console (with color) and to the file.
        /// Prepends a timestamp and log level to the message.
        /// Console output is colored based on level. File output is plain text.
        /// This method is thread-safe for file writing.
        /// </summary>
        /// <param name="level">The log level (e.g., "INFO", "WARN", "ERROR").</param>
        /// <param name="message">The message content to log.</param>
        private static void Log(string level, string message)
        {
            string logEntryContent = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

            ConsoleColor originalConsoleColor = Console.ForegroundColor;
            switch (level.ToUpperInvariant())
            {
                case "INFO":
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case "WARN":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "ERROR":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Gray; 
                    break;
            }
            Console.WriteLine(logEntryContent);
            Console.ForegroundColor = originalConsoleColor;

            if (!string.IsNullOrEmpty(_logFilePath))
            {
                lock (_lock)
                {
                    try
                    {
                        File.AppendAllText(_logFilePath, logEntryContent + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        // Fallback error message to console if file write fails
                        ConsoleColor originalColorFallback = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.DarkMagenta; 
                        Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [LOG_WRITE_FAIL] Failed to write to log file '{_logFilePath}': {ex.Message}. Original log: [{level}] {message}");
                        Console.ForegroundColor = originalColorFallback;
                    }
                }
            }
            // If _logFilePath is empty (e.g., Initialize failed), messages will only go to the console.
        }
    }
}