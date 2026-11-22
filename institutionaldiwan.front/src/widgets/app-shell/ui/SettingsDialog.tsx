import {
  AlignJustify,
  CircleHelp,
  GraduationCap,
  Monitor,
  PanelsTopLeft,
  LayoutTemplate,
  Moon,
  Pause,
  Play,
  Rows3,
  Sun,
} from 'lucide-react'
import {
  setTutorialChoice,
  useThemeChoice,
  useTutorial,
  useAppearanceChoice,
  cn,
  t,
} from '@/shared/lib'
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
  { value: 'light', labelKey: 'settings.theme-light', icon: Sun },
  { value: 'dark', labelKey: 'settings.theme-dark', icon: Moon },
  { value: 'system', labelKey: 'settings.theme-system', icon: Monitor },
] as const

const appearanceOptions = [
  { value: 'legacy', labelKey: 'settings.appearance-legacy', icon: PanelsTopLeft },
  { value: 'modern', labelKey: 'settings.appearance-modern', icon: LayoutTemplate },
] as const

const densityOptions = [
  { value: 'comfortable', labelKey: 'settings.density-comfortable', icon: AlignJustify },
  { value: 'compact', labelKey: 'settings.density-compact', icon: Rows3 },
] as const

const motionOptions = [
  { value: 'on', labelKey: 'settings.motion-on', icon: Play },
  { value: 'off', labelKey: 'settings.motion-off', icon: Pause },
] as const

const tutorialOptions = [
  { value: 'on', labelKey: 'settings.tutorial-on', icon: GraduationCap },
  { value: 'off', labelKey: 'settings.tutorial-off', icon: CircleHelp },
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
  const { appearance, setAppearance } = useAppearanceChoice()
  // Read here, not threaded from AppShell: every surface that renders help chips already
  // subscribes to this preference, so the shell has no state of its own to hold.
  const tutorial = useTutorial()

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('settings.title')}
      description={t('settings.description')}
      footer={
        <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
          {t('common.action.close')}
        </Button>
      }
    >
      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium text-foreground">{t('settings.theme')}</legend>
        <div className="grid grid-cols-3 gap-2">
          {themeOptions.map(({ value, labelKey, icon: Icon }) => (
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
              {t(labelKey)}
            </button>
          ))}
        </div>
      </fieldset>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium text-foreground">{t('settings.appearance')}</legend>
        <div className="grid grid-cols-2 gap-2">
          {appearanceOptions.map(({ value, labelKey, icon: Icon }) => (
            <button
              key={value}
              type="button"
              aria-pressed={appearance === value}
              onClick={() => setAppearance(value)}
              className={cn(
                'flex cursor-pointer items-center justify-center gap-2 rounded-lg border p-3 text-xs font-medium transition-colors',
                appearance === value
                  ? 'border-accent bg-accent/10 text-foreground'
                  : 'border-border text-muted hover:bg-surface-muted hover:text-foreground',
              )}
            >
              <Icon className="size-4" aria-hidden />
              {t(labelKey)}
            </button>
          ))}
        </div>
        <p className="mt-1 text-xs text-muted">{t('settings.appearance-note')}</p>
      </fieldset>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium text-foreground">{t('settings.density')}</legend>
        <div className="grid grid-cols-2 gap-2">
          {densityOptions.map(({ value, labelKey, icon: Icon }) => (
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
              {t(labelKey)}
            </button>
          ))}
        </div>
      </fieldset>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium text-foreground">{t('settings.motion')}</legend>
        <div className="grid grid-cols-2 gap-2">
          {motionOptions.map(({ value, labelKey, icon: Icon }) => (
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
              {t(labelKey)}
            </button>
          ))}
        </div>
        <p className="mt-1 text-xs text-muted">{t('settings.motion-note')}</p>
      </fieldset>

      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-medium text-foreground">{t('settings.tutorial')}</legend>
        <div className="grid grid-cols-2 gap-2">
          {tutorialOptions.map(({ value, labelKey, icon: Icon }) => (
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
              {t(labelKey)}
            </button>
          ))}
        </div>
        <p className="mt-1 text-xs text-muted">{t('settings.tutorial-note')}</p>
      </fieldset>
    </Dialog>
  )
}