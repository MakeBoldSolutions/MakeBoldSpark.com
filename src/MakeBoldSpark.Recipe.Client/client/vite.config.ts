/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
export default defineConfig({ plugins: [react()], base: '/recipes/', build: { outDir: '../build', emptyOutDir: true }, server: { port: 5174, proxy: { '/api': { target: 'http://localhost:5290', changeOrigin: true } } }, test: { environment: 'jsdom', globals: true, setupFiles: ['./src/test-setup.ts'] } });
