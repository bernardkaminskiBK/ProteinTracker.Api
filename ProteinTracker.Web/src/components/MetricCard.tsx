interface MetricCardProps {
  label: string
  consumed: number
  target: number
  remaining: number
  unit: string
  tone: 'protein' | 'carbs' | 'fat' | 'calories'
}

const numberFormatter = new Intl.NumberFormat('en-US', {
  maximumFractionDigits: 1,
})

export function MetricCard({ label, consumed, target, remaining, unit, tone }: MetricCardProps) {
  const exceeded = remaining < 0
  const progress = target > 0 ? Math.min((consumed / target) * 100, 100) : 0

  return (
    <article className={`metric-card ${tone} ${exceeded ? 'exceeded' : ''}`}>
      <div className="metric-heading">
        <span className="metric-icon" aria-hidden="true" />
        <span>{label}</span>
        {exceeded && <span className="exceeded-badge">Exceeded</span>}
      </div>
      <div className="metric-primary">
        <strong>{numberFormatter.format(consumed)}</strong>
        <span>
          / {numberFormatter.format(target)} {unit}
        </span>
      </div>
      <div className="progress-track" aria-hidden="true">
        <span style={{ width: `${progress}%` }} />
      </div>
      <div className="metric-footer">
        <span>{exceeded ? 'Over target' : 'Remaining'}</span>
        <strong>
          {numberFormatter.format(Math.abs(remaining))} {unit}
        </strong>
      </div>
    </article>
  )
}
