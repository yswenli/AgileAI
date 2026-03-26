<template>
  <section class="page page-chat">
    <div class="chat-layout">
      <!-- Left Sidebar: Agents -->
      <n-card class="glass-card chat-side" embedded>
        <template #header>
          <div class="section-heading">
            <div>
              <p class="eyebrow">Agents</p>
              <h3>Select an agent</h3>
            </div>
          </div>
        </template>

        <div class="agent-list">
          <n-empty v-if="store.agents.length === 0" description="No agents" size="small" />
          <n-button
            v-for="agent in store.agents"
            :key="agent.id"
            text
            block
            class="agent-item"
            :class="{ active: selectedAgentId === agent.id }"
            @click="selectAgent(agent.id)"
          >
            <n-space align="center" :size="8" style="width: 100%; justify-content: flex-start;">
              <n-icon size="16"><boat-outline /></n-icon>
              <n-ellipsis style="max-width: 140px;">{{ agent.name }}</n-ellipsis>
              <n-tag v-if="agent.isPinned" type="success" size="tiny" round style="flex-shrink: 0;">Pinned</n-tag>
            </n-space>
          </n-button>
        </div>
      </n-card>

      <!-- Main Chat Area -->
      <n-card class="glass-card chat-stage" embedded>
        <template #header>
          <div class="section-heading">
            <div>
              <p class="eyebrow">Live chat</p>
              <h3>{{ activeConversation?.title ?? 'New chat' }}</h3>
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
              <span> Workspace file tools are available for repository files.</span>
              <span v-if="store.isStreaming"> Streaming response in progress...</span>
              <span v-if="store.streamError"> {{ store.streamError }}</span>
            </p>
            <n-button type="primary" :loading="isSending" data-testid="send-message" @click="submitPrompt">Send</n-button>
          </div>
        </div>
      </n-card>

      <!-- Right Sidebar: Conversation History -->
      <n-card class="glass-card chat-side" embedded>
        <template #header>
          <div class="section-heading">
            <div>
              <p class="eyebrow">History</p>
              <h3>Conversations</h3>
            </div>
          </div>
        </template>

        <div class="conversation-list">
          <n-empty v-if="sortedConversations.length === 0" description="No conversations" size="small" />
          <n-list hoverable clickable data-testid="conversation-list">
            <n-list-item v-for="conv in sortedConversations" :key="conv.id">
              <button
                class="conversation-link"
                :class="{ active: conv.id === store.activeConversationId }"
                :data-testid="`conversation-${conv.id}`"
                @click="selectConversation(conv.id)"
              >
                <strong>{{ conv.title }}</strong>
                <span>{{ conv.agentName }} · {{ conv.messageCount }} messages</span>
              </button>
            </n-list-item>
          </n-list>
        </div>
      </n-card>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useMessage, NButton, NCard, NEmpty, NInput, NList, NListItem, NSpin, NTag, NIcon, NEllipsis, NSpace } from 'naive-ui'
import { BoatOutline } from '@vicons/ionicons5'
import { useStudioStore } from '../stores/studio'

const route = useRoute()
const store = useStudioStore()
const message = useMessage()

const prompt = ref('')
const selectedAgentId = ref('')
const isSending = ref(false)
const transcriptRef = ref<HTMLElement | null>(null)

const activeConversation = computed(() => store.activeConversation)
const activeMessages = computed(() => store.activeMessages)
const activeAgent = computed(() => {
  if (activeConversation.value?.agentId) {
    return store.agents.find((item) => item.id === activeConversation.value?.agentId) ?? null
  }

  if (selectedAgentId.value) {
    return store.agents.find((item) => item.id === selectedAgentId.value) ?? null
  }

  return null
})

// Filter conversations by selected agent
const sortedConversations = computed(() => {
  if (!selectedAgentId.value) return []
  return [...store.conversations]
    .filter(conv => conv.agentId === selectedAgentId.value)
    .sort((a, b) => new Date(b.updatedAtUtc).getTime() - new Date(a.updatedAtUtc).getTime())
})

function getLatestConversationForAgent(agentId: string) {
  return [...store.conversations]
    .filter((conv) => conv.agentId === agentId)
    .sort((a, b) => new Date(b.updatedAtUtc).getTime() - new Date(a.updatedAtUtc).getTime())[0] ?? null
}

async function syncAgentConversation(agentId: string) {
  selectedAgentId.value = agentId
  const latestConversation = getLatestConversationForAgent(agentId)

  if (latestConversation) {
    await store.fetchMessages(latestConversation.id)
    return
  }

  store.activeConversationId = ''
}

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

// Watch for route query changes (when navigating from Agents page)
watch(
  () => route.query.agentId,
  async (agentId) => {
    if (agentId && typeof agentId === 'string') {
      selectedAgentId.value = agentId
      if (store.conversations.length > 0) {
        await syncAgentConversation(agentId)
      }
    }
  },
  { immediate: true },
)

watch(
  () => store.conversations,
  async (conversations) => {
    if (!selectedAgentId.value || conversations.length === 0) {
      return
    }

    if (!store.activeConversation || store.activeConversation.agentId !== selectedAgentId.value) {
      await syncAgentConversation(selectedAgentId.value)
    }
  },
  { deep: true },
)

// Watch for agents loading
watch(
  () => store.agents,
  async (agents) => {
    if (!selectedAgentId.value && agents.length > 0) {
      await syncAgentConversation(agents[0].id)
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
  const conversation = store.conversations.find((item) => item.id === id)
  if (conversation) {
    selectedAgentId.value = conversation.agentId
  }
  await store.fetchMessages(id)
}

async function selectAgent(agentId: string) {
  await syncAgentConversation(agentId)
}

async function submitPrompt() {
  if (!prompt.value.trim()) {
    return
  }

  if (!selectedAgentId.value) {
    message.warning('Select an agent first')
    return
  }

  if (activeConversation.value && activeConversation.value.agentId !== selectedAgentId.value) {
    await syncAgentConversation(selectedAgentId.value)
  }

  if (!store.activeConversation || store.activeConversation.agentId !== selectedAgentId.value) {
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

<style scoped>
.conversation-link {
  display: block;
  width: 100%;
  padding: 8px 12px;
  text-align: left;
  background: transparent;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.2s;
}

.conversation-link:hover {
  background: var(--hover-color);
}

.conversation-link.active {
  background: var(--primary-color-suppl);
  color: var(--primary-color);
}

.conversation-link strong {
  display: block;
  margin-bottom: 4px;
}

.conversation-link span {
  font-size: 12px;
  opacity: 0.7;
}

.agent-list,
.conversation-list {
  max-height: 400px;
  overflow-y: auto;
}

.agent-item {
  width: 100%;
  justify-content: flex-start;
  padding: 8px 12px;
  margin-bottom: 4px;
  border-radius: 6px;
}

.agent-item:hover {
  background: var(--hover-color);
}

.agent-item.active {
  background: var(--primary-color-suppl);
  color: var(--primary-color);
}
</style>
