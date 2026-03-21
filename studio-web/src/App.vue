<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { NConfigProvider, NLayout, NLayoutContent, NMessageProvider, darkTheme } from 'naive-ui'

import StudioShell from './components/StudioShell.vue'
import { useStudioStore } from './stores/studio'

const store = useStudioStore()

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
    cardColor: '#0e1a23',
    bodyColor: '#081117',
    modalColor: '#10202a',
    popoverColor: '#10202a',
    textColorBase: '#e8f4f8',
  },
}))

onMounted(async () => {
  await store.bootstrap()
})
</script>

<template>
  <n-config-provider :theme="darkTheme" :theme-overrides="themeOverrides">
    <n-message-provider>
      <n-layout embedded class="app-layout">
        <n-layout-content>
          <StudioShell>
            <router-view />
          </StudioShell>
        </n-layout-content>
      </n-layout>
    </n-message-provider>
  </n-config-provider>
</template>
