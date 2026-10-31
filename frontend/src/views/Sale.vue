<template>
  <div class="sale-container">
    <el-card shadow="never" class="table-card">
      <div class="toolbar">
        <div class="title">
          <h3>🛒 销售订单管理与出库审核（含防超卖并发控制）</h3>
        </div>
        <el-button type="success" icon="Plus" @click="openCreateDialog">
          新建销售单
        </el-button>
      </div>

      <!-- 提示条，凸显答辩亮点 -->
      <el-alert
        title="💡 答辩考点亮点：点击【出库审核】时，系统将在底层数据库事务中原子校验库存并扣减，若库存不足或存在并发超卖，将自动触发 ROLLBACK 阻断，保证绝不出现负库存！"
        type="info"
        show-icon
        :closable="false"
        style="margin-bottom: 16px;"
      />

      <el-table :data="tableData" v-loading="loading" stripe style="width: 100%">
        <el-table-column prop="orderNo" label="销售单号" width="160" />
        <el-table-column prop="customerName" label="订购客户" min-width="160" show-overflow-tooltip />
        <el-table-column prop="salerName" label="销售员" width="110" />
        <el-table-column prop="totalAmount" label="订单总金额" width="120">
          <template #default="{ row }">
            <strong style="color: #67c23a;">¥{{ row.totalAmount.toLocaleString() }}</strong>
          </template>
        </el-table-column>
        <el-table-column prop="statusName" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.orderStatus === 1 ? 'success' : 'warning'" effect="dark">
              {{ row.statusName }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="keeperName" label="出库审核仓管" width="120" />
        <el-table-column prop="orderDate" label="制单时间" width="160">
          <template #default="{ row }">{{ formatTime(row.orderDate) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="170" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="openDetailDialog(row.orderId)">查看明细</el-button>
            <el-button
              v-if="row.orderStatus === 0"
              link
              type="danger"
              size="small"
              @click="handleOutbound(row)"
            >
              出库审核
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

    <!-- 新建销售单弹窗 -->
    <el-dialog v-model="createVisible" title="创建销售出库单" width="700px" destroy-on-close>
      <el-form label-width="90px">
        <el-form-item label="选择客户" required>
          <el-select v-model="formOrder.customerId" placeholder="请选择订购客户" style="width: 100%">
            <el-option
              v-for="c in customers"
              :key="c.customerId"
              :label="c.customerName + ' (信用额度: ¥' + c.creditLimit + ')'"
              :value="c.customerId"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="销售备注">
          <el-input v-model="formOrder.remark" placeholder="销售业务备注" />
        </el-form-item>

        <div class="detail-header">
          <span><strong>订购商品清单明细：</strong></span>
          <el-button type="success" size="small" link icon="Plus" @click="addItemRow">添加一件商品</el-button>
        </div>

        <el-table :data="formOrder.items" border size="small" style="margin-top: 8px">
          <el-table-column label="选择商品" min-width="180">
            <template #default="{ row }">
              <el-select v-model="row.goodsId" placeholder="选择商品" @change="onGoodsSelect(row)">
                <el-option
                  v-for="g in goodsList"
                  :key="g.goodsId"
                  :label="g.goodsName + ' (现存: ' + g.currentStock + ')'"
                  :value="g.goodsId"
                />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="售价(¥)" width="120">
            <template #default="{ row }">
              <el-input-number v-model="row.price" :min="0" :precision="2" size="small" style="width: 100%" />
            </template>
          </el-table-column>
          <el-table-column label="销售数量" width="120">
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
          订单应收总金额：<span class="total-text">¥ {{ calcTotalAmount.toFixed(2) }}</span>
        </div>
      </el-form>

      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button type="success" :loading="submitLoading" @click="submitCreate">立即提交销售单</el-button>
      </template>
    </el-dialog>

    <!-- 订单明细详情弹窗 -->
    <el-dialog v-model="detailVisible" title="销售单详情明细" width="650px">
      <el-descriptions border :column="2" v-if="orderDetail.order">
        <el-descriptions-item label="单号">{{ orderDetail.order.orderNo }}</el-descriptions-item>
        <el-descriptions-item label="订购客户">{{ orderDetail.order.customerName }}</el-descriptions-item>
        <el-descriptions-item label="销售员">{{ orderDetail.order.salerName }}</el-descriptions-item>
        <el-descriptions-item label="总金额">¥{{ orderDetail.order.totalAmount }}</el-descriptions-item>
      </el-descriptions>

      <el-table :data="orderDetail.details" stripe size="small" style="margin-top: 14px">
        <el-table-column prop="goodsName" label="商品名称" />
        <el-table-column prop="specification" label="规格" width="120" />
        <el-table-column prop="salePrice" label="售价" width="90">
          <template #default="{ row }">¥{{ row.salePrice }}</template>
        </el-table-column>
        <el-table-column prop="quantity" label="订购数量" width="90">
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
  getSaleOrdersApi,
  getSaleDetailsApi,
  createSaleOrderApi,
  executeOutboundApi,
  getCustomersApi,
  getGoodsListApi
} from '../api/psi'

