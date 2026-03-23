using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Contests;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.Contests;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel de un QSO mostrado en la lista del contest activo.
/// </summary>
public partial class QsoContestVm : ViewModelBase
{
    /// <summary>
    /// Hora UTC del contacto.
    /// </summary>
    [ObservableProperty]
    private string _hora = string.Empty;

    /// <summary>
    /// Indicativo de la estación contactada.
    /// </summary>
    [ObservableProperty]
    private string _indicativo = string.Empty;

    /// <summary>
    /// Banda del contacto.
    /// </summary>
    [ObservableProperty]
    private string _banda = string.Empty;

    /// <summary>
    /// Modo de operación del contacto.
    /// </summary>
    [ObservableProperty]
    private string _modo = string.Empty;

    /// <summary>
    /// Señal enviada (RST).
    /// </summary>
    [ObservableProperty]
    private string _senalEnviada = string.Empty;

    /// <summary>
    /// Señal recibida (RST).
    /// </summary>
    [ObservableProperty]
    private string _senalRecibida = string.Empty;

    /// <summary>
    /// Puntos aportados por este QSO.
    /// </summary>
    [ObservableProperty]
    private int _puntos;
}

/// <summary>
/// ViewModel que gestiona la participación en concursos de radioaficionado.
/// Muestra puntuación en tiempo real, lista de QSOs del contest activo
/// y permite iniciar, finalizar y exportar resultados en formato Cabrillo.
/// </summary>
public partial class PanelContestViewModel : ViewModelBase
{
    private readonly IRepositorioQso _repositorioQso;
    private readonly ILogger<PanelContestViewModel> _logger;
    private readonly MotorContest _motorContest;
    private readonly GeneradorCabrillo _generadorCabrillo;

    private ReglaContest? _reglaActiva;
    private DateTimeOffset _inicioContest;

    /// <summary>
    /// Lista de contests disponibles con sus reglas.
    /// </summary>
    public ObservableCollection<ReglaContest> ContestsDisponibles { get; } = new();

    /// <summary>
    /// QSOs registrados durante el contest activo.
    /// </summary>
    public ObservableCollection<QsoContestVm> QsosDelContest { get; } = new();

    /// <summary>
    /// Contest seleccionado en el ComboBox.
    /// </summary>
    [ObservableProperty]
    private ReglaContest? _contestSeleccionado;

    /// <summary>
    /// Indica si hay un contest activo en ejecución.
    /// </summary>
    [ObservableProperty]
    private bool _contestActivo;

    /// <summary>
    /// Nombre del contest activo.
    /// </summary>
    [ObservableProperty]
    private string _nombreContestActivo = "Ninguno";

    /// <summary>
    /// Número de QSOs válidos en el contest activo.
    /// </summary>
    [ObservableProperty]
    private int _qsosValidos;

    /// <summary>
    /// Puntos brutos acumulados.
    /// </summary>
    [ObservableProperty]
    private int _puntos;

    /// <summary>
    /// Multiplicadores únicos obtenidos.
    /// </summary>
    [ObservableProperty]
    private int _multiplicadores;

    /// <summary>
    /// Puntuación final (Puntos × Multiplicadores).
    /// </summary>
    [ObservableProperty]
    private long _puntuacionFinal;

    /// <summary>
    /// Tasa de QSOs por hora (últimos 60 minutos).
    /// </summary>
    [ObservableProperty]
    private double _tasaQsosPorHora;

    /// <summary>
    /// QSOs duplicados detectados.
    /// </summary>
    [ObservableProperty]
    private int _qsosDuplicados;

    /// <summary>
    /// QSOs inválidos (banda/modo no permitido).
    /// </summary>
    [ObservableProperty]
    private int _qsosInvalidos;

    /// <summary>
    /// Mensaje de estado para el usuario.
    /// </summary>
    [ObservableProperty]
    private string _mensajeEstado = string.Empty;

    /// <summary>
    /// Crea el ViewModel del panel de contest.
    /// </summary>
    /// <param name="repositorioQso">Repositorio de QSOs para obtener los contactos.</param>
    /// <param name="logger">Logger para registrar eventos y errores.</param>
    public PanelContestViewModel(
        IRepositorioQso repositorioQso,
        ILogger<PanelContestViewModel> logger)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _motorContest = new MotorContest();
        _generadorCabrillo = new GeneradorCabrillo();

