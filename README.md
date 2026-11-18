# ChoyERP — Backend

Backend interno del ERP de CHOY. Implementa la **Fase 1** y el módulo de
**Contrataciones** definidos en
[`ERP_CHOY_Especificacion_Backend.md`](ERP_CHOY_Especificacion_Backend.md):

- Estructura base de la solución y manejo consistente de errores.
- Autenticación con JWT y refresh tokens rotativos.
- Usuarios, roles y permisos.
- Trabajadores de CHOY.
- Clientes y sus direcciones.
- Solicitudes, cotizaciones, contratos y proyectos.
- Documentación OpenAPI / Swagger UI.
- Migraciones de base de datos y seed inicial.

> **Estado:** Fase 1 y módulo de Contrataciones. Los módulos de ingeniería,
> ventas, equipos, mantenimiento y almacén se incorporarán en las siguientes
> fases sobre esta misma estructura.

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
    Entities/{Users,Workers,Clients,Contracting,Auditing}
  ChoyERP.Application/
    Common/{Exceptions,Extensions,Interfaces,Models,Security}
    Features/{Auth,Users,Roles,Workers,Clients,Contracting}
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
| Solicitudes | `requests.read`, `requests.create`, `requests.update`, `requests.cancel` |
| Cotizaciones | `quotes.read`, `quotes.create`, `quotes.update`, `quotes.send`, `quotes.decide` |
| Contratos | `contracts.read`, `contracts.create`, `contracts.update` |
| Proyectos | `projects.read`, `projects.create`, `projects.update` |

`Administrador` recibe todos los permisos al sembrarse. `Comercial` recibe el
flujo completo de contrataciones; `Ingeniería` consulta solicitudes,
cotizaciones y contratos, y actualiza proyectos; `Técnico` consulta proyectos.

> Al incorporar permisos nuevos, vuelva a ejecutar el seed (`Seed:Enabled=true`)
> una vez para registrarlos y asignarlos a los roles del sistema.

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

### Contrataciones

Flujo completo: **solicitud → cotización → contrato → proyecto**. Los códigos
se generan automáticamente con los prefijos `SOL-`, `COT-`, `CTR-` y `PRY-`.

#### Solicitudes de proyecto

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/project-requests` | `requests.read` |
| GET | `/api/project-requests/{id}` | `requests.read` |
| POST | `/api/project-requests` | `requests.create` |
| PUT | `/api/project-requests/{id}` | `requests.update` |
| PATCH | `/api/project-requests/{id}/status` | `requests.cancel` |

#### Cotizaciones

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/quotes` | `quotes.read` |
| GET | `/api/quotes/{id}` | `quotes.read` |
| POST | `/api/quotes` | `quotes.create` |
| PUT | `/api/quotes/{id}` | `quotes.update` |
| POST | `/api/quotes/{id}/send` | `quotes.send` |
| POST | `/api/quotes/{id}/accept` | `quotes.decide` |
| POST | `/api/quotes/{id}/reject` | `quotes.decide` |
| POST | `/api/quotes/{id}/cancel` | `quotes.update` |

Reglas del flujo:

- Solo puede existir **una cotización en borrador** por solicitud. Al enviarla
  se libera la creación de una nueva versión (`COT-00002`, `COT-00003`…).
- Las cotizaciones enviadas son **inmutables**: para cambiar importes se crea
  una versión nueva.
- Aceptar una cotización **rechaza automáticamente** las demás y marca la
  solicitud como `Accepted`.
- Una cotización vencida (`validUntil`) no se puede aceptar: pasa a `Expired`.
- Los estados `Quoted` y `Accepted` de la solicitud los asigna el flujo de
  cotizaciones; no se pueden fijar manualmente.

#### Contratos

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/contracts` | `contracts.read` |
| GET | `/api/contracts/{id}` | `contracts.read` |
| POST | `/api/contracts` | `contracts.create` |
| PUT | `/api/contracts/{id}` | `contracts.update` |
| PATCH | `/api/contracts/{id}/status` | `contracts.update` |

- Se crea un contrato por cotización **aceptada**; el importe y la moneda se
  copian de la cotización.
- Transiciones: `Draft → Active → Completed`, y `Draft`/`Active → Cancelled`.
- Al anular un contrato se cancela el proyecto asociado si no está finalizado, la
  cotización aceptada pasa a `Cancelled` y la solicitud vuelve a `InEvaluation`
  para permitir una nueva cotización.
- No se puede completar un contrato mientras su proyecto no esté completado, ni
  anularlo si el proyecto ya está completado.

#### Proyectos

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/projects` | `projects.read` |
| GET | `/api/projects/{id}` | `projects.read` |
| POST | `/api/projects` | `projects.create` |
| PUT | `/api/projects/{id}` | `projects.update` |
| PATCH | `/api/projects/{id}/progress` | `projects.update` |
| PATCH | `/api/projects/{id}/status` | `projects.update` |

- Se crea un proyecto por contrato **vigente** (`Active`).
- El nombre y la ubicación se heredan de la solicitud; la ubicación queda
  congelada como texto para conservar el historial.
