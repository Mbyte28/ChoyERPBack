# ChoyERP — Backend

Backend interno del ERP de CHOY. Implementa la **Fase 1**, el módulo de
**Contrataciones**, el de **Almacén**, el de **Ventas e instalaciones** y el de
**Equipos instalados y su seguimiento** definidos en
[`ERP_CHOY_Especificacion_Backend.md`](ERP_CHOY_Especificacion_Backend.md):

- Estructura base de la solución y manejo consistente de errores.
- Autenticación con JWT y refresh tokens rotativos.
- Usuarios, roles y permisos.
- Trabajadores de CHOY.
- Clientes y sus direcciones.
- Solicitudes, cotizaciones, contratos y proyectos.
- Almacén: artículos, existencias, entradas, salidas, ajustes y kardex.
- Ventas de equipos con descuento de stock al confirmar.
- Instalaciones derivadas de una venta o de un proyecto.
- Equipos instalados con su ficha técnica e historial de estados operativos.
- Documentación OpenAPI / Swagger UI.
- Migraciones de base de datos y seed inicial.

> **Estado:** Fase 1, Contrataciones, Almacén, Ventas e instalaciones y Equipos
> instalados. Los módulos de ingeniería y mantenimiento se incorporarán en las
> siguientes fases sobre esta misma estructura.

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
    Entities/{Users,Workers,Clients,Contracting,Inventory,Sales,Installations,Equipment,Auditing}
  ChoyERP.Application/
    Common/{Exceptions,Extensions,Interfaces,Models,Security}
    Features/{Auth,Users,Roles,Workers,Clients,Contracting,Inventory,Sales,Installations,Equipment}
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
demostración (`Seed:DemoData`): tres trabajadores, dos clientes, cuatro
artículos de almacén y su stock inicial (uno de ellos bajo el mínimo).

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
| Almacén | `inventory.read`, `inventory.create`, `inventory.update`, `inventory.deactivate`, `inventory.move` |
| Ventas | `sales.read`, `sales.create`, `sales.update`, `sales.confirm`, `sales.deliver`, `sales.complete`, `sales.cancel` |
| Instalaciones | `installations.read`, `installations.create`, `installations.update`, `installations.complete`, `installations.cancel` |
| Equipos instalados | `equipment.read`, `equipment.create`, `equipment.update`, `equipment.change_status` |

`Administrador` recibe todos los permisos al sembrarse. `Comercial` recibe el
flujo completo de contrataciones y de ventas (registro, confirmación y cierre) y
registra los equipos instalados; `Ingeniería` consulta solicitudes, cotizaciones,
contratos, proyectos, almacén, ventas e instalaciones, actualiza proyectos y
equipos, y cambia el estado operativo de los equipos; `Almacenero` gestiona el
catálogo y los movimientos de inventario, registra la entrega de ventas y
consulta los equipos; `Técnico` consulta proyectos, existencias y ventas, y
edita, inicia y completa instalaciones, además de editar equipos y cambiar su
estado.

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

### Almacén

Catálogo de artículos y control de existencias. Los códigos se generan
automáticamente con los prefijos `ART-` (artículo) y `MOV-` (movimiento).

#### Artículos

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/inventory/items` | `inventory.read` |
| GET | `/api/inventory/items/{id}` | `inventory.read` |
| GET | `/api/inventory/items/{id}/movements` | `inventory.read` |
| POST | `/api/inventory/items` | `inventory.create` |
| PUT | `/api/inventory/items/{id}` | `inventory.update` |
| PATCH | `/api/inventory/items/{id}/status` | `inventory.deactivate` |

#### Movimientos

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/inventory/movements` | `inventory.read` |
| GET | `/api/inventory/movements/{id}` | `inventory.read` |
| POST | `/api/inventory/movements` | `inventory.move` |

Reglas del flujo:

- La existencia **solo** cambia con movimientos; editar el artículo no altera
  el stock. Un artículo nuevo empieza con existencia cero.
- Una **entrada** (`Entry`) suma cantidad y recalcula el **costo promedio
  ponderado**. Si no se envía `unitCost` se usa el costo promedio vigente.
- Una **salida** (`Exit`) valida la disponibilidad y descuenta al costo
  promedio; si no hay existencia suficiente devuelve `409` con el disponible.
  Puede asociarse a un proyecto (`projectId`), pero no a uno cancelado.
- Un **ajuste** (`Adjustment`) interpreta `quantity` como la **existencia
  contada resultante**, exige `reason` y guarda la diferencia. Un ajuste que no
  cambia la existencia devuelve `409`.
