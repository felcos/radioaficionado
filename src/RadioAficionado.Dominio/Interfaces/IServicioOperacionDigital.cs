namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Fase del QSO FT8 en la maquina de estados de auto-sequencing.
/// </summary>
public enum FaseQsoFt8
{
    /// <summary>No hay QSO en curso.</summary>
    Inactivo,
    /// <summary>Se envio un CQ y se espera respuesta.</summary>
    CQEnviado,
    /// <summary>Se selecciono una estacion y se espera su respuesta.</summary>
    EsperandoRespuesta,
    /// <summary>Se envio el reporte de señal.</summary>
    ReporteEnviado,
    /// <summary>Se envio RRR.</summary>
    RRREnviado,
    /// <summary>Se envio 73.</summary>
    SetentatresEnviado,
    /// <summary>QSO completado, listo para log.</summary>
    QsoCompletado
}

/// <summary>
/// Estado actual de la secuencia de operacion digital.
/// </summary>
/// <param name="Fase">Fase actual del QSO.</param>
/// <param name="IndicativoDx">Indicativo de la estacion DX en curso.</param>
/// <param name="GridDx">Localizador de la estacion DX.</param>
/// <param name="ReporteEnviado">Reporte de señal enviado.</param>
/// <param name="ReporteRecibido">Reporte de señal recibido.</param>
/// <param name="MensajeTxActual">Texto del mensaje que se transmitira.</param>
/// <param name="TxHabilitado">Si la transmision esta habilitada.</param>
/// <param name="AutoSecuenciaActiva">Si el auto-sequencing esta activo.</param>
/// <param name="TransmitiendoAhora">Si se esta transmitiendo en este momento.</param>
/// <param name="VentanaPar">True para ventanas pares (0, 30s), false para impares (15, 45s).</param>
public sealed record EstadoSecuencia(
    FaseQsoFt8 Fase,
    string? IndicativoDx,
    string? GridDx,
    int? ReporteEnviado,
    int? ReporteRecibido,
    string? MensajeTxActual,
    bool TxHabilitado,
    bool AutoSecuenciaActiva,
    bool TransmitiendoAhora,
    bool VentanaPar);

/// <summary>
/// Configuracion de la secuencia de operacion digital.
/// </summary>
/// <param name="MiIndicativo">Indicativo propio del operador.</param>
/// <param name="MiLocalizador">Localizador Maidenhead propio.</param>
/// <param name="FrecuenciaTxHz">Frecuencia de audio TX en Hz.</param>
/// <param name="VentanaPar">True para transmitir en ventanas pares.</param>
public sealed record ConfiguracionSecuencia(
    string MiIndicativo,
    string MiLocalizador,
    int FrecuenciaTxHz,
    bool VentanaPar);

/// <summary>
/// Servicio que orquesta la operacion digital FT8 con auto-sequencing.
/// Gestiona la maquina de estados del QSO, genera mensajes TX,
/// sincroniza con ventanas de 15 segundos y controla PTT.
/// </summary>
/// <remarks>
/// <para><b>Para que sirve:</b> Automatiza el flujo de un QSO FT8: CQ -> respuesta -> reporte -> RRR -> 73. Maneja la sincronizacion temporal, generacion de audio TX y control de PTT.</para>
/// <para><b>Como se usa:</b> Se inyecta por constructor. Se configura con <see cref="ConfigurarAsync"/>, se activa TX con <see cref="HabilitarTxAsync"/> y se responde a decodificaciones con <see cref="ProcesarDecodificacionAsync"/>.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Servicio.Servicios.ServicioOperacionDigital</c>.</para>
/// <para><b>Registro DI:</b> Registrada como Singleton en <c>RadioAficionado.Servicio.Program</c>.</para>
/// <para><b>Dependencias:</b> <see cref="IControlRig"/> (PTT), <see cref="IAudioPipeline"/> (audio TX), <see cref="IDecodificadorDigital"/> (FT8).</para>
/// </remarks>
public interface IServicioOperacionDigital : IAsyncDisposable
{
    /// <summary>Estado actual de la secuencia.</summary>
    EstadoSecuencia EstadoActual { get; }

    /// <summary>Evento disparado cuando cambia el estado de la secuencia.</summary>
    event EventHandler<EstadoSecuencia>? EstadoCambiado;

    /// <summary>Configura la secuencia con indicativo y localizador propios.</summary>
    Task ConfigurarAsync(ConfiguracionSecuencia configuracion, CancellationToken ct = default);

    /// <summary>Habilita o deshabilita la transmision.</summary>
    Task HabilitarTxAsync(bool habilitar, CancellationToken ct = default);

    /// <summary>Activa o desactiva el auto-sequencing.</summary>
    Task HabilitarAutoSecuenciaAsync(bool habilitar, CancellationToken ct = default);

    /// <summary>Inicia un CQ.</summary>
    Task LlamarCqAsync(CancellationToken ct = default);

    /// <summary>Selecciona una estacion DX para iniciar QSO.</summary>
    Task SeleccionarEstacionAsync(string indicativoDx, string? grid, int? reporte, CancellationToken ct = default);

    /// <summary>Procesa una decodificacion recibida para avanzar la secuencia.</summary>
    Task ProcesarDecodificacionAsync(string textoMensaje, string? indicativoEmisor, int snr, CancellationToken ct = default);

    /// <summary>Detiene la transmision inmediatamente.</summary>
    Task DetenerTxAsync(CancellationToken ct = default);

    /// <summary>Selecciona un mensaje TX especifico (Tx1-Tx6).</summary>
    Task SeleccionarMensajeTxAsync(int numeroTx, string? textoLibre = null, CancellationToken ct = default);
}
