import { useCallback, useEffect, useMemo, useState } from 'react'
import { dailySummaryApi, foodEntriesApi, foodsApi } from '../api/client'
import { FeedbackBanner } from '../components/FeedbackBanner'
import { FoodEntryForm } from '../components/FoodEntryForm'
import { MetricCard } from '../components/MetricCard'
import { DatePickerField } from '../components/DatePickerFields'
import { Modal } from '../components/Modal'
import type {
  DailySummaryResponse,
  FoodEntryRequest,
  FoodEntryResponse,
  FoodResponse,
} from '../types/api'
import type { FeedbackMessage } from '../types/ui'
import {
  formatConsumedAt,
  formatSelectedDate,
  getAppDayUtcRange,
  todayInAppTimeZone,
} from '../utils/dateTime'

interface DashboardPageProps {
  onOpenFoods: () => void
}

const numberFormatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 1 })

export function DashboardPage({ onOpenFoods }: DashboardPageProps) {
  const [selectedDate, setSelectedDate] = useState(todayInAppTimeZone)
  const [summary, setSummary] = useState<DailySummaryResponse | null>(null)
  const [entries, setEntries] = useState<FoodEntryResponse[]>([])
  const [foods, setFoods] = useState<FoodResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [feedback, setFeedback] = useState<FeedbackMessage | null>(null)
  const [editor, setEditor] = useState<'new' | FoodEntryResponse | null>(null)
  const [editorError, setEditorError] = useState('')

  const loadDashboard = useCallback(async (date: string, showLoader = true) => {
    if (showLoader) setLoading(true)

    try {
      const range = getAppDayUtcRange(date)
      const [summaryResponse, entryResponse, activeFoods, archivedFoods] = await Promise.all([
        dailySummaryApi.get(date),
        foodEntriesApi.getByRange(range.start, range.end),
        foodsApi.getActive(),
        foodsApi.getArchived(),
      ])
      setSummary(summaryResponse)
      setEntries(entryResponse.sort((a, b) => a.consumedAt.localeCompare(b.consumedAt)))
      setFoods([...activeFoods, ...archivedFoods])
    } catch (error) {
      setFeedback({ type: 'error', text: getErrorMessage(error) })
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    // Data fetching is the external synchronization performed by this effect.
    // oxlint-disable-next-line react/set-state-in-effect
    void loadDashboard(selectedDate, false)
  }, [loadDashboard, selectedDate])

  const metrics = useMemo(() => {
    if (!summary) return []
    return [
      {
        label: 'Protein',
        consumed: summary.consumed.protein,
        target: summary.target.protein,
        remaining: summary.remaining.protein,
        unit: 'g',
        tone: 'protein' as const,
      },
      {
        label: 'Carbohydrates',
        consumed: summary.consumed.carbohydrates,
        target: summary.target.carbohydrates,
        remaining: summary.remaining.carbohydrates,
        unit: 'g',
        tone: 'carbs' as const,
      },
      {
        label: 'Fat',
        consumed: summary.consumed.fat,
        target: summary.target.fat,
        remaining: summary.remaining.fat,
        unit: 'g',
        tone: 'fat' as const,
      },
      {
        label: 'Calories',
        consumed: summary.consumed.calories,
        target: summary.target.calories,
        remaining: summary.remaining.calories,
        unit: 'kcal',
        tone: 'calories' as const,
      },
    ]
  }, [summary])

  const saveEntry = async (payload: FoodEntryRequest): Promise<boolean> => {
    setEditorError('')
    try {
      if (editor && editor !== 'new') {
        await foodEntriesApi.update(editor.id, payload)
        setFeedback({ type: 'success', text: 'Food entry updated.' })
      } else {
        await foodEntriesApi.create(payload)
        setFeedback({ type: 'success', text: 'Food entry added.' })
      }
      setEditor(null)
      await loadDashboard(selectedDate, false)
      return true
    } catch (error) {
      const message = getErrorMessage(error)
      setEditorError(message)
      setFeedback({ type: 'error', text: message })
      return false
    }
  }

  const deleteEntry = async (entry: FoodEntryResponse) => {
    if (!window.confirm(`Delete the ${entry.foodName} entry? This cannot be undone.`)) return

    try {
      await foodEntriesApi.delete(entry.id)
      setFeedback({ type: 'success', text: 'Food entry deleted.' })
      await loadDashboard(selectedDate, false)
    } catch (error) {
      setFeedback({ type: 'error', text: getErrorMessage(error) })
    }
  }

  return (
    <div className="page dashboard-page">
      <header className="page-header dashboard-header">
        <div>
          <span className="eyebrow">Daily overview</span>
          <h1>{formatSelectedDate(selectedDate)}</h1>
          <p>Your intake, targets, and meals in one place.</p>
        </div>
        <div className="date-controls">
          <button
            className="icon-button bordered"
            type="button"
            aria-label="Previous day"
            onClick={() => setSelectedDate(addDays(selectedDate, -1))}
          >
            ‹
          </button>
          <DatePickerField
            className="date-picker"
            label="Selected date"
            hideLabel
            value={selectedDate}
            fallbackValue={todayInAppTimeZone()}
            onChange={setSelectedDate}
          />
          <button
            className="icon-button bordered"
            type="button"
            aria-label="Next day"
            onClick={() => setSelectedDate(addDays(selectedDate, 1))}
          >
            ›
          </button>
          {selectedDate !== todayInAppTimeZone() && (
            <button className="button ghost compact" type="button" onClick={() => setSelectedDate(todayInAppTimeZone())}>
              Today
            </button>
          )}
        </div>
      </header>

      <FeedbackBanner feedback={feedback} onDismiss={() => setFeedback(null)} />

      {loading ? (
        <DashboardSkeleton />
      ) : (
        <>
          <section className="metric-grid" aria-label="Daily nutrition progress">
            {metrics.map((metric) => (
              <MetricCard key={metric.label} {...metric} />
            ))}
          </section>

          <section className="content-card entries-card">
            <header className="card-header">
              <div>
                <h2>Food entries</h2>
                <p>{entries.length === 0 ? 'Nothing recorded yet' : `${entries.length} ${entries.length === 1 ? 'entry' : 'entries'}`}</p>
              </div>
              <button className="button primary" type="button" onClick={() => { setEditorError(''); setEditor('new') }}>
                <span aria-hidden="true">＋</span> Add food
              </button>
            </header>

            {entries.length === 0 ? (
              <div className="empty-state">
                <span className="empty-icon" aria-hidden="true">○</span>
                <h3>Start logging your day</h3>
                <p>Add your first meal or snack to see nutrition totals update.</p>
                {foods.some((food) => !food.isArchived) ? (
                  <button className="button secondary" type="button" onClick={() => { setEditorError(''); setEditor('new') }}>
                    Add first entry
                  </button>
                ) : (
                  <button className="button secondary" type="button" onClick={onOpenFoods}>
                    Create a food first
                  </button>
                )}
              </div>
            ) : (
              <div className="entry-list">
                {entries.map((entry) => (
                  <article className="entry-row" key={entry.id}>
                    <div className="entry-identity">
                      <span className="food-avatar" aria-hidden="true">{entry.foodName.charAt(0).toUpperCase()}</span>
                      <div>
                        <strong>{entry.foodName}</strong>
                        <span>{formatConsumedAt(entry.consumedAt)} · {numberFormatter.format(entry.amountInGrams)}g</span>
                      </div>
                    </div>
                    <div className="entry-macros">
                      <MacroValue label="Protein" value={entry.protein} />
                      <MacroValue label="Carbs" value={entry.carbohydrates} />
                      <MacroValue label="Fat" value={entry.fat} />
                      <MacroValue label="Calories" value={entry.calories} unit="kcal" />
                    </div>
                    <div className="row-actions">
                      <button className="text-button" type="button" onClick={() => { setEditorError(''); setEditor(entry) }}>Edit</button>
                      <button className="text-button danger" type="button" onClick={() => void deleteEntry(entry)}>Delete</button>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>
        </>
      )}

      {editor && (
        <Modal
          title={editor === 'new' ? 'Add food entry' : 'Edit food entry'}
          subtitle="Nutrition totals use the food's current values."
          onClose={() => { setEditorError(''); setEditor(null) }}
        >
          <FoodEntryForm
            foods={foods}
            entry={editor === 'new' ? undefined : editor}
            serverError={editorError}
            onSubmit={saveEntry}
            onCancel={() => { setEditorError(''); setEditor(null) }}
          />
        </Modal>
      )}
    </div>
  )
}

function MacroValue({ label, value, unit = 'g' }: { label: string; value: number; unit?: string }) {
  return (
    <div>
      <span>{label}</span>
      <strong>{numberFormatter.format(value)} {unit}</strong>
    </div>
  )
}

function DashboardSkeleton() {
  return (
    <div className="skeleton-layout" aria-label="Loading dashboard">
      <div className="metric-grid">
        {[0, 1, 2, 3].map((item) => <div className="skeleton metric-skeleton" key={item} />)}
      </div>
      <div className="skeleton list-skeleton" />
    </div>
  )
}

function addDays(date: string, days: number): string {
  const result = new Date(`${date}T00:00:00Z`)
  result.setUTCDate(result.getUTCDate() + days)
  return result.toISOString().slice(0, 10)
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.'
}
