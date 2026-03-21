<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useMessage, NButton, NCard, NDataTable, NForm, NFormItem, NInput, NModal, NPopconfirm, NSelect, NSpace, NSwitch, NTag } from 'naive-ui'

import { useStudioStore } from '../stores/studio'
import type { ProviderConnectionPayload, ModelPayload } from '../api/studio'
import type { ModelItem, ProviderConnection } from '../types'

const store = useStudioStore()
const message = useMessage()

const showProviderModal = ref(false)
const showModelModal = ref(false)
const editingProvider = ref<ProviderConnection | null>(null)
const editingModel = ref<ModelItem | null>(null)

const providerForm = reactive<ProviderConnectionPayload>({
  name: '',
  providerType: 'OpenAI',
  apiKey: '',
  baseUrl: 'https://api.openai.com/v1/',
  endpoint: '',
  providerName: '',
  relativePath: 'chat/completions',
  apiKeyHeaderName: '',
  authMode: 'Bearer',
  apiVersion: '2024-02-01',
  isEnabled: true,
})

const modelForm = reactive<ModelPayload>({
  providerConnectionId: '',
  displayName: '',
  modelKey: '',
  supportsStreaming: true,
  supportsTools: true,
  supportsVision: false,
  isEnabled: true,
})

const providerOptions = [
  { label: 'OpenAI', value: 'OpenAI' },
  { label: 'OpenAI Compatible', value: 'OpenAICompatible' },
  { label: 'Azure OpenAI', value: 'AzureOpenAI' },
]

function applyProviderPreset(type: 'OpenAI' | 'OpenAICompatible' | 'AzureOpenAI') {
  if (type === 'OpenAI') {
    providerForm.baseUrl = 'https://api.openai.com/v1/'
    providerForm.providerName = ''
    providerForm.relativePath = 'chat/completions'
    providerForm.authMode = 'Bearer'
    providerForm.apiKeyHeaderName = ''
    providerForm.endpoint = ''
    return
  }

  if (type === 'OpenAICompatible') {
    providerForm.baseUrl = 'https://api.openai.com/v1/'
    providerForm.providerName = 'openapi'
    providerForm.relativePath = 'chat/completions'
    providerForm.authMode = 'Bearer'
    providerForm.apiKeyHeaderName = ''
    providerForm.endpoint = ''
    return
  }

  providerForm.endpoint = 'https://your-resource.openai.azure.com/'
  providerForm.baseUrl = ''
  providerForm.providerName = ''
  providerForm.relativePath = 'chat/completions'
  providerForm.authMode = 'Bearer'
}

const providerConnectionOptions = computed(() =>
  store.providerConnections.map((item) => ({ label: `${item.name} · ${item.providerType}`, value: item.id })),
)

const isProviderValid = computed(() => {
  if (!providerForm.name.trim() || !providerForm.apiKey.trim()) {
    return false
  }

  if (providerForm.providerType === 'AzureOpenAI') {
    return Boolean(providerForm.endpoint?.trim())
  }

  if (!providerForm.baseUrl?.trim()) {
    return false
  }

  if (providerForm.providerType === 'OpenAICompatible') {
    return Boolean(providerForm.providerName?.trim() && providerForm.relativePath?.trim())
  }

  return true
})

const isModelValid = computed(() =>
  Boolean(modelForm.providerConnectionId && modelForm.displayName.trim() && modelForm.modelKey.trim()),
)

const columns = [
  { title: 'Model', key: 'displayName' },
  { title: 'Provider', key: 'providerConnectionName' },
  { title: 'Runtime Key', key: 'modelKey' },
  {
    title: 'Capabilities',
    key: 'capabilities',
    render: (row: ModelItem) =>
      [row.supportsStreaming && 'Streaming', row.supportsTools && 'Tools', row.supportsVision && 'Vision']
        .filter(Boolean)
        .join(' · '),
  },
  {
    title: 'Status',
    key: 'isEnabled',
    render: (row: ModelItem) => (row.isEnabled ? 'Enabled' : 'Disabled'),
  },
]

function resetProviderForm() {
  Object.assign(providerForm, {
    name: '',
    providerType: 'OpenAI',
    apiKey: '',
    baseUrl: 'https://api.openai.com/v1/',
    endpoint: '',
    providerName: '',
    relativePath: 'chat/completions',
    apiKeyHeaderName: '',
    authMode: 'Bearer',
    apiVersion: '2024-02-01',
    isEnabled: true,
  })
  applyProviderPreset('OpenAI')
}

