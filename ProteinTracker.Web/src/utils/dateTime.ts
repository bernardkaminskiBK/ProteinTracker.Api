const APP_TIME_ZONE = 'Europe/Bratislava'

const dateFormatter = new Intl.DateTimeFormat('en-CA', {
  timeZone: APP_TIME_ZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
})

const dateTimeFormatter = new Intl.DateTimeFormat('en-GB', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

export function todayInAppTimeZone(): string {
  const parts = dateFormatter.formatToParts(new Date())
  const values = Object.fromEntries(parts.map((part) => [part.type, part.value]))
  return `${values.year}-${values.month}-${values.day}`
}

export function getAppDayUtcRange(date: string): { start: string; end: string } {
  return {
    start: zonedMidnightToUtc(date).toISOString(),
    end: zonedMidnightToUtc(addCalendarDays(date, 1)).toISOString(),
  }
}

function zonedMidnightToUtc(date: string): Date {
  const [year, month, day] = date.split('-').map(Number)
  const desiredUtcShape = Date.UTC(year, month - 1, day)
  let candidate = desiredUtcShape

  for (let attempt = 0; attempt < 2; attempt += 1) {
    const parts = new Intl.DateTimeFormat('en-CA', {
      timeZone: APP_TIME_ZONE,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hourCycle: 'h23',
    }).formatToParts(new Date(candidate))
    const values = Object.fromEntries(parts.map((part) => [part.type, part.value]))
    const representedAsUtc = Date.UTC(
      Number(values.year),
      Number(values.month) - 1,
      Number(values.day),
      Number(values.hour),
      Number(values.minute),
      Number(values.second),
    )
    candidate = desiredUtcShape - (representedAsUtc - candidate)
  }

  return new Date(candidate)
}

function addCalendarDays(date: string, days: number): string {
  const [year, month, day] = date.split('-').map(Number)
  const result = new Date(Date.UTC(year, month - 1, day + days))
  return result.toISOString().slice(0, 10)
}

export function toDateTimeLocalValue(value: Date | string = new Date()): string {
  const date = typeof value === 'string' ? new Date(value) : value
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

export function dateTimeLocalToOffsetAware(value: string): string {
  return new Date(value).toISOString()
}

export function formatConsumedAt(value: string): string {
  return dateTimeFormatter.format(new Date(value))
}

export function formatSelectedDate(date: string): string {
  return new Intl.DateTimeFormat('en-GB', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(`${date}T00:00:00Z`))
}
