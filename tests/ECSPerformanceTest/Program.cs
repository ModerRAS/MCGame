using MCGame.ECS;

/// <summary>
/// ECS性能测试入口点
/// </summary>
public class Program
{
    public static void Main()
    {
        try
        {
            var test = new ECSPerformanceTest();
            test.RunPerformanceTest();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"性能测试失败: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
    }
}