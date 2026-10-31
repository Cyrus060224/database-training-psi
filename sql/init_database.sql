-- ===================================================================
-- 项目名称：商品购销存管理系统 (PSI System)
-- 适用课程：《数据库开发与应用综合实训》- 南京审计大学
-- 数据库版本：Microsoft SQL Server 2016 / 2019 / 2022 / Azure SQL Edge
-- 说明：包含数据库创建、安全倒序清理、表定义、完整性约束、索引优化、存储过程与种子数据
-- ===================================================================

-- 1. 创建数据库（若不存在）
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'PsiDb')
BEGIN
    CREATE DATABASE PsiDb COLLATE Chinese_PRC_CI_AS;
END
GO

USE PsiDb;
GO

-- ===================================================================
-- 第零部分：清理已存在对象（按外键依赖严格倒序删除，确保可反复重复执行）
-- ===================================================================

-- 0.1 删除视图
DROP VIEW IF EXISTS dbo.v_inventory_alert;
DROP VIEW IF EXISTS dbo.v_top_selling_goods;

-- 0.2 删除存储过程
DROP PROCEDURE IF EXISTS dbo.sp_ExecutePurchaseInbound;
DROP PROCEDURE IF EXISTS dbo.sp_ExecuteSaleOutbound;

-- 0.3 按外键从属关系【从最底层子表到最顶层父表】严格倒序删除
DROP TABLE IF EXISTS dbo.wms_stock_log;
DROP TABLE IF EXISTS dbo.sale_order_detail;
DROP TABLE IF EXISTS dbo.sale_order;
DROP TABLE IF EXISTS dbo.pur_order_detail;
DROP TABLE IF EXISTS dbo.pur_order;
DROP TABLE IF EXISTS dbo.wms_inventory;
DROP TABLE IF EXISTS dbo.base_goods;
DROP TABLE IF EXISTS dbo.base_customer;
DROP TABLE IF EXISTS dbo.base_supplier;
DROP TABLE IF EXISTS dbo.base_category;
DROP TABLE IF EXISTS dbo.sys_role_menu;
DROP TABLE IF EXISTS dbo.sys_menu;
DROP TABLE IF EXISTS dbo.sys_user_role;
DROP TABLE IF EXISTS dbo.sys_user;
DROP TABLE IF EXISTS dbo.sys_role;
GO


-- ===================================================================
-- 第一部分：系统管理与权限控制模块 (RBAC)
-- ===================================================================

-- 1.1 角色表
CREATE TABLE dbo.sys_role (
    role_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sys_role PRIMARY KEY,
    role_code VARCHAR(50) NOT NULL CONSTRAINT UQ_sys_role_code UNIQUE,
    role_name NVARCHAR(50) NOT NULL,
    description NVARCHAR(200) NULL,
    create_time DATETIME NOT NULL CONSTRAINT DF_sys_role_create_time DEFAULT GETDATE()
);
GO

-- 1.2 用户表
CREATE TABLE dbo.sys_user (
    user_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sys_user PRIMARY KEY,
    username VARCHAR(50) NOT NULL CONSTRAINT UQ_sys_user_username UNIQUE,
    password_hash VARCHAR(128) NOT NULL, -- 密码密文存储 (如 SHA256)
    real_name NVARCHAR(50) NOT NULL,
    phone VARCHAR(20) NULL,
    email VARCHAR(100) NULL,
    status TINYINT NOT NULL CONSTRAINT DF_sys_user_status DEFAULT 1, -- 1: 启用, 0: 停用
    create_time DATETIME NOT NULL CONSTRAINT DF_sys_user_create_time DEFAULT GETDATE()
);
GO

-- 1.3 用户-角色关联表 (多对多)
CREATE TABLE dbo.sys_user_role (
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sys_user_role PRIMARY KEY,
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    CONSTRAINT FK_sys_user_role_user FOREIGN KEY (user_id) REFERENCES dbo.sys_user(user_id) ON DELETE CASCADE,
    CONSTRAINT FK_sys_user_role_role FOREIGN KEY (role_id) REFERENCES dbo.sys_role(role_id) ON DELETE CASCADE,
    CONSTRAINT UQ_sys_user_role UNIQUE (user_id, role_id)
);
GO

