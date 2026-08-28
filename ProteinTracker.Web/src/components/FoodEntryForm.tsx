import { useState, type FormEvent } from 'react'
import type { FoodEntryRequest, FoodEntryResponse, FoodResponse } from '../types/api'
import { dateTimeLocalToOffsetAware, toDateTimeLocalValue } from '../utils/dateTime'
import { DateTimePickerField } from './DatePickerFields'

interface FoodEntryFormProps {
  foods: FoodResponse[]
  entry?: FoodEntryResponse
  serverError?: string
  onSubmit: (payload: FoodEntryRequest) => Promise<boolean>
  onCancel: () => void
}

export function FoodEntryForm({ foods, entry, serverError, onSubmit, onCancel }: FoodEntryFormProps) {
  const selectableFoods = entry
    ? foods.filter((food) => !food.isArchived || food.id === entry.foodId)
    : foods.filter((food) => !food.isArchived)
  const [foodId, setFoodId] = useState(entry?.foodId ?? selectableFoods[0]?.id ?? 0)
  const [amount, setAmount] = useState(entry?.amountInGrams.toString() ?? '')
  const [consumedAt, setConsumedAt] = useState(
    toDateTimeLocalValue(entry?.consumedAt ?? new Date()),
  )
  const [validationError, setValidationError] = useState('')
  const [saving, setSaving] = useState(false)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const numericAmount = Number(amount)

    if (!foodId) {
      setValidationError('Select a food before saving the entry.')
      return
    }

    if (!Number.isFinite(numericAmount) || numericAmount <= 0) {
      setValidationError('Amount must be greater than zero grams.')
      return
    }

    if (!consumedAt) {
      setValidationError('Choose when the food was consumed.')
      return
    }

    setValidationError('')
    setSaving(true)

    try {
      await onSubmit({
        foodId,
        amountInGrams: numericAmount,
        consumedAt: dateTimeLocalToOffsetAware(consumedAt),
      })
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form-stack" onSubmit={handleSubmit}>
      {selectableFoods.length === 0 ? (
        <div className="inline-empty">
          <strong>No active foods yet</strong>
          <p>Create an active food before recording an entry.</p>
        </div>
      ) : (
        <label className="field">
          <span>Food</span>
          <select value={foodId} onChange={(event) => setFoodId(Number(event.target.value))}>
            {selectableFoods.map((food) => (
              <option key={food.id} value={food.id}>
                {food.name}{food.isArchived ? ' (archived)' : ''}
              </option>
            ))}
          </select>
          {entry && selectableFoods.find((food) => food.id === entry.foodId)?.isArchived && (
            <small>This archived food may remain assigned, but cannot be selected for new entries.</small>
          )}
        </label>
      )}

      <div className="form-grid two-columns">
        <label className="field">
          <span>Amount</span>
          <div className="input-with-suffix">
            <input
              type="number"
              min="0.001"
              step="any"
              inputMode="decimal"
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              placeholder="150"
            />
            <span>g</span>
          </div>
        </label>

        <DateTimePickerField
          className="field"
          label="Consumed at"
          value={consumedAt}
          onChange={setConsumedAt}
        />
      </div>

      {validationError && <p className="form-error">{validationError}</p>}
      {!validationError && serverError && <p className="form-error">{serverError}</p>}

      <footer className="form-actions">
        <button className="button secondary" type="button" onClick={onCancel} disabled={saving}>
          Cancel
        </button>
        <button className="button primary" type="submit" disabled={saving || selectableFoods.length === 0}>
          {saving ? 'Saving…' : entry ? 'Save changes' : 'Add entry'}
        </button>
      </footer>
    </form>
  )
}
