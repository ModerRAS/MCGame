using System;
using System.IO;
using System.Threading;

namespace MCGame.Utils
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }

    /// <summary>
    /// 日志系统接口
    /// </summary>
    public interface ILogger
    {
        void Log(LogLevel level, string message, Exception exception = null);
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception exception = null);
        void Fatal(string message, Exception exception = null);
    }

    /// <summary>
    /// 日志系统基类
    /// </summary>
    public abstract class BaseLogger : ILogger
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        public virtual void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level < MinLevel) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] [{level}] {message}";

            if (exception != null)
            {
                logMessage += $"\nException: {exception.GetType().Name}: {exception.Message}";
                logMessage += $"\nStack Trace: {exception.StackTrace}";
            }

            WriteLog(logMessage);
        }

        public virtual void Debug(string message) => Log(LogLevel.Debug, message);
        public virtual void Info(string message) => Log(LogLevel.Info, message);
        public virtual void Warning(string message) => Log(LogLevel.Warning, message);
        public virtual void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public virtual void Fatal(string message, Exception exception = null) => Log(LogLevel.Fatal, message, exception);

        protected abstract void WriteLog(string message);
    }

    /// <summary>
    /// 控制台日志记录器
    /// </summary>
    public class ConsoleLogger : BaseLogger
    {
        protected override void WriteLog(string message)
        {
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// 文件日志记录器
    /// </summary>
    public class FileLogger : BaseLogger
    {
        private readonly string _logFilePath;
        private readonly object _lock = new object();

        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
            EnsureLogDirectoryExists();
        }

        protected override void WriteLog(string message)
        {
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, message + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // 如果文件写入失败，回退到控制台输出
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                    Console.WriteLine($"Original message: {message}");
                }
            }
        }

        private void EnsureLogDirectoryExists()
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create log directory: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 组合日志记录器（可以同时输出到多个目标）
    /// </summary>
    public class CompositeLogger : ILogger
    {
        private readonly ILogger[] _loggers;

        public CompositeLogger(params ILogger[] loggers)
        {
            _loggers = loggers ?? Array.Empty<ILogger>();
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            foreach (var logger in _loggers)
            {
                try
                {
                    logger.Log(level, message, exception);
                }
                catch (Exception ex)
                {
                    // 避免日志记录器本身的异常影响程序运行
                    Console.WriteLine($"Logger failed: {ex.Message}");
                }
            }
        }

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public void Fatal(string message, Exception exception = null) => Log(LogLevel.Fatal, message, exception);
    }

    /// <summary>
    /// 静态日志管理器
    /// </summary>
    public static class Logger
    {
        private static ILogger _instance;
        private static readonly object _lock = new object();

        public static ILogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // 默认配置：控制台 + 文件日志
                            _instance = CreateDefaultLogger();
                        }
                    }
                }
                return _instance;
            }
            set
            {
                lock (_lock)
                {
                    _instance = value;
                }
            }
        }

        public static void Log(LogLevel level, string message, Exception exception = null)
        {
            Instance.Log(level, message, exception);
        }

        public static void Debug(string message) => Instance.Debug(message);
        public static void Info(string message) => Instance.Info(message);
        public static void Warning(string message) => Instance.Warning(message);
        public static void Error(string message, Exception exception = null) => Instance.Error(message, exception);
        public static void Fatal(string message, Exception exception = null) => Instance.Fatal(message, exception);

        private static ILogger CreateDefaultLogger()
        {
            try
            {
                // 加载配置
                var config = LogConfigManager.Config;
                
                var loggers = new List<ILogger>();
                
                // 控制台日志记录器
                if (config.Logging.Console.Enabled)
                {
                    var consoleLogger = new ConsoleLogger 
                    { 
                        MinLevel = LogConfigManager.ParseLogLevel(config.Logging.Console.LogLevel) 
                    };
                    loggers.Add(consoleLogger);
                }
                
                // 文件日志记录器
                if (config.Logging.File.Enabled)
                {
                    var logDir = Path.Combine(AppContext.BaseDirectory, config.Logging.File.Path);
                    var fileName = config.Logging.File.FileName
                        .Replace("{date}", DateTime.Now.ToString("yyyyMMdd"))
                        .Replace("{time}", DateTime.Now.ToString("HHmmss"));
                    var logFile = Path.Combine(logDir, fileName);
                    
                    var fileLogger = new FileLogger(logFile) 
                    { 
                        MinLevel = LogConfigManager.ParseLogLevel(config.Logging.File.LogLevel) 
                    };
                    loggers.Add(fileLogger);
                }
                
                if (loggers.Count == 0)
                {
                    // 如果没有启用任何日志记录器，至少使用控制台
                    loggers.Add(new ConsoleLogger { MinLevel = LogLevel.Info });
                }
                
                Console.WriteLine($"Logger initialized with {loggers.Count} targets");
                return new CompositeLogger(loggers.ToArray());
            }
            catch (Exception ex)
            {
                // 如果创建默认日志记录器失败，至少保证控制台日志可用
                Console.WriteLine($"Failed to create default logger: {ex.Message}");
                return new ConsoleLogger { MinLevel = LogLevel.Debug };
            }
        }

        public static void Initialize(string logFilePath = null, LogLevel minLevel = LogLevel.Debug)
        {
            try
            {
                if (string.IsNullOrEmpty(logFilePath))
                {
                    logFilePath = Path.Combine(AppContext.BaseDirectory, "logs", $"mcgame_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                }

                var consoleLogger = new ConsoleLogger { MinLevel = LogLevel.Info };
                var fileLogger = new FileLogger(logFilePath) { MinLevel = minLevel };

                Instance = new CompositeLogger(consoleLogger, fileLogger);
                
                Console.WriteLine("Logger initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize logger: {ex.Message}");
                Instance = new ConsoleLogger { MinLevel = LogLevel.Debug };
            }
        }
    }
}