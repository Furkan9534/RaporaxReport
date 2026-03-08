# RaporaxReport - Authentication Sistemi

## Genel Mimari Özet

| Katman | Teknoloji |
|--------|-----------|
| API | ASP.NET Core Web API (.NET 8) |
| ORM | Entity Framework Core |
| Identity | ASP.NET Core Identity |
| Auth | JWT Bearer Token |
| Veritabanı | SQL Server (tek DB, multi-tenant) |
| Container | Docker |

---

## Proje Parçaları (Sıralı Yapım Planı)

```
[1] Temel Auth Altyapısı   ← ŞU AN BURADAYIZ
[2] Şifre Yönetimi
[3] Rol Tabanlı Yetkilendirme
[4] Rate Limiting
[5] Email / Şifre Sıfırlama
[6] Test Katmanı
[7] Docker & Deployment
```

---

## BÖLÜM 1 — Temel Auth Altyapısı (İlk Yapılacak Kısım)

Bu bölümde sistemi ayağa kaldıran temel iskelet kurulur.
Diğer tüm bölümler bu üzerine inşa edilir.

### 1.1 Proje Kurulumu

```
RaporaxReport.sln
├── src/
│   └── RaporaxReport.API/          ← ASP.NET Core Web API projesi
│       ├── Controllers/
│       │   └── AuthController.cs   ← Login endpoint
│       ├── Data/
│       │   └── AppDbContext.cs     ← EF Core context
│       ├── Models/
│       │   ├── AppUser.cs          ← Identity kullanıcı modeli
│       │   └── Company.cs          ← Tenant/firma modeli
│       ├── DTOs/
│       │   ├── LoginRequest.cs
│       │   └── LoginResponse.cs
│       ├── Services/
│       │   └── TokenService.cs     ← JWT üretimi
│       └── Program.cs
└── tests/
    └── RaporaxReport.Tests/        ← Unit/Integration testler
```

### 1.2 Veritabanı Şeması

Tek bir veritabanı kullanılır. Multi-tenant yapı, her tabloya `CompanyId` (TenantId) eklenerek sağlanır.

```sql
-- ASP.NET Core Identity tabloları (otomatik üretilir)
AspNetUsers        -- Kullanıcılar (AppUser extend eder)
AspNetRoles        -- Roller
AspNetUserRoles    -- Kullanıcı-Rol ilişkisi

-- Ek tablolar
Companies          -- Firmalar / Tenant bilgisi
  Id (Guid)
  Name (string)
  IsActive (bool)
```

`AppUser` (Identity'yi extend eder):
```csharp
public class AppUser : IdentityUser
{
    public Guid CompanyId { get; set; }       // Hangi firmaya ait
    public Company Company { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 1.3 JWT Yapısı

Token içinde taşınan claim'ler:

```json
{
  "sub":        "user-guid",
  "email":      "kullanici@firma.com",
  "name":       "Ad Soyad",
  "companyId":  "tenant-guid",          // Multi-tenant kimliği
  "role":       "ReportViewer",         // Rol
  "jti":        "token-unique-id",
  "iat":        1710000000,
  "exp":        1710003600
}
```

- Token **stateless**'tır; server hiçbir oturum bilgisi tutmaz.
- Expiry: 1 saat (access token), yapılandırılabilir.
- İmzalama: HS256 (secret key environment variable'dan gelir).

### 1.4 Login Endpoint

```
POST /api/auth/login
Content-Type: application/json

{
  "email": "kullanici@firma.com",
  "password": "Sifre123!"
}
```

**Başarılı yanıt (200):**
```json
{
  "token": "eyJhbGci...",
  "expiresAt": "2026-03-09T11:00:00Z",
  "user": {
    "id": "guid",
    "email": "kullanici@firma.com",
    "fullName": "Ad Soyad",
    "companyId": "tenant-guid",
    "role": "ReportViewer"
  }
}
```

**Hatalı yanıt (401):**
```json
{
  "message": "Geçersiz e-posta veya şifre."
}
```
> Güvenlik: Kullanıcının var olup olmadığı hata mesajında **kesinlikle açıklanmaz** (OWASP A07).

### 1.5 Güvenlik Kuralları (Bu Bölümde Uygulananlar)

| Risk | Önlem |
|------|-------|
| Brute force | Rate limiting (Bölüm 4'te detaylanır, şimdilik temel middleware) |
| SQL Injection | EF Core ORM — raw query yok |
| Bilgi sızıntısı | Generic hata mesajları |
| Zayıf şifre | Identity `PasswordOptions` ile politika tanımı |
| Token güvensizliği | Secret key environment variable'dan, kod içinde hardcode yok |

### 1.6 Parola Politikası (Program.cs'de tanımlanır)

```csharp
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequiredLength = 8;
```

---

## Bu Bölümde Yapılacaklar (Checklist)

- [ ] `dotnet new webapi` ile proje oluştur
- [ ] EF Core + ASP.NET Core Identity paketlerini ekle
- [ ] `AppUser` ve `Company` modellerini yaz
- [ ] `AppDbContext`'i konfigüre et
- [ ] JWT ayarlarını `appsettings.json` ve `Program.cs`'e ekle
- [ ] `TokenService`'i yaz (claim'leri oluştur, token imzala)
- [ ] `AuthController` — `POST /api/auth/login` endpoint'ini yaz
- [ ] İlk migration'ı oluştur ve veritabanını seed et (1 firma + 1 admin kullanıcı)
- [ ] Postman / .http dosyasıyla login testi yap

---

## Sonraki Bölüm

**Bölüm 2 — Şifre Yönetimi:**
`POST /api/auth/change-password` ve `POST /api/auth/forgot-password` endpoint'leri,
email doğrulama token üretimi ve SMTP entegrasyonu.

---

*Son güncelleme: 2026-03-09*
