# Sesion — RadioAficionado

## Ultima sesion: 2026-05-26

### Lo que se hizo

#### Fase 14: Filtros DX Cluster + alertas + alarma (2026-05-26)
- Filtro por modo en escritorio (ComboBox 13 modos) y filtro por indicativo en web
- Sistema de alertas completo: AlertaDxCluster (dominio), IServicioAlertas, ServicioAlertas (5 tipos de alerta)
- Integracion SignalR para alertas web con toasts y sonido (Web Audio API)
- Alarma configurable (0-60 min) en panel activaciones
- 21 tests para ServicioAlertas

#### Fase 15: Sistema de awards/diplomas (2026-05-26)
- EstadisticasAwards: WAC (6 continentes), WAZ (40 zonas CQ), WAS (50 estados USA), VUCC (grids VHF+)
- CatalogoEstadosUsa con 50 estados
- 24 tests unitarios cubriendo todos los diplomas

#### Fase 16: Control avanzado rig — SWR/ALC (2026-05-26)
- Lectura SWR (`l SWR`) y ALC (`l ALC`) desde rigctld, solo durante TX
- EstadoRig, EstadoRigDto, EstadoRigRemotoDto: propiedades Swr y Alc
- Medidores visuales en barra rig web (ocultos cuando no TX, gradiente verde/amarillo/rojo)
- operacion.js: renderizado dinamico SWR (1.0-3.0:1) y ALC (0-100%)
- Tests actualizados para DTOs con Swr/Alc

### Que quedo pendiente
- Fase 17: Calidad de vida (formatos fecha configurables, notas estacion, backup automatico, busqueda entre paneles)
- Fase 18: Modos digitales adicionales (PSK250, THOR, DominoEX, MFSK-128, FSQ)
- WebRTC audio real (SIPSorcery), migracion claves_api PostgreSQL, i18n web, e2e tests

### Siguiente paso sugerido
- Continuar con Fase 17 (calidad de vida) o Fase 18 (modos digitales adicionales)

---

#### Despliegue produccion + utilidades publicas web + descargas corregidas (2026-05-17)

**Despliegue en ham.felcos.es:**
- Publicacion completa en servidor Ubuntu ARM64 (rivendel) con PostgreSQL, Nginx, SSL
- Servicio systemd `radioaficionado` en puerto 5700
- Migracion Identity PostgreSQL (idempotent SQL) + tablas dominio manuales (Qsos, Activaciones)
- Permisos BD: GRANT ALL + ALTER DEFAULT PRIVILEGES para radioaficionado_app
- Credenciales guardadas en DESPLIEGUE.md (local, excluido de git)

**Paginas publicas (sin autenticacion):**
- Estadisticas, Foro (lectura), Mapa — movidos fuera del bloque [Authorize]
- Solo Control Remoto permanece privado

**Utilidades publicas nuevas (UtilidadesController):**
- Herramientas: conversor potencia, calculadora distancia grid, conversor Maidenhead, plan bandas IARU, RST, alfabeto NATO
- Espectro: tabla del espectro radioelectrico (58+ entradas, 8 categorias, filtros)
- Propagacion Solar: dashboard NOAA en tiempo real con Chart.js (SFI, Kp, viento solar, escalas, alertas)
- Satelites: tabla de 13 satelites amateur con frecuencias y modos

**Descargas app corregidas:**
- Cambiado de RadioAficionado.Escritorio (app vieja Avalonia) a RadioAficionado.Servicio (app web local correcta)
- PublishSingleFile: un solo ejecutable por plataforma (~55-69 MB)
- Eliminados BuildHost-net472, createdump.exe, .pdb y otros artefactos de build
- Windows: RadioAficionado.Servicio.exe + wwwroot
- Linux/macOS: RadioAficionado.Servicio + wwwroot

**Otros:**
- Favicon SVG (antena con ondas, gradiente verde/azul)
- Layout: dropdown "Descargar App" con Windows/Linux/macOS, dropdown "Utilidades"
- .gitignore actualizado: excluye DESPLIEGUE.md, conexion_vm.txt, *.key, *.tar.gz, publish-*, logs

**ADR-005 Fases 1-5 completas: Control remoto del rig (2026-05-16 — 2026-05-17)**

- DTOs compartidos: ComandoRemotoRig, RespuestaRemotoRig, EstadoRigRemotoDto, TipoComandoRig
- ClaveApi + ServicioApiKeys + ApiKeyAuthenticationHandler
- HubTunelServicio (auth ApiKey) + HubRelayRig (auth cookie)
- ClienteRelaySignalR (IHostedService, reconexion exponencial)
- ControlRemotoController + controlRemoto.js (LCD, S-meter, PTT)
- Relay waterfall/decodificaciones con throttle
- WebRTC audio stub (AdaptadorWebRtcAudio)
- RateLimitingMiddleware, ControladorTimeoutPtt, MetricasConexion
- Tests: control remoto, PTT timeout, metricas, rate limiting

**Bugs corregidos:**
- ApiKeysController: using incorrecto
- AdaptadorWebRtcAudio: logger type mismatch
- ControladorTimeoutPtt: metodo inexistente + sync-over-async
- Vista ApiKeys: UltimoUso -> FechaUltimoUso
- Serilog.ILogger DI para decodificadores
- PostgreSQL: permisos tablas, columna Potencia faltante

**Push a GitHub:** commit c65d5e2 en feature/fase-1-rig-waterfall-ft8 (116 archivos, 12,820 lineas)

### Pendiente
- Migracion EF para tabla claves_api en PostgreSQL
- Implementacion real WebRTC con SIPSorcery (actualmente stub)
- i18n RadioAficionado.Web (solo Servicio esta internacionalizado)
- Test end-to-end con radio real conectada via rigctld + control remoto web
- Merge feature branch a develop/main

### Siguiente paso sugerido
- Merge feature/fase-1-rig-waterfall-ft8 a main (crear PR)
- Migracion EF para tabla claves_api
- Integrar SIPSorcery para audio WebRTC real
