# Cache Me If You Can

## Overview
Cache Me If You Can is a .NET Core Web API that simulates a small e-commerce backend with an emphasis on performance optimization using distributed caching (e.g., Redis). The system demonstrates how caching strategies improve response times and reduce database load.

## Features
- Product catalog CRUD operations
- Category-based product filtering
- Shopping cart management
- Cache-aside pattern implementation
- Distributed caching with Redis
- Cache invalidation on updates
- Rate limiting for API endpoints
- Logging and telemetry

## Tech Stack
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server (or PostgreSQL)
- Redis (distributed cache)
- MediatR (optional for CQRS)
- Serilog for structured logging

## Architecture
- Layered architecture:
  - Controllers (API layer)
  - Services (business logic)
  - Repositories (data access)
- Use Dependency Injection throughout
- Implement caching at the service layer

## Key Concepts
- Cache-aside pattern
- Cache invalidation strategies
- Distributed systems performance optimization

## Example Endpoints
- GET /api/products
- GET /api/products/{id}
- POST /api/cart/add
- PUT /api/products/{id}

## Stretch Goals
- Add background job to warm cache
- Implement CQRS pattern
- Add authentication with JWT