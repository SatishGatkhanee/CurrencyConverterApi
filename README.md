# Currency Converter API

A robust, scalable, and secure ASP.NET Core Web API for real-time and historical currency conversions. Supports JWT authentication, rate limiting, structured logging, distributed tracing, and dynamic provider selection.

---

## Setup Instructions

### Prerequisites

- .NET 8 SDK
- Redis (optional — for distributed caching)
- Seq or ELK Stack (optional — for structured logging)
- Docker (optional — for containerized deployment)
- PostgreSQL or SQL Server (optional — for persistence)
  
### Clone the Repository
  
  - git clone https://github.com/SatishGatkhanee/CurrencyConverterApi.git
  - cd CurrencyConverterApi

### Configure App Settings
- Edit appsettings.{Environment}.json as needed:
json
{
  "Jwt": {
    "Issuer": "your-app",
    "Audience": "your-users",
    "SecretKey": "your-super-secure-secret"
  },
  "ExchangeProvider": "frankfurter",
  "AllowedHosts": "*"
}

### Run the Application
bash
dotnet run --launch-profile "Development"

### Access Swagger UI
https://localhost:{port}/swagger

### Run Tests with Coverage
bash
dotnet test --collect:"XPlat Code Coverage"

### Generate Coverage Report
bash
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html

### Authentication Endpoint**

The API includes a basic JWT-based authentication controller to issue tokens for secured access.

POST `/api/auth/login`
- Description: Generates a JWT token upon successful login using hardcoded credentials. This token can then be used for authenticated and role-based requests.

Sample Request Body: #json
`{
  "username": "admin",
  "password": "admin123"
}`


### Available Users

| Username | Password  | Role  |
|----------|-----------|-------|
| admin    | admin123  | Admin |
| user     | user123   | User  |


Sample Response: #json
`{
  "token": "{JWT token here}"
}`

Notes:
The JWT payload includes:
- name claim with the username
- role claim with user role (Admin or User)
- Token expiration is defined in `appsettings.json` under `Jwt:ExpireMinutes`.
- Authentication is disabled by default via `"IsAuthEnabled": false`. You can enable it by setting this to true.

### Features
- JWT Authentication with Role-Based Access Control (RBAC)
- Real-time & Historical Currency Conversion
- Excludes TRY, PLN, THB, and MXN with 400 Bad Request
- Clean Architecture with Factory-Based Provider Selection
- Scalable with Horizontal Scaling Support
- Resilience via Polly (Retry, Circuit Breaker)
- In-memory Caching
- Structured Logging with Serilog (Seq/ELK)
- Distributed Tracing via OpenTelemetry
- API Versioning for forward compatibility
- Unit & Integration Tests

### Core Endpoints
- GET /api/v1/rates/latest?base=USD — Latest exchange rates
- GET /api/v1/convert?from=USD&to=INR&amount=100 — Currency conversion
- GET /api/v1/rates/history?base=EUR&start=2020-01-01&end=2020-01-31&page=1&pageSize=10 — Historical rates (paginated)

### Assumptions
- The default exchange provider is Frankfurter API
- JWT client_id claim is used for logging and access control
- Rate limiting is IP-based using AspNetCoreRateLimit
- Structured logs include client IP, user claims, status code, method, path, and response time
- Middleware and services are registered via dependency injection
- Provider architecture allows future plug-in support

### Future Enhancements
- Add support for additional exchange providers (Fixer.io, OpenExchangeRates)
- Add Dockerfile, Compose, and Helm for container deployment
- Integrate OAuth2 enterprise-grade auth
- OpenAPI compliance testing via Postman/Newman
- Currency formatting and localization based on locale
