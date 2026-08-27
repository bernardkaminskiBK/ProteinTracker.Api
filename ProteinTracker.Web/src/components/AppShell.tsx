import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'

interface AppShellProps {
  children: ReactNode
}

const navigation: Array<{ path: string; label: string; icon: ReactNode }> = [
  {
    path: '/',
    label: 'Today',
    icon: (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M4 5.5h16v14H4zM8 3v5M16 3v5M4 10h16" />
      </svg>
    ),
  },
  {
    path: '/foods',
    label: 'Foods',
    icon: (
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M7 4v7a3 3 0 0 0 3 3V4M8.5 14v7M16 3v18M16 3c3 2 4 5 4 8h-4" />
      </svg>
    ),
  },
  {
    path: '/targets',
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

export function AppShell({ children }: AppShellProps) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <NavLink className="brand" to="/">
          <span className="brand-mark" aria-hidden="true">
            P
          </span>
          <span>
            <strong>Protein</strong>
            <small>Tracker</small>
          </span>
        </NavLink>

        <nav className="main-nav" aria-label="Main navigation">
          {navigation.map((item) => (
            <NavLink
              className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}
              end={item.path === '/'}
              key={item.path}
              to={item.path}
            >
              {item.icon}
              <span>{item.label}</span>
            </NavLink>
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
          <NavLink
            className={({ isActive }) => isActive ? 'active' : ''}
            end={item.path === '/'}
            key={item.path}
            to={item.path}
          >
            {item.icon}
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
