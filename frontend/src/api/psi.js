import request from './request'

export const loginApi = (data) => request.post('/auth/login', data)
export const getUserInfoApi = () => request.get('/auth/info')

// 首页看板
export const getDashboardStatsApi = () => request.get('/dashboard/stats')
export const getStockAlertsApi = () => request.get('/dashboard/stock-alerts')

// 商品管理
export const getGoodsListApi = (params) => request.get('/goods/list', { params })
export const getCategoriesApi = () => request.get('/goods/categories')
export const saveGoodsApi = (data) => request.post('/goods/save', data)
export const deleteGoodsApi = (id) => request.delete(`/goods/${id}`)

// 供应商与客户
export const getSuppliersApi = () => request.get('/partner/suppliers')
export const getCustomersApi = () => request.get('/partner/customers')

// 采购
export const getPurchaseOrdersApi = (params) => request.get('/purchase/orders', { params })
export const getPurchaseDetailsApi = (id) => request.get(`/purchase/orders/${id}`)
export const createPurchaseOrderApi = (data) => request.post('/purchase/create', data)
export const executeInboundApi = (id) => request.post(`/purchase/inbound/${id}`)

// 销售
export const getSaleOrdersApi = (params) => request.get('/sale/orders', { params })
export const getSaleDetailsApi = (id) => request.get(`/sale/orders/${id}`)
export const createSaleOrderApi = (data) => request.post('/sale/create', data)
export const executeOutboundApi = (id) => request.post(`/sale/outbound/${id}`)

// 库存与流水
export const getStockOverviewApi = (params) => request.get('/inventory/stock', { params })
export const getStockLogsApi = (params) => request.get('/inventory/logs', { params })
