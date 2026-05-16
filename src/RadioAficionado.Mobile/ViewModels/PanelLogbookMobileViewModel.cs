using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Configuracion;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Mobile.ViewModels;

/// <summary>
/// Elemento visual que representa un QSO en la lista del logbook móvil.
/// </summary>
public partial class QsoMobileVm : ViewModelBase
{
    /// <summary>
    /// Identificador único del QSO original.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Fecha y hora formateada (UTC).
    /// </summary>
    [ObservableProperty]
    private string _fecha = string.Empty;

    /// <summary>
    /// Indicativo de la estación contactada.
    /// </summary>
    [ObservableProperty]
    private string _indicativo = string.Empty;

    /// <summary>
    /// Nombre de la banda (ej: "20m", "40m").
    /// </summary>
    [ObservableProperty]
    private string _banda = string.Empty;

    /// <summary>
    /// Modo de operación (ej: "FT8", "SSB").
    /// </summary>
    [ObservableProperty]
    private string _modo = string.Empty;

    /// <summary>
    /// Reporte de señal enviado.
    /// </summary>
    [ObservableProperty]
    private string _senalEnviada = string.Empty;

    /// <summary>
    /// Reporte de señal recibido.
    /// </summary>
    [ObservableProperty]
    private string _senalRecibida = string.Empty;

    /// <summary>
    /// Frecuencia formateada en MHz.
    /// </summary>
    [ObservableProperty]
    private string _frecuencia = string.Empty;

    /// <summary>
    /// Crea un QsoMobileVm a partir de una entidad Qso del dominio.
    /// </summary>
    /// <param name="qso">Entidad QSO de origen.</param>
    /// <returns>Instancia formateada para la lista móvil.</returns>
    public static QsoMobileVm DesdeEntidad(Qso qso)
    {
        ArgumentNullException.ThrowIfNull(qso);

        BandaRadio? banda = qso.Frecuencia.ObtenerBanda();
        string nombreBanda = banda.HasValue
            ? banda.Value.ObtenerNombre()
            : $"{qso.Frecuencia.MHz:F3}";

        return new QsoMobileVm
        {
            Id = qso.Id,
            Fecha = qso.FechaHoraInicio.UtcDateTime.ToString("yyyy-MM-dd HH:mm"),
            Indicativo = qso.IndicativoContacto.Valor,
            Banda = nombreBanda,
            Modo = qso.Modo.ObtenerNombreAdif(),
            SenalEnviada = qso.SenalEnviada,
            SenalRecibida = qso.SenalRecibida,
            Frecuencia = $"{qso.Frecuencia.MHz:F3}"
        };
    }
}

/// <summary>
/// ViewModel del panel de Logbook móvil.
/// Proporciona lista de QSOs con búsqueda, filtros, paginación
/// e importación/exportación ADIF.
/// </summary>
public partial class PanelLogbookMobileViewModel : ViewModelBase
{
    private readonly IRepositorioQso _repositorioQso;
    private readonly IServicioConfiguracion _servicioConfiguracion;
    private readonly ILogger<PanelLogbookMobileViewModel> _logger;

    /// <summary>
    /// Colección observable de QSOs para la lista móvil.
    /// </summary>
    public ObservableCollection<QsoMobileVm> Qsos { get; } = new();

    /// <summary>
    /// Página actual (base 1).
    /// </summary>
    [ObservableProperty]
    private int _paginaActual = 1;

    /// <summary>
    /// Número total de páginas.
    /// </summary>
    [ObservableProperty]
    private int _totalPaginas = 1;

    /// <summary>
    /// Cantidad de elementos por página (menor que escritorio para rendimiento móvil).
    /// </summary>
    [ObservableProperty]
    private int _tamanoPagina = 25;

    /// <summary>
    /// Total de QSOs que coinciden con los filtros actuales.
    /// </summary>
    [ObservableProperty]
    private int _totalQsos;

    /// <summary>
    /// Texto de búsqueda por indicativo.
    /// </summary>
    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    /// <summary>
    /// Filtro por banda seleccionada.
    /// </summary>
    [ObservableProperty]
    private string _filtroBanda = string.Empty;

    /// <summary>
    /// Filtro por modo de operación.
    /// </summary>
    [ObservableProperty]
    private string _filtroModo = string.Empty;

    /// <summary>
    /// Mensaje de estado de la última operación.
    /// </summary>
    [ObservableProperty]
    private string _mensajeEstado = string.Empty;

    /// <summary>
    /// Indica si hay una operación en curso.
    /// </summary>
    [ObservableProperty]
    private bool _estaCargando;

    /// <summary>
    /// Indica si el formulario de creación de QSO manual está visible.
    /// </summary>
    [ObservableProperty]
    private bool _formularioQsoVisible;

