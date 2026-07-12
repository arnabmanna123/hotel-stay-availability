# Copilot instructions for this repository

- This repository contains a .NET 8/10 API in HotelStay.Api and a Vite + React + TypeScript frontend in hotelstay-ui.
- Preserve the existing API contract unless a change explicitly updates the specification.
- Prefer small, targeted changes and keep tests alongside the behavior they cover.
- Backend verification command: dotnet test HotelStay.Tests/HotelStay.Tests.csproj
- Frontend verification command: npm test -- --run --watch=false
- When changing endpoint behavior, keep error codes and status codes aligned with the documented contract.
