<template>
  <div class="purchase-container">
    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <div class="title">
          <h3>📦 采购订单管理与验收入库</h3>
        </div>
        <el-button type="primary" icon="Plus" @click="openCreateDialog">
          新建采购入库单
        </el-button>
      </div>

      <el-table :data="tableData" v-loading="loading" stripe style="width: 100%">
        <el-table-column prop="orderNo" label="采购单号" width="160" />
        <el-table-column prop="supplierName" label="供应商名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="purchaserName" label="采购制单人" width="110" />
        <el-table-column prop="totalAmount" label="订单总金额" width="120">
          <template #default="{ row }">
            <strong style="color: #409eff;">¥{{ row.totalAmount.toLocaleString() }}</strong>
          </template>
        </el-table-column>
        <el-table-column prop="statusName" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.orderStatus === 1 ? 'success' : 'warning'" effect="dark">
              {{ row.statusName }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="keeperName" label="验收仓管员" width="110" />
        <el-table-column prop="orderDate" label="制单时间" width="160">
          <template #default="{ row }">{{ formatTime(row.orderDate) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="170" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="openDetailDialog(row.orderId)">查看明细</el-button>
            <el-button
              v-if="row.orderStatus === 0"
              link
              type="success"
              size="small"
              @click="handleInbound(row)"
            >
              验收入库
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-bar">
        <el-pagination
          v-model:current-page="pageIndex"
          v-model:page-size="pageSize"
          layout="total, prev, pager, next"
          :total="total"
          @current-change="fetchList"
        />
      </div>
    </el-card>

    <!-- 新建采购单弹窗 -->
    <el-dialog v-model="createVisible" title="创建新采购订单" width="700px" destroy-on-close>
      <el-form label-width="90px">
        <el-form-item label="选择供应商" required>
          <el-select v-model="formOrder.supplierId" placeholder="请选择供货商" style="width: 100%">
            <el-option
              v-for="s in suppliers"
              :key="s.supplierId"
              :label="s.supplierName + ' (' + s.contactPerson + ')'"
              :value="s.supplierId"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="单据备注">
          <el-input v-model="formOrder.remark" placeholder="采购补充说明" />
        </el-form-item>

        <div class="detail-header">
          <span><strong>采购商品清单明细：</strong></span>
          <el-button type="primary" size="small" link icon="Plus" @click="addItemRow">添加一件商品</el-button>
        </div>

        <el-table :data="formOrder.items" border size="small" style="margin-top: 8px">
          <el-table-column label="选择商品" min-width="180">
            <template #default="{ row }">
              <el-select v-model="row.goodsId" placeholder="选择商品" @change="onGoodsSelect(row)">
                <el-option
                  v-for="g in goodsList"
                  :key="g.goodsId"
                  :label="g.goodsName"
                  :value="g.goodsId"
                />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="单价(¥)" width="120">
            <template #default="{ row }">
              <el-input-number v-model="row.price" :min="0" :precision="2" size="small" style="width: 100%" />
            </template>
          </el-table-column>
          <el-table-column label="数量" width="110">
            <template #default="{ row }">
              <el-input-number v-model="row.quantity" :min="1" size="small" style="width: 100%" />
            </template>
          </el-table-column>
          <el-table-column label="小计(¥)" width="100">
            <template #default="{ row }">
              <strong>¥{{ (row.price * row.quantity).toFixed(2) }}</strong>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="60">
            <template #default="{ $index }">
              <el-button link type="danger" icon="Delete" @click="formOrder.items.splice($index, 1)" />
            </template>
          </el-table-column>
        </el-table>

        <div class="total-bar">
          订单预计总金额：<span class="total-text">¥ {{ calcTotalAmount.toFixed(2) }}</span>
        </div>
      </el-form>

      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="submitCreate">提交制单</el-button>
      </template>
    </el-dialog>

    <!-- 订单明细详情弹窗 -->
    <el-dialog v-model="detailVisible" title="采购单详情明细" width="650px">
      <el-descriptions border :column="2" v-if="orderDetail.order">
        <el-descriptions-item label="单号">{{ orderDetail.order.orderNo }}</el-descriptions-item>
        <el-descriptions-item label="供应商">{{ orderDetail.order.supplierName }}</el-descriptions-item>
        <el-descriptions-item label="采购员">{{ orderDetail.order.purchaserName }}</el-descriptions-item>
        <el-descriptions-item label="总金额">¥{{ orderDetail.order.totalAmount }}</el-descriptions-item>
      </el-descriptions>

      <el-table :data="orderDetail.details" stripe size="small" style="margin-top: 14px">
        <el-table-column prop="goodsName" label="商品名称" />
        <el-table-column prop="specification" label="规格" width="120" />
        <el-table-column prop="purchasePrice" label="进价" width="90">
          <template #default="{ row }">¥{{ row.purchasePrice }}</template>
        </el-table-column>
        <el-table-column prop="quantity" label="数量" width="80">
          <template #default="{ row }">{{ row.quantity }} {{ row.unit }}</template>
        </el-table-column>
        <el-table-column prop="subtotal" label="小计" width="90">
          <template #default="{ row }">¥{{ row.subtotal }}</template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  getPurchaseOrdersApi,
  getPurchaseDetailsApi,
  createPurchaseOrderApi,
  executeInboundApi,
  getSuppliersApi,
  getGoodsListApi
} from '../api/psi'

