# Sesión — RadioAficionado

## Última sesión: 2026-03-22

### Lo que se hizo
- Creada toda la estructura de la solución (14 proyectos + 5 test)
- Implementados objetos de valor: Indicativo, Frecuencia, Localizador, Coordenadas
- Implementado BandaRadio con 24 bandas (LF/MF a microondas)
- Implementado ModoOperacion con 48 modos ADIF + 43 submodos
- Creado modelo de compliance regulatorio (PlanDeBanda, SegmentoBanda, ResultadoCompliance)
- Creadas interfaces: IControlRig, IControlRotador, IAudioPipeline, IDecodificadorDigital
- Implementada entidad Qso + handler MediatR RegistrarQso
- Configurado EF Core con conversiones + proveedores SQLite/PostgreSQL
- Creado shell Avalonia (escritorio) y ASP.NET MVC (web)
- 89 tests unitarios del dominio, todos pasando
- Renombrado Nativo.Ft8 → Nativo.ModosDigitales (extensibilidad)
- Añadido proyecto Nativo.Rotador

### Pendiente
- Implementar IControlRig (cliente TCP rigctld)
- Implementar captura de audio con NAudio
- Implementar FFT con FFTW3 para waterfall
- Implementar decodificador FT8 con ft8_lib
- Implementar WaterfallControl con SkiaSharp

### Problemas encontrados
- Ninguno crítico. El build y los tests pasan sin errores ni warnings.

### Siguiente paso sugerido
- Fase 1: Empezar con IControlRig (cliente TCP a rigctld) — es la base sobre la que se construye todo lo demás.
