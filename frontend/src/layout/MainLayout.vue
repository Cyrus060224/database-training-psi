<template>
  <el-container class="layout-container">
    <!-- 苹果风悬浮毛玻璃侧边栏 -->
    <el-aside width="260px" class="glass-aside">
      <div class="brand-header">
        <div class="brand-logo-glow">
          <el-icon class="brand-icon"><Box /></el-icon>
        </div>
        <div class="brand-text">
          <div class="brand-title">PSI 购销存</div>
          <div class="brand-badge">PRO ENTERPRISE</div>
        </div>
      </div>

      <div class="menu-wrapper">
        <el-menu
          :default-active="activeMenu"
          class="glass-menu"
          router
        >
          <div class="menu-group-label">核心运营</div>

          <el-menu-item index="/dashboard">
            <el-icon><Odometer /></el-icon>
            <span>系统总览大屏</span>
          </el-menu-item>

          <el-menu-item index="/inventory">
            <el-icon><Box /></el-icon>
            <span>实时库存与审计</span>
          </el-menu-item>

          <div class="menu-group-label">业务协同</div>

          <el-menu-item index="/purchase">
            <el-icon><ShoppingCart /></el-icon>
            <span>采购制单与入库</span>
          </el-menu-item>

          <el-menu-item index="/sale">
            <el-icon><Sell /></el-icon>
            <span>销售开单与出库</span>
          </el-menu-item>

          <div class="menu-group-label">基础档案</div>

          <el-menu-item index="/goods">
            <el-icon><Goods /></el-icon>
            <span>商品主数据</span>
          </el-menu-item>

          <el-menu-item index="/partners">
            <el-icon><UserFilled /></el-icon>
            <span>往来单位 (客商)</span>
          </el-menu-item>
        </el-menu>
      </div>

      <div class="aside-footer">
        <div class="version-pill">
          <span class="pulse-dot"></span>
          <span>SQL Server · 正常运行</span>
        </div>
      </div>
    </el-aside>

    <!-- 右侧主体内容 -->
    <el-container class="main-wrapper">
      <!-- 悬浮毛玻璃顶栏 -->
      <el-header class="glass-header">
        <div class="header-left">
          <div class="breadcrumb-nav">
            <span class="crumb-parent">管理平台</span>
            <span class="crumb-separator">/</span>
            <span class="crumb-active">{{ currentRouteTitle }}</span>
          </div>
        </div>

        <div class="header-right">
          <div class="role-badge-box">
            <span class="role-label-tag" :class="roleClass">{{ roleDisplayName }}</span>
          </div>

          <div class="user-profile-pill">
            <div class="user-avatar-circle">{{ realName.substring(0, 1) }}</div>
            <span class="user-name-text">{{ realName }}</span>
          </div>

          <el-tooltip content="安全退出系统" placement="bottom">
            <button class="logout-action-btn" @click="handleLogout">
              <el-icon><SwitchButton /></el-icon>
            </button>
          </el-tooltip>
        </div>
      </el-header>

      <!-- 动态路由工作区 -->
      <el-main class="content-body">
        <router-view v-slot="{ Component }">
          <transition name="fade-slide" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { computed, ref } from 'vue'
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

const roleClass = computed(() => {
  if (roles.value.includes('ROLE_ADMIN')) return 'role-admin'
  if (roles.value.includes('ROLE_PURCHASER')) return 'role-purchaser'
  if (roles.value.includes('ROLE_SALESPERSON')) return 'role-sales'
  if (roles.value.includes('ROLE_KEEPER')) return 'role-keeper'
  return 'role-default'
})