- `movementDate` no puede ser futura. `stockBefore`, `stockAfter` y
  `quantityDelta` quedan congelados en cada movimiento para construir el kardex.
- `totalCost` lleva signo: positivo en entradas y negativo en salidas o
  faltantes de un ajuste.
- Un artículo inactivo no admite movimientos y no puede desactivarse mientras
  tenga existencia. La unidad de medida tampoco cambia si ya hay movimientos
  registrados (devuelve `409`).

#### Catálogos de almacén

| Catálogo | Valores |
|---|---|
| `category` | `Spare`, `Component`, `Equipment`, `Supply`, `Tool`, `Other` |
| `unit` | `Unit`, `Kilogram`, `Liter`, `Meter`, `Box`, `Set`, `Other` |
| `type` (movimiento) | `Entry`, `Exit`, `Adjustment` |

### Ventas de equipos

Venta de artículos del catálogo de almacén a un cliente, opcionalmente
asociada a un proyecto de ese mismo cliente. Los códigos se generan
automáticamente con el prefijo `VEN-`.

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/sales` | `sales.read` |
| GET | `/api/sales/{id}` | `sales.read` |
| POST | `/api/sales` | `sales.create` |
| PUT | `/api/sales/{id}` | `sales.update` |
| POST | `/api/sales/{id}/confirm` | `sales.confirm` |
| POST | `/api/sales/{id}/deliver` | `sales.deliver` |
| POST | `/api/sales/{id}/complete` | `sales.complete` |
| POST | `/api/sales/{id}/cancel` | `sales.cancel` |

Reglas del flujo:

- Ciclo de vida: `Draft → Confirmed → Delivered → Completed`, y
  `Draft → Cancelled`.
- Crear o editar un borrador **no** modifica el stock. Al **confirmar** se
  valida la disponibilidad de cada artículo y se descuenta el inventario con
  movimientos `Exit` que guardan `saleId` y `reference = VEN-xxxxx`.
- Una venta confirmada es **inmutable**: no se puede editar ni anular. Para
  devolver equipos se registra una entrada de almacén.
- Los importes se calculan en el servidor: `subtotal` es la suma de las líneas,
  `taxAmount = subtotal × taxRate` y `total = subtotal + taxAmount`. La tasa de
  impuesto debe estar entre 0 y 1.
- Cada línea congela la descripción y la unidad del artículo en el momento de
  la venta, y no admite artículos repetidos.
- Se valida que el cliente esté activo, que el proyecto pertenezca al cliente y
  no esté cancelado, y que las fechas no sean futuras (`saleDate`) ni
  incoherentes (`expectedDeliveryDate ≥ saleDate`).
- La entrega solo aplica a ventas confirmadas; `deliveredDate` no puede ser
  futura ni anterior a `saleDate`.
- El cierre solo aplica a ventas entregadas y se bloquea mientras la
  instalación asociada siga `Scheduled` o `InProgress`.
- La anulación solo aplica a borradores y exige un motivo.

| Catálogo | Valores |
|---|---|
| Estado de venta | `Draft`, `Confirmed`, `Delivered`, `Completed`, `Cancelled` |

### Instalaciones

Instalación de los equipos vendidos o de un proyecto de ingeniería. Los
códigos se generan automáticamente con el prefijo `INS-`.

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/installations` | `installations.read` |
| GET | `/api/installations/{id}` | `installations.read` |
| POST | `/api/installations` | `installations.create` |
| PUT | `/api/installations/{id}` | `installations.update` |
| POST | `/api/installations/{id}/start` | `installations.complete` |
| POST | `/api/installations/{id}/complete` | `installations.complete` |
| POST | `/api/installations/{id}/cancel` | `installations.cancel` |

Reglas del flujo:

- Una instalación se asocia **exactamente a una venta o a un proyecto**, nunca a
  ambos. Solo puede existir una instalación por venta y una por proyecto.
- La venta de origen debe estar `Confirmed` o `Delivered`; el proyecto no puede
  estar cancelado. El cliente debe estar activo.
- Si no se envían `name` ni `location`, se heredan del origen (por ejemplo,
  `Instalación de venta VEN-00001` y la ubicación del proyecto o de la
  dirección del cliente). El nombre y la ubicación quedan congelados como texto.
- La dirección y el técnico son opcionales, pero la dirección debe pertenecer al
  cliente y el técnico debe ser un trabajador activo.
