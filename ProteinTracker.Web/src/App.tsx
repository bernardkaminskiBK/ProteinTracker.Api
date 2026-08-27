import { useState } from 'react'
import './App.css'
import { AppShell } from './components/AppShell'
import { DailyTargetPage } from './pages/DailyTargetPage'
import { DashboardPage } from './pages/DashboardPage'
import { FoodsPage } from './pages/FoodsPage'
import type { PageId } from './types/ui'

function App() {
  const [activePage, setActivePage] = useState<PageId>('dashboard')

  return (
    <AppShell activePage={activePage} onNavigate={setActivePage}>
      {activePage === 'dashboard' && <DashboardPage onOpenFoods={() => setActivePage('foods')} />}
      {activePage === 'foods' && <FoodsPage />}
      {activePage === 'target' && <DailyTargetPage />}
    </AppShell>
  )
}

export default App
