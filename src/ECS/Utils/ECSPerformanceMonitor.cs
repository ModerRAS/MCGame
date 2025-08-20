using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MCGame.ECS.Utils
{
    /// <summary>
    /// ECS性能监控器
    /// 监控ECS系统的性能指标，提供实时统计和历史数据
    /// 简化实现：专注于关键性能指标的收集和分析
    /// </summary>
    public class ECSPerformanceMonitor
    {
        private readonly Dictionary<string, PerformanceCounter> _counters = new Dictionary<string, PerformanceCounter>();
        private readonly Dictionary<string, List<double>> _history = new Dictionary<string, List<double>>();
        private readonly int _maxHistorySize;
        private readonly Stopwatch _frameStopwatch;
        private readonly ECSObjectPool _objectPool;

        // 性能统计
        private long _totalFrames;
        private double _totalFrameTime;
        private double _minFrameTime = double.MaxValue;
        private double _maxFrameTime;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ECSPerformanceMonitor(ECSObjectPool objectPool, int maxHistorySize = 1000)
        {
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
            _maxHistorySize = maxHistorySize;
            _frameStopwatch = new Stopwatch();
            
            // 初始化默认计数器
            InitializeDefaultCounters();
        }

        /// <summary>
        /// 初始化默认计数器
        /// </summary>
        private void InitializeDefaultCounters()
        {
            // 系统性能计数器
            AddCounter("EntityStore.Count", "实体总数");
            AddCounter("EntityStore.Creations", "实体创建数量");
            AddCounter("EntityStore.Deletions", "实体删除数量");
            AddCounter("System.UpdateTime", "系统更新时间(ms)");
            AddCounter("System.QueryTime", "查询时间(ms)");
            AddCounter("System.RenderTime", "渲染时间(ms)");
            
            // 查询性能计数器
            AddCounter("Query.VisibleBlocks", "可见方块数量");
            AddCounter("Query.VisibleChunks", "可见区块数量");
            AddCounter("Query.ChunkOperations", "区块操作数量");
            AddCounter("Query.BlockOperations", "方块操作数量");
            
            // 内存性能计数器
            AddCounter("Memory.PoolUsage", "对象池使用率(%)");
            AddCounter("Memory.CacheHitRate", "缓存命中率(%)");
            AddCounter("Memory.GCCollections", "GC集合次数");
            
            // 渲染性能计数器
            AddCounter("Rendering.DrawCalls", "DrawCall数量");
            AddCounter("Rendering.Triangles", "三角形数量");
            AddCounter("Rendering.VisibleEntities", "可见实体数量");
        }

        /// <summary>
        /// 添加性能计数器
        /// </summary>
        public void AddCounter(string name, string description)
        {
            if (!_counters.ContainsKey(name))
            {
                _counters[name] = new PerformanceCounter(name, description);
                _history[name] = new List<double>();
            }
        }

        /// <summary>
        /// 开始帧计时
        /// </summary>
        public void BeginFrame()
        {
            _frameStopwatch.Restart();
        }

        /// <summary>
        /// 结束帧计时
        /// </summary>
        public void EndFrame()
        {
            _frameStopwatch.Stop();
            var frameTime = _frameStopwatch.Elapsed.TotalMilliseconds;
            
            // 更新帧统计
            _totalFrames++;
            _totalFrameTime += frameTime;
            _minFrameTime = Math.Min(_minFrameTime, frameTime);
            _maxFrameTime = Math.Max(_maxFrameTime, frameTime);
            
            // 记录帧时间
            RecordValue("System.FrameTime", frameTime);
        }

        /// <summary>
        /// 记录性能值
        /// </summary>
        public void RecordValue(string counterName, double value)
        {
            if (_counters.TryGetValue(counterName, out var counter))
            {
                counter.Record(value);
                
                // 添加到历史记录
                if (_history.TryGetValue(counterName, out var history))
                {
                    history.Add(value);
                    if (history.Count > _maxHistorySize)
                    {
                        history.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// 记录操作耗时
        /// </summary>
        public void RecordOperationTime(string operationName, Action operation)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
                RecordValue($"Operation.{operationName}.Time", stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public PerformanceStats GetStats()
        {
            return new PerformanceStats
            {
                TotalFrames = _totalFrames,
                AverageFrameTime = _totalFrames > 0 ? _totalFrameTime / _totalFrames : 0,
                MinFrameTime = _minFrameTime == double.MaxValue ? 0 : _minFrameTime,
                MaxFrameTime = _maxFrameTime,
                FPS = _totalFrames > 0 ? 1000.0 / (_totalFrameTime / _totalFrames) : 0,
                Counters = _counters.Values.ToDictionary(c => c.Name, c => c.GetStats()),
                History = _history.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray())
            };
        }

        /// <summary>
        /// 获取性能报告
        /// </summary>
        public string GetPerformanceReport()
        {
            var stats = GetStats();
            var report = new StringBuilder();
            
            report.AppendLine("=== ECS性能报告 ===");
            report.AppendLine($"总帧数: {stats.TotalFrames}");
            report.AppendLine($"平均帧时间: {stats.AverageFrameTime:F2}ms");
            report.AppendLine($"最小帧时间: {stats.MinFrameTime:F2}ms");
            report.AppendLine($"最大帧时间: {stats.MaxFrameTime:F2}ms");
            report.AppendLine($"FPS: {stats.FPS:F1}");
            report.AppendLine();
            
            report.AppendLine("=== 计数器统计 ===");
            foreach (var counter in stats.Counters.Values.OrderBy(c => c.Name))
            {
                report.AppendLine($"{counter.Name} ({counter.Description}):");
                report.AppendLine($"  当前值: {counter.CurrentValue:F2}");
                report.AppendLine($"  平均值: {counter.AverageValue:F2}");
                report.AppendLine($"  最小值: {counter.MinValue:F2}");
                report.AppendLine($"  最大值: {counter.MaxValue:F2}");
                report.AppendLine($"  总计: {counter.TotalValue:F2}");
                report.AppendLine();
            }
            
            return report.ToString();
        }

        /// <summary>
        /// 获取性能警告
        /// </summary>
        public List<string> GetPerformanceWarnings()
        {
            var warnings = ListPool<string>.Get();
            var stats = GetStats();
            
            // 帧时间警告
            if (stats.AverageFrameTime > 16.67) // 60 FPS
            {
                warnings.Add($"平均帧时间过高: {stats.AverageFrameTime:F2}ms (目标: <16.67ms)");
            }
            
            if (stats.MaxFrameTime > 33.33) // 30 FPS
            {
                warnings.Add($"最大帧时间过高: {stats.MaxFrameTime:F2}ms (目标: <33.33ms)");
            }
            
            // FPS警告
            if (stats.FPS < 60)
            {
                warnings.Add($"FPS过低: {stats.FPS:F1} (目标: >=60)");
            }
            
            // 实体数量警告
            if (stats.Counters.TryGetValue("EntityStore.Count", out var entityCounter))
            {
                if (entityCounter.CurrentValue > 10000)
                {
                    warnings.Add($"实体数量过多: {entityCounter.CurrentValue:F0} (建议: <10000)");
                }
            }
            
            // 查询性能警告
            if (stats.Counters.TryGetValue("System.QueryTime", out var queryCounter))
            {
                if (queryCounter.AverageValue > 2.0)
                {
                    warnings.Add($"查询时间过长: {queryCounter.AverageValue:F2}ms (建议: <2ms)");
                }
            }
            
            // 渲染性能警告
            if (stats.Counters.TryGetValue("Rendering.DrawCalls", out var drawCallCounter))
            {
                if (drawCallCounter.CurrentValue > 1000)
                {
                    warnings.Add($"DrawCall数量过多: {drawCallCounter.CurrentValue:F0} (建议: <1000)");
                }
            }
            
            return warnings;
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void Reset()
        {
            _totalFrames = 0;
            _totalFrameTime = 0;
            _minFrameTime = double.MaxValue;
            _maxFrameTime = 0;
            
            foreach (var counter in _counters.Values)
            {
                counter.Reset();
            }
            
            foreach (var history in _history.Values)
            {
                history.Clear();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _counters.Clear();
            _history.Clear();
            _frameStopwatch?.Stop();
        }
    }

    /// <summary>
    /// 性能计数器
    /// </summary>
    public class PerformanceCounter
    {
        private readonly string _name;
        private readonly string _description;
        private double _currentValue;
        private double _totalValue;
        private double _minValue = double.MaxValue;
        private double _maxValue;
        private int _count;

        /// <summary>
        /// 构造函数
        /// </summary>
        public PerformanceCounter(string name, string description)
        {
            _name = name;
            _description = description;
        }

        /// <summary>
        /// 计数器名称
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// 计数器描述
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// 当前值
        /// </summary>
        public double CurrentValue => _currentValue;

        /// <summary>
        /// 平均值
        /// </summary>
        public double AverageValue => _count > 0 ? _totalValue / _count : 0;

        /// <summary>
        /// 最小值
        /// </summary>
        public double MinValue => _minValue == double.MaxValue ? 0 : _minValue;

        /// <summary>
        /// 最大值
        /// </summary>
        public double MaxValue => _maxValue;

        /// <summary>
        /// 总计值
        /// </summary>
        public double TotalValue => _totalValue;

        /// <summary>
        /// 记录值
        /// </summary>
        public void Record(double value)
        {
            _currentValue = value;
            _totalValue += value;
            _minValue = Math.Min(_minValue, value);
            _maxValue = Math.Max(_maxValue, value);
            _count++;
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public CounterStats GetStats()
        {
            return new CounterStats
            {
                Name = _name,
                Description = _description,
                CurrentValue = _currentValue,
                AverageValue = AverageValue,
                MinValue = MinValue,
                MaxValue = MaxValue,
                TotalValue = _totalValue,
                Count = _count
            };
        }

        /// <summary>
        /// 重置计数器
        /// </summary>
        public void Reset()
        {
            _currentValue = 0;
            _totalValue = 0;
            _minValue = double.MaxValue;
            _maxValue = 0;
            _count = 0;
        }
    }

    /// <summary>
    /// 性能统计信息
    /// </summary>
    public class PerformanceStats
    {
        public long TotalFrames { get; set; }
        public double AverageFrameTime { get; set; }
        public double MinFrameTime { get; set; }
        public double MaxFrameTime { get; set; }
        public double FPS { get; set; }
        public Dictionary<string, CounterStats> Counters { get; set; }
        public Dictionary<string, double[]> History { get; set; }
    }

    /// <summary>
    /// 计数器统计信息
    /// </summary>
    public class CounterStats
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double CurrentValue { get; set; }
        public double AverageValue { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double TotalValue { get; set; }
        public int Count { get; set; }
    }
}