using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Propagacion;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// Elemento visual que representa la predicción de propagación para una banda HF.
/// </summary>
public sealed class PrediccionBandaVm
{
    /// <summary>Nombre descriptivo de la banda (ej. "20 metros").</summary>
    public string NombreBanda { get; init; } = string.Empty;

    /// <summary>Nivel de propagación como texto.</summary>
    public string Nivel { get; init; } = string.Empty;

    /// <summary>Color asociado al nivel de propagación (#hex).</summary>
    public string ColorNivel { get; init; } = "#888888";

    /// <summary>Descripción textual de las condiciones esperadas.</summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>Mejor hora de propagación formateada (ej. "14:00 - 18:00 UTC").</summary>
    public string MejorHora { get; init; } = string.Empty;

    /// <summary>Regiones alcanzables con esta banda.</summary>
    public string Regiones { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel del panel de propagación HF.
/// Muestra los índices solares actuales y predicciones de propagación por banda,
/// con auto-actualización cada 30 minutos.
/// </summary>
public partial class PanelPropagacionViewModel : ViewModelBase
{
    private readonly IServicioPropagacion _servicioPropagacion;
    private readonly ILogger<PanelPropagacionViewModel> _logger;
    private IDisposable? _temporizadorAutoActualizacion;

    /// <summary>
    /// Colección observable de predicciones por banda HF.
    /// </summary>
    public ObservableCollection<PrediccionBandaVm> Predicciones { get; } = [];

    /// <summary>
    /// Solar Flux Index (60-300). Valores altos favorecen bandas altas de HF.
    /// </summary>
    [ObservableProperty]
    private int _sfi;

    /// <summary>
    /// Índice K planetario (0-9). Valores bajos indican condiciones estables.
    /// </summary>
    [ObservableProperty]
    private int _kp;

    /// <summary>
    /// Índice A planetario. Promedio diario de actividad geomagnética.
    /// </summary>
    [ObservableProperty]
    private int _ap;

    /// <summary>
    /// Número de manchas solares observadas (SSN).
    /// </summary>
    [ObservableProperty]
    private double _ssn;

    /// <summary>
    /// Fecha y hora de la última actualización de los índices solares.
    /// </summary>
    [ObservableProperty]
    private string _ultimaActualizacion = "Sin datos";

    /// <summary>
    /// Texto de estado para la barra inferior del panel.
    /// </summary>
    [ObservableProperty]
    private string _textoEstado = "Esperando datos de propagación...";

    /// <summary>
    /// Indica si se está procesando una operación (para deshabilitar botones).
    /// </summary>
    [ObservableProperty]
    private bool _procesando;

    /// <summary>
    /// Color del indicador de SFI según su valor.
    /// </summary>
    [ObservableProperty]
    private string _colorSfi = "#888888";

    /// <summary>
    /// Color del indicador de Kp según su valor.
    /// </summary>
    [ObservableProperty]
    private string _colorKp = "#888888";

    /// <summary>
    /// Texto descriptivo del estado geomagnético.
    /// </summary>
    [ObservableProperty]
    private string _estadoGeomagnetico = "Sin datos";

    /// <summary>
    /// Color del estado geomagnético.
    /// </summary>
    [ObservableProperty]
    private string _colorEstadoGeomagnetico = "#888888";

    /// <summary>
    /// Crea el ViewModel del panel de propagación con las dependencias inyectadas.
    /// </summary>
    /// <param name="servicioPropagacion">Servicio de predicción de propagación HF.</param>
    /// <param name="logger">Logger para registro de eventos.</param>
    public PanelPropagacionViewModel(
        IServicioPropagacion servicioPropagacion,
        ILogger<PanelPropagacionViewModel> logger)
    {
        _servicioPropagacion = servicioPropagacion ?? throw new ArgumentNullException(nameof(servicioPropagacion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _ = CargarDatosInicialesAsync();
        IniciarAutoActualizacion();
    }

    /// <summary>
    /// Consulta los índices solares actuales desde el servicio de propagación.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand]
    private async Task ActualizarIndicesAsync(CancellationToken ct)
    {
        try
        {
            Procesando = true;
            TextoEstado = "Consultando índices solares...";

            IndicesSolares indices = await _servicioPropagacion.ObtenerIndicesSolaresAsync(ct);

            Dispatcher.UIThread.Post(() =>
            {
                Sfi = indices.Sfi;
                Kp = indices.Kp;
                Ap = indices.Ap;
                Ssn = indices.NumeroManchasSolares;
                UltimaActualizacion = indices.FechaActualizacion.ToString("yyyy-MM-dd HH:mm UTC");

                ColorSfi = CalcularColorSfi(indices.Sfi);
                ColorKp = CalcularColorKp(indices.Kp);
                (EstadoGeomagnetico, ColorEstadoGeomagnetico) = CalcularEstadoGeomagnetico(indices);
            });

            TextoEstado = $"Índices actualizados — SFI: {indices.Sfi}, Kp: {indices.Kp}";
            _logger.LogInformation("Índices solares actualizados: SFI={Sfi}, Kp={Kp}, Ap={Ap}, SSN={Ssn}",
                indices.Sfi, indices.Kp, indices.Ap, indices.NumeroManchasSolares);
        }
        catch (OperationCanceledException)
        {
            TextoEstado = "Actualización cancelada";
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error al obtener índices: {ex.Message}";
            _logger.LogError(ex, "Error al obtener índices solares");
        }
        finally
        {
            Procesando = false;
        }
    }

    /// <summary>
    /// Consulta las predicciones de propagación para todas las bandas HF.
    /// Usa coordenadas por defecto (centro de España) para predicción general.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand]
    private async Task ActualizarPrediccionesAsync(CancellationToken ct)
    {
        try
        {
            Procesando = true;
            TextoEstado = "Calculando predicciones de propagación...";

            // Coordenadas por defecto: centro de España (Madrid)
            Coordenadas origen = new Coordenadas(40.4168, -3.7038);

            IReadOnlyList<PrediccionBanda> predicciones = await _servicioPropagacion.PredecirPropagacionAsync(
                origen,
                null,
                DateTime.UtcNow,
                ct);

            Dispatcher.UIThread.Post(() =>
            {
                Predicciones.Clear();

                foreach (PrediccionBanda prediccion in predicciones)
                {
                    Predicciones.Add(ConvertirAVm(prediccion));
                }
            });

            TextoEstado = $"Predicciones actualizadas — {predicciones.Count} bandas evaluadas";
            _logger.LogInformation("Predicciones de propagación actualizadas: {CantidadBandas} bandas", predicciones.Count);
        }
        catch (OperationCanceledException)
        {
            TextoEstado = "Actualización cancelada";
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error al calcular predicciones: {ex.Message}";
            _logger.LogError(ex, "Error al calcular predicciones de propagación");
        }
        finally
        {
            Procesando = false;
        }
    }

    /// <summary>
    /// Actualiza tanto los índices solares como las predicciones de banda.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand]
    private async Task ActualizarTodoAsync(CancellationToken ct)
    {
        await ActualizarIndicesAsync(ct);
        await ActualizarPrediccionesAsync(ct);
    }

    /// <summary>
    /// Carga los datos iniciales al crear el ViewModel.
    /// </summary>
    private async Task CargarDatosInicialesAsync()
    {
        try
        {
            await ActualizarIndicesAsync(CancellationToken.None);
            await ActualizarPrediccionesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar datos iniciales de propagación");
            TextoEstado = "Error al cargar datos iniciales";
        }
    }

    /// <summary>
    /// Inicia el timer de auto-actualización cada 30 minutos.
    /// </summary>
    private void IniciarAutoActualizacion()
    {
        _temporizadorAutoActualizacion = DispatcherTimer.Run(() =>
        {
            _ = ActualizarTodoAsync(CancellationToken.None);
            return true;
        }, TimeSpan.FromMinutes(30));
    }

    /// <summary>
    /// Convierte una predicción de dominio a un ViewModel visual.
    /// </summary>
    /// <param name="prediccion">Predicción del dominio.</param>
    /// <returns>Objeto visual para la lista.</returns>
    private static PrediccionBandaVm ConvertirAVm(PrediccionBanda prediccion)
    {
        string colorNivel = ObtenerColorNivel(prediccion.Nivel);
        string mejorHora = $"{prediccion.MejorHoraInicio:hh\\:mm} - {prediccion.MejorHoraFin:hh\\:mm} UTC";
        string regiones = prediccion.RegionesAlcanzables.Count > 0
            ? string.Join(", ", prediccion.RegionesAlcanzables)
            : "—";

        return new PrediccionBandaVm
        {
            NombreBanda = prediccion.Banda.ObtenerNombre(),
            Nivel = prediccion.Nivel.ToString(),
            ColorNivel = colorNivel,
            Descripcion = prediccion.Descripcion,
            MejorHora = mejorHora,
            Regiones = regiones
        };
    }

    /// <summary>
    /// Obtiene el color hexadecimal asociado a un nivel de propagación.
    /// </summary>
    /// <param name="nivel">Nivel de propagación.</param>
    /// <returns>Color hexadecimal.</returns>
    private static string ObtenerColorNivel(NivelPropagacion nivel)
    {
        return nivel switch
        {
            NivelPropagacion.Nulo => "#e94560",       // Rojo
            NivelPropagacion.Pobre => "#ff8c00",      // Naranja
            NivelPropagacion.Regular => "#ffd700",     // Amarillo
            NivelPropagacion.Bueno => "#00c853",       // Verde
            NivelPropagacion.Excelente => "#00e5ff",   // Cyan
            _ => "#888888"
        };
    }

    /// <summary>
    /// Calcula el color del indicador SFI según su valor.
    /// </summary>
    /// <param name="sfi">Valor de SFI.</param>
    /// <returns>Color hexadecimal.</returns>
    private static string CalcularColorSfi(int sfi)
    {
        return sfi switch
        {
            >= 150 => "#00e5ff",    // Excelente — cyan
            >= 120 => "#00c853",    // Bueno — verde
            >= 90 => "#ffd700",     // Regular — amarillo
            >= 70 => "#ff8c00",     // Pobre — naranja
            _ => "#e94560"          // Nulo — rojo
        };
    }

    /// <summary>
    /// Calcula el color del indicador Kp según su valor.
    /// Kp bajo = bueno para propagación.
    /// </summary>
    /// <param name="kp">Valor de Kp.</param>
    /// <returns>Color hexadecimal.</returns>
    private static string CalcularColorKp(int kp)
    {
        return kp switch
        {
            <= 1 => "#00e5ff",     // Tranquilo — cyan
            <= 2 => "#00c853",     // Estable — verde
            <= 3 => "#ffd700",     // Inestable — amarillo
            <= 5 => "#ff8c00",     // Tormenta menor — naranja
            _ => "#e94560"         // Tormenta severa — rojo
        };
    }

    /// <summary>
    /// Calcula el estado geomagnético descriptivo y su color.
    /// </summary>
    /// <param name="indices">Índices solares actuales.</param>
    /// <returns>Tupla con texto descriptivo y color hexadecimal.</returns>
    private static (string Texto, string Color) CalcularEstadoGeomagnetico(IndicesSolares indices)
    {
        if (indices.CondicionesPerturbadas)
        {
            return ("Perturbado", "#e94560");
        }

        if (indices.FlujoSolarAlto)
        {
            return ("Excelente", "#00e5ff");
        }

        if (indices.FlujoSolarBajo)
        {
            return ("Bajo", "#ff8c00");
        }

        return ("Estable", "#00c853");
    }
}
