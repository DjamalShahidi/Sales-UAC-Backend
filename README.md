# RbacApp — سیستم مدیریت کاربران و RBAC چندسازمانی (بک‌اند)

سیستم مدیریت کاربران و کنترل سطح دسترسی (RBAC) با معماری **Clean Architecture** روی **.NET 10**، با پشتیبانی کامل از **multi-tenancy** به روش **Database-per-tenant**.

## فناوری‌ها

| بخش | فناوری |
|------|--------|
| Runtime | .NET 10 |
| دیتابیس | SQL Server |
| ORM | Entity Framework Core 10 |
| احراز هویت | ASP.NET Core Identity + JWT |
| CQRS | MediatR |
| اعتبارسنجی | FluentValidation |
| نگاشت | AutoMapper |
| لاگ | Serilog |
| مستندسازی | Swagger / OpenAPI |

## ساختار پروژه (Clean Architecture)

```
backend/
├── src/
│   ├── RbacApp.Domain/            # قوانین کسب‌وکار (بدون وابستگی خارجی)
│   │   ├── Entities/              # Tenant، ApplicationUser/Role، RefreshToken
│   │   ├── Enums/                 # Permissions، TenantStatus
│   │   ├── Common/                # BaseEntity، Result
│   │   └── Exceptions/            # AppException و مشتقاتش
│   │
│   ├── RbacApp.Application/       # Use case‌ها و قراردادها
│   │   ├── Common/                # Interfaces، Behaviors، Mappings، Models
│   │   ├── Dtos/                  # Auth، User، Role، Tenant DTOs
│   │   └── Features/              # Auth، Users، Roles، Tenants (CQRS)
│   │
│   ├── RbacApp.Infrastructure/    # پیاده‌سازی فنی
│   │   ├── Persistence/
│   │   │   ├── Catalog/           # CatalogDbContext (رژیستری tenantها)
│   │   │   │   └── Migrations/
│   │   │   └── Tenant/            # TenantDbContext (داده‌های هر tenant)
│   │   │       └── Migrations/
│   │   ├── Identity/              # IdentityService، JwtTokenService، TenantIdentityContext
│   │   ├── MultiTenancy/          # TenantContext، TenantResolverMiddleware، CurrentUser، Provisioner
│   │   └── Services/              # TenantSeeder
│   │
│   └── RbacApp.WebApi/            # لایه ارائه
│       ├── Controllers/           # Auth، Users، Roles، Tenants
│       ├── Middleware/            # ExceptionHandler، RequirePermission/Role، Swagger filter
│       ├── Extensions/            # AuthorizationExtensions
│       └── Program.cs
│
└── tests/
    └── RbacApp.UnitTests/
```

## معماری Multi-Tenancy (Database-per-tenant)

```
┌─────────────────────────────────────────────┐
│           Catalog DB (ثابت، مرکزی)            │
│  Tenants: Id | Name | Slug | ConnectionName   │
│           | Status | AdminEmail | CreatedAt   │
└─────────────────────────────────────────────┘
                    │ (lookup + cache در هر request)
                    ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Tenant A DB  │  │ Tenant B DB  │  │ Tenant C DB  │
│ AspNetUsers  │  │ AspNetUsers  │  │ AspNetUsers  │
│ AspNetRoles  │  │ AspNetRoles  │  │ AspNetRoles  │
│ RolePerms    │  │ RolePerms    │  │ RolePerms    │
│ RefreshTok.  │  │ RefreshTok.  │  │ RefreshTok.  │
└──────────────┘  └──────────────┘  └──────────────┘
```

### جریان resolve یک tenant در هر request:

1. `TenantResolverMiddleware` با slug (از header `X-Tenant-Slug` یا subdomain) tenant را پیدا می‌کند.
2. `(slug → Tenant)` در `IMemoryCache` کش می‌شود (۱۰ دقیقه).
3. `TenantContext` (Scoped) برای request جاری پر می‌شود.
4. `TenantDbContextFactory` با connection string درست، DbContext می‌سازد.

### جریان provisioning یک tenant جدید:

1. super admin → `POST /api/admin/tenants` با slug و نام connection.
2. رکورد tenant در Catalog با وضعیت `Provisioning` ثبت می‌شود.
3. `TenantDbProvisioner` دیتابیس فیزیکی را می‌سازد و migration اعمال می‌کند.
4. `TenantSeeder` نقش‌های `Admin` (تمام دسترسی‌ها) و `Viewer` (فقط خواندن) را seed می‌کند.
5. اولین ادمین tenant ساخته و به نقش Admin متصل می‌شود.
6. وضعیت tenant روی `Active` تنظیم می‌شود.

## مدل RBAC

```
User ──(AspNetUserRoles)──► Role ──(RolePermissions)──► Permission
```

- **Permissions**: لیست ثابت سیستم در `Domain/Enums/Permissions.cs` (مثلا `users.read`، `roles.manage_permissions`).
- **Roles**: توسط ادمین هر tenant قابل تعریف؛ نقش‌های `Admin` و `Viewer` سیستمی هستند (IsSystem).
- **Authorization**: attribute‌های `[RequirePermission("users.create")]` و `[RequireRole("Admin")]`.
- **SuperAdmin**: نقش سراسری با دسترسی به مدیریت tenantها.

