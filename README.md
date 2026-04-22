# Auto Gallery SaaS Backend

Bu proje, SaaS modelinde çalışan, multi-tenant bir oto galeri yönetim sistemidir.

## Teknolojiler
- .NET 8 Web API
- PostgreSQL
- Entity Framework Core
- JWT + Refresh Token
- Serilog
- Docker

## Mimari
- **Domain:** Core entityler, arayüzler ve domain mantığı.
- **Application:** Business logic, DTOs, servis arayüzleri.
- **Infrastructure:** JWT, Logging, File Storage implementasyonları.
- **Persistence:** EF Core DbContext, Migrations, Seeding.
- **Api:** RESTful endpointler, Middleware.

## Multi-Tenancy
Sistem, paylaşımlı veritabanı (Shared Database) modelini kullanır. Her tabloda `TenantId` alanı bulunur ve EF Core Global Query Filter'lar ile veriler izole edilir.

## Kurulum
```bash
docker-compose up --build
```

API'ye `http://localhost:5000/swagger` adresinden erişilebilir.