    /// <summary>
    /// Indicativo para el formulario de nuevo QSO.
    /// </summary>
    [ObservableProperty]
    private string _nuevoIndicativo = string.Empty;

    /// <summary>
    /// Frecuencia en MHz para el formulario de nuevo QSO.
    /// </summary>
    [ObservableProperty]
    private string _nuevaFrecuencia = string.Empty;

    /// <summary>
    /// Modo para el formulario de nuevo QSO.
    /// </summary>
    [ObservableProperty]
    private string _nuevoModo = "SSB";

    /// <summary>
    /// Señal enviada para el formulario de nuevo QSO.
    /// </summary>
    [ObservableProperty]
    private string _nuevaSenalEnviada = "59";

    /// <summary>
    /// Señal recibida para el formulario de nuevo QSO.
    /// </summary>
    [ObservableProperty]
    private string _nuevaSenalRecibida = "59";

    /// <summary>
    /// Texto descriptivo de la paginación.
    /// </summary>
    public string TextoPaginacion => $"Página {PaginaActual} de {TotalPaginas}";

    /// <summary>
    /// Opciones de banda disponibles para el filtro.
    /// </summary>
    public ObservableCollection<string> OpcionesBanda { get; } = new()
    {
        "", "160m", "80m", "60m", "40m", "30m", "20m", "17m", "15m", "12m", "10m",
        "6m", "4m", "2m", "70cm", "23cm"
    };

    /// <summary>
    /// Opciones de modo disponibles para el filtro.
    /// </summary>
    public ObservableCollection<string> OpcionesModo { get; } = new()
    {
        "", "SSB", "CW", "FT8", "FT4", "FM", "AM", "RTTY", "PSK", "JT65", "JT9"
    };