-- 1.4 菜单与权限表
CREATE TABLE dbo.sys_menu (
    menu_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sys_menu PRIMARY KEY,
    parent_id INT NULL,
    menu_name NVARCHAR(50) NOT NULL,
    route_path VARCHAR(100) NULL,
    component_path VARCHAR(100) NULL,
    permission_code VARCHAR(100) NULL,
    menu_type CHAR(1) NOT NULL CONSTRAINT CK_sys_menu_type CHECK (menu_type IN ('M', 'B')), -- M: 菜单, B: 按钮/操作
    icon VARCHAR(50) NULL,
    sort_order INT NOT NULL CONSTRAINT DF_sys_menu_sort DEFAULT 0,
    create_time DATETIME NOT NULL CONSTRAINT DF_sys_menu_create_time DEFAULT GETDATE()
);
GO

-- 1.5 角色-菜单权限关联表
CREATE TABLE dbo.sys_role_menu (
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sys_role_menu PRIMARY KEY,
    role_id INT NOT NULL,
    menu_id INT NOT NULL,
    CONSTRAINT FK_sys_role_menu_role FOREIGN KEY (role_id) REFERENCES dbo.sys_role(role_id) ON DELETE CASCADE,
    CONSTRAINT FK_sys_role_menu_menu FOREIGN KEY (menu_id) REFERENCES dbo.sys_menu(menu_id) ON DELETE CASCADE,
    CONSTRAINT UQ_sys_role_menu UNIQUE (role_id, menu_id)
);
GO


-- ===================================================================
-- 第二部分：基础档案信息模块
-- ===================================================================

-- 2.1 商品类别表 (支持层级分类)
CREATE TABLE dbo.base_category (
    category_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_base_category PRIMARY KEY,
    parent_id INT NULL,
    category_code VARCHAR(50) NOT NULL CONSTRAINT UQ_base_category_code UNIQUE,
    category_name NVARCHAR(50) NOT NULL,
    sort_order INT NOT NULL CONSTRAINT DF_base_category_sort DEFAULT 0,
    create_time DATETIME NOT NULL CONSTRAINT DF_base_category_create_time DEFAULT GETDATE()
);
GO

-- 2.2 供应商表
CREATE TABLE dbo.base_supplier (
    supplier_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_base_supplier PRIMARY KEY,
    supplier_code VARCHAR(50) NOT NULL CONSTRAINT UQ_base_supplier_code UNIQUE,
    supplier_name NVARCHAR(100) NOT NULL,
    contact_person NVARCHAR(50) NULL,
    phone VARCHAR(20) NOT NULL,
    address NVARCHAR(200) NULL,
    bank_name NVARCHAR(100) NULL,
    bank_account VARCHAR(50) NULL,
    status TINYINT NOT NULL CONSTRAINT DF_base_supplier_status DEFAULT 1,
    create_time DATETIME NOT NULL CONSTRAINT DF_base_supplier_create_time DEFAULT GETDATE()
);
GO

-- 2.3 客户表
CREATE TABLE dbo.base_customer (
    customer_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_base_customer PRIMARY KEY,
    customer_code VARCHAR(50) NOT NULL CONSTRAINT UQ_base_customer_code UNIQUE,
    customer_name NVARCHAR(100) NOT NULL,
    contact_person NVARCHAR(50) NULL,
    phone VARCHAR(20) NOT NULL,
    address NVARCHAR(200) NULL,
    credit_limit DECIMAL(12,2) NOT NULL CONSTRAINT DF_base_customer_credit DEFAULT 0.00,
    status TINYINT NOT NULL CONSTRAINT DF_base_customer_status DEFAULT 1,
    create_time DATETIME NOT NULL CONSTRAINT DF_base_customer_create_time DEFAULT GETDATE()
);
GO

