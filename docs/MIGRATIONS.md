# Veritabanı Migration Rehberi

## Mevcut durum

Uygulama şu anda şemayı iki yolla kuruyor:

1. `Program.cs` içinde `await dbContext.Database.EnsureCreatedAsync();`
2. `DbInitializer.EnsureVehicleCatalogSchemaAsync` içinde elle yazılmış
   idempotent `CREATE TABLE IF NOT EXISTS` / `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`
   SQL blokları.

Bu yaklaşım çalışır ama **versiyonlanmış migration** sağlamaz: şema değişiklikleri
takip edilemez, geri alınamaz ve elle yazılan SQL ile gerçek model arasında sapma
(drift) oluşabilir. `EnsureCreated` ile EF Core migration'ları **aynı anda kullanılamaz**.

## Hedef: EF Core migration'larına geçiş

Tasarım-zamanı altyapısı hazırdır: `src/Persistence/DesignTimeDbContextFactory.cs`
sayesinde `dotnet ef` komutları DI'a ihtiyaç duymadan çalışır. Bağlantı dizesi
`AUTOGALLERY_DB` ortam değişkeninden okunur (yoksa yerel PostgreSQL varsayılanı).

> Not: Bu repoyu klonlayan ortamda .NET SDK kurulu olmayabilir. Aşağıdaki komutlar
> .NET 8 SDK + `dotnet-ef` aracının bulunduğu bir ortamda çalıştırılmalıdır
> (`dotnet-tools.json` zaten `dotnet-ef` 10.x'i tanımlıyor; .NET 8 hedefi için
> `dotnet tool install dotnet-ef --version 8.*` tercih edilebilir).

### 1. Aracı geri yükle

```bash
dotnet tool restore
```

### 2. İlk migration'ı üret

```bash
dotnet ef migrations add InitialCreate \
  --project src/Persistence/AutoGallerySaaS.Persistence.csproj \
  --startup-project src/Api/AutoGallerySaaS.Api.csproj
```

### 3. `Program.cs`'i güncelle

`EnsureCreatedAsync` çağrısını migration uygulamasıyla değiştir:

```csharp
// await dbContext.Database.EnsureCreatedAsync();
await dbContext.Database.MigrateAsync();
```

### 4. (Geçiş senaryosu) Mevcut veritabanı zaten doluysa

`EnsureCreated` ile oluşturulmuş mevcut bir veritabanı varsa, ilk migration'ı
"uygulanmış" kabul ettirmek için tabloları silmeden migration geçmişini işaretle:

```bash
dotnet ef migrations add InitialCreate ...   # yukarıdaki gibi
dotnet ef database update --connection "$AUTOGALLERY_DB"
```

Şema farklıysa, `__EFMigrationsHistory` tablosunu elle ekleyip `InitialCreate`
satırını işaretlemek (baseline) gerekebilir. Detay:
<https://learn.microsoft.com/ef/core/managing-schemas/migrations/managing#baselining>

### 5. Sonraki değişiklikler

Model değiştikçe:

```bash
dotnet ef migrations add <AnlamliIsim> --project src/Persistence/... --startup-project src/Api/...
dotnet ef database update
```

## Temizlik (geçiş tamamlandığında)

Migration'lara tam geçiş yapıldıktan sonra `DbInitializer.EnsureVehicleCatalogSchemaAsync`
içindeki elle `ALTER/CREATE` SQL'leri kaldırılabilir; şema yönetimi tamamen
migration'lara devredilir. `DbInitializer` yalnızca **seed verisi** (planlar,
izinler, demo tenant/kullanıcı, ortak katalog) için kullanılmaya devam eder.
