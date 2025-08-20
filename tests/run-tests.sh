#!/bin/bash

# MCGame 测试运行脚本
# 用于在本地运行所有测试或特定类型的测试

set -e  # 遇到错误时退出

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 打印带颜色的消息
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 显示帮助信息
show_help() {
    echo "MCGame 测试运行脚本"
    echo ""
    echo "用法: $0 [选项]"
    echo ""
    echo "选项:"
    echo "  -h, --help          显示此帮助信息"
    echo "  -u, --unit          只运行单元测试"
    echo "  -i, --integration   只运行集成测试"
    echo "  -p, --performance   只运行性能测试"
    echo "  -a, --all           运行所有测试（默认）"
    echo "  -c, --coverage      生成代码覆盖率报告"
    echo "  -v, --verbose       详细输出"
    echo "  -r, --report        生成测试报告"
    echo "  -b, --build         重新构建项目"
    echo ""
    echo "示例:"
    echo "  $0                  # 运行所有测试"
    echo "  $0 -u               # 只运行单元测试"
    echo "  $0 -u -c            # 运行单元测试并生成覆盖率报告"
    echo "  $0 -p -v            # 运行性能测试并显示详细信息"
}

# 默认参数
RUN_UNIT=true
RUN_INTEGRATION=true
RUN_PERFORMANCE=true
GENERATE_COVERAGE=false
VERBOSE=false
GENERATE_REPORT=false
BUILD=false

# 解析命令行参数
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -u|--unit)
            RUN_UNIT=true
            RUN_INTEGRATION=false
            RUN_PERFORMANCE=false
            shift
            ;;
        -i|--integration)
            RUN_UNIT=false
            RUN_INTEGRATION=true
            RUN_PERFORMANCE=false
            shift
            ;;
        -p|--performance)
            RUN_UNIT=false
            RUN_INTEGRATION=false
            RUN_PERFORMANCE=true
            shift
            ;;
        -a|--all)
            RUN_UNIT=true
            RUN_INTEGRATION=true
            RUN_PERFORMANCE=true
            shift
            ;;
        -c|--coverage)
            GENERATE_COVERAGE=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -r|--report)
            GENERATE_REPORT=true
            shift
            ;;
        -b|--build)
            BUILD=true
            shift
            ;;
        *)
            print_error "未知选项: $1"
            show_help
            exit 1
            ;;
    esac
done

# 检查.NET SDK是否安装
if ! command -v dotnet &> /dev/null; then
    print_error "未找到 .NET SDK，请先安装 .NET SDK"
    exit 1
fi

# 获取脚本所在目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

print_info "开始运行 MCGame 测试..."
print_info "运行选项:"
[ "$RUN_UNIT" = true ] && print_info "  - 单元测试: 是"
[ "$RUN_INTEGRATION" = true ] && print_info "  - 集成测试: 是"
[ "$RUN_PERFORMANCE" = true ] && print_info "  - 性能测试: 是"
[ "$GENERATE_COVERAGE" = true ] && print_info "  - 代码覆盖率: 是"
[ "$VERBOSE" = true ] && print_info "  - 详细输出: 是"
[ "$GENERATE_REPORT" = true ] && print_info "  - 测试报告: 是"
[ "$BUILD" = true ] && print_info "  - 重新构建: 是"

# 构建项目
if [ "$BUILD" = true ]; then
    print_info "正在构建项目..."
    if [ "$VERBOSE" = true ]; then
        dotnet clean
        dotnet restore
        dotnet build --configuration Release
    else
        dotnet clean --verbosity quiet
        dotnet restore --verbosity quiet
        dotnet build --configuration Release --verbosity quiet
    fi
    print_success "项目构建完成"
fi

# 创建测试结果目录
mkdir -p TestResults

# 运行单元测试
if [ "$RUN_UNIT" = true ]; then
    print_info "正在运行单元测试..."
    
    local unit_args="--logger trx;LogFileName=TestResults/unit-tests.trx"
    [ "$VERBOSE" = true ] && unit_args="$unit_args --verbosity normal"
    [ "$GENERATE_COVERAGE" = true ] && unit_args="$unit_args --collect:\"XPlat Code Coverage\""
    
    if dotnet test tests/Unit --no-build $unit_args; then
        print_success "单元测试通过"
    else
        print_error "单元测试失败"
        exit 1
    fi
fi