-- 2.4 商品主数据表
CREATE TABLE dbo.base_goods (
    goods_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_base_goods PRIMARY KEY,
    goods_code VARCHAR(50) NOT NULL CONSTRAINT UQ_base_goods_code UNIQUE,
    goods_name NVARCHAR(100) NOT NULL,
    category_id INT NOT NULL,
    barcode VARCHAR(50) NULL,
    specification NVARCHAR(100) NULL, -- 规格型号
    unit NVARCHAR(20) NOT NULL CONSTRAINT DF_base_goods_unit DEFAULT N'件',
    cost_price DECIMAL(10,2) NOT NULL CONSTRAINT CK_base_goods_cost_price CHECK (cost_price >= 0.00),
    retail_price DECIMAL(10,2) NOT NULL CONSTRAINT CK_base_goods_retail_price CHECK (retail_price >= 0.00),
    min_stock_alert INT NOT NULL CONSTRAINT DF_base_goods_min_stock DEFAULT 10 CONSTRAINT CK_base_goods_min_stock CHECK (min_stock_alert >= 0),
    max_stock_alert INT NOT NULL CONSTRAINT DF_base_goods_max_stock DEFAULT 1000,
    status TINYINT NOT NULL CONSTRAINT DF_base_goods_status DEFAULT 1,
    create_time DATETIME NOT NULL CONSTRAINT DF_base_goods_create_time DEFAULT GETDATE(),
    CONSTRAINT FK_base_goods_category FOREIGN KEY (category_id) REFERENCES dbo.base_category(category_id),
    CONSTRAINT CK_base_goods_stock_range CHECK (max_stock_alert >= min_stock_alert)
);
GO


-- ===================================================================
-- 第三部分：仓储与进销存业务模块 (核心业务与事务支持)
-- ===================================================================

-- 3.1 仓库库存表
CREATE TABLE dbo.wms_inventory (
    inventory_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_wms_inventory PRIMARY KEY,
    goods_id INT NOT NULL CONSTRAINT UQ_wms_inventory_goods UNIQUE,
    current_stock INT NOT NULL CONSTRAINT DF_wms_inventory_stock DEFAULT 0 CONSTRAINT CK_wms_inventory_stock CHECK (current_stock >= 0), -- 约束：库存绝对禁止小于0
    last_inbound_time DATETIME NULL,
    last_outbound_time DATETIME NULL,
    updated_time DATETIME NOT NULL CONSTRAINT DF_wms_inventory_updated DEFAULT GETDATE(),
    CONSTRAINT FK_wms_inventory_goods FOREIGN KEY (goods_id) REFERENCES dbo.base_goods(goods_id) ON DELETE CASCADE
);
GO

-- 3.2 采购入库单主表
CREATE TABLE dbo.pur_order (
    order_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_pur_order PRIMARY KEY,
    order_no VARCHAR(50) NOT NULL CONSTRAINT UQ_pur_order_no UNIQUE,
    supplier_id INT NOT NULL,
    purchaser_id INT NOT NULL, -- 采购员 (sys_user)
    warehouse_keeper_id INT NULL, -- 验收入库员 (sys_user)
    total_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_pur_order_amount DEFAULT 0.00 CONSTRAINT CK_pur_order_amount CHECK (total_amount >= 0.00),
    order_status TINYINT NOT NULL CONSTRAINT DF_pur_order_status DEFAULT 0, -- 0: 待入库, 1: 已入库, 2: 已取消
    remark NVARCHAR(500) NULL,
    order_date DATETIME NOT NULL CONSTRAINT DF_pur_order_date DEFAULT GETDATE(),
    inbound_date DATETIME NULL,
    CONSTRAINT FK_pur_order_supplier FOREIGN KEY (supplier_id) REFERENCES dbo.base_supplier(supplier_id),
    CONSTRAINT FK_pur_order_purchaser FOREIGN KEY (purchaser_id) REFERENCES dbo.sys_user(user_id),
    CONSTRAINT FK_pur_order_keeper FOREIGN KEY (warehouse_keeper_id) REFERENCES dbo.sys_user(user_id)
);
GO

