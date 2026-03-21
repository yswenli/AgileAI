<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { NConfigProvider, NLayout, NLayoutContent, NMessageProvider, darkTheme } from 'naive-ui'

import StudioShell from './components/StudioShell.vue'
import { useStudioStore } from './stores/studio'

const store = useStudioStore()
const themeMode = ref<'light' | 'dark'>(localStorage.getItem('studio-theme') === 'dark' ? 'dark' : 'light')

const isDark = computed(() => themeMode.value === 'dark')

const themeOverrides = computed(() => ({
  common: {
    primaryColor: '#0f766e',
    primaryColorHover: '#14b8a6',
    primaryColorPressed: '#115e59',
    primaryColorSuppl: '#14b8a6',
    infoColor: '#2563eb',
    successColor: '#10b981',
    warningColor: '#d97706',
    errorColor: '#dc2626',
    borderRadius: '18px',
    cardColor: isDark.value ? '#0e1a23' : '#ffffff',
    bodyColor: isDark.value ? '#081117' : '#f6fbff',
    modalColor: isDark.value ? '#10202a' : '#ffffff',
    popoverColor: isDark.value ? '#10202a' : '#ffffff',
    textColorBase: isDark.value ? '#e8f4f8' : '#102034',
  },
}))

watch(
  themeMode,
  (value) => {
    document.documentElement.dataset.theme = value
    localStorage.setItem('studio-theme', value)
  },
  { immediate: true },
)

function toggleTheme() {
  themeMode.value = themeMode.value === 'dark' ? 'light' : 'dark'
}

onMounted(async () => {
  await store.bootstrap()
})
</script>

<template>
  <n-config-provider :theme="isDark ? darkTheme : null" :theme-overrides="themeOverrides">
    <n-message-provider>
      <n-layout embedded class="app-layout">
        <n-layout-content>
          <StudioShell :theme-mode="themeMode" @toggle-theme="toggleTheme">
            <router-view />
          </StudioShell>
        </n-layout-content>
      </n-layout>
    </n-message-provider>
  </n-config-provider>
</template>
