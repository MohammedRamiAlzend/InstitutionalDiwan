import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { LogOut } from 'lucide-react'
import { tokenStorage } from '@/shared/api'
import { ROUTES } from '@/shared/config'
import { useSessionStore, userKeys } from '@/entities/user'
import { Button, type ButtonProps } from '@/shared/ui'

type LogoutButtonProps = Omit<ButtonProps, 'onClick'>

/** Clears the JWT, resets the session store and query cache, and returns to `/login`. */
export function LogoutButton({ children, ...props }: LogoutButtonProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const clear = useSessionStore((state) => state.clear)

  const handleLogout = () => {
    tokenStorage.clear()
    clear()
    queryClient.removeQueries({ queryKey: userKeys.all })
    navigate(ROUTES.LOGIN, { replace: true })
  }

  return (
    <Button
      type="button"
      variant="outline"
      onClick={handleLogout}
      {...props}
    >
      <LogOut className="size-4 rtl:-scale-x-100" aria-hidden />
      {children ?? 'تسجيل الخروج'}
    </Button>
  )
}
