export interface FoodResponse {
  id: number
  name: string
  proteinPer100g: number
  carbohydratesPer100g: number
  fatPer100g: number
  caloriesPer100g: number
  isArchived: boolean
}

export interface FoodRequest {
  name: string
  proteinPer100g: number
  carbohydratesPer100g: number
  fatPer100g: number
}

export interface FoodEntryResponse {
  id: number
  foodId: number
  foodName: string
  amountInGrams: number
  consumedAt: string
  protein: number
  carbohydrates: number
  fat: number
  calories: number
}

export interface FoodEntryRequest {
  foodId: number
  amountInGrams: number
  consumedAt: string
}

export interface DailyTargetResponse {
  proteinTarget: number
  carbohydratesTarget: number
  fatTarget: number
  calorieTarget: number
}

export interface UpdateDailyTargetRequest {
  proteinTarget: number
  carbohydratesTarget: number
  fatTarget: number
}

export interface NutritionSummary {
  protein: number
  carbohydrates: number
  fat: number
  calories: number
}

export interface DailySummaryResponse {
  date: string
  consumed: NutritionSummary
  target: NutritionSummary
  remaining: NutritionSummary
}

export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

export interface AuthRequest {
  email: string
  password: string
}

export interface AuthResponse {
  token: string
  email: string
  expiresAt: string
}
