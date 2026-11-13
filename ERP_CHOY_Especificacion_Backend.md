# ERP CHOY --- Especificación inicial del Backend

**Proyecto:** ERP interno de CHOY\
**Versión:** 1.0\
**Fecha:** 12 de noviembre de 2026\
**Estado:** Propuesta inicial para definición y desarrollo

## 1. Descripción general

El backend será responsable de implementar la lógica de negocio,
gestionar la información, validar permisos y exponer los servicios
utilizados por el frontend web. El ERP será de uso interno de CHOY. Los
usuarios serán trabajadores de la empresa; los clientes se registrarán
para relacionarlos con proyectos, instalaciones y equipos, pero no
tendrán acceso directo al sistema.

La primera versión no incluirá sensores físicos ni monitoreo IoT.

## 2. Tecnologías propuestas

  Componente             Tecnología
  ---------------------- ------------------------------------
  Lenguaje               C#
  Framework              ASP.NET Core Web API
  Base de datos          PostgreSQL
  ORM                    Entity Framework Core
  Documentación          OpenAPI / Swagger UI
  Autenticación          JWT o cookies seguras, por definir
  Pruebas                xUnit (propuesta)
  Control de versiones   Git

Las versiones concretas se elegirán al crear el proyecto.

## 3. Responsabilidades del backend

-   Exponer endpoints HTTP para el frontend.
-   Implementar reglas y validaciones del negocio.
-   Autenticar trabajadores y autorizar operaciones según roles y
    permisos.
-   Gestionar clientes, proyectos, instalaciones y equipos.
-   Controlar inventario y movimientos de almacén.
-   Registrar visitas, inspecciones, mantenimientos y reparaciones.
-   Administrar usuarios y permisos.
-   Validar datos, devolver respuestas consistentes y gestionar errores.
-   Proteger el acceso a la base de datos.

La seguridad se aplicará en el backend. Ocultar una pantalla o botón en
React no reemplaza la autorización del servidor.

## 4. Módulos y funcionalidades

### 4.1. Autenticación y autorización

-   Inicio y cierre de sesión.
-   Validación de credenciales.
-   Gestión de sesión y renovación de credenciales, según el mecanismo
    elegido.
-   Consulta del usuario autenticado.
-   Protección de endpoints según roles y permisos.
-   Desactivación de cuentas.
-   Almacenamiento de contraseñas mediante hash seguro, nunca en texto
    plano.

El backend determinará los permisos a partir de la identidad
autenticada; no confiará en roles enviados libremente por el frontend.

### 4.2. Usuarios y permisos

-   Crear, consultar, editar y desactivar usuarios.
-   Relacionar cuentas con trabajadores de CHOY.
-   Asignar roles y permisos.
-   Consultar los permisos efectivos de un usuario.
-   Restringir operaciones administrativas.

Roles iniciales propuestos:

  Rol             Acceso principal
  --------------- -------------------------------------------------------
  Administrador   Todos los módulos y gestión de usuarios.
  Comercial       Contrataciones y venta de equipos.
  Ingeniería      Sistemas de refrigeración y documentación técnica.
  Almacenero      Almacén y movimientos de inventario.
  Técnico         Seguimiento de equipos, inspecciones y mantenimiento.

Los roles y permisos específicos deberán ajustarse a las
responsabilidades reales de CHOY.

### 4.3. Clientes

-   Registrar, consultar, editar y desactivar clientes.
-   Guardar datos de contacto y datos generales.
-   Registrar direcciones y ubicaciones de instalaciones.
-   Consultar proyectos, ventas, instalaciones y equipos relacionados.
-   Los clientes no tendrán cuentas de acceso en esta primera versión.

### 4.4. Contrataciones

-   Registrar solicitudes de proyectos.
-   Crear y gestionar cotizaciones.
-   Registrar aceptación de cotizaciones.
-   Registrar contratos u órdenes de trabajo.
-   Gestionar proyectos de cámaras frigoríficas, túneles de
    congelamiento y túneles de enfriamiento.
-   Registrar fechas, ubicación, descripción, responsables y estado.
-   Consultar avance e información asociada al proyecto.

### 4.5. Sistemas de refrigeración

-   Registrar diseños y trabajos de ingeniería.
-   Asociar documentación técnica a proyectos e instalaciones.
-   Registrar especificaciones y componentes de cada sistema.
-   Adjuntar planos, documentos y archivos técnicos.
-   Consultar la documentación técnica disponible.

### 4.6. Venta de equipos

-   Registrar equipos o artículos disponibles para la venta.
-   Registrar solicitudes y cotizaciones.
-   Registrar ventas y sus datos principales.
-   Registrar la instalación de equipos vendidos.
-   Asociar ventas con clientes e instalaciones.
-   Consultar el estado de cada operación.

