import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  webServer: [
    {
      command: 'dotnet run --project src/AgileAI.Studio.Api/AgileAI.Studio.Api.csproj --launch-profile http',
      url: 'http://127.0.0.1:5117/api/overview',
      reuseExistingServer: true,
      timeout: 120_000,
      cwd: '..',
      env: {
        ASPNETCORE_URLS: 'http://127.0.0.1:5117',
      },
    },
    {
      command: 'npm run dev -- --host 127.0.0.1 --port 5173',
      url: 'http://127.0.0.1:5173',
      reuseExistingServer: true,
      timeout: 120_000,
    },
  ],
  use: {
    baseURL: 'http://127.0.0.1:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
})
