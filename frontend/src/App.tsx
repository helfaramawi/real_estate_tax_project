import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { isAuthenticated } from './lib/auth'
import Layout from './components/Layout'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import PropertiesPage from './pages/properties/PropertiesPage'
import PropertyDetailPage from './pages/properties/PropertyDetailPage'
import TaxpayersPage from './pages/taxpayers/TaxpayersPage'
import TaxpayerDetailPage from './pages/taxpayers/TaxpayerDetailPage'
import BillsPage from './pages/bills/BillsPage'
import PaymentsPage from './pages/payments/PaymentsPage'
import AppealsPage from './pages/appeals/AppealsPage'
import ExemptionsPage from './pages/exemptions/ExemptionsPage'
import ValuationsPage from './pages/valuations/ValuationsPage'
import FieldSurveysPage from './pages/fieldsurveys/FieldSurveysPage'

const qc = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: 1 },
  },
})

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  return isAuthenticated() ? <>{children}</> : <Navigate to="/login" replace />
}

export default function App() {
  return (
    <QueryClientProvider client={qc}>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <Layout />
              </ProtectedRoute>
            }
          >
            <Route index element={<DashboardPage />} />
            <Route path="properties" element={<PropertiesPage />} />
            <Route path="properties/:id" element={<PropertyDetailPage />} />
            <Route path="taxpayers" element={<TaxpayersPage />} />
            <Route path="taxpayers/:id" element={<TaxpayerDetailPage />} />
            <Route path="bills" element={<BillsPage />} />
            <Route path="payments" element={<PaymentsPage />} />
            <Route path="appeals" element={<AppealsPage />} />
            <Route path="exemptions" element={<ExemptionsPage />} />
            <Route path="valuations" element={<ValuationsPage />} />
            <Route path="field-surveys" element={<FieldSurveysPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}
