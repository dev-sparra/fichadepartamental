#Requires -Version 5.1
<#
.SYNOPSIS
    Script de despliegue para Plesk/IIS - Portal Gobernanza Musical
.DESCRIPTION
    Compila backend (.NET 9) y frontend (Angular), luego sube los archivos al servidor Plesk via FTP.
.PARAMETER FtpHost
    Host FTP del servidor Plesk (default: fichadepartamental.musigrafia.org)
.PARAMETER FtpUser
    Usuario FTP (default: sparra)
.PARAMETER FtpPass
    Contrasena FTP
.EXAMPLE
    .\deploy.ps1 -FtpPass '$Zg14tf9'
#>

param(
    [string]$FtpHost = "fichadepartamental.musigrafia.org",
    [string]$FtpUser = "sparra",
    [Parameter(Mandatory=$true)]
    [string]$FtpPass,
    [string]$BackendPath = "C:\Mincultura\ficha-gobernanza",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$BackendDir = "$BackendPath\src\backend"
$FrontendDir = "$BackendPath\src\frontend\portal-gobernanza-musical"
$DeployDir = "$BackendPath\deploy\plesk"

$PublishBackend = "$BackendPath\publish-backend"
$PublishFrontend = "$FrontendDir\dist\portal-gobernanza-musical\browser"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE PORTAL GOBERNANZA MUSICAL" -ForegroundColor Cyan
Write-Host "  Subdominio: fichadepartamental.musigrafia.org" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ── PASO 1: Compilar Frontend (Angular) ──
# Compilamos primero el frontend para inyectar su build en wwwroot del backend.
if (-not $SkipBuild) {
    Write-Host "[1/4] Compilando Frontend (Angular)..." -ForegroundColor Yellow
    Push-Location $FrontendDir
    ng build --configuration staging
    Pop-Location

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Fallo al compilar el frontend" -ForegroundColor Red
        exit 1
    }

    Write-Host "  Frontend compilado OK" -ForegroundColor Green
} else {
    Write-Host "[1/4] Saltando compilacion (SkipBuild)" -ForegroundColor DarkGray
}

