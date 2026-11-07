<template>
  <div class="glass-login-container">
    <!-- 背景流光氛围光斑 (苹果风环境光) -->
    <div class="ambient-glow orb-1"></div>
    <div class="ambient-glow orb-2"></div>
    <div class="ambient-glow orb-3"></div>

    <div class="login-card-glass">
      <div class="brand-badge-row">
        <div class="app-icon-squircle">
          <el-icon><Box /></el-icon>
        </div>
      </div>

      <div class="welcome-text">
        <h2>商品购销存管理系统</h2>
        <p>Enterprise Purchase, Sales & Inventory System</p>
      </div>

      <el-form :model="loginForm" :rules="rules" ref="formRef" class="login-input-group">
        <el-form-item prop="username">
          <el-input
            v-model="loginForm.username"
            placeholder="登录工号 / 用户名"
            size="large"
            prefix-icon="User"
            class="apple-input"
          />
        </el-form-item>

        <el-form-item prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="安全密码 (默认 123456)"
            size="large"
            prefix-icon="Lock"
            show-password
            class="apple-input"
            @keyup.enter="handleLogin"
          />
        </el-form-item>

        <el-button
          type="primary"
          size="large"
          class="submit-glow-btn"
          :loading="loading"
          @click="handleLogin"
        >
          立即登录控制台
        </el-button>
      </el-form>

      <!-- 答辩演示快捷身份选择区 -->
      <div class="demo-role-section">
        <div class="section-label">⚡️ 答辩演示 · 一键快速填入身份</div>
        <div class="pill-grid">
          <div class="role-card-pill" @click="fillAccount('admin', '123456')">
            <span class="role-tag-dot dot-admin"></span>
            <div class="role-desc">
              <span class="role-name">系统管理员</span>
              <span class="role-account">admin</span>
            </div>
          </div>

          <div class="role-card-pill" @click="fillAccount('purchaser01', '123456')">
            <span class="role-tag-dot dot-pur"></span>
            <div class="role-desc">
              <span class="role-name">采购专员</span>
              <span class="role-account">purchaser01</span>
            </div>
          </div>

          <div class="role-card-pill" @click="fillAccount('sales01', '123456')">
            <span class="role-tag-dot dot-sale"></span>
            <div class="role-desc">
              <span class="role-name">销售代表</span>
              <span class="role-account">sales01</span>
            </div>
          </div>

          <div class="role-card-pill" @click="fillAccount('keeper01', '123456')">
            <span class="role-tag-dot dot-keeper"></span>
            <div class="role-desc">
              <span class="role-name">仓管员</span>
              <span class="role-account">keeper01</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { loginApi } from '../api/psi'

const router = useRouter()
const formRef = ref(null)
const loading = ref(false)

const loginForm = reactive({
  username: 'admin',
  password: '123456'
})

const rules = {
  username: [{ required: true, message: '请输入工号或用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const fillAccount = (u, p) => {
  loginForm.username = u
  loginForm.password = p
  ElMessage.success(`已切换预设身份：${u}`)
}

const handleLogin = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    loading.value = true
    try {
      const res = await loginApi(loginForm)
      if (res.code === 200) {
        localStorage.setItem('psi_token', res.data.token)
        localStorage.setItem('psi_user', JSON.stringify(res.data))
        ElMessage.success(`欢迎回来，${res.data.realName}！`)
        router.push('/dashboard')
      }
    } catch (e) {
      console.error(e)
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped>
.glass-login-container {
  height: 100vh;
  width: 100vw;
  background: radial-gradient(circle at 15% 20%, #e0e7ff 0%, #f1f5f9 60%, #e2e8f0 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
}

/* 弥散光晕效果 */
.ambient-glow {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  pointer-events: none;
  opacity: 0.6;
}

.orb-1 {
  width: 380px;
  height: 380px;
  background: rgba(59, 130, 246, 0.35);
  top: 10%;
  left: 20%;
  animation: float-1 8s ease-in-out infinite alternate;
}

.orb-2 {
  width: 320px;
  height: 320px;
  background: rgba(168, 85, 247, 0.25);
  bottom: 15%;
  right: 22%;
  animation: float-2 10s ease-in-out infinite alternate;
}

.orb-3 {
  width: 260px;
  height: 260px;
  background: rgba(16, 185, 129, 0.25);
  top: 45%;
  right: 35%;
}

@keyframes float-1 {
  0% { transform: translateY(0) scale(1); }
  100% { transform: translateY(30px) scale(1.08); }
}

@keyframes float-2 {
  0% { transform: translateY(0) scale(1); }
  100% { transform: translateY(-35px) scale(1.05); }
}

/* 核心毛玻璃卡片 */
.login-card-glass {
  position: relative;
  z-index: 10;
  width: 440px;
  background: rgba(255, 255, 255, 0.75);
  backdrop-filter: blur(28px);
  -webkit-backdrop-filter: blur(28px);
  border: 1px solid rgba(255, 255, 255, 0.85);
  border-radius: 24px;
  padding: 40px 36px 32px;
  box-shadow: 0 20px 40px -15px rgba(30, 41, 59, 0.12), 0 0 0 1px rgba(255, 255, 255, 0.7);
}

.brand-badge-row {
  display: flex;
  justify-content: center;
  margin-bottom: 16px;
}

.app-icon-squircle {
  width: 58px;
  height: 58px;
  background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
  border-radius: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  font-size: 30px;
  box-shadow: 0 10px 20px -4px rgba(59, 130, 246, 0.45);
}

.welcome-text {
  text-align: center;
  margin-bottom: 24px;
}

.welcome-text h2 {
  font-size: 22px;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: -0.4px;
  margin-bottom: 4px;
}

.welcome-text p {
  font-size: 12px;
  color: #64748b;
}

.apple-input :deep(.el-input__wrapper) {
  height: 44px;
  border-radius: 12px !important;
  background: rgba(255, 255, 255, 0.8) !important;
  box-shadow: 0 0 0 1px rgba(203, 213, 225, 0.8) inset !important;
  padding: 0 14px;
}

.submit-glow-btn {
  width: 100%;
  height: 44px;
  border-radius: 12px !important;
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%) !important;
  border: none !important;
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 0.5px;
  box-shadow: 0 8px 18px -4px rgba(59, 130, 246, 0.4);
  margin-top: 6px;
}

.submit-glow-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 12px 24px -4px rgba(59, 130, 246, 0.5);
}

/* 角色快速填入卡片 */
.demo-role-section {
  margin-top: 24px;
  padding-top: 20px;
  border-top: 1px solid rgba(226, 232, 240, 0.8);
}

.section-label {
  font-size: 11px;
  font-weight: 600;
  color: #64748b;
  margin-bottom: 12px;
  text-align: center;
  letter-spacing: 0.3px;
}

.pill-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
}

.role-card-pill {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  background: rgba(255, 255, 255, 0.6);
  border: 1px solid rgba(226, 232, 240, 0.8);
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.role-card-pill:hover {
  background: #ffffff;
  border-color: #3b82f6;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px -2px rgba(59, 130, 246, 0.15);
}

.role-tag-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.dot-admin { background: #ef4444; }
.dot-pur { background: #3b82f6; }
.dot-sale { background: #10b981; }
.dot-keeper { background: #f59e0b; }

.role-desc {
  display: flex;
  flex-direction: column;
}

.role-name {
  font-size: 12px;
  font-weight: 600;
  color: #1e293b;
}

.role-account {
  font-size: 10px;
  color: #94a3b8;
}
</style>
