export type PageId = 'dashboard' | 'foods' | 'target'

export interface FeedbackMessage {
  type: 'success' | 'error'
  text: string
}
