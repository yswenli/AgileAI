export type ProviderType = 'OpenAI' | 'OpenAICompatible' | 'AzureOpenAI'

export interface ProviderConnection {
  id: string
  name: string
  providerType: ProviderType
  apiKeyPreview: string
  baseUrl?: string | null
  endpoint?: string | null
  providerName?: string | null
  relativePath?: string | null
  apiKeyHeaderName?: string | null
  authMode?: string | null
  apiVersion?: string | null
  isEnabled: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface ModelItem {
  id: string
  providerConnectionId: string
  providerConnectionName: string
  providerType: ProviderType
  displayName: string
  modelKey: string
  supportsStreaming: boolean
  supportsTools: boolean
  supportsVision: boolean
  isEnabled: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface AgentItem {
  id: string
  studioModelId: string
  name: string
  description: string
  systemPrompt: string
  temperature: number
  maxTokens: number
  enableSkills: boolean
  isPinned: boolean
  modelDisplayName: string
  runtimeModelId: string
  createdAtUtc: string
  updatedAtUtc: string
}

export interface ConversationItem {
  id: string
  agentId: string
  agentName: string
  title: string
  createdAtUtc: string
  updatedAtUtc: string
  messageCount: number
}

export type MessageRole = 'System' | 'User' | 'Assistant' | 'Tool'

export interface MessageItem {
  id: string
  conversationId: string
  role: MessageRole
  content: string
  isStreaming: boolean
  finishReason?: string | null
  inputTokens?: number | null
  outputTokens?: number | null
  createdAtUtc: string
}

export interface ChatStreamStart {
  conversation: ConversationItem
  userMessage: MessageItem
  assistantMessage: MessageItem
}

export interface Overview {
  modelCount: number
  agentCount: number
  conversationCount: number
  recentConversations: ConversationItem[]
}
