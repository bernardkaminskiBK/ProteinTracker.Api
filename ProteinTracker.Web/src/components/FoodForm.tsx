import { useState, type FormEvent } from 'react'
import type { FoodRequest, FoodResponse } from '../types/api'

interface FoodFormProps {
  food?: FoodResponse
  serverError?: string
  onSubmit: (payload: FoodRequest) => Promise<boolean>
  onCancel: () => void
}

export function FoodForm({ food, serverError, onSubmit, onCancel }: FoodFormProps) {
  const [name, setName] = useState(food?.name ?? '')
  const [protein, setProtein] = useState(food?.proteinPer100g.toString() ?? '')
  const [carbohydrates, setCarbohydrates] = useState(
    food?.carbohydratesPer100g.toString() ?? '',
  )
  const [fat, setFat] = useState(food?.fatPer100g.toString() ?? '')
  const [validationError, setValidationError] = useState('')
  const [saving, setSaving] = useState(false)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const values = [Number(protein), Number(carbohydrates), Number(fat)]

    if (!name.trim()) {
      setValidationError('Food name is required.')
      return
    }

    if (values.some((value) => !Number.isFinite(value) || value < 0)) {
      setValidationError('Macro values must be zero or greater.')
      return
    }

    setValidationError('')
    setSaving(true)

    try {
      await onSubmit({
        name,
        proteinPer100g: values[0],
        carbohydratesPer100g: values[1],
        fatPer100g: values[2],
      })
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="form-stack" onSubmit={handleSubmit}>
      <label className="field">
        <span>Food name</span>
        <input
          autoFocus
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="e.g. Oats"
        />
      </label>

      <fieldset className="macro-fieldset">
        <legend>Nutrition per 100g</legend>
        <div className="form-grid three-columns">
          <MacroInput label="Protein" value={protein} onChange={setProtein} />
          <MacroInput label="Carbohydrates" value={carbohydrates} onChange={setCarbohydrates} />
          <MacroInput label="Fat" value={fat} onChange={setFat} />
        </div>
      </fieldset>

      {validationError && <p className="form-error">{validationError}</p>}
      {!validationError && serverError && <p className="form-error">{serverError}</p>}

      <footer className="form-actions">
        <button className="button secondary" type="button" onClick={onCancel} disabled={saving}>
          Cancel
        </button>
        <button className="button primary" type="submit" disabled={saving}>
          {saving ? 'Saving…' : food ? 'Save changes' : 'Create food'}
        </button>
      </footer>
    </form>
  )
}

interface MacroInputProps {
  label: string
  value: string
  onChange: (value: string) => void
}

function MacroInput({ label, value, onChange }: MacroInputProps) {
  return (
    <label className="field">
      <span>{label}</span>
      <div className="input-with-suffix">
        <input
          type="number"
          min="0"
          step="any"
          inputMode="decimal"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder="0"
        />
        <span>g</span>
      </div>
    </label>
  )
}
