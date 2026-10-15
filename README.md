# RetailStock

Sistema de inventario y ventas para retail (supermercado / comisariato mayorista).
.NET 8 · ASP.NET Core Web API · SQL Server 2022 · EF Core + Dapper · JWT · React.

## Estructura

```
src/
  RetailStock.Domain/          Entidades y reglas de negocio (sin dependencias)
  RetailStock.Application/     DTOs, interfaces, casos de uso, validaciones
  RetailStock.Infrastructure/  EF Core, Dapper, JWT, cliente SAP
  RetailStock.Api/             Controladores, middleware, Program.cs
tests/
  RetailStock.Tests/           xUnit + Moq
retailstock-front/             React + Vite (Fase 9)
```

## Arranque rápido

```bash
cp .env.example .env
docker compose up -d                                  # SQL Server en localhost:1433
dotnet run --project src/RetailStock.Api --launch-profile http
# Swagger: http://localhost:5080/swagger
```

> README completo con diagrama de capas en la Fase 10.
