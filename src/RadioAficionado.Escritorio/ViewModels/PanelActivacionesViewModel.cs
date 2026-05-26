using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Activaciones;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// Elemento visual que representa una activación en el historial.
/// </summary>
public sealed class ActivacionVm
{
    /// <summary>Identificador único de la activación.</summary>
    public Guid Id { get; init; }

    /// <summary>Tipo de programa (POTA, SOTA, WWFF, IOTA).</summary>
    public string Tipo { get; init; } = string.Empty;

    /// <summary>Referencia del lugar activado.</summary>
    public string Referencia { get; init; } = string.Empty;

    /// <summary>Fecha de inicio formateada.</summary>
    public string Fecha { get; init; } = string.Empty;

    /// <summary>Cantidad de QSOs realizados durante la activación.</summary>
    public int QsoCount { get; init; }

    /// <summary>Estado actual de la activación.</summary>
    public string Estado { get; init; } = string.Empty;

    /// <summary>Indicativo del activador.</summary>
    public string Indicativo { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel del panel de activaciones POTA/SOTA/WWFF/IOTA.
/// Permite crear, iniciar, completar y cancelar activaciones de radio,
/// mostrando la activación en curso y el historial de activaciones pasadas.
/// </summary>
public partial class PanelActivacionesViewModel : ViewModelBase
{
    private readonly IServicioActivaciones _servicioActivaciones;
    private readonly ILogger<PanelActivacionesViewModel> _logger;
    private readonly Stopwatch _cronometro = new();
    private IDisposable? _temporizador;

    /// <summary>
    /// Historial de activaciones pasadas y planificadas.
    /// </summary>
    public ObservableCollection<ActivacionVm> Historial { get; } = [];

    /// <summary>
    /// Tipos de activación disponibles para seleccionar.
    /// </summary>
    public ObservableCollection<string> TiposDisponibles { get; } =
    [
        "POTA",
        "SOTA",
        "WWFF",
        "IOTA"
    ];

    /// <summary>
    /// Tipo de activación seleccionado actualmente (POTA, SOTA, WWFF, IOTA).
    /// </summary>
    [ObservableProperty]
    private string _tipoSeleccionado = "POTA";

    /// <summary>
    /// Referencia ingresada por el usuario para la nueva activación.
    /// </summary>
    [ObservableProperty]
    private string _referenciaIngresada = string.Empty;

    /// <summary>
    /// Indica si hay una activación en curso actualmente.
    /// </summary>
    [ObservableProperty]
    private bool _hayActivacionEnCurso;

    /// <summary>
    /// Referencia de la activación en curso.
    /// </summary>
    [ObservableProperty]
    private string _referenciaEnCurso = string.Empty;

    /// <summary>
    /// Tipo de la activación en curso.
    /// </summary>
    [ObservableProperty]
    private string _tipoEnCurso = string.Empty;

    /// <summary>
    /// Estado de la activación en curso.
    /// </summary>
    [ObservableProperty]
    private string _estadoEnCurso = string.Empty;

    /// <summary>
    /// Total de QSOs realizados en la activación en curso.
    /// </summary>
    [ObservableProperty]
    private int _totalQsosEnActivacion;

    /// <summary>
    /// Tiempo transcurrido desde el inicio de la activación en curso.
    /// </summary>
    [ObservableProperty]
    private string _tiempoTranscurrido = "00:00:00";

    /// <summary>
    /// Minutos para la alarma del cronometro (0 = sin alarma).
    /// </summary>
    [ObservableProperty]
    private int _minutosAlarma = 10;

    /// <summary>
    /// Indica si la alarma ya se disparo en esta activacion.
    /// Se resetea al iniciar una nueva activacion.
    /// </summary>
    private bool _alarmaDisparada;

    /// <summary>
    /// Texto de la alarma mostrado cuando se dispara.
    /// </summary>
    [ObservableProperty]
    private string _textoAlarma = string.Empty;

    /// <summary>
    /// Indica si la alarma esta visible en la UI.
    /// </summary>
    [ObservableProperty]
    private bool _alarmaVisible;

    /// <summary>
    /// Minutos disponibles para configurar la alarma del cronometro.
    /// </summary>
    public ObservableCollection<int> MinutosAlarmaDisponibles { get; } =
    [
        0, 5, 10, 15, 20, 30, 45, 60
    ];

    /// <summary>
    /// Identificador de la activación en curso (para comandos).
    /// </summary>
    [ObservableProperty]
    private Guid? _idActivacionEnCurso;

    /// <summary>
    /// Indica si hay una activación planificada pendiente de iniciar.
    /// </summary>
    [ObservableProperty]
    private bool _hayActivacionPlanificada;

    /// <summary>
    /// Identificador de la activación planificada.
    /// </summary>
    [ObservableProperty]
    private Guid? _idActivacionPlanificada;

    /// <summary>
    /// Texto de estado para la barra inferior del panel.
    /// </summary>
    [ObservableProperty]
    private string _textoEstado = "Sin activación en curso";

    /// <summary>
    /// Indica si se está procesando una operación (para deshabilitar botones).
    /// </summary>
    [ObservableProperty]
    private bool _procesando;

    /// <summary>
    /// Crea el ViewModel del panel de activaciones con las dependencias inyectadas.
    /// </summary>
    /// <param name="servicioActivaciones">Servicio de dominio para gestionar activaciones.</param>
    /// <param name="logger">Logger para registro de eventos.</param>
    public PanelActivacionesViewModel(
        IServicioActivaciones servicioActivaciones,
        ILogger<PanelActivacionesViewModel> logger)
    {
        _servicioActivaciones = servicioActivaciones ?? throw new ArgumentNullException(nameof(servicioActivaciones));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _ = CargarDatosInicialesAsync();
    }

    /// <summary>
    /// Crea una nueva activación con el tipo y referencia seleccionados.
    /// La activación se crea en estado Planificada.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand(CanExecute = nameof(PuedeCrearActivacion))]
    private async Task CrearActivacionAsync(CancellationToken ct)
    {
        try
        {
            Procesando = true;
            TextoEstado = "Creando activación...";

            TipoActivacion tipo = ConvertirTipoActivacion(TipoSeleccionado);

            // Se usa un indicativo temporal; en producción se obtendría de la configuración del usuario
            Indicativo indicativo = new Indicativo("TEMP");

            Activacion activacion = await _servicioActivaciones.CrearActivacionAsync(
                tipo,
                ReferenciaIngresada,
                indicativo,
                ct: ct);

            _logger.LogInformation(
                "Activación creada: {Tipo} {Referencia} (Id: {Id})",
                TipoSeleccionado, ReferenciaIngresada, activacion.Id);

            IdActivacionPlanificada = activacion.Id;
            HayActivacionPlanificada = true;

            ReferenciaIngresada = string.Empty;

            TextoEstado = $"Activación {activacion.Referencia} creada — lista para iniciar";

            await CargarHistorialAsync(ct);
            await CargarActivacionActualAsync(ct);
        }
        catch (ArgumentException ex)
        {
            TextoEstado = $"Error de formato: {ex.Message}";
            _logger.LogWarning(ex, "Error de formato al crear activación");
        }
        catch (InvalidOperationException ex)
        {
            TextoEstado = $"Error: {ex.Message}";
            _logger.LogWarning(ex, "Error de operación al crear activación");
        }
        catch (OperationCanceledException)
        {
            TextoEstado = "Operación cancelada";
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error inesperado: {ex.Message}";
            _logger.LogError(ex, "Error inesperado al crear activación");
        }
        finally
        {
            Procesando = false;
        }
    }

    /// <summary>
    /// Inicia la activación planificada, cambiando su estado a EnCurso.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand(CanExecute = nameof(PuedeIniciarActivacion))]
    private async Task IniciarActivacionAsync(CancellationToken ct)
    {
        if (IdActivacionPlanificada is null && IdActivacionEnCurso is null)
        {
            return;
        }

        Guid idActivacion = IdActivacionPlanificada ?? IdActivacionEnCurso!.Value;

        try
        {
            Procesando = true;
            TextoEstado = "Iniciando activación...";

            Activacion activacion = await _servicioActivaciones.IniciarAsync(idActivacion, ct);

            _logger.LogInformation(
                "Activación {Id} ({Referencia}) iniciada",
                activacion.Id, activacion.Referencia);

            ActualizarActivacionEnCurso(activacion);
            HayActivacionPlanificada = false;
            IdActivacionPlanificada = null;

            IniciarCronometro();

            TextoEstado = $"Activación {activacion.Referencia} en curso";

            await CargarHistorialAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            TextoEstado = $"Error: {ex.Message}";
            _logger.LogWarning(ex, "Error al iniciar activación {Id}", idActivacion);
        }
        catch (OperationCanceledException)
        {
            TextoEstado = "Operación cancelada";
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error inesperado: {ex.Message}";
            _logger.LogError(ex, "Error inesperado al iniciar activación");
        }
        finally
        {
            Procesando = false;
        }
    }

    /// <summary>
    /// Completa la activación en curso, cambiando su estado a Completada.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand(CanExecute = nameof(PuedeCompletarActivacion))]
    private async Task CompletarActivacionAsync(CancellationToken ct)
    {
        if (IdActivacionEnCurso is null)
        {
            return;
        }

        try
        {
            Procesando = true;
            TextoEstado = "Completando activación...";

            Activacion activacion = await _servicioActivaciones.CompletarAsync(
                IdActivacionEnCurso.Value, ct);

            _logger.LogInformation(
                "Activación {Id} ({Referencia}) completada con {Qsos} QSOs",
                activacion.Id, activacion.Referencia, activacion.Qsos.Count);

            DetenerCronometro();
            LimpiarActivacionEnCurso();

            TextoEstado = $"Activación {activacion.Referencia} completada con {activacion.Qsos.Count} QSOs";

            await CargarHistorialAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            TextoEstado = $"Error: {ex.Message}";
            _logger.LogWarning(ex, "Error al completar activación");
        }
        catch (OperationCanceledException)
        {
            TextoEstado = "Operación cancelada";
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error inesperado: {ex.Message}";
            _logger.LogError(ex, "Error inesperado al completar activación");
        }
        finally
        {
            Procesando = false;
        }
    }

    /// <summary>
    /// Cancela la activación en curso o planificada.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand(CanExecute = nameof(PuedeCancelarActivacion))]
    private async Task CancelarActivacionAsync(CancellationToken ct)
    {
        Guid? idActivacion = IdActivacionEnCurso ?? IdActivacionPlanificada;

        if (idActivacion is null)
        {
            return;
        }

        try
        {
            Procesando = true;
            TextoEstado = "Cancelando activación...";

            Activacion activacion = await _servicioActivaciones.CancelarAsync(
                idActivacion.Value, ct);

            _logger.LogInformation(
                "Activación {Id} ({Referencia}) cancelada",
                activacion.Id, activacion.Referencia);

            DetenerCronometro();
            LimpiarActivacionEnCurso();
            HayActivacionPlanificada = false;
            IdActivacionPlanificada = null;

            TextoEstado = $"Activación {activacion.Referencia} cancelada";

            await CargarHistorialAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            TextoEstado = $"Error: {ex.Message}";
            _logger.LogWarning(ex, "Error al cancelar activación");
        }
        catch (OperationCanceledException)
        {
            TextoEstado = "Operación cancelada";
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error inesperado: {ex.Message}";
            _logger.LogError(ex, "Error inesperado al cancelar activación");
        }
        finally
        {
            Procesando = false;
        }
    }

    /// <summary>
    /// Determina si se puede crear una nueva activación.
    /// </summary>
    private bool PuedeCrearActivacion =>
        !Procesando && !string.IsNullOrWhiteSpace(ReferenciaIngresada);

    /// <summary>
    /// Determina si se puede iniciar una activación planificada.
    /// </summary>
    private bool PuedeIniciarActivacion =>
        !Procesando && (HayActivacionPlanificada || (HayActivacionEnCurso && EstadoEnCurso == "Planificada"));

    /// <summary>
    /// Determina si se puede completar la activación en curso.
    /// </summary>
    private bool PuedeCompletarActivacion =>
        !Procesando && HayActivacionEnCurso && EstadoEnCurso == "EnCurso";

    /// <summary>
    /// Determina si se puede cancelar la activación actual.
    /// </summary>
    private bool PuedeCancelarActivacion =>
        !Procesando && (HayActivacionEnCurso || HayActivacionPlanificada);

    partial void OnProcesandoChanged(bool value)
    {
        NotificarCambiosDeComandos();
    }

    partial void OnReferenciaIngresadaChanged(string value)
    {
        CrearActivacionCommand.NotifyCanExecuteChanged();
    }

    partial void OnHayActivacionEnCursoChanged(bool value)
    {
        NotificarCambiosDeComandos();
    }

    partial void OnHayActivacionPlanificadaChanged(bool value)
    {
        NotificarCambiosDeComandos();
    }

    partial void OnEstadoEnCursoChanged(string value)
    {
        NotificarCambiosDeComandos();
    }

    /// <summary>
    /// Notifica a todos los comandos que las condiciones de ejecución pueden haber cambiado.
    /// </summary>
    private void NotificarCambiosDeComandos()
    {
        CrearActivacionCommand.NotifyCanExecuteChanged();
        IniciarActivacionCommand.NotifyCanExecuteChanged();
        CompletarActivacionCommand.NotifyCanExecuteChanged();
        CancelarActivacionCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Carga los datos iniciales: activación en curso e historial.
    /// </summary>
    private async Task CargarDatosInicialesAsync()
    {
        try
        {
            await CargarActivacionActualAsync(CancellationToken.None);
            await CargarHistorialAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar datos iniciales de activaciones");
            TextoEstado = "Error al cargar datos iniciales";
        }
    }

    /// <summary>
    /// Carga la activación actualmente en curso desde el servicio.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    private async Task CargarActivacionActualAsync(CancellationToken ct)
    {
        Activacion? activacionActual = await _servicioActivaciones.ObtenerActivacionActualAsync(ct);

        if (activacionActual is not null)
        {
            ActualizarActivacionEnCurso(activacionActual);

            if (activacionActual.EstadoActivacion == EstadoActivacion.EnCurso)
            {
                IniciarCronometro();
            }
        }
    }

    /// <summary>
    /// Carga el historial de activaciones desde el servicio.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    private async Task CargarHistorialAsync(CancellationToken ct)
    {
        try
        {
            IReadOnlyList<Activacion> activaciones = await _servicioActivaciones.ObtenerTodasAsync(ct);

            Dispatcher.UIThread.Post(() =>
            {
                Historial.Clear();

                foreach (Activacion activacion in activaciones)
                {
                    Historial.Add(ConvertirAVm(activacion));
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar historial de activaciones");
        }
    }

    /// <summary>
    /// Actualiza las propiedades de la activación en curso a partir de la entidad de dominio.
    /// </summary>
    /// <param name="activacion">Activación del dominio.</param>
    private void ActualizarActivacionEnCurso(Activacion activacion)
    {
        IdActivacionEnCurso = activacion.Id;
        ReferenciaEnCurso = activacion.Referencia;
        TipoEnCurso = activacion.TipoActivacion.ToString();
        EstadoEnCurso = activacion.EstadoActivacion.ToString();
        TotalQsosEnActivacion = activacion.Qsos.Count;
        HayActivacionEnCurso = true;
    }

    /// <summary>
    /// Limpia las propiedades de la activación en curso.
    /// </summary>
    private void LimpiarActivacionEnCurso()
    {
        IdActivacionEnCurso = null;
        ReferenciaEnCurso = string.Empty;
        TipoEnCurso = string.Empty;
        EstadoEnCurso = string.Empty;
        TotalQsosEnActivacion = 0;
        TiempoTranscurrido = "00:00:00";
        HayActivacionEnCurso = false;
    }

    /// <summary>
    /// Inicia el cronómetro de la activación en curso.
    /// </summary>
    private void IniciarCronometro()
    {
        _cronometro.Restart();
        _alarmaDisparada = false;
        AlarmaVisible = false;
        TextoAlarma = string.Empty;

        _temporizador = DispatcherTimer.Run(() =>
        {
            TiempoTranscurrido = _cronometro.Elapsed.ToString(@"hh\:mm\:ss");
            EvaluarAlarma();
            return true;
        }, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Evalua si se ha alcanzado el tiempo configurado para la alarma.
    /// </summary>
    private void EvaluarAlarma()
    {
        if (_alarmaDisparada || MinutosAlarma <= 0)
        {
            return;
        }

        if (_cronometro.Elapsed.TotalMinutes >= MinutosAlarma)
        {
            _alarmaDisparada = true;
            TextoAlarma = $"Alarma: {MinutosAlarma} minutos alcanzados";
            AlarmaVisible = true;
            _logger.LogInformation(
                "Alarma de activacion disparada: {Minutos} minutos alcanzados",
                MinutosAlarma);
        }
    }

    /// <summary>
    /// Descarta la alarma visible.
    /// </summary>
    [RelayCommand]
    private void DescartarAlarma()
    {
        AlarmaVisible = false;
        TextoAlarma = string.Empty;
    }

    /// <summary>
    /// Detiene el cronómetro de la activación.
    /// </summary>
    private void DetenerCronometro()
    {
        _cronometro.Stop();
        _temporizador?.Dispose();
        _temporizador = null;
    }

    /// <summary>
    /// Convierte una entidad de dominio Activacion a un ActivacionVm para la vista.
    /// </summary>
    /// <param name="activacion">Entidad de dominio.</param>
    /// <returns>Objeto visual para la lista.</returns>
    private static ActivacionVm ConvertirAVm(Activacion activacion)
    {
        return new ActivacionVm
        {
            Id = activacion.Id,
            Tipo = activacion.TipoActivacion.ToString(),
            Referencia = activacion.Referencia,
            Fecha = activacion.FechaInicio.ToString("yyyy-MM-dd HH:mm"),
            QsoCount = activacion.Qsos.Count,
            Estado = activacion.EstadoActivacion.ToString(),
            Indicativo = activacion.IndicativoActivador.Valor
        };
    }

    /// <summary>
    /// Convierte el texto del tipo seleccionado al enum TipoActivacion del dominio.
    /// </summary>
    /// <param name="tipo">Texto del tipo de activación.</param>
    /// <returns>Valor del enum correspondiente.</returns>
    /// <exception cref="ArgumentException">Si el tipo no es reconocido.</exception>
    private static TipoActivacion ConvertirTipoActivacion(string tipo)
    {
        return tipo.ToUpperInvariant() switch
        {
            "POTA" => TipoActivacion.Pota,
            "SOTA" => TipoActivacion.Sota,
            "WWFF" => TipoActivacion.Wwff,
            "IOTA" => TipoActivacion.Iota,
            _ => throw new ArgumentException($"Tipo de activación no reconocido: '{tipo}'.", nameof(tipo))
        };
    }
}
