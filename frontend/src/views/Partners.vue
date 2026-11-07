<template>
  <div class="glass-page-container">
    <div class="glass-card page-card">
      <el-tabs v-model="activeTab" class="apple-tabs">
        <!-- Tab 1: 供应商档案管理 -->
        <el-tab-pane label="🏢 合作供应商档案" name="suppliers">
          <div class="tab-toolbar">
            <div class="toolbar-title">
              <h4>供应商合作名录（用于采购制单溯源）</h4>
            </div>
            <el-button type="primary" icon="Plus" class="glass-action-btn" @click="openSupplierDialog()">
              新增合作供应商
            </el-button>
          </div>

          <el-table :data="suppliers" v-loading="loading" stripe style="width: 100%">
            <el-table-column prop="supplierCode" label="供应商编号" width="140" />
            <el-table-column prop="supplierName" label="供应商全称" min-width="180">
              <template #default="{ row }">
                <strong>{{ row.supplierName }}</strong>
              </template>
            </el-table-column>
            <el-table-column prop="contactPerson" label="联系人" width="120" />
            <el-table-column prop="phone" label="联系电话" width="140" />
            <el-table-column prop="address" label="经营地址" min-width="200" show-overflow-tooltip />
            <el-table-column label="操作" width="140" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openSupplierDialog(row)">编辑</el-button>
                <el-button link type="danger" size="small" @click="handleDeleteSupplier(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <!-- Tab 2: 客户档案管理 -->
        <el-tab-pane label="👥 签约客户档案" name="customers">
          <div class="tab-toolbar">
            <div class="toolbar-title">
              <h4>销售往来客户名录（含信用额度授信）</h4>
            </div>
            <el-button type="success" icon="Plus" class="glass-action-btn" @click="openCustomerDialog()">
              新增签约客户
            </el-button>
          </div>

          <el-table :data="customers" v-loading="loading" stripe style="width: 100%">
            <el-table-column prop="customerCode" label="客户编号" width="140" />
            <el-table-column prop="customerName" label="客户企业名称" min-width="180">
              <template #default="{ row }">
                <strong>{{ row.customerName }}</strong>
              </template>
            </el-table-column>
            <el-table-column prop="contactPerson" label="负责人" width="120" />
            <el-table-column prop="phone" label="联系方式" width="140" />
            <el-table-column prop="creditLimit" label="信用额度(¥)" width="150">
              <template #default="{ row }">
                <span class="credit-tag">¥ {{ Number(row.creditLimit).toLocaleString() }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="address" label="送货/注册地址" min-width="200" show-overflow-tooltip />
            <el-table-column label="操作" width="140" fixed="right">
              <template #default="{ row }">
                <el-button link type="primary" size="small" @click="openCustomerDialog(row)">编辑</el-button>
                <el-button link type="danger" size="small" @click="handleDeleteCustomer(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </div>

    <!-- 供应商编辑弹窗 -->
    <el-dialog
      v-model="supplierDialogVisible"
      :title="supplierForm.supplierId ? '编辑供应商档案' : '新建合作供应商'"
      width="520px"
      destroy-on-close
    >
      <el-form :model="supplierForm" label-width="100px">
        <el-form-item label="企业全称" required>
          <el-input v-model="supplierForm.supplierName" placeholder="例如：联想供应链有限公司" />
        </el-form-item>
        <el-form-item label="供应商编码">
          <el-input v-model="supplierForm.supplierCode" placeholder="留空则按时间戳生成" />
        </el-form-item>
        <el-form-item label="商务联系人">
          <el-input v-model="supplierForm.contactPerson" placeholder="姓名 / 职务" />
        </el-form-item>
        <el-form-item label="联系电话" required>
          <el-input v-model="supplierForm.phone" placeholder="手机号或企业总机" />
        </el-form-item>
        <el-form-item label="注册/经营地">
          <el-input v-model="supplierForm.address" placeholder="详细地址" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="supplierDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitSupplier">保存提交</el-button>
      </template>
    </el-dialog>

    <!-- 客户编辑弹窗 -->
    <el-dialog
      v-model="customerDialogVisible"
      :title="customerForm.customerId ? '编辑客户档案' : '新建签约客户'"
      width="520px"
      destroy-on-close
    >
      <el-form :model="customerForm" label-width="100px">
        <el-form-item label="客户全称" required>
          <el-input v-model="customerForm.customerName" placeholder="例如：南京审计大学后勤处" />
        </el-form-item>
        <el-form-item label="客户编码">
          <el-input v-model="customerForm.customerCode" placeholder="留空则按时间戳生成" />
        </el-form-item>
        <el-form-item label="对接人">
          <el-input v-model="customerForm.contactPerson" placeholder="姓名 / 职务" />
        </el-form-item>
        <el-form-item label="联系电话" required>
          <el-input v-model="customerForm.phone" placeholder="联系电话" />
        </el-form-item>
        <el-form-item label="信用授信额度">
          <el-input-number v-model="customerForm.creditLimit" :min="0" :step="10000" style="width: 100%" />
        </el-form-item>
        <el-form-item label="送货地址">
          <el-input v-model="customerForm.address" placeholder="货物交付收货地址" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="customerDialogVisible = false">取消</el-button>
        <el-button type="success" @click="submitCustomer">保存提交</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  getSuppliersApi,
  saveSupplierApi,
  deleteSupplierApi,
  getCustomersApi,
  saveCustomerApi,
  deleteCustomerApi
} from '../api/psi'