## پیش‌نیازها

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [SQL Server](https://www.microsoft.com/sql-server) (نسخه‌ی 2019+) یا SQL Server در Docker
- ابزار EF Core: `dotnet tool restore` (در پوشه‌ی backend)

## راه‌اندازی سریع

```bash
cd backend

# 1) Restore ابزارها و پکیج‌ها
dotnet tool restore
dotnet restore RbacApp.sln

# 2) Build
dotnet build RbacApp.sln

# 3) اعمال migration روی Catalog (ابتدا SQL Server را اجرا کنید و connection string را در appsettings.json تنظیم کنید)
dotnet ef database update \
  --project src/RbacApp.Infrastructure \
  --startup-project src/RbacApp.WebApi \
  --context CatalogDbContext

# 4) اجرای API
dotnet run --project src/RbacApp.WebApi
```

Swagger UI روی `https://localhost:5001/swagger` در دسترس است.

> **نکته:** فایل `src/RbacApp.WebApi/RbacApp.http` شامل نمونه‌ی آماده‌ی تمام endpointها برای تست با REST Client است.

## استفاده

### ساخت اولین tenant (دستی)

یک رکورد tenant در Catalog وارد کنید و دیتابیس tenant را provision کنید:

```sql
USE RbacApp_Catalog;
INSERT INTO Tenants (Id, Name, Slug, ConnectionName, AdminEmail, Status, CreatedAt)
VALUES (NEWID(), 'Acme Corp', 'acme', 'Tenant_Acme', 'admin@acme.com', 1, GETUTCDATE());
```

```bash
dotnet ef database update \
  --project src/RbacApp.Infrastructure \
  --startup-project src/RbacApp.WebApi \
  --context TenantDbContext \
  --connection "Server=localhost,1433;Database=RbacApp_Tenant_Acme;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

### ثبت اولین ادمین tenant

```http
POST /api/auth/register-first-admin
X-Tenant-Slug: acme

{
  "fullName": "ادمین اصلی",
  "email": "admin@acme.com",
  "password": "StrongPass1"
}
```

> فقط زمانی کار می‌کند که tenant هنوز کاربری نداشته باشد.

### ورود

```http
POST /api/auth/login
X-Tenant-Slug: acme

{
  "tenantSlug": "acme",
  "email": "admin@acme.com",
  "password": "StrongPass1"
}
```

در تمام درخواست‌های بعدی: `Authorization: Bearer <token>` و `X-Tenant-Slug: acme`.

## Endpointها

### Auth (بدون احراز هویت)
| متد | مسیر | توضیح |
|-----|------|-------|
| POST | `/api/auth/login` | ورود با tenant + email + password |
| POST | `/api/auth/refresh` | تمدید توکن |
| POST | `/api/auth/register-first-admin` | ثبت اولین ادمین tenant |
| POST | `/api/auth/logout` | خروج (نیازمند توکن) |

### Users
| متد | مسیر | دسترسی |
|-----|------|--------|
| GET | `/api/users` | `users.view` |
| GET | `/api/users/{id}` | `users.view` |
| POST | `/api/users` | `users.create` |
| PUT | `/api/users/{id}` | `users.update` |
| PUT | `/api/users/{id}/roles` | `users.manage_roles` |
| PATCH | `/api/users/{id}/toggle-active` | `users.update` |
| POST | `/api/users/{id}/reset-password` | `users.update` |
| DELETE | `/api/users/{id}` | `users.delete` |

### Roles
| متد | مسیر | دسترسی |
|-----|------|--------|
| GET | `/api/roles` | `roles.view` |
| GET | `/api/roles/{id}` | `roles.view` |
| GET | `/api/roles/permissions` | `roles.view` |
| POST | `/api/roles` | `roles.create` |
| PUT | `/api/roles/{id}` | `roles.update` |
| DELETE | `/api/roles/{id}` | `roles.delete` |

### Tenants (فقط `SuperAdmin`)
| متد | مسیر | توضیح |
|-----|------|-------|
| GET | `/api/admin/tenants` | لیست tenantها |
| GET | `/api/admin/tenants/{id}` | جزئیات tenant |
| POST | `/api/admin/tenants` | ساخت tenant جدید (provisioning خودکار) |
| PUT | `/api/admin/tenants/{id}` | ویرایش tenant |

## فهرست دسترسی‌ها (Permissions)

```
dashboard.view

users.view          users.create       users.update
users.delete        users.manage_roles

roles.view          roles.create       roles.update
roles.delete        roles.manage_permissions

tenants.view        tenants.create     tenants.update
tenants.delete
```

تعریف کامل در `src/RbacApp.Domain/Enums/Permissions.cs`.

## امنیت

- **رمز عبور**: حداقل ۸ کاراکتر با حروف بزرگ/کوچک و عدد.
- **Lockout**: ۵ تلاش ناموفق → قفل ۱۵ دقیقه‌ای.
- **JWT**: مدت اعمال ۱۵ دقیقه، قابل پیکربندی در `Jwt:AccessTokenMinutes`.
- **Refresh Token**: هش شده، با چرخش (rotation) و ابطال هنگام خروج.
- **User enumeration**: پیام خطای یکسان برای ایمیل/رمز اشتباه.
- **Soft isolation**: هر tenant یک دیتابیس فیزیکی جدا دارد.

## توسعه

### افزودن migration به Catalog

```bash
dotnet ef migrations add <Name> \
  --project src/RbacApp.Infrastructure \
  --startup-project src/RbacApp.WebApi \
  --context CatalogDbContext \
  --output-dir Persistence/Catalog/Migrations
```

### افزودن migration به Tenant template

```bash
dotnet ef migrations add <Name> \
  --project src/RbacApp.Infrastructure \
  --startup-project src/RbacApp.WebApi \
  --context TenantDbContext \
  --output-dir Persistence/Tenant/Migrations
```

## مراحل بعدی (فاز ۲)

- فرانت‌اند: Vite + React + TypeScript + Tailwind (RTL/LTR) + i18next
- پنل مدیریت users / roles / tenants
- Docker Compose برای کل استک
- تست‌های یکپارچه‌سازی و unit test‌های بیشتر
