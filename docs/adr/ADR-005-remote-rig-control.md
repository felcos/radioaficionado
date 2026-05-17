# ADR-005: Remote Rig Control - Arquitectura de Control Remoto de Radio

**Fecha:** 2026-05-16
**Estado:** Propuesto

## Contexto

RadioAficionado tiene dos aplicaciones separadas:
- **RadioAficionado.Servicio**: app local en `http://localhost:5200` con SignalR hubs. Controla la radio fisica via CAT serial o rigctld TCP.
- **RadioAficionado.Web**: web publica con ASP.NET Identity, PostgreSQL. No tiene acceso directo al hardware.

El objetivo: un usuario autenticado en la Web puede controlar remotamente su radio conectada al Servicio corriendo en su PC local.

## Diagrama de Flujo

```
                            INTERNET
  Browser          RadioAficionado.Web           RadioAficionado.Servicio
  (User)           (Servidor Publico)            (Casa del usuario)
    |                      |                              |
    |---SignalR WSS------->|                              |
    |  CambiarFrecuencia   |---SignalR WSS (tunel)------->|
    |                      |  EjecutarComandoRig          |
    |                      |                              |---> IControlRig
    |                      |                              |     (CAT/rigctld)
    |                      |                              |<--- Radio responde
    |                      |<--ReportarEstadoRig----------|
    |<--RecibirEstadoRig---|                              |
    |                      |                              |
    |===WebRTC P2P (audio)=============================>  |
    |  (microfono TX)      |  (senalizacion via SignalR)  |---> Audio a radio
    |<===Audio RX de radio=|==============================|
```

## Componentes Nuevos

### En RadioAficionado.Web
- `HubTunelServicio` — acepta conexiones del Servicio local (auth por API key)
- `HubRelayRig` — acepta conexiones del browser (auth por cookie Identity)
- `HubRelaySenalizacion` — relay de senalizacion WebRTC
- `RegistroServiciosConectados` — singleton: mapea UsuarioId -> ConnectionId del Servicio
- `ServicioApiKeys` — genera/valida API keys hasheadas en PostgreSQL
- `ApiKeyAuthenticationHandler` — handler de auth custom para el header X-Api-Key
- Tabla `api_keys` — UUID PK, usuario_id FK, hash_clave, nombre, fechas, activa

### En RadioAficionado.Servicio
- `ClienteRelaySignalR` — IHostedService que se conecta a Web como cliente SignalR
- `ConfiguracionRemoto` — URL servidor + API key, persistido en JSON local
- `AdaptadorWebRtcAudio` — bridge entre NAudio PCM y SIPSorcery RTCPeerConnection

### En RadioAficionado.Dominio
- `ComandoRemotoRig` — record DTO para comandos del relay
- `RespuestaRemotoRig` — record DTO para respuestas

## WebRTC para Audio SSB

- WebRTC P2P elimina latencia del relay (<150ms vs 200-500ms por SignalR)
- Codec Opus a 48kHz, 16kbps suficiente para voz SSB
- SIPSorcery (NuGet, C# puro) como stack WebRTC en el Servicio
- Senalizacion (SDP offer/answer/ICE) via SignalR como canal auxiliar
- Browser usa API nativa RTCPeerConnection + getUserMedia

## Seguridad

- API key por usuario, hasheada SHA-256 + salt en PostgreSQL
- Aislamiento: cada usuario solo puede controlar SU Servicio (validacion de UsuarioId)
- Rate limiting: 20 comandos/seg por usuario
- PTT timeout: 180s maximo (regulaciones radioaficionado)
- HTTPS/WSS obligatorio, DTLS-SRTP en WebRTC
- TURN con credenciales temporales (12h) para NAT simetrico

## Latencia Objetivo

| Operacion | Target |
|---|---|
| Cambio frecuencia/modo | <500ms |
| PTT on/off | <200ms |
| Audio RX/TX | <150ms (WebRTC P2P) |
| Estado del rig | <1000ms (push) |
| Waterfall remoto | <2000ms (5-10 fps) |

## Fases de Implementacion

1. **Infraestructura de tunelado** — API keys, HubTunelServicio, ClienteRelaySignalR, reconexion
2. **Relay de comandos** — DTOs compartidos, HubRelayRig, vista web de control remoto
3. **Relay waterfall/decodificaciones** — forward throttleado de espectro y mensajes
4. **Audio WebRTC** — SIPSorcery, AdaptadorWebRtcAudio, PTT con microfono web
5. **Hardening** — rate limiting, metricas, logging, documentacion

## Alternativas Descartadas

- **Puerto publico con port forwarding**: requiere config de router, inseguro
- **gRPC bidireccional**: complicacion innecesaria con HTTP/2
- **Audio por SignalR**: latencia inaceptable (200-500ms) para SSB
- **Tailscale/WireGuard**: dependencia externa, pero opcion futura para audio

## Consecuencias

- Complejidad de tunelado inverso, pero transparente para el usuario
- RadioAficionado.Web pasa a tener funcionalidad real-time critica
- DTOs deben mantenerse sincronizados (mover a proyecto RadioAficionado.Contratos)
- Se necesita servidor TURN para ~5-10% de usuarios con NAT simetrico
