import { fileURLToPath, URL } from 'node:url';

import { defineConfig, loadEnv } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env as processEnv } from 'process';

// Folder this config lives in — `.env`, `.env.local` and `.env.<mode>` are read from here.
const projectFolder = fileURLToPath(new URL('.', import.meta.url));

/**
 * Merges the external `.env` files with the real process environment for one key: values coming
 * from the shell/Visual Studio win over the file, so CI or a one-off run can override `.env`
 * without editing it. Only keys used by this config (no `VITE_` prefix) are read, so nothing here
 * is ever exposed to the browser bundle.
 */
function loadConfigEnv(mode: string): Record<string, string> {
    const merged: Record<string, string> = loadEnv(mode, projectFolder, '');

    for (const [key, value] of Object.entries(processEnv)) {
        if (value !== undefined) {
            merged[key] = value;
        }
    }

    return merged;
}

/** `true` / `false` / an address; blank or absent means "let Vite use its default (localhost)". */
function resolveHost(value: string | undefined): string | boolean | undefined {
    const normalized = value?.trim();
    if (!normalized) return undefined;
    if (normalized.toLowerCase() === 'true') return true;
    if (normalized.toLowerCase() === 'false') return false;
    return normalized;
}

/** `true` allows any host; otherwise a comma-separated hostname/IP list; blank means unset. */
function resolveAllowedHosts(value: string | undefined): string[] | true | undefined {
    const normalized = value?.trim();
    if (!normalized) return undefined;
    if (normalized.toLowerCase() === 'true') return true;

    const hosts = normalized
        .split(',')
        .map(host => host.trim())
        .filter(host => host.length > 0);

    return hosts.length > 0 ? hosts : undefined;
}

function resolvePort(value: string | undefined, fallback: number): number {
    const parsed = Number.parseInt(value ?? '', 10);
    return Number.isNaN(parsed) ? fallback : parsed;
}

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadConfigEnv(mode);

    const baseFolder =
        processEnv.APPDATA !== undefined && processEnv.APPDATA !== ''
            ? `${processEnv.APPDATA}/ASP.NET/https`
            : `${processEnv.HOME}/.aspnet/https`;

    const certificateName = "institutionaldiwan.client";
    const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
    const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

    if (!fs.existsSync(baseFolder)) {
        fs.mkdirSync(baseFolder, { recursive: true });
    }

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        if (0 !== child_process.spawnSync('dotnet', [
            'dev-certs',
            'https',
            '--export-path',
            certFilePath,
            '--format',
            'Pem',
            '--no-password',
        ], { stdio: 'inherit', }).status) {
            throw new Error("Could not create certificate.");
        }
    }

    const target = env.DEV_PROXY_TARGET ||
        (env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
            env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7126');

    return {
        plugins: [plugin()],
        resolve: {
            alias: {
                '@': fileURLToPath(new URL('./src', import.meta.url))
            }
        },
        server: {
            proxy: {
                '^/api': {
                    target,
                    secure: false,
                    changeOrigin: true
                },
                '^/hubs': {
                    target,
                    secure: false,
                    changeOrigin: true,
                    ws: true
                }
            },
            port: resolvePort(env.DEV_SERVER_PORT, 61325),
            host: resolveHost(env.DEV_HOST),
            allowedHosts: resolveAllowedHosts(env.DEV_ALLOWED_HOSTS),
            https: {
                key: fs.readFileSync(keyFilePath),
                cert: fs.readFileSync(certFilePath),
            }
        }
    };
});
