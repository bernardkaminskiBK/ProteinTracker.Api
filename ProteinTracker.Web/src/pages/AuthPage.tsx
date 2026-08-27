import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const { session, login, register } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  if (session) return <Navigate to="/" replace />

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setError('')

    if (!email.trim() || !password) {
      setError('Enter your email and password.')
      return
    }

    setSaving(true)
    try {
      const request = { email: email.trim(), password }
      await (mode === 'login' ? login(request) : register(request))
      const returnTo = (location.state as { from?: string } | null)?.from || '/'
      navigate(returnTo, { replace: true })
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Something went wrong. Please try again.')
    } finally {
      setSaving(false)
    }
  }

  const isLogin = mode === 'login'

  return (
    <main className="auth-page">
      <section className="auth-card">
        <div className="auth-brand">
          <span className="brand-mark" aria-hidden="true">P</span>
          <div><strong>Protein</strong><small>Tracker</small></div>
        </div>
        <span className="eyebrow">{isLogin ? 'Welcome back' : 'Create your account'}</span>
        <h1>{isLogin ? 'Log in to continue' : 'Start tracking your nutrition'}</h1>
        <p>Your foods, entries, and targets stay private to your account.</p>

        <form className="auth-form" onSubmit={submit}>
          <label>
            <span>Email</span>
            <input
              autoComplete="email"
              inputMode="email"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </label>
          <label>
            <span>Password</span>
            <input
              autoComplete={isLogin ? 'current-password' : 'new-password'}
              minLength={8}
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
            {!isLogin && <small>Use at least 8 characters.</small>}
          </label>
          {error && <div className="inline-error" role="alert">{error}</div>}
          <button className="button primary wide" disabled={saving} type="submit">
            {saving ? 'Please wait…' : isLogin ? 'Log in' : 'Create account'}
          </button>
        </form>

        <p className="auth-switch">
          {isLogin ? 'New to Protein Tracker?' : 'Already have an account?'}{' '}
          <Link to={isLogin ? '/register' : '/login'}>{isLogin ? 'Create one' : 'Log in'}</Link>
        </p>
      </section>
    </main>
  )
}
