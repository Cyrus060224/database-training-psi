# 商品购销存管理系统 (PSI System)

> 南京审计大学《数据库开发与应用综合实训》课程项目

---

## 📖 项目简介
本系统是一套基于现代化 Web 架构的**商品购销存管理系统 (Purchase, Sales and Inventory Management System)**。系统面向商业企业的进销存业务场景，遵循软件工程规范与关系数据库规范化理论进行设计，完整实现了从采购进货、入库验收、销售开单、出库审核到库存审计盘点的全流程闭环。

---

## 🛠 技术架构
- **前端前端**：Vue 3 + Vite + Element Plus + Pinia + Axios + ECharts
- **后端服务**：ASP.NET Core 8.0 Web API (C#)
- **数据库**：Microsoft SQL Server / Azure SQL Edge
- **ORM 与数据访问**：Entity Framework Core 8.0 + 原生事务 SQL (ACID 保障)
- **容器化支持**：Docker & Docker Compose

---

## 🌟 核心特色与答辩考点设计
1. **RBAC 多角色权限控制**：系统管理员、采购专员、销售代表、仓管员四类角色严格隔离。
2. **并发防超卖与事务控制**：销售出库审核采用底层原子行锁与事务机制，保证高并发场景下绝不出现超卖与负库存。
3. **出入库全生命周期审计流水**：每一次采购验收入库（`PUR_IN`）和销售审核出库（`SALE_OUT`）均自动生成不可篡改的库存变动快照。
4. **可视化监控看板**：集成 ECharts 图表，动态展示销售额、采购支出、畅销商品 TOP 5 排行以及低于安全阈值的库存告警。

---

## 🚀 快速启动指南

### 1. 启动环境依赖
确保本地已安装 Docker Desktop，并在项目根目录运行：
```bash
./start.sh
```
该脚本会自动检查并启动 SQL Server 数据库容器、后台启动后端 API 服务以及前端开发服务器，并自动在浏览器中打开系统。

### 2. 服务地址与默认账号
- **前端系统入口**：[http://localhost:5173](http://localhost:5173)
- **Swagger 接口文档**：[http://localhost:5050/swagger](http://localhost:5050/swagger)
- **默认管理员账号**：`admin` / `123456`
- **采购专员账号**：`purchaser01` / `123456`
- **销售代表账号**：`sales01` / `123456`
- **仓储管理员账号**：`keeper01` / `123456`

### 3. 停止服务
```bash
./stop.sh
```
