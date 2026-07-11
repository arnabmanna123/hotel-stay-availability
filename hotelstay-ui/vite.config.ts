import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/hotels': {
        target: 'http://localhost:5080',
        changeOrigin: false,
      },
    },
  },
})