- Ciclo de vida: `Scheduled → InProgress → Completed`, y
  `Scheduled`/`InProgress → Cancelled`.
- Una instalación debe **iniciarse antes de completarse**; cerrar una
  instalación programada sin fecha de inicio devuelve `409`.
- Las fechas de inicio y fin no pueden ser futuras, y el fin no puede ser
  anterior al inicio.
- Completar la instalación de una venta ya **entregada** cierra la venta
  automáticamente (`Completed`) y lo registra en auditoría.
- Una instalación completada o anulada no se puede editar ni volver a anular.

| Catálogo | Valores |
|---|---|
| Estado de instalación | `Scheduled`, `InProgress`, `Completed`, `Cancelled` |

### Equipos instalados

Equipos entregados e instalados en el cliente, con su identidad física (serie,
marca y modelo) y el seguimiento de su estado operativo. Los códigos se generan
automáticamente con el prefijo `EQP-`.

| Método | Ruta | Permiso |
|---|---|---|
| GET | `/api/equipment` | `equipment.read` |
| GET | `/api/equipment/{id}` | `equipment.read` |
| GET | `/api/equipment/{id}/history` | `equipment.read` |
| POST | `/api/equipment` | `equipment.create` |
| PUT | `/api/equipment/{id}` | `equipment.update` |
| POST | `/api/equipment/{id}/status` | `equipment.change_status` |

Reglas del flujo:

- Un equipo solo se registra sobre una **instalación completada**. El cliente
  se hereda de la instalación y la fecha de instalación, si no se envía, se
  toma de la fecha de cierre de la instalación.
- El artículo debe estar activo y representa el modelo del equipo. Si no se
  envían `name`, `brand` o `model`, se heredan del artículo (`name` incluye el
  modelo cuando existe).
- El número de serie es opcional, se normaliza a mayúsculas y es **único por
  artículo**; el mismo número puede repetirse en artículos distintos.
- El equipo nace en estado `Operational` y el registro inicial queda en el
  historial con `fromStatus` nulo.
- Cada cambio de estado exige un motivo, no puede repetir el estado actual ni
  usar una fecha futura o anterior a la instalación.
- La ficha (nombre, serie, marca, modelo, ubicación, garantía y notas) se puede
  editar mientras el equipo no esté dado de baja. El fin de garantía no puede
  ser anterior a la fecha de instalación.
- `Retired` es un estado **terminal**: un equipo dado de baja no admite más
  cambios de estado ni edición de su ficha.
- El historial devuelve los cambios del más reciente al más antiguo e incluye
  el estado anterior, el nuevo, la fecha y el motivo.

| Catálogo | Valores |
|---|---|
| Estado de equipo | `Operational`, `UnderMaintenance`, `OutOfService`, `Retired` |

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
- Artículos: `name` (por defecto), `code`, `category`, `quantityOnHand`, `minimumStock`, `createdAt`
- Movimientos: `movementDate` (por defecto), `code`, `type`, `quantity`, `createdAt`
- Ventas: `createdAt` (por defecto), `code`, `saleDate`, `status`, `total`, `clientName`
- Instalaciones: `scheduledDate` (por defecto), `code`, `status`, `clientName`, `createdAt`
- Equipos: `installedDate` (por defecto), `code`, `name`, `status`, `warrantyEndDate`, `clientName`, `createdAt`

Además, los listados aceptan filtros específicos:

| Recurso | Filtros |
|---|---|
| Solicitudes | `status`, `clientId`, `projectType`, `priority`, `responsibleWorkerId` |
| Cotizaciones | `status`, `projectRequestId`, `clientId` |
| Contratos | `status`, `clientId`, `quoteId` |
| Proyectos | `status`, `clientId`, `contractId`, `responsibleWorkerId` |
| Artículos | `category`, `unit`, `isActive`, `belowMinimum` |
| Movimientos | `itemId`, `type`, `projectId`, `fromDate`, `toDate` |
| Ventas | `status`, `clientId`, `projectId`, `fromDate`, `toDate` (por fecha de venta) |
| Instalaciones | `status`, `clientId`, `saleId`, `projectId`, `technicianWorkerId`, `fromDate`, `toDate` (por fecha programada) |
| Equipos | `status`, `clientId`, `installationId`, `inventoryItemId`, `underWarranty`, `fromDate`, `toDate` (por fecha de instalación) |

En equipos, `search` cubre código, nombre, número de serie, ubicación, cliente,
instalación y artículo; `underWarranty=true` devuelve solo equipos con garantía
vigente y `false` los vencidos o sin garantía.

