<template>
  <div class="dashboard-container">
    <!-- 顶部数据指标卡片 -->
    <el-row :gutter="16" class="metric-row">
      <el-col :span="6">
        <el-card shadow="hover" class="metric-card sales-card">
          <div class="metric-content">
            <div class="metric-label">销售总营业额</div>
            <div class="metric-value">¥ {{ (stats.totalSaleAmount || 0).toLocaleString() }}</div>
            <div class="metric-sub">累计已完成出库单据</div>
          </div>
          <div class="metric-icon-wrap sales-icon">
            <el-icon><Money /></el-icon>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="metric-card purchase-card">
          <div class="metric-content">
            <div class="metric-label">采购总支出</div>
            <div class="metric-value">¥ {{ (stats.totalPurchaseAmount || 0).toLocaleString() }}</div>
            <div class="metric-sub">累计已验收入库单据</div>
          </div>
          <div class="metric-icon-wrap purchase-icon">
            <el-icon><ShoppingBag /></el-icon>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="metric-card goods-card">
          <div class="metric-content">
            <div class="metric-label">商品主数据种类</div>
            <div class="metric-value">{{ stats.totalGoodsCount || 0 }} <span class="unit">种</span></div>
            <div class="metric-sub">在售状态商品条目</div>
          </div>
          <div class="metric-icon-wrap goods-icon">
            <el-icon><Goods /></el-icon>
          </div>
        </el-card>
      </el-col>

      <el-col :span="6">
        <el-card shadow="hover" class="metric-card alert-card">
          <div class="metric-content">
            <div class="metric-label">库存不足预警</div>
            <div class="metric-value alert-text">{{ stats.stockAlertCount || 0 }} <span class="unit">项</span></div>
            <div class="metric-sub">低于最低安全库存线</div>
          </div>
          <div class="metric-icon-wrap alert-icon">
            <el-icon><WarningFilled /></el-icon>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 中间图表与近期单据 -->
    <el-row :gutter="16" class="chart-row">
      <el-col :span="14">
        <el-card shadow="hover" class="chart-card">
          <template #header>
            <div class="card-header">
              <span>📊 畅销商品销量排行 TOP 5 (ECharts)</span>
              <el-tag size="small" type="success">实时统计</el-tag>
            </div>
          </template>
          <div ref="chartRef" class="echarts-box"></div>
        </el-card>
      </el-col>

      <el-col :span="10">
        <el-card shadow="hover" class="chart-card">
          <template #header>
            <div class="card-header">
              <span>📑 最近进销业务动态</span>
              <el-button link type="primary" size="small" @click="fetchData">刷新</el-button>
            </div>
          </template>
          <el-table :data="stats.recentOrders" stripe style="width: 100%" size="small">
            <el-table-column prop="orderNo" label="单号" width="130" />
            <el-table-column prop="type" label="类型" width="60">
              <template #default="{ row }">
                <el-tag size="small" :type="row.type === '销售' ? 'success' : 'primary'">{{ row.type }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="partnerName" label="往来单位" show-overflow-tooltip />
            <el-table-column prop="amount" label="金额" width="90">
              <template #default="{ row }">¥{{ row.amount }}</template>
            </el-table-column>
            <el-table-column prop="statusName" label="状态" width="70">
              <template #default="{ row }">
                <el-tag size="small" effect="plain">{{ row.statusName }}</el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>

    <!-- 底部：库存紧缺预警专区 -->
    <el-card shadow="hover" class="alert-table-card">
      <template #header>
        <div class="card-header">
          <span style="color: #f56c6c; font-weight: bold;">
            <el-icon><WarningFilled /></el-icon> 实时紧缺商品告警列表（触发数据库预警阈值）
          </span>
          <el-button type="primary" size="small" @click="$router.push('/purchase')">
            一键去采购补货
          </el-button>
        </div>
      </template>

      <el-table :data="alertList" stripe style="width: 100%" v-loading="loading">
        <el-table-column prop="goodsCode" label="商品编码" width="110" />
        <el-table-column prop="goodsName" label="商品名称" min-width="150" />
        <el-table-column prop="categoryName" label="分类" width="100" />
        <el-table-column prop="specification" label="规格" width="130" />
        <el-table-column prop="currentStock" label="当前现存量" width="110">
          <template #default="{ row }">
            <el-tag type="danger" effect="dark">{{ row.currentStock }} {{ row.unit }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="minStockAlert" label="最低安全预警值" width="130">
          <template #default="{ row }">{{ row.minStockAlert }} {{ row.unit }}</template>
        </el-table-column>
        <el-table-column prop="shortageQty" label="当前缺口量" width="110">
          <template #default="{ row }">
            <span style="color: #f56c6c; font-weight: bold;">-{{ row.shortageQty }} {{ row.unit }}</span>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted, nextTick } from 'vue'
import * as echarts from 'echarts'
import { getDashboardStatsApi, getStockAlertsApi } from '../api/psi'

const stats = ref({})
const alertList = ref([])
const loading = ref(false)
const chartRef = ref(null)
let myChart = null

const initChart = () => {
  if (!chartRef.value) return
  if (!myChart) {
    myChart = echarts.init(chartRef.value)
  }

  const items = stats.value.topSellingGoods || []
  const names = items.map(x => x.goodsName).reverse()
  const qtys = items.map(x => x.saleQty).reverse()

  const option = {
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: { type: 'value', boundaryGap: [0, 0.01] },
    yAxis: { type: 'category', data: names.length ? names : ['ThinkPad X1', 'MX Master鼠标', 'A4复印纸', '中性笔', '矿泉水'] },
    series: [
      {
        name: '累计销售数量',
        type: 'bar',
        data: qtys.length ? qtys : [15, 28, 42, 60, 120],
        itemStyle: {
          color: new echarts.graphic.LinearGradient(1, 0, 0, 0, [
            { offset: 0, color: '#409eff' },
            { offset: 1, color: '#79bbff' }
          ]),
          borderRadius: [0, 4, 4, 0]
        }
      }
    ]
  }
  myChart.setOption(option)
}

const fetchData = async () => {
  loading.value = true
  try {
    const [resStats, resAlerts] = await Promise.all([
      getDashboardStatsApi(),
      getStockAlertsApi()
    ])
    if (resStats.code === 200) stats.value = resStats.data
    if (resAlerts.code === 200) alertList.value = resAlerts.data

    await nextTick()
    initChart()
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchData()
  window.addEventListener('resize', () => myChart && myChart.resize())
})
</script>

<style scoped>
.metric-row {
  margin-bottom: 16px;
}

.metric-card {
  position: relative;
  overflow: hidden;
  border-radius: 8px;
}

.metric-content {
  position: relative;
  z-index: 1;
}

.metric-label {
  font-size: 13px;
  color: #8c8c8c;
  margin-bottom: 8px;
}

.metric-value {
  font-size: 24px;
  font-weight: 700;
  color: #262626;
  margin-bottom: 4px;
}

.metric-value .unit {
  font-size: 14px;
  font-weight: normal;
  color: #8c8c8c;
}

.metric-sub {
  font-size: 11px;
  color: #bfbfbf;
}

.alert-text {
  color: #f56c6c;
}

.metric-icon-wrap {
  position: absolute;
  right: 16px;
  top: 50%;
  transform: translateY(-50%);
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.sales-icon { background: #e8f4ff; color: #409eff; }
.purchase-icon { background: #f0f9eb; color: #67c23a; }
.goods-icon { background: #fdf6ec; color: #e6a23c; }
.alert-icon { background: #fef0f0; color: #f56c6c; }

.chart-row {
  margin-bottom: 16px;
}

.chart-card {
  border-radius: 8px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-weight: 600;
  font-size: 14px;
}

.echarts-box {
  height: 280px;
  width: 100%;
}

.alert-table-card {
  border-radius: 8px;
}
</style>
