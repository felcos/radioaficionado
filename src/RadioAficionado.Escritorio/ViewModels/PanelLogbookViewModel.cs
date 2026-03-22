using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.Adif;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel para un QSO mostrado en el DataGrid del logbook.
/// Contiene propiedades formateadas para la presentación en la tabla.
/// </summary>
public partial class QsoEnLogbookVm : ViewModelBase
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
    /// Notas del contacto (truncadas para la tabla).
    /// </summary>
    [ObservableProperty]
    private string _notas = string.Empty;

    /// <summary>
    /// Frecuencia formateada en MHz.
    /// </summary>
    [ObservableProperty]
    private string _frecuencia = string.Empty;

    /// <summary>
    /// Crea un QsoEnLogbookVm a partir de una entidad Qso del dominio.
    /// </summary>
    /// <param name="qso">Entidad QSO de origen.</param>
    /// <returns>Instancia formateada para el DataGrid.</returns>
    public static QsoEnLogbookVm DesdeEntidad(Qso qso)
    {
        ArgumentNullException.ThrowIfNull(qso);

        BandaRadio? banda = qso.Frecuencia.ObtenerBanda();
        string nombreBanda = banda.HasValue
            ? ObtenerEtiquetaBanda(banda.Value)
            : $"{qso.Frecuencia.MHz:F3}";

        return new QsoEnLogbookVm
        {
            Id = qso.Id,
            Fecha = qso.FechaHoraInicio.UtcDateTime.ToString("yyyy-MM-dd HH:mm"),
            Indicativo = qso.IndicativoContacto.Valor,
            Banda = nombreBanda,
            Modo = qso.Modo.ObtenerNombreAdif(),
            SenalEnviada = qso.SenalEnviada,
            SenalRecibida = qso.SenalRecibida,
            Frecuencia = $"{qso.Frecuencia.MHz:F3}",
            Notas = qso.Notas ?? string.Empty
        };
    }

    /// <summary>
    /// Obtiene la etiqueta corta de la banda para mostrar en el DataGrid.
    /// </summary>
    private static string ObtenerEtiquetaBanda(BandaRadio banda)
    {
        return banda switch
        {
            BandaRadio.Banda2200m => "2200m",
            BandaRadio.Banda630m => "630m",
            BandaRadio.Banda160m => "160m",
            BandaRadio.Banda80m => "80m",
            BandaRadio.Banda60m => "60m",
            BandaRadio.Banda40m => "40m",
            BandaRadio.Banda30m => "30m",
            BandaRadio.Banda20m => "20m",
            BandaRadio.Banda17m => "17m",
            BandaRadio.Banda15m => "15m",
            BandaRadio.Banda12m => "12m",
            BandaRadio.Banda10m => "10m",
            BandaRadio.Banda6m => "6m",
            BandaRadio.Banda4m => "4m",
            BandaRadio.Banda2m => "2m",
            BandaRadio.Banda1_25m => "1.25m",
            BandaRadio.Banda70cm => "70cm",
            BandaRadio.Banda33cm => "33cm",
            BandaRadio.Banda23cm => "23cm",
            BandaRadio.Banda13cm => "13cm",
            BandaRadio.Banda9cm => "9cm",
            BandaRadio.Banda5cm => "5cm",
            BandaRadio.Banda3cm => "3cm",
            BandaRadio.Banda1_2cm => "1.2cm",
            _ => "?"
        };
    }
}

/// <summary>
/// ViewModel del panel Logbook (libro de guardia) con paginación, filtros,
/// importación/exportación ADIF y estadísticas básicas.
/// </summary>
public partial class PanelLogbookViewModel : ViewModelBase
{
    private readonly IRepositorioQso _repositorioQso;
    private readonly ILogger<PanelLogbookViewModel> _logger;

    /// <summary>
    /// Colección de QSOs visibles en la página actual del DataGrid.
    /// </summary>
    public ObservableCollection<QsoEnLogbookVm> Qsos { get; } = new();

