# ChoyERP — Backend

Backend interno del ERP de CHOY. Implementa la **Fase 1** definida en
[`ERP_CHOY_Especificacion_Backend.md`](ERP_CHOY_Especificacion_Backend.md):

- Estructura base de la solución y manejo consistente de errores.
- Autenticación con JWT y refresh tokens rotativos.
- Usuarios, roles y permisos.
- Trabajadores de CHOY.
- Clientes y sus direcciones.
- Documentación OpenAPI / Swagger UI.
- Migraciones de base de datos y seed inicial.

> **Estado:** Fase 1. Los módulos de contrataciones, ingeniería, ventas,
> equipos, mantenimiento y almacén se incorporarán en las siguientes fases
> sobre esta misma estructura.

---

## 1. Stack

| Componente | Tecnología |
|---|---|
| Lenguaje | C# 14 / .NET 10 |
| Framework | ASP.NET Core Web API (controladores) |
| Base de datos | PostgreSQL 17 |
| ORM | Entity Framework Core 10 + Npgsql |
| Autenticación | JWT (access token) + refresh token opaco rotativo |
| Contraseñas | BCrypt (work factor 12) |
| Validación | FluentValidation |
| Documentación | OpenAPI + Swagger UI (Swashbuckle) |
| Pruebas | xUnit + EF Core InMemory |
| Contenedores | Docker / Docker Compose |

---

## 2. Arquitectura

Monolito modular organizado con **Clean Architecture**. No son
microservicios: es un único proceso desplegable, dividido en capas con
dependencias hacia adentro y módulos por funcionalidad. Esa separación
permite extraer un módulo a un microservicio más adelante sin reescribir
el dominio.

```
ChoyERP.Api ──────────────► ChoyERP.Infrastructure ──► ChoyERP.Application ──► ChoyERP.Domain
     │                              │                          │
     └──────────────► ChoyERP.Application ◄────────────────────┘
```

| Proyecto | Responsabilidad |
|---|---|
| `ChoyERP.Domain` | Entidades y reglas centrales. Sin dependencias de framework. |
| `ChoyERP.Application` | Casos de uso, DTOs, validaciones, contratos de persistencia y seguridad. |
| `ChoyERP.Infrastructure` | EF Core/PostgreSQL, BCrypt, JWT, auditoría y seed. |
| `ChoyERP.Api` | Endpoints, autenticación/autorización HTTP, middleware y composición. |
| `tests/ChoyERP.UnitTests` | Pruebas de reglas y servicios de aplicación. |

### Estructura

```
src/
  ChoyERP.Domain/
    Entities/{Users,Workers,Clients,Auditing}
  ChoyERP.Application/
    Common/{Exceptions,Extensions,Interfaces,Models,Security}
    Features/{Auth,Users,Roles,Workers,Clients}
  ChoyERP.Infrastructure/
    Persistence/{Configurations,Migrations,Seed}
    Identity/  Auditing/
  ChoyERP.Api/
    Authorization/  Controllers/  Filters/  Middleware/  Services/
tests/ChoyERP.UnitTests/
```

---

## 3. Puesta en marcha

### Requisitos

- .NET SDK 10
- Docker Desktop (para PostgreSQL) o una instancia de PostgreSQL 17

### Opción A — API local + PostgreSQL en Docker (recomendada para desarrollo)

```bash
# 1. Levantar solo PostgreSQL
docker compose up -d db

# 2. Restaurar herramientas y dependencias
dotnet tool restore
dotnet restore

# 3. Ejecutar la API (aplica migraciones y seed en Development)
dotnet run --project src/ChoyERP.Api
```

- Swagger UI: <https://localhost:7080/swagger>
- Health check: <https://localhost:7080/health>
- API HTTP: <http://localhost:5080>

### Opción B — Todo con Docker

```bash
docker compose up --build
```

- API: <http://localhost:5080/swagger>
- PostgreSQL: `localhost:5432`

### Credenciales iniciales (solo desarrollo)

| Campo | Valor |
|---|---|
| Correo | `admin@choy.local` |
| Contraseña | `Choy.Admin#2026` |

El seed es idempotente y se controla con la sección `Seed`. En producción
configure `Seed__AdminPassword` (o desactive el seed) y **cambie la
contraseña en el primer acceso**. En Development también se crean datos de
demostración (`Seed:DemoData`): tres trabajadores y dos clientes.

---

## 4. Configuración

| Clave | Descripción | Producción |
|---|---|---|
| `ConnectionStrings:Default` | Cadena de conexión a PostgreSQL | Obligatoria |
| `Jwt:Issuer` / `Jwt:Audience` | Emisor y audiencia del token | Obligatorias |
| `Jwt:SigningKey` | Clave HMAC de al menos 32 caracteres | Obligatoria (secreto) |
| `Jwt:AccessTokenMinutes` | Vida del access token (15 por defecto) | Configurable |
| `Jwt:RefreshTokenDays` | Vida del refresh token (7 por defecto) | Configurable |
| `Cors:AllowedOrigins` | Orígenes permitidos del frontend | Obligatorio |
| `Database:MigrateOnStartup` | Aplica migraciones al arrancar | Recomendado `false` en prod |
| `Seed:Enabled` | Ejecuta el seed idempotente | `false` tras el primer despliegue |
| `Seed:AdminEmail` / `Seed:AdminPassword` | Administrador inicial | Solo el primer arranque |
| `Seed:DemoData` | Datos de demostración | `false` |

