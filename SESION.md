# Sesion — RadioAficionado

## Ultima sesion: 2026-05-17

### Lo que se hizo

#### ADR-005 Fases 1-5 completas: Control remoto del rig (2026-05-16 — 2026-05-17)

**Fase 1 — Infraestructura:**
- DTOs compartidos: ComandoRemotoRig, RespuestaRemotoRig, EstadoRigRemotoDto, TipoComandoRig, LineaEspectroRemotaDto, MensajeDecodificadoRemotoDto, SenalizacionWebRtc
- Entidad ClaveApi + tabla claves_api (SHA-256 + salt, prefijo, FK usuario)
- ServicioApiKeys, ApiKeyAuthenticationHandler, RegistroServiciosConectados
- HubTunelServicio (auth ApiKey) + HubRelayRig (auth cookie)
- ClienteRelaySignalR (IHostedService, reconexion exponencial)
- ConfiguracionRemoto, ConversorEstadoRemoto

**Fase 2 — Vista web:**
- ControlRemotoController + ApiKeysController
- Vistas: control remoto LCD + gestion API keys
- controlRemoto.js con SignalR /hubs/relay-rig
- Nav links para usuarios autenticados

**Fase 3 — Relay waterfall/decodificaciones:**
- IClienteHubRelay/IClienteHubTunel ampliadas
- Throttle waterfall: 8fps servicio->web, 10fps web->browser
- Suscripcion a IServicioWaterfall + IRegistroDecodificadores

**Fase 4 — WebRTC audio (stub):**
- AdaptadorWebRtcAudio stub con logging
- Senalizacion SDP/ICE relay browser <-> servicio

**Fase 5 — Hardening:**
- RateLimitingMiddleware (20 req/seg, 429 + Retry-After)
- ControladorTimeoutPtt (180s max, desactiva via EjecutarComandoRig por usuario)
- MetricasConexion (contadores atomicos Interlocked)
- Endpoint /api/metricas (solo desarrollo)

**Bugs corregidos durante integracion:**
- ConversorEstadoRemoto: object initializer -> constructor posicional
- ClienteRelaySignalR: payload tipo mismatch, logger tipo mismatch
- ApiKeysController: using incorrecto (Dominio.Interfaces -> Web.Servicios)
- ControladorTimeoutPtt: llamaba RecibirCambiarPtt inexistente -> EjecutarComandoRig + sync-over-async
- Vista ApiKeys: UltimoUso -> FechaUltimoUso

**Tests:** 1362 pasando, 0 fallos + tests nuevos para Fases 2-5 en progreso

#### Dashboard solar, Mapa QSOs, Espectro, Herramientas, 39 rigs (2026-05-16)

**Dashboard solar NOAA en tiempo real:**
- ClienteDatosSolares con 6 endpoints NOAA + XML N0NBH, cache SemaphoreSlim
- Fusionado en Propagacion: SFI, Kp, viento solar, escalas NOAA, condiciones HF/VHF, alertas, grafico historico
- 18 tests unitarios para todos los parsers

**Mapa QSOs:**
- Leaflet + Geodesic great circle lines, conversor Maidenhead, Haversine, filtro por banda

**Tabla Espectro Radioelectrico:**
- 58+ entradas de 0 Hz a 250 GHz, 8 categorias coloreadas, filtros

**Herramientas para radioaficionados:**
- Conversor potencia, distancia grids, conversor Maidenhead, plan bandas IARU R1, RST, alfabeto NATO

**39 modelos de radio CAT:**
- Yaesu 16, Icom 12 (CI-V), Kenwood 8, Elecraft 5, FlexRadio 3

**Sidebar:** 12 secciones (Operacion, Logbook, DX Cluster, DXCC, Propagacion, POTA/SOTA, Contest, Satelites, Mapa QSOs, Espectro, Herramientas)

**Tests:** 1348 tests pasando (0 fallos)

### Pendiente
- Migracion EF para tabla claves_api en PostgreSQL (pendiente: requiere conexion a DB)
- Implementacion real WebRTC con SIPSorcery (actualmente stub)
- i18n RadioAficionado.Web (solo Servicio esta internacionalizado)
- Test end-to-end con radio real conectada via rigctld + control remoto web

