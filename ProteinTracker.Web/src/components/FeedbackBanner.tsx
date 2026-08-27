import type { FeedbackMessage } from '../types/ui'

interface FeedbackBannerProps {
  feedback: FeedbackMessage | null
  onDismiss: () => void
}

export function FeedbackBanner({ feedback, onDismiss }: FeedbackBannerProps) {
  if (!feedback) return null

  return (
    <div className={`feedback ${feedback.type}`} role={feedback.type === 'error' ? 'alert' : 'status'}>
      <span>{feedback.type === 'success' ? '✓' : '!'}</span>
      <p>{feedback.text}</p>
      <button type="button" onClick={onDismiss} aria-label="Dismiss message">
        ×
      </button>
    </div>
  )
}
