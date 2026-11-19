<div align="center">

<img src="docs/images/banner.jpg" alt="LaraLauncher — 10-Foot Desktop Hub for Windows" width="100%" />

# LaraLauncher

**Launcher de escritorio 10-Foot estilo Roku / Flex Launcher para Windows, construido en C# (WPF / .NET 8) bajo principios SOLID y KISS.**

[![.NET 8](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF%2010--Foot-7E4AE8?style=for-the-badge&logo=windows&logoColor=white)](#)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?style=for-the-badge&logo=windows11&logoColor=white)](#)
[![Architecture](https://img.shields.io/badge/Architecture-SOLID%20%2B%20KISS-1A7F3E?style=for-the-badge)](#arquitectura-solid--kiss)
[![Tests](https://img.shields.io/badge/Tests-58%20Passing-2EA44F?style=for-the-badge&logo=xunit&logoColor=white)](#desarrollo-y-pruebas)

[Características](#-características-principales) •
[Vistas Previas](#-vistas-previas) •
[Controles](#-controles-y-navegación) •
[Arquitectura](#-arquitectura-solid--kiss) •
[Instalación](#-instalación-y-empaquetado) •
[Desarrollo](#-desarrollo-y-pruebas)

</div>

---

## ✨ Características Principales

- **📺 Interfaz 10-Foot estilo Roku:** Menú lateral oscuro, tarjetas horizontales con foco de alto contraste y zoom animado, barra de ventana negra nativa (`DwmSetWindowAttribute`), reloj en vivo y sonidos sutiles sintetizados en memoria (desactivables).
- **🖱️ Gestión 100% Gráfica (cero edición manual de archivos):** Escanea automáticamente las aplicaciones instaladas en el Menú Inicio de Windows, extrae sus íconos en caché PNG y permite marcar/desmarcar qué apps aparecen en Inicio, añadir `.exe` / `.lnk` manualmente, editar nombre/categoría/color y **reordenar tarjetas visualmente** con el control remoto o teclado.
- **🌐 Creador de WebApps + Teclado Virtual 5×10:** Convierte cualquier sitio web (`youtube.com`, `netflix.com`, `twitch.tv`, servidores locales, etc.) en una tarjeta de Inicio que se abre en tu navegador por defecto, con descarga automática de favicon y un **teclado virtual integrado** con atajos URL (`www.`, `.com`, `.cl`, `.tv`) operable totalmente con control remoto o gamepad.
- **⛅ Clima Actual y Pronóstico de 7 Días:** Integración gratuita y sin API Key con **Open-Meteo** (clima actual, sensación térmica, humedad, viento, pronóstico de los próximos días y buscador gráfico de ciudad), con caché local en disco (TTL de 30 min) y respaldo automático *offline*.
- **🎮 Teclado, Control Remoto y Gamepad (XInput):** Enrutador unificado de entrada para teclado, controles remotos IR/RF/Air Mouse/CEC y mandos **XInput** (D-Pad, stick izquierdo con *deadzone* y auto-repetición suave). El polling del mando se pausa automáticamente al abrir juegos o perder foco.
- **🚀 Listo para Uso Diario:** Arranque automático con Windows por usuario (`HKCU\...\Run` sin privilegios de administrador), recuperación automática de foco al cerrar una app lanzada, instancia única (`Mutex`), logging rotativo de 7 días e instalador **Inno Setup 6**.

---

## 🖼️ Vistas Previas

### Pantalla de Inicio (Grilla 10-Foot estilo Roku)
<div align="center">
  <img src="docs/images/preview-home.jpg" alt="LaraLauncher — Pantalla de Inicio" width="92%" />
</div>

### Creador de WebApps con Teclado Virtual y Pronóstico de 7 Días
<div align="center">
  <img src="docs/images/preview-features.jpg" alt="LaraLauncher — WebApps con Teclado Virtual y Pronóstico de 7 Días" width="92%" />
</div>

---

## 🎮 Controles y Navegación

Toda la aplicación (Inicio, WebApps, Teclado Virtual, Clima, Gestor de Apps, Ajustes y Diálogos de Energía) está diseñada para operarse **sin tocar el mouse**:

| Acción | Teclado / Control Remoto (IR · RF · CEC) | Mando / Gamepad (XInput) |
| :--- | :--- | :--- |
| **Navegar** | `↑` `↓` `←` `→` | `D-Pad` o `Stick Izquierdo` |
| **Abrir / Seleccionar** | `Enter` / `Espacio` | Botón `A` |
| **Atrás / Volver al menú lateral** | `Esc` / `Backspace` / `BrowserBack` | Botón `B` |
| **Opciones de tarjeta** *(Mover · Editar · Quitar)* | `M` / Tecla `Menú` / `Clic derecho` | Botón `Y` o `Start` |
| **Volver directo a Inicio** | `Home` / `BrowserHome` | Botón `Back` |
| **Alternar Pantalla Completa / Ventana** | `F11` | *(desde Ajustes)* |

---

## 🏛️ Arquitectura (SOLID + KISS)

La solución mantiene **3 proyectos de producción + 1 de pruebas** con fronteras claras y sin sobre-ingeniería:

```mermaid
flowchart LR
    subgraph App["LaraLauncher.App (WPF / MVVM)"]
        V["Views y Controls (Roku UI + Teclado Virtual)"]
        VM["ViewModels (CommunityToolkit.Mvvm)"]
        IR["WpfInputRouter"]
    end

    subgraph Core["LaraLauncher.Core (C# Puro)"]
        M["Models (AppTile, Settings, Weather, GeoLocation)"]
        I["Interfaces Segregadas (ISP / DIP)"]
        S["Servicios Puros (GridNavigator, CachedWeather, GamepadInterpreter)"]
    end

    subgraph Infra["LaraLauncher.Infrastructure (Windows / IO / HTTP)"]
        JSON["JsonConfigRepository y FileAppLogger"]
        WIN["WindowsAppLauncher, Scanner, Startup, Power"]
        NET["OpenMeteoWeatherService y FaviconFetcher"]
        HW["XInputGamepadProvider y NavigationSoundService"]
    end

    V --> VM
    IR --> VM
    VM --> I
    VM --> S
    S --> I
    JSON -.-> I
    WIN -.-> I
    NET -.-> I
    HW -.-> I
```

### Principios aplicados en el código
- **SRP (Responsabilidad Única):** Escaneo de apps (`WindowsInstalledAppsScanner`), caché de íconos (`IconCacheService`), construcción de procesos (`AppLaunchInfoBuilder`) y estado de mando (`GamepadStateInterpreter`) viven separados y enfocados.
- **OCP (Abierto/Cerrado):** Nuevos dispositivos de entrada se agregan implementando `IInputProvider`, y el caché de clima decora `IWeatherService` vía `CachedWeatherService`.
- **ISP & DIP:** `LaraLauncher.Core` no tiene dependencias de WPF ni Win32; todos los ViewModels dependen de interfaces pequeñas cableadas en `App.xaml.cs`.
- **KISS:** Persistencia atómica en JSON (`%AppData%\LaraLauncher\`), cero motores SQL pesados, P/Invoke directo y liviano para XInput/DWM/Audio.

### Estructura del Repositorio

```text
LaraLauncher/
├── LaraLauncher.sln
├── docs/images/                      # Recursos visuales del README
├── installer/setup.iss               # Script de instalador Inno Setup 6
├── scripts/build-release.ps1         # Pipeline Release (Tests + Publish + Setup)
├── src/
│   ├── LaraLauncher.Core/            # Dominio puro, interfaces y lógica testeable
│   ├── LaraLauncher.Infrastructure/  # Windows, JSON atómico, Open-Meteo, XInput, Audio
│   └── LaraLauncher.App/             # WPF, RokuTheme, Controls, Views y ViewModels
└── tests/
    └── LaraLauncher.Tests/           # 58 pruebas unitarias con xUnit
```

---

## 📦 Instalación y Empaquetado

### Requisitos previos
- **Windows 10 / 11 (x64)**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) *(solo para compilar; el paquete publicado es autocontenido)*
- *(Opcional)* [Inno Setup 6](https://jrsoftware.org/isinfo.php) para generar el instalador `.exe`:
  ```powershell
  winget install --id JRSoftware.InnoSetup --exact
  ```

### Compilar versión Release e instalador en 1 paso
```powershell
.\scripts\build-release.ps1
```
El script ejecuta:
1. Toda la suite de pruebas unitarias en modo `Release`.
2. Publicación autocontenida `win-x64` en `publish\win-x64\LaraLauncher.exe`.
3. Compilación automática de `installer\Output\LaraLauncherSetup-1.0.0.exe` (incluye casilla para iniciar automáticamente con Windows).

---

## 🛠️ Desarrollo y Pruebas

```powershell
# Ejecutar en modo ventana (recomendado para desarrollo; F11 pasa a pantalla completa)
dotnet run --project src\LaraLauncher.App -- --windowed

# Ejecutar directamente en pantalla completa (modo TV)
dotnet run --project src\LaraLauncher.App

# Ejecutar todas las pruebas unitarias
dotnet test LaraLauncher.sln -c Release
```

### Datos locales (gestionados automáticamente por la app)
Todos los datos del usuario se almacenan en `%AppData%\LaraLauncher\`:
- `config.json` — Tarjetas, WebApps y preferencias (escritura atómica con respaldo automático ante corrupción).
- `icons\` — Caché PNG de íconos extraídos de Windows y favicons de WebApps.
- `cache\weather-cache.json` — Caché local del pronóstico de 7 días para arranque instantáneo y soporte *offline*.
- `logs\` — Logs diarios con rotación automática de 7 días.

---

<div align="center">
<sub>Hecho con C#, WPF y .NET 8 — Diseñado para disfrutarse desde el sofá.</sub>
</div>
