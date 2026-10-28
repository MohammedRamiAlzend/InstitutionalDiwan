import { LogOut } from 'lucide-react'
import { Button, type ButtonProps } from '@/shared/ui'
import { useLogout } from '../model/use-logout'

type LogoutButtonProps = Omit<ButtonProps, 'onClick'>

/** Clears the session and returns to `/login`. */
export function LogoutButton({ children, ...props }: LogoutButtonProps) {
  const logout = useLogout()

  return (
    <Button type="button" variant="outline" onClick={logout} {...props}>
      <LogOut className="size-4 rtl:-scale-x-100" aria-hidden />
      {children ?? 'تسجيل الخروج'}
    </Button>
  )
}
