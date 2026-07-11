// Mirror of the API's domain types (spec §2).
// Kept in one file so a schema change on the API produces a single obvious TS compile error.

export type ProviderId = 'PremierStays' | 'BudgetNests'
export type RoomType = 'Standard' | 'Deluxe' | 'Suite'
export type DocumentType = 'Passport' | 'NationalId'
export type CityClass = 'Domestic' | 'International'
export type CancellationPolicyType = 'FreeCancellation' | 'Flexible' | 'NonRefundable'

export interface CancellationPolicy {
  type: CancellationPolicyType
  hoursBeforeCheckIn: number | null
}

export interface Room {
  id: string
  providerId: ProviderId
  roomType: RoomType
  pricePerNight: number
  totalPrice: number
  currency: string
  cancellationPolicy: CancellationPolicy
  amenities: string[]
  starRating: number | null
}

export interface Reservation {
  reference: string
  providerId: ProviderId
  roomId: string
  roomType: RoomType
  checkIn: string
  checkOut: string
  nights: number
  pricePerNight: number
  totalPrice: number
  currency: string
  cancellationPolicy: CancellationPolicy
  guestName: string
  documentType: DocumentType
  documentNumber: string
}

export interface SearchResponse {
  nights: number
  currency: string
  results: Room[]
}

export interface CityInfo {
  name: string
  class: CityClass
}

export interface ApiError {
  error: string
  code: string
}

export interface ReserveRequest {
  roomId: string
  providerId: ProviderId
  destination: string
  checkIn: string
  checkOut: string
  guestName: string
  documentType: DocumentType
  documentNumber: string
}
