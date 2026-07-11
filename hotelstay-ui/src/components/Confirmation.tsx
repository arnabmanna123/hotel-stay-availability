import type { Reservation } from '../types'

interface Props {
  reservation: Reservation
  onNewSearch: () => void
}

function formatPolicy(reservation: Reservation): string {
  const { type, hoursBeforeCheckIn } = reservation.cancellationPolicy
  switch (type) {
    case 'FreeCancellation':
      return `Free cancellation up to ${hoursBeforeCheckIn}h before check-in`
    case 'Flexible':
      return `Flexible cancellation up to ${hoursBeforeCheckIn}h before check-in`
    case 'NonRefundable':
      return 'Non-refundable'
  }
}

export function Confirmation({ reservation, onNewSearch }: Props) {
  return (
    <section className="confirmation">
      <header>
        <h2>✓ Reservation confirmed</h2>
        <p className="reference">
          Reference: <strong>{reservation.reference}</strong>
        </p>
      </header>

      <dl className="summary">
        <div><dt>Provider</dt><dd>{reservation.providerId}</dd></div>
        <div><dt>Room</dt><dd>{reservation.roomType}</dd></div>
        <div><dt>Guest</dt><dd>{reservation.guestName}</dd></div>
        <div>
          <dt>Dates</dt>
          <dd>
            {reservation.checkIn} → {reservation.checkOut} ({reservation.nights} night
            {reservation.nights === 1 ? '' : 's'})
          </dd>
        </div>
        <div>
          <dt>Total price</dt>
          <dd className="total">
            {reservation.currency} {reservation.totalPrice.toFixed(2)}
          </dd>
        </div>
        <div><dt>Cancellation</dt><dd>{formatPolicy(reservation)}</dd></div>
      </dl>

      <button type="button" onClick={onNewSearch}>
        New search
      </button>
    </section>
  )
}