- Transiciones: `Planned → InProgress → Paused/Completed`, y
  `Planned`/`InProgress`/`Paused → Cancelled`.
- Al registrar un avance mayor que cero el proyecto pasa a `InProgress`; al
  llegar a **100 %** se completa, se registra la fecha real de fin y el
  contrato vigente se cierra automáticamente.

#### Catálogos y estados

Los enums se serializan como texto (`JsonStringEnumConverter`):

| Catálogo | Valores |
|---|---|
| `projectType` | `ColdRoom`, `FreezingTunnel`, `CoolingTunnel`, `Other` |
| `priority` | `Low`, `Normal`, `High`, `Urgent` |
| Estado de solicitud | `Received`, `InEvaluation`, `Quoted`, `Accepted`, `Rejected`, `Cancelled` |
| Estado de cotización | `Draft`, `Sent`, `Accepted`, `Rejected`, `Expired`, `Cancelled` |
| Estado de contrato | `Draft`, `Active`, `Completed`, `Cancelled` |
| Estado de proyecto | `Planned`, `InProgress`, `Paused`, `Completed`, `Cancelled` |

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
- Solicitudes: `requestedAt` (por defecto), `code`, `title`, `status`, `priority`, `createdAt`
- Cotizaciones: `createdAt` (por defecto), `code`, `version`, `status`, `total`, `validUntil`
- Contratos: `createdAt` (por defecto), `code`, `status`, `amount`, `startDate`
- Proyectos: `createdAt` (por defecto), `code`, `name`, `status`, `progress`, `startDate`

Además, los listados de contrataciones aceptan filtros específicos:

| Recurso | Filtros |
|---|---|
| Solicitudes | `status`, `clientId`, `projectType`, `priority`, `responsibleWorkerId` |
| Cotizaciones | `status`, `projectRequestId`, `clientId` |
| Contratos | `status`, `clientId`, `quoteId` |
| Proyectos | `status`, `clientId`, `contractId`, `responsibleWorkerId` |

---

## 7. Base de datos

Esquema en `snake_case` generado con migraciones de EF Core:

`users`, `roles`, `permissions`, `user_roles`, `role_permissions`,
`refresh_tokens`, `workers`, `clients`, `client_addresses`, `audit_logs`,
`project_requests`, `quotes`, `quote_items`, `contracts`, `projects`.

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
- Códigos de solicitud, cotización, contrato y proyecto únicos.
- Una sola cotización en borrador por solicitud y una sola aceptada.
- Un solo contrato por cotización y un solo proyecto por contrato.
- Importes y cantidades validados en API y protegidos con *check constraints*
  en PostgreSQL (`quantity > 0`, avance entre 0 y 100).
- Borrado en cascada de sesiones, líneas de cotización y relaciones
  rol/permiso.
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
- `requests.created`, `requests.updated`, `requests.status_changed`
- `quotes.created`, `quotes.updated`, `quotes.sent`, `quotes.accepted`,
  `quotes.rejected`, `quotes.cancelled`, `quotes.expired`
- `contracts.created`, `contracts.updated`, `contracts.status_changed`,
  `contracts.completed`
- `projects.created`, `projects.updated`, `projects.progress_updated`,
  `projects.status_changed`, `projects.completed`, `projects.cancelled`

Cada registro incluye usuario, correo, IP, user agent, entidad afectada y
detalle. Los intentos fallidos de login también se registran.

---

## 9. Pruebas

```bash
dotnet test
```

Cobertura actual (33 pruebas):

- Política de contraseñas, BCrypt y hash de tokens.
- Login correcto/incorrecto/cuenta desactivada.
- Rotación de refresh token y detección de reutilización.
- Reglas de usuarios: correo duplicado, autodesactivación, revocación de sesiones.
- Reglas de clientes: código automático, RUC único, direcciones primarias.
- Contrataciones: flujo completo solicitud → cotización → contrato → proyecto,
  versionado de cotizaciones, aceptación y rechazo de competidoras, vencimiento,
  inmutabilidad de cotizaciones enviadas, unicidad de contrato y proyecto,
  cancelación en cascada y validación de dirección del cliente.

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

### Simplificaciones conocidas

- El código de cliente se autogenera con el patrón `CLI-00001`. En alta
  concurrencia el índice único protege la integridad, pero puede devolver
  `409`; el cliente puede reintentar. Lo mismo aplica a `SOL-`, `COT-`, `CTR-`
  y `PRY-`.
- Los roles del sistema no son editables desde la API; se actualizan por
  seed cuando cambia el catálogo de permisos.
- El catálogo comercial y el de almacén se definirán en la fase de ventas.
- No hay endpoint de consulta de auditoría todavía (los datos ya se
  registran).

---

## 11. Siguientes fases

1. Ingeniería y documentación técnica con archivos adjuntos.
2. Almacén: catálogo de artículos, movimientos de inventario y existencias.
3. Ventas de equipos e instalaciones asociadas a clientes y proyectos.
4. Seguimiento de equipos, visitas e inspecciones.
5. Mantenimiento preventivo y correctivo con órdenes de trabajo.
6. Endpoint de auditoría, pruebas de integración con Testcontainers y CI/CD.
