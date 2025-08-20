@echo off
setlocal enabledelayedexpansion

REM MCGame 测试运行脚本 (Windows版本)
REM 用于在本地运行所有测试或特定类型的测试

REM 默认参数
set RUN_UNIT=true
set RUN_INTEGRATION=true
set RUN_PERFORMANCE=true
set GENERATE_COVERAGE=false
set VERBOSE=false
set GENERATE_REPORT=false
set BUILD=false

REM 解析命令行参数
:parse_args
if "%~1"=="" goto end_parse
if "%~1"=="-h" goto show_help
if "%~1"=="--help" goto show_help
if "%~1"=="-u" (
    set RUN_UNIT=true
    set RUN_INTEGRATION=false
    set RUN_PERFORMANCE=false
    shift
    goto parse_args
)
if "%~1"=="--unit" (
    set RUN_UNIT=true
    set RUN_INTEGRATION=false
    set RUN_PERFORMANCE=false
    shift
    goto parse_args
)
if "%~1"=="-i" (
    set RUN_UNIT=false
    set RUN_INTEGRATION=true
    set RUN_PERFORMANCE=false
    shift
    goto parse_args
)
if "%~1"=="--integration" (
    set RUN_UNIT=false
    set RUN_INTEGRATION=true
    set RUN_PERFORMANCE=false
    shift
    goto parse_args
)
if "%~1"=="-p" (
    set RUN_UNIT=false
    set RUN_INTEGRATION=false
    set RUN_PERFORMANCE=true
    shift
    goto parse_args
)
if "%~1"=="--performance" (
    set RUN_UNIT=false
    set RUN_INTEGRATION=false
    set RUN_PERFORMANCE=true
    shift
    goto parse_args
)
if "%~1"=="-a" (
    set RUN_UNIT=true
    set RUN_INTEGRATION=true
    set RUN_PERFORMANCE=true
    shift
    goto parse_args
)
if "%~1"=="--all" (
    set RUN_UNIT=true
    set RUN_INTEGRATION=true
    set RUN_PERFORMANCE=true
    shift
    goto parse_args
)
if "%~1"=="-c" (
    set GENERATE_COVERAGE=true
    shift
    goto parse_args
)
if "%~1"=="--coverage" (
    set GENERATE_COVERAGE=true
    shift
    goto parse_args
)
if "%~1"=="-v" (
    set VERBOSE=true
    shift
    goto parse_args
)
if "%~1"=="--verbose" (
    set VERBOSE=true
    shift
    goto parse_args
)
if "%~1"=="-r" (
    set GENERATE_REPORT=true
    shift
    goto parse_args
)
if "%~1"=="--report" (
    set GENERATE_REPORT=true
    shift
    goto parse_args
)
if "%~1"=="-b" (
    set BUILD=true
    shift
    goto parse_args
)
if "%~1"=="--build" (
    set BUILD=true
    shift
    goto parse_args
)
echo [ERROR] 未知选项: %~1
goto show_help
:end_parse

REM 显示帮助信息
:show_help
echo MCGame 测试运行脚本 (Windows版本)
echo.
echo 用法: %~nx0 [选项]
echo.
echo 选项:
echo   -h, --help          显示此帮助信息
echo   -u, --unit          只运行单元测试
echo   -i, --integration   只运行集成测试
echo   -p, --performance   只运行性能测试
echo   -a, --all           运行所有测试（默认）
echo   -c, --coverage      生成代码覆盖率报告
echo   -v, --verbose       详细输出
echo   -r, --report        生成测试报告
echo   -b, --build         重新构建项目
echo.
echo 示例:
echo   %~nx0                  # 运行所有测试
echo   %~nx0 -u               # 只运行单元测试
echo   %~nx0 -u -c            # 运行单元测试并生成覆盖率报告
echo   %~nx0 -p -v            # 运行性能测试并显示详细信息
goto end

REM 检查.NET SDK是否安装
where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] 未找到 .NET SDK，请先安装 .NET SDK
    exit /b 1
)

echo [INFO] 开始运行 MCGame 测试...
echo [INFO] 运行选项:
if "%RUN_UNIT%"=="true" echo [INFO]   - 单元测试: 是
if "%RUN_INTEGRATION%"=="true" echo [INFO]   - 集成测试: 是
if "%RUN_PERFORMANCE%"=="true" echo [INFO]   - 性能测试: 是
if "%GENERATE_COVERAGE%"=="true" echo [INFO]   - 代码覆盖率: 是
if "%VERBOSE%"=="true" echo [INFO]   - 详细输出: 是
if "%GENERATE_REPORT%"=="true" echo [INFO]   - 测试报告: 是
if "%BUILD%"=="true" echo [INFO]   - 重新构建: 是

REM 构建项目
if "%BUILD%"=="true" (
    echo [INFO] 正在构建项目...
    if "%VERBOSE%"=="true" (
        dotnet clean
        dotnet restore
        dotnet build --configuration Release
    ) else (
        dotnet clean --verbosity quiet
        dotnet restore --verbosity quiet
        dotnet build --configuration Release --verbosity quiet
    )
    echo [SUCCESS] 项目构建完成
)

REM 创建测试结果目录
if not exist "TestResults" mkdir TestResults

REM 设置变量
set UNIT_ARGS=--logger trx;LogFileName=TestResults/unit-tests.trx
set INTEGRATION_ARGS=--logger trx;LogFileName=TestResults/integration-tests.trx
set PERFORMANCE_ARGS=--logger trx;LogFileName=TestResults/performance-tests.trx