const handleLogout = () => {
  ElMessageBox.confirm('确定要退出当前系统吗？', '安全退出提示', {
    confirmButtonText: '退出',
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
  padding: 14px;
  gap: 16px;
  background: var(--psi-bg-gradient);
}

/* 侧边栏 */
.glass-aside {
  background: rgba(255, 255, 255, 0.75);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.9);
  border-radius: 20px;
  box-shadow: 0 10px 30px -6px rgba(30, 41, 59, 0.06);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.brand-header {
  padding: 24px 20px 18px;
  display: flex;
  align-items: center;
  gap: 14px;
  border-bottom: 1px solid rgba(226, 232, 240, 0.6);
}

.brand-logo-glow {
  width: 44px;
  height: 44px;
  background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  font-size: 24px;
  box-shadow: 0 8px 16px -4px rgba(59, 130, 246, 0.4);
}

.brand-text .brand-title {
  font-size: 17px;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: -0.3px;
}

.brand-text .brand-badge {
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 1px;
  color: #3b82f6;
  background: rgba(59, 130, 246, 0.1);
  padding: 2px 6px;
  border-radius: 4px;
  display: inline-block;
  margin-top: 3px;
}

.menu-wrapper {
  flex: 1;
  padding: 14px 12px;
  overflow-y: auto;
}

.menu-group-label {
  font-size: 11px;
  font-weight: 600;
  color: #94a3b8;
  padding: 12px 14px 6px;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

.glass-menu {
  background: transparent;
  border-right: none;
}

.glass-menu :deep(.el-menu-item) {
  height: 44px;
  line-height: 44px;
  border-radius: 12px;
  margin-bottom: 4px;
  color: #475569;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  padding: 0 14px !important;
}

.glass-menu :deep(.el-menu-item:hover) {
  background-color: rgba(241, 245, 249, 0.8);
  color: #1e293b;
  transform: translateX(3px);
}

.glass-menu :deep(.el-menu-item.is-active) {
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.15) 0%, rgba(59, 130, 246, 0.08) 100%) !important;
  color: #2563eb !important;
  font-weight: 600;
  box-shadow: 0 2px 8px -2px rgba(59, 130, 246, 0.2);
}

.glass-menu :deep(.el-menu-item .el-icon) {
  font-size: 18px;
  margin-right: 10px;
}

.aside-footer {
  padding: 16px 20px;
  border-top: 1px solid rgba(226, 232, 240, 0.6);
  background: rgba(248, 250, 252, 0.5);
}

.version-pill {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #64748b;
}

.pulse-dot {
  width: 8px;
  height: 8px;
  background-color: #10b981;
  border-radius: 50%;
  box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.6);
  animation: pulse-ring 2s infinite;
}

@keyframes pulse-ring {
  0% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.6); }
  70% { box-shadow: 0 0 0 6px rgba(16, 185, 129, 0); }
  100% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
}

/* 主工作区外壳 */
.main-wrapper {
  display: flex;
  flex-direction: column;
  gap: 16px;
  overflow: hidden;
}

/* 顶部栏 */
.glass-header {
  height: 64px !important;
  background: rgba(255, 255, 255, 0.75);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.9);
  border-radius: 18px;
  box-shadow: 0 10px 30px -6px rgba(30, 41, 59, 0.06);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
}

.breadcrumb-nav {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}

.crumb-parent {
  color: #94a3b8;
  font-weight: 400;
}

.crumb-separator {
  color: #cbd5e1;
}

.crumb-active {
  color: #0f172a;
  font-weight: 600;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 14px;
}

.role-label-tag {
  font-size: 12px;
  padding: 4px 10px;
  border-radius: 20px;
  font-weight: 600;
}

.role-admin { background: #fee2e2; color: #dc2626; }
.role-purchaser { background: #dbeafe; color: #2563eb; }
.role-sales { background: #dcfce7; color: #16a34a; }
.role-keeper { background: #fef3c7; color: #d97706; }
.role-default { background: #f1f5f9; color: #475569; }

.user-profile-pill {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 12px 4px 6px;
  background: rgba(241, 245, 249, 0.8);
  border-radius: 30px;
}

.user-avatar-circle {
  width: 28px;
  height: 28px;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%);
  color: #fff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 600;
}

.user-name-text {
  font-size: 13px;
  font-weight: 600;
  color: #1e293b;
}

.logout-action-btn {
  background: transparent;
  border: none;
  cursor: pointer;
  color: #94a3b8;
  font-size: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 10px;
  transition: all 0.2s ease;
}

.logout-action-btn:hover {
  background: #fee2e2;
  color: #ef4444;
}

/* 主内容区 */
.content-body {
  padding: 0 !important;
  overflow-y: auto;
  border-radius: 18px;
}

.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
