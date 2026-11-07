#!/bin/bash

# ===================================================================
# 商品购销存管理系统 (PSI System) - 极端基准与并发安全一键测试脚本
# ===================================================================

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "🧪 正在启动商品购销存管理系统极端基准与并发安全自动化测试..."

# 检查后端服务是否就绪
if ! lsof -ti:5050 >/dev/null 2>&1; then
    echo "⚠️ 检测到后端服务 (端口 5050) 未运行，请先执行 ./start.sh 启动系统！"
    exit 1
fi

python3 "$PROJECT_ROOT/tests/run_extreme_tests.py"