    /// <summary>
    /// Crea el ViewModel del logbook móvil con sus dependencias.
    /// </summary>
    /// <param name="repositorioQso">Repositorio de QSOs.</param>
    /// <param name="servicioConfiguracion">Servicio de configuración para obtener el indicativo propio.</param>
    /// <param name="logger">Logger para diagnóstico.</param>
    public PanelLogbookMobileViewModel(
        IRepositorioQso repositorioQso,
        IServicioConfiguracion servicioConfiguracion,
        ILogger<PanelLogbookMobileViewModel> logger)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
        _servicioConfiguracion = servicioConfiguracion ?? throw new ArgumentNullException(nameof(servicioConfiguracion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Carga la página actual de QSOs aplicando los filtros vigentes.
    /// </summary>
    [RelayCommand]
    private async Task CargarPaginaAsync()
    {
        EstaCargando = true;
        MensajeEstado = string.Empty;

        try
        {
            FiltroQso? filtro = ConstruirFiltro();

            ResultadoPaginado<Qso> resultado = await _repositorioQso.ObtenerPaginadoAsync(
                PaginaActual, TamanoPagina, filtro, CancellationToken.None);

            Qsos.Clear();
            foreach (Qso qso in resultado.Elementos)
            {
                Qsos.Add(QsoMobileVm.DesdeEntidad(qso));
            }

            TotalQsos = resultado.TotalElementos;
            TotalPaginas = Math.Max(1, (int)Math.Ceiling((double)TotalQsos / TamanoPagina));

            if (PaginaActual > TotalPaginas)
            {
                PaginaActual = TotalPaginas;
            }

            OnPropertyChanged(nameof(TextoPaginacion));

            _logger.LogDebug(
                "Logbook móvil cargado: página {Pagina}/{Total}, {Cantidad} QSOs.",
                PaginaActual, TotalPaginas, TotalQsos);
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al cargar QSOs: {ex.Message}";
            _logger.LogError(ex, "Error al cargar página del logbook móvil.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    /// <summary>
    /// Avanza a la página siguiente.
    /// </summary>
    [RelayCommand]
    private async Task PaginaSiguienteAsync()
    {
        if (PaginaActual < TotalPaginas)
        {
            PaginaActual++;
            await CargarPaginaAsync();
        }
    }

    /// <summary>
    /// Retrocede a la página anterior.
    /// </summary>
    [RelayCommand]
    private async Task PaginaAnteriorAsync()
    {
        if (PaginaActual > 1)
        {
            PaginaActual--;
            await CargarPaginaAsync();
        }
    }

    /// <summary>
    /// Aplica el filtro de búsqueda y recarga desde la primera página.
    /// </summary>
    [RelayCommand]
    private async Task BuscarAsync()
    {
        PaginaActual = 1;
        await CargarPaginaAsync();
    }

    /// <summary>
    /// Limpia todos los filtros y recarga.
    /// </summary>
    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        TextoBusqueda = string.Empty;
        FiltroBanda = string.Empty;
        FiltroModo = string.Empty;
        PaginaActual = 1;
        await CargarPaginaAsync();
    }

    /// <summary>
    /// Muestra u oculta el formulario de creación de QSO manual.
    /// </summary>
    [RelayCommand]
    private void MostrarFormularioQso()
    {
        FormularioQsoVisible = !FormularioQsoVisible;
    }

    /// <summary>
    /// Crea un nuevo QSO manual con los datos del formulario.
    /// </summary>
    [RelayCommand]
    private async Task CrearQsoManualAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoIndicativo))
        {
            MensajeEstado = "El indicativo es obligatorio.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NuevaFrecuencia) ||
            !double.TryParse(NuevaFrecuencia, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double frecuenciaMhz))
        {
            MensajeEstado = "La frecuencia debe ser un número válido en MHz.";
            return;
        }

        try
        {
            EstaCargando = true;

            if (!ModoOperacionExtensiones.IntentarDesdeAdif(NuevoModo, out ModoOperacion modoOperacion))
            {
                MensajeEstado = $"Modo no reconocido: {NuevoModo}";
                return;
            }

            Indicativo indicativoContacto = new Indicativo(NuevoIndicativo.Trim().ToUpperInvariant());

            ConfiguracionCompleta configuracion = await _servicioConfiguracion.CargarAsync(CancellationToken.None);
            string indicativoTexto = string.IsNullOrWhiteSpace(configuracion.Estacion.IndicativoPropio)
                ? "N0CALL"
                : configuracion.Estacion.IndicativoPropio;
            Indicativo indicativoPropio = new Indicativo(indicativoTexto);

            Frecuencia frecuencia = Frecuencia.DesdeMHz(frecuenciaMhz);

            Qso nuevoQso = Qso.Crear(
                indicativoPropio,
                indicativoContacto,
                DateTimeOffset.UtcNow,
                frecuencia,
                modoOperacion,
                NuevaSenalEnviada);

            await _repositorioQso.AgregarAsync(nuevoQso, CancellationToken.None);

            MensajeEstado = $"QSO con {NuevoIndicativo.Trim().ToUpperInvariant()} registrado.";
            _logger.LogInformation("QSO manual creado desde mobile: {Indicativo} en {Frecuencia} MHz.",
                NuevoIndicativo, frecuenciaMhz);

            // Limpiar formulario
            NuevoIndicativo = string.Empty;
            NuevaFrecuencia = string.Empty;
            NuevoModo = "SSB";
            NuevaSenalEnviada = "59";
            NuevaSenalRecibida = "59";
            FormularioQsoVisible = false;

            // Recargar lista
            PaginaActual = 1;
            await CargarPaginaAsync();
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al crear QSO: {ex.Message}";
            _logger.LogError(ex, "Error al crear QSO manual desde mobile.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    /// <summary>
    /// Construye un FiltroQso a partir de los valores actuales de búsqueda y filtros.
    /// Devuelve null si no hay ningún filtro activo.
    /// </summary>
    private FiltroQso? ConstruirFiltro()
    {
        string? indicativo = string.IsNullOrWhiteSpace(TextoBusqueda) ? null : TextoBusqueda.Trim();
        BandaRadio? banda = ParsearBanda(FiltroBanda);
        ModoOperacion? modo = ParsearModo(FiltroModo);

        if (indicativo is null && banda is null && modo is null)
        {
            return null;
        }

        return new FiltroQso(indicativo, banda, modo, null, null);
    }

    /// <summary>
    /// Convierte la cadena de banda a un valor BandaRadio.
    /// </summary>
    private static BandaRadio? ParsearBanda(string? textoFiltro)
    {
        if (string.IsNullOrWhiteSpace(textoFiltro))
        {
            return null;
        }

        return textoFiltro.ToUpperInvariant() switch
        {
            "160M" => BandaRadio.Banda160m,
            "80M" => BandaRadio.Banda80m,
            "60M" => BandaRadio.Banda60m,
            "40M" => BandaRadio.Banda40m,
            "30M" => BandaRadio.Banda30m,
            "20M" => BandaRadio.Banda20m,
            "17M" => BandaRadio.Banda17m,
            "15M" => BandaRadio.Banda15m,
            "12M" => BandaRadio.Banda12m,
            "10M" => BandaRadio.Banda10m,
            "6M" => BandaRadio.Banda6m,
            "4M" => BandaRadio.Banda4m,
            "2M" => BandaRadio.Banda2m,
            "70CM" => BandaRadio.Banda70cm,
            "23CM" => BandaRadio.Banda23cm,
            _ => null
        };
    }

    /// <summary>
    /// Convierte la cadena de modo a un valor ModoOperacion.
    /// </summary>
    private static ModoOperacion? ParsearModo(string? textoFiltro)
    {
        if (string.IsNullOrWhiteSpace(textoFiltro))
        {
            return null;
        }

        if (ModoOperacionExtensiones.IntentarDesdeAdif(textoFiltro, out ModoOperacion modo))
        {
            return modo;
        }

        return null;
    }
}
