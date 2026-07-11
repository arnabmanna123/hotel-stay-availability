import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../App'
import * as hotelsApi from '../api/hotels'

vi.mock('../api/hotels')

const mockCities = [
  { name: 'London', class: 'Domestic' },
  { name: 'Paris', class: 'International' },
]

const mockRooms = [
  {
    id: 'PS-LON-STD-001',
    providerId: 'PremierStays',
    roomType: 'Standard',
    pricePerNight: 75,
    totalPrice: 150,
    currency: 'USD',
    cancellationPolicy: { type: 'FreeCancellation', hoursBeforeCheckIn: 48 },
    amenities: ['WiFi'],
    starRating: 4,
  },
]

const reservation = {
  reference: 'HS-12345678',
  providerId: 'PremierStays',
  roomId: 'PS-LON-STD-001',
  roomType: 'Standard',
  checkIn: '2026-09-01',
  checkOut: '2026-09-03',
  nights: 2,
  pricePerNight: 75,
  totalPrice: 150,
  currency: 'USD',
  cancellationPolicy: { type: 'FreeCancellation', hoursBeforeCheckIn: 48 },
  guestName: 'Grace Hopper',
  documentType: 'NationalId',
  documentNumber: 'GH-1906',
}

const mockedApi = hotelsApi as unknown as {
  fetchCities: vi.Mock
  searchRooms: vi.Mock
  reserveRoom: vi.Mock
  fetchReservation: vi.Mock
}

describe('App', () => {
  beforeEach(() => {
    mockedApi.fetchCities.mockResolvedValue(mockCities)
    mockedApi.searchRooms.mockResolvedValue({ nights: 2, currency: 'USD', results: mockRooms })
    mockedApi.reserveRoom.mockResolvedValue(reservation)
    mockedApi.fetchReservation.mockResolvedValue(reservation)
  })

  it('renders search form and performs a full search + reserve flow', async () => {
    const user = userEvent.setup()
    render(<App />)

    expect(await screen.findByLabelText('Destination')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /search/i }))

    expect(await screen.findByRole('heading', { name: /1\s+room\s+found/i })).toBeInTheDocument()
    expect(screen.getByText('PremierStays')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /reserve/i }))

    expect(await screen.findByRole('button', { name: /confirm reservation/i })).toBeInTheDocument()

    await user.type(screen.getByLabelText(/Guest name/i), 'Grace Hopper')
    await user.type(screen.getByLabelText(/Document number/i), 'GH-1906')
    await user.click(screen.getByRole('button', { name: /confirm reservation/i }))

    expect(await screen.findByText(/Reservation confirmed/i)).toBeInTheDocument()
    expect(screen.getByText(/HS-12345678/)).toBeInTheDocument()
  })

  it('looks up an existing reservation by reference', async () => {
    render(<App />)

    expect(await screen.findByLabelText('Reservation reference')).toBeInTheDocument()

    await userEvent.type(screen.getByLabelText('Reservation reference'), 'HS-12345678')
    await userEvent.click(screen.getByRole('button', { name: /look up/i }))

    expect(await screen.findByText(/Reservation confirmed/i)).toBeInTheDocument()
    expect(screen.getByText(/HS-12345678/)).toBeInTheDocument()
  })
})
