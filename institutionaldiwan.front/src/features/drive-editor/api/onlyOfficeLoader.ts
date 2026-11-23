interface OnlyOfficeEditor {
  destroy?: () => void
  /** `DocEditor.serviceCommand`, e.g. 'forcesave' to commit the pending edit to the server. */
  serviceCommand?: (command: string) => void
}

interface OnlyOfficeApi {
  DocEditor: new (placeholder: string, config: Record<string, unknown>) => OnlyOfficeEditor
}

declare global {
  interface Window {
    DocsAPI?: OnlyOfficeApi
  }
}

const loaders = new Map<string, Promise<OnlyOfficeApi>>()

function normalizedServerUrl(serverUrl: string): string {
  return serverUrl.replace(/\/+$/, '')
}

export function loadOnlyOfficeApi(serverUrl: string): Promise<OnlyOfficeApi> {
  const source = `${normalizedServerUrl(serverUrl)}/web-apps/apps/api/documents/api.js`
  const existing = loaders.get(source)
  if (existing) return existing

  const promise = new Promise<OnlyOfficeApi>((resolve, reject) => {
    if (window.DocsAPI) {
      resolve(window.DocsAPI)
      return
    }

    const script = document.createElement('script')
    script.src = source
    script.async = true
    script.onload = () => {
      if (window.DocsAPI) resolve(window.DocsAPI)
      else reject(new Error('OnlyOffice DocsAPI loaded without a global API'))
    }
    script.onerror = () => reject(new Error('OnlyOffice DocsAPI failed to load'))
    document.head.appendChild(script)
  })

  loaders.set(source, promise)
  return promise
}