Se deberá definir si el catálogo comercial comparte el catálogo de
artículos de Almacén.

### 4.7. Seguimiento de equipos

**Instalaciones y equipos** - Registrar instalaciones asociadas a
clientes y ubicaciones. - Registrar equipos por instalación:
compresores, evaporadores, condensadores, sistemas de refrigeración y
otros. - Guardar identificación, características y datos técnicos. -
Consultar el historial de cada equipo.

**Visitas e inspecciones** - Registrar visitas realizadas por
trabajadores de CHOY. - Guardar fecha, responsable, motivo y
resultado. - Registrar observaciones y mediciones manuales cuando
corresponda. - Adjuntar fotografías y documentos.

**Mantenimiento preventivo** - Programar mantenimientos por equipo o
instalación. - Configurar frecuencias, por ejemplo mensual, trimestral,
semestral o anual. - Asignar responsables. - Consultar mantenimientos
próximos, pendientes, realizados y vencidos. - Registrar actividades
ejecutadas.

**Mantenimiento correctivo y reparaciones** - Registrar incidencias y
solicitudes de atención. - Identificar el equipo afectado y describir el
problema. - Registrar prioridad y estado. - Asignar trabajadores
responsables. - Registrar diagnóstico, solución y actividades
realizadas. - Registrar repuestos utilizados. - Conservar el historial
de intervenciones.

### 4.8. Almacén

-   Registrar y mantener el catálogo de artículos.
-   Clasificar repuestos, componentes y equipos.
-   Registrar entradas, salidas y ajustes.
-   Consultar existencias y disponibilidad.
-   Definir niveles mínimos de stock.
-   Consultar historial de movimientos.
-   Asociar salidas de materiales con proyectos u órdenes de
    mantenimiento.
-   Registrar recepción de artículos provenientes de compras cuando se
    implemente ese proceso.

Las operaciones de inventario deberán validar disponibilidad y evitar
movimientos inconsistentes.

## 5. Entidades principales del dominio

Esta lista es preliminar y no representa todavía el modelo definitivo de
base de datos.

  -----------------------------------------------------------------------
  Entidad                             Responsabilidad
  ----------------------------------- -----------------------------------
  Usuario                             Cuenta de acceso al ERP.

  Rol                                 Agrupación de permisos.

  Permiso                             Acción autorizada sobre un recurso.

  Trabajador                          Información del personal de CHOY.

  Cliente                             Datos de los clientes.

  Ubicación                           Dirección o lugar de una
                                      instalación.

  Solicitud                           Requerimiento inicial de un
                                      cliente.

  Cotización                          Propuesta comercial.

  Contrato                            Acuerdo u orden aceptada.

  Proyecto                            Trabajo contratado por CHOY.

  Diseño de ingeniería                Información y documentación
                                      técnica.

  Venta                               Operación de venta de equipos.

  Instalación                         Instalación asociada a un cliente y
                                      proyecto o venta.

  Equipo                              Activo registrado en una
                                      instalación.

  Visita técnica                      Visita realizada por un trabajador.

  Inspección                          Resultado de una revisión técnica.

  Plan de mantenimiento               Frecuencia y programación
                                      preventiva.

  Orden de trabajo                    Trabajo de mantenimiento o
                                      reparación.

  Artículo                            Producto, repuesto, componente o
                                      equipo de almacén.

  Movimiento de inventario            Entrada, salida o ajuste de
                                      existencias.

  Archivo adjunto                     Fotografía o documento relacionado
                                      con un registro.
  -----------------------------------------------------------------------

Las relaciones, cardinalidades, campos obligatorios, estados y
restricciones se definirán durante el diseño del dominio y la base de
datos.

## 6. Organización de la API

La API seguirá convenciones REST y utilizará JSON para solicitudes y
respuestas.

Rutas ilustrativas:

  Recurso              Método   Ruta de ejemplo
  -------------------- -------- ----------------------------
  Sesión               POST     `/api/auth/login`
  Usuario actual       GET      `/api/users/me`
  Usuarios             GET      `/api/users`
  Clientes             GET      `/api/clients`
  Crear cliente        POST     `/api/clients`
  Proyectos            GET      `/api/projects`
  Crear proyecto       POST     `/api/projects`
  Equipos              GET      `/api/equipment`
  Detalle de equipo    GET      `/api/equipment/{id}`
  Visitas              POST     `/api/equipment-visits`
  Órdenes de trabajo   POST     `/api/work-orders`
  Mantenimientos       GET      `/api/maintenance`
  Artículos            GET      `/api/inventory/items`
  Movimientos          POST     `/api/inventory/movements`

Las rutas son ejemplos y podrán cambiar al definir los casos de uso.
Cada endpoint deberá validar autenticación, autorización y datos de
entrada según corresponda.

