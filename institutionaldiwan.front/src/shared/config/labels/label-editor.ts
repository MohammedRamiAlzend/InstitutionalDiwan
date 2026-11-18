/** The in-place label editor (superadmin only): its cluster arias/titles, save
 * and reset toasts, and the pre-save Zod guards. Save/cancel arias reuse
 * `common.action.save` / `common.action.cancel`. */
export const LABEL_EDITOR_LABELS = {
  'label-editor.toast-saved': 'تم حفظ التسمية.',
  'label-editor.toast-reset': 'تمت استعادة التسمية الافتراضية.',
  'label-editor.group-aria': 'تحرير التسمية',
  'label-editor.input-aria': 'نص التسمية',
  'label-editor.save-title': 'حفظ (Enter)',
  'label-editor.reset-aria': 'استعادة الافتراضي',
  'label-editor.reset-title': 'استعادة التسمية الافتراضية',
  'label-editor.cancel-title': 'إلغاء (Esc)',
  'label-editor.validation.required': 'التسمية مطلوبة.',
  'label-editor.validation.max': 'التسمية تتجاوز الحد الأقصى ({max} حرفًا).',
  'label-editor.validation.html': 'التسمية لا تقبل وسوم HTML.',
  'label-editor.validation.placeholders': 'يجب الإبقاء على العناصر النائبة: {placeholders}.',
} as const