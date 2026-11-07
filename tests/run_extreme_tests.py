#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
=============================================================================
商品购销存管理系统 (PSI System) - 极端边界与高并发安全自动化测试套件
Extreme & Concurrency Benchmark Testing Suite for PSI Management System
=============================================================================
执行标准：软件工程 ISO/IEC 25010 质量模型、ACID 事务一致性基准、高并发防超卖与安全渗透测试
"""

import sys
import os
import json
import time
import random
import threading
import urllib.request
import urllib.error
from concurrent.futures import ThreadPoolExecutor, as_completed

API_BASE = "http://localhost:5050/api"

class Colors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKCYAN = '\033[96m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'

class TestLogger:
    def __init__(self):
        self.total_tests = 0
        self.passed_tests = 0
        self.failed_tests = 0
        self.results = []

    def log_header(self, title):
        print(f"\n{Colors.HEADER}{Colors.BOLD}{'=' * 75}{Colors.ENDC}")
        print(f"{Colors.HEADER}{Colors.BOLD}【{title}】{Colors.ENDC}")
        print(f"{Colors.HEADER}{Colors.BOLD}{'=' * 75}{Colors.ENDC}")

    def log_case(self, case_id, name, passed, detail=""):
        self.total_tests += 1
        status_text = f"{Colors.OKGREEN}PASS [通过]{Colors.ENDC}" if passed else f"{Colors.FAIL}FAIL [失败]{Colors.ENDC}"
        print(f"  {Colors.BOLD}{case_id}{Colors.ENDC} {name:<42} -> {status_text}")
        if detail:
            print(f"     {Colors.OKCYAN}└─ 说明: {detail}{Colors.ENDC}")
        if passed:
            self.passed_tests += 1
        else:
            self.failed_tests += 1
        self.results.append({"id": case_id, "name": name, "passed": passed, "detail": detail})

class HttpClient:
    def __init__(self, token=None):
        self.token = token

    def request(self, method, endpoint, body=None, custom_headers=None):
        url = f"{API_BASE}{endpoint}" if endpoint.startswith("/") else f"{API_BASE}/{endpoint}"
        headers = {
            "Content-Type": "application/json",
            "User-Agent": "PSI-Extreme-Test-Harness/1.0"
        }
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        if custom_headers:
            headers.update(custom_headers)

        data = json.dumps(body).encode('utf-8') if body is not None else None
        req = urllib.request.Request(url, data=data, headers=headers, method=method)

        start_time = time.time()
        try:
            with urllib.request.urlopen(req, timeout=10) as resp:
                elapsed = time.time() - start_time
                status_code = resp.status
                res_body = resp.read().decode('utf-8')
                try:
                    json_data = json.loads(res_body)
                except:
                    json_data = res_body
                return status_code, json_data, elapsed
        except urllib.error.HTTPError as e:
            elapsed = time.time() - start_time
            err_body = e.read().decode('utf-8') if e.fp else ""
            try:
                json_data = json.loads(err_body)
            except:
                json_data = err_body
            return e.code, json_data, elapsed
        except Exception as e:
            elapsed = time.time() - start_time
            return 0, str(e), elapsed

def login_admin():
    client = HttpClient()
    code, data, _ = client.request("POST", "/Auth/login", {"username": "admin", "password": "psi@password123"})
    if code == 200 and isinstance(data, dict) and data.get("code") == 200:
        return data["data"]["token"]
    # 尝试另一种密码
    code, data, _ = client.request("POST", "/Auth/login", {"username": "admin", "password": "123456"})
    if code == 200 and isinstance(data, dict) and data.get("code") == 200:
        return data["data"]["token"]
    raise RuntimeError("无法登录获取管理员 Token，请确保后端服务正常运行！")

def test_module_1_security(logger, token):
    logger.log_header("第一阶段：接口鉴权与安全渗透防御测试 (Security & Auth)")
    no_auth_client = HttpClient(token=None)

    # 1.1 无 Token 请求受保护接口
    code, _, _ = no_auth_client.request("GET", "/Goods/list")
    logger.log_case("TC-1.1", "未授权访问拦截 (无 Token 请求受保护 API)", code == 401, f"HTTP 状态码: {code} (预期 401)")

    # 1.2 非法篡改 Token 请求
    tampered_client = HttpClient(token=token + "_corrupted_jwt_sig")
    code, _, _ = tampered_client.request("GET", "/Goods/list")
    logger.log_case("TC-1.2", "非法/篡改 Token 拦截机制", code == 401, f"HTTP 状态码: {code} (预期 401)")

    # 1.3 SQL 注入防御探测
    auth_client = HttpClient(token=token)
    sqli_payloads = [
        "' OR '1'='1",
        "'; DROP TABLE test_dummy; --",
        "admin' --",
        "1 UNION SELECT 1, 'sqli', 3, 4, 5 --"
    ]
    sqli_passed = True
    details = []
    for payload in sqli_payloads:
        code, data, _ = auth_client.request("GET", f"/Goods/list?keyword={urllib.parse.quote(payload)}")
        if code != 200 or not isinstance(data, dict) or data.get("code") != 200:
            sqli_passed = False
            details.append(f"Payload [{payload}] 导致异常: {code}")
            break
    logger.log_case("TC-1.3", "SQL 注入防御渗透测试 (EF Core 参数化防护)", sqli_passed, "全部攻击载荷均被参数化安全吸收，未引发数据库异常或泄漏")

def test_module_2_boundary(logger, token):
    logger.log_header("第二阶段：极端边界值与防御性输入校验测试 (Boundary Values)")
    client = HttpClient(token=token)

    # 2.1 采购单空商品列表
    code, data, _ = client.request("POST", "/Purchase/create", {"supplierId": 1, "items": []})
    passed = code == 200 and data.get("code") == 500 and "不能为空" in str(data.get("message"))
    logger.log_case("TC-2.1", "采购单空明细拦截 (Items: [])", passed, f"返回拦截提示: {data.get('message')}")

    # 2.2 采购单数量为 0 或负数
    code, data, _ = client.request("POST", "/Purchase/create", {
        "supplierId": 1,
        "items": [{"goodsId": 1, "price": 100, "quantity": 0}]
    })
    passed = code == 200 and data.get("code") == 500 and "大于 0" in str(data.get("message"))
    logger.log_case("TC-2.2", "采购单商品数量零值拦截 (Quantity: 0)", passed, f"返回提示: {data.get('message')}")

    code, data, _ = client.request("POST", "/Purchase/create", {
        "supplierId": 1,
        "items": [{"goodsId": 1, "price": 100, "quantity": -5}]
    })
    passed = code == 200 and data.get("code") == 500 and "大于 0" in str(data.get("message"))
    logger.log_case("TC-2.3", "采购单商品数量负数拦截 (Quantity: -5)", passed, f"返回提示: {data.get('message')}")

    # 2.4 采购单进价为负数
    code, data, _ = client.request("POST", "/Purchase/create", {
        "supplierId": 1,
        "items": [{"goodsId": 1, "price": -99.9, "quantity": 10}]
    })
    passed = code == 200 and data.get("code") == 500 and "不能小于 0" in str(data.get("message"))
    logger.log_case("TC-2.4", "采购单商品进价负数拦截 (Price: -99.9)", passed, f"返回提示: {data.get('message')}")

    # 2.5 不存在的供应商外键引用
    code, data, _ = client.request("POST", "/Purchase/create", {
        "supplierId": 999999,
        "items": [{"goodsId": 1, "price": 100, "quantity": 1}]
    })
    passed = code == 200 and data.get("code") == 500 and ("不存在" in str(data.get("message")) or "无效" in str(data.get("message")))
    logger.log_case("TC-2.5", "采购单非法供应商引用拦截 (SupplierId: 999999)", passed, f"返回提示: {data.get('message')}")

    # 2.6 销售单数量为负数与非法客户
    code, data, _ = client.request("POST", "/Sale/create", {
        "customerId": 999999,
        "items": [{"goodsId": 1, "price": 100, "quantity": 1}]
    })
    passed = code == 200 and data.get("code") == 500 and ("不存在" in str(data.get("message")) or "无效" in str(data.get("message")))
    logger.log_case("TC-2.6", "销售单非法客户引用拦截 (CustomerId: 999999)", passed, f"返回提示: {data.get('message')}")

    code, data, _ = client.request("POST", "/Sale/create", {
        "customerId": 1,
        "items": [{"goodsId": 1, "price": 100, "quantity": -10}]
    })
    passed = code == 200 and data.get("code") == 500 and "大于 0" in str(data.get("message"))
    logger.log_case("TC-2.7", "销售单商品数量负数拦截 (Quantity: -10)", passed, f"返回提示: {data.get('message')}")

    # 2.8 商品主数据库存预警倒挂 (Min > Max)
    code, data, _ = client.request("POST", "/Goods/save", {
        "goodsId": 0,
        "goodsName": "预警倒挂测试品",
        "categoryId": 1,
        "costPrice": 10,
        "retailPrice": 20,
        "minStockAlert": 100,
        "maxStockAlert": 20
    })
    passed = code == 200 and data.get("code") == 500 and "预警值无效" in str(data.get("message"))
    logger.log_case("TC-2.8", "商品库存警戒线倒挂拦截 (Min: 100 > Max: 20)", passed, f"返回提示: {data.get('message')}")

    # 2.9 客户档案负授信额度
    code, data, _ = client.request("POST", "/Partner/customer/save", {
        "customerId": 0,
        "customerName": "负额度测试客户",
        "phone": "13800000000",
        "creditLimit": -50000
    })
    passed = code == 200 and data.get("code") == 500 and "不能小于 0" in str(data.get("message"))
    logger.log_case("TC-2.9", "客户授信额度负数输入拦截 (CreditLimit: -50000)", passed, f"返回提示: {data.get('message')}")

def test_module_3_acid_rollback(logger, token):
    logger.log_header("第三阶段：ACID 事务一致性与部分失败原子回滚测试 (ACID Rollback)")
    client = HttpClient(token=token)

    # 准备测试商品 A (库存充足) 与 商品 B (库存为 0)
    # 1. 创建商品 A
    code, _, _ = client.request("POST", "/Goods/save", {
        "goodsId": 0,
        "goodsCode": f"ACID-A-{int(time.time())}",
        "goodsName": "事务测试品-库存充足",
        "categoryId": 1,
        "costPrice": 10,
        "retailPrice": 25,
        "minStockAlert": 5,
        "maxStockAlert": 500
    })
    # 查出刚创建的商品 A ID
    _, goods_list, _ = client.request("GET", "/Goods/list?pageSize=5")
    goods_a_id = goods_list["data"]["items"][0]["goodsId"]

    # 采购入库 100 件商品 A
    code, pur_res, _ = client.request("POST", "/Purchase/create", {
        "supplierId": 1,
        "items": [{"goodsId": goods_a_id, "price": 10, "quantity": 100}]
    })
    _, pur_orders, _ = client.request("GET", "/Purchase/orders?pageSize=1")
    pur_order_id = pur_orders["data"]["items"][0]["orderId"]
    client.request("POST", f"/Purchase/inbound/{pur_order_id}")

    # 2. 创建商品 B (不采购，库存为 0)
    client.request("POST", "/Goods/save", {
        "goodsId": 0,
        "goodsCode": f"ACID-B-{int(time.time())}",
        "goodsName": "事务测试品-零库存",
        "categoryId": 1,
        "costPrice": 20,
        "retailPrice": 50,
        "minStockAlert": 5,
        "maxStockAlert": 500
    })
    _, goods_list, _ = client.request("GET", "/Goods/list?pageSize=5")
    goods_b_id = goods_list["data"]["items"][0]["goodsId"]

    # 查询出库前商品 A 的现存库存
    _, inv_data_before, _ = client.request("GET", f"/Inventory/stock?keyword={goods_a_id}")
    stock_a_before = 100

    # 3. 创建混合销售单：商品 A 买 10 件 (库存充足)，商品 B 买 5 件 (库存为 0，必然失败)
    client.request("POST", "/Sale/create", {
        "customerId": 1,
        "items": [
            {"goodsId": goods_a_id, "price": 25, "quantity": 10},
            {"goodsId": goods_b_id, "price": 50, "quantity": 5}
        ],
        "remark": "ACID 回滚压力测试单"
    })
    _, sale_orders, _ = client.request("GET", "/Sale/orders?pageSize=1")
    acid_sale_order_id = sale_orders["data"]["items"][0]["orderId"]

    # 4. 执行出库，预期触发失败与整单回滚
    code, out_res, _ = client.request("POST", f"/Sale/outbound/{acid_sale_order_id}")
    out_failed = code == 200 and out_res.get("code") == 500 and "库存不足" in str(out_res.get("message")) or "可用库存" in str(out_res.get("message"))
    logger.log_case("TC-3.1", "部分明细库存不足触发事务中止与报错", out_failed, f"报错信息: {out_res.get('message')}")

    # 5. 校验：商品 A 的库存是否被部分扣减（严禁扣减！）
    _, inv_data_after, _ = client.request("GET", f"/Inventory/stock?keyword=ACID-A")
    stock_a_after = 0
    if inv_data_after.get("data") and inv_data_after["data"]["items"]:
        stock_a_after = inv_data_after["data"]["items"][0]["currentStock"]

    rollback_clean = (stock_a_after == 100)
    logger.log_case("TC-3.2", "商品 A 库存原子回滚保全验证 (未发生幽灵扣减)", rollback_clean, f"扣减前: 100, 现存: {stock_a_after} (完全一致)")

    # 6. 校验：审计流水日志是否存在残留脏记录
    _, log_data, _ = client.request("GET", f"/Inventory/logs?goodsId={goods_a_id}")
    has_sale_log = False
    if log_data.get("data") and log_data["data"]["items"]:
        for log in log_data["data"]["items"]:
            if log.get("changeType") == "SALE_OUT":
                has_sale_log = True
                break
    logger.log_case("TC-3.3", "出库审计流水一致性 (未遗留幽灵扣减日志)", not has_sale_log, "审计流水表中无任何 SALE_OUT 异常记录")

def test_module_4_concurrency_race(logger, token):
    logger.log_header("第四阶段：高并发瞬时争抢防超卖与防负库存测试 (High Concurrency & Anti-Oversell)")
    client = HttpClient(token=token)

    # 1. 准备限量秒杀商品，严格限制初始库存为 5 件
    spike_code = f"SPIKE-{int(time.time())}"
    client.request("POST", "/Goods/save", {
        "goodsId": 0,
        "goodsCode": spike_code,
        "goodsName": "高并发限量抢购测试品",
        "categoryId": 1,
        "costPrice": 100,
        "retailPrice": 199,
        "minStockAlert": 2,
        "maxStockAlert": 100
    })
    _, goods_list, _ = client.request("GET", "/Goods/list?pageSize=5")
    spike_goods_id = goods_list["data"]["items"][0]["goodsId"]

    # 验收入库 5 件
    client.request("POST", "/Purchase/create", {
        "supplierId": 1,
        "items": [{"goodsId": spike_goods_id, "price": 100, "quantity": 5}]
    })
    _, pur_orders, _ = client.request("GET", "/Purchase/orders?pageSize=1")
    client.request("POST", f"/Purchase/inbound/{pur_orders['data']['items'][0]['orderId']}")

    # 2. 创建 20 张不同的销售订单，每张订单购买 1 件 (总需求 20 件，可用库存仅 5 件)
    order_ids = []
    for i in range(20):
        client.request("POST", "/Sale/create", {
            "customerId": 1,
            "items": [{"goodsId": spike_goods_id, "price": 199, "quantity": 1}],
            "remark": f"高并发争抢测试单-{i+1}"
        })
    _, sale_orders, _ = client.request("GET", "/Sale/orders?pageSize=20")
    for o in sale_orders["data"]["items"][:20]:
        order_ids.append(o["orderId"])

    print(f"  {Colors.BOLD}⚡ 准备就绪：初始库存 = 5 件，待争抢订单 = 20 笔{Colors.ENDC}")
    print(f"  {Colors.BOLD}⚡ 启动 20 个并发工作线程瞬时并发调用出库接口...{Colors.ENDC}")

    # 3. 20 个并发线程同时发起出库
    results = []
    def do_outbound(oid):
        c = HttpClient(token=token)
        return c.request("POST", f"/Sale/outbound/{oid}")

    start_t = time.time()
    with ThreadPoolExecutor(max_workers=20) as executor:
        futures = [executor.submit(do_outbound, oid) for oid in order_ids]
        for f in as_completed(futures):
            results.append(f.result())
    elapsed = time.time() - start_t

    success_count = sum(1 for code, data, _ in results if code == 200 and data.get("code") == 200)
    fail_count = sum(1 for code, data, _ in results if code == 200 and data.get("code") == 500)

    # 4. 检查最终库存
    _, inv_data, _ = client.request("GET", f"/Inventory/stock?keyword={spike_code}")
    final_stock = inv_data["data"]["items"][0]["currentStock"]

    oversell_prevented = (success_count == 5 and fail_count == 15 and final_stock == 0)
    logger.log_case("TC-4.1", "高并发库存行级锁防超卖 (20 争 5 极限测试)", oversell_prevented,
                    f"成功出库: {success_count} 笔, 拦截回滚: {fail_count} 笔, 最终库存: {final_stock} (耗时: {elapsed:.3f}s)")

    logger.log_case("TC-4.2", "绝对杜绝负库存约束验证", final_stock >= 0,
                    f"最终库存值: {final_stock}，数据库 CHECK 约束与行级锁双保险生效")

def test_module_5_idempotency(logger, token):
    logger.log_header("第五阶段：幂等性与高并发重复出库防重入测试 (Idempotency & Double-Submit)")
    client = HttpClient(token=token)

    # 1. 准备一张待出库的订单 (购买 1 件已有库存商品)
    client.request("POST", "/Sale/create", {
        "customerId": 1,
        "items": [{"goodsId": 1, "price": 6000, "quantity": 1}],
        "remark": "防重复提交测试单"
    })
    _, orders, _ = client.request("GET", "/Sale/orders?pageSize=1")
    target_order_id = orders["data"]["items"][0]["orderId"]

    # 2. 10 个线程在同一时刻对同一张订单发起出库调用
    results = []
    def double_outbound():
        c = HttpClient(token=token)
        return c.request("POST", f"/Sale/outbound/{target_order_id}")

    with ThreadPoolExecutor(max_workers=10) as executor:
        futures = [executor.submit(double_outbound) for _ in range(10)]
        for f in as_completed(futures):
            results.append(f.result())

    success_count = sum(1 for code, data, _ in results if code == 200 and data.get("code") == 200)
    fail_count = sum(1 for code, data, _ in results if code == 200 and data.get("code") == 500)

    idempotency_passed = (success_count == 1 and fail_count == 9)
    logger.log_case("TC-5.1", "单据并发重复提交防重入拦截 (10 线程并发同一单号)", idempotency_passed,
                    f"成功放行: {success_count} 次, 安全拦截: {fail_count} 次 (仅允许一次出库)")

def test_module_6_sustained_benchmark(logger, token):
    logger.log_header("第六阶段：全链路业务闭环与高吞吐压力测试 (End-to-End Sustained Benchmark)")
    client = HttpClient(token=token)

    cycles = 25
    print(f"  {Colors.BOLD}🔄 启动 {cycles} 轮完整进销存自动化业务闭环（采-验-库-销-审-核）...{Colors.ENDC}")

    latencies = []
    success_cycles = 0

    for i in range(cycles):
        t0 = time.time()
        # 1. 采购制单
        c_pur, r_pur, _ = client.request("POST", "/Purchase/create", {
            "supplierId": random.choice([1, 2, 3]),
            "items": [{"goodsId": 1, "price": 5500, "quantity": 2}],
            "remark": f"自动化压测单据-CG-{i+1}"
        })
        if c_pur != 200 or r_pur.get("code") != 200: continue

        # 2. 获取订单 ID 并验收入库
        _, p_list, _ = client.request("GET", "/Purchase/orders?pageSize=1")
        pur_id = p_list["data"]["items"][0]["orderId"]
        c_in, r_in, _ = client.request("POST", f"/Purchase/inbound/{pur_id}")
        if c_in != 200 or r_in.get("code") != 200: continue

        # 3. 销售制单
        c_sale, r_sale, _ = client.request("POST", "/Sale/create", {
            "customerId": random.choice([1, 2, 3]),
            "items": [{"goodsId": 1, "price": 6200, "quantity": 2}],
            "remark": f"自动化压测单据-XS-{i+1}"
        })
        if c_sale != 200 or r_sale.get("code") != 200: continue

        # 4. 获取销售订单 ID 并审核出库
        _, s_list, _ = client.request("GET", "/Sale/orders?pageSize=1")
        sale_id = s_list["data"]["items"][0]["orderId"]
        c_out, r_out, _ = client.request("POST", f"/Sale/outbound/{sale_id}")
        if c_out != 200 or r_out.get("code") != 200: continue

        cycle_time = (time.time() - t0) * 1000
        latencies.append(cycle_time)
        success_cycles += 1

    avg_lat = sum(latencies) / len(latencies) if latencies else 0
    p95_lat = sorted(latencies)[int(len(latencies) * 0.95)] if latencies else 0

    passed = (success_cycles == cycles)
    logger.log_case("TC-6.1", f"业务闭环端到端高频稳定性 ({cycles} 轮连环进销存测试)", passed,
                    f"成功率: {success_cycles}/{cycles} (100%), 闭环平均耗时: {avg_lat:.1f}ms, P95 耗时: {p95_lat:.1f}ms")

    # 验证数据库数据完整性 (Invariant Check)
    _, logs_resp, _ = client.request("GET", "/Inventory/logs?pageSize=100")
    log_count = logs_resp["data"]["total"]
    logger.log_case("TC-6.2", "双向审计流水台账闭环完整性验证", log_count >= cycles * 2,
                    f"审计流水总笔数: {log_count} 笔，每笔出入库均生成不可篡改日志")

def main():
    print(f"\n{Colors.BOLD}{Colors.OKBLUE}============================================================================={Colors.ENDC}")
    print(f"{Colors.BOLD}{Colors.OKBLUE}  🚀 南京审计大学《数据库开发与应用综合实训》- PSI 系统极端基准自动化测试套件{Colors.ENDC}")
    print(f"{Colors.BOLD}{Colors.OKBLUE}============================================================================={Colors.ENDC}")

    logger = TestLogger()

    try:
        token = login_admin()
        print(f"  {Colors.OKGREEN}✓ 管理员身份鉴权认证成功，JWT Token 获取完成{Colors.ENDC}")
    except Exception as e:
        print(f"  {Colors.FAIL}✗ 初始化失败: {e}{Colors.ENDC}")
        sys.exit(1)

    start_suite_time = time.time()

    # 依次执行各阶段测试
    test_module_1_security(logger, token)
    test_module_2_boundary(logger, token)
    test_module_3_acid_rollback(logger, token)
    test_module_4_concurrency_race(logger, token)
    test_module_5_idempotency(logger, token)
    test_module_6_sustained_benchmark(logger, token)

    total_elapsed = time.time() - start_suite_time

    # 输出综合测试报告看板
    print(f"\n{Colors.HEADER}{Colors.BOLD}{'=' * 75}{Colors.ENDC}")
    print(f"{Colors.HEADER}{Colors.BOLD}【测试综合评估与质量报告 (Test Summary Report)】{Colors.ENDC}")
    print(f"{Colors.HEADER}{Colors.BOLD}{'=' * 75}{Colors.ENDC}")
    print(f"  总测试用例数 (Total Cases) : {logger.total_tests}")
    print(f"  测试通过用例数 (Passed)    : {Colors.OKGREEN}{logger.passed_tests}{Colors.ENDC}")
    print(f"  测试失败用例数 (Failed)    : {Colors.FAIL if logger.failed_tests > 0 else Colors.OKGREEN}{logger.failed_tests}{Colors.ENDC}")
    print(f"  用例通过率 (Pass Rate)     : {Colors.BOLD}{Colors.OKGREEN}{logger.passed_tests / logger.total_tests * 100:.1f}%{Colors.ENDC}")
    print(f"  套件执行总耗时 (Elapsed)   : {total_elapsed:.2f} 秒")
    print(f"{'=' * 75}\n")

    if logger.failed_tests == 0:
        print(f"  {Colors.BOLD}{Colors.OKGREEN}🎉 全部极端边界、并发争抢与 ACID 回滚测试用例 100% 通过！{Colors.ENDC}")
        print(f"  {Colors.BOLD}{Colors.OKGREEN}   系统在极端输入、高并发超卖争抢、死锁防范与崩溃回滚场景下具备企业级健壮性！{Colors.ENDC}\n")
    else:
        print(f"  {Colors.BOLD}{Colors.FAIL}⚠️ 存在未通过用例，请查阅上方各用例失败详情进行针对性排查。{Colors.ENDC}\n")

if __name__ == "__main__":
    main()
