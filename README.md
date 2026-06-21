# AutoGallery SaaS

Multi-tenant oto galeri yönetim sistemi. Backend `.NET 8 Web API`, frontend `React + Vite` ile hazırlanmıştır.

## Backend

Teknolojiler:
- .NET 8 Web API
- PostgreSQL
- Entity Framework Core
- JWT
- Swagger

Çalıştırma:

```powershell
dotnet run --project src/Api/AutoGallerySaaS.Api.csproj
```

Swagger:

[http://localhost:5056/swagger/index.html](http://localhost:5056/swagger/index.html)

## Frontend

Frontend klasörü:

[frontend](C:/OtoGaleri/OtoGaleri2/Otogaleri2/frontend)

Kullanılan ekranlar:
- Login / Register
- Dashboard
- Satınalma
- Satış
- Araçlar
- Stok

Çalıştırma:

```powershell
cd frontend
npm install
npm run dev
```

Vite geliştirme sunucusu varsayılan olarak `http://localhost:5173` adresinde açılır ve `/api` isteklerini backend'e proxy eder.

İsterseniz farklı bir API adresi için [frontend/.env.example](C:/OtoGaleri/OtoGaleri2/Otogaleri2/frontend/.env.example) dosyasını kopyalayıp `.env` oluşturabilirsiniz.