function resetModelForm() {
  Object.assign(modelForm, {
    providerConnectionId: store.providerConnections[0]?.id ?? '',
    displayName: '',
    modelKey: '',
    supportsStreaming: true,
    supportsTools: true,
    supportsVision: false,
    isEnabled: true,
  })
}

function openProviderModal(item?: ProviderConnection) {
  editingProvider.value = item ?? null
  if (item) {
    Object.assign(providerForm, {
      name: item.name,
      providerType: item.providerType,
      apiKey: '',
      baseUrl: item.baseUrl ?? '',
      endpoint: item.endpoint ?? '',
      providerName: item.providerName ?? '',
      relativePath: item.relativePath ?? 'chat/completions',
      apiKeyHeaderName: item.apiKeyHeaderName ?? '',
      authMode: item.authMode ?? 'Bearer',
      apiVersion: item.apiVersion ?? '2024-02-01',
      isEnabled: item.isEnabled,
    })
  } else {
    resetProviderForm()
  }
  showProviderModal.value = true
}

function openModelModal(item?: ModelItem) {
  editingModel.value = item ?? null
  if (item) {
    Object.assign(modelForm, {
      providerConnectionId: item.providerConnectionId,
      displayName: item.displayName,
      modelKey: item.modelKey,
      supportsStreaming: item.supportsStreaming,
      supportsTools: item.supportsTools,
      supportsVision: item.supportsVision,
      isEnabled: item.isEnabled,
    })
  } else {
    resetModelForm()
  }
  showModelModal.value = true
}

async function submitProvider() {
  if (!isProviderValid.value) {
    message.warning('Complete the provider fields before saving.')
    return
  }

  try {
    if (editingProvider.value) {
      await store.updateProviderConnection(editingProvider.value.id, providerForm)
      message.success('Provider connection updated')
    } else {
      await store.createProviderConnection(providerForm)
      message.success('Provider connection created')
    }
    showProviderModal.value = false
    resetProviderForm()
  } catch (error) {
    message.error((error as Error).message)
  }
}

async function submitModel() {
  if (!isModelValid.value) {
    message.warning('Choose a provider and complete the model fields.')
    return
  }

  try {
    if (editingModel.value) {
      await store.updateModel(editingModel.value.id, modelForm)
      message.success('Model updated')
    } else {
      await store.createModel(modelForm)
      message.success('Model created')
    }
    showModelModal.value = false
    resetModelForm()
  } catch (error) {
    message.error((error as Error).message)
  }
}

async function handleDeleteModel(id: string) {
  await store.deleteModel(id)
  message.success('Model deleted')
}

async function handleDeleteProvider(id: string) {
  await store.deleteProviderConnection(id)
  message.success('Provider connection deleted')
}

async function handleTestModel(id: string) {
  const result = await store.testModel(id)
  message.info(result.message)
}
</script>

