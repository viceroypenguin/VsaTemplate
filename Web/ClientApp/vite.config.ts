import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const backend = process.env.WEB_API_URL ?? 'http://localhost:5106';

export default defineConfig({
  plugins: [react()],
  build: { sourcemap: false },
  server: {
    port: 5173,
    strictPort: true,
    proxy: Object.fromEntries(
      ['/api', '/Logout'].map((path) => [
        path, { target: backend, changeOrigin: true, secure: true },
      ]),
    ),
  },
});
