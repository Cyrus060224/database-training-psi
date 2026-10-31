import axios from 'axios'
import { ElMessage } from 'element-plus'

const service = axios.create({
  baseURL: 'http://localhost:5050/api',
  timeout: 10000
})

// 请求拦截器
service.interceptors.request.use(
  config => {
    const token = localStorage.getItem('psi_token')
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`
    }
    return config
  },
  error => {
    return Promise.reject(error)
  }
)

// 响应拦截器
service.interceptors.response.use(
  response => {
    const res = response.data
    // 如果返回数据包含 code
    if (res.code !== undefined && res.code !== 200) {
      ElMessage.error(res.message || '操作失败')
      return Promise.reject(new Error(res.message || 'Error'))
    }
    return res
  },
  error => {
    if (error.response && error.response.status === 401) {
      ElMessage.error('登录状态已过期，请重新登录')
      localStorage.removeItem('psi_token')
      localStorage.removeItem('psi_user')
      window.location.href = '/login'
    } else {
      ElMessage.error(error.message || '网络连接异常')
    }
    return Promise.reject(error)
  }
)

export default service