-- 3.3 采购入库单明细表 (从表)
CREATE TABLE dbo.pur_order_detail (
    detail_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_pur_order_detail PRIMARY KEY,
    order_id INT NOT NULL,
    goods_id INT NOT NULL,
    purchase_price DECIMAL(10,2) NOT NULL CONSTRAINT CK_pur_detail_price CHECK (purchase_price >= 0.00),
    quantity INT NOT NULL CONSTRAINT CK_pur_detail_qty CHECK (quantity > 0),
    subtotal DECIMAL(12,2) NOT NULL CONSTRAINT CK_pur_detail_subtotal CHECK (subtotal >= 0.00),
    CONSTRAINT FK_pur_detail_order FOREIGN KEY (order_id) REFERENCES dbo.pur_order(order_id) ON DELETE CASCADE,
    CONSTRAINT FK_pur_detail_goods FOREIGN KEY (goods_id) REFERENCES dbo.base_goods(goods_id)
);
GO

-- 3.4 销售出库单主表
CREATE TABLE dbo.sale_order (
    order_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sale_order PRIMARY KEY,
    order_no VARCHAR(50) NOT NULL CONSTRAINT UQ_sale_order_no UNIQUE,
    customer_id INT NOT NULL,
    saler_id INT NOT NULL, -- 销售员 (sys_user)
    warehouse_keeper_id INT NULL, -- 出库审核员 (sys_user)
    total_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_sale_order_amount DEFAULT 0.00 CONSTRAINT CK_sale_order_amount CHECK (total_amount >= 0.00),
    order_status TINYINT NOT NULL CONSTRAINT DF_sale_order_status DEFAULT 0, -- 0: 待出库, 1: 已出库, 2: 已作废
    remark NVARCHAR(500) NULL,
    order_date DATETIME NOT NULL CONSTRAINT DF_sale_order_date DEFAULT GETDATE(),
    outbound_date DATETIME NULL,
    CONSTRAINT FK_sale_order_customer FOREIGN KEY (customer_id) REFERENCES dbo.base_customer(customer_id),
    CONSTRAINT FK_sale_order_saler FOREIGN KEY (saler_id) REFERENCES dbo.sys_user(user_id),
    CONSTRAINT FK_sale_order_keeper FOREIGN KEY (warehouse_keeper_id) REFERENCES dbo.sys_user(user_id)
);
GO

-- 3.5 销售出库单明细表 (从表)
CREATE TABLE dbo.sale_order_detail (
    detail_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sale_order_detail PRIMARY KEY,
    order_id INT NOT NULL,
    goods_id INT NOT NULL,
    sale_price DECIMAL(10,2) NOT NULL CONSTRAINT CK_sale_detail_price CHECK (sale_price >= 0.00),
    quantity INT NOT NULL CONSTRAINT CK_sale_detail_qty CHECK (quantity > 0),
    subtotal DECIMAL(12,2) NOT NULL CONSTRAINT CK_sale_detail_subtotal CHECK (subtotal >= 0.00),
    CONSTRAINT FK_sale_detail_order FOREIGN KEY (order_id) REFERENCES dbo.sale_order(order_id) ON DELETE CASCADE,
    CONSTRAINT FK_sale_detail_goods FOREIGN KEY (goods_id) REFERENCES dbo.base_goods(goods_id)
);
GO

-- 3.6 库存审计流水日志表 (记录每一次出入库实况)
CREATE TABLE dbo.wms_stock_log (
    log_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_wms_stock_log PRIMARY KEY,
    goods_id INT NOT NULL,
    change_type VARCHAR(20) NOT NULL, -- PUR_IN (采购入库), SALE_OUT (销售出库), CHECK_ADJ (盘点调整)
    related_order_no VARCHAR(50) NOT NULL,
    before_stock INT NOT NULL,
    change_qty INT NOT NULL, -- 正数增加，负数减少
    after_stock INT NOT NULL,
    operator_id INT NOT NULL,
    create_time DATETIME NOT NULL CONSTRAINT DF_wms_stock_log_time DEFAULT GETDATE(),
    CONSTRAINT FK_wms_stock_log_goods FOREIGN KEY (goods_id) REFERENCES dbo.base_goods(goods_id),
    CONSTRAINT FK_wms_stock_log_operator FOREIGN KEY (operator_id) REFERENCES dbo.sys_user(user_id)
);
GO