    // ── Paginación ──────────────────────────────────────────────

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
    /// Cantidad de elementos por página.
    /// </summary>
    [ObservableProperty]
    private int _tamanoPagina = 50;

    /// <summary>
    /// Total de QSOs que coinciden con los filtros actuales.
    /// </summary>
    [ObservableProperty]
    private int _totalQsos;

    /// <summary>
    /// Total de prefijos (DXCC) únicos en todo el log.
    /// </summary>
    [ObservableProperty]
    private int _totalDxcc;

    // ── Filtros ─────────────────────────────────────────────────

    /// <summary>
    /// Filtro por indicativo (búsqueda parcial).
    /// </summary>
    [ObservableProperty]
    private string _filtroIndicativo = string.Empty;

    /// <summary>
    /// Filtro por banda seleccionada. Null para todas.
    /// </summary>
    [ObservableProperty]
    private string _filtroBanda = string.Empty;

    /// <summary>
    /// Filtro por modo de operación. Null para todos.
    /// </summary>
    [ObservableProperty]
    private string _filtroModo = string.Empty;

    /// <summary>
    /// Filtro de fecha desde (inclusive).
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset? _filtroFechaDesde;

    /// <summary>
    /// Filtro de fecha hasta (inclusive).
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset? _filtroFechaHasta;

    // ── Mensajes de estado ──────────────────────────────────────

    /// <summary>
    /// Mensaje de estado/resultado de la última operación (importar, exportar, etc.).
    /// </summary>
    [ObservableProperty]
    private string _mensajeEstado = string.Empty;

    /// <summary>
    /// Indica si hay una operación en curso (carga, importación, exportación).
    /// </summary>
    [ObservableProperty]
    private bool _estaCargando;

    // ── Listas para los ComboBox de filtros ─────────────────────

    /// <summary>
    /// Opciones de banda disponibles para el ComboBox de filtro.
    /// </summary>
    public ObservableCollection<string> OpcionesBanda { get; } = new()
    {
        "", "160m", "80m", "60m", "40m", "30m", "20m", "17m", "15m", "12m", "10m",
        "6m", "4m", "2m", "70cm", "23cm"
    };

    /// <summary>
    /// Opciones de modo disponibles para el ComboBox de filtro.
    /// </summary>
    public ObservableCollection<string> OpcionesModo { get; } = new()
    {
        "", "SSB", "CW", "FT8", "FT4", "FM", "AM", "RTTY", "PSK", "JT65", "JT9",
        "JS8", "OLIVIA", "MFSK", "DIGITALVOICE"
    };

    /// <summary>
    /// Texto descriptivo de la paginación: "Página X de Y".
    /// </summary>
    public string TextoPaginacion => $"Página {PaginaActual} de {TotalPaginas}";

