using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Satelites;
using RadioAficionado.Infraestructura.Satelites;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// Elemento visual que representa un satélite en la lista del panel.
/// </summary>
public sealed class SateliteVm
{
    /// <summary>Número de catálogo NORAD del satélite.</summary>
    public int NumeroNorad { get; init; }

    /// <summary>Nombre del satélite (ej. "ISS (ZARYA)").</summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Indicativo de radioaficionado del satélite.</summary>
    public string Indicativo { get; init; } = string.Empty;

    /// <summary>Indica si el satélite está operativo.</summary>
    public bool Activo { get; init; }

    /// <summary>Cantidad de transponders disponibles.</summary>
    public int CantidadTransponders { get; init; }

    /// <summary>Texto combinado para mostrar en la lista.</summary>
    public string DisplayText => $"{Nombre} ({Indicativo}) — NORAD {NumeroNorad}";
}

/// <summary>
/// Elemento visual que representa un transponder del satélite seleccionado.
/// </summary>
public sealed class TransponderVm
{
    /// <summary>Nombre del transponder.</summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Frecuencia de enlace de subida formateada.</summary>
    public string EnlaceSubida { get; init; } = string.Empty;

    /// <summary>Frecuencia de enlace de bajada formateada.</summary>
    public string EnlaceBajada { get; init; } = string.Empty;

    /// <summary>Modo de operación del transponder.</summary>
    public string Modo { get; init; } = string.Empty;

    /// <summary>Indica si el transponder invierte la banda lateral.</summary>
    public bool Invertido { get; init; }
}

/// <summary>
/// Elemento visual que representa un paso predicho de un satélite.
/// </summary>
public sealed class PasoSateliteVm
{
    /// <summary>Hora de adquisición de señal formateada (HH:mm:ss).</summary>
    public string HoraAos { get; init; } = string.Empty;

    /// <summary>Hora de pérdida de señal formateada (HH:mm:ss).</summary>
    public string HoraLos { get; init; } = string.Empty;

    /// <summary>Elevación máxima formateada.</summary>
    public string ElevacionMaxima { get; init; } = string.Empty;

    /// <summary>Duración del paso en formato legible.</summary>
    public string Duracion { get; init; } = string.Empty;

    /// <summary>Azimut de aparición formateado.</summary>
    public string AzimutAos { get; init; } = string.Empty;

    /// <summary>Azimut de desaparición formateado.</summary>
    public string AzimutLos { get; init; } = string.Empty;

    /// <summary>Indica si es un paso de alta elevación (>45°).</summary>
    public bool EsAltaElevacion { get; init; }
}

/// <summary>
/// ViewModel del panel de satélites amateur.
/// Muestra el catálogo de satélites, transponders, posición en tiempo real
/// y predicción de pasos sobre la ubicación del observador.
/// </summary>
public partial class PanelSatelitesViewModel : ViewModelBase
{
    private readonly IServicioSatelites _servicioSatelites;
    private readonly ILogger<PanelSatelitesViewModel> _logger;

    /// <summary>
    /// Colección observable de satélites disponibles en el catálogo.
    /// </summary>
    public ObservableCollection<SateliteVm> Satelites { get; } = [];

    /// <summary>
    /// Transponders del satélite seleccionado.
    /// </summary>
    public ObservableCollection<TransponderVm> Transponders { get; } = [];

    /// <summary>
    /// Pasos predichos del satélite seleccionado.
    /// </summary>
    public ObservableCollection<PasoSateliteVm> PasosPredichos { get; } = [];

    /// <summary>
    /// Satélite seleccionado actualmente en la lista.
    /// </summary>
    [ObservableProperty]
    private SateliteVm? _sateliteSeleccionado;

    /// <summary>
    /// Hora AOS del próximo paso.
    /// </summary>
    [ObservableProperty]
    private string _proximoAos = "—";

    /// <summary>
    /// Hora LOS del próximo paso.
    /// </summary>
    [ObservableProperty]
    private string _proximoLos = "—";

    /// <summary>
    /// Elevación máxima del próximo paso.
    /// </summary>
    [ObservableProperty]
    private string _proximoElevacionMaxima = "—";

    /// <summary>
    /// Azimut actual del satélite (si hay tracking activo).
    /// </summary>
    [ObservableProperty]
    private string _azimutActual = "—";

    /// <summary>
    /// Elevación actual del satélite (si hay tracking activo).
    /// </summary>
    [ObservableProperty]
    private string _elevacionActual = "—";

    /// <summary>
    /// Distancia actual al satélite en km.
    /// </summary>
    [ObservableProperty]
    private string _distanciaActual = "—";

    /// <summary>
    /// Indica si el satélite está actualmente sobre el horizonte.
    /// </summary>
    [ObservableProperty]
    private bool _sateliteVisible;

    /// <summary>
    /// Latitud del observador en grados decimales.
    /// </summary>
    [ObservableProperty]
    private double _latitudObservador = 40.4168;

    /// <summary>
    /// Longitud del observador en grados decimales.
    /// </summary>
    [ObservableProperty]
    private double _longitudObservador = -3.7038;

    /// <summary>
    /// Texto de estado del panel.
    /// </summary>
    [ObservableProperty]
    private string _textoEstado = "Listo";

    /// <summary>
    /// Indica si hay una operación en progreso.
    /// </summary>
    [ObservableProperty]
    private bool _cargando;

    /// <summary>
    /// Crea el ViewModel del panel de satélites con las dependencias inyectadas.
    /// </summary>
    /// <param name="servicioSatelites">Servicio de tracking de satélites.</param>
    /// <param name="logger">Logger para registro de eventos.</param>
    public PanelSatelitesViewModel(
        IServicioSatelites servicioSatelites,
        ILogger<PanelSatelitesViewModel> logger)
    {
        _servicioSatelites = servicioSatelites ?? throw new ArgumentNullException(nameof(servicioSatelites));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        CargarCatalogo();
    }

    /// <summary>
    /// Actualiza la predicción de pasos del satélite seleccionado para las próximas 24 horas.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand]
    private async Task ActualizarPasosAsync(CancellationToken ct)
    {
        if (SateliteSeleccionado is null)
        {
            return;
        }

        try
        {
            Cargando = true;
            TextoEstado = $"Calculando pasos de {SateliteSeleccionado.Nombre}...";
            _logger.LogInformation("Calculando pasos de {Satelite} (NORAD {Norad})",
                SateliteSeleccionado.Nombre, SateliteSeleccionado.NumeroNorad);

            Coordenadas observador = new Coordenadas(LatitudObservador, LongitudObservador);
            DateTime ahora = DateTime.UtcNow;

            IReadOnlyList<PasoSatelite> pasos = await _servicioSatelites.PredecirPasosAsync(
                SateliteSeleccionado.NumeroNorad,
                observador,
                ahora,
                ahora.AddHours(24),
                ct);

            PasosPredichos.Clear();

            foreach (PasoSatelite paso in pasos)
            {
                PasosPredichos.Add(ConvertirPasoAVm(paso));
            }

            // Actualizar próximo paso
            PasoSatelite? proximoPaso = await _servicioSatelites.ObtenerProximoPasoAsync(
                SateliteSeleccionado.NumeroNorad,
                observador,
                ct);

            if (proximoPaso is not null)
            {
                ProximoAos = proximoPaso.Aos.ToString("HH:mm:ss") + " UTC";
                ProximoLos = proximoPaso.Los.ToString("HH:mm:ss") + " UTC";
                ProximoElevacionMaxima = $"{proximoPaso.ElevacionMaxima:F1}°";
            }
            else
            {
                ProximoAos = "Sin pasos";
                ProximoLos = "—";
                ProximoElevacionMaxima = "—";
            }

            TextoEstado = $"{pasos.Count} paso(s) predicho(s) en las próximas 24h";
            _logger.LogInformation("{CantidadPasos} pasos predichos para {Satelite}",
                pasos.Count, SateliteSeleccionado.Nombre);
        }
        catch (OperationCanceledException)
        {
            TextoEstado = "Cálculo cancelado";
            _logger.LogInformation("Cálculo de pasos cancelado por el usuario");
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error: {ex.Message}";
            _logger.LogError(ex, "Error al calcular pasos del satélite");
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Calcula la posición actual del satélite seleccionado.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand]
    private async Task ActualizarPosicionAsync(CancellationToken ct)
    {
        if (SateliteSeleccionado is null)
        {
            return;
        }

        try
        {
            Coordenadas observador = new Coordenadas(LatitudObservador, LongitudObservador);

            PosicionSatelite posicion = await _servicioSatelites.CalcularPosicionAsync(
                SateliteSeleccionado.NumeroNorad,
                observador,
                DateTime.UtcNow,
                ct);

            AzimutActual = $"{posicion.Azimut:F1}°";
            ElevacionActual = $"{posicion.Elevacion:F1}°";
            DistanciaActual = $"{posicion.Distancia:F0} km";
            SateliteVisible = posicion.SobreHorizonte;

            TextoEstado = posicion.SobreHorizonte
                ? $"{SateliteSeleccionado.Nombre} VISIBLE — Az: {posicion.Azimut:F1}° El: {posicion.Elevacion:F1}°"
                : $"{SateliteSeleccionado.Nombre} bajo el horizonte";
        }
        catch (OperationCanceledException)
        {
            // Ignorar cancelaciones silenciosamente
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error posición: {ex.Message}";
            _logger.LogError(ex, "Error al calcular posición del satélite");
        }
    }

    /// <summary>
    /// Selecciona un satélite y carga sus transponders y próximo paso.
    /// </summary>
    /// <param name="satelite">Satélite a seleccionar.</param>
    [RelayCommand]
    private async Task SeleccionarSateliteAsync(SateliteVm? satelite)
    {
        if (satelite is null)
        {
            return;
        }

        SateliteSeleccionado = satelite;
    }

    /// <summary>
    /// Reacciona al cambio de satélite seleccionado cargando transponders y próximo paso.
    /// </summary>
    partial void OnSateliteSeleccionadoChanged(SateliteVm? value)
    {
        Transponders.Clear();
        PasosPredichos.Clear();
        ProximoAos = "—";
        ProximoLos = "—";
        ProximoElevacionMaxima = "—";
        AzimutActual = "—";
        ElevacionActual = "—";
        DistanciaActual = "—";
        SateliteVisible = false;

        if (value is null)
        {
            return;
        }

        SateliteAmateur? sateliteDominio = CatalogoSatelites.BuscarPorNorad(value.NumeroNorad);

        if (sateliteDominio is null)
        {
            return;
        }

        foreach (TransponderSatelite transponder in sateliteDominio.Transponders)
        {
            Transponders.Add(new TransponderVm
            {
                Nombre = transponder.Nombre,
                EnlaceSubida = transponder.EnlaceSubida.ToString(),
                EnlaceBajada = transponder.EnlaceBajada.ToString(),
                Modo = transponder.Modo.ToString(),
                Invertido = transponder.Invertido
            });
        }

        _logger.LogInformation("Satélite seleccionado: {Nombre} (NORAD {Norad})",
            value.Nombre, value.NumeroNorad);
    }

    /// <summary>
    /// Carga el catálogo estático de satélites amateur en la colección observable.
    /// </summary>
    private void CargarCatalogo()
    {
        IReadOnlyList<SateliteAmateur> catalogo = CatalogoSatelites.ObtenerTodos();

        foreach (SateliteAmateur satelite in catalogo)
        {
            Satelites.Add(new SateliteVm
            {
                NumeroNorad = satelite.NumeroNorad,
                Nombre = satelite.Nombre,
                Indicativo = satelite.Indicativo,
                Activo = satelite.Activo,
                CantidadTransponders = satelite.Transponders.Count
            });
        }

        _logger.LogInformation("{CantidadSatelites} satélites cargados del catálogo", catalogo.Count);
    }

    /// <summary>
    /// Convierte un PasoSatelite del dominio a un PasoSateliteVm para la vista.
    /// </summary>
    /// <param name="paso">Paso del dominio.</param>
    /// <returns>ViewModel del paso para la vista.</returns>
    private static PasoSateliteVm ConvertirPasoAVm(PasoSatelite paso)
    {
        TimeSpan duracion = paso.Los - paso.Aos;

        return new PasoSateliteVm
        {
            HoraAos = paso.Aos.ToString("HH:mm:ss"),
            HoraLos = paso.Los.ToString("HH:mm:ss"),
            ElevacionMaxima = $"{paso.ElevacionMaxima:F1}°",
            Duracion = $"{duracion.Minutes:D2}:{duracion.Seconds:D2}",
            AzimutAos = $"{paso.AzimutAos:F0}°",
            AzimutLos = $"{paso.AzimutLos:F0}°",
            EsAltaElevacion = paso.EsAltaElevacion
        };
    }
}