---

## 7. Base de datos

Esquema en `snake_case` generado con migraciones de EF Core:

`users`, `roles`, `permissions`, `user_roles`, `role_permissions`,
`refresh_tokens`, `workers`, `clients`, `client_addresses`, `audit_logs`,
`project_requests`, `quotes`, `quote_items`, `contracts`, `projects`,
`inventory_items`, `inventory_movements`, `sales`, `sale_items`,
`installations`, `installed_equipment`, `equipment_status_changes`.

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
- Códigos de solicitud, cotización, contrato, proyecto, artículo y movimiento únicos.
- Códigos de venta (`VEN-`), instalación (`INS-`) y equipo (`EQP-`) únicos.
- Una sola cotización en borrador por solicitud y una sola aceptada.
- Un solo contrato por cotización y un solo proyecto por contrato.
- Una sola instalación por venta y una sola por proyecto; el *check constraint*
  `ck_installations_origin` exige exactamente un origen (venta **o** proyecto).
- Un número de serie único por artículo (`inventory_item_id`, `serial_number`);
  el mismo serial puede existir en artículos distintos.
- La garantía de un equipo no vence antes de su instalación
  (`ck_installed_equipment_warranty`) y el historial no admite transiciones al
  mismo estado (`ck_equipment_status_changes_transition`).
- Importes y cantidades validados en API y protegidos con *check constraints*
  en PostgreSQL (`quantity > 0`, avance entre 0 y 100, tasa de impuesto entre
  0 y 1, importes de venta no negativos).
- Existencia, stock mínimo y costo promedio no negativos; cada movimiento es
  coherente con su kardex (`stock_before`/`stock_after`).
- Borrado en cascada de sesiones, líneas de cotización, líneas de venta,
  movimientos de almacén, historial de estados de equipos y relaciones
  rol/permiso. El proyecto o la venta asociados a un movimiento se conservan con
  `ON DELETE SET NULL`; el cliente, la instalación y el artículo de un equipo se
  conservan con `ON DELETE RESTRICT`.
- Desactivar un trabajador con cuenta activa devuelve `409`.
- Desactivar un artículo con existencia devuelve `409`.

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
- `inventory.items.created`, `inventory.items.updated`,
  `inventory.items.activated`, `inventory.items.deactivated`
- `inventory.movements.created` (incluye tipo, artículo y existencia resultante)
- `sales.created`, `sales.updated`, `sales.confirmed`, `sales.delivered`,
  `sales.completed`, `sales.cancelled`
- `installations.created`, `installations.updated`, `installations.started`,
  `installations.completed`, `installations.cancelled`
- `equipment.created`, `equipment.updated`, `equipment.status_changed`

Cada registro incluye usuario, correo, IP, user agent, entidad afectada y
detalle. Los intentos fallidos de login también se registran.

---

## 9. Pruebas

```bash
dotnet test
```

Cobertura actual (71 pruebas):

- Política de contraseñas, BCrypt y hash de tokens.
- Login correcto/incorrecto/cuenta desactivada.
- Rotación de refresh token y detección de reutilización.
- Reglas de usuarios: correo duplicado, autodesactivación, revocación de sesiones.
- Reglas de clientes: código automático, RUC único, direcciones primarias.
- Contrataciones: flujo completo solicitud → cotización → contrato → proyecto,
  versionado de cotizaciones, aceptación y rechazo de competidoras, vencimiento,
  inmutabilidad de cotizaciones enviadas, unicidad de contrato y proyecto,
  cancelación en cascada y validación de dirección del cliente.
- Almacén: código automático de artículos, costo promedio ponderado en
  entradas, validación de disponibilidad en salidas, ajustes por conteo
  físico, bloqueo de desactivación con existencia, movimientos sobre
  artículos inactivos o proyectos cancelados, fechas futuras, cambio de unidad
  con movimientos, filtro de stock mínimo y validaciones de entrada.
- Ventas: código automático, cálculo de importes, stock intacto en borrador,
  descuento y movimientos de salida al confirmar, rechazo por stock
  insuficiente, inmutabilidad tras confirmar, ciclo de entrega y cierre,
  bloqueo del cierre con instalación pendiente, anulación solo en borrador y
  filtros por estado, cliente y fechas.
- Instalaciones: origen exclusivo venta/proyecto, unicidad por venta y
  proyecto, herencia de nombre y ubicación, asignación de técnicos activos,
  flujo programada → en curso → completada, cierre automático de la venta
  entregada y bloqueo de edición o anulación en estados finales.
