import type { GuideEntry, PageGuide } from './types'

export const DOCUMENT_SCHEMAS_GUIDE_ENTRIES: readonly GuideEntry[] = [
  {
    id: 'document-schemas.type',
    title: 'نوع الوثيقة',
    body: 'لكل نوع وثيقة مخططه الخاص من الحقول الإضافية. اختر النوع لعرض نسخ مخططه وحقولها.',
    note: 'يحتاج صلاحية إدارة القوائم المرجعية.',
  },
  {
    id: 'document-schemas.versions',
    title: 'نسخ المخطط',
    body: 'النسخة المنشورة مجمّدة لا تتغير، والوثيقة تبقى مرتبطة بالنسخة التي أُنشئت عليها. المسودة وحدها قابلة للتعديل، وللنوع مسودة واحدة كحد أقصى. النسخة المتقاعدة هي التي حلّت محلها نسخة منشورة أحدث.',
  },
  {
    id: 'document-schemas.fields',
    title: 'حقول النسخة',
    body: 'الحقول بترتيب ظهورها في نموذج الإنشاء، مع نوع كل حقل وكونه إجباريًا وقيوده. مفتاح الحقل ونوعه لا يتغيران بعد إنشائه.',
  },
]

export const DOCUMENT_SCHEMAS_GUIDE: PageGuide = {
  pageId: 'document-schemas',
  intro:
    'هنا تعرّف الإدارة حقولًا إضافية لكل نوع وثيقة دون تعديل النظام. اختر النوع ثم تصفّح نسخ مخططه.',
  entryIds: [
    'document-schemas.type',
    'document-schemas.versions',
    'document-schemas.fields',
    'table.search',
    'table.columns',
    'table.pageSize',
    'table.refresh',
    'table.rowMenu',
    'table.empty',
  ],
}
