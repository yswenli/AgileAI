import { createRouter, createWebHistory } from 'vue-router'

import DashboardPage from './views/DashboardPage.vue'
import ModelsPage from './views/ModelsPage.vue'
import AgentsPage from './views/AgentsPage.vue'
import ChatPage from './views/ChatPage.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: DashboardPage },
    { path: '/models', component: ModelsPage },
    { path: '/agents', component: AgentsPage },
    { path: '/chat', component: ChatPage },
  ],
})
