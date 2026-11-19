# LaraLauncher

Launcher de escritorio estilo **Roku / Flex Launcher** en **C# (WPF / .NET 8)** diseñado para usarse cómodamente con **teclado**, **control remoto (IR/RF/Air Mouse/CEC)** y **mando Gamepad (XInput)**.

## Características Principales
- **Interfaz 10-Foot estilo Roku:** Menú lateral izquierdo, grilla de tarjetas horizontales (3, 4 o 5 columnas configurables), foco de alto contraste con zoom suave, reloj en vivo y sonidos sutiles de navegación (desactivables).
- **Gestión 100% gráfica de aplicaciones:** Detecta automáticamente las apps instaladas del Menú Inicio de Windows, extrae sus íconos en caché PNG y permite marcar/desmarcar qué apps aparecen en Inicio, agregar `.exe`/`.lnk` manualmente, editar nombre/categoría/color y reordenar tarjetas visualmente sin editar archivos de configuración.
- **Creador de WebApps + Teclado Virtual 10-Foot:** Sección **WebApps** para crear tarjetas de cualquier sitio web que se abren en el navegador por defecto (con descarga automática de favicon), incluyendo un teclado virtual integrado de 5×10 con atajos URL (`www.`, `.com`, `.cl`, `.tv`) operable al 100% con control remoto o gamepad.
- **Clima actual y pronóstico de 7 días:** Integración sin API Key con **Open-Meteo** (pronóstico + buscador gráfico de ciudad), caché local en disco con TTL de 30 minutos y respaldo *offline*.
- **Soporte completo de entradas:** Teclado, controles remotos estándar y mandos **XInput** (D-Pad, stick izquierdo con *deadzone*, `A` Abrir, `B` Atrás, `Y`/`Start` Opciones, `Back` Inicio). El polling del mando se pausa automáticamente al abrir juegos o perder foco.
- **Instalable y con arranque automático:** Registro limpio por usuario en `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, instancia única con recuperación de ventana y script listo para **Inno Setup**.

## Arquitectura (SOLID + KISS)
- `src/LaraLauncher.Core`: Modelos puros, interfaces segregadas y lógica testeable (`GridNavigator`, `AppCatalogService`, `AppDiscoveryFilter`, `CachedWeatherService`, `GamepadStateInterpreter`). Sin dependencias de WPF ni Win32.
- `src/LaraLauncher.Infrastructure`: Persistencia JSON atómica (`%AppData%\LaraLauncher\`), escáner de apps e íconos, lanzador de procesos con retorno de foco, cliente Open-Meteo, XInput P/Invoke, registro de arranque, energía, instancia única, audio y logging rotativo.
- `src/LaraLauncher.App`: Capa WPF / MVVM (`CommunityToolkit.Mvvm` + `Microsoft.Extensions.DependencyInjection`).
- `tests/LaraLauncher.Tests`: Pruebas unitarias con xUnit.

## Controles Rápidos
| Acción | Teclado / Control Remoto | Mando (XInput) |
| :--- | :--- | :--- |
| Navegar | Flechas | D-Pad / Stick izquierdo |
| Abrir / Confirmar | `Enter` / `Espacio` | `A` |
| Atrás / Volver al menú | `Esc` / `Back` / `BrowserBack` | `B` |
| Opciones de tarjeta (Mover / Editar / Quitar) | `M` / Tecla Menú / Clic derecho | `Y` / `Start` |
| Ir a Inicio | `Home` / `BrowserHome` | `Back` |
| Pantalla completa / Ventana | `F11` | *(desde Ajustes)* |

## Cómo Ejecutar en Desarrollo
```powershell
# En modo ventana (ideal para pruebas):
dotnet run --project src\LaraLauncher.App -- --windowed

# En pantalla completa:
dotnet run --project src\LaraLauncher.App

# Ejecutar pruebas unitarias:
dotnet test LaraLauncher.sln
```

## Cómo Publicar y Generar el Instalador de Windows
```powershell
.\scripts\build-release.ps1
```
El script ejecuta todas las pruebas en `Release`, publica el ejecutable autocontenido en `publish\win-x64\` y, si **Inno Setup 6** está instalado (`winget install --id JRSoftware.InnoSetup --exact`), compila automáticamente el instalador en `installer\Output\LaraLauncherSetup-1.0.0.exe`.
