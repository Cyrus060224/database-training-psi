<template>
  <div class="goods-container">
    <el-card shadow="never" class="table-card">
      <!-- 搜索与筛选工具栏 -->
      <div class="filter-toolbar">
        <div class="left-filters">
          <el-input
            v-model="queryParams.keyword"
            placeholder="输入商品名称 / 编码搜索"
            clearable
            style="width: 240px"
            @keyup.enter="handleSearch"
          />

          <el-select
            v-model="queryParams.categoryId"
            placeholder="全部分类"
            clearable
            style="width: 160px"
            @change="handleSearch"
          >
            <el-option
              v-for="c in categories"
              :key="c.categoryId"
              :label="c.categoryName"
              :value="c.categoryId"
            />
          </el-select>

          <el-checkbox v-model="queryParams.alertOnly" label="⚠️ 仅看库存预警商品" @change="handleSearch" />

          <el-button type="primary" icon="Search" @click="handleSearch">查询</el-button>
          <el-button icon="Refresh" @click="resetSearch">重置</el-button>
        </div>

        <div class="right-actions">
          <el-button type="success" icon="Plus" @click="openDialog()">新增商品主数据</el-button>
        </div>
      </div>

      <!-- 数据表格 -->
      <el-table :data="tableData" v-loading="loading" stripe style="width: 100%">
        <el-table-column prop="goodsCode" label="商品编码" width="130" />
        <el-table-column prop="goodsName" label="商品名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="categoryName" label="类别" width="110">
          <template #default="{ row }">
            <el-tag size="small">{{ row.categoryName }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="specification" label="规格型号" width="140" show-overflow-tooltip />
        <el-table-column prop="costPrice" label="参考进价" width="100">
          <template #default="{ row }">¥{{ row.costPrice }}</template>
        </el-table-column>
        <el-table-column prop="retailPrice" label="销售售价" width="100">
          <template #default="{ row }">
            <strong style="color: #67c23a;">¥{{ row.retailPrice }}</strong>
          </template>
        </el-table-column>
        <el-table-column prop="currentStock" label="当前库存量" width="120">
          <template #default="{ row }">
            <el-tag :type="row.isAlert ? 'danger' : 'info'" effect="dark">
              {{ row.currentStock }} {{ row.unit }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="minStockAlert" label="最低安全库存" width="110">
          <template #default="{ row }">{{ row.minStockAlert }} {{ row.unit }}</template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="openDialog(row)">编辑</el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页栏 -->
      <div class="pagination-bar">
        <el-pagination
          v-model:current-page="queryParams.pageIndex"
          v-model:page-size="queryParams.pageSize"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next, jumper"
          :total="total"
          @size-change="fetchList"
          @current-change="fetchList"
        />
      </div>
    </el-card>

    <!-- 新增 / 编辑弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formData.goodsId ? '编辑商品档案' : '新增商品主数据'"
      width="560px"
      destroy-on-close
    >
      <el-form :model="formData" :rules="rules" ref="formRef" label-width="110px">
        <el-form-item label="商品名称" prop="goodsName">
          <el-input v-model="formData.goodsName" placeholder="例如：ThinkPad 笔记本电脑" />
        </el-form-item>

        <el-form-item label="商品分类" prop="categoryId">
          <el-select v-model="formData.categoryId" placeholder="请选择类别" style="width: 100%">
            <el-option
              v-for="c in categories"
              :key="c.categoryId"
              :label="c.categoryName"
              :value="c.categoryId"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="商品编码" prop="goodsCode">
          <el-input v-model="formData.goodsCode" placeholder="留空则自动按时间戳生成" />
        </el-form-item>

        <el-form-item label="规格型号">
          <el-input v-model="formData.specification" placeholder="例如：i7/16G/512G 黑色" />
        </el-form-item>

        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="计量单位">
              <el-input v-model="formData.unit" placeholder="件 / 台 / 盒 / 箱" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="商品条形码">
              <el-input v-model="formData.barcode" placeholder="69010001" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="参考进价(¥)" prop="costPrice">
              <el-input-number v-model="formData.costPrice" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="销售售价(¥)" prop="retailPrice">
              <el-input-number v-model="formData.retailPrice" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="12">
          <el-col :span="12">
            <el-form-item label="安全预警值" prop="minStockAlert">
              <el-input-number v-model="formData.minStockAlert" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="库存上限">
              <el-input-number v-model="formData.maxStockAlert" :min="formData.minStockAlert" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">保存提交</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { reactive, ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getGoodsListApi, getCategoriesApi, saveGoodsApi, deleteGoodsApi } from '../api/psi'

const loading = ref(false)
const submitLoading = ref(false)
const tableData = ref([])
const total = ref(0)
const categories = ref([])

const queryParams = reactive({
  keyword: '',
  categoryId: null,
  alertOnly: false,
  pageIndex: 1,
  pageSize: 10
})

const dialogVisible = ref(false)
const formRef = ref(null)
const formData = reactive({
  goodsId: 0,
  goodsCode: '',
  goodsName: '',
  categoryId: 1,
  barcode: '',
  specification: '',
  unit: '件',
  costPrice: 0,
  retailPrice: 0,
  minStockAlert: 10,
  maxStockAlert: 1000
})

const rules = {
  goodsName: [{ required: true, message: '商品名称不能为空', trigger: 'blur' }],
  categoryId: [{ required: true, message: '请选择商品类别', trigger: 'change' }],
  costPrice: [{ required: true, message: '进价不能为空', trigger: 'blur' }],
  retailPrice: [{ required: true, message: '售价不能为空', trigger: 'blur' }]
}

const fetchCategories = async () => {
  const res = await getCategoriesApi()
  if (res.code === 200) categories.value = res.data
}

const fetchList = async () => {
  loading.value = true
  try {
    const res = await getGoodsListApi(queryParams)
    if (res.code === 200) {
      tableData.value = res.data.items
      total.value = res.data.total
    }
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  queryParams.pageIndex = 1
  fetchList()
}

const resetSearch = () => {
  queryParams.keyword = ''
  queryParams.categoryId = null
  queryParams.alertOnly = false
  handleSearch()
}

const openDialog = (row = null) => {
  if (row) {
    Object.assign(formData, row)
  } else {
    Object.assign(formData, {
      goodsId: 0,
      goodsCode: '',
      goodsName: '',
      categoryId: categories.value[0]?.categoryId || 1,
      barcode: '',
      specification: '',
      unit: '件',
      costPrice: 0,
      retailPrice: 0,
      minStockAlert: 10,
      maxStockAlert: 1000
    })
  }
  dialogVisible.value = true
}

const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitLoading.value = true
    try {
      const res = await saveGoodsApi(formData)
      if (res.code === 200) {
        ElMessage.success(res.message || '保存成功')
        dialogVisible.value = false
        fetchList()
      }
    } catch (e) {
      console.error(e)
    } finally {
      submitLoading.value = false
    }
  })
}

const handleDelete = (row) => {
  ElMessageBox.confirm(`确定要删除商品【${row.goodsName}】吗？`, '删除确认', {
    type: 'warning'
  }).then(async () => {
    const res = await deleteGoodsApi(row.goodsId)
    if (res.code === 200) {
      ElMessage.success('删除成功')
      fetchList()
    }
  })
}

onMounted(() => {
  fetchCategories()
  fetchList()
})
</script>

<style scoped>
.table-card {
  border-radius: 8px;
}

.filter-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  margin-bottom: 16px;
}

.left-filters {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.pagination-bar {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
</style>
