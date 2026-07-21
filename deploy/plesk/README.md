# Despliegue en Plesk/IIS - Portal Gobernanza Musical

## Arquitectura: APP UNICA COMBINADA

El backend ASP.NET Core sirve **ambos**:
- API REST en `/api/*` y health checks en `/health*`
- Frontend Angular desde `wwwroot/` (archivos estaticos + SPA fallback a `index.html`)

```
httpdocs/
├── web.config                              # Configura AspNetCoreModuleV2
├── PortalNacionalGobernanzaMusical.API.dll
├── PortalNacionalGobernanzaMusical.API.exe
├── appsettings.json                        # Configuracion (Staging sobreescribe)
├── (resto de DLLs del backend)
└── wwwroot/                                # Build de Angular
    ├── index.html
    ├── main.*.js
    ├── styles.*.css
    └── ...
```

En **Development** (local) el backend no sirve archivos estaticos y el frontend corre aparte con `ng serve` en `http://localhost:4200`.

---

## Informacion del servidor

| Item | Valor |
|------|-------|
| Subdominio | fichadepartamental.musigrafia.org |
| FTP Usuario | sparra |
| FTP Contrasena | $Zg14tf9 |
| Base de datos (PROD) | rafiaorg_ficha_departamental |
| Usuario BD (PROD) | rafiaorg_admin |
| Contrasena BD (PROD) | 57W3*n6at |
| BD local (pruebas) | musigraf_ficha_departamental / musig_admin / 8Du1hd3_ (comentada) |

---

## PASO 1: Preparar el servidor Plesk (una sola vez)

### 1.1 Verificar ASP.NET Core Module

1. Entra al **Panel de Plesk**
2. Tools & Settings → Updates and Upgrades
3. Busca **"ASP.NET Core Module"** y verifica que este instalado
4. Si no esta, instalalo desde Add/Remove Components

### 1.2 Verificar .NET 9 Runtime

1. Conectate al servidor via RDP o Plesk SSH
2. Ejecuta: `dotnet --list-runtimes`
3. Deberia mostrar: `Microsoft.AspNetCore.App 9.0.x`
4. Si no esta, descarga e instala desde: https://dotnet.microsoft.com/download/dotnet/9.0

### 1.3 Crear el dominio en Plesk