-- ===================================================================
-- 第四部分：数据库性能优化（高频查询索引创建）
-- ===================================================================

CREATE NONCLUSTERED INDEX IX_base_goods_name ON dbo.base_goods(goods_name);
CREATE NONCLUSTERED INDEX IX_base_goods_category ON dbo.base_goods(category_id);
CREATE NONCLUSTERED INDEX IX_pur_order_status_date ON dbo.pur_order(order_status, order_date DESC);
CREATE NONCLUSTERED INDEX IX_sale_order_status_date ON dbo.sale_order(order_status, order_date DESC);
CREATE NONCLUSTERED INDEX IX_wms_stock_log_goods_time ON dbo.wms_stock_log(goods_id, create_time DESC);
GO


-- ===================================================================
-- 第五部分：核心业务存储过程（体现事务 ACID 与并发防超卖控制）
-- ===================================================================

-- 5.1 采购单确认验收入库存储过程 (带事务处理)
CREATE PROCEDURE dbo.sp_ExecutePurchaseInbound
    @OrderId INT,
    @WarehouseKeeperId INT,
    @StatusCode INT OUTPUT,       -- 0: 成功, 1: 单据不存在或已处理, 99: 内部异常
    @StatusMessage NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. 检查订单状态是否为待入库 (order_status = 0)
        DECLARE @CurrentStatus TINYINT, @OrderNo VARCHAR(50);
        SELECT @CurrentStatus = order_status, @OrderNo = order_no
        FROM dbo.pur_order WITH (UPDLOCK, ROWLOCK)
        WHERE order_id = @OrderId;

        IF @CurrentStatus IS NULL
        BEGIN
            SET @StatusCode = 1;
            SET @StatusMessage = N'采购订单不存在！';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @CurrentStatus <> 0
        BEGIN
            SET @StatusCode = 1;
            SET @StatusMessage = N'该订单已完成入库或已取消，不可重复操作！';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 2. 遍历将明细中的商品增加至库存，并写入流水
        DECLARE @GoodsId INT, @Qty INT, @BeforeStock INT, @AfterStock INT;

        DECLARE cur_details CURSOR LOCAL FAST_FORWARD FOR
            SELECT goods_id, quantity FROM dbo.pur_order_detail WHERE order_id = @OrderId;

        OPEN cur_details;
        FETCH NEXT FROM cur_details INTO @GoodsId, @Qty;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- 获取当前库存 (若不存在则初始化)
            IF NOT EXISTS (SELECT 1 FROM dbo.wms_inventory WHERE goods_id = @GoodsId)
            BEGIN
                INSERT INTO dbo.wms_inventory (goods_id, current_stock, last_inbound_time, updated_time)
                VALUES (@GoodsId, 0, GETDATE(), GETDATE());
            END

            SELECT @BeforeStock = current_stock 
            FROM dbo.wms_inventory WITH (UPDLOCK, ROWLOCK)
            WHERE goods_id = @GoodsId;

            SET @AfterStock = @BeforeStock + @Qty;

            -- 更新库存
            UPDATE dbo.wms_inventory
            SET current_stock = @AfterStock,
                last_inbound_time = GETDATE(),
                updated_time = GETDATE()
            WHERE goods_id = @GoodsId;

            -- 插入库存流水
            INSERT INTO dbo.wms_stock_log (goods_id, change_type, related_order_no, before_stock, change_qty, after_stock, operator_id, create_time)
            VALUES (@GoodsId, 'PUR_IN', @OrderNo, @BeforeStock, @Qty, @AfterStock, @WarehouseKeeperId, GETDATE());

            FETCH NEXT FROM cur_details INTO @GoodsId, @Qty;
        END

        CLOSE cur_details;
        DEALLOCATE cur_details;

        -- 3. 更新采购单主表状态为已入库
        UPDATE dbo.pur_order
        SET order_status = 1,
            warehouse_keeper_id = @WarehouseKeeperId,
            inbound_date = GETDATE()
        WHERE order_id = @OrderId;

        COMMIT TRANSACTION;
        SET @StatusCode = 0;
        SET @StatusMessage = N'验收入库成功！';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        IF CURSOR_STATUS('local', 'cur_details') >= 0
        BEGIN
            CLOSE cur_details;
            DEALLOCATE cur_details;
        END
        SET @StatusCode = 99;
        SET @StatusMessage = ERROR_MESSAGE();
    END CATCH