    /// <summary>
    /// Crea el ViewModel del logbook con sus dependencias.
    /// </summary>
    /// <param name="repositorioQso">Repositorio de QSOs.</param>
    /// <param name="logger">Logger para diagnóstico.</param>
    public PanelLogbookViewModel(
        IRepositorioQso repositorioQso,
        ILogger<PanelLogbookViewModel> logger)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
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
                Qsos.Add(QsoEnLogbookVm.DesdeEntidad(qso));
            }

            TotalQsos = resultado.TotalElementos;
            TotalPaginas = Math.Max(1, (int)Math.Ceiling((double)TotalQsos / TamanoPagina));

            if (PaginaActual > TotalPaginas)
            {
                PaginaActual = TotalPaginas;
            }

            OnPropertyChanged(nameof(TextoPaginacion));
            await ActualizarEstadisticasDxccAsync();

            _logger.LogDebug(
                "Logbook cargado: página {Pagina}/{Total}, {Cantidad} QSOs.",
                PaginaActual, TotalPaginas, TotalQsos);
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al cargar QSOs: {ex.Message}";
            _logger.LogError(ex, "Error al cargar página del logbook.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    /// <summary>
    /// Avanza a la página siguiente del logbook.
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
    /// Retrocede a la página anterior del logbook.
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
    /// Aplica los filtros actuales y recarga desde la primera página.
    /// </summary>
    [RelayCommand]
    private async Task AplicarFiltrosAsync()
    {
        PaginaActual = 1;
        await CargarPaginaAsync();
    }

    /// <summary>
    /// Limpia todos los filtros y recarga desde la primera página.
    /// </summary>
    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        FiltroIndicativo = string.Empty;
        FiltroBanda = string.Empty;
        FiltroModo = string.Empty;
        FiltroFechaDesde = null;
        FiltroFechaHasta = null;
        PaginaActual = 1;
        await CargarPaginaAsync();
    }

    /// <summary>
    /// Exporta todos los QSOs (con los filtros activos) a un archivo ADIF.
    /// Abre un diálogo para seleccionar la ruta de destino.
    /// </summary>
    [RelayCommand]
    private async Task ExportarAdifAsync()
    {
        EstaCargando = true;
        MensajeEstado = string.Empty;

        try
        {
            Window? ventana = ObtenerVentanaPrincipal();
            if (ventana is null)
            {
                MensajeEstado = "No se pudo obtener la ventana para el diálogo de guardado.";
                return;
            }

            IStorageFile? archivo = await ventana.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Exportar ADIF",
                    DefaultExtension = "adi",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("ADIF") { Patterns = new[] { "*.adi", "*.adif" } }
                    },
                    SuggestedFileName = $"logbook_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.adi"
                });

            if (archivo is null)
            {
                return;
            }

            // Obtener todos los QSOs con el filtro actual (sin paginación)
            FiltroQso? filtro = ConstruirFiltro();
            int totalConFiltro = await _repositorioQso.ContarConFiltroAsync(filtro, CancellationToken.None);
            ResultadoPaginado<Qso> todos = await _repositorioQso.ObtenerPaginadoAsync(
                1, totalConFiltro > 0 ? totalConFiltro : 1, filtro, CancellationToken.None);

            IReadOnlyList<RegistroAdif> registrosAdif = ConvertidorAdifQso.ConvertirListaAAdif(todos.Elementos);
            string contenidoAdif = GeneradorAdif.Generar(registrosAdif);

            await using Stream flujo = await archivo.OpenWriteAsync();
            await using StreamWriter escritor = new StreamWriter(flujo, System.Text.Encoding.UTF8);
            await escritor.WriteAsync(contenidoAdif);

            MensajeEstado = $"Exportados {registrosAdif.Count} QSOs a ADIF.";
            _logger.LogInformation("Exportados {Cantidad} QSOs a archivo ADIF.", registrosAdif.Count);
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al exportar: {ex.Message}";
            _logger.LogError(ex, "Error al exportar archivo ADIF.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    /// <summary>
    /// Importa QSOs desde un archivo ADIF seleccionado por el usuario.
    /// </summary>
    [RelayCommand]
    private async Task ImportarAdifAsync()
    {
        EstaCargando = true;
        MensajeEstado = string.Empty;

        try
        {
            Window? ventana = ObtenerVentanaPrincipal();
            if (ventana is null)
            {
                MensajeEstado = "No se pudo obtener la ventana para el diálogo de apertura.";
                return;
            }

            IReadOnlyList<IStorageFile> archivos = await ventana.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Importar ADIF",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("ADIF") { Patterns = new[] { "*.adi", "*.adif" } }
                    }
                });

            if (archivos.Count == 0)
            {
                return;
            }

            await using Stream flujo = await archivos[0].OpenReadAsync();
            ResultadoParserAdif resultadoParser = await ParserAdif.ParsearDesdeStreamAsync(flujo);

            (IReadOnlyList<Qso> qsosImportados, int descartados) =
                ConvertidorAdifQso.ConvertirListaAQsos(resultadoParser.Registros);

            int importados = 0;
            foreach (Qso qso in qsosImportados)
            {
                await _repositorioQso.AgregarAsync(qso, CancellationToken.None);
                importados++;
            }

            MensajeEstado = $"Importados {importados} QSOs. Descartados: {descartados}.";
            if (resultadoParser.Advertencias.Count > 0)
            {
                MensajeEstado += $" Advertencias: {resultadoParser.Advertencias.Count}.";
            }

            _logger.LogInformation(
                "Importación ADIF completada: {Importados} importados, {Descartados} descartados, {Advertencias} advertencias.",
                importados, descartados, resultadoParser.Advertencias.Count);

            // Recargar la página actual
            PaginaActual = 1;
            await CargarPaginaAsync();
        }
        catch (Exception ex)
        {
            MensajeEstado = $"Error al importar: {ex.Message}";
            _logger.LogError(ex, "Error al importar archivo ADIF.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    /// <summary>
    /// Construye un FiltroQso a partir de los valores actuales de los filtros de la UI.
    /// Devuelve null si no hay ningún filtro activo.
    /// </summary>
    private FiltroQso? ConstruirFiltro()
    {
        string? indicativo = string.IsNullOrWhiteSpace(FiltroIndicativo) ? null : FiltroIndicativo.Trim();
        BandaRadio? banda = ParsearBandaDesdeFiltro(FiltroBanda);
        ModoOperacion? modo = ParsearModoDesdeFiltro(FiltroModo);

        if (indicativo is null && banda is null && modo is null &&
            FiltroFechaDesde is null && FiltroFechaHasta is null)
        {
            return null;
        }

        return new FiltroQso(indicativo, banda, modo, FiltroFechaDesde, FiltroFechaHasta);
    }

    /// <summary>
    /// Convierte la cadena de banda seleccionada en el ComboBox a un valor BandaRadio.
    /// </summary>
    private static BandaRadio? ParsearBandaDesdeFiltro(string? textoFiltro)
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
    /// Convierte la cadena de modo seleccionada en el ComboBox a un valor ModoOperacion.
    /// </summary>
    private static ModoOperacion? ParsearModoDesdeFiltro(string? textoFiltro)
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

    /// <summary>
    /// Actualiza el conteo de prefijos DXCC únicos en todo el logbook.
    /// </summary>
    private async Task ActualizarEstadisticasDxccAsync()
    {
        try
        {
            // Obtener todos los QSOs para calcular DXCC únicos
            // Para un cálculo más eficiente en el futuro, esto debería hacerse en el repositorio
            IReadOnlyList<Qso> todos = await _repositorioQso.ObtenerTodosAsync(CancellationToken.None);
            HashSet<string> prefijosUnicos = new(StringComparer.OrdinalIgnoreCase);

            foreach (Qso qso in todos)
            {
                string prefijo = ExtraerPrefijo(qso.IndicativoContacto.Valor);
                if (!string.IsNullOrWhiteSpace(prefijo))
                {
                    prefijosUnicos.Add(prefijo);
                }
            }

            TotalDxcc = prefijosUnicos.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudieron calcular las estadísticas DXCC.");
        }
    }

    /// <summary>
    /// Extrae el prefijo de un indicativo (letras y números hasta el último dígito antes del sufijo).
    /// Ej: "EA4FKR" -> "EA4", "W1ABC" -> "W1", "VK2ABC" -> "VK2".
    /// </summary>
    private static string ExtraerPrefijo(string indicativo)
    {
        if (string.IsNullOrWhiteSpace(indicativo))
        {
            return string.Empty;
        }

        // Buscar el último dígito seguido de letras (sufijo)
        int ultimoDigito = -1;
        for (int i = 0; i < indicativo.Length; i++)
        {
            if (char.IsDigit(indicativo[i]))
            {
                ultimoDigito = i;
            }
        }

        if (ultimoDigito < 0)
        {
            return indicativo;
        }

        return indicativo[..(ultimoDigito + 1)];
    }

    /// <summary>
    /// Obtiene la ventana principal de la aplicación para mostrar diálogos.
    /// </summary>
    private static Window? ObtenerVentanaPrincipal()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime escritorio)
        {
            return escritorio.MainWindow;
        }

        return null;
    }
}
