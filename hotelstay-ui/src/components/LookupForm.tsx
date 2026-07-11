import { useState, type FormEvent } from 'react'

interface Props {
  disabled?: boolean
  onLookup: (reference: string) => void
}

const REFERENCE_PATTERN = /^HS-[A-Z0-9]{8}$/

export function LookupForm({ disabled, onLookup }: Props) {
  const [reference, setReference] = useState('')

  const trimmed = reference.trim().toUpperCase()
  const valid = REFERENCE_PATTERN.test(trimmed)

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!valid || disabled) return
    onLookup(trimmed)
  }

  return (
    <section className="lookup" aria-label="Look up existing reservation">
      <p className="lookup__title">Already booked? Look up your reservation</p>
      <form className="lookup__form" onSubmit={handleSubmit}>
        <input
          type="text"
          className="lookup__input"
          placeholder="HS-XXXXXXXX"
          value={reference}
          onChange={(e) => setReference(e.target.value.toUpperCase())}
          disabled={disabled}
          maxLength={11}
          aria-label="Reservation reference"
          spellCheck={false}
          autoCapitalize="characters"
        />
        <button type="submit" disabled={disabled || !valid}>
          Look up
        </button>
      </form>
    </section>
  )
}
