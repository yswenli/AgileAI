# SESSION_HANDOFF.md — AgileAI 当前交接状态

> 最后更新: 2026-03-29

---

## 1. 本次交付摘要

本轮工作主要集中在 AgileAI Studio 的可用性修复、聊天体验完善，以及 Agent 工具配置能力落地。

当前已完成并验证的结果：

- `dotnet build AgileAI.slnx` ✅
- `studio-web npm run build` ✅
- `npx playwright test tests/studio.spec.ts -g "agent create and edit can configure selected tools with default all checked"` ✅

---

## 2. 本轮已完成内容

### 2.1 Studio 聊天与会话体验修复

修改文件：

- `src/AgileAI.Studio.Api/Services/AgentExecutionService.cs`
- `src/AgileAI.Studio.Api/Services/ConversationService.cs`
- `studio-web/src/api/studio.ts`
- `studio-web/src/stores/studio.ts`
- `studio-web/src/styles.css`
- `studio-web/src/views/ChatPage.vue`

已完成：

- 修复消息角色枚举映射错误，恢复经典聊天布局：
  - 用户消息在右侧
  - assistant 消息在左侧
- 聊天输入支持：
  - `Enter` 发送
  - `Shift+Enter` 换行
  - 发送后立即清空输入框，失败时恢复
- Send 按钮对齐修复为右侧
- assistant 消息支持 Markdown 渲染：
  - 段落 / 列表 / 代码块 / 行内 code / blockquote / link
- 聊天头部改为：
  - Agent 名字单独显示
  - tag 显示 `provider · model`
- 右侧 session 列表新增 `New Session`
- session 列表显示：
  - 标题
  - 创建时间
  - message 数
- 后端 `/stream` 接口改为真实 SSE 增量输出：
  - `message-created`
  - `text-delta`
  - `usage`
  - `final-message`
  - `completed`
- 首轮对话后会话标题支持自动更新：
  - 优先尝试 LLM 生成
  - 失败时退回首条用户消息截断标题

### 2.2 Provider / Model 管理页修复

修改文件：

- `src/AgileAI.Studio.Api/Services/ModelCatalogService.cs`
- `studio-web/src/api/studio.ts`
- `studio-web/src/views/ModelsPage.vue`

已完成：

- 修复删除 provider / model 时的 500：
  - 删除 provider 前清理 `models -> agents -> conversations/messages`
  - 删除 model 前清理 `agents -> conversations/messages`
- 修复 provider 编辑页 `providerType` 数字枚举显示问题
- 修复 provider 编辑页 `OpenAI Compatible` 缺字段问题
- 修复切换 provider type 时表单值被粗暴覆盖的问题

### 2.3 Agent 工具选择能力落地

修改文件：

- `src/AgileAI.Studio.Api/Contracts/Models.cs`
- `src/AgileAI.Studio.Api/Data/StudioDbContext.cs`
- `src/AgileAI.Studio.Api/Domain/AgentToolSelection.cs`
- `src/AgileAI.Studio.Api/Infrastructure/StudioDbSeeder.cs`
- `src/AgileAI.Studio.Api/Program.cs`
- `src/AgileAI.Studio.Api/Services/AgentExecutionService.cs`
- `src/AgileAI.Studio.Api/Services/AgentService.cs`
- `src/AgileAI.Extensions.FileSystem/FileSystemToolRegistryFactory.cs`
- `studio-web/src/api/studio.ts`
- `studio-web/src/stores/studio.ts`
- `studio-web/src/types.ts`
- `studio-web/src/views/AgentsPage.vue`
- `studio-web/tests/studio.spec.ts`

已完成：

- Agent 新建 / 编辑支持配置所需工具
- 默认勾选所有工具
- 保存后会持久化已选工具
- 重新编辑时会正确回填
- 后端新增 `/api/agent-tools` 返回可选工具列表
- 运行时只向 agent 注入它已选择的工具
- 兼容旧 SQLite 库：启动时自动创建 `AgentToolSelections` 表
- Agent 弹窗已恢复为单栏流式布局
- Tools 配置默认折叠
- tools item 的灰色背景已移除

---

## 3. 当前运行方式

### 后端

```bash
dotnet run --project src/AgileAI.Studio.Api/AgileAI.Studio.Api.csproj
```

默认访问：

- `http://localhost:5117`

### 前端

```bash
cd studio-web
npm install
npm run dev -- --host 0.0.0.0 --port 5173
```

默认访问：

- `http://localhost:5173`

### 验证命令

```bash
dotnet build AgileAI.slnx

cd studio-web
npm run build
npx playwright test tests/studio.spec.ts -g "agent create and edit can configure selected tools with default all checked"
```

---

## 4. 当前已知情况

- Studio 主要前端/后端改动已经通过构建验证
- Agent 工具选择功能已经过 Playwright e2e 验证
- 当前环境里仍存在一类与 provider/runtime 配置有关的运行时问题：某些真实 provider 请求可能返回 `500/502`，这不属于本轮 Agent tools UI 功能本身的编译或表单链路错误

---

## 5. 下一步建议（待做）

### 5.1 排查真实 provider 的 500 / 502 问题

建议继续检查：

- provider type 与 base URL 是否匹配
- OpenAI / OpenAICompatible / AzureOpenAI 的 runtime 映射是否一致
- 当前实际 provider 是否支持同步 `/messages` 与流式 `/stream` 两条路径

### 5.2 Agent 执行本地命令能力设计

用户已经提出希望让 agent 可以执行本地命令，当前还**未实现**。建议按现有 tool 架构做，不要直接开放裸 shell。

建议方案：

- 新增受控工具，例如：`run_local_command`
- 通过现有 Agent tools 选择机制挂入 Studio
- 限制：
  - 命令白名单
  - 工作目录白名单
  - 超时
  - 输出长度截断
  - 默认禁止危险命令
- 返回结构化结果：
  - `exitCode`
  - `stdout`
  - `stderr`
  - `durationMs`
  - `timedOut`

最小落地顺序建议：

1. 先做只读/低风险命令白名单版
2. 再考虑人工确认、审计日志和更高权限命令

---

## 6. 关键文件清单

- `SESSION_HANDOFF.md`
- `src/AgileAI.Extensions.FileSystem/FileSystemToolRegistryFactory.cs`
- `src/AgileAI.Studio.Api/Contracts/Models.cs`
- `src/AgileAI.Studio.Api/Data/StudioDbContext.cs`
- `src/AgileAI.Studio.Api/Domain/AgentToolSelection.cs`
- `src/AgileAI.Studio.Api/Infrastructure/StudioDbSeeder.cs`
- `src/AgileAI.Studio.Api/Program.cs`
- `src/AgileAI.Studio.Api/Services/AgentExecutionService.cs`
- `src/AgileAI.Studio.Api/Services/AgentService.cs`
- `src/AgileAI.Studio.Api/Services/ConversationService.cs`
- `src/AgileAI.Studio.Api/Services/ModelCatalogService.cs`
- `studio-web/package.json`
- `studio-web/package-lock.json`
- `studio-web/src/api/studio.ts`
- `studio-web/src/stores/studio.ts`
- `studio-web/src/styles.css`
- `studio-web/src/types.ts`
- `studio-web/src/views/AgentsPage.vue`
- `studio-web/src/views/ChatPage.vue`
- `studio-web/src/views/ModelsPage.vue`
- `studio-web/tests/studio.spec.ts`

---

**交接人**: Sisyphus  
**日期**: 2026-03-29
