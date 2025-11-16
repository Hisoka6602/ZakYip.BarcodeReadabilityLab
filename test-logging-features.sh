#!/bin/bash

# 日志配置功能验证脚本
# 此脚本用于验证新增的日志配置功能

echo "=========================================="
echo "日志配置功能验证测试"
echo "=========================================="
echo ""

# 设置服务 URL
BASE_URL="http://localhost:4000"

echo "1. 测试环境：$BASE_URL"
echo ""

# 测试 1: 获取当前日志级别
echo "测试 1: 获取当前日志级别"
echo "执行命令: curl -s ${BASE_URL}/api/logging/level"
echo "----------------------------------------"
curl -s ${BASE_URL}/api/logging/level | python3 -m json.tool 2>/dev/null || echo "服务未运行或响应异常"
echo ""
echo ""

# 测试 2: 设置日志级别为 Debug
echo "测试 2: 设置日志级别为 Debug"
echo "执行命令: curl -s -X PUT ${BASE_URL}/api/logging/level -H 'Content-Type: application/json' -d '{\"level\":\"Debug\",\"operator\":\"test-user\"}'"
echo "----------------------------------------"
curl -s -X PUT ${BASE_URL}/api/logging/level \
  -H "Content-Type: application/json" \
  -d '{"level":"Debug","operator":"test-user"}' | python3 -m json.tool 2>/dev/null || echo "服务未运行或响应异常"
echo ""
echo ""

# 测试 3: 再次获取日志级别，确认更改
echo "测试 3: 确认日志级别已更改为 Debug"
echo "执行命令: curl -s ${BASE_URL}/api/logging/level"
echo "----------------------------------------"
curl -s ${BASE_URL}/api/logging/level | python3 -m json.tool 2>/dev/null || echo "服务未运行或响应异常"
echo ""
echo ""

# 测试 4: 恢复日志级别为 Information
echo "测试 4: 恢复日志级别为 Information"
echo "执行命令: curl -s -X PUT ${BASE_URL}/api/logging/level -H 'Content-Type: application/json' -d '{\"level\":\"Information\",\"operator\":\"test-user\"}'"
echo "----------------------------------------"
curl -s -X PUT ${BASE_URL}/api/logging/level \
  -H "Content-Type: application/json" \
  -d '{"level":"Information","operator":"test-user"}' | python3 -m json.tool 2>/dev/null || echo "服务未运行或响应异常"
echo ""
echo ""

# 测试 5: 测试无效的日志级别
echo "测试 5: 测试无效的日志级别（应该返回错误）"
echo "执行命令: curl -s -X PUT ${BASE_URL}/api/logging/level -H 'Content-Type: application/json' -d '{\"level\":\"InvalidLevel\",\"operator\":\"test-user\"}'"
echo "----------------------------------------"
curl -s -X PUT ${BASE_URL}/api/logging/level \
  -H "Content-Type: application/json" \
  -d '{"level":"InvalidLevel","operator":"test-user"}' | python3 -m json.tool 2>/dev/null || echo "服务未运行或响应异常"
echo ""
echo ""

# 测试 6: 检查审计日志
echo "测试 6: 查看最新的审计日志（如果日志文件存在）"
echo "----------------------------------------"
if [ -d "logs" ]; then
    echo "查找最新的日志文件..."
    LATEST_LOG=$(ls -t logs/barcode-lab-*.log 2>/dev/null | head -1)
    if [ -n "$LATEST_LOG" ]; then
        echo "最新日志文件: $LATEST_LOG"
        echo ""
        echo "最近的 API 请求日志:"
        tail -20 "$LATEST_LOG" | grep "API 请求" || echo "未找到 API 请求日志"
    else
        echo "未找到日志文件"
    fi
else
    echo "日志目录不存在，请先运行服务"
fi
echo ""
echo ""

echo "=========================================="
echo "测试完成"
echo "=========================================="
echo ""
echo "注意事项:"
echo "1. 如果所有测试返回'服务未运行或响应异常'，请确保服务正在运行"
echo "2. 启动服务命令: cd src/ZakYip.BarcodeReadabilityLab.Service && dotnet run"
echo "3. 查看完整日志: tail -f logs/barcode-lab-*.log"
echo "4. 查看审计日志: tail -f logs/barcode-lab-*.log | grep 'API 请求'"
echo "5. 查看慢操作: tail -f logs/barcode-lab-*.log | grep '慢操作'"
