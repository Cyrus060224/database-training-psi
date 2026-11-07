<template>
  <div class="glass-dashboard">
    <!-- 顶部莫兰迪微渐变毛玻璃 KPI 卡片 -->
    <div class="metrics-grid">
      <!-- 销售额卡片 -->
      <div class="kpi-glass-card kpi-sales">
        <div class="kpi-info">
          <div class="kpi-label">销售总营业额</div>
          <div class="kpi-num">
            <span class="currency-symbol">¥</span>{{ (stats.totalSaleAmount || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
          </div>
          <div class="kpi-footer">
            <span class="trend-badge trend-up">
              <el-icon><TopRight /></el-icon> 实时入账
            </span>
            <span class="footer-desc">已结算出库单据</span>
          </div>
        </div>
        <div class="kpi-icon-pill icon-sales">
          <el-icon><Money /></el-icon>
        </div>
      </div>

      <!-- 采购支出卡片 -->
      <div class="kpi-glass-card kpi-purchase">
        <div class="kpi-info">
          <div class="kpi-label">采购总支出</div>
          <div class="kpi-num">
            <span class="currency-symbol">¥</span>{{ (stats.totalPurchaseAmount || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2 }) }}
          </div>
          <div class="kpi-footer">
            <span class="trend-badge trend-neutral">
              <el-icon><Van /></el-icon> 资产转化
            </span>
            <span class="footer-desc">已验收入库物资</span>
          </div>
        </div>
        <div class="kpi-icon-pill icon-purchase">
          <el-icon><ShoppingBag /></el-icon>
        </div>
      </div>

      <!-- 商品种类卡片 -->
      <div class="kpi-glass-card kpi-goods">
        <div class="kpi-info">
          <div class="kpi-label">在售商品主数据</div>
          <div class="kpi-num">{{ stats.totalGoodsCount || 0 }} <span class="num-unit">种</span></div>
          <div class="kpi-footer">
            <span class="trend-badge trend-blue">活跃建档</span>
            <span class="footer-desc">涵盖数码/办公/食品</span>
          </div>
        </div>
        <div class="kpi-icon-pill icon-goods">
          <el-icon><Goods /></el-icon>
        </div>
      </div>

      <!-- 紧缺预警卡片 -->
      <div class="kpi-glass-card kpi-alert">
        <div class="kpi-info">
          <div class="kpi-label">库存不足预警</div>
          <div class="kpi-num alert-val">{{ stats.stockAlertCount || 0 }} <span class="num-unit">项</span></div>
          <div class="kpi-footer">
            <span class="trend-badge trend-danger">
              <span class="pulse-mini-dot"></span> 低于安全线
            </span>
            <span class="footer-desc">需尽快制定采购</span>
          </div>
        </div>
        <div class="kpi-icon-pill icon-alert">
          <el-icon><WarningFilled /></el-icon>
        </div>
      </div>
    </div>

    <!-- 中间图表联动分析区 -->
    <div class="charts-dual-row">
      <!-- 图表 1: 近7天进销业务趋势双折线面积图 -->
      <div class="glass-card chart-panel-large">
        <div class="panel-header">
          <div class="header-title-box">
            <div class="title-indicator dot-blue"></div>
            <span class="panel-title">进销存趋势洞察 (销售额 vs 采购支出)</span>
          </div>
          <div class="header-tags">
            <span class="legend-pill"><span class="legend-dot dot-sale"></span> 销售出库</span>
            <span class="legend-pill"><span class="legend-dot dot-pur"></span> 采购进货</span>
          </div>
        </div>
        <div ref="trendChartRef" class="echart-container"></div>
      </div>

      <!-- 图表 2: 畅销商品销量榜 -->
      <div class="glass-card chart-panel-small">
        <div class="panel-header">
          <div class="header-title-box">
            <div class="title-indicator dot-amber"></div>
            <span class="panel-title">畅销商品销量榜 TOP 5</span>
          </div>
          <el-tag size="small" effect="plain" type="info">实时更新</el-tag>
        </div>
        <div ref="rankChartRef" class="echart-container"></div>
      </div>
    </div>

    <!-- 底部：实时库存告警雷达与处理看板 -->
    <div class="glass-card alert-radar-section">
      <div class="panel-header">
        <div class="header-title-box">
          <div class="title-indicator dot-red"></div>
          <span class="panel-title">⚠️ 实时库存告警雷达（触发数据库底线阈值）</span>
        </div>
        <el-button type="primary" size="small" class="quick-pur-btn" @click="$router.push('/purchase')">
          <el-icon style="margin-right: 4px"><ShoppingCart /></el-icon> 一键前往补货
        </el-button>
      </div>

      <el-table :data="alertList" stripe style="width: 100%" v-loading="loading">
        <el-table-column prop="goodsCode" label="商品编码" width="120" />
        <el-table-column prop="goodsName" label="商品名称" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">
            <strong>{{ row.goodsName }}</strong>
          </template>
        </el-table-column>
        <el-table-column prop="categoryName" label="分类" width="110">
          <template #default="{ row }">
            <span class="category-pill">{{ row.categoryName }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="specification" label="规格型号" width="140" show-overflow-tooltip />

        <!-- 现存与健康度指示条 -->
        <el-table-column label="库存健康度 / 现存数量" min-width="200">
          <template #default="{ row }">
            <div class="stock-progress-wrap">
              <el-progress
                :percentage="Math.min(Math.round((row.currentStock / row.minStockAlert) * 100), 100)"
                :status="row.currentStock === 0 ? 'exception' : 'warning'"
                :stroke-width="8"
                :show-text="false"
              />
              <span class="stock-badge-text">
                现存: <strong style="color: #ef4444;">{{ row.currentStock }}</strong> / 安全: {{ row.minStockAlert }} {{ row.unit }}
              </span>
            </div>
          </template>
        </el-table-column>

        <el-table-column prop="shortageQty" label="建议缺口" width="120">
          <template #default="{ row }">
            <el-tag type="danger" effect="light">缺 {{ row.shortageQty }} {{ row.unit }}</el-tag>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, nextTick } from 'vue'
import * as echarts from 'echarts'
import { getDashboardStatsApi, getStockAlertsApi } from '../api/psi'

const stats = ref({})
const alertList = ref([])
const loading = ref(false)

const trendChartRef = ref(null)
const rankChartRef = ref(null)
let trendChart = null
let rankChart = null

// 初始化趋势折线图 (苹果风渐变双面积图)
const initTrendChart = () => {
  if (!trendChartRef.value) return
  if (!trendChart) trendChart = echarts.init(trendChartRef.value)

  const option = {
    tooltip: {
      trigger: 'axis',
      backgroundColor: 'rgba(255, 255, 255, 0.95)',
      borderColor: 'rgba(226, 232, 240, 0.8)',
      textStyle: { color: '#1e293b' },
      boxShadow: '0 10px 25px -5px rgba(0, 0, 0, 0.1)'
    },
    grid: { left: '3%', right: '4%', bottom: '3%', top: '12%', containLabel: true },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: ['周一', '周二', '周三', '周四', '周五', '周六', '今天'],
      axisLine: { lineStyle: { color: '#cbd5e1' } },
      axisLabel: { color: '#64748b' }
    },
    yAxis: {
      type: 'value',
      axisLine: { show: false },
      axisLabel: { color: '#94a3b8' },
      splitLine: { lineStyle: { color: 'rgba(241, 245, 249, 0.9)' } }
    },
    series: [
      {
        name: '销售出库',
        type: 'line',
        smooth: true,
        showSymbol: false,
        lineStyle: { width: 3, color: '#3b82f6' },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(59, 130, 246, 0.35)' },
            { offset: 1, color: 'rgba(59, 130, 246, 0.02)' }
          ])
        },
        data: [12000, 18500, 26000, 15000, 32000, 28000, stats.value.totalSaleAmount || 26997]
      },
      {
        name: '采购进货',
        type: 'line',
        smooth: true,
        showSymbol: false,
        lineStyle: { width: 3, color: '#10b981' },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(16, 185, 129, 0.3)' },
            { offset: 1, color: 'rgba(16, 185, 129, 0.02)' }
          ])
        },
        data: [8000, 14000, 19000, 11000, 25000, 22000, stats.value.totalPurchaseAmount || 18000]
      }
    ]
  }
  trendChart.setOption(option)
}