        CargarContestsDisponibles();
    }

    /// <summary>
    /// Constructor sin parámetros para el diseñador de Avalonia.
    /// </summary>
    public PanelContestViewModel()
    {
        _repositorioQso = null!;
        _logger = null!;
        _motorContest = new MotorContest();
        _generadorCabrillo = new GeneradorCabrillo();
    }

    /// <summary>
    /// Inicia el contest seleccionado y carga los QSOs existentes.
    /// </summary>
    [RelayCommand(CanExecute = nameof(PuedeIniciarContest))]
    private async Task IniciarContestAsync()
    {
        if (ContestSeleccionado is null)
        {
            return;
        }

        try
        {
            _reglaActiva = ContestSeleccionado;
            _inicioContest = DateTimeOffset.UtcNow;
            ContestActivo = true;
            NombreContestActivo = _reglaActiva.Nombre;
            MensajeEstado = $"Contest iniciado: {_reglaActiva.Nombre}";

            _logger.LogInformation("Contest iniciado: {NombreContest} ({TipoContest})",
                _reglaActiva.Nombre, _reglaActiva.Tipo);

            await ActualizarPuntuacionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar el contest {NombreContest}", ContestSeleccionado.Nombre);
            MensajeEstado = $"Error al iniciar: {ex.Message}";
        }
    }

    /// <summary>
    /// Finaliza el contest activo.
    /// </summary>
    [RelayCommand(CanExecute = nameof(ContestActivo))]
    private Task FinalizarContestAsync()
    {
        ContestActivo = false;
        MensajeEstado = $"Contest finalizado: {NombreContestActivo} — Puntuación: {PuntuacionFinal:N0}";

        _logger.LogInformation("Contest finalizado: {NombreContest}. Puntuación final: {Puntuacion}",
            NombreContestActivo, PuntuacionFinal);

        NombreContestActivo = "Ninguno";
        _reglaActiva = null;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Exporta el log del contest activo en formato Cabrillo 3.0.
    /// </summary>
    [RelayCommand(CanExecute = nameof(TieneQsosParaExportar))]
    private async Task ExportarCabrilloAsync()
    {
        if (_reglaActiva is null && ContestSeleccionado is null)
        {
            MensajeEstado = "Seleccione un contest antes de exportar.";
            return;
        }

        try
        {
            ReglaContest regla = _reglaActiva ?? ContestSeleccionado!;
            IReadOnlyList<Qso> qsos = await _repositorioQso.ObtenerTodosAsync(CancellationToken.None);

            // Configuración por defecto para la exportación
            ConfiguracionContest configuracion = new(
                Indicativo: new Indicativo("MI_INDICATIVO"),
                CategoriaOperador: "SINGLE-OP",
                CategoriaBanda: "ALL",
                CategoriaModo: "MIXED",
                CategoriaPotencia: "HIGH",
                NombreOperador: "Operador");

            string contenidoCabrillo = _generadorCabrillo.GenerarCabrillo(qsos, regla, configuracion);

            string nombreArchivo = $"{regla.Abreviatura}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.cbr";
            string rutaArchivo = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                nombreArchivo);

            await File.WriteAllTextAsync(rutaArchivo, contenidoCabrillo);

            MensajeEstado = $"Cabrillo exportado: {rutaArchivo}";
            _logger.LogInformation("Archivo Cabrillo exportado: {Ruta}", rutaArchivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar Cabrillo");
            MensajeEstado = $"Error al exportar: {ex.Message}";
        }
    }

    /// <summary>
    /// Actualiza la puntuación en tiempo real con los QSOs actuales.
    /// Debe llamarse cada vez que se agrega un nuevo QSO.
    /// </summary>
    public async Task ActualizarPuntuacionAsync()
    {
        if (_reglaActiva is null)
        {
            return;
        }

        try
        {
            IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(CancellationToken.None);

            ResultadoContest resultado = _motorContest.CalcularPuntuacion(todosLosQsos, _reglaActiva);

            QsosValidos = resultado.QsosValidos;
            Puntos = resultado.Puntos;
            Multiplicadores = resultado.Multiplicadores;
            PuntuacionFinal = resultado.PuntuacionFinal;
            QsosDuplicados = resultado.QsosDuplicados;
            QsosInvalidos = resultado.QsosInvalidos;

            TasaQsosPorHora = _motorContest.ObtenerTasaQsos(todosLosQsos, TimeSpan.FromHours(1));

            ActualizarListaQsos(todosLosQsos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar puntuación del contest");
        }
    }

    /// <summary>
    /// Notifica al panel de contest que se ha agregado un nuevo QSO.
    /// </summary>
    public async Task NotificarNuevoQsoAsync()
    {
        if (ContestActivo)
        {
            await ActualizarPuntuacionAsync();
        }
    }

    private bool PuedeIniciarContest()
    {
        return ContestSeleccionado is not null && !ContestActivo;
    }

    private bool TieneQsosParaExportar()
    {
        return QsosDelContest.Count > 0;
    }

    private void CargarContestsDisponibles()
    {
        foreach (KeyValuePair<TipoContest, ReglaContest> par in MotorContest.RegistroDeReglas)
        {
            ContestsDisponibles.Add(par.Value);
        }
    }

    private void ActualizarListaQsos(IReadOnlyList<Qso> qsos)
    {
        QsosDelContest.Clear();

        foreach (Qso qso in qsos)
        {
            BandaRadio? banda = qso.Frecuencia.ObtenerBanda();

            QsoContestVm vm = new()
            {
                Hora = qso.FechaHoraInicio.UtcDateTime.ToString("HH:mm:ss"),
                Indicativo = qso.IndicativoContacto.Valor,
                Banda = banda?.ToString() ?? "—",
                Modo = qso.Modo.ToString(),
                SenalEnviada = qso.SenalEnviada,
                SenalRecibida = !string.IsNullOrWhiteSpace(qso.SenalRecibida) ? qso.SenalRecibida : "—",
                Puntos = qso.Modo switch
                {
                    ModoOperacion.CW => 2,
                    ModoOperacion.RTTY => 2,
                    ModoOperacion.FT8 => 2,
                    ModoOperacion.FT4 => 2,
                    _ => 1
                }
            };

            QsosDelContest.Add(vm);
        }

        IniciarContestCommand.NotifyCanExecuteChanged();
        FinalizarContestCommand.NotifyCanExecuteChanged();
        ExportarCabrilloCommand.NotifyCanExecuteChanged();
    }

    partial void OnContestSeleccionadoChanged(ReglaContest? value)
    {
        IniciarContestCommand.NotifyCanExecuteChanged();
    }

    partial void OnContestActivoChanged(bool value)
    {
        IniciarContestCommand.NotifyCanExecuteChanged();
        FinalizarContestCommand.NotifyCanExecuteChanged();
    }
}
