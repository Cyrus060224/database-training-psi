<template>
  <div class="inventory-container">
    <el-card shadow="never" class="table-card">
      <el-tabs v-model="activeTab" type="card" @tab-change="onTabChange">
        <!-- Tab 1: 实时库存现存量一览 -->
        <el-tab-pane label="📊 实时库存现存量与安全预警" name="stock">
          <div class="search-bar">
            <el-input
              v-model="stockKeyword"
              placeholder="搜索商品名称 / 编码"
              clearable
              style="width: 260px"
              @keyup.enter="fetchStock"
            />
            <el-button type="primary" icon="Search" @click="fetchStock">查询</el-button>
          </div>

          <el-table :data="stockList" v-loading="stockLoading" stripe style="width: 100%">
            <el-table-column prop="goodsCode" label="商品编码" width="130" />
            <el-table-column prop="goodsName" label="商品名称" min-width="160" show-overflow-tooltip />
            <el-table-column prop="categoryName" label="类别" width="110">
              <template #default="{ row }">
                <el-tag size="small">{{ row.categoryName }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="specification" label="规格型号" width="140" show-overflow-tooltip />
            <el-table-column prop="currentStock" label="现存数量" width="130">
              <template #default="{ row }">
                <el-badge :is-dot="row.isAlert" class="item">
                  <el-tag :type="row.isAlert ? 'danger' : 'success'" effect="dark">
                    {{ row.currentStock }} {{ row.unit }}
                  </el-tag>
                </el-badge>
              </template>
            </el-table-column>
            <el-table-column prop="minStockAlert" label="最低安全阈值" width="120">
              <template #default="{ row }">
                {{ row.minStockAlert }} {{ row.unit }}
              </template>
            </el-table-column>
            <el-table-column prop="lastInboundTime" label="最近入库时间" width="160">
              <template #default="{ row }">{{ formatTime(row.lastInboundTime) }}</template>
            </el-table-column>
            <el-table-column prop="lastOutboundTime" label="最近出库时间" width="160">
              <template #default="{ row }">{{ formatTime(row.lastOutboundTime) }}</template>
            </el-table-column>
          </el-table>

          <div class="pagination-bar">
            <el-pagination
              v-model:current-page="stockPage"
              v-model:page-size="stockPageSize"
              layout="total, prev, pager, next"
              :total="stockTotal"
              @current-change="fetchStock"
            />
          </div>
        </el-tab-pane>

        <!-- Tab 2: 库存变动审计流水台账 -->
        <el-tab-pane label="📝 库存出入库审计流水台账" name="logs">
          <div class="audit-tip">
            <el-alert
              title="进销存审计溯源：每一次采购入库、销售出库均会自动生成不可篡改的流水快照，精确记录变动前、变动量、变动后结存及经手操作人。"
              type="success"
              :closable="false"
              show-icon
              style="margin-bottom: 12px"
            />
          </div>

          <el-table :data="logList" v-loading="logLoading" stripe size="small" style="width: 100%">
            <el-table-column prop="logId" label="流水号" width="90" />
            <el-table-column prop="goodsName" label="变动商品" min-width="150" show-overflow-tooltip />
            <el-table-column prop="changeTypeName" label="业务类型" width="130">
              <template #default="{ row }">
                <el-tag
                  size="small"
                  :type="row.changeType === 'PUR_IN' ? 'primary' : (row.changeType === 'SALE_OUT' ? 'success' : 'info')"
                >
                  {{ row.changeTypeName }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="relatedOrderNo" label="关联单据编号" width="160" />
            <el-table-column prop="beforeStock" label="变动前库存" width="100" />
            <el-table-column prop="changeQty" label="变动数量" width="100">
              <template #default="{ row }">
                <strong :style="{ color: row.changeQty > 0 ? '#409eff' : '#f56c6c' }">
                  {{ row.changeQty > 0 ? '+' + row.changeQty : row.changeQty }}
                </strong>
              </template>
            </el-table-column>
            <el-table-column prop="afterStock" label="变动后结存" width="100">
              <template #default="{ row }">
                <strong>{{ row.afterStock }}</strong>
              </template>
            </el-table-column>
            <el-table-column prop="operatorName" label="经办操作人" width="110" />
            <el-table-column prop="createTime" label="审计发生时间" width="160">
              <template #default="{ row }">{{ formatTime(row.createTime) }}</template>
            </el-table-column>
          </el-table>

          <div class="pagination-bar">
            <el-pagination
              v-model:current-page="logPage"
              v-model:page-size="logPageSize"
              layout="total, prev, pager, next"
              :total="logTotal"
              @current-change="fetchLogs"
            />
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getStockOverviewApi, getStockLogsApi } from '../api/psi'

const activeTab = ref('stock')

// Tab 1 数据
const stockList = ref([])
const stockLoading = ref(false)
const stockTotal = ref(0)
const stockPage = ref(1)
const stockPageSize = ref(10)
const stockKeyword = ref('')

// Tab 2 数据
const logList = ref([])
const logLoading = ref(false)
const logTotal = ref(0)
const logPage = ref(1)
const logPageSize = ref(15)

const formatTime = (t) => t ? t.replace('T', ' ').substring(0, 16) : '-'

const fetchStock = async () => {
  stockLoading.value = true
  try {
    const res = await getStockOverviewApi({
      keyword: stockKeyword.value,
      pageIndex: stockPage.value,
      pageSize: stockPageSize.value
    })
    if (res.code === 200) {
      stockList.value = res.data.items
      stockTotal.value = res.data.total
    }
  } finally {
    stockLoading.value = false
  }
}

const fetchLogs = async () => {
  logLoading.value = true
  try {
    const res = await getStockLogsApi({
      pageIndex: logPage.value,
      pageSize: logPageSize.value
    })
    if (res.code === 200) {
      logList.value = res.data.items
      logTotal.value = res.data.total
    }
  } finally {
    logLoading.value = false
  }
}

const onTabChange = (name) => {
  if (name === 'stock') fetchStock()
  if (name === 'logs') fetchLogs()
}

onMounted(() => {
  fetchStock()
})
</script>

<style scoped>
.table-card { border-radius: 8px; }
.search-bar { display: flex; gap: 12px; margin-bottom: 14px; }
.pagination-bar { display: flex; justify-content: flex-end; margin-top: 16px; }
</style>