# ── PASO 2: Compilar Backend (.NET 9) ──
if (-not $SkipBuild) {
    Write-Host "[2/4] Compilando Backend (.NET 9)..." -ForegroundColor Yellow
    if (Test-Path $PublishBackend) {
        Remove-Item -Recurse -Force $PublishBackend
    }
    dotnet publish "$BackendDir\PortalNacionalGobernanzaMusical.API\PortalNacionalGobernanzaMusical.API.csproj" `
        -c Release `
        -o $PublishBackend `
        --self-contained false

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Fallo al compilar el backend" -ForegroundColor Red
        exit 1
    }

    # Copiar web.config del deploy al publish (raiz httpdocs para AspNetCoreModule)
    Copy-Item "$DeployDir\web.config.backend" "$PublishBackend\web.config" -Force

    # Copiar appsettings.Staging.json como appsettings.json
    Copy-Item "$DeployDir\appsettings.Staging.json" "$PublishBackend\appsettings.json" -Force

    # Copiar build de Angular dentro de wwwroot del backend (app unica combinada)
    $WwwRootTarget = "$PublishBackend\wwwroot"
    if (Test-Path $WwwRootTarget) {
        Remove-Item -Recurse -Force $WwwRootTarget
    }
    New-Item -ItemType Directory -Path $WwwRootTarget | Out-Null
    Copy-Item -Path "$PublishFrontend\*" -Destination $WwwRootTarget -Recurse -Force

    Write-Host "  Backend compilado OK (con wwwroot de Angular)" -ForegroundColor Green
} else {
    Write-Host "[2/4] Saltando compilacion (SkipBuild)" -ForegroundColor DarkGray
}

# ── PASO 3: Subir app combinada via FTP a /httpdocs/ ──
Write-Host "[3/4] Subiendo app combinada (backend + frontend en wwwroot) a fichadepartamental.musigrafia.org/httpdocs/..." -ForegroundColor Yellow

# App unica: backend en raiz httpdocs, Angular dentro de httpdocs/wwwroot/
$FtpBackendDir = "/fichadepartamental.musigrafia.org/httpdocs"

# Crear directorio raiz si no existe
try {
    $ftpWebRequest = [System.Net.FtpWebRequest]::Create("ftp://$FtpHost/$FtpBackendDir/")
    $ftpWebRequest.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
    $ftpWebRequest.Credentials = New-Object System.Net.NetworkCredential($FtpUser, $FtpPass)
    $ftpWebRequest.UsePassive = $true
    $response = $ftpWebRequest.GetResponse()
    $response.Close()
} catch {
    # Directorio ya existe, ignorar
}

# Funcion para subir archivos via FTP
function Upload-FtpDirectory {
    param(
        [string]$LocalPath,
        [string]$RemotePath
    )

    $items = Get-ChildItem -Path $LocalPath -Force
    foreach ($item in $items) {
        $remoteItemPath = "$RemotePath/$($item.Name)"

        if ($item.PSIsContainer) {
            # Crear directorio remoto
            try {
                $ftpWebRequest = [System.Net.FtpWebRequest]::Create("ftp://$FtpHost$remoteItemPath")
                $ftpWebRequest.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
                $ftpWebRequest.Credentials = New-Object System.Net.NetworkCredential($FtpUser, $FtpPass)
                $ftpWebRequest.UsePassive = $true
                $response = $ftpWebRequest.GetResponse()
                $response.Close()
            } catch {
                # Directorio ya existe
            }
            Upload-FtpDirectory -LocalPath $item.FullName -RemotePath $remoteItemPath
        } else {
            # Subir archivo
            $ftpWebRequest = [System.Net.FtpWebRequest]::Create("ftp://$FtpHost$remoteItemPath")
            $ftpWebRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
            $ftpWebRequest.Credentials = New-Object System.Net.NetworkCredential($FtpUser, $FtpPass)
            $ftpWebRequest.UseBinary = $true
            $ftpWebRequest.UsePassive = $true
            $ftpWebRequest.ContentLength = $item.Length

            $fileContent = [System.IO.File]::ReadAllBytes($item.FullName)
            $ftpWebRequest.ContentLength = $fileContent.Length

            $requestStream = $ftpWebRequest.GetRequestStream()
            $requestStream.Write($fileContent, 0, $fileContent.Length)
            $requestStream.Close()
            $requestStream.Dispose()

            $response = $ftpWebRequest.GetResponse()
            $response.Close()

            Write-Host "  Subido: $($item.Name)" -ForegroundColor DarkGray
        }
    }
}

Upload-FtpDirectory -LocalPath $PublishBackend -RemotePath $FtpBackendDir
Write-Host "  App combinada subida OK" -ForegroundColor Green

# ── PASO 4: Recordatorio de configuracion manual en Plesk ──
Write-Host "[4/4] Recordatorio de configuracion en Plesk (manual, una sola vez):" -ForegroundColor Yellow
Write-Host "  - Panel ASP.NET Core del dominio:" -ForegroundColor Cyan
Write-Host "      * Archivo de inicio: \fichadepartamental\httpdocs\PortalNacionalGobernanzaMusical.API.dll" -ForegroundColor Cyan
Write-Host "      * Entorno: Staging" -ForegroundColor Cyan
Write-Host "  - Variables de entorno (boton Editar...):" -ForegroundColor Cyan
Write-Host "      ASPNETCORE_ENVIRONMENT=Staging" -ForegroundColor Cyan
Write-Host "      ConnectionStrings__DefaultConnection=server=localhost;port=3306;database=rafiaorg_ficha_departamental;user=rafiaorg_admin;password=57W3*n6at" -ForegroundColor Cyan
Write-Host "      Jwt__SecretKey=G0b3rn4nz4M_s3cr3t_K3y_2026_s3gur0_pr0d_32b!" -ForegroundColor Cyan
Write-Host "      Jwt__Issuer=PortalNacionalGobernanzaMusical" -ForegroundColor Cyan
Write-Host "      Jwt__Audience=PortalNacionalGobernanzaMusical.Client" -ForegroundColor Cyan
Write-Host "      Cors__AllowedOrigins=http://fichadepartamental.musigrafia.org" -ForegroundColor Cyan

# ── RESUMEN ──
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  DESPLIEGUE COMPLETADO" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "App unica: http://fichadepartamental.musigrafia.org" -ForegroundColor Cyan
Write-Host "Health:    http://fichadepartamental.musigrafia.org/health" -ForegroundColor Cyan
Write-Host "API:       http://fichadepartamental.musigrafia.org/api" -ForegroundColor Cyan
Write-Host ""
Write-Host "IMPORTANTE:" -ForegroundColor Yellow
Write-Host "1. Configurar en Plesk ASP.NET Core: Archivo de inicio = PortalNacionalGobernanzaMusical.API.dll" -ForegroundColor Yellow
Write-Host "2. Configurar Variables de entorno en Plesk (ver arriba)" -ForegroundColor Yellow
Write-Host "3. Verificar que el dominio fichadepartamental.musigrafia.org apunte al servidor" -ForegroundColor Yellow
Write-Host "4. Verificar que la BD MySQL rafiaorg_ficha_departamental este accesible desde el servidor" -ForegroundColor Yellow
Write-Host ""
