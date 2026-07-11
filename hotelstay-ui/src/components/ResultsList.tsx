import { useMemo, useState } from 'react'
import type { Room } from '../types'
import { RoomCard } from './RoomCard'

interface Props {
  rooms: Room[]
  nights: number
  onReserve: (room: Room) => void
}

type SortOrder = 'asc' | 'desc'

export function ResultsList({ rooms, nights, onReserve }: Props) {
  const [order, setOrder] = useState<SortOrder>('asc')

  const sorted = useMemo(() => {
    const copy = [...rooms]
    copy.sort((a, b) =>
      order === 'asc' ? a.totalPrice - b.totalPrice : b.totalPrice - a.totalPrice,
    )
    return copy
  }, [rooms, order])

  if (rooms.length === 0) {
    return (
      <div className="empty-state">
        <p>No rooms available for the selected destination and dates.</p>
      </div>
    )
  }

  return (
    <section className="results">
      <div className="results__head">
        <h2>
          {rooms.length} room{rooms.length === 1 ? '' : 's'} found
        </h2>
        <label className="sort">
          Sort by total price:
          <select
            value={order}
            onChange={(e) => setOrder(e.target.value as SortOrder)}
          >
            <option value="asc">Low to high</option>
            <option value="desc">High to low</option>
          </select>
        </label>
      </div>
      <div className="results__list">
        {sorted.map((r) => (
          <RoomCard key={r.id} room={r} nights={nights} onReserve={() => onReserve(r)} />
        ))}
      </div>
    </section>
  )
}
