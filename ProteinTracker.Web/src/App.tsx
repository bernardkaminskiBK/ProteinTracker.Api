import { Navigate, Outlet, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import './App.css'
import { AppShell } from './components/AppShell'
import { DailyTargetPage } from './pages/DailyTargetPage'
import { DashboardPage } from './pages/DashboardPage'
import { FoodsPage } from './pages/FoodsPage'
import { AuthPage } from './pages/AuthPage'
import { useAuth } from './auth/AuthContext'

function App() {
  return (
    <Routes>
      <Route path="/login" element={<AuthPage mode="login" />} />
      <Route path="/register" element={<AuthPage mode="register" />} />
      <Route element={<ProtectedLayout />}>
        <Route path="/" element={<DashboardRoute />} />
        <Route path="/foods" element={<FoodsPage />} />
        <Route path="/targets" element={<DailyTargetPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

function ProtectedLayout() {
  const { session, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()

  if (!session) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return (
    <AppShell email={session.email} onLogout={() => { logout(); navigate('/login', { replace: true }) }}>
      <Outlet />
    </AppShell>
  )
}

function DashboardRoute() {
  const navigate = useNavigate()
  return <DashboardPage onOpenFoods={() => navigate('/foods')} />
}

export default App
