import {
  AlignJustify,
  CircleHelp,
  GraduationCap,
  Monitor,
  Moon,
  Pause,
  Play,
  Rows3,
  Sun,
} from 'lucide-react'
import { setTutorialChoice, useThemeChoice, useTutorial, cn } from '@/shared/lib'
import { Button, Dialog } from '@/shared/ui'
import type { Density } from '../model/density'
import type { Motion } from '../model/motion'

interface SettingsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  density: Density
  onDensityChange: (density: Density) => void
  motion: Motion
  onMotionChange: (motion: Motion) => void
}

const themeOptions = [
  { value: 'light', label: 'فاتح', icon: Sun },
  { value: 'dark', label: 'داكن', icon: Moon },
  { value: 'system', label: 'حسب النظام', icon: Monitor },
] as const

const densityOptions = [
  { value: 'comfortable', label: 'مريح', icon: AlignJustify },
  { value: 'compact', label: 'مضغوط', icon: Rows3 },
] as const

const motionOptions = [
  { value: 'on', label: 'مفعّلة', icon: Play },
  { value: 'off', label: 'متوقفة', icon: Pause },
] as const

const tutorialOptions = [
  { value: 'on', label: 'مفعّل', icon: GraduationCap },
  { value: 'off', label: 'متوقف', icon: CircleHelp },
] as const

/** Theme, density, motion, and tutorial preferences; all are per-browser view settings. */
export function SettingsDialog({
  open,
  onOpenChange,
  density,
  onDensityChange,
  motion,
  onMotionChange,
}: SettingsDialogProps) {
  const { choice, setChoice } = useThemeChoice()
  // Read here, not threaded from AppShell: every surface that renders help chips already
  // subscribes to this preference, so the shell has no state of its own to hold.
  const tutorial = useTutorial()

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="إعدادات العرض"
      description="اختر المظهر وكثافة العرض وحركات التنقل والوضع التعليمي المناسب لشاشتك. تُحفظ هذه الإعدادات في هذا المتصفح."
      footer={
        <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
          إغلاق
        </Button>
      }
    >
      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium text-foreground">المظهر</legend>
        <div className="grid grid-cols-3 gap-2">
          {themeOptions.map(({ value, label, icon: Icon }) => (
            <button
              key={value}
              type="button"
              aria-pressed={choice === value}
              onClick={() => setChoice(value)}
              className={cn(
                'flex cursor-pointer flex-col items-center gap-2 rounded-lg border p-3 text-xs font-medium transition-colors',
                choice === value
                  ? 'border-accent bg-accent/10 text-foreground'
                  : 'border-border text-muted hover:bg-surface-muted hover:text-foreground',
              )}
            >
              <Icon className="size-5" aria-hidden />
              {label}
            </button>
          ))}
        </div>
      </fieldset>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium text-foreground">كثافة العرض</legend>
        <div className="grid grid-cols-2 gap-2">
          {densityOptions.map(({ value, label, icon: Icon }) => (
            <button
              key={value}
              type="button"
              aria-pressed={density === value}
              onClick={() => onDensityChange(value)}
              className={cn(
                'flex cursor-pointer items-center justify-center gap-2 rounded-lg border p-3 text-xs font-medium transition-colors',
                density === value
                  ? 'border-accent bg-accent/10 text-foreground'
                  : 'border-border text-muted hover:bg-surface-muted hover:text-foreground',
              )}
            >
              <Icon className="size-4" aria-hidden />
              {label}
            </button>
          ))}
        </div>
      </fieldset>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium text-foreground">حركات التنقل</legend>
        <div className="grid grid-cols-2 gap-2">
          {motionOptions.map(({ value, label, icon: Icon }) => (
            <button
              key={value}
              type="button"
              aria-pressed={motion === value}
              onClick={() => onMotionChange(value)}
              className={cn(
                'flex cursor-pointer items-center justify-center gap-2 rounded-lg border p-3 text-xs font-medium transition-colors',
                motion === value
                  ? 'border-accent bg-accent/10 text-foreground'
                  : 'border-border text-muted hover:bg-surface-muted hover:text-foreground',
              )}
            >
              <Icon className="size-4" aria-hidden />
              {label}
            </button>
          ))}
        </div>
        <p className="mt-1 text-xs text-muted">
          تُظهر الحركات الصفحات الجديدة تدريجيًا. عند إيقافها تُعرض الصفحات والمربعات الحوارية
          فورًا، مع بقاء مؤشرات التحميل دوّارة للدلالة على أن العمل جارٍ.
        </p>
      </fieldset>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium text-foreground">الوضع التعليمي</legend>
        <div className="grid grid-cols-2 gap-2">
          {tutorialOptions.map(({ value, label, icon: Icon }) => (
            <button
              key={value}
              type="button"
              aria-pressed={tutorial === value}
              onClick={() => setTutorialChoice(value)}
              className={cn(
                'flex cursor-pointer items-center justify-center gap-2 rounded-lg border p-3 text-xs font-medium transition-colors',
                tutorial === value
                  ? 'border-accent bg-accent/10 text-foreground'
                  : 'border-border text-muted hover:bg-surface-muted hover:text-foreground',
              )}
            >
              <Icon className="size-4" aria-hidden />
              {label}
            </button>
          ))}
        </div>
        <p className="mt-1 text-xs text-muted">
          يضيف أزرار شرح صغيرة بجانب أجزاء الواجهة: كل زر يفتح مربعًا حواريًا يشرح الغرض
          والشروط وأثر الإجراء، وزرًا في رأس الصفحة يمرّ على أجزائها واحدًا واحدًا. لا يُخفي
          عنصرًا ولا يغيّر في البيانات شيئًا، وعند إيقافه ترجع الواجهة كما كانت تمامًا.
        </p>
      </fieldset>
    </Dialog>
  )
}