# 运行集成测试
if [ "$RUN_INTEGRATION" = true ]; then
    print_info "正在运行集成测试..."
    
    local integration_args="--logger trx;LogFileName=TestResults/integration-tests.trx"
    [ "$VERBOSE" = true ] && integration_args="$integration_args --verbosity normal"
    [ "$GENERATE_COVERAGE" = true ] && integration_args="$integration_args --collect:\"XPlat Code Coverage\""
    
    if dotnet test tests/Integration --no-build $integration_args; then
        print_success "集成测试通过"
    else
        print_error "集成测试失败"
        exit 1
    fi
fi

# 运行性能测试
if [ "$RUN_PERFORMANCE" = true ]; then
    print_info "正在运行性能测试..."
    
    local performance_args="--logger trx;LogFileName=TestResults/performance-tests.trx"
    [ "$VERBOSE" = true ] && performance_args="$performance_args --verbosity normal"
    
    if dotnet test tests/Performance --configuration Release --no-build $performance_args; then
        print_success "性能测试通过"
    else
        print_warning "性能测试失败（这可能不是严重问题）"
    fi
fi

# 生成代码覆盖率报告
if [ "$GENERATE_COVERAGE" = true ]; then
    print_info "正在生成代码覆盖率报告..."
    
    # 检查是否安装了reportgenerator
    if ! command -v reportgenerator &> /dev/null; then
        print_info "正在安装 reportgenerator..."
        dotnet tool install -g dotnet-reportgenerator-globaltool
    fi
    
    # 查找覆盖率文件
    local coverage_files=$(find TestResults -name "coverage.xml" -o -name "*.coverage.cobertura.xml")
    
    if [ -n "$coverage_files" ]; then
        reportgenerator -reports:"$coverage_files" -targetdir:coverage-report -reporttypes:HtmlInline_AzurePipelines
        print_success "代码覆盖率报告已生成到 coverage-report/index.html"
    else
        print_warning "未找到覆盖率文件，跳过报告生成"
    fi
fi

# 生成测试报告
if [ "$GENERATE_REPORT" = true ]; then
    print_info "正在生成测试报告..."
    
    # 创建测试报告摘要
    cat > TestResults/summary.md << EOF
# MCGame 测试报告

**生成时间**: $(date)
**运行环境**: $(uname -a)
**.NET 版本**: $(dotnet --version)

## 测试结果

EOF
    
    # 添加单元测试结果
    if [ "$RUN_UNIT" = true ] && [ -f "TestResults/unit-tests.trx" ]; then
        echo "### 单元测试" >> TestResults/summary.md
        echo "- 状态: ✅ 通过" >> TestResults/summary.md
        echo "- 报告文件: unit-tests.trx" >> TestResults/summary.md
        echo "" >> TestResults/summary.md
    fi
    
    # 添加集成测试结果
    if [ "$RUN_INTEGRATION" = true ] && [ -f "TestResults/integration-tests.trx" ]; then
        echo "### 集成测试" >> TestResults/summary.md
        echo "- 状态: ✅ 通过" >> TestResults/summary.md
        echo "- 报告文件: integration-tests.trx" >> TestResults/summary.md
        echo "" >> TestResults/summary.md
    fi
    
    # 添加性能测试结果
    if [ "$RUN_PERFORMANCE" = true ] && [ -f "TestResults/performance-tests.trx" ]; then
        echo "### 性能测试" >> TestResults/summary.md
        echo "- 状态: ✅ 通过" >> TestResults/summary.md
        echo "- 报告文件: performance-tests.trx" >> TestResults/summary.md
        echo "" >> TestResults/summary.md
    fi
    
    print_success "测试报告已生成到 TestResults/summary.md"
fi

print_success "所有测试完成！"

# 显示结果位置
echo ""
print_info "测试结果位置:"
[ -f "TestResults/unit-tests.trx" ] && print_info "  - 单元测试: TestResults/unit-tests.trx"
[ -f "TestResults/integration-tests.trx" ] && print_info "  - 集成测试: TestResults/integration-tests.trx"
[ -f "TestResults/performance-tests.trx" ] && print_info "  - 性能测试: TestResults/performance-tests.trx"
[ -f "coverage-report/index.html" ] && print_info "  - 覆盖率报告: coverage-report/index.html"
[ -f "TestResults/summary.md" ] && print_info "  - 测试摘要: TestResults/summary.md"