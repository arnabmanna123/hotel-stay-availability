import type { Room } from '../types'

interface Props {
  room: Room
  nights: number
  onReserve: () => void
}

function formatMoney(amount: number, currency: string): string {
  return `${currency} ${amount.toFixed(2)}`
}

function formatPolicy(room: Room): string {
  const { type, hoursBeforeCheckIn } = room.cancellationPolicy
  switch (type) {
    case 'FreeCancellation':
      return `Free cancellation up to ${hoursBeforeCheckIn}h before check-in`
    case 'Flexible':
      return `Flexible cancellation up to ${hoursBeforeCheckIn}h before check-in`
    case 'NonRefundable':
      return 'Non-refundable'
  }
}

export function RoomCard({ room, nights, onReserve }: Props) {
  return (
    <article className="room-card" data-testid={`room-${room.id}`}>
      <header className="room-card__head">
        <span className={`badge badge--${room.providerId.toLowerCase()}`}>
          {room.providerId}
        </span>
        <span className="badge badge--type">{room.roomType}</span>
        {room.starRating !== null && (
          <span className="stars" aria-label={`${room.starRating} star rating`}>
            {'★'.repeat(room.starRating)}
            <span className="stars__empty">{'★'.repeat(5 - room.starRating)}</span>
          </span>
        )}
      </header>

      {room.amenities.length > 0 && (
        <ul className="amenities">
          {room.amenities.map((a) => (
            <li key={a}>{a}</li>
          ))}
        </ul>
      )}

      <p className="policy">{formatPolicy(room)}</p>

      <footer className="room-card__foot">
        <div className="prices">
          <div className="per-night">
            {formatMoney(room.pricePerNight, room.currency)}{' '}
            <small>/ night</small>
          </div>
          <div className="total">
            {formatMoney(room.totalPrice, room.currency)}{' '}
            <small>for {nights} night{nights === 1 ? '' : 's'}</small>
          </div>
        </div>
        <button type="button" onClick={onReserve}>
          Reserve
        </button>
      </footer>
    </article>
  )
}
