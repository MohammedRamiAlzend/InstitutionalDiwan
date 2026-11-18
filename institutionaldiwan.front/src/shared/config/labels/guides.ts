/** Tutorial-mode chrome: the page-tour introduction title, the chip explain
 * prefix, the header trigger and the Driver.js tour buttons. The guide entries'
 * own title/body/note/intro are authored content in `config/guides`, not labels. */
export const GUIDE_LABELS = {
  'guide.about-page': 'عن هذه الصفحة',
  'guide.explain': 'اشرح: {title}',
  'guide.explain-page': 'شرح هذه الصفحة',
  'guide.tour.next': 'التالي',
  'guide.tour.prev': 'السابق',
  'guide.tour.progress': '{{current}} من {{total}}',
} as const