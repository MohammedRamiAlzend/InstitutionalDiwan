import { Paperclip } from 'lucide-react'
import { DocumentAttachmentsPanel } from '@/widgets/document-attachments-panel'

interface DocumentFilesTabProps {
  documentId: string
  /**
   * `DOCUMENT_EDIT` on a document that is not canceled. Upload, new versions, and archiving are all
   * writes, so the three of them stay hidden without it; downloading and verifying stay for readers.
   */
  canManage: boolean
}

/** Files tab: the document's attachments grid with its uploads, versions, archiving, and verify. */
export function DocumentFilesTab({ documentId, canManage }: DocumentFilesTabProps) {
  return (
    <section className="flex flex-col gap-4 rounded-xl border border-border bg-surface p-5">
      <div className="flex flex-col gap-1">
        <h2 className="flex items-center gap-2 text-base font-semibold text-foreground">
          <Paperclip className="size-4 text-accent" aria-hidden />
          ملفات الوثيقة
        </h2>
        <p className="max-w-2xl text-xs text-muted">
          كل ملف مرفق بهذه الوثيقة مع إصداره الحالي وحجمه واسمه الأصلي وبصمته. من هذه القائمة تُنزَّل
          الإصدارات وتُرفع وتُؤرشف الملفات وتُتحقق بصمتها. الأرشفة توقف الإصدارات الجديدة ولا تُخفي
          الملف، لذلك تظهر الحالات كلها هنا.
        </p>
      </div>

      <DocumentAttachmentsPanel documentId={documentId} canManage={canManage} />
    </section>
  )
}