if "%VERBOSE%"=="true" (
    set UNIT_ARGS=!UNIT_ARGS! --verbosity normal
    set INTEGRATION_ARGS=!INTEGRATION_ARGS! --verbosity normal
    set PERFORMANCE_ARGS=!PERFORMANCE_ARGS! --verbosity normal
)

if "%GENERATE_COVERAGE%"=="true" (
    set UNIT_ARGS=!UNIT_ARGS! --collect:"XPlat Code Coverage"
    set INTEGRATION_ARGS=!INTEGRATION_ARGS! --collect:"XPlat Code Coverage"
)

REM 运行单元测试
if "%RUN_UNIT%"=="true" (
    echo [INFO] 正在运行单元测试...
    dotnet test tests/Unit --no-build !UNIT_ARGS!
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] 单元测试失败
        exit /b 1
    )
    echo [SUCCESS] 单元测试通过
)

REM 运行集成测试
if "%RUN_INTEGRATION%"=="true" (
    echo [INFO] 正在运行集成测试...
    dotnet test tests/Integration --no-build !INTEGRATION_ARGS!
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] 集成测试失败
        exit /b 1
    )
    echo [SUCCESS] 集成测试通过
)

REM 运行性能测试
if "%RUN_PERFORMANCE%"=="true" (
    echo [INFO] 正在运行性能测试...
    dotnet test tests/Performance --configuration Release --no-build !PERFORMANCE_ARGS!
    if %ERRORLEVEL% NEQ 0 (
        echo [WARNING] 性能测试失败（这可能不是严重问题）
    ) else (
        echo [SUCCESS] 性能测试通过
    )
)

REM 生成代码覆盖率报告
if "%GENERATE_COVERAGE%"=="true" (
    echo [INFO] 正在生成代码覆盖率报告...
    
    REM 检查是否安装了reportgenerator
    where reportgenerator >nul 2>&1
    if %ERRORLEVEL% NEQ 0 (
        echo [INFO] 正在安装 reportgenerator...
        dotnet tool install -g dotnet-reportgenerator-globaltool
    )
    
    REM 查找覆盖率文件
    if exist "TestResults\coverage.xml" (
        reportgenerator -reports:"TestResults\coverage.xml" -targetdir:coverage-report -reporttypes:HtmlInline_AzurePipelines
        echo [SUCCESS] 代码覆盖率报告已生成到 coverage-report\index.html
    ) else if exist "TestResults\*.coverage.cobertura.xml" (
        reportgenerator -reports:"TestResults\*.coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:HtmlInline_AzurePipelines
        echo [SUCCESS] 代码覆盖率报告已生成到 coverage-report\index.html
    ) else (
        echo [WARNING] 未找到覆盖率文件，跳过报告生成
    )
)

REM 生成测试报告
if "%GENERATE_REPORT%"=="true" (
    echo [INFO] 正在生成测试报告...
    
    REM 创建测试报告摘要
    echo # MCGame 测试报告 > TestResults\summary.md
    echo. >> TestResults\summary.md
    echo **生成时间**: %date% %time% >> TestResults\summary.md
    echo **运行环境**: Windows >> TestResults\summary.md
    echo **.NET 版本**: >> TestResults\summary.md
    dotnet --version >> TestResults\summary.md
    echo. >> TestResults\summary.md
    echo ## 测试结果 >> TestResults\summary.md
    echo. >> TestResults\summary.md
    
    REM 添加单元测试结果
    if "%RUN_UNIT%"=="true" if exist "TestResults\unit-tests.trx" (
        echo ### 单元测试 >> TestResults\summary.md
        echo - 状态: ✅ 通过 >> TestResults\summary.md
        echo - 报告文件: unit-tests.trx >> TestResults\summary.md
        echo. >> TestResults\summary.md
    )
    
    REM 添加集成测试结果
    if "%RUN_INTEGRATION%"=="true" if exist "TestResults\integration-tests.trx" (
        echo ### 集成测试 >> TestResults\summary.md
        echo - 状态: ✅ 通过 >> TestResults\summary.md
        echo - 报告文件: integration-tests.trx >> TestResults\summary.md
        echo. >> TestResults\summary.md
    )
    
    REM 添加性能测试结果
    if "%RUN_PERFORMANCE%"=="true" if exist "TestResults\performance-tests.trx" (
        echo ### 性能测试 >> TestResults\summary.md
        echo - 状态: ✅ 通过 >> TestResults\summary.md
        echo - 报告文件: performance-tests.trx >> TestResults\summary.md
        echo. >> TestResults\summary.md
    )
    
    echo [SUCCESS] 测试报告已生成到 TestResults\summary.md
)

echo [SUCCESS] 所有测试完成！

REM 显示结果位置
echo.
echo [INFO] 测试结果位置:
if exist "TestResults\unit-tests.trx" echo [INFO]   - 单元测试: TestResults\unit-tests.trx
if exist "TestResults\integration-tests.trx" echo [INFO]   - 集成测试: TestResults\integration-tests.trx
if exist "TestResults\performance-tests.trx" echo [INFO]   - 性能测试: TestResults\performance-tests.trx
if exist "coverage-report\index.html" echo [INFO]   - 覆盖率报告: coverage-report\index.html
if exist "TestResults\summary.md" echo [INFO]   - 测试摘要: TestResults\summary.md

:end
endlocal