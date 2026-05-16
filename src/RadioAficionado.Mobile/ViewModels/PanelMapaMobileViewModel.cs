using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Mobile.ViewModels;

/// <summary>
/// Estadísticas de contactos por continente para el mapa móvil.
/// </summary>
public sealed class EstadisticaContinenteVm
{
    /// <summary>
    /// Nombre del continente (ej: "Europa", "América del Norte").
    /// </summary>
    public string Continente { get; init; } = string.Empty;

    /// <summary>
    /// Cantidad de QSOs realizados con estaciones de este continente.
    /// </summary>
    public int CantidadQsos { get; init; }

    /// <summary>
    /// Cantidad de prefijos DXCC únicos contactados en este continente.
    /// </summary>
    public int PrefijosUnicos { get; init; }

    /// <summary>
    /// Color asociado al continente para la UI.
    /// </summary>
    public string Color { get; init; } = "#888888";
}

/// <summary>
/// ViewModel del panel de Mapa móvil.
/// Como Avalonia no tiene control de mapa nativo, muestra estadísticas textuales
/// de contactos por continente/región y un resumen de prefijos DXCC contactados.
/// </summary>
public partial class PanelMapaMobileViewModel : ViewModelBase
{
    private readonly IRepositorioQso _repositorioQso;
    private readonly ILogger<PanelMapaMobileViewModel> _logger;

    /// <summary>
    /// Estadísticas de contactos agrupadas por prefijo de continente.
    /// </summary>
    public ObservableCollection<EstadisticaContinenteVm> EstadisticasPorContinente { get; } = new();

    /// <summary>
    /// Total de QSOs en el logbook.
    /// </summary>
    [ObservableProperty]
    private int _totalQsos;

    /// <summary>
    /// Total de prefijos DXCC únicos contactados.
    /// </summary>
    [ObservableProperty]
    private int _totalDxcc;

    /// <summary>
    /// Total de bandas diferentes utilizadas.
    /// </summary>
    [ObservableProperty]
    private int _totalBandas;

    /// <summary>
    /// Total de modos diferentes utilizados.
    /// </summary>
    [ObservableProperty]
    private int _totalModos;

    /// <summary>
    /// Mensaje de estado.
    /// </summary>
    [ObservableProperty]
    private string _textoEstado = "Esperando carga de datos...";

    /// <summary>
    /// Indica si hay una operación en curso.
    /// </summary>
    [ObservableProperty]
    private bool _estaCargando;

    /// <summary>
    /// Crea el ViewModel del mapa móvil con sus dependencias.
    /// </summary>
    /// <param name="repositorioQso">Repositorio de QSOs.</param>
    /// <param name="logger">Logger para diagnóstico.</param>
    public PanelMapaMobileViewModel(
        IRepositorioQso repositorioQso,
        ILogger<PanelMapaMobileViewModel> logger)
    {
        _repositorioQso = repositorioQso ?? throw new ArgumentNullException(nameof(repositorioQso));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Carga las estadísticas de contactos del logbook agrupadas por prefijo.
    /// </summary>
    [RelayCommand]
    private async Task CargarEstadisticasAsync()
    {
        EstaCargando = true;
        TextoEstado = "Cargando estadísticas...";

        try
        {
            IReadOnlyList<Qso> todosLosQsos = await _repositorioQso.ObtenerTodosAsync(CancellationToken.None);

            TotalQsos = todosLosQsos.Count;

            HashSet<string> prefijosUnicos = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> bandasUnicas = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> modosUnicos = new(StringComparer.OrdinalIgnoreCase);

            Dictionary<string, (int Qsos, HashSet<string> Prefijos)> porContinente = new();

            foreach (Qso qso in todosLosQsos)
            {
                string prefijo = ExtraerPrefijo(qso.IndicativoContacto.Valor);
                if (!string.IsNullOrWhiteSpace(prefijo))
                {
                    prefijosUnicos.Add(prefijo);
                }

                BandaRadio? banda = qso.Frecuencia.ObtenerBanda();
                if (banda.HasValue)
                {
                    bandasUnicas.Add(banda.Value.ObtenerNombre());
                }

                modosUnicos.Add(qso.Modo.ObtenerNombreAdif());

                string continente = ClasificarContinente(prefijo);
                if (!porContinente.ContainsKey(continente))
                {
                    porContinente[continente] = (0, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                }

                (int qsosActual, HashSet<string> prefijosActual) = porContinente[continente];
                prefijosActual.Add(prefijo);
                porContinente[continente] = (qsosActual + 1, prefijosActual);
            }

            TotalDxcc = prefijosUnicos.Count;
            TotalBandas = bandasUnicas.Count;
            TotalModos = modosUnicos.Count;

            EstadisticasPorContinente.Clear();
            string[] colores = ["#e94560", "#00c853", "#00e5ff", "#ffd700", "#ff8c00", "#533483"];
            int indiceColor = 0;

            foreach (KeyValuePair<string, (int Qsos, HashSet<string> Prefijos)> entrada in porContinente
                .OrderByDescending(e => e.Value.Qsos))
            {
                EstadisticasPorContinente.Add(new EstadisticaContinenteVm
                {
                    Continente = entrada.Key,
                    CantidadQsos = entrada.Value.Qsos,
                    PrefijosUnicos = entrada.Value.Prefijos.Count,
                    Color = colores[indiceColor % colores.Length]
                });
                indiceColor++;
            }

            TextoEstado = $"Estadísticas cargadas: {TotalQsos} QSOs, {TotalDxcc} DXCC.";
            _logger.LogInformation("Estadísticas de mapa móvil cargadas: {TotalQsos} QSOs, {TotalDxcc} DXCC.",
                TotalQsos, TotalDxcc);
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error al cargar estadísticas: {ex.Message}";
            _logger.LogError(ex, "Error al cargar estadísticas del mapa móvil.");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    /// <summary>
    /// Extrae el prefijo de un indicativo (letras y números hasta el último dígito antes del sufijo).
    /// </summary>
    private static string ExtraerPrefijo(string indicativo)
    {
        if (string.IsNullOrWhiteSpace(indicativo))
        {
            return string.Empty;
        }

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
    /// Clasifica un prefijo en un continente aproximado basándose en las primeras letras.
    /// Esta es una clasificación simplificada; para mayor precisión se usaría la tabla DXCC completa.
    /// </summary>
    private static string ClasificarContinente(string prefijo)
    {
        if (string.IsNullOrWhiteSpace(prefijo))
        {
            return "Desconocido";
        }

        string prefijoUpper = prefijo.ToUpperInvariant();

        // Clasificación simplificada por primeras letras
        if (prefijoUpper.StartsWith("EA") || prefijoUpper.StartsWith("F") ||
            prefijoUpper.StartsWith("G") || prefijoUpper.StartsWith("DL") ||
            prefijoUpper.StartsWith("D") || prefijoUpper.StartsWith("I") ||
            prefijoUpper.StartsWith("ON") || prefijoUpper.StartsWith("PA") ||
            prefijoUpper.StartsWith("SM") || prefijoUpper.StartsWith("OH") ||
            prefijoUpper.StartsWith("OK") || prefijoUpper.StartsWith("SP") ||
            prefijoUpper.StartsWith("HA") || prefijoUpper.StartsWith("OE") ||
            prefijoUpper.StartsWith("HB") || prefijoUpper.StartsWith("LX") ||
            prefijoUpper.StartsWith("CT") || prefijoUpper.StartsWith("YO") ||
            prefijoUpper.StartsWith("LZ") || prefijoUpper.StartsWith("SV") ||
            prefijoUpper.StartsWith("9A") || prefijoUpper.StartsWith("S5") ||
            prefijoUpper.StartsWith("LA") || prefijoUpper.StartsWith("OZ"))
        {
            return "Europa";
        }

        if (prefijoUpper.StartsWith("W") || prefijoUpper.StartsWith("K") ||
            prefijoUpper.StartsWith("N") || prefijoUpper.StartsWith("VE") ||
            prefijoUpper.StartsWith("XE"))
        {
            return "América del Norte";
        }

        if (prefijoUpper.StartsWith("LU") || prefijoUpper.StartsWith("PY") ||
            prefijoUpper.StartsWith("CE") || prefijoUpper.StartsWith("CX") ||
            prefijoUpper.StartsWith("HC") || prefijoUpper.StartsWith("OA") ||
            prefijoUpper.StartsWith("HK") || prefijoUpper.StartsWith("YV"))
        {
            return "América del Sur";
        }

        if (prefijoUpper.StartsWith("JA") || prefijoUpper.StartsWith("J") ||
            prefijoUpper.StartsWith("BV") || prefijoUpper.StartsWith("HL") ||
            prefijoUpper.StartsWith("VU") || prefijoUpper.StartsWith("HS") ||
            prefijoUpper.StartsWith("9V") || prefijoUpper.StartsWith("DU"))
        {
            return "Asia";
        }

        if (prefijoUpper.StartsWith("VK") || prefijoUpper.StartsWith("ZL"))
        {
            return "Oceanía";
        }

        if (prefijoUpper.StartsWith("5") || prefijoUpper.StartsWith("7") ||
            prefijoUpper.StartsWith("ET") || prefijoUpper.StartsWith("ZS") ||
            prefijoUpper.StartsWith("SU") || prefijoUpper.StartsWith("CN"))
        {
            return "África";
        }

        return "Otros";
    }
}
