import { Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import './App.css'
import { AppShell } from './components/AppShell'
import { DailyTargetPage } from './pages/DailyTargetPage'
import { DashboardPage } from './pages/DashboardPage'
import { FoodsPage } from './pages/FoodsPage'

function App() {
  const navigate = useNavigate()

  return (
    <AppShell>
      <Routes>
        <Route path="/" element={<DashboardPage onOpenFoods={() => navigate('/foods')} />} />
        <Route path="/foods" element={<FoodsPage />} />
        <Route path="/targets" element={<DailyTargetPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppShell>
  )
}

export default App
