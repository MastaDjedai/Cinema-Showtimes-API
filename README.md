# Cinema Showtimes API

Small ASP.NET Core REST API for movies, showtimes, and seat reservations.

A reservation holds seats for **10 minutes**. Pending reservations expire after that and the seats become available again. Sold seats and currently reserved seats cannot be reserved twice.

## Requirements

- .NET 10 SDK

## Run

```bash
cd "Cinema Showtimes API"
dotnet run
```

The API listens on `http://localhost:5208` (see `Properties/launchSettings.json`). SQLite database file `cinema.db` is created next to the app on first start.

OpenAPI document (Development): `http://localhost:5208/openapi/v1.json`

## Seeded data

On startup the database is created (if missing) and seeded with:

- Movies: *The Matrix*, *Amelie*, *Spirited Away*, *Dune: Part Two*, *Parasite*
- Auditoriums:
  - **Hall 1** (`aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1`) — rows A–E, seats 1–10
  - **Hall 2** (`aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2`) — rows A–F, seats 1–12

Movie ids are stable:

| Title | Id |
| --- | --- |
| The Matrix | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1` |
| Amelie | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2` |
| Spirited Away | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3` |
| Dune: Part Two | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4` |
| Parasite | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5` |

## Endpoints

| Method | Path | Description |
| --- | --- | --- |
| `GET` | `/api/movies` | List movies |
| `POST` | `/api/movies` | Create a movie |
| `GET` | `/api/auditoriums` | List auditoriums and seats |
| `POST` | `/api/showtimes` | Create a showtime |
| `POST` | `/api/showtimes/{showtimeId}/reservations` | Reserve specific seats |
| `POST` | `/api/showtimes/{showtimeId}/reservations/contiguous` | Reserve a contiguous block |
| `POST` | `/api/reservations/{reservationReference}/confirm` | Confirm (buy) a reservation |

### Create a movie

```http
POST /api/movies
Content-Type: application/json

{
  "title": "Heat",
  "category": "Crime",
  "year": 1995,
  "durationMinutes": 170
}
```

### Create a showtime

`startTime` must be in the future.

```http
POST /api/showtimes
Content-Type: application/json

{
  "movieId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
  "auditoriumId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
  "startTime": "2026-10-01T18:30:00Z"
}
```

### Reserve seats

```http
POST /api/showtimes/{showtimeId}/reservations
Content-Type: application/json

{
  "seats": [
    { "row": "A", "number": 5 },
    { "row": "A", "number": 6 }
  ]
}
```

Response includes `reservationReference`, `seatCount`, `auditorium`, `movie`, `expiresAt`, and the seats.

### Reserve contiguous seats

Finds the first free block of consecutive seats in the same row (row A upward, lowest seat numbers first).

```http
POST /api/showtimes/{showtimeId}/reservations/contiguous
Content-Type: application/json

{
  "seatCount": 3
}
```

Returns `409` with error code `no_contiguous_block` if no such block exists.

### Confirm a reservation

```http
POST /api/reservations/{reservationReference}/confirm
```

Confirmation is rejected if the reservation is already confirmed or older than 10 minutes.

## Errors

Request validation (missing fields, ranges, empty seat list) returns **400** with ASP.NET `ValidationProblemDetails`.

Domain errors use `ProblemDetails` plus an `errorCode` field:

| HTTP | Typical `errorCode` | When |
| --- | --- | --- |
| 404 | `movie_not_found`, `auditorium_not_found`, `showtime_not_found`, `reservation_not_found` | Unknown id |
| 409 | `seats_already_sold`, `seats_currently_reserved`, `reservation_expired`, `already_confirmed`, `no_contiguous_block`, `showtime_overlap` | Booking conflict |
| 422 | `duplicate_seats`, `unknown_seats`, `showtime_in_the_past` | Business rule / invalid combination |

Expired pending reservations are marked `Expired` when someone tries to reserve seats for that showtime or confirm the old reservation. Those seats can then be reserved again.
