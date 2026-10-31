<template>
  <el-container class="layout-container">
    <!-- 侧边栏导航 -->
    <el-aside width="240px" class="aside-menu">
      <div class="logo-area">
        <el-icon class="logo-icon"><Box /></el-icon>
        <div class="logo-text">
          <div class="title">PSI 购销存管理</div>
          <div class="subtitle">数据库综合实训平台</div>
        </div>
      </div>

      <el-menu
        :default-active="activeMenu"
        class="el-menu-vertical"
        router
        background-color="#1e222d"
        text-color="#c1c6d0"
        active-text-color="#409eff"
      >
        <el-menu-item index="/dashboard">
          <el-icon><Odometer /></el-icon>
          <span>系统首页看板</span>
        </el-menu-item>

        <el-menu-item index="/goods">
          <el-icon><Goods /></el-icon>
          <span>商品主数据管理</span>
        </el-menu-item>

        <el-menu-item index="/purchase">
          <el-icon><ShoppingCart /></el-icon>
          <span>采购制单与入库</span>
        </el-menu-item>

        <el-menu-item index="/sale">
          <el-icon><Sell /></el-icon>
          <span>销售制单与出库</span>
        </el-menu-item>

        <el-menu-item index="/inventory">
          <el-icon><Box /></el-icon>
          <span>库存一览与流水审计</span>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <!-- 顶栏 -->
      <el-header class="header-bar">
        <div class="header-left">
          <span class="route-title">{{ currentRouteTitle }}</span>
        </div>
        <div class="header-right">
          <el-tag :type="roleTagType" effect="dark" class="role-tag">
            {{ roleDisplayName }}
          </el-tag>
          <span class="user-greeting">欢迎，<strong>{{ realName }}</strong></span>
          <el-button type="danger" link @click="handleLogout">
            <el-icon><SwitchButton /></el-icon> 退出登录
          </el-button>
        </div>
      </el-header>

      <!-- 主工作区 -->
      <el-main class="main-body">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessageBox, ElMessage } from 'element-plus'

const route = useRoute()
const router = useRouter()

const activeMenu = computed(() => route.path)
const currentRouteTitle = computed(() => route.meta?.title || '管理控制台')

const userInfo = ref(JSON.parse(localStorage.getItem('psi_user') || '{}'))
const realName = computed(() => userInfo.value.realName || '用户')
const roles = computed(() => userInfo.value.roles || [])

const roleDisplayName = computed(() => {
  if (roles.value.includes('ROLE_ADMIN')) return '系统管理员'
  if (roles.value.includes('ROLE_PURCHASER')) return '采购专员'
  if (roles.value.includes('ROLE_SALESPERSON')) return '销售代表'
  if (roles.value.includes('ROLE_KEEPER')) return '仓储管理员'
  return '普通用户'
})

const roleTagType = computed(() => {
  if (roles.value.includes('ROLE_ADMIN')) return 'danger'
  if (roles.value.includes('ROLE_PURCHASER')) return 'primary'
  if (roles.value.includes('ROLE_SALESPERSON')) return 'success'
  if (roles.value.includes('ROLE_KEEPER')) return 'warning'
  return 'info'
})

const handleLogout = () => {
  ElMessageBox.confirm('确定要退出当前系统吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    localStorage.removeItem('psi_token')
    localStorage.removeItem('psi_user')
    ElMessage.success('已安全退出')
    router.push('/login')
  })
}
</script>

<style scoped>
.layout-container {
  height: 100vh;
  width: 100vw;
  overflow: hidden;
}

.aside-menu {
  background-color: #1e222d;
  display: flex;
  flex-direction: column;
}

.logo-area {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 20px 16px;
  background-color: #161922;
  color: #fff;
  border-bottom: 1px solid #282d3c;
}

.logo-icon {
  font-size: 28px;
  color: #409eff;
}

.logo-text .title {
  font-size: 16px;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.logo-text .subtitle {
  font-size: 11px;
  color: #88909d;
  margin-top: 2px;
}

.el-menu-vertical {
  border-right: none;
  flex: 1;
}

.header-bar {
  background-color: #fff;
  border-bottom: 1px solid #e6e8eb;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  height: 60px;
}

.route-title {
  font-size: 17px;
  font-weight: 600;
  color: #2c3e50;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.role-tag {
  font-size: 12px;
  padding: 0 10px;
}

.user-greeting {
  font-size: 14px;
  color: #606266;
}

.main-body {
  background-color: #f4f6f9;
  padding: 20px;
  overflow-y: auto;
}
</style>
