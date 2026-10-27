import { Moon, Sun } from 'lucide-react'
import { cn, useThemeChoice } from '@/shared/lib'
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
      aria-label={theme === 'dark' ? 'التبديل إلى الوضع الفاتح' : 'التبديل إلى الوضع الداكن'}
      title={theme === 'dark' ? 'الوضع الفاتح' : 'الوضع الداكن'}
    >
      {theme === 'dark' ? (
        <Sun className="size-5" aria-hidden />
      ) : (
        <Moon className="size-5" aria-hidden />
      )}
    </Button>
  )
}
