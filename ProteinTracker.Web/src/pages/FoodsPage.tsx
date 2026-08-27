import { useCallback, useEffect, useMemo, useState } from 'react'
import { ApiError, foodsApi } from '../api/client'
import { FeedbackBanner } from '../components/FeedbackBanner'
import { FoodForm } from '../components/FoodForm'
import { Modal } from '../components/Modal'
import type { FoodRequest, FoodResponse } from '../types/api'
import type { FeedbackMessage } from '../types/ui'

const numberFormatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 1 })

export function FoodsPage() {
  const [activeFoods, setActiveFoods] = useState<FoodResponse[]>([])
  const [archivedFoods, setArchivedFoods] = useState<FoodResponse[]>([])
  const [view, setView] = useState<'active' | 'archived'>('active')
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<number | null>(null)
  const [deletingId, setDeletingId] = useState<number | null>(null)
  const [feedback, setFeedback] = useState<FeedbackMessage | null>(null)
  const [editor, setEditor] = useState<'new' | FoodResponse | null>(null)
  const [editorError, setEditorError] = useState('')

  const loadFoods = useCallback(async (showLoader = true) => {
    if (showLoader) setLoading(true)
    try {
      const [active, archived] = await Promise.all([
        foodsApi.getActive(),
        foodsApi.getArchived(),
      ])
      setActiveFoods(active.sort((a, b) => a.name.localeCompare(b.name)))
      setArchivedFoods(archived.sort((a, b) => a.name.localeCompare(b.name)))
    } catch (error) {
      setFeedback({ type: 'error', text: getErrorMessage(error) })
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    // Data fetching is the external synchronization performed by this effect.
    // oxlint-disable-next-line react/set-state-in-effect
    void loadFoods(false)
  }, [loadFoods])

  const displayedFoods = useMemo(() => {
    const source = view === 'active' ? activeFoods : archivedFoods
    const normalizedQuery = query.trim().toLocaleLowerCase()
    return normalizedQuery
      ? source.filter((food) => food.name.toLocaleLowerCase().includes(normalizedQuery))
      : source
  }, [activeFoods, archivedFoods, query, view])

  const saveFood = async (payload: FoodRequest): Promise<boolean> => {
    setEditorError('')

    if (editor === 'new') {
      const normalizedName = payload.name.trim().toLocaleLowerCase()
      const duplicate = [...activeFoods, ...archivedFoods].find(
        (food) => food.name.trim().toLocaleLowerCase() === normalizedName,
      )

      if (
        duplicate &&
        !window.confirm(
          `An ${duplicate.isArchived ? 'archived' : 'active'} food named "${duplicate.name}" already exists. Create another anyway?`,
        )
      ) {
        return false
      }
    }

    try {
      if (editor && editor !== 'new') {
        await foodsApi.update(editor.id, payload)
        setFeedback({ type: 'success', text: `${payload.name.trim()} updated.` })
      } else {
        await foodsApi.create(payload)
        setFeedback({ type: 'success', text: `${payload.name.trim()} created.` })
      }
      setEditor(null)
      await loadFoods(false)
      return true
    } catch (error) {
      const message = getErrorMessage(error)
      setEditorError(message)
      setFeedback({ type: 'error', text: message })
      return false
    }
  }

  const archiveFood = async (food: FoodResponse) => {
    if (!window.confirm(`Archive ${food.name}? Historical entries will remain available.`)) return
    setBusyId(food.id)
    try {
      await foodsApi.archive(food.id)
      setFeedback({ type: 'success', text: `${food.name} archived.` })
      await loadFoods(false)
    } catch (error) {
      setFeedback({ type: 'error', text: getErrorMessage(error) })
    } finally {
      setBusyId(null)
    }
  }

  const restoreFood = async (food: FoodResponse) => {
    setBusyId(food.id)
    try {
      await foodsApi.restore(food.id)
      setFeedback({ type: 'success', text: `${food.name} restored and ready to use.` })
      await loadFoods(false)
    } catch (error) {
      setFeedback({ type: 'error', text: getErrorMessage(error) })
    } finally {
      setBusyId(null)
    }
  }

  const deleteFood = async (food: FoodResponse) => {
    if (!window.confirm(`Permanently delete ${food.name}? This cannot be undone.`)) return
    setDeletingId(food.id)
    try {
      await foodsApi.delete(food.id)
      setFeedback({ type: 'success', text: `${food.name} permanently deleted.` })
      await loadFoods(false)
    } catch (error) {
      setFeedback({
        type: 'error',
        text: error instanceof ApiError && error.status === 409
          ? `${food.name} cannot be deleted because it is used by historical food entries.`
          : getErrorMessage(error),
      })
    } finally {
      setDeletingId(null)
    }
  }

  return (
    <div className="page foods-page">
      <header className="page-header">
        <div>
          <span className="eyebrow">Food library</span>
          <h1>Foods</h1>
          <p>Reusable nutritional definitions, always measured per 100 grams.</p>
        </div>
        <button className="button primary" type="button" onClick={() => { setEditorError(''); setEditor('new') }}>
          <span aria-hidden="true">＋</span> New food
        </button>
      </header>

      <FeedbackBanner feedback={feedback} onDismiss={() => setFeedback(null)} />

      <section className="content-card foods-card">
        <div className="food-toolbar">
          <div className="segmented-control" role="group" aria-label="Food archive status">
            <button className={view === 'active' ? 'active' : ''} type="button" onClick={() => setView('active')}>
              Active <span>{activeFoods.length}</span>
            </button>
            <button className={view === 'archived' ? 'active' : ''} type="button" onClick={() => setView('archived')}>
              Archived <span>{archivedFoods.length}</span>
            </button>
          </div>
          <label className="search-field">
            <span aria-hidden="true">⌕</span>
            <span className="sr-only">Search foods</span>
            <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search foods" />
          </label>
        </div>

        {loading ? (
          <div className="food-grid">
            {[0, 1, 2, 3, 4, 5].map((item) => <div className="skeleton food-skeleton" key={item} />)}
          </div>
        ) : displayedFoods.length === 0 ? (
          <div className="empty-state">
            <span className="empty-icon" aria-hidden="true">◇</span>
            <h3>{query ? 'No matching foods' : view === 'active' ? 'Your food library is empty' : 'No archived foods'}</h3>
            <p>{query ? 'Try a different search term.' : view === 'active' ? 'Create a food to start recording meals.' : 'Archived foods will appear here and can be restored.'}</p>
            {!query && view === 'active' && (
              <button className="button secondary" type="button" onClick={() => { setEditorError(''); setEditor('new') }}>Create first food</button>
            )}
          </div>
        ) : (
          <div className="food-grid">
            {displayedFoods.map((food) => (
              <article className="food-card" key={food.id}>
                <div className="food-card-heading">
                  <span className="food-avatar large" aria-hidden="true">{food.name.charAt(0).toUpperCase()}</span>
                  <div>
                    <h3>{food.name}</h3>
                    <span>Per 100g</span>
                  </div>
                  {food.isArchived && <span className="archive-badge">Archived</span>}
                </div>
                <div className="food-calories">
                  <strong>{numberFormatter.format(food.caloriesPer100g)}</strong>
                  <span>kcal</span>
                </div>
                <div className="food-macros">
                  <FoodMacro label="Protein" value={food.proteinPer100g} tone="protein" />
                  <FoodMacro label="Carbs" value={food.carbohydratesPer100g} tone="carbs" />
                  <FoodMacro label="Fat" value={food.fatPer100g} tone="fat" />
                </div>
                <footer className="food-card-actions">
                  {!food.isArchived && (
                    <button className="text-button" type="button" onClick={() => { setEditorError(''); setEditor(food) }}>Edit</button>
                  )}
                  {food.isArchived ? (
                    <>
                      <button className="button secondary compact" type="button" disabled={busyId === food.id || deletingId === food.id} onClick={() => void restoreFood(food)}>
                        {busyId === food.id ? 'Restoring…' : 'Restore'}
                      </button>
                      <button className="text-button danger" type="button" disabled={busyId === food.id || deletingId === food.id} onClick={() => void deleteFood(food)}>
                        {deletingId === food.id ? 'Deleting…' : 'Delete'}
                      </button>
                    </>
                  ) : (
                    <button className="text-button danger" type="button" disabled={busyId === food.id} onClick={() => void archiveFood(food)}>
                      {busyId === food.id ? 'Archiving…' : 'Archive'}
                    </button>
                  )}
                </footer>
              </article>
            ))}
          </div>
        )}
      </section>

      {editor && (
        <Modal
          title={editor === 'new' ? 'Create food' : `Edit ${editor.name}`}
          subtitle="Enter macronutrients per 100g. Calories are calculated automatically."
          onClose={() => { setEditorError(''); setEditor(null) }}
        >
          <FoodForm
            food={editor === 'new' ? undefined : editor}
            serverError={editorError}
            onSubmit={saveFood}
            onCancel={() => { setEditorError(''); setEditor(null) }}
          />
        </Modal>
      )}
    </div>
  )
}

function FoodMacro({ label, value, tone }: { label: string; value: number; tone: string }) {
  return (
    <div>
      <span className={`macro-dot ${tone}`} />
      <span>{label}</span>
      <strong>{numberFormatter.format(value)}g</strong>
    </div>
  )
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.'
}