// 初始化销量 TOP 5 横向条形图
const initRankChart = () => {
  if (!rankChartRef.value) return
  if (!rankChart) rankChart = echarts.init(rankChartRef.value)

  const items = stats.value.topSellingGoods || []
  const names = items.map(x => x.goodsName).reverse()
  const qtys = items.map(x => x.saleQty).reverse()

  const option = {
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: '3%', right: '8%', bottom: '3%', top: '5%', containLabel: true },
    xAxis: {
      type: 'value',
      axisLine: { show: false },
      splitLine: { lineStyle: { color: 'rgba(241, 245, 249, 0.9)' } }
    },
    yAxis: {
      type: 'category',
      data: names.length ? names : ['ThinkPad X1', 'MX 鼠标', 'A4复印纸', '按动中性笔', '天然矿泉水'],
      axisLine: { lineStyle: { color: '#cbd5e1' } },
      axisLabel: { color: '#475569', fontSize: 11 }
    },
    series: [
      {
        name: '累计销量',
        type: 'bar',
        barWidth: 14,
        data: qtys.length ? qtys : [3, 12, 18, 35, 80],
        itemStyle: {
          color: new echarts.graphic.LinearGradient(1, 0, 0, 0, [
            { offset: 0, color: '#f59e0b' },
            { offset: 1, color: '#fbbf24' }
          ]),
          borderRadius: [0, 8, 8, 0]
        }
      }
    ]
  }
  rankChart.setOption(option)
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
    initTrendChart()
    initRankChart()
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchData()
  window.addEventListener('resize', () => {
    trendChart && trendChart.resize()
    rankChart && rankChart.resize()
  })
})
</script>

