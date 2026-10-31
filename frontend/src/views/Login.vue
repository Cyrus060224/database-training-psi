<template>
  <div class="login-wrapper">
    <div class="login-card">
      <div class="login-header">
        <div class="logo-circle">
          <el-icon><Box /></el-icon>
        </div>
        <h2>商品购销存管理系统</h2>
        <p>Comprehensive PSI Management Information System</p>
      </div>

      <el-form :model="loginForm" :rules="rules" ref="formRef" class="login-form">
        <el-form-item prop="username">
          <el-input
            v-model="loginForm.username"
            placeholder="请输入账号 (如 admin)"
            size="large"
            prefix-icon="User"
          />
        </el-form-item>

        <el-form-item prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="请输入密码 (默认 123456)"
            size="large"
            prefix-icon="Lock"
            show-password
            @keyup.enter="handleLogin"
          />
        </el-form-item>

        <el-button
          type="primary"
          size="large"
          class="login-btn"
          :loading="loading"
          @click="handleLogin"
        >
          立即登录系统
        </el-button>
      </el-form>

      <!-- 答辩演示快捷切换角色面板 -->
      <div class="quick-roles-card">
        <div class="quick-title">⚡️ 答辩演示 · 预设角色一键填入：</div>
        <div class="quick-btns">
          <el-tag
            class="role-pill"
            type="danger"
            effect="plain"
            @click="fillAccount('admin', '123456')"
          >
            系统管理员 (admin)
          </el-tag>
          <el-tag
            class="role-pill"
            type="primary"
            effect="plain"
            @click="fillAccount('purchaser01', '123456')"
          >
            采购专员 (purchaser01)
          </el-tag>
          <el-tag
            class="role-pill"
            type="success"
            effect="plain"
            @click="fillAccount('sales01', '123456')"
          >
            销售代表 (sales01)
          </el-tag>
          <el-tag
            class="role-pill"
            type="warning"
            effect="plain"
            @click="fillAccount('keeper01', '123456')"
          >
            仓储管理员 (keeper01)
          </el-tag>
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
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const fillAccount = (u, p) => {
  loginForm.username = u
  loginForm.password = p
  ElMessage.info(`已填入账号：${u}`)
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
.login-wrapper {
  height: 100vh;
  width: 100vw;
  background: linear-gradient(135deg, #1f232e 0%, #0d1117 100%);
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-card {
  width: 440px;
  background: #ffffff;
  border-radius: 12px;
  padding: 40px 36px 30px;
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.35);
}

.login-header {
  text-align: center;
  margin-bottom: 28px;
}

.logo-circle {
  width: 56px;
  height: 56px;
  background: #ecf5ff;
  color: #409eff;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 30px;
  margin: 0 auto 12px;
}

.login-header h2 {
  font-size: 22px;
  color: #1a1a1a;
  margin: 0 0 6px;
  font-weight: 700;
}

.login-header p {
  font-size: 12px;
  color: #8c8c8c;
  margin: 0;
}

.login-form {
  margin-top: 10px;
}

.login-btn {
  width: 100%;
  margin-top: 10px;
  font-weight: 600;
  letter-spacing: 1px;
}

.quick-roles-card {
  margin-top: 28px;
  padding: 14px;
  background-color: #f7f9fc;
  border-radius: 8px;
  border: 1px dashed #dcdfe6;
}

.quick-title {
  font-size: 12px;
  color: #606266;
  font-weight: 600;
  margin-bottom: 10px;
}

.quick-btns {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.role-pill {
  cursor: pointer;
  transition: all 0.2s ease;
  user-select: none;
}

.role-pill:hover {
  transform: translateY(-2px);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
}
</style>
