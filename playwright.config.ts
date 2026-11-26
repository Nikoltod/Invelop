import { defineConfig, devices } from '@playwright/test';
import { mkdirSync, rmSync } from 'node:fs';
import { join } from 'node:path';

/**
 * The end-to-end run is fully isolated from whatever the reviewer has running locally:
 * its own API port, its own SPA port, and its own throwaway SQLite file.
 *
 * Nothing here touches `src/Invelop.Contacts.Api/contacts.db`, so running the tests can never
 * disturb - or depend on - the data you created by hand while trying the app.
 */
const E2E_DIR = join(__dirname, 'e2e', '.tmp');
const E2E_DATABASE = join(E2E_DIR, 'e2e.db');
const DOTNET_ROOT = process.env.DOTNET_ROOT ?? join(process.env.HOME ?? '', '.dotnet');
const DOTNET_BIN_DIR = join(DOTNET_ROOT);

const API_PORT = 5099;
const SPA_PORT = 4300;

// Deleted at config load, which happens before any server starts, so every run begins from an
// empty schema and the tests never inherit a previous run's rows.
rmSync(E2E_DIR, { recursive: true, force: true });
mkdirSync(E2E_DIR, { recursive: true });

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  reporter: [['list'], ['html', { open: 'never' }]],

  use: {
    baseURL: `http://localhost:${SPA_PORT}`,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },

  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],

  webServer: [
    {
      // `dotnet` must be on PATH - see README prerequisites.
      command: `dotnet run --project src/Invelop.Contacts.Api --urls http://localhost:${API_PORT}`,
      // Waiting on /health rather than the port means we only start testing once the app has
      // actually finished migrating, not merely once the socket is open.
      url: `http://localhost:${API_PORT}/health`,
      reuseExistingServer: false,
      timeout: 180_000,
      stdout: 'pipe',
      stderr: 'pipe',
      env: {
        PATH: `${DOTNET_BIN_DIR}:${process.env.PATH ?? ''}`,
        DOTNET_ROOT,
        DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE: 'false',
        ASPNETCORE_HOSTBUILDER__RELOADCONFIGONCHANGE: 'false',
        ASPNETCORE_ENVIRONMENT: 'Development',
        ConnectionStrings__Default: `Data Source=${E2E_DATABASE}`,
        // The only place this is switched on: the throwaway database is created and thrown away
        // inside the test run, so there is nobody to show the migration step to.
        Database__AutoMigrate: 'true',
        Spa__Origin: `http://localhost:${SPA_PORT}`,
      },
    },
    {
      command: `npm start -- --port ${SPA_PORT} --proxy-config proxy.e2e.json`,
      cwd: 'web',
      url: `http://localhost:${SPA_PORT}`,
      reuseExistingServer: false,
      timeout: 180_000,
      stdout: 'pipe',
      stderr: 'pipe',
      env: {
        ...process.env,
        CHOKIDAR_USEPOLLING: '1',
        WATCHPACK_POLLING: 'true',
      },
    },
  ],
});
