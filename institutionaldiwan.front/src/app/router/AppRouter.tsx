import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { LoginPage } from '@/pages/login'
import { ChangePasswordPage } from '@/pages/change-password'
import { DashboardPage } from '@/pages/dashboard'
import { DepartmentsPage } from '@/pages/departments'
import { DocumentsPage, DocumentDetailsPage } from '@/pages/documents'
import { MovementsPage } from '@/pages/movements'
import { ArchivePage } from '@/pages/archive'
import { DecisionsPage } from '@/pages/decisions'
import { MyRegistersPage } from '@/pages/my-registers'
import { InstitutionSettingsPage } from '@/pages/institution-settings'
import { UsersPage } from '@/pages/users'
import { RolesPage } from '@/pages/roles'
import { EmployeesPage } from '@/pages/employees'
import { DocumentSchemasPage } from '@/pages/document-schemas'
import { NumberingPage } from '@/pages/numbering'
import { AuditPage } from '@/pages/audit'
import { SystemErrorsPage } from '@/pages/system-errors'
import { PersonDetailsPage, PersonsPage } from '@/pages/persons'
import { NotificationsPage } from '@/pages/notifications'
import { LookupsPage } from '@/pages/lookups'
import { AppShell } from '@/widgets/app-shell'
import { NotificationBell } from '@/widgets/notification-bell'
import { NotificationLiveLayout } from './NotificationLiveLayout'
import { ProtectedRoute } from './ProtectedRoute'

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path={ROUTES.LOGIN} element={<LoginPage />} />
        <Route element={<ProtectedRoute />}>
          <Route path={ROUTES.CHANGE_PASSWORD} element={<ChangePasswordPage />} />
          <Route element={<NotificationLiveLayout />}>
          <Route element={<AppShell headerSlot={<NotificationBell />} />}>
            <Route path={ROUTES.DASHBOARD} element={<DashboardPage />} />
            <Route path={ROUTES.DOCUMENTS} element={<DocumentsPage />} />
            <Route path={ROUTES.DOCUMENT_DETAIL_PATTERN} element={<DocumentDetailsPage />} />
            <Route path={ROUTES.MOVEMENTS} element={<MovementsPage />} />
            <Route path={ROUTES.DECISIONS} element={<DecisionsPage />} />
            <Route path={ROUTES.MY_REGISTERS} element={<MyRegistersPage />} />
            <Route path={ROUTES.ARCHIVE} element={<ArchivePage />} />
            <Route path={ROUTES.DEPARTMENTS} element={<DepartmentsPage />} />
            <Route path={ROUTES.INSTITUTION_SETTINGS} element={<InstitutionSettingsPage />} />
            <Route path={ROUTES.USERS} element={<UsersPage />} />
            <Route path={ROUTES.ROLES} element={<RolesPage />} />
            <Route path={ROUTES.EMPLOYEES} element={<EmployeesPage />} />
            <Route path={ROUTES.NUMBERING} element={<NumberingPage />} />
            <Route path={ROUTES.DOCUMENT_SCHEMAS} element={<DocumentSchemasPage />} />
            <Route path={ROUTES.LOOKUPS} element={<LookupsPage />} />
            <Route path={ROUTES.AUDIT} element={<AuditPage />} />
            <Route path={ROUTES.SYSTEM_ERRORS} element={<SystemErrorsPage />} />
            <Route path={ROUTES.PERSONS} element={<PersonsPage />} />
            <Route path={ROUTES.PERSON_DETAIL_PATTERN} element={<PersonDetailsPage />} />
            <Route path={ROUTES.NOTIFICATIONS} element={<NotificationsPage />} />
          </Route>
          </Route>
        </Route>
        <Route path="*" element={<Navigate to={ROUTES.DASHBOARD} replace />} />
      </Routes>
    </BrowserRouter>
  )
}