const activeTab = ref('suppliers')
const loading = ref(false)

const suppliers = ref([])
const customers = ref([])

// 供应商弹窗
const supplierDialogVisible = ref(false)
const supplierForm = reactive({
  supplierId: 0,
  supplierCode: '',
  supplierName: '',
  contactPerson: '',
  phone: '',
  address: ''
})

// 客户弹窗
const customerDialogVisible = ref(false)
const customerForm = reactive({
  customerId: 0,
  customerCode: '',
  customerName: '',
  contactPerson: '',
  phone: '',
  address: '',
  creditLimit: 50000
})

const fetchSuppliers = async () => {
  const res = await getSuppliersApi()
  if (res.code === 200) suppliers.value = res.data
}

const fetchCustomers = async () => {
  const res = await getCustomersApi()
  if (res.code === 200) customers.value = res.data
}

const openSupplierDialog = (row = null) => {
  if (row) {
    Object.assign(supplierForm, row)
  } else {
    Object.assign(supplierForm, {
      supplierId: 0,
      supplierCode: '',
      supplierName: '',
      contactPerson: '',
      phone: '',
      address: ''
    })
  }
  supplierDialogVisible.value = true
}

const submitSupplier = async () => {
  if (!supplierForm.supplierName) return ElMessage.warning('请输入供应商名称')
  const res = await saveSupplierApi(supplierForm)
  if (res.code === 200) {
    ElMessage.success(res.message || '操作成功')
    supplierDialogVisible.value = false
    fetchSuppliers()
  }
}

const handleDeleteSupplier = (row) => {
  ElMessageBox.confirm(`确定要删除供应商【${row.supplierName}】吗？`, '删除确认', { type: 'warning' })
    .then(async () => {
      const res = await deleteSupplierApi(row.supplierId)
      if (res.code === 200) {
        ElMessage.success('删除成功')
        fetchSuppliers()
      }
    })
}

const openCustomerDialog = (row = null) => {
  if (row) {
    Object.assign(customerForm, row)
  } else {
    Object.assign(customerForm, {
      customerId: 0,
      customerCode: '',
      customerName: '',
      contactPerson: '',
      phone: '',
      address: '',
      creditLimit: 50000
    })
  }
  customerDialogVisible.value = true
}

const submitCustomer = async () => {
  if (!customerForm.customerName) return ElMessage.warning('请输入客户名称')
  const res = await saveCustomerApi(customerForm)
  if (res.code === 200) {
    ElMessage.success(res.message || '操作成功')
    customerDialogVisible.value = false
    fetchCustomers()
  }
}

const handleDeleteCustomer = (row) => {
  ElMessageBox.confirm(`确定要删除客户【${row.customerName}】吗？`, '删除确认', { type: 'warning' })
    .then(async () => {
      const res = await deleteCustomerApi(row.customerId)
      if (res.code === 200) {
        ElMessage.success('删除成功')
        fetchCustomers()
      }
    })
}

onMounted(() => {
  fetchSuppliers()
  fetchCustomers()
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

.toolbar-title h4 {
  margin: 0;
  font-size: 15px;
  color: #1e293b;
  font-weight: 600;
}

.glass-action-btn {
  border-radius: 10px !important;
}

.credit-tag {
  color: #059669;
  font-weight: 600;
  background: #ecfdf5;
  padding: 2px 8px;
  border-radius: 6px;
}
</style>