1. Plesk → **Domains** → **Add Domain**
2. Dominio: `fichadepartamental.musigrafia.org`
3. Document Root: `httpdocs`
4. PHP: **None** (deshabilitar)
5. HTTPS: Habilitar (Let's Encrypt via Plesk)

### 1.4 Crear la base de datos MySQL

1. Plesk → **Databases** → **Add Database**
2. Nombre: `rafiaorg_ficha_departamental`
3. Usuario: `rafiaorg_admin` / Pass: `57W3*n6at`
4. Server: localhost
5. Ejecutar los scripts de `database/` para crear tablas y datos semilla.

---

## PASO 2: Despliegue automatico (recomendado)

Desde tu PC:

```powershell
cd C:\Mincultura\ficha-gobernanza
.\deploy\plesk\deploy.ps1 -FtpPass '$Zg14tf9'
```

El script automaticamente:
1. Compila el frontend con `ng build --configuration staging`
2. Compila el backend con `dotnet publish -c Release`
3. Copia el build de Angular dentro de `publish-backend/wwwroot/`
4. Copia `web.config.backend` y `appsettings.Staging.json` (renombrado a `appsettings.json`)
5. Sube todo el contenido de `publish-backend/` a `/fichadepartamental.musigrafia.org/httpdocs/` via FTP

> Nota: El script no borra archivos existentes en el servidor. En el primer despliegue, vacia `httpdocs/` desde el File Manager de Plesk (o por FTP) antes de ejecutarlo.

---

## PASO 3: Despliegue manual (si el script falla)

### 3.1 Compilar Frontend

```powershell
cd C:\Mincultura\ficha-gobernanza\src\frontend\portal-gobernanza-musical
ng build --configuration staging
```

### 3.2 Compilar Backend

```powershell
cd C:\Mincultura\ficha-gobernanza

if (Test-Path publish-backend) { Remove-Item -Recurse -Force publish-backend }

dotnet publish src\backend\PortalNacionalGobernanzaMusical.API\PortalNacionalGobernanzaMusical.API.csproj `
    -c Release -o publish-backend --self-contained false

Copy-Item deploy\plesk\web.config.backend publish-backend\web.config -Force
Copy-Item deploy\plesk\appsettings.Staging.json publish-backend\appsettings.json -Force

# Inyectar Angular dentro de wwwroot del backend
$wwwroot = "publish-backend\wwwroot"
if (Test-Path $wwwroot) { Remove-Item -Recurse -Force $wwwroot }
New-Item -ItemType Directory -Path $wwwroot | Out-Null
Copy-Item -Path "src\frontend\portal-gobernanza-musical\dist\portal-gobernanza-musical\browser\*" `
    -Destination $wwwroot -Recurse -Force
```

### 3.3 Subir por FTP (WinSCP / FileZilla)

| Host | `fichadepartamental.musigrafia.org` |
| User | `sparra` |
| Pass | `$Zg14tf9` |
| Puerto | `21` |

**Subir** todo el contenido de `C:\Mincultura\ficha-gobernanza\publish-backend\*`
**a** `/fichadepartamental.musigrafia.org/httpdocs/`

---

## PASO 4: Configurar en panel ASP.NET Core de Plesk

### 4.1 Configurar Aplicacion

1. Plesk → **fichadepartamental.musigrafia.org** → **ASP.NET Core** (pesta a existente; Plesk ya trae soporte)
2. **Archivo de inicio de la aplicacion**: `\fichadepartamental\httpdocs\PortalNacionalGobernanzaMusical.API.dll`
3. **Entorno (Environment)**: `Staging`
4. **Redireccionar stdout/stderr a un archivo**: marcar para debug inicial (lo puedes desactivar luego)
5. **Ruta al directorio de archivos de registro**: `\logs` (asegurate que la carpeta `httpdocs\logs` exista y tenga permisos de escritura)
6. Aplicar / OK

### 4.2 Configurar Variables de Entorno

En la misma pantalla, bot **"Variables de entorno → Editar..."**:

| Variable | Valor |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `ConnectionStrings__DefaultConnection` | `server=localhost;port=3306;database=rafiaorg_ficha_departamental;user=rafiaorg_admin;password=57W3*n6at` |
| `Jwt__SecretKey` | `G0b3rn4nz4M_s3cr3t_K3y_2026_s3gur0_pr0d_32b!` |
| `Jwt__Issuer` | `PortalNacionalGobernanzaMusical` |
| `Jwt__Audience` | `PortalNacionalGobernanzaMusical.Client` |
| `Cors__AllowedOrigins` | `http://fichadepartamental.musigrafia.org` (o incluir HTTPS si ya tienes cert) |

> El caracter `__` (doble guion bajo) en el nombre de la variable es interpretado por ASP.NET Core como separador jerarquico de config.

### 4.3 Reciclar el App Pool

Despues de cualquier cambio, recicla el app pool:
- Plesk → **fichadepartamental.musigrafia.org** → **ASP.NET Core** → **Reiniciar app** (o desde Plesk → ** Websites & Domains** → bot **Restart**)

---

## PASO 5: Verificar

### 5.1 Backend healthcheck

```
http://fichadepartamental.musigrafia.org/health
```
Respuesta esperada: `Healthy`

```
http://fichadepartamental.musigrafia.org/health/ready
```
Reporta detalle e incluye la prueba de conexion a la BD.

### 5.2 Frontend

```
http://fichadepartamental.musigrafia.org
```
Deberia cargar la pantalla de login. Si sale en blanco, inspecciona la consola del navegador (F12).

### 5.3 API

```
http://fichadepartamental.musigrafia.org/api/...
```
Prob a con un endpoint publico (puede responder 401 si requiere auth; eso confirma que el routing funciona).

### 5.4 Login de prueba

1. Ingresa con las credenciales de admin semilla.
2. Cambia la contrasena.
3. Navega por el sistema.

---

## Estructura final en el servidor

```
fichadepartamental.musigrafia.org/
└── httpdocs/                                # App unica combinada
    ├── web.config                          # AspNetCoreModuleV2 -> PortalNacionalGobernanzaMusical.API.exe
    ├── PortalNacionalGobernanzaMusical.API.dll
    ├── PortalNacionalGobernanzaMusical.API.exe
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── (mas DLLs del backend)
    ├── logs/                                # Carpeta de logs de stdout (con permisos de escritura)
    └── wwwroot/                            # Frontend Angular (servido por UseStaticFiles)
        ├── index.html
        ├── main.*.js
        ├── styles.*.css
        ├── polyfills.*.js
        ├── runtime.*.js
        └── assets/
```

---

## Solucion de problemas

### Error 500 - ASP.NET Core Module

1. Verifica que .NET 9 Runtime este instalado en el servidor.
2. Verifica que en el panel ASP.NET Core de Plesk, **Archivo de inicio** apunte a `.\PortalNacionalGobernanzaMusical.API.dll`.
3. Revisa los logs en `\httpdocs\logs\` (si activaste stdout) o en Plesk → **Logs** → **Error Log**.
4. Verifica que el app pool del dominio tenga permisos de lectura sobre `httpdocs\`.

### Frontend en blanco (404 en `main.*.js`)

1. Verifica que `httpdocs\wwwroot\` existe y contiene `index.html`, `main.*.js`, `styles.*.css`.
2. Verifica que el build de Angular se haya copiado dentro de `wwwroot/` (no en `httpdocs/` raiz).
3. Revisa `Program.cs`: `app.UseStaticFiles()` debe estar activo en Staging.

### Rutas del frontend dan 404 al recargar (ej. `/login`)

1. Confirma que `app.MapFallbackToFile("index.html")` esta en `Program.cs` para no-Development.
2. Recicla el app pool.
3. Verifica que el archivo `wwwroot\index.html` exista.

### CORS error en navegador

1. Si abriste el frontend en `https://`, `Cors:AllowedOrigins` debe listar `https://fichadepartamental.musigrafia.org`.
2. Como ahora la app combina frontend+backend en el mismo origen, CORS normalmente no deberia dispararse. Revisa que el frontend no este llamando a un dominio distinto.

### Base de datos no conecta

1. Verifica que MySQL este corriendo en el servidor.
2. Verifica que el usuario `rafiaorg_admin` tenga permisos sobre `rafiaorg_ficha_departamental`.
3. Prueba la conexion desde el servidor: `mysql -u rafiaorg_admin -p rafiaorg_ficha_departamental`.
4. Verifica que la variable `ConnectionStrings__DefaultConnection` este en el panel de Plesk (con `__` dobles, no `:`).

### La API responde pero la BD responde 503 en /health/ready

1. Revisa logs stdout en `\httpdocs\logs\`.
2. Comprueba que el esquema de la BD este aplicado (scripts en `database/`).
3. Comprueba permisos del usuario MySQL.

---

## Como alternar entre PROD y LOCAL

Para pruebas locales con la BD `musigraf_*`:
1. En `deploy\plesk\appsettings.Staging.json`: comentar la linea `DefaultConnection` de rafiaorg y descomentar la de musigraf.
2. En `deploy\env\.env.staging`: idem.
3. En `src\frontend\portal-gobernanza-musical\src\environments\environment.staging.ts`: si corres el frontend aparte con `ng serve`, cambiar `apiBaseUrl` a `http://localhost:5100/api`.

Recuerda revertir los cambios antes de desplegar a Plesk.