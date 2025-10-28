# API-Minimizer

API-Minimizer is a .NET 6.0 API project that provides various API endpoints including status checks and external API consumers.

## Features

### ESPN NFL API Consumer
The API includes endpoints to consume ESPN's NFL scoreboard data.

#### Endpoints

**GET /api/EspnNfl**
- Retrieves NFL scoreboard data for a specific year
- Query Parameters:
  - `year` (optional, default: 2023): The year for which to retrieve NFL scoreboard data
  - `limit` (optional, default: 1000): Maximum number of results to return
- Example: `GET /api/EspnNfl?year=2023&limit=1000`

**GET /api/EspnNfl/daterange**
- Retrieves NFL scoreboard data for a specific date range
- Query Parameters:
  - `startDate` (required): Start date in YYYYMMDD format
  - `endDate` (required): End date in YYYYMMDD format
  - `limit` (optional, default: 1000): Maximum number of results to return
- Example: `GET /api/EspnNfl/daterange?startDate=20230901&endDate=20231231&limit=1000`

### Status API
Check the health and status of the API.

## Building and Running

### Prerequisites
- .NET 6.0 SDK or later

### Build
```bash
cd API-Minimizer
dotnet build API-Minimizer.sln
```

### Run
```bash
cd API-Minimizer/ApiBack/API-Minimizer-back
dotnet run
```

### Swagger Documentation
When running in development mode, Swagger UI is available at:
- `https://localhost:5001/swagger`
- `http://localhost:5000/swagger`

## Testing
```bash
cd API-Minimizer
dotnet test API-Minimizer.sln
```