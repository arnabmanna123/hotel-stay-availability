import { useEffect, useState } from 'react'
import { fetchCities, fetchReservation, HotelApiError, reserveRoom, searchRooms } from './api/hotels'
import type { CityInfo, Reservation, Room, RoomType, SearchResponse } from './types'
import { SearchForm } from './components/SearchForm'
import { LookupForm } from './components/LookupForm'
import { ResultsList } from './components/ResultsList'
import { ReserveForm } from './components/ReserveForm'
import { Confirmation } from './components/Confirmation'

type Mode =
  | { kind: 'idle' }
  | { kind: 'searching' }
  | { kind: 'results'; response: SearchResponse; searchArgs: SearchArgs }
  | { kind: 'reserving'; room: Room; response: SearchResponse; searchArgs: SearchArgs; submitting: boolean }
  | { kind: 'confirmed'; reservation: Reservation }

interface SearchArgs {
  destination: string
  checkIn: string
  checkOut: string
  roomType: RoomType | null
}

function userFacingError(error: unknown, fallback: string): string {
  if (error instanceof HotelApiError) {
    return error.status >= 500 ? 'Server error, please try again later.' : error.message
  }

  return fallback
}

function App() {
  const [cities, setCities] = useState<CityInfo[]>([])
  const [citiesError, setCitiesError] = useState<string | null>(null)
  const [mode, setMode] = useState<Mode>({ kind: 'idle' })
  const [error, setError] = useState<string | null>(null)
  const [lookingUp, setLookingUp] = useState(false)

  useEffect(() => {
    fetchCities()
      .then(setCities)
      .catch((e: unknown) =>
        setCitiesError(e instanceof Error ? e.message : 'Failed to load cities'),
      )
  }, [])

  const findCityClass = (name: string) =>
    cities.find((c) => c.name.toLowerCase() === name.toLowerCase())?.class ?? 'International'

  async function handleSearch(args: SearchArgs) {
    setError(null)
    setMode({ kind: 'searching' })
    try {
      const response = await searchRooms(args)
      setMode({ kind: 'results', response, searchArgs: args })
    } catch (e: unknown) {
      setError(userFacingError(e, 'Search failed'))
      setMode({ kind: 'idle' })
    }
  }

  async function handleLookup(reference: string) {
    setError(null)
    setLookingUp(true)
    try {
      const reservation = await fetchReservation(reference)
      setMode({ kind: 'confirmed', reservation })
    } catch (e: unknown) {
      setError(userFacingError(e, 'Reservation lookup failed'))
    } finally {
      setLookingUp(false)
    }
  }

  function handleReserveClick(room: Room) {
    if (mode.kind !== 'results') return
    setMode({
      kind: 'reserving',
      room,
      response: mode.response,
      searchArgs: mode.searchArgs,
      submitting: false,
    })
  }

  async function handleReserveSubmit(payload: {
    guestName: string
    documentType: 'Passport' | 'NationalId'
    documentNumber: string
  }) {
    if (mode.kind !== 'reserving') return
    setError(null)
    setMode({ ...mode, submitting: true })
    try {
      const reservation = await reserveRoom({
        roomId: mode.room.id,
        providerId: mode.room.providerId,
        destination: mode.searchArgs.destination,
        checkIn: mode.searchArgs.checkIn,
        checkOut: mode.searchArgs.checkOut,
        guestName: payload.guestName,
        documentType: payload.documentType,
        documentNumber: payload.documentNumber,
      })
      setMode({ kind: 'confirmed', reservation })
    } catch (e: unknown) {
      setError(userFacingError(e, 'Reservation failed'))
      setMode({ ...mode, submitting: false })
    }
  }

  function handleBackToResults() {
    if (mode.kind !== 'reserving') return
    setError(null)
    setMode({ kind: 'results', response: mode.response, searchArgs: mode.searchArgs })
  }

  function handleNewSearch() {
    setError(null)
    setMode({ kind: 'idle' })
  }

  const searching = mode.kind === 'searching'
  const showSearchAndLookup = mode.kind !== 'confirmed' && mode.kind !== 'reserving'

  return (
    <div className="app">
      <header className="app__head">
        <h1>SkyRoute</h1>
        <p className="app__sub">Hotel Stay Availability</p>
      </header>

      {citiesError && (
        <div className="banner banner--error">
          Couldn't load cities: {citiesError}. Is the API running on :5080?
        </div>
      )}

      {error && (
        <div className="banner banner--error" role="alert">
          {error}
        </div>
      )}

      {showSearchAndLookup && (
        <>
          <SearchForm cities={cities} disabled={searching} onSearch={handleSearch} />
          <LookupForm disabled={lookingUp} onLookup={handleLookup} />
        </>
      )}

      {mode.kind === 'results' && (
        <ResultsList
          rooms={mode.response.results}
          nights={mode.response.nights}
          onReserve={handleReserveClick}
        />
      )}

      {mode.kind === 'reserving' && (
        <ReserveForm
          room={mode.room}
          destination={mode.searchArgs.destination}
          cityClass={findCityClass(mode.searchArgs.destination)}
          checkIn={mode.searchArgs.checkIn}
          checkOut={mode.searchArgs.checkOut}
          nights={mode.response.nights}
          disabled={mode.submitting}
          onCancel={handleBackToResults}
          onSubmit={handleReserveSubmit}
        />
      )}

      {mode.kind === 'confirmed' && (
        <Confirmation reservation={mode.reservation} onNewSearch={handleNewSearch} />
      )}
    </div>
  )
}

export default App
