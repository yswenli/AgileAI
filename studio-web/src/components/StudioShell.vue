<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router'
import { NButton, NLayout, NLayoutSider, NTag } from 'naive-ui'

defineProps<{
  themeMode: 'light' | 'dark'
}>()

defineEmits<{
  toggleTheme: []
}>()

const route = useRoute()

const navItems = [
  { label: 'Overview', path: '/' },
  { label: 'Models', path: '/models' },
  { label: 'Agents', path: '/agents' },
  { label: 'Chat', path: '/chat' },
]
</script>

<template>
  <n-layout has-sider class="shell">
    <n-layout-sider bordered collapse-mode="width" :collapsed-width="0" :width="280" class="shell-sider">
      <div class="brand-panel">
        <div class="brand-mark">A</div>
        <div>
          <p class="eyebrow">AgileAI Product Lab</p>
          <h1>AgileAI.Studio</h1>
        </div>
      </div>

      <div class="status-card glass-card">
        <div>
          <p class="eyebrow">Version One</p>
          <h2>Model-driven AI workspace</h2>
        </div>
        <div class="shell-tags">
          <n-tag size="small" round type="success">Local-first</n-tag>
          <n-button size="small" secondary @click="$emit('toggleTheme')">
            {{ themeMode === 'dark' ? 'Light Mode' : 'Dark Mode' }}
          </n-button>
        </div>
      </div>

      <nav class="nav-list">
        <RouterLink
          v-for="item in navItems"
          :key="item.path"
          :to="item.path"
          class="nav-link"
          :class="{ active: route.path === item.path }"
        >
          <span>{{ item.label }}</span>
        </RouterLink>
      </nav>

      <div class="cta-card glass-card">
        <p class="eyebrow">Studio Flow</p>
        <p class="cta-copy">Add a model, shape an agent, then iterate in chat with a polished control center.</p>
        <RouterLink to="/models">
          <n-button type="primary" block secondary>Add your first model</n-button>
        </RouterLink>
      </div>
    </n-layout-sider>

    <section class="shell-main">
      <div class="ambient ambient-one"></div>
      <div class="ambient ambient-two"></div>
      <slot />
    </section>
  </n-layout>
</template>