END;
GO

-- 5.2 销售出库审核存储过程 (并发原子扣减、防超卖负库存机制)
CREATE PROCEDURE dbo.sp_ExecuteSaleOutbound
    @OrderId INT,
    @WarehouseKeeperId INT,
    @StatusCode INT OUTPUT,       -- 0: 成功, 1: 单据状态错误, 2: 库存不足, 99: 内部异常
    @StatusMessage NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. 锁定并校验订单状态
        DECLARE @CurrentStatus TINYINT, @OrderNo VARCHAR(50);
        SELECT @CurrentStatus = order_status, @OrderNo = order_no
        FROM dbo.sale_order WITH (UPDLOCK, ROWLOCK)
        WHERE order_id = @OrderId;

        IF @CurrentStatus IS NULL OR @CurrentStatus <> 0
        BEGIN
            SET @StatusCode = 1;
            SET @StatusMessage = N'订单不存在或已被审核出库！';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 2. 逐项校验库存并原子扣减
        DECLARE @GoodsId INT, @Qty INT, @GoodsName NVARCHAR(100), @BeforeStock INT, @AfterStock INT;

        DECLARE cur_sale CURSOR LOCAL FAST_FORWARD FOR
            SELECT d.goods_id, d.quantity, g.goods_name 
            FROM dbo.sale_order_detail d
            INNER JOIN dbo.base_goods g ON d.goods_id = g.goods_id
            WHERE d.order_id = @OrderId;

        OPEN cur_sale;
        FETCH NEXT FROM cur_sale INTO @GoodsId, @Qty, @GoodsName;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- 获取当前库存并上锁，防止并发超卖
            SELECT @BeforeStock = current_stock 
            FROM dbo.wms_inventory WITH (UPDLOCK, ROWLOCK)
            WHERE goods_id = @GoodsId;

            IF @BeforeStock IS NULL OR @BeforeStock < @Qty
            BEGIN
                SET @StatusCode = 2;
                SET @StatusMessage = N'商品【' + @GoodsName + N'】库存不足！当前库存：' + CAST(ISNULL(@BeforeStock, 0) AS NVARCHAR(20)) + N'，需求：' + CAST(@Qty AS NVARCHAR(20));
                CLOSE cur_sale;
                DEALLOCATE cur_sale;
                ROLLBACK TRANSACTION;
                RETURN;
            END

            SET @AfterStock = @BeforeStock - @Qty;

            -- 原子更新扣减库存
            UPDATE dbo.wms_inventory
            SET current_stock = @AfterStock,
                last_outbound_time = GETDATE(),
                updated_time = GETDATE()
            WHERE goods_id = @GoodsId AND (current_stock - @Qty) >= 0;

            IF @@ROWCOUNT = 0
            BEGIN
                SET @StatusCode = 2;
                SET @StatusMessage = N'并发扣减冲突：商品【' + @GoodsName + N'】库存不足！';
                CLOSE cur_sale;
                DEALLOCATE cur_sale;
                ROLLBACK TRANSACTION;
                RETURN;
            END

            -- 插入库存出库流水
            INSERT INTO dbo.wms_stock_log (goods_id, change_type, related_order_no, before_stock, change_qty, after_stock, operator_id, create_time)
            VALUES (@GoodsId, 'SALE_OUT', @OrderNo, @BeforeStock, -@Qty, @AfterStock, @WarehouseKeeperId, GETDATE());

            FETCH NEXT FROM cur_sale INTO @GoodsId, @Qty, @GoodsName;
        END

        CLOSE cur_sale;
        DEALLOCATE cur_sale;

        -- 3. 更新销售订单状态为已出库
        UPDATE dbo.sale_order
        SET order_status = 1,
            warehouse_keeper_id = @WarehouseKeeperId,
            outbound_date = GETDATE()
        WHERE order_id = @OrderId;

        COMMIT TRANSACTION;
        SET @StatusCode = 0;
        SET @StatusMessage = N'销售单出库成功！';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        IF CURSOR_STATUS('local', 'cur_details') >= 0
        BEGIN
            CLOSE cur_details;
            DEALLOCATE cur_details;
        END
        SET @StatusCode = 99;
        SET @StatusMessage = ERROR_MESSAGE();
    END CATCH
