# Dockerization & Production Readiness Walkthrough

Bu aşamada AuthApi projesini tamamen konteynerize ettik ve "canlı" (production) ortama uygun hale getirdik.

## Yapılan Değişiklikler

### 1. [NEW] [Dockerfile](file:///c:/Users/pasaa/Desktop/backend/Dockerfile)
- **Multi-stage Build:** Uygulamayı SDK ile derleyip, sadece Runtime üzerinde çalıştırıyoruz. Bu, imaj boyutunu küçültür ve kaynak tüketimini azaltır.
- **Port 8080:** .NET 8 standartlarına uygun olarak 8080 portunu dışa açtık.

### 2. [NEW] [docker-compose.yml](file:///c:/Users/pasaa/Desktop/backend/docker-compose.yml)
- **MSSQL 2022:** Resmi Microsoft SQL Server imajını ekledik.
- **Port ve Ağ:** API 8080'den, Veritabanı 1433'ten (isteğe bağlı) erişilebilir durumda.
- **Environment Variables:** Veritabanı bağlantı cümlesini ve anahtar bilgilerini Docker üzerinden yönetilebilir hale getirdik.

### 3. [MODIFY] [Program.cs](file:///c:/Users/pasaa/Desktop/backend/Program.cs)
- **HTTPS Kontrolü:** `RequireHttpsMetadata = !builder.Environment.IsDevelopment();` olarak güncellendi. Artık canlıda gerçek SSL doğrulaması arayacak, lokalde ise sorunsuz çalışmaya devam edecek.

## Nasıl Çalıştırılır?

Proje kök dizininde aşağıdaki komutla tüm sistemi ayağa kaldırabilirsiniz:

```bash
docker-compose up --build -d
```

> [!IMPORTANT]
> **Güvenlik Hatırlatması:** 
> - `docker-compose.yml` içindeki `JwtSettings__Key` ve `MSSQL_SA_PASSWORD` değerlerini gerçek ortamda mutlaka kendiniz belirlediğiniz çok güçlü şifrelerle değiştirin.
> - Seeding logic sayesinde ilk `Admin` kullanıcısı otomatik oluşturulacaktır.

## Doğrulama
- API: `http://localhost:8080/swagger`
- DB: `1433` portu üzerinden SQL Management Studio ile bağlanılabilir.
