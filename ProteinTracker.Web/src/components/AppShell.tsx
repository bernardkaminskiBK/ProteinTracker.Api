import type { ReactNode } from 'react'
import type { PageId } from '../types/ui'

interface AppShellProps {
  activePage: PageId
  onNavigate: (page: PageId) => void
  children: ReactNode
}

const navigation: Array<{ id: PageId; label: string; icon: ReactNode }> = [
  {
    id: 'dashboard',
    label: 'Today',
    icon: (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M4 5.5h16v14H4zM8 3v5M16 3v5M4 10h16" />
      </svg>
    ),
  },
  {
    id: 'foods',
    label: 'Foods',
    icon: (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M7 4v7a3 3 0 0 0 3 3V4M8.5 14v7M16 3v18M16 3c3 2 4 5 4 8h-4" />
      </svg>
    ),
  },
  {
    id: 'target',
    label: 'Targets',
    icon: (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <circle cx="12" cy="12" r="8" />
        <circle cx="12" cy="12" r="3" />
        <path d="M12 2v3M22 12h-3" />
      </svg>
    ),
  },
]

export function AppShell({ activePage, onNavigate, children }: AppShellProps) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <button className="brand" type="button" onClick={() => onNavigate('dashboard')}>
          <span className="brand-mark" aria-hidden="true">
            P
          </span>
          <span>
            <strong>Protein</strong>
            <small>Tracker</small>
          </span>
        </button>

        <nav className="main-nav" aria-label="Main navigation">
          {navigation.map((item) => (
            <button
              className={activePage === item.id ? 'nav-item active' : 'nav-item'}
              key={item.id}
              type="button"
              onClick={() => onNavigate(item.id)}
            >
              {item.icon}
              <span>{item.label}</span>
            </button>
          ))}
        </nav>

        <div className="sidebar-note">
          <span className="status-dot" />
          <div>
            <strong>Single-user workspace</strong>
            <small>Europe/Bratislava</small>
          </div>
        </div>
      </aside>

      <main className="main-content">{children}</main>

      <nav className="mobile-nav" aria-label="Mobile navigation">
        {navigation.map((item) => (
          <button
            className={activePage === item.id ? 'active' : ''}
            key={item.id}
            type="button"
            onClick={() => onNavigate(item.id)}
          >
            {item.icon}
            <span>{item.label}</span>
          </button>
        ))}
      </nav>
    </div>
  )
}
