#!/bin/bash

# ===================================================================
# 商品购销存管理系统 (PSI System) - 一键本地启动脚本
# ===================================================================

export PATH="/usr/local/share/dotnet:/usr/local/bin:/opt/homebrew/bin:/Applications/Docker.app/Contents/Resources/bin:$HOME/.docker/bin:$PATH"

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
echo "🚀 正在启动商品购销存管理系统..."
echo "📂 项目路径: $PROJECT_ROOT"

# 1. 检查并确保 Docker SQL Server 容器运行
echo ""
echo "📦 [1/3] 检查 SQL Server 数据库容器..."
if ! docker ps --format '{{.Names}}' | grep -q "psi-sqlserver"; then
    echo "正在启动数据库容器..."
    docker compose -f "$PROJECT_ROOT/docker-compose.yml" up -d
    sleep 3
else
    echo "✅ 数据库容器已在运行中 (端口: 1433)"
fi

# 2. 启动后端 ASP.NET Core 8 Web API (端口 5050，避开 macOS 5000 AirPlay 冲突)
echo ""
echo "⚙️ [2/3] 启动后端服务 (ASP.NET Core 8 Web API)..."
lsof -ti:5050 | xargs kill -9 2>/dev/null || true
cd "$PROJECT_ROOT/backend/Psi.Api"
nohup dotnet run --no-launch-profile --urls "http://localhost:5050" < /dev/null > "$PROJECT_ROOT/backend.log" 2>&1 &
BACKEND_PID=$!
echo "✅ 后端服务已在后台启动 (PID: $BACKEND_PID, 地址: http://localhost:5050)"
echo "   Swagger 接口文档: http://localhost:5050/swagger"

# 3. 启动前端 Vue 3 + Vite
echo ""
echo "🎨 [3/3] 启动前端服务 (Vue 3 + Element Plus)..."
lsof -ti:5173 | xargs kill -9 2>/dev/null || true
cd "$PROJECT_ROOT/frontend"
CI=true nohup node ./node_modules/vite/bin/vite.js --host < /dev/null > "$PROJECT_ROOT/frontend.log" 2>&1 &
FRONTEND_PID=$!
echo "✅ 前端服务已在后台启动 (PID: $FRONTEND_PID, 地址: http://localhost:5173)"

sleep 2

echo ""
echo "================================================================="
echo "🎉 系统全部启动完成！"
echo ""
echo "  👉 前端管理系统入口: http://localhost:5173"
echo "  👉 后端 Swagger 文档: http://localhost:5050/swagger"
echo "  👉 默认登录账号: admin"
echo "  👉 默认登录密码: 123456"
echo ""
echo "提示：若需要停止服务，直接在终端执行 ./stop.sh 即可"
echo "================================================================="

open "http://localhost:5173" 2>/dev/null || true
