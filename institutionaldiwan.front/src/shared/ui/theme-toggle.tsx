import { Moon, Sun } from 'lucide-react'
import { cn, t, useThemeChoice } from '@/shared/lib'
import { Button } from './button'

interface ThemeToggleProps {
  className?: string
}

/** Light/dark toggle persisted in `localStorage` under `diwan.theme`. */
export function ThemeToggle({ className }: ThemeToggleProps) {
  const { theme, toggle } = useThemeChoice()

  return (
    <Button
      type="button"
      variant="secondary"
      size="icon"
      onClick={toggle}
      className={cn('rounded-full', className)}
      aria-label={theme === 'dark' ? t('common.theme.to-light') : t('common.theme.to-dark')}
      title={theme === 'dark' ? t('common.theme.light') : t('common.theme.dark')}
    >
      {theme === 'dark' ? (
        <Sun className="size-5" aria-hidden />
      ) : (
        <Moon className="size-5" aria-hidden />
      )}
    </Button>
  )
}