- Equipos instalados: registro solo sobre instalaciones completadas, herencia
  de cliente, fecha, nombre, marca y modelo, normalización y unicidad de serie
  por artículo, fechas de instalación y garantía, edición de la ficha,
  historial inicial y de cambios de estado, bloqueo del estado repetido y del
  estado terminal `Retired`, filtros por estado, cliente, instalación, artículo,
  garantía y fechas, y validaciones de los DTOs.

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
5. **Desactivación en lugar de borrado.** Usuarios, trabajadores, clientes y
   artículos se desactivan para conservar el historial de proyectos, equipos y
   movimientos.
6. **`snake_case` en PostgreSQL.** Esquema consistente con las convenciones
   habituales de PostgreSQL, aplicado por `EFCore.NamingConventions`.
7. **Auditoría en base de datos.** Requisito de seguridad de la
   especificación, especialmente para inventario y órdenes de trabajo.
8. **Validación con FluentValidation.** Reglas expresivas y reutilizables,
   ejecutadas por un filtro global antes de entrar al caso de uso.
9. **Kardex inmutable y costo promedio ponderado.** Cada movimiento guarda la
   existencia anterior y resultante y el costo aplicado, de modo que el
   historial no dependa del estado actual del artículo. El costo promedio se
   recalcula en cada entrada.
10. **Doble protección de la existencia.** El servicio valida disponibilidad
    antes de guardar y PostgreSQL impide existencias negativas con un *check
    constraint*, incluso ante errores de concurrencia.
11. **El stock se descuenta al confirmar la venta.** El borrador es un
    documento comercial sin efectos en almacén; confirmar es el punto de no
    retorno que descuenta el inventario y genera el kardex asociado (`saleId`).
12. **Venta confirmada inmutable.** Editar o anular una venta ya confirmada
    rompería la trazabilidad del stock y de la instalación. Las devoluciones se
    registran como entradas de almacén.
13. **Instalación con origen exclusivo.** Una instalación nace de una venta o
    de un proyecto, nunca de ambos, y se refuerza con validación en el servicio
    y un *check constraint*. Completarla cierra la venta entregada para no
    dejar ventas abiertas sin trabajo pendiente.
14. **Equipo instalado como activo trazable.** El equipo guarda una copia de su
    identidad física (nombre, serie, marca, modelo y ubicación) en lugar de
    depender del artículo de almacén, que puede cambiar de precio o
    descripción. El historial de estados es inmutable y `Retired` es terminal
    para conservar la trazabilidad de la baja.

### Simplificaciones conocidas

- El código de cliente se autogenera con el patrón `CLI-00001`. En alta
  concurrencia el índice único protege la integridad, pero puede devolver
  `409`; el cliente puede reintentar. Lo mismo aplica a `SOL-`, `COT-`, `CTR-`,
  `PRY-`, `ART-`, `MOV-`, `VEN-`, `INS-` y `EQP-`.
- Los roles del sistema no son editables desde la API; se actualizan por
  seed cuando cambia el catálogo de permisos.
- El catálogo comercial reutiliza los artículos de almacén: no hay un catálogo
  de venta separado. Cada línea congela descripción, unidad y precio, de modo
  que editar el artículo no altera ventas pasadas.
- Una venta no se elimina: la anulación solo aplica a borradores y, como el
  stock aún no se descontó, no requiere revertir inventario.
- El inventario usa costo promedio ponderado; no hay valorización por lote
  ni FIFO.
- El equipo se registra manualmente sobre una instalación completada; el stock
  ya se descontó al confirmar la venta, por lo que el registro no genera
  movimientos adicionales.
- Un equipo no se elimina: se conserva con su historial y se marca como
  `Retired` para reflejar la baja.
- Las salidas concurrentes del mismo artículo pueden competir. El *check
  constraint* impide existencias negativas y la API devuelve `409`, pero aún
  no hay bloqueo optimista explícito (por ejemplo, `xmin`).
- No hay endpoint de consulta de auditoría todavía (los datos ya se
  registran).

---

## 11. Siguientes fases

1. Ingeniería y documentación técnica con archivos adjuntos.
2. Visitas e inspecciones técnicas de los equipos instalados.
3. Mantenimiento preventivo y correctivo con órdenes de trabajo.
4. Endpoint de auditoría, pruebas de integración con Testcontainers y CI/CD.
