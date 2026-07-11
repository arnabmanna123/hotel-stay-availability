import type {
  ApiError,
  CityInfo,
  Reservation,
  ReserveRequest,
  RoomType,
  SearchResponse,
} from '../types'

// Errors this API can throw. Keeping the code + message pair lets the UI localise later.
export class HotelApiError extends Error {
  readonly code: string
  readonly status: number
  constructor(message: string, code: string, status: number) {
    super(message)
    this.code = code
    this.status = status
  }
}

async function parseOrThrow<T>(response: Response): Promise<T> {
  if (response.ok) {
    return (await response.json()) as T
  }
  let body: ApiError | null = null
  try {
    body = (await response.json()) as ApiError
  } catch {
    // response wasn't JSON — fall through to generic message
  }
  throw new HotelApiError(
    body?.error ?? `Request failed with status ${response.status}`,
    body?.code ?? 'unknown_error',
    response.status,
  )
}

export async function fetchCities(): Promise<CityInfo[]> {
  const response = await fetch('/hotels/cities')
  return parseOrThrow<CityInfo[]>(response)
}

export interface SearchArgs {
  destination: string
  checkIn: string
  checkOut: string
  roomType?: RoomType | null
}

export async function searchRooms(args: SearchArgs): Promise<SearchResponse> {
  const params = new URLSearchParams({
    destination: args.destination,
    checkIn: args.checkIn,
    checkOut: args.checkOut,
  })
  if (args.roomType) params.set('roomType', args.roomType)
  const response = await fetch(`/hotels/search?${params.toString()}`)
  return parseOrThrow<SearchResponse>(response)
}

export async function reserveRoom(body: ReserveRequest): Promise<Reservation> {
  const response = await fetch('/hotels/reserve', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  return parseOrThrow<Reservation>(response)
}

export async function fetchReservation(reference: string): Promise<Reservation> {
  const response = await fetch(`/hotels/reservation/${encodeURIComponent(reference)}`)
  return parseOrThrow<Reservation>(response)
}
