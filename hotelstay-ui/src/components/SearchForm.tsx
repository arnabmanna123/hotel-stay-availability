import { useEffect, useState } from 'react'
import type { CityInfo, RoomType } from '../types'

interface Props {
  cities: CityInfo[]
  disabled: boolean
  onSearch: (args: {
    destination: string
    checkIn: string
    checkOut: string
    roomType: RoomType | null
  }) => void
}

const ROOM_TYPES: RoomType[] = ['Standard', 'Deluxe', 'Suite']

function today(): string {
  const d = new Date()
  return d.toISOString().slice(0, 10)
}

function addDays(iso: string, n: number): string {
  const d = new Date(iso)
  d.setDate(d.getDate() + n)
  return d.toISOString().slice(0, 10)
}

export function SearchForm({ cities, disabled, onSearch }: Props) {
  const [destination, setDestination] = useState('')
  const [checkIn, setCheckIn] = useState(today())
  const [checkOut, setCheckOut] = useState(addDays(today(), 3))
  const [roomType, setRoomType] = useState<RoomType | ''>('')

  useEffect(() => {
    if (!destination && cities.length > 0) setDestination(cities[0].name)
  }, [cities, destination])

  const datesInvalid = checkOut <= checkIn
  const canSubmit = !disabled && destination && !datesInvalid

  return (
    <form
      className="search-form"
      onSubmit={(e) => {
        e.preventDefault()
        if (!canSubmit) return
        onSearch({ destination, checkIn, checkOut, roomType: roomType || null })
      }}
    >
      <div className="form-row">
        <label>
          Destination
          <select
            value={destination}
            onChange={(e) => setDestination(e.target.value)}
            disabled={disabled || cities.length === 0}
          >
            {cities.length === 0 ? (
              <option value="">No destination options are available</option>
            ) : (
              cities.map((c) => (
                <option key={c.name} value={c.name}>
                  {c.name} ({c.class})
                </option>
              ))
            )}
          </select>
        </label>

        <label>
          Check-in
          <input
            type="date"
            value={checkIn}
            onChange={(e) => setCheckIn(e.target.value)}
            disabled={disabled}
          />
        </label>

        <label>
          Check-out
          <input
            type="date"
            value={checkOut}
            onChange={(e) => setCheckOut(e.target.value)}
            disabled={disabled}
          />
        </label>

        <label>
          Room type
          <select
            value={roomType}
            onChange={(e) => setRoomType(e.target.value as RoomType | '')}
            disabled={disabled}
          >
            <option value="">Any</option>
            {ROOM_TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
        </label>

        <button type="submit" disabled={!canSubmit || cities.length === 0}>
          {disabled ? 'Searching…' : 'Search'}
        </button>
      </div>

      {datesInvalid && (
        <p className="inline-warn">Check-out must be after check-in.</p>
      )}
    </form>
  )
}