<template>
  <section class="page">
    <div class="page-header">
      <div>
        <p class="eyebrow">Model catalog</p>
        <h2 class="page-title">Connect providers and publish the models your agents can use.</h2>
      </div>
      <n-space>
        <n-button secondary type="primary" @click="openProviderModal()">New connection</n-button>
        <n-button type="primary" @click="openModelModal()">New model</n-button>
      </n-space>
    </div>

    <div class="grid-two">
      <n-card class="glass-card" embedded>
        <template #header>
          <div class="section-heading">
            <div>
              <p class="eyebrow">Provider connections</p>
              <h3>Credentials and endpoints</h3>
            </div>
          </div>
        </template>
        <div class="list-stack" data-testid="provider-list">
          <article v-for="item in store.providerConnections" :key="item.id" class="list-row list-row-rich">
            <div>
              <strong>{{ item.name }}</strong>
              <p>{{ item.providerType }} · {{ item.apiKeyPreview }}</p>
            </div>
            <div class="row-actions">
              <n-tag :type="item.isEnabled ? 'success' : 'warning'">{{ item.isEnabled ? 'Enabled' : 'Disabled' }}</n-tag>
              <n-button text @click="openProviderModal(item)">Edit</n-button>
              <n-popconfirm @positive-click="handleDeleteProvider(item.id)">
                <template #trigger>
                  <n-button text type="error" :loading="store.deletingProviderIds.includes(item.id)">Delete</n-button>
                </template>
                Delete this provider connection?
              </n-popconfirm>
            </div>
          </article>
        </div>
      </n-card>

      <n-card class="glass-card" embedded>
        <template #header>
          <div class="section-heading">
            <div>
              <p class="eyebrow">Model inventory</p>
              <h3>Available runtime targets</h3>
            </div>
          </div>
        </template>
        <n-data-table :columns="columns" :data="store.models" :bordered="false" />
        <div class="table-actions">
          <article v-for="item in store.models" :key="item.id" class="inline-card" data-testid="model-card">
            <div>
              <strong>{{ item.displayName }}</strong>
              <p>{{ item.providerConnectionName }} · {{ item.modelKey }}</p>
            </div>
            <n-space>
              <n-button text :loading="store.validatingModelIds.includes(item.id)" @click="handleTestModel(item.id)">Test</n-button>
              <n-button text @click="openModelModal(item)">Edit</n-button>
              <n-popconfirm @positive-click="handleDeleteModel(item.id)">
                <template #trigger>
                  <n-button text type="error" :loading="store.deletingModelIds.includes(item.id)">Delete</n-button>
                </template>
                Delete this model?
              </n-popconfirm>
            </n-space>
          </article>
        </div>
      </n-card>
    </div>

    <n-modal v-model:show="showProviderModal" preset="card" title="Provider connection" class="modal-shell">
      <n-form label-placement="top">
        <n-form-item label="Name"><n-input v-model:value="providerForm.name" data-testid="provider-name-input" /></n-form-item>
        <n-form-item label="Provider type"><n-select v-model:value="providerForm.providerType" :options="providerOptions" @update:value="applyProviderPreset" /></n-form-item>
        <n-form-item label="API key"><n-input v-model:value="providerForm.apiKey" type="password" show-password-on="click" data-testid="provider-key-input" /></n-form-item>
        <n-form-item v-if="providerForm.providerType !== 'AzureOpenAI'" label="Base URL"><n-input v-model:value="providerForm.baseUrl" /></n-form-item>
        <n-form-item v-if="providerForm.providerType === 'AzureOpenAI'" label="Endpoint"><n-input v-model:value="providerForm.endpoint" /></n-form-item>
        <n-form-item v-if="providerForm.providerType === 'OpenAICompatible'" label="Runtime provider name"><n-input v-model:value="providerForm.providerName" placeholder="openapi" /></n-form-item>
        <n-form-item v-if="providerForm.providerType === 'OpenAICompatible'" label="Relative path"><n-input v-model:value="providerForm.relativePath" /></n-form-item>
        <n-form-item v-if="providerForm.providerType === 'OpenAICompatible'" label="Tip">
          <n-input
            :value="'To use a real OpenAI-compatible endpoint such as the OpenCode GPT-5.4 provider, set Base URL, Provider Name, API key, and the model key exactly as required by that service.'"
            type="textarea"
            readonly
            :autosize="{ minRows: 3, maxRows: 4 }"
          />
        </n-form-item>
        <n-form-item label="Enabled"><n-switch v-model:value="providerForm.isEnabled" /></n-form-item>
        <n-space justify="end"><n-button @click="showProviderModal = false">Cancel</n-button><n-button type="primary" :disabled="!isProviderValid" data-testid="save-provider" @click="submitProvider">Save</n-button></n-space>
      </n-form>
    </n-modal>

    <n-modal v-model:show="showModelModal" preset="card" title="Model" class="modal-shell">
      <n-form label-placement="top">
        <n-form-item label="Provider connection"><n-select v-model:value="modelForm.providerConnectionId" :options="providerConnectionOptions" /></n-form-item>
        <n-form-item label="Display name"><n-input v-model:value="modelForm.displayName" data-testid="model-display-name-input" /></n-form-item>
        <n-form-item label="Model key / deployment"><n-input v-model:value="modelForm.modelKey" placeholder="gpt-4o-mini" data-testid="model-key-input" /></n-form-item>
        <n-form-item label="Streaming"><n-switch v-model:value="modelForm.supportsStreaming" /></n-form-item>
        <n-form-item label="Tools"><n-switch v-model:value="modelForm.supportsTools" /></n-form-item>
        <n-form-item label="Vision"><n-switch v-model:value="modelForm.supportsVision" /></n-form-item>
        <n-form-item label="Enabled"><n-switch v-model:value="modelForm.isEnabled" /></n-form-item>
        <n-space justify="end"><n-button @click="showModelModal = false">Cancel</n-button><n-button type="primary" :disabled="!isModelValid" data-testid="save-model" @click="submitModel">Save</n-button></n-space>
      </n-form>
    </n-modal>
  </section>
</template>
