import { useEffect, useState, type FormEvent } from 'react'
import { dailyTargetApi } from '../api/client'
import { FeedbackBanner } from '../components/FeedbackBanner'
import type { DailyTargetResponse } from '../types/api'
import type { FeedbackMessage } from '../types/ui'

const numberFormatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 1 })

export function DailyTargetPage() {
  const [target, setTarget] = useState<DailyTargetResponse | null>(null)
  const [protein, setProtein] = useState('')
  const [carbohydrates, setCarbohydrates] = useState('')
  const [fat, setFat] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [validationError, setValidationError] = useState('')
  const [feedback, setFeedback] = useState<FeedbackMessage | null>(null)

  const applyTarget = (response: DailyTargetResponse) => {
    setTarget(response)
    setProtein(response.proteinTarget.toString())
    setCarbohydrates(response.carbohydratesTarget.toString())
    setFat(response.fatTarget.toString())
  }

  useEffect(() => {
    const loadTarget = async () => {
      try {
        const response = await dailyTargetApi.getCurrent()
        applyTarget(response)
      } catch (error) {
        setFeedback({ type: 'error', text: getErrorMessage(error) })
      } finally {
        setLoading(false)
      }
    }

    void loadTarget()
  }, [])

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const values = [Number(protein), Number(carbohydrates), Number(fat)]

    if (values.some((value) => !Number.isFinite(value) || value < 0)) {
      setValidationError('All targets must be zero or greater.')
      return
    }

    setValidationError('')
    setSaving(true)
    try {
      const response = await dailyTargetApi.update({
        proteinTarget: values[0],
        carbohydratesTarget: values[1],
        fatTarget: values[2],
      })
      applyTarget(response)
      setFeedback({ type: 'success', text: 'Daily targets saved.' })
    } catch (error) {
      setFeedback({ type: 'error', text: getErrorMessage(error) })
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="page target-page">
      <header className="page-header">
        <div>
          <span className="eyebrow">Daily goals</span>
          <h1>Macro targets</h1>
          <p>Set the daily baseline used by your nutrition summary.</p>
        </div>
      </header>

      <FeedbackBanner feedback={feedback} onDismiss={() => setFeedback(null)} />

      {loading ? (
        <div className="target-layout">
          <div className="skeleton target-form-skeleton" />
          <div className="skeleton target-summary-skeleton" />
        </div>
      ) : (
        <div className="target-layout">
          <section className="content-card target-form-card">
            <header>
              <h2>Daily macros</h2>
              <p>Use grams for each target. Zero values are allowed.</p>
            </header>

            <form className="target-form" onSubmit={handleSubmit}>
              <TargetInput label="Protein" description="Supports muscle growth and recovery" value={protein} onChange={setProtein} tone="protein" />
              <TargetInput label="Carbohydrates" description="Your primary source of daily energy" value={carbohydrates} onChange={setCarbohydrates} tone="carbs" />
              <TargetInput label="Fat" description="Supports hormones and nutrient absorption" value={fat} onChange={setFat} tone="fat" />

              {validationError && <p className="form-error">{validationError}</p>}

              <button className="button primary wide" type="submit" disabled={saving}>
                {saving ? 'Saving targets…' : 'Save daily targets'}
              </button>
            </form>
          </section>

          <aside className="target-summary-card">
            <div className="target-orbit" aria-hidden="true">
              <span className="orbit-ring outer" />
              <span className="orbit-ring inner" />
              <span className="orbit-core">{numberFormatter.format(target?.calorieTarget ?? 0)}<small>kcal</small></span>
              <span className="orbit-dot protein" />
              <span className="orbit-dot carbs" />
              <span className="orbit-dot fat" />
            </div>
            <div className="target-summary-copy">
              <span className="eyebrow light">Calculated for you</span>
              <h2>{numberFormatter.format(target?.calorieTarget ?? 0)} daily calories</h2>
              <p>Calories update automatically from your protein, carbohydrate, and fat targets.</p>
            </div>
            <div className="target-mini-grid">
              <TargetMini label="Protein" value={target?.proteinTarget ?? 0} />
              <TargetMini label="Carbs" value={target?.carbohydratesTarget ?? 0} />
              <TargetMini label="Fat" value={target?.fatTarget ?? 0} />
            </div>
          </aside>
        </div>
      )}
    </div>
  )
}

interface TargetInputProps {
  label: string
  description: string
  value: string
  onChange: (value: string) => void
  tone: string
}

function TargetInput({ label, description, value, onChange, tone }: TargetInputProps) {
  return (
    <label className="target-input-row">
      <span className={`target-input-icon ${tone}`} aria-hidden="true" />
      <span className="target-input-copy">
        <strong>{label}</strong>
        <small>{description}</small>
      </span>
      <span className="input-with-suffix target-value-input">
        <input
          type="number"
          min="0"
          step="any"
          inputMode="decimal"
          value={value}
          onChange={(event) => onChange(event.target.value)}
        />
        <span>g</span>
      </span>
    </label>
  )
}

function TargetMini({ label, value }: { label: string; value: number }) {
  return (
    <div>
      <span>{label}</span>
      <strong>{numberFormatter.format(value)}g</strong>
    </div>
  )
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.'
}
