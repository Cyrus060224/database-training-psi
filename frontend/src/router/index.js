import { createRouter, createWebHistory } from 'vue-router'
import MainLayout from '../layout/MainLayout.vue'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/Login.vue'),
    meta: { title: '用户登录' }
  },
  {
    path: '/',
    component: MainLayout,
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('../views/Dashboard.vue'),
        meta: { title: '系统首页看板', icon: 'Odometer' }
      },
      {
        path: 'goods',
        name: 'Goods',
        component: () => import('../views/Goods.vue'),
        meta: { title: '商品档案管理', icon: 'Goods' }
      },
      {
        path: 'purchase',
        name: 'Purchase',
        component: () => import('../views/Purchase.vue'),
        meta: { title: '采购入库管理', icon: 'ShoppingCart' }
      },
      {
        path: 'sale',
        name: 'Sale',
        component: () => import('../views/Sale.vue'),
        meta: { title: '销售出库管理', icon: 'Sell' }
      },
      {
        path: 'inventory',
        name: 'Inventory',
        component: () => import('../views/Inventory.vue'),
        meta: { title: '库存与审计流水', icon: 'Box' }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫：未登录拦截
router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('psi_token')
  if (to.path !== '/login' && !token) {
    next('/login')
  } else {
    next()
  }
})

export default router
