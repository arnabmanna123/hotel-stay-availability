import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { SearchForm } from '../components/SearchForm'

const cities = [{ name: 'London', class: 'Domestic' as const }]

describe('SearchForm', () => {
  it('submits the selected destination and dates', async () => {
    const user = userEvent.setup()
    const onSearch = vi.fn()

    render(<SearchForm cities={cities} disabled={false} onSearch={onSearch} />)

    await user.selectOptions(screen.getByLabelText('Destination'), 'London')
    await user.clear(screen.getByLabelText('Check-in'))
    await user.type(screen.getByLabelText('Check-in'), '2026-08-01')
    await user.clear(screen.getByLabelText('Check-out'))
    await user.type(screen.getByLabelText('Check-out'), '2026-08-04')
    await user.click(screen.getByRole('button', { name: /search/i }))

    expect(onSearch).toHaveBeenCalledWith({
      destination: 'London',
      checkIn: '2026-08-01',
      checkOut: '2026-08-04',
      roomType: null,
    })
  })

  it('shows a validation message when check-out is not after check-in', async () => {
    render(<SearchForm cities={cities} disabled={false} onSearch={vi.fn()} />)

    const checkIn = screen.getByLabelText('Check-in')
    const checkOut = screen.getByLabelText('Check-out')

    await userEvent.clear(checkIn)
    await userEvent.type(checkIn, '2026-08-05')
    await userEvent.clear(checkOut)
    await userEvent.type(checkOut, '2026-08-04')

    expect(screen.getByText('Check-out must be after check-in.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /search/i })).toBeDisabled()
  })
})