END;
GO


-- ===================================================================
-- 第六部分：核心统计视图 (供 Web 端 ECharts 仪表盘与预警使用)
-- ===================================================================

CREATE VIEW dbo.v_inventory_alert AS
SELECT 
    g.goods_id,
    g.goods_code,
    g.goods_name,
    c.category_name,
    g.specification,
    g.unit,
    ISNULL(i.current_stock, 0) AS current_stock,
    g.min_stock_alert,
    (g.min_stock_alert - ISNULL(i.current_stock, 0)) AS shortage_qty
FROM dbo.base_goods g
LEFT JOIN dbo.wms_inventory i ON g.goods_id = i.goods_id
LEFT JOIN dbo.base_category c ON g.category_id = c.category_id
WHERE ISNULL(i.current_stock, 0) <= g.min_stock_alert AND g.status = 1;
GO

CREATE VIEW dbo.v_top_selling_goods AS
SELECT 
    g.goods_id,
    g.goods_name,
    c.category_name,
    SUM(d.quantity) AS total_sale_qty,
    SUM(d.subtotal) AS total_sale_amount
FROM dbo.sale_order_detail d
INNER JOIN dbo.sale_order o ON d.order_id = o.order_id
INNER JOIN dbo.base_goods g ON d.goods_id = g.goods_id
LEFT JOIN dbo.base_category c ON g.category_id = c.category_id
WHERE o.order_status = 1
GROUP BY g.goods_id, g.goods_name, c.category_name;
GO


-- ===================================================================
-- 第七部分：初始化模拟与测试种子数据
-- ===================================================================

-- 7.1 初始化角色
INSERT INTO dbo.sys_role (role_code, role_name, description) VALUES
('ROLE_ADMIN', N'系统管理员', N'拥有系统全功能权限'),
('ROLE_PURCHASER', N'采购专员', N'负责供应商对接与采购订单制单'),
('ROLE_SALESPERSON', N'销售代表', N'负责客户维护与销售出库制单'),
('ROLE_KEEPER', N'仓管员', N'负责商品验收入库、出库复核及库存盘点');

-- 7.2 初始化用户 (密码均为 123456 的 SHA256 / MD5 模拟密文)
INSERT INTO dbo.sys_user (username, password_hash, real_name, phone, email, status) VALUES
('admin', 'e10adc3949ba59abbe56e057f20f883e', N'系统管理员', '13800000001', 'admin@nau.edu.cn', 1),
('purchaser01', 'e10adc3949ba59abbe56e057f20f883e', N'张采购', '13800000002', 'pur@nau.edu.cn', 1),
('sales01', 'e10adc3949ba59abbe56e057f20f883e', N'李销售', '13800000003', 'sale@nau.edu.cn', 1),
('keeper01', 'e10adc3949ba59abbe56e057f20f883e', N'王仓管', '13800000004', 'keeper@nau.edu.cn', 1);

