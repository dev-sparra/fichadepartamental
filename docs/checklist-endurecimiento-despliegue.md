# Checklist de endurecimiento y despliegue — Módulo `/governance`

> Fase 8. Complementa el diseño en [diseno-modulo-gobernanza.md](diseno-modulo-gobernanza.md).
> Objetivo: llevar el módulo a producción (Plesk / IIS / MySQL, subdominio de `musigrafia.org`) de forma segura.

## 1. Round-trip Excel↔Web — verificado ✅

`ExcelRoundTripTests` prueba de extremo a extremo que:
- La librería de importación (**ClosedXML**) puede abrir el `.xlsm` oficial **con su modelo Power Query, macros y validaciones** (requisito para que la importación funcione).
- Un libro **exportado** se relee con los **mismos valores en las mismas celdas** que espera la importación, incluida la **selección múltiple con separador `", "`**, las **fechas reales** y los acentos.

Esto cierra la garantía central: exportar → volver a importar no pierde datos.

## 2. Acciones OBLIGATORIAS antes de producción

### 2.1 Base de datos (los ejecuta el DBA / usuario, no la app)
Aplicar en orden sobre MySQL:
- Esquema: `database/schema/001` … `008_audit_ip_address.sql` (el **008 es nuevo** de la Fase 7: columna `ip_address`).
- Semillas: `database/seed/001_master_catalogs.sql`, `002_security_seed.sql`, `003_admin_password.sql`.
- **Cambiar la contraseña del admin** sembrada en `003_admin_password.sql`.
- Usar un **usuario MySQL de mínimo privilegio** (`app_user` con contraseña fuerte), **no `root`**. El `appsettings.json` trae `user=root;password=` solo para desarrollo.

### 2.2 Secretos y configuración (vía variables de entorno, ver `deploy/env/.env.example`)
- **`Jwt__SecretKey`**: reemplazar el placeholder de `appsettings.json` (`PortalGobernanzaMusical_ClaveTemporal_2026_Segura`) por un secreto **aleatorio ≥ 32 bytes** en env. **Nunca** desplegar con el valor de `appsettings.json`.
- **`ConnectionStrings__DefaultConnection`**: apuntar a la BD de producción con el usuario de mínimo privilegio.
- **`Cors:AllowedOrigins`**: fijar el origen real (subdominio de `musigrafia.org`), no `http://localhost:4200`.
- **`AllowedHosts`**: cambiar `"*"` por el/los host(s) reales.
- **`ASPNETCORE_ENVIRONMENT=Production`** (desactiva Swagger y detalles de error).

### 2.3 Plantilla oficial
La plantilla `.xlsm` va **embebida en el ensamblado** (recurso de `Infrastructure`), así que viaja con el DLL — no requiere archivo externo en el servidor. Si se actualiza el Excel oficial, recompilar (y correr las pruebas de paridad).

## 3. Postura de seguridad actual (ya implementado)

| Control | Estado |
|---|---|
| Autenticación JWT con validación de emisor/audiencia/vigencia/firma | ✅ `Program.cs` |
| Hash de contraseñas (ASP.NET Identity `PasswordHasher`) | ✅ |
| Autorización por rol (RBAC) en rutas y en el formulario (Gestor/Líder/Admin) | ✅ Fase 6 |
| Auditoría de cambios (usuario, fecha, **IP**, operación, valor anterior/nuevo) | ✅ Fase 7 |
| Inyección SQL: EF Core parametriza; lookups por nombre vía LINQ | ✅ |
| Límite de tamaño de subida de import (50 MB) | ✅ `ImportsController` |
| CORS restringido por configuración (no `AllowAnyOrigin`) | ✅ |
| HTTPS redirection + `UseExceptionHandler` + ProblemDetails | ✅ |

## 4. Recomendaciones de endurecimiento (pendientes)

1. **Rate limiting** (OWASP: fuerza bruta en login / abuso de API). .NET 9 incluye `AddRateLimiter`; aplicar una ventana fija al endpoint `POST /api/auth/login` y un límite global por IP. *(No incluido aún: requiere validación en runtime.)*
2. **Cabeceras de seguridad**: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Content-Security-Policy` y **HSTS** — configurar en el `web.config` de IIS (frontend) y/o middleware del API.
3. **TLS**: terminar HTTPS en IIS/Plesk; `RequireHttpsMetadata=false` es solo para desarrollo — asegurar TLS real en el proxy.
4. **Refresh token**: `JwtSettings.RefreshTokenDays` existe pero el flujo de refresh no está implementado (tokens de 60 min). Implementarlo o ajustar la caducidad según UX.
5. **Serilog** a un sink persistente (archivo con rotación / base de datos), no solo consola, para operación y trazabilidad.
6. **Backups** de MySQL y del proyecto VBA/plantilla oficial.

## 5. Rendimiento (notas)

- Catálogos (departamentos, municipios ×1123, etc.) se consultan por petición; considerar **caché en memoria** (`IMemoryCache`) para los catálogos, que cambian rara vez.
- Los servicios de `governance` usan patrón *replace* (borrar+insertar) para colecciones; para fichas con muchas filas, evaluar *diff* en lugar de reemplazo (también reduciría el ruido de auditoría).
- Export/import son en memoria (`MemoryStream`); adecuado para el tamaño de una ficha (≤ 50 filas por hoja).