En contenedores use variables de entorno con doble guion bajo, por ejemplo
`Jwt__SigningKey`. **Nunca** versione secretos reales: `appsettings.json`
deja las claves sensibles vacías a propósito.

---

## 5. Autenticación y autorización

### Flujo

1. `POST /api/auth/login` valida credenciales y devuelve:
   - `accessToken` JWT (15 min) con `sub`, `email`, `name`, `role` y `permission`.
   - `refreshToken` opaco (7 días). En la base solo se guarda su hash SHA-256.
2. El frontend envía `Authorization: Bearer <accessToken>`.
3. `POST /api/auth/refresh` **rota** el refresh token: revoca el anterior y
   emite uno nuevo. Si se reutiliza un token ya rotado, se revocan todas las
   sesiones del usuario (detección de robo).
4. `POST /api/auth/logout` revoca el refresh token indicado o todas las
   sesiones si se omite.
5. `POST /api/auth/change-password` cambia la contraseña propia y revoca
   todas las sesiones.
6. `GET /api/users/me` devuelve la cuenta con sus permisos efectivos.

### Reglas

- La autorización se evalúa **en el servidor** con permisos, no con roles
  enviados por el frontend.
- Los permisos efectivos son la unión de los permisos de todos los roles.
- Los roles del sistema (`Administrador`, `Comercial`, `Ingeniería`,
  `Almacenero`, `Técnico`) no se pueden renombrar ni eliminar.
- El login está limitado a 10 intentos por minuto por IP.
- Al desactivar una cuenta se revocan sus refresh tokens.
- No es posible desactivar la propia cuenta.

### Permisos iniciales

| Módulo | Permisos |
|---|---|
| Usuarios | `users.read`, `users.create`, `users.update`, `users.deactivate`, `users.reset_password` |
| Roles | `roles.read`, `roles.manage` |
| Trabajadores | `workers.read`, `workers.create`, `workers.update`, `workers.deactivate` |
| Clientes | `clients.read`, `clients.create`, `clients.update`, `clients.deactivate` |

`Administrador` recibe todos los permisos al sembrarse.

---

## 6. Endpoints

Todas las respuestas son JSON. Los errores usan `application/problem+json`
(`ProblemDetails`) con `traceId` y, en validaciones, un diccionario
`errors` por campo.

### Autenticación

| Método | Ruta | Acceso |
|---|---|---|
| POST | `/api/auth/login` | Anónimo (rate limited) |
| POST | `/api/auth/refresh` | Anónimo |
| POST | `/api/auth/logout` | Autenticado |
| POST | `/api/auth/change-password` | Autenticado |

### Usuarios

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/users/me` | Autenticado |
| GET | `/api/users` | `users.read` |
| GET | `/api/users/{id}` | `users.read` |
| POST | `/api/users` | `users.create` |
| PUT | `/api/users/{id}` | `users.update` |
| PATCH | `/api/users/{id}/status` | `users.deactivate` |
| POST | `/api/users/{id}/reset-password` | `users.reset_password` |

### Roles y permisos

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/roles` | `roles.read` |
| GET | `/api/roles/{id}` | `roles.read` |
| POST | `/api/roles` | `roles.manage` |
| PUT | `/api/roles/{id}` | `roles.manage` |
| DELETE | `/api/roles/{id}` | `roles.manage` |
| GET | `/api/permissions` | `roles.read` |

### Trabajadores

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/workers` | `workers.read` |
| GET | `/api/workers/{id}` | `workers.read` |
| POST | `/api/workers` | `workers.create` |
| PUT | `/api/workers/{id}` | `workers.update` |
| PATCH | `/api/workers/{id}/status` | `workers.deactivate` |

### Clientes

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/clients` | `clients.read` |
| GET | `/api/clients/{id}` | `clients.read` |
| POST | `/api/clients` | `clients.create` |
| PUT | `/api/clients/{id}` | `clients.update` |
| PATCH | `/api/clients/{id}/status` | `clients.deactivate` |
| POST | `/api/clients/{id}/addresses` | `clients.update` |
| PUT | `/api/clients/{id}/addresses/{addressId}` | `clients.update` |
| DELETE | `/api/clients/{id}/addresses/{addressId}` | `clients.update` |

### Listados

Los listados aceptan `page` (1..n), `pageSize` (máx. 100), `search`,
`isActive`, `sortBy` y `sortDesc`. La respuesta tiene la forma:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0
}
```

Ordenamientos admitidos:

- Usuarios: `email`, `createdAt`, `lastLoginAt`, `isActive`
- Trabajadores: `lastName` (por defecto), `documentNumber`, `createdAt`, `hireDate`
- Clientes: `name` (por defecto), `code`, `createdAt`, `isActive`

---

## 7. Base de datos

Esquema en `snake_case` generado con migraciones de EF Core:

`users`, `roles`, `permissions`, `user_roles`, `role_permissions`,
`refresh_tokens`, `workers`, `clients`, `client_addresses`, `audit_logs`.

### Comandos

```bash
# Aplicar migraciones
dotnet ef database update \
  --project src/ChoyERP.Infrastructure \
  --startup-project src/ChoyERP.Api

