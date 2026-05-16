using RadioAficionado.Dominio.Sdr;

namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Argumentos del evento de muestras IQ recibidas desde un dispositivo SDR.
/// Contiene las muestras en fase (I) y cuadratura (Q) junto con metadatos.
/// </summary>
public sealed class MuestrasSdrEventArgs : EventArgs
{
    /// <summary>
    /// Muestras de la componente en fase (In-phase).
    /// </summary>
    public required double[] MuestrasI { get; init; }

    /// <summary>
    /// Muestras de la componente en cuadratura (Quadrature).
    /// </summary>
    public required double[] MuestrasQ { get; init; }

    /// <summary>
    /// Frecuencia central en Hz a la que se capturaron las muestras.
    /// </summary>
    public double FrecuenciaCentralHz { get; init; }

    /// <summary>
    /// Marca de tiempo de la captura.
    /// </summary>
    public DateTimeOffset Marca { get; init; }
}

/// <summary>
/// Interfaz para el control de un receptor SDR (Software Defined Radio).
/// Permite conectar, configurar y recibir muestras IQ de dispositivos SDR
/// a través de la capa de abstracción SoapySDR.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Controla dispositivos SDR (RTL-SDR, HackRF, etc.) para recibir muestras IQ en bruto. Permite configurar frecuencia, ganancia, ancho de banda y tasa de muestreo.</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor. Se conecta con <see cref="ConectarAsync"/> y se suscribe a <see cref="MuestrasRecibidas"/> para procesar las muestras IQ.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Nativo.Sdr.ReceptorSoapySdr</c> (usa SoapySDR como abstracción).</para>
/// <para><b>Registro DI:</b> Registrada como Singleton en <c>RadioAficionado.Nativo.Sdr.ConfiguracionServiciosSdr.AgregarCapaDeSdr()</c>.</para>
/// <para><b>Configuración necesaria:</b> <c>ConfiguracionSdr</c> con frecuencia central, tasa de muestreo, ancho de banda y ganancia. Requiere drivers del dispositivo SDR instalados en el sistema.</para>
/// <para><b>Dependencias:</b> SoapySDR (librería nativa), <c>ConfiguracionSdr</c>. No depende de otras interfaces de dominio.</para>
/// </remarks>
public interface IReceptorSdr : IDisposable
{
    /// <summary>
    /// Indica si el receptor está conectado a un dispositivo SDR.
    /// </summary>
    bool EstaConectado { get; }

    /// <summary>
    /// Frecuencia central actual de sintonización en Hz.
    /// </summary>
    double FrecuenciaCentralHz { get; }

    /// <summary>
    /// Ancho de banda del filtro analógico en Hz.
    /// </summary>
    double AnchoDeBandaHz { get; }

    /// <summary>
    /// Ganancia actual del LNA en dB.
    /// </summary>
    double GananciaDb { get; }

    /// <summary>
    /// Tasa de muestreo actual en Hz.
    /// </summary>
    double TasaDeMuestreoHz { get; }

    /// <summary>
    /// Nombre o identificador del dispositivo SDR actualmente conectado. Null si no hay conexión.
    /// </summary>
    string? DispositivoActual { get; }

    /// <summary>
    /// Evento disparado cada vez que se recibe un bloque de muestras IQ del dispositivo.
    /// </summary>
    event EventHandler<MuestrasSdrEventArgs>? MuestrasRecibidas;

    /// <summary>
    /// Conecta al dispositivo SDR especificado e inicia la recepción de muestras.
    /// </summary>
    /// <param name="dispositivo">Identificador del dispositivo (ej: "driver=rtlsdr").</param>
    /// <param name="ct">Token de cancelación.</param>
    Task ConectarAsync(string dispositivo, CancellationToken ct = default);

    /// <summary>
    /// Desconecta del dispositivo SDR y detiene la recepción de muestras.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    Task DesconectarAsync(CancellationToken ct = default);

    /// <summary>
    /// Configura la frecuencia central de sintonización.
    /// </summary>
    /// <param name="frecuenciaHz">Frecuencia central en Hz.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task ConfigurarFrecuenciaAsync(double frecuenciaHz, CancellationToken ct = default);

    /// <summary>
    /// Configura la ganancia del LNA.
    /// </summary>
    /// <param name="gananciaDb">Ganancia en dB.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task ConfigurarGananciaAsync(double gananciaDb, CancellationToken ct = default);

    /// <summary>
    /// Configura el ancho de banda del filtro analógico.
    /// </summary>
    /// <param name="anchoBandaHz">Ancho de banda en Hz.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task ConfigurarAnchoDeBandaAsync(double anchoBandaHz, CancellationToken ct = default);

    /// <summary>
    /// Obtiene la lista de dispositivos SDR disponibles en el sistema.
    /// </summary>
    /// <returns>Lista de dispositivos SDR detectados.</returns>
    IReadOnlyList<DispositivoSdr> ObtenerDispositivosDisponibles();
}