### Siguiente paso sugerido
- Migracion EF para tabla claves_api: `dotnet ef migrations add AgregarClavesApi`
- Integrar SIPSorcery para audio WebRTC real (reemplazar AdaptadorWebRtcAudio stub)

#### Graficos, ADIF drag-drop, Docker, CI/CD (2026-05-16)

**Graficos interactivos:**
- Chart.js 4.4.8 local: grafico de barras en panel propagacion (dia/noche por banda)
- Leaflet 1.9.4 local: mapa mundial DXCC con marcadores por entidad trabajada/confirmada
- Ambas librerias descargadas localmente en wwwroot/lib/ (sin CDN)

**Importar ADIF drag-and-drop:**
- Zona de drop overlay sobre panel logbook con indicador visual
- Detecta extensiones .adi/.adif, usa endpoint POST existente
- Contador dragenter/dragleave para evitar parpadeo al arrastrar sobre hijos

**Docker compose:**
- Dockerfile multi-stage (SDK build + ASP.NET runtime)
- docker-compose.yml con web (puerto 5200) + PostgreSQL 17 Alpine
- .dockerignore para excluir bin/obj/logs/tests

**CI/CD GitHub Actions:**
- Workflow ci.yml: compilar, testear con PostgreSQL, reportar resultados TRX
- Job Docker en push a main/develop con cache GHA

**Tests nuevos: 17**
- 8 para ClienteDxClusterTelnet (parseo spots)
- 4 para PropagacionApiController
- 5 para DxccApiController

**Total: 1300 tests, 0 fallos**

#### Paneles conectados, Log QSO, Tests integracion, Lanzador persistido (2026-05-15)

**Bugs corregidos:**
- dxcc.js: clases CSS `dxcc-confirmado` corregidas a `dxcc-banda-confirmado` (mismatch con paneles.css)
- propagacion.js: indices solares ahora muestran colores semaforo (verde/amarillo/rojo)

**DXCC mejorado:**
- Barra de progreso actualizada dinamicamente (meta 340 entidades DXCC)
- Filtro por continente con botones toggle (AF, AS, EU, NA, OC, SA)

**Log QSO desde operacion:**
- Nuevo DTO RegistroQsoDto + endpoint POST /api/logbook/registrar
- Validacion completa: campos obligatorios, parseo modo, deteccion duplicados
- Boton Log QSO conectado en operacion.js con notificacion temporal
- Variables frecuenciaActualHz/modoActual sincronizadas con SignalR

**ContestApiController con datos reales:**
- Inyeccion de IRepositorioQso, QSOs de ultimas 48h
- Multiplicadores como entidades DXCC unicas por banda
- Puntos por continente (3 pts inter-continente, 1 pt propio), rate por hora
- Fallback a datos ejemplo si no hay QSOs recientes

**Lanzador WebView2 persistido:**
- ConfiguracionLanzador.cs: JSON persistence (posicion, tamano, maximizado, puerto, DevTools)
- VentanaPrincipal restaura posicion/tamano al abrir, guarda al cerrar
- Puerto configurable (ya no hardcodeado a 5200)

**Tests de integracion con WebApplicationFactory:**
- 22 tests de integracion nuevos: 8 vistas MVC + 14 APIs REST (GET + POST)
- InMemory DB para evitar incompatibilidades SQLite/DateTimeOffset
- Mocks de servicios nativos (audio, waterfall, rotador)

### Que quedo pendiente
- Verificacion end-to-end con radio real conectada
- Exportar ADIF desde la UI web (endpoint GET ya existe, falta enlazar descarga)
- Panel de configuracion DX Cluster en la UI (cambiar servidor/indicativo sin editar appsettings)
- Despliegue real con docker compose en servidor
- Push a GitHub y verificar que CI pasa

### Siguiente paso sugerido
- Verificar despliegue Docker en local: `docker compose up -d --build`
- Push a GitHub y validar pipeline CI/CD
- Test end-to-end con radio real conectada via rigctld