# Nueva migración
dotnet ef migrations add NombreMigracion \
  --project src/ChoyERP.Infrastructure \
  --startup-project src/ChoyERP.Api \
  --output-dir Persistence/Migrations

# Script SQL para despliegue controlado
dotnet ef migrations script --idempotent \
  --project src/ChoyERP.Infrastructure \
  --startup-project src/ChoyERP.Api \
  --output migrations.sql
```

La fábrica de diseño (`AppDbContextFactory`) permite ejecutar `dotnet ef`
sin levantar la API; usa `ConnectionStrings__Default` o la cadena local por
defecto.

El repositorio ya incluye `migrations.sql`, generado con `--idempotent`, para
que un DBA pueda revisar y aplicar el esquema en entornos controlados sin
ejecutar `dotnet ef`.

### Reglas de integridad

- Correo de usuario y nombre de rol únicos (normalizados).
- Documento de trabajador y código/RUC de cliente únicos.
- Una cuenta por trabajador (índice único en `users.worker_id`).
- Borrado en cascada de sesiones, direcciones y relaciones rol/permiso.
- Desactivar un trabajador con cuenta activa devuelve `409`.

---

## 8. Auditoría

Se registran en `audit_logs` los eventos relevantes sin datos sensibles:

- `auth.login.succeeded`, `auth.login.failed`, `auth.login.blocked`
- `auth.refresh.reuse_detected`, `auth.logout`, `auth.password_changed`
- `users.created`, `users.updated`, `users.activated`, `users.deactivated`, `users.password_reset`
- `roles.created`, `roles.updated`, `roles.deleted`
- `workers.created`, `workers.updated`, `workers.activated`, `workers.deactivated`
- `clients.created`, `clients.updated`, `clients.activated`, `clients.deactivated`
- `clients.address_added`, `clients.address_updated`, `clients.address_deleted`

Cada registro incluye usuario, correo, IP, user agent, entidad afectada y
detalle. Los intentos fallidos de login también se registran.

---

## 9. Pruebas

```bash
dotnet test
```

Cobertura actual (21 pruebas):

- Política de contraseñas, BCrypt y hash de tokens.
- Login correcto/incorrecto/cuenta desactivada.
- Rotación de refresh token y detección de reutilización.
- Reglas de usuarios: correo duplicado, autodesactivación, revocación de sesiones.
- Reglas de clientes: código automático, RUC único, direcciones primarias.

---

## 10. Decisiones de diseño

1. **Monolito modular con Clean Architecture.** Responde a la complejidad
   real del ERP sin el coste operativo de microservicios. La separación por
   `Features` permite extraer módulos en el futuro.
2. **JWT + refresh token rotativo.** El frontend React puede consumir la API
   desde otro origen. El refresh token es opaco, se guarda hasheado y se
   rota en cada uso; la reutilización revoca la familia completa.
3. **Autorización por permisos.** Escala mejor que comprobar roles: los
   roles agrupan permisos y el token lleva los permisos efectivos.
4. **DTOs y ProblemDetails.** El contrato HTTP no expone entidades y los
   errores son uniformes y documentados.
5. **Desactivación en lugar de borrado.** Usuarios, trabajadores y clientes
   se desactivan para conservar el historial futuro (proyectos, equipos,
   movimientos).
6. **`snake_case` en PostgreSQL.** Esquema consistente con las convenciones
   habituales de PostgreSQL, aplicado por `EFCore.NamingConventions`.
7. **Auditoría en base de datos.** Requisito de seguridad de la
   especificación, especialmente para inventario y órdenes de trabajo.
8. **Validación con FluentValidation.** Reglas expresivas y reutilizables,
   ejecutadas por un filtro global antes de entrar al caso de uso.

### Simplificaciones conocidas de la Fase 1

- El código de cliente se autogenera con el patrón `CLI-00001`. En alta
  concurrencia el índice único protege la integridad, pero puede devolver
  `409`; el cliente puede reintentar.
- Los roles del sistema no son editables desde la API; se actualizan por
  seed cuando cambia el catálogo de permisos.
- El catálogo comercial y el de almacén se definirán en la fase de ventas.
- No hay endpoint de consulta de auditoría todavía (los datos ya se
  registran).

---

## 11. Siguientes fases

1. Contrataciones: solicitudes, cotizaciones, contratos y proyectos.
2. Ingeniería y documentación técnica con archivos adjuntos.
3. Ventas, instalaciones y equipos.
4. Visitas, inspecciones y mantenimiento preventivo/correctivo.
5. Almacén, movimientos de inventario y transacciones.
6. Endpoint de auditoría, pruebas de integración con Testcontainers y CI/CD.