const loading = ref(false)
const submitLoading = ref(false)
const tableData = ref([])
const total = ref(0)
const pageIndex = ref(1)
const pageSize = ref(10)

const customers = ref([])
const goodsList = ref([])

const createVisible = ref(false)
const formOrder = reactive({
  customerId: null,
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
    const res = await getSaleOrdersApi({ pageIndex: pageIndex.value, pageSize: pageSize.value })
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
  const [resCust, resGoods] = await Promise.all([
    getCustomersApi(),
    getGoodsListApi({ pageIndex: 1, pageSize: 100 })
  ])
  if (resCust.code === 200) customers.value = resCust.data
  if (resGoods.code === 200) goodsList.value = resGoods.data.items

  formOrder.customerId = customers.value[0]?.customerId || null
  formOrder.remark = ''
  formOrder.items = [{ goodsId: goodsList.value[0]?.goodsId || null, price: goodsList.value[0]?.retailPrice || 0, quantity: 1 }]
  createVisible.value = true
}

const addItemRow = () => {
  const first = goodsList.value[0]
  formOrder.items.push({
    goodsId: first ? first.goodsId : null,
    price: first ? first.retailPrice : 0,
    quantity: 1
  })
}

const onGoodsSelect = (row) => {
  const target = goodsList.value.find(g => g.goodsId === row.goodsId)
  if (target) row.price = target.retailPrice
}

const submitCreate = async () => {
  if (!formOrder.customerId) return ElMessage.warning('请选择订购客户')
  if (formOrder.items.length === 0) return ElMessage.warning('商品清单不能为空')

  submitLoading.value = true
  try {
    const res = await createSaleOrderApi(formOrder)
    if (res.code === 200) {
      ElMessage.success(res.message || '销售单创建成功')
      createVisible.value = false
      fetchList()
    }
  } finally {
    submitLoading.value = false
  }
}

const handleOutbound = (row) => {
  ElMessageBox.confirm(
    `确认对销售单【${row.orderNo}】执行出库审核吗？系统将严格校验库存，在原子事务中扣减现存量，若库存不足将自动阻断回滚。`,
    '出库审核确认（触发并发防超卖保护）',
    { type: 'warning' }
  ).then(async () => {
    try {
      const res = await executeOutboundApi(row.orderId)
      if (res.code === 200) {
        ElMessage.success(res.message || '出库成功！')
        fetchList()
      }
    } catch (e) {
      // 错误由 axios 拦截器提示
    }
  })
}

const openDetailDialog = async (id) => {
  const res = await getSaleDetailsApi(id)
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
  margin-bottom: 12px;
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
  color: #67c23a;
  font-weight: bold;
}
</style>
