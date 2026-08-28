import { useId } from 'react'
import DatePicker from 'react-datepicker'
import 'react-datepicker/dist/react-datepicker.css'

interface PickerFieldProps {
  label: string
  value: string
  onChange: (value: string) => void
  className?: string
  hideLabel?: boolean
}

interface DatePickerFieldProps extends PickerFieldProps {
  fallbackValue: string
}

export function DatePickerField({
  label,
  value,
  fallbackValue,
  onChange,
  className = '',
  hideLabel = false,
}: DatePickerFieldProps) {
  const id = useId()

  return (
    <div className={`picker-field ${className}`.trim()}>
      <label className={hideLabel ? 'sr-only' : undefined} htmlFor={id}>{label}</label>
      <DatePicker
        id={id}
        selected={parseDateOnly(value)}
        onChange={(date: Date | null) => onChange(date ? formatDateOnly(date) : fallbackValue)}
        dateFormat="dd MMM yyyy"
        calendarStartDay={1}
        showPopperArrow={false}
        shouldCloseOnSelect
      />
    </div>
  )
}

export function DateTimePickerField({
  label,
  value,
  onChange,
  className = '',
  hideLabel = false,
}: PickerFieldProps) {
  const id = useId()

  return (
    <div className={`picker-field ${className}`.trim()}>
      <label className={hideLabel ? 'sr-only' : undefined} htmlFor={id}>{label}</label>
      <DatePicker
        id={id}
        selected={value ? new Date(value) : null}
        onChange={(date: Date | null) => onChange(date ? formatDateTimeLocal(date) : '')}
        dateFormat="dd MMM yyyy, HH:mm"
        timeFormat="HH:mm"
        timeIntervals={15}
        timeCaption="Time"
        showTimeSelect
        calendarStartDay={1}
        showPopperArrow={false}
        placeholderText="Select date and time"
      />
    </div>
  )
}

function parseDateOnly(value: string): Date | null {
  const [year, month, day] = value.split('-').map(Number)
  return year && month && day ? new Date(year, month - 1, day) : null
}

function formatDateOnly(date: Date): string {
  return [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, '0'),
    String(date.getDate()).padStart(2, '0'),
  ].join('-')
}

function formatDateTimeLocal(date: Date): string {
  return `${formatDateOnly(date)}T${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
}
