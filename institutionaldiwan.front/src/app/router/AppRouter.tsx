import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { LoginPage } from '@/pages/login'
import { ChangePasswordPage } from '@/pages/change-password'
import { DashboardPage } from '@/pages/dashboard'
import { DepartmentsPage } from '@/pages/departments'
import { DocumentsPage, DocumentDetailsPage } from '@/pages/documents'
import { MovementsPage } from '@/pages/movements'
import { InstitutionSettingsPage } from '@/pages/institution-settings'
import { UsersPage } from '@/pages/users'
import { RolesPage } from '@/pages/roles'
import { EmployeesPage } from '@/pages/employees'
import { NumberingPage } from '@/pages/numbering'
import { AuditPage } from '@/pages/audit'
import { AppShell } from '@/widgets/app-shell'
import { ProtectedRoute } from './ProtectedRoute'

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path={ROUTES.LOGIN} element={<LoginPage />} />
        <Route element={<ProtectedRoute />}>
          <Route path={ROUTES.CHANGE_PASSWORD} element={<ChangePasswordPage />} />
          <Route element={<AppShell />}>
            <Route path={ROUTES.DASHBOARD} element={<DashboardPage />} />
            <Route path={ROUTES.DOCUMENTS} element={<DocumentsPage />} />
            <Route path={ROUTES.DOCUMENT_DETAIL_PATTERN} element={<DocumentDetailsPage />} />
            <Route path={ROUTES.MOVEMENTS} element={<MovementsPage />} />
            <Route path={ROUTES.DEPARTMENTS} element={<DepartmentsPage />} />
            <Route path={ROUTES.INSTITUTION_SETTINGS} element={<InstitutionSettingsPage />} />
            <Route path={ROUTES.USERS} element={<UsersPage />} />
            <Route path={ROUTES.ROLES} element={<RolesPage />} />
            <Route path={ROUTES.EMPLOYEES} element={<EmployeesPage />} />
            <Route path={ROUTES.NUMBERING} element={<NumberingPage />} />
            <Route path={ROUTES.AUDIT} element={<AuditPage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to={ROUTES.DASHBOARD} replace />} />
      </Routes>
    </BrowserRouter>
  )
}
