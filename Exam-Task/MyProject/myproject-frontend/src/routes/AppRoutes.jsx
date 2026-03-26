import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useSelector } from 'react-redux'
import ProtectedRoute from './guards/ProtectedRoute'
import PublicRoute from './guards/PublicRoute'
import AppLayout from '../layouts/AppLayout'
import SignUpPage from '../pages/auth/SignUpPage'
import SignInPage from '../pages/auth/SignInPage'
import AuthCallbackPage from '../pages/auth/AuthCallbackPage'
import UnauthorizedPage from '../pages/auth/UnauthorizedPage'
import DashboardPage from '../pages/dashboard/DashboardPage'
import ConnectionPage from '../pages/quickbooks/ConnectionPage'
import AccountsPage from '../pages/accounts/AccountsPage'
import CustomersPage from '../pages/customers/CustomersPage'
import ItemsPage from '../pages/items/ItemsPage'
import InvoicesListPage from '../pages/invoices/InvoicesListPage'
import InvoiceFormPage from '../pages/invoices/InvoiceFormPage'
import ProfilePage from '../pages/profile/ProfilePage'

const AppRoutes = () => {
  const { isAuthenticated } = useSelector((state) => state.auth)

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to={isAuthenticated ? '/dashboard' : '/signin'} replace />} />
        <Route
          path="/signup"
          element={(
            <PublicRoute>
              <SignUpPage />
            </PublicRoute>
          )}
        />
        <Route
          path="/signin"
          element={(
            <PublicRoute>
              <SignInPage />
            </PublicRoute>
          )}
        />
        <Route path="/auth/callback" element={<AuthCallbackPage />} />
        <Route path="/unauthorized" element={<UnauthorizedPage />} />

        <Route
          path="/dashboard"
          element={(
            <ProtectedRoute>
              <AppLayout>
                <DashboardPage />
              </AppLayout>
            </ProtectedRoute>
          )}
        />
        <Route
          path="/connection"
          element={(
            <ProtectedRoute>
              <AppLayout>
                <ConnectionPage />
              </AppLayout>
            </ProtectedRoute>
          )}
        />
        <Route
          path="/accounts"
          element={(
            <ProtectedRoute>
              <AppLayout>
                <AccountsPage />
              </AppLayout>
            </ProtectedRoute>
          )}
        />
        <Route
          path="/customers"
          element={(
            <ProtectedRoute>
              <AppLayout>
                <CustomersPage />
              </AppLayout>
            </ProtectedRoute>
          )}
        />
        <Route
          path="/items"
          element={(
            <ProtectedRoute>
              <AppLayout>
                <ItemsPage />
              </AppLayout>
            </ProtectedRoute>
          )}
        />
        <Route
          path="/invoices"
          element={(
            <ProtectedRoute>
              <AppLayout>
                <InvoicesListPage />
              </AppLayout>
            </ProtectedRoute>
          )}
        />
        <Route
          path="/invoices/new"
          element={(
            <ProtectedRoute>
              <AppLayout>
                <InvoiceFormPage />
              </AppLayout>
            </ProtectedRoute>
          )}
        />
        <Route
          path="/invoices/edit/:id"
          element={(
            <ProtectedRoute>
              <AppLayout>
                <InvoiceFormPage />
              </AppLayout>
            </ProtectedRoute>
          )}
        />
        <Route
          path="/profile"
          element={(
            <ProtectedRoute>
              <AppLayout>
                <ProfilePage />
              </AppLayout>
            </ProtectedRoute>
          )}
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default AppRoutes