## 7. DTOs y OpenAPI

Se utilizarán DTOs (Data Transfer Objects) para definir los datos que
entran y salen de la API.

Objetivos: - Evitar exponer directamente las entidades de base de
datos. - Mantener contratos claros entre frontend y backend. - Validar
solicitudes. - Controlar los campos devueltos. - Reducir el acoplamiento
entre el modelo interno y el frontend.

OpenAPI describirá endpoints, parámetros, esquemas, respuestas y códigos
HTTP. Swagger UI permitirá consultar y probar la documentación durante
el desarrollo.

Los DTOs pueden ayudar a mantener estable el contrato público aunque
cambie el modelo interno. Si cambia el contrato, se deberá actualizar la
documentación y coordinar el cambio con el frontend.

## 8. Seguridad

Requisitos iniciales: - Autenticación para operaciones privadas. -
Autorización en el servidor por roles y permisos. - Contraseñas
almacenadas con hash seguro. - Validación de datos en el backend. -
Protección frente al acceso a registros no autorizados. - HTTPS en
entornos desplegados. - Secretos y cadenas de conexión fuera del código
fuente. - Registro de operaciones relevantes, especialmente cambios de
inventario y órdenes de trabajo. - Manejo seguro de archivos adjuntos. -
CORS limitado a los orígenes autorizados.

La estrategia concreta de tokens o cookies, renovación de sesión,
almacenamiento de credenciales y protección CSRF se definirá antes de
implementar autenticación.

## 9. Arquitectura interna del backend

**Pendiente de definición.**

ASP.NET Core Web API será la tecnología del backend, pero la
arquitectura interna se elegirá y documentará por separado.

Una alternativa a evaluar es Clean Architecture, con una posible
separación entre: - **API:** endpoints, configuración y composición de
dependencias. - **Application:** casos de uso y reglas de aplicación. -
**Domain:** entidades y reglas centrales del negocio. -
**Infrastructure:** persistencia, archivos e integraciones externas.

No es obligatorio crear todas estas capas desde el primer momento. La
estructura definitiva deberá responder a la complejidad real del ERP.

## 10. Requisitos técnicos y de calidad

-   Validar entradas y devolver errores consistentes.
-   Utilizar códigos HTTP adecuados.
-   Implementar paginación, filtros y ordenamiento en listados extensos.
-   Controlar el rendimiento de las consultas.
-   Mantener integridad referencial en la base de datos.
-   Gestionar transacciones en operaciones que afecten varios registros,
    como movimientos de inventario.
-   Registrar errores y eventos relevantes sin exponer información
    sensible.
-   Crear pruebas unitarias y de integración para reglas y endpoints
    importantes.
-   Gestionar cambios de base de datos mediante migraciones.
-   Separar configuración de desarrollo, pruebas y producción.

## 11. Alcance de la primera versión

**Incluido** - Backend de uso interno de CHOY. - Autenticación,
usuarios, roles y permisos. - Clientes. - Contrataciones y proyectos. -
Ingeniería y documentación técnica. - Venta e instalación de equipos. -
Registro de instalaciones y equipos. - Visitas, inspecciones y
mantenimientos manuales. - Programación de mantenimiento preventivo. -
Reparaciones y órdenes de trabajo. - Control de almacén e historial de
movimientos. - Documentación OpenAPI.

**Fuera del alcance inicial** - Sensores físicos y monitoreo IoT. -
Portal de acceso para clientes. - Aplicación móvil; podrá desarrollarse
posteriormente consumiendo la misma API. - BFF; inicialmente el frontend
web consumirá directamente ASP.NET Core Web API. - Integraciones
contables o de facturación electrónica, salvo que se incorporen
posteriormente.

## 12. Etapas sugeridas de desarrollo

1.  Crear la solución ASP.NET Core Web API y configurar el entorno.
2.  Configurar PostgreSQL y Entity Framework Core.
3.  Definir arquitectura interna, convenciones y manejo de errores.
4.  Implementar autenticación, usuarios, roles y permisos.
5.  Implementar clientes y datos maestros.
6.  Implementar contrataciones, cotizaciones y proyectos.
7.  Implementar ingeniería y documentación.
8.  Implementar ventas, instalaciones y equipos.
9.  Implementar seguimiento, visitas y mantenimientos.
10. Implementar almacén y movimientos de inventario.
11. Completar documentación OpenAPI y pruebas.
12. Integrar y probar el frontend con la API.

El orden podrá ajustarse según las dependencias entre módulos y las
prioridades del proyecto.

------------------------------------------------------------------------

**Nota:** Este documento define el alcance funcional inicial del
backend. Antes de programar cada módulo se deberán detallar sus casos de
uso, reglas de negocio, permisos, DTOs, estados y relaciones de datos.