-- 7.3 用户与角色绑定
INSERT INTO dbo.sys_user_role (user_id, role_id) VALUES
(1, 1), -- admin -> ROLE_ADMIN
(2, 2), -- purchaser01 -> ROLE_PURCHASER
(3, 3), -- sales01 -> ROLE_SALESPERSON
(4, 4); -- keeper01 -> ROLE_KEEPER

-- 7.4 商品分类
INSERT INTO dbo.base_category (parent_id, category_code, category_name, sort_order) VALUES
(NULL, 'CAT_DIGITAL', N'数码3C', 1),
(NULL, 'CAT_OFFICE', N'办公用品', 2),
(NULL, 'CAT_FOOD', N'食品饮料', 3);

-- 7.5 供应商
INSERT INTO dbo.base_supplier (supplier_code, supplier_name, contact_person, phone, address) VALUES
('SUP001', N'联想商用供应链有限公司', N'刘经理', '13911112222', N'北京市海淀区联想总部大厦'),
('SUP002', N'晨光文具股份有限公司', N'陈总监', '13933334444', N'上海市松江区文具工业园'),
('SUP003', N'农夫山泉销售总公司', N'黄主管', '13955556666', N'浙江省杭州市西湖区');

-- 7.6 客户
INSERT INTO dbo.base_customer (customer_code, customer_name, contact_person, phone, address, credit_limit) VALUES
('CUST001', N'南京审计大学后勤处', N'高老师', '13700001111', N'江苏省南京市浦口区雨山西路86号', 50000.00),
('CUST002', N'苏宁易购零售终端', N'周经理', '13700002222', N'江苏省南京市玄武区苏宁大道', 200000.00),
('CUST003', N'先锋书店', N'钱馆长', '13700003333', N'江苏省南京市鼓楼区广州路173号', 30000.00);

-- 7.7 商品主数据
INSERT INTO dbo.base_goods (goods_code, goods_name, category_id, barcode, specification, unit, cost_price, retail_price, min_stock_alert, max_stock_alert) VALUES
('G001', N'ThinkPad X1 商务笔记本', 1, '69010001', N'i7/16G/512G 黑色', N'台', 7500.00, 8999.00, 5, 50),
('G002', N'罗技 MX Master 3S 无线鼠标', 1, '69010002', N'静音版 深灰色', N'只', 450.00, 599.00, 10, 200),
('G003', N'晨光 A4 80g 复印纸', 2, '69020001', N'8包/箱 500张/包', N'箱', 120.00, 160.00, 20, 500),
('G004', N'得力 金属按动中性笔', 2, '69020002', N'0.5mm 黑色 12支装', N'盒', 18.00, 28.00, 15, 300),
('G005', N'农夫山泉 饮用天然水 550ml', 3, '69030001', N'24瓶/箱', N'箱', 22.00, 36.00, 30, 1000);

-- 7.8 初始库存
INSERT INTO dbo.wms_inventory (goods_id, current_stock, last_inbound_time) VALUES
(1, 15, GETDATE()), -- 笔记本 15台
(2, 30, GETDATE()), -- 鼠标 30只
(3, 8, GETDATE()),  -- 复印纸 8箱 (低于警戒线 20箱，触发预警!)
(4, 50, GETDATE()), -- 中性笔 50盒
(5, 100, GETDATE());-- 矿泉水 100箱

-- 7.9 写入初始化入库流水记录
INSERT INTO dbo.wms_stock_log (goods_id, change_type, related_order_no, before_stock, change_qty, after_stock, operator_id) VALUES
(1, 'INIT', 'INIT-20261017', 0, 15, 15, 1),
(2, 'INIT', 'INIT-20261017', 0, 30, 30, 1),
(3, 'INIT', 'INIT-20261017', 0, 8, 8, 1),
(4, 'INIT', 'INIT-20261017', 0, 50, 50, 1),
(5, 'INIT', 'INIT-20261017', 0, 100, 100, 1);
GO