const loading = ref(false)
const submitLoading = ref(false)
const tableData = ref([])
const total = ref(0)
const pageIndex = ref(1)
const pageSize = ref(10)

const suppliers = ref([])
const goodsList = ref([])

const createVisible = ref(false)
const formOrder = reactive({
  supplierId: null,
  remark: '',
  items: []
})

const detailVisible = ref(false)
const orderDetail = ref({ order: null, details: [] })

const calcTotalAmount = computed(() => {
  return formOrder.items.reduce((sum, item) => sum + (item.price * item.quantity), 0)
})

const formatTime = (t) => t ? t.replace('T', ' ').substring(0, 16) : '-'

const fetchList = async () => {
  loading.value = true
  try {
    const res = await getPurchaseOrdersApi({ pageIndex: pageIndex.value, pageSize: pageSize.value })
    if (res.code === 200) {
      tableData.value = res.data.items
      total.value = res.data.total
    }
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

const openCreateDialog = async () => {
  const [resSup, resGoods] = await Promise.all([
    getSuppliersApi(),
    getGoodsListApi({ pageIndex: 1, pageSize: 100 })
  ])
  if (resSup.code === 200) suppliers.value = resSup.data
  if (resGoods.code === 200) goodsList.value = resGoods.data.items

  formOrder.supplierId = suppliers.value[0]?.supplierId || null
  formOrder.remark = ''
  formOrder.items = [{ goodsId: goodsList.value[0]?.goodsId || null, price: goodsList.value[0]?.costPrice || 0, quantity: 10 }]
  createVisible.value = true
}

const addItemRow = () => {
  const first = goodsList.value[0]
  formOrder.items.push({
    goodsId: first ? first.goodsId : null,
    price: first ? first.costPrice : 0,
    quantity: 1
  })
}

const onGoodsSelect = (row) => {
  const target = goodsList.value.find(g => g.goodsId === row.goodsId)
  if (target) row.price = target.costPrice
}

const submitCreate = async () => {
  if (!formOrder.supplierId) return ElMessage.warning('请选择供应商')
  if (formOrder.items.length === 0) return ElMessage.warning('商品清单不能为空')

  submitLoading.value = true
  try {
    const res = await createPurchaseOrderApi(formOrder)
    if (res.code === 200) {
      ElMessage.success(res.message || '制单成功')
      createVisible.value = false
      fetchList()
    }
  } finally {
    submitLoading.value = false
  }
}

const handleInbound = (row) => {
  ElMessageBox.confirm(
    `确认对采购单【${row.orderNo}】进行验收入库吗？系统将自动在数据库事务中累加现存库存，并生成入库流水记录。`,
    '验收入库确认（触发数据库事务）',
    { type: 'warning' }
  ).then(async () => {
    const res = await executeInboundApi(row.orderId)
    if (res.code === 200) {
      ElMessage.success(res.message || '验收入库成功！')
      fetchList()
    }
  })
}

const openDetailDialog = async (id) => {
  const res = await getPurchaseDetailsApi(id)
  if (res.code === 200) {
    orderDetail.value = res.data
    detailVisible.value = true
  }
}

onMounted(fetchList)
</script>

<style scoped>
.table-card { border-radius: 8px; }
.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.toolbar h3 { margin: 0; font-size: 16px; color: #1a1a1a; }
.pagination-bar { display: flex; justify-content: flex-end; margin-top: 16px; }
.detail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 10px;
}
.total-bar {
  text-align: right;
  margin-top: 14px;
  font-size: 14px;
  color: #606266;
}
.total-text {
  font-size: 18px;
  color: #409eff;
  font-weight: bold;
}
</style>
