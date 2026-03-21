<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useMessage, NButton, NCard, NEmpty, NInput, NList, NListItem, NSpin, NTag } from 'naive-ui'

import { useStudioStore } from '../stores/studio'

const store = useStudioStore()
const message = useMessage()

const prompt = ref('')
const selectedAgentId = ref('')
const isSending = ref(false)
const transcriptRef = ref<HTMLElement | null>(null)

const activeConversation = computed(() => store.activeConversation)
const activeMessages = computed(() => store.activeMessages)
const activeAgent = computed(() => store.agents.find((item) => item.id === activeConversation.value?.agentId) ?? null)

watch(
  activeMessages,
  async () => {
    await nextTick()
    if (transcriptRef.value) {
      transcriptRef.value.scrollTop = transcriptRef.value.scrollHeight
    }
  },
  { deep: true },
)

watch(
  () => store.agents,
  (agents) => {
    if (!selectedAgentId.value && agents.length > 0) {
      selectedAgentId.value = agents[0].id
    }
  },
  { immediate: true },
)

async function createConversationForAgent() {
  if (!selectedAgentId.value) {
    message.warning('Create a model and agent first')
    return
  }

  const agent = store.agents.find((item) => item.id === selectedAgentId.value)
  const conversation = await store.createConversation(selectedAgentId.value, agent ? `${agent.name} session` : 'New chat')
  await store.fetchMessages(conversation.id)
}

async function selectConversation(id: string) {
  await store.fetchMessages(id)
}

async function submitPrompt() {
  if (!prompt.value.trim()) {
    return
  }

  if (!activeConversation.value) {
    await createConversationForAgent()
  }

  if (!store.activeConversationId) {
    return
  }

  isSending.value = true
  try {
    await store.streamMessage(store.activeConversationId, prompt.value)
    await store.fetchMessages(store.activeConversationId)
    prompt.value = ''
  } catch (error) {
    message.error((error as Error).message)
  } finally {
    isSending.value = false
  }
}
</script>

<template>
  <section class="page page-chat">
    <div class="chat-layout">
      <n-card class="glass-card chat-side" embedded>
        <template #header>
          <div class="section-heading">
            <div>
              <p class="eyebrow">Session control</p>
              <h3>Conversations</h3>
            </div>
          </div>
        </template>

        <div class="inline-card chat-create">
          <label class="field-label">Start with agent</label>
          <select v-model="selectedAgentId" class="native-select" data-testid="agent-select">
            <option v-for="agent in store.agents" :key="agent.id" :value="agent.id">{{ agent.name }}</option>
          </select>
          <n-button type="primary" block data-testid="new-conversation" @click="createConversationForAgent">New conversation</n-button>
        </div>

        <n-list>
          <n-list-item v-for="conversation in store.conversations" :key="conversation.id">
            <button class="conversation-link" :class="{ active: conversation.id === store.activeConversationId }" :data-testid="`conversation-${conversation.id}`" @click="selectConversation(conversation.id)">
              <strong>{{ conversation.title }}</strong>
              <span>{{ conversation.agentName }} · {{ conversation.messageCount }} messages</span>
            </button>
          </n-list-item>
        </n-list>
      </n-card>

      <n-card class="glass-card chat-stage" embedded>
        <template #header>
          <div class="section-heading">
            <div>
              <p class="eyebrow">Live chat</p>
              <h3>{{ activeConversation?.title ?? 'No active conversation' }}</h3>
            </div>
            <div class="chat-heading-meta">
              <n-tag v-if="activeConversation" type="success">{{ activeConversation.agentName }}</n-tag>
              <n-tag v-if="activeAgent" type="info" bordered>{{ activeAgent.modelDisplayName }}</n-tag>
            </div>
          </div>
        </template>

        <div v-if="activeMessages.length" ref="transcriptRef" class="message-stack" data-testid="chat-transcript">
          <article v-for="item in activeMessages" :key="item.id" class="message-bubble" :class="item.role.toLowerCase()" :data-testid="`message-${item.role.toLowerCase()}`">
            <span class="message-role">{{ item.role }}</span>
            <p>{{ item.content }}</p>
            <div v-if="item.role === 'Assistant'" class="message-meta">
              <span v-if="item.inputTokens || item.outputTokens">{{ item.inputTokens ?? 0 }} in · {{ item.outputTokens ?? 0 }} out</span>
              <n-spin v-if="item.isStreaming" size="small" />
            </div>
          </article>
        </div>
        <n-empty v-else description="No messages yet. Start a conversation to talk with your agent." class="chat-empty" />

        <div class="composer glass-card">
          <n-input
            v-model:value="prompt"
            type="textarea"
            placeholder="Ask your agent to plan, write, summarize, or analyze..."
            :autosize="{ minRows: 3, maxRows: 6 }"
            data-testid="chat-input"
          />
          <div class="composer-actions">
            <p>
              Agent memory is persisted per conversation.
              <span v-if="store.isStreaming"> Streaming response in progress...</span>
              <span v-if="store.streamError"> {{ store.streamError }}</span>
            </p>
            <n-button type="primary" :loading="isSending" data-testid="send-message" @click="submitPrompt">Send</n-button>
          </div>
        </div>
      </n-card>
    </div>
  </section>
</template>