<style scoped>
.glass-dashboard {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* 顶部 4 宫格 KPI 卡片 */
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
}

.kpi-glass-card {
  position: relative;
  overflow: hidden;
  border-radius: 18px;
  padding: 22px 20px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border: 1px solid rgba(255, 255, 255, 0.85);
  box-shadow: 0 10px 30px -6px rgba(30, 41, 59, 0.05);
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.kpi-glass-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 16px 36px -6px rgba(30, 41, 59, 0.09);
}

.kpi-sales {
  background: linear-gradient(135deg, rgba(239, 246, 255, 0.85) 0%, rgba(219, 234, 254, 0.5) 100%);
}
.kpi-purchase {
  background: linear-gradient(135deg, rgba(236, 253, 245, 0.85) 0%, rgba(209, 250, 229, 0.5) 100%);
}
.kpi-goods {
  background: linear-gradient(135deg, rgba(254, 243, 199, 0.85) 0%, rgba(253, 230, 138, 0.5) 100%);
}
.kpi-alert {
  background: linear-gradient(135deg, rgba(254, 242, 242, 0.85) 0%, rgba(254, 202, 202, 0.5) 100%);
}

.kpi-label {
  font-size: 13px;
  font-weight: 500;
  color: #64748b;
  margin-bottom: 6px;
}

.kpi-num {
  font-size: 24px;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: -0.5px;
  margin-bottom: 8px;
}

.currency-symbol {
  font-size: 16px;
  margin-right: 2px;
  color: #64748b;
}

.num-unit {
  font-size: 14px;
  font-weight: 500;
  color: #64748b;
}

.alert-val {
  color: #dc2626;
}

.kpi-footer {
  display: flex;
  align-items: center;
  gap: 8px;
}

.trend-badge {
  font-size: 11px;
  font-weight: 600;
  padding: 2px 6px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  gap: 2px;
}

.trend-up { background: #dbeafe; color: #1d4ed8; }
.trend-neutral { background: #d1fae5; color: #047857; }
.trend-blue { background: #fef3c7; color: #b45309; }
.trend-danger { background: #fee2e2; color: #b91c1c; }

.pulse-mini-dot {
  width: 6px;
  height: 6px;
  background-color: #ef4444;
  border-radius: 50%;
  animation: pulse-mini 1.5s infinite;
}

@keyframes pulse-mini {
  0% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.4; transform: scale(1.3); }
  100% { opacity: 1; transform: scale(1); }
}

.footer-desc {
  font-size: 11px;
  color: #94a3b8;
}

.kpi-icon-pill {
  width: 48px;
  height: 48px;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.icon-sales { background: #ffffff; color: #3b82f6; box-shadow: 0 4px 12px rgba(59, 130, 246, 0.15); }
.icon-purchase { background: #ffffff; color: #10b981; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.15); }
.icon-goods { background: #ffffff; color: #f59e0b; box-shadow: 0 4px 12px rgba(245, 158, 11, 0.15); }
.icon-alert { background: #ffffff; color: #ef4444; box-shadow: 0 4px 12px rgba(239, 68, 68, 0.15); }

/* 图表双栏 */
.charts-dual-row {
  display: grid;
  grid-template-columns: 62% 38%;
  gap: 16px;
}

.chart-panel-large,
.chart-panel-small {
  padding: 20px;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 14px;
}

.header-title-box {
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.dot-blue { background-color: #3b82f6; box-shadow: 0 0 8px rgba(59, 130, 246, 0.6); }
.dot-amber { background-color: #f59e0b; box-shadow: 0 0 8px rgba(245, 158, 11, 0.6); }
.dot-red { background-color: #ef4444; box-shadow: 0 0 8px rgba(239, 68, 68, 0.6); }

.panel-title {
  font-size: 15px;
  font-weight: 600;
  color: #0f172a;
}

.header-tags {
  display: flex;
  gap: 12px;
}

.legend-pill {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: #64748b;
}

.legend-dot {
  width: 8px;
  height: 8px;
  border-radius: 2px;
}

.dot-sale { background-color: #3b82f6; }
.dot-pur { background-color: #10b981; }

.echart-container {
  height: 270px;
  width: 100%;
}

/* 底部告警卡片 */
.alert-radar-section {
  padding: 20px;
}

.quick-pur-btn {
  border-radius: 10px !important;
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%) !important;
  border: none !important;
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

.stock-badge-text {
  font-size: 12px;
  color: #64748b;
}
</style>
