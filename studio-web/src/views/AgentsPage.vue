<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useMessage, NButton, NCard, NForm, NFormItem, NInput, NInputNumber, NList, NListItem, NModal, NPopconfirm, NSelect, NSwitch, NTag } from 'naive-ui'

import { useStudioStore } from '../stores/studio'
import type { AgentPayload } from '../api/studio'
import type { AgentItem } from '../types'

const store = useStudioStore()
const message = useMessage()

const showModal = ref(false)
const editing = ref<AgentItem | null>(null)

const form = reactive<AgentPayload>({
  studioModelId: '',
  name: '',
  description: '',
  systemPrompt: '',
  temperature: 0.6,
  maxTokens: 2048,
  enableSkills: false,
  isPinned: false,
})

const modelOptions = computed(() =>
  store.models.map((item) => ({ label: `${item.displayName} · ${item.providerConnectionName}`, value: item.id })),
)

const isFormValid = computed(() => {
  return Boolean(
    form.studioModelId &&
      form.name.trim() &&
      form.description.trim() &&
      form.systemPrompt.trim() &&
      form.maxTokens > 0,
  )
})

function resetForm() {
  Object.assign(form, {
    studioModelId: store.models[0]?.id ?? '',
    name: '',
    description: '',
    systemPrompt: '',
    temperature: 0.6,
    maxTokens: 2048,
    enableSkills: false,
    isPinned: false,
  })
}

function openModal(item?: AgentItem) {
  editing.value = item ?? null
  if (item) {
    Object.assign(form, {
      studioModelId: item.studioModelId,
      name: item.name,
      description: item.description,
      systemPrompt: item.systemPrompt,
      temperature: item.temperature,
      maxTokens: item.maxTokens,
      enableSkills: item.enableSkills,
      isPinned: item.isPinned,
    })
  } else {
    resetForm()
  }
  showModal.value = true
}

async function submit() {
  if (!isFormValid.value) {
    message.warning('Complete the agent fields before saving.')
    return
  }

  try {
    if (editing.value) {
      await store.updateAgent(editing.value.id, form)
      message.success('Agent updated')
    } else {
      await store.createAgent(form)
      message.success('Agent created')
    }
    showModal.value = false
  } catch (error) {
    message.error((error as Error).message)
  }
}

async function removeAgent(id: string) {
  await store.deleteAgent(id)
  message.success('Agent deleted')
}
</script>

<template>
  <section class="page">
    <div class="page-header">
      <div>
        <p class="eyebrow">Agent studio</p>
        <h2 class="page-title">Compose reusable assistants with strong prompts and tuned defaults.</h2>
      </div>
      <n-button type="primary" data-testid="create-agent" @click="openModal()">Create agent</n-button>
    </div>

    <div class="grid-two">
      <n-card class="glass-card" embedded>
        <template #header>
          <div class="section-heading">
            <div>
              <p class="eyebrow">Roster</p>
              <h3>Agent list</h3>
            </div>
          </div>
        </template>
        <n-list hoverable clickable data-testid="agent-list">
          <n-list-item v-for="agent in store.agents" :key="agent.id">
            <div class="agent-card">
              <div>
                <div class="agent-headline">
                  <strong>{{ agent.name }}</strong>
                  <n-tag v-if="agent.isPinned" type="success" size="small">Pinned</n-tag>
                </div>
                <p>{{ agent.description }}</p>
                <span>{{ agent.modelDisplayName }} · {{ agent.runtimeModelId }}</span>
              </div>
              <div class="row-actions">
                <n-button text @click="openModal(agent)">Edit</n-button>
                <n-popconfirm @positive-click="removeAgent(agent.id)">
                  <template #trigger>
                    <n-button text type="error" :loading="store.deletingAgentIds.includes(agent.id)">Delete</n-button>
                  </template>
                  Delete this agent?
                </n-popconfirm>
              </div>
            </div>
          </n-list-item>
        </n-list>
      </n-card>

      <n-card class="glass-card agent-preview" embedded>
        <template #header>
          <div class="section-heading">
            <div>
              <p class="eyebrow">Prompt strategy</p>
              <h3>What makes a great v1 agent</h3>
            </div>
          </div>
        </template>
        <div class="tip-stack">
          <article class="inline-card">
            <strong>Identity first</strong>
            <p>Give the agent a clear role, tone, and boundary so outputs stay consistent.</p>
          </article>
          <article class="inline-card">
            <strong>Model alignment</strong>
            <p>Pair lightweight agents with cheaper models and reserve premium models for harder tasks.</p>
          </article>
          <article class="inline-card">
            <strong>Stay editable</strong>
            <p>Keep prompts structured enough that your team can iterate on them quickly in the UI.</p>
          </article>
        </div>
      </n-card>
    </div>

    <n-modal v-model:show="showModal" preset="card" title="Agent" class="modal-shell modal-wide">
      <n-form label-placement="top">
        <n-form-item label="Name"><n-input v-model:value="form.name" data-testid="agent-name-input" /></n-form-item>
        <n-form-item label="Description"><n-input v-model:value="form.description" type="textarea" :autosize="{ minRows: 2, maxRows: 4 }" data-testid="agent-description-input" /></n-form-item>
        <n-form-item label="Model">
          <n-select v-model:value="form.studioModelId" :options="modelOptions" />
        </n-form-item>
        <n-form-item label="System prompt"><n-input v-model:value="form.systemPrompt" type="textarea" :autosize="{ minRows: 6, maxRows: 12 }" data-testid="agent-prompt-input" /></n-form-item>
        <div class="form-grid">
          <n-form-item label="Temperature"><n-input-number v-model:value="form.temperature" :min="0" :max="2" :step="0.1" /></n-form-item>
          <n-form-item label="Max tokens"><n-input-number v-model:value="form.maxTokens" :min="256" :max="8192" :step="128" /></n-form-item>
        </div>
        <div class="form-grid">
          <n-form-item label="Enable skills"><n-switch v-model:value="form.enableSkills" /></n-form-item>
          <n-form-item label="Pin on top"><n-switch v-model:value="form.isPinned" /></n-form-item>
        </div>
        <div class="modal-actions"><n-button @click="showModal = false">Cancel</n-button><n-button type="primary" :disabled="!isFormValid" data-testid="save-agent" @click="submit">Save</n-button></div>
      </n-form>
    </n-modal>
  </section>
</template>
