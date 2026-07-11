import { useState } from 'react'
import type { CityClass, DocumentType, Room } from '../types'

interface Props {
  room: Room
  destination: string
  cityClass: CityClass
  checkIn: string
  checkOut: string
  nights: number
  disabled: boolean
  onCancel: () => void
  onSubmit: (payload: {
    guestName: string
    documentType: DocumentType
    documentNumber: string
  }) => void
}

// Mirror of the server-side rule (spec §5.1) — this is UX only; the server is the authority.
function clientSideDocError(
  cityClass: CityClass,
  documentType: DocumentType,
  documentNumber: string,
): string | null {
  if (!documentNumber.trim()) return 'Document number is required.'
  if (cityClass === 'International' && documentType !== 'Passport') {
    return 'Passport required for international destinations.'
  }
  return null
}

export function ReserveForm({
  room,
  destination,
  cityClass,
  checkIn,
  checkOut,
  nights,
  disabled,
  onCancel,
  onSubmit,
}: Props) {
  const [guestName, setGuestName] = useState('')
  const [documentType, setDocumentType] = useState<DocumentType>(
    cityClass === 'International' ? 'Passport' : 'NationalId',
  )
  const [documentNumber, setDocumentNumber] = useState('')

  const docError = clientSideDocError(cityClass, documentType, documentNumber)
  const nameEmpty = !guestName.trim()
  const canSubmit = !disabled && !nameEmpty && docError === null

  return (
    <section className="reserve">
      <header className="reserve__head">
        <h2>Confirm your reservation</h2>
        <button
          type="button"
          className="link"
          onClick={onCancel}
          disabled={disabled}
        >
          ← back to results
        </button>
      </header>

      <dl className="summary">
        <div><dt>Destination</dt><dd>{destination} <em>({cityClass})</em></dd></div>
        <div><dt>Dates</dt><dd>{checkIn} → {checkOut} ({nights} night{nights === 1 ? '' : 's'})</dd></div>
        <div><dt>Room</dt><dd>{room.roomType} · {room.providerId}</dd></div>
        <div><dt>Total</dt><dd>{room.currency} {room.totalPrice.toFixed(2)}</dd></div>
      </dl>

      <form
        className="reserve__form"
        onSubmit={(e) => {
          e.preventDefault()
          if (!canSubmit) return
          onSubmit({ guestName: guestName.trim(), documentType, documentNumber: documentNumber.trim() })
        }}
      >
        <label>
          Guest name
          <input
            type="text"
            value={guestName}
            onChange={(e) => setGuestName(e.target.value)}
            required
            disabled={disabled}
          />
        </label>

        <fieldset className="doc-type" disabled={disabled}>
          <legend>Document type</legend>
          <label>
            <input
              type="radio"
              name="doctype"
              value="Passport"
              checked={documentType === 'Passport'}
              onChange={() => setDocumentType('Passport')}
            />
            Passport
          </label>
          <label>
            <input
              type="radio"
              name="doctype"
              value="NationalId"
              checked={documentType === 'NationalId'}
              onChange={() => setDocumentType('NationalId')}
            />
            National ID
            {cityClass === 'International' && (
              <span className="hint"> (not accepted for international)</span>
            )}
          </label>
        </fieldset>

        <label>
          Document number
          <input
            type="text"
            value={documentNumber}
            onChange={(e) => setDocumentNumber(e.target.value)}
            required
            disabled={disabled}
          />
        </label>

        {docError && <p className="inline-warn">{docError}</p>}

        <button type="submit" disabled={!canSubmit}>
          {disabled ? 'Confirming…' : 'Confirm reservation'}
        </button>
      </form>
    </section>
  )
}
