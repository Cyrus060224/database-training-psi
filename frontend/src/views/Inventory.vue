<template>
  <div class="glass-page-container">
    <div class="glass-card page-card">
      <el-tabs v-model="activeTab" class="apple-tabs" @tab-change="onTabChange">
        <!-- Tab 1: 实时库存现存量一览 -->
        <el-tab-pane label="📊 实时库存动态一览" name="stock">
          <div class="tab-toolbar">
            <div class="search-bar">
              <el-input
                v-model="stockKeyword"
                placeholder="搜索商品名称 / 编码"
                clearable
                style="width: 260px"
                prefix-icon="Search"
                @keyup.enter="fetchStock"
              />
              <el-button type="primary" class="glass-action-btn" @click="fetchStock">查询</el-button>
            </div>
            <div class="right-tip">
              <span class="pulse-mini-dot"></span>
              <span style="font-size: 13px; color: #64748b;">实时锁定行级数据校验</span>
            </div>
          </div>

          <el-table :data="stockList" v-loading="stockLoading" stripe style="width: 100%">
            <el-table-column prop="goodsCode" label="商品编码" width="130" />
            <el-table-column prop="goodsName" label="商品名称" min-width="170" show-overflow-tooltip>
              <template #default="{ row }">
                <strong>{{ row.goodsName }}</strong>
              </template>
            </el-table-column>
            <el-table-column prop="categoryName" label="类别" width="110">
              <template #default="{ row }">
                <span class="category-pill">{{ row.categoryName }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="specification" label="规格型号" width="140" show-overflow-tooltip />

            <!-- 现存与健康度指示条 -->
            <el-table-column label="库存健康度 / 现存数量" min-width="210">
              <template #default="{ row }">
                <div class="stock-progress-wrap">
                  <el-progress
                    :percentage="Math.min(Math.round((row.currentStock / row.minStockAlert) * 100), 100)"
                    :status="row.isAlert ? 'exception' : 'success'"
                    :stroke-width="8"
                    :show-text="false"
                  />
                  <div class="stock-status-row">
                    <span :style="{ color: row.isAlert ? '#ef4444' : '#059669', fontWeight: 600 }">
                      {{ row.currentStock }} {{ row.unit }}
                    </span>
                    <span style="color: #94a3b8; font-size: 11px;">安全预警线: {{ row.minStockAlert }}</span>
                  </div>
                </div>
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
        <el-tab-pane label="📝 进销存出入库审计流水台账" name="logs">
          <div class="audit-banner">
            <el-icon style="font-size: 18px; color: #3b82f6;"><DocumentChecked /></el-icon>
            <span>不可篡改全生命周期台账：精准记录业务类型、单号凭证、变动前库存、变动数量、变动后结存及经办人。</span>
          </div>

          <el-table :data="logList" v-loading="logLoading" stripe style="width: 100%; margin-top: 14px;">
            <el-table-column prop="logId" label="流水号" width="90" />
            <el-table-column prop="goodsName" label="变动商品名称" min-width="160" show-overflow-tooltip>
              <template #default="{ row }">
                <strong>{{ row.goodsName }}</strong>
              </template>
            </el-table-column>
            <el-table-column prop="changeTypeName" label="业务类型" width="140">
              <template #default="{ row }">
                <el-tag
                  size="small"
                  :type="row.changeType === 'PUR_IN' ? 'primary' : (row.changeType === 'SALE_OUT' ? 'success' : 'info')"
                  effect="light"
                >
                  {{ row.changeTypeName }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="relatedOrderNo" label="关联业务单据" width="170" />
            <el-table-column prop="beforeStock" label="变动前库存" width="110" />
            <el-table-column prop="changeQty" label="变动量" width="110">
              <template #default="{ row }">
                <strong :style="{ color: row.changeQty > 0 ? '#2563eb' : '#dc2626' }">
                  {{ row.changeQty > 0 ? '+' + row.changeQty : row.changeQty }}
                </strong>
              </template>
            </el-table-column>
            <el-table-column prop="afterStock" label="变动后结存" width="110">
              <template #default="{ row }">
                <strong>{{ row.afterStock }}</strong>
              </template>
            </el-table-column>
            <el-table-column prop="operatorName" label="经办操作人" width="120" />
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
    </div>
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
.glass-page-container {
  display: flex;
  flex-direction: column;
}

.page-card {
  padding: 24px;
}

.tab-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.search-bar {
  display: flex;
  gap: 12px;
}

.right-tip {
  display: flex;
  align-items: center;
  gap: 8px;
}

.pulse-mini-dot {
  width: 8px;
  height: 8px;
  background-color: #10b981;
  border-radius: 50%;
  animation: pulse-ring 2s infinite;
}

@keyframes pulse-ring {
  0% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.6); }
  70% { box-shadow: 0 0 0 6px rgba(16, 185, 129, 0); }
  100% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
}

.category-pill {
  background: rgba(241, 245, 249, 0.9);
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 12px;
  color: #475569;
}

.stock-progress-wrap {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.stock-status-row {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
}

.audit-banner {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  background: rgba(239, 246, 255, 0.7);
  border: 1px solid rgba(191, 219, 254, 0.8);
  border-radius: 12px;
  font-size: 13px;
  color: #1e40af;
}

.pagination-bar {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}
</style>
