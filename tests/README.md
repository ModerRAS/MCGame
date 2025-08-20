# MCGame ECS 测试套件

这个测试套件为MCGame项目的ECS（Entity Component System）实现提供了全面的测试覆盖。

## 测试结构

```
tests/
├── Unit/                           # 单元测试
│   ├── TestBase.cs                 # 测试基类
│   ├── Components/                 # 组件测试
│   │   └── ComponentTests.cs
│   │   └── EntityComponentTests.cs
│   ├── Systems/                    # 系统测试
│   │   └── SystemTests.cs
│   ├── Queries/                    # 查询测试
│   │   └── QueryTests.cs
│   └── Managers/                   # 管理器测试
│       └── ManagerTests.cs
├── Integration/                    # 集成测试
│   ├── ECSWorld/                   # ECS世界测试
│   │   └── ECSWorldTests.cs
│   ├── Performance/                # 性能测试
│   │   └── PerformanceTests.cs
│   └── ErrorHandling/             # 错误处理测试
│       └── ErrorHandlingTests.cs
├── Documentation/                  # 文档验证测试
│   └── Examples/
│       └── DocumentationExamplesTest.cs
├── Benchmark/                      # 基准测试
│   ├── Performance/
│   │   └── ECSBenchmarks.cs
│   └── BenchmarkRunner.cs
├── TestRunner.cs                   # 测试运行器
├── MCGame.Tests.csproj             # 测试项目配置
└── README.md                       # 本文件
```

## 测试类型

### 1. 单元测试 (Unit Tests)
- **组件测试**: 验证所有ECS组件的基本功能
- **系统测试**: 测试ECS系统的正确执行
- **查询测试**: 验证Friflo ECS查询功能
- **管理器测试**: 测试ECSBlockManager和ECSChunkManager

### 2. 集成测试 (Integration Tests)
- **ECS世界测试**: 验证ECSWorld的整体功能集成
- **性能测试**: 测试批量操作和内存管理性能
- **错误处理测试**: 验证异常处理和错误恢复机制

### 3. 文档验证测试 (Documentation Tests)
- 验证文档中的所有代码示例都能正确运行
- 确保API文档的准确性

### 4. 基准测试 (Benchmark Tests)
- 使用BenchmarkDotNet进行性能基准测试
- 提供详细的性能指标和内存使用分析

## 运行测试

### 使用测试运行器

```bash
# 运行所有测试
dotnet run --project tests/MCGame.Tests.csproj

# 运行特定类型的测试
dotnet run --project tests/MCGame.Tests.csproj -- unit
dotnet run --project tests/MCGame.Tests.csproj -- integration
dotnet run --project tests/MCGame.Tests.csproj -- performance
dotnet run --project tests/MCGame.Tests.csproj -- error
dotnet run --project tests/MCGame.Tests.csproj -- docs
dotnet run --project tests/MCGame.Tests.csproj -- benchmark
dotnet run --project tests/MCGame.Tests.csproj -- quick
dotnet run --project tests/MCGame.Tests.csproj -- all
```

### 使用dotnet test

```bash
# 运行所有测试
dotnet test tests/MCGame.Tests.csproj

# 运行特定测试
dotnet test tests/MCGame.Tests.csproj --filter "TestClassName"
dotnet test tests/MCGame.Tests.csproj --filter "TestCategory=Unit"
dotnet test tests/MCGame.Tests.csproj --filter "TestCategory=Integration"

# 生成代码覆盖率报告
dotnet test tests/MCGame.Tests.csproj --collect:"XPlat Code Coverage"
```

### 运行基准测试

```bash
# 运行快速基准测试
dotnet run --project tests/MCGame.Tests.csproj -- benchmark

# 运行完整基准测试
dotnet run --project tests/MCGame.Tests.csproj --configuration Release
```

## 测试覆盖范围

### 核心功能测试
- ✅ EntityStore和实体管理
- ✅ 组件系统（Position, Velocity, Block, Chunk, Player等）
- ✅ 系统管理（PlayerInputSystem, PhysicsSystem, CameraSystem等）
- ✅ 查询系统（ArchetypeQuery, 过滤条件等）
- ✅ 管理器系统（ECSBlockManager, ECSChunkManager）

### 性能优化测试
- ✅ 批量操作性能
- ✅ 内存管理和GC优化
- ✅ 查询性能
- ✅ 系统更新性能

### 错误处理测试
- ✅ 组件访问错误处理
- ✅ 系统异常处理
- ✅ 资源清理和内存泄漏检测
- ✅ 并发访问安全性

### 文档验证
- ✅ 所有API文档示例验证
- ✅ 代码片段正确性验证
- ✅ 用户指南示例验证

## 测试要求

### 必需的NuGet包
- `xunit` - 单元测试框架
- `xunit.runner.visualstudio` - Visual Studio测试运行器
- `Moq` - 模拟框架
- `FluentAssertions` - 断言库
- `BenchmarkDotNet` - 性能基准测试
- `coverlet.collector` - 代码覆盖率

### .NET版本要求
- .NET 9.0 或更高版本

## 测试结果

测试运行后会生成以下报告：

1. **控制台输出**: 实时测试结果和性能指标
2. **代码覆盖率报告**: `coverage.xml` 文件
3. **基准测试报告**: Markdown和CSV格式
4. **测试日志**: 详细的测试执行日志

## 性能基准

测试套件包含以下性能基准：

- **实体创建**: > 10,000 entities/second
- **查询性能**: > 100 queries/second
- **系统更新**: > 60 FPS
- **内存使用**: < 500 bytes/entity (after GC)
- **批量操作**: > 50,000 blocks/second

## 故障排除

### 常见问题

1. **测试失败**: 检查ECS API兼容性
2. **性能问题**: 确保在Release模式下运行基准测试
3. **内存问题**: 检查是否有内存泄漏
4. **并发问题**: 验证线程安全性

### 调试技巧

1. 使用 `--filter` 参数运行特定测试
2. 在Debug模式下运行以获取详细信息
3. 检查测试输出日志
4. 使用代码覆盖率报告识别未测试的代码

## 贡献指南

### 添加新测试
1. 在适当的目录中创建测试文件
2. 继承自 `TestBase` 基类
3. 遵循命名约定：`[ClassName]Tests.cs`
4. 添加适当的测试用例和断言

### 测试标准
- 所有公共API必须有对应的测试
- 测试覆盖率要求 > 80%
- 包含边界条件和错误情况测试
- 性能测试必须包含基准指标

## 许可证

本测试套件遵循与主项目相同的许可证。

## 支持

如果遇到问题，请：
1. 检查文档和故障排除部分
2. 查看现有的测试用例作为参考
3. 提交Issue报告问题
4. 提交Pull Request贡献改进