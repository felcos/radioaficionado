using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para una entidad DXCC mostrada en el panel de tracking.
/// Contiene propiedades formateadas para presentación visual con indicadores de estado.
/// </summary>
public partial class EntidadDxccVm : ViewModelBase
{
    /// <summary>
    /// Número identificador DXCC único.
    /// </summary>
    public int Numero { get; init; }

    /// <summary>
    /// Nombre oficial de la entidad DXCC.
    /// </summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Prefijo principal asignado a la entidad.
    /// </summary>
    public string Prefijo { get; init; } = string.Empty;

    /// <summary>
    /// Código de continente (AF, AS, EU, NA, OC, SA).
    /// </summary>
    public string Continente { get; init; } = string.Empty;

    /// <summary>
    /// Indica si la entidad ha sido trabajada (al menos un QSO registrado).
    /// </summary>
    [ObservableProperty]
    private bool _trabajada;

    /// <summary>
    /// Indica si la entidad ha sido confirmada (QSO verificado por servicio externo).
    /// </summary>
    [ObservableProperty]
    private bool _confirmada;

    /// <summary>
    /// Resumen de las bandas en las que se ha trabajado esta entidad.
    /// </summary>
    [ObservableProperty]
    private string _bandasTrabajadas = string.Empty;

    /// <summary>
    /// Color indicador del estado: verde=confirmada, amarillo=trabajada, gris=no trabajada.
    /// </summary>
    public string ColorEstado
    {
        get
        {
            if (Confirmada) return "#4caf50";
            if (Trabajada) return "#ffc107";
            return "#555555";
        }
    }

    /// <summary>
    /// Texto descriptivo del estado de la entidad.
    /// </summary>
    public string TextoEstado
    {
        get
        {
            if (Confirmada) return "Confirmada";
            if (Trabajada) return "Trabajada";
            return "No trabajada";
        }
    }

    /// <inheritdoc/>
    partial void OnTrabajadaChanged(bool value)
    {
        OnPropertyChanged(nameof(ColorEstado));
        OnPropertyChanged(nameof(TextoEstado));
    }

    /// <inheritdoc/>
    partial void OnConfirmadaChanged(bool value)
    {
        OnPropertyChanged(nameof(ColorEstado));
        OnPropertyChanged(nameof(TextoEstado));
    }
}

/// <summary>
/// Resumen de estadísticas DXCC por continente para mostrar en el panel.
/// </summary>
public partial class ResumenContinenteVm : ViewModelBase
{
    /// <summary>
    /// Código del continente (AF, AS, EU, NA, OC, SA).
    /// </summary>
    public string Continente { get; init; } = string.Empty;

    /// <summary>
    /// Total de entidades DXCC en este continente.
    /// </summary>
    public int TotalEntidades { get; init; }

    /// <summary>
    /// Número de entidades trabajadas en este continente.
    /// </summary>
    [ObservableProperty]
    private int _trabajadas;

    /// <summary>
    /// Número de entidades confirmadas en este continente.
    /// </summary>
    [ObservableProperty]
    private int _confirmadas;

    /// <summary>
    /// Porcentaje de entidades trabajadas sobre el total del continente.
    /// </summary>
    public string PorcentajeTrabajadas =>
        TotalEntidades > 0 ? $"{(double)Trabajadas / TotalEntidades * 100:F0}%" : "0%";
}

/// <summary>
/// Resumen de estadísticas DXCC por banda para mostrar en el panel.
/// </summary>
public partial class ResumenBandaVm : ViewModelBase
{
    /// <summary>
    /// Nombre legible de la banda (ej: "20 metros").
    /// </summary>
    public string NombreBanda { get; init; } = string.Empty;

    /// <summary>
    /// Número de entidades DXCC trabajadas en esta banda.
    /// </summary>
    [ObservableProperty]
    private int _entidadesTrabajadas;
}

/// <summary>
/// Opciones de filtro por estado de entidad DXCC.
/// </summary>
public enum FiltroEstadoDxcc
{
    /// <summary>Mostrar todas las entidades.</summary>
    Todas,

    /// <summary>Mostrar solo las entidades trabajadas.</summary>
    Trabajadas,

    /// <summary>Mostrar solo las entidades no trabajadas.</summary>
    NoTrabajadas,

    /// <summary>Mostrar solo las entidades confirmadas.</summary>
    Confirmadas
}

/// <summary>
/// ViewModel del panel de tracking DXCC.
/// Muestra estadísticas generales, filtros por continente/estado, tabla de entidades
/// con indicadores visuales y resúmenes por continente y banda.
/// </summary>
public partial class PanelDxccViewModel : ViewModelBase
{
    private readonly IRepositorioQso _repositorioQso;
    private readonly ILogger<PanelDxccViewModel> _logger;
    private readonly EstadisticasDxcc _estadisticasDxcc;

    private List<EntidadDxccVm> _todasLasEntidades = new();

    /// <summary>
    /// Colección de entidades DXCC filtradas para mostrar en la tabla.
    /// </summary>
    public ObservableCollection<EntidadDxccVm> Entidades { get; } = new();

    /// <summary>
    /// Resúmenes agrupados por continente.
    /// </summary>
    public ObservableCollection<ResumenContinenteVm> ResumenesPorContinente { get; } = new();

    /// <summary>
    /// Resúmenes agrupados por banda.
    /// </summary>
    public ObservableCollection<ResumenBandaVm> ResumenesPorBanda { get; } = new();

    /// <summary>
    /// Opciones de continente disponibles para filtrar (incluye "Todos").
    /// </summary>
    public ObservableCollection<string> OpcionesContinente { get; } = new()
    {
        "Todos", "AF", "AS", "EU", "NA", "OC", "SA"
    };

    /// <summary>
    /// Opciones de estado disponibles para filtrar.
    /// </summary>
    public ObservableCollection<FiltroEstadoDxcc> OpcionesEstado { get; } = new(Enum.GetValues<FiltroEstadoDxcc>());

    /// <summary>
    /// Filtro de continente activo.
    /// </summary>
    [ObservableProperty]
    private string _filtroContinente = "Todos";

    /// <summary>
    /// Filtro de estado activo.
    /// </summary>
    [ObservableProperty]
    private FiltroEstadoDxcc _filtroEstado = FiltroEstadoDxcc.Todas;

    /// <summary>
    /// Total de entidades DXCC activas en el catálogo.
    /// </summary>
    [ObservableProperty]
    private int _totalEntidades;

    /// <summary>
    /// Total de entidades DXCC trabajadas.
    /// </summary>
    [ObservableProperty]
    private int _totalTrabajadas;

    /// <summary>
    /// Total de entidades DXCC confirmadas.
    /// </summary>
    [ObservableProperty]
    private int _totalConfirmadas;

    /// <summary>
    /// Porcentaje de entidades trabajadas sobre el total.
    /// </summary>
    [ObservableProperty]
    private string _porcentajeTrabajadas = "0%";

    /// <summary>
    /// Porcentaje de entidades confirmadas sobre el total.
    /// </summary>
    [ObservableProperty]
    private string _porcentajeConfirmadas = "0%";

    /// <summary>
    /// Indica si los datos se están cargando.
    /// </summary>
    [ObservableProperty]
    private bool _estaCargando;

    /// <summary>
    /// Mensaje de estado para el usuario.
    /// </summary>
    [ObservableProperty]
    private string _mensajeEstado = string.Empty;

    /// <summary>
    /// Crea el ViewModel del panel DXCC.
    /// </summary>
    /// <param name="repositorioQso">Repositorio de QSOs para obtener los contactos.</param>
    /// <param name="logger">Logger para registrar eventos y errores.</param>
    public PanelDxccViewModel(
        IRepositorioQso repositorioQso,
        ILogger<PanelDxccViewModel> logger)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _estadisticasDxcc = new EstadisticasDxcc();
    }

    /// <summary>
    /// Carga los datos DXCC desde el repositorio de QSOs y calcula todas las estadísticas.
    /// </summary>
    [RelayCommand]
    private async Task CargarDatosAsync(CancellationToken ct)
    {
        try
        {
            EstaCargando = true;
            MensajeEstado = "Cargando datos DXCC...";

            IReadOnlyList<Qso> qsos = await _repositorioQso.ObtenerTodosAsync(ct);
            IReadOnlyList<EntidadDxcc> entidadesActivas = CatalogoDxcc.ObtenerActivas();

            HashSet<int> trabajadas = _estadisticasDxcc.EntidadesTrabajadas(qsos);
            // Sin servicio de confirmaciones inyectado, por ahora las confirmadas quedan vacías
            HashSet<int> confirmadas = new();
            Dictionary<BandaRadio, HashSet<int>> porBanda = _estadisticasDxcc.PorBanda(qsos);

            // Construir mapa de bandas por entidad DXCC
            Dictionary<int, List<string>> bandasPorEntidad = new();
            foreach (KeyValuePair<BandaRadio, HashSet<int>> kvp in porBanda)
            {
                string nombreBanda = kvp.Key.ObtenerNombre();
                foreach (int numeroDxcc in kvp.Value)
                {
                    if (!bandasPorEntidad.ContainsKey(numeroDxcc))
                    {
                        bandasPorEntidad[numeroDxcc] = new List<string>();
                    }
                    bandasPorEntidad[numeroDxcc].Add(nombreBanda);
                }
            }

            // Crear ViewModels de entidades
            List<EntidadDxccVm> listaVm = new();
            foreach (EntidadDxcc entidad in entidadesActivas)
            {
                bool esTrabajada = trabajadas.Contains(entidad.Numero);
                bool esConfirmada = confirmadas.Contains(entidad.Numero);
                string bandas = bandasPorEntidad.TryGetValue(entidad.Numero, out List<string>? listaBandas)
                    ? string.Join(", ", listaBandas)
                    : string.Empty;

                EntidadDxccVm vm = new()
                {
                    Numero = entidad.Numero,
                    Nombre = entidad.Nombre,
                    Prefijo = entidad.Prefijo,
                    Continente = entidad.Continente,
                    Trabajada = esTrabajada,
                    Confirmada = esConfirmada,
                    BandasTrabajadas = bandas
                };

                listaVm.Add(vm);
            }

            _todasLasEntidades = listaVm;

            // Estadísticas generales
            TotalEntidades = entidadesActivas.Count;
            TotalTrabajadas = trabajadas.Count;
            TotalConfirmadas = confirmadas.Count;
            PorcentajeTrabajadas = TotalEntidades > 0
                ? $"{(double)TotalTrabajadas / TotalEntidades * 100:F1}%"
                : "0%";
            PorcentajeConfirmadas = TotalEntidades > 0
                ? $"{(double)TotalConfirmadas / TotalEntidades * 100:F1}%"
                : "0%";

            // Resúmenes por continente
            CalcularResumenesContinente(entidadesActivas, trabajadas, confirmadas);

            // Resúmenes por banda
            CalcularResumenesBanda(porBanda);

            // Aplicar filtros
            AplicarFiltros();

            MensajeEstado = $"Datos cargados: {TotalTrabajadas}/{TotalEntidades} trabajadas";
            _logger.LogInformation("Panel DXCC cargado: {Trabajadas}/{Total} trabajadas, {Confirmadas} confirmadas",
                TotalTrabajadas, TotalEntidades, TotalConfirmadas);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Carga de datos DXCC cancelada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar datos DXCC");
            MensajeEstado = $"Error al cargar datos: {ex.Message}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    /// <inheritdoc/>
    partial void OnFiltroContinenteChanged(string value)
    {
        AplicarFiltros();
    }

    /// <inheritdoc/>
    partial void OnFiltroEstadoChanged(FiltroEstadoDxcc value)
    {
        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        Entidades.Clear();

        IEnumerable<EntidadDxccVm> filtradas = _todasLasEntidades;

        // Filtro por continente
        if (!string.IsNullOrWhiteSpace(FiltroContinente) && FiltroContinente != "Todos")
        {
            filtradas = filtradas.Where(e => e.Continente == FiltroContinente);
        }

        // Filtro por estado
        filtradas = FiltroEstado switch
        {
            FiltroEstadoDxcc.Trabajadas => filtradas.Where(e => e.Trabajada),
            FiltroEstadoDxcc.NoTrabajadas => filtradas.Where(e => !e.Trabajada),
            FiltroEstadoDxcc.Confirmadas => filtradas.Where(e => e.Confirmada),
            _ => filtradas
        };

        foreach (EntidadDxccVm entidad in filtradas.OrderBy(e => e.Nombre))
        {
            Entidades.Add(entidad);
        }
    }

    private void CalcularResumenesContinente(
        IReadOnlyList<EntidadDxcc> entidadesActivas,
        HashSet<int> trabajadas,
        HashSet<int> confirmadas)
    {
        ResumenesPorContinente.Clear();

        Dictionary<string, List<EntidadDxcc>> porContinente = new();
        foreach (EntidadDxcc entidad in entidadesActivas)
        {
            if (!porContinente.ContainsKey(entidad.Continente))
            {
                porContinente[entidad.Continente] = new List<EntidadDxcc>();
            }
            porContinente[entidad.Continente].Add(entidad);
        }

        foreach (KeyValuePair<string, List<EntidadDxcc>> kvp in porContinente.OrderBy(x => x.Key))
        {
            int trabajadasCont = kvp.Value.Count(e => trabajadas.Contains(e.Numero));
            int confirmadasCont = kvp.Value.Count(e => confirmadas.Contains(e.Numero));

            ResumenesPorContinente.Add(new ResumenContinenteVm
            {
                Continente = kvp.Key,
                TotalEntidades = kvp.Value.Count,
                Trabajadas = trabajadasCont,
                Confirmadas = confirmadasCont
            });
        }
    }

    private void CalcularResumenesBanda(Dictionary<BandaRadio, HashSet<int>> porBanda)
    {
        ResumenesPorBanda.Clear();

        // Solo mostrar bandas HF principales (las más relevantes para DXCC)
        BandaRadio[] bandasHf =
        {
            BandaRadio.Banda160m, BandaRadio.Banda80m, BandaRadio.Banda40m,
            BandaRadio.Banda30m, BandaRadio.Banda20m, BandaRadio.Banda17m,
            BandaRadio.Banda15m, BandaRadio.Banda12m, BandaRadio.Banda10m,
            BandaRadio.Banda6m, BandaRadio.Banda2m
        };

        foreach (BandaRadio banda in bandasHf)
        {
            int cantidad = porBanda.TryGetValue(banda, out HashSet<int>? entidades)
                ? entidades.Count
                : 0;

            ResumenesPorBanda.Add(new ResumenBandaVm
            {
                NombreBanda = banda.ObtenerNombre(),
                EntidadesTrabajadas = cantidad
            });
        }
    }
}
