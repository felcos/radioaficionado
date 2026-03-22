using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// Elemento visual que representa un spot DX en la lista del panel.
/// </summary>
public sealed class SpotDxVm
{
    /// <summary>Hora UTC formateada (HH:mm).</summary>
    public string Hora { get; init; } = string.Empty;

    /// <summary>Indicativo del spotter que publicó el spot.</summary>
    public string IndicativoSpotter { get; init; } = string.Empty;

    /// <summary>Indicativo de la estación DX spotteada.</summary>
    public string IndicativoDx { get; init; } = string.Empty;

    /// <summary>Frecuencia formateada para mostrar (ej: "14.074 MHz").</summary>
    public string FrecuenciaDisplay { get; init; } = string.Empty;

    /// <summary>Banda de radioaficionado inferida de la frecuencia.</summary>
    public string Banda { get; init; } = string.Empty;

    /// <summary>Modo de operación inferido del comentario si es posible.</summary>
    public string Modo { get; init; } = string.Empty;

    /// <summary>Comentario libre del spotter.</summary>
    public string Comentario { get; init; } = string.Empty;

    /// <summary>Frecuencia original para poder sintonizar la radio.</summary>
    public Frecuencia FrecuenciaOriginal { get; init; }
}

/// <summary>
/// Representa un servidor DX Cluster disponible para conexión.
/// </summary>
public sealed class ServidorDxClusterVm
{
    /// <summary>Dirección del servidor.</summary>
    public string Servidor { get; init; } = string.Empty;

    /// <summary>Puerto TCP del servidor.</summary>
    public int Puerto { get; init; }

    /// <summary>Descripción legible del servidor.</summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>Texto combinado para mostrar en el ComboBox.</summary>
    public string DisplayText => $"{Servidor}:{Puerto} — {Descripcion}";
}

/// <summary>
/// ViewModel del panel de DX Cluster que muestra spots DX en tiempo real.
/// Permite conectarse a servidores DX Cluster, filtrar spots y sintonizar frecuencias.
/// </summary>
public partial class PanelDxClusterViewModel : ViewModelBase, IDisposable
{
    private const int MaximoSpots = 200;

    private readonly IDxCluster _dxCluster;
    private readonly ILogger<PanelDxClusterViewModel> _logger;
    private bool _disposed;

    /// <summary>
    /// Colección observable de spots DX visibles (filtrados).
    /// </summary>
    public ObservableCollection<SpotDxVm> SpotsVisibles { get; } = [];

    /// <summary>
    /// Lista completa de spots sin filtrar (FIFO, máximo 200).
    /// </summary>
    private readonly List<SpotDxVm> _todosLosSpots = [];

    /// <summary>
    /// Servidores DX Cluster disponibles para conexión.
    /// </summary>
    public ObservableCollection<ServidorDxClusterVm> Servidores { get; } = [];

    /// <summary>
    /// Indica si está conectado al servidor DX Cluster.
    /// </summary>
    [ObservableProperty]
    private bool _conectado;

    /// <summary>
    /// Servidor DX Cluster seleccionado actualmente.
    /// </summary>
    [ObservableProperty]
    private ServidorDxClusterVm? _servidorSeleccionado;

    /// <summary>
    /// Indicativo propio para autenticarse en el cluster.
    /// </summary>
    [ObservableProperty]
    private string _indicativoPropio = string.Empty;

    /// <summary>
    /// Filtro por indicativo (spotter o DX). Vacío muestra todos.
    /// </summary>
    [ObservableProperty]
    private string _filtroIndicativo = string.Empty;

    /// <summary>
    /// Filtro por banda. Vacío muestra todas las bandas.
    /// </summary>
    [ObservableProperty]
    private string _filtroBanda = string.Empty;

    /// <summary>
    /// Texto del botón de conexión (Conectar/Desconectar).
    /// </summary>
    [ObservableProperty]
    private string _textoBotonConexion = "Conectar";

    /// <summary>
    /// Texto de estado mostrado en la barra inferior del panel.
    /// </summary>
    [ObservableProperty]
    private string _textoEstado = "Desconectado";

    /// <summary>
    /// Cantidad total de spots recibidos.
    /// </summary>
    [ObservableProperty]
    private int _totalSpots;

    /// <summary>
    /// Bandas disponibles para filtrar.
    /// </summary>
    public ObservableCollection<string> BandasDisponibles { get; } =
    [
        "",
        "160m", "80m", "60m", "40m", "30m", "20m",
        "17m", "15m", "12m", "10m", "6m", "2m", "70cm"
    ];

    /// <summary>
    /// Crea el ViewModel del panel DX Cluster con las dependencias inyectadas.
    /// </summary>
    /// <param name="dxCluster">Cliente de DX Cluster para la conexión Telnet.</param>
    /// <param name="logger">Logger para registro de eventos.</param>
    public PanelDxClusterViewModel(IDxCluster dxCluster, ILogger<PanelDxClusterViewModel> logger)
    {
        _dxCluster = dxCluster ?? throw new ArgumentNullException(nameof(dxCluster));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        CargarServidores();
        SuscribirEventos();
    }

    /// <summary>
    /// Conecta al servidor DX Cluster seleccionado.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand(CanExecute = nameof(PuedeConectar))]
    private async Task ConectarAsync(CancellationToken ct)
    {
        if (ServidorSeleccionado is null)
        {
            return;
        }

        try
        {
            TextoEstado = $"Conectando a {ServidorSeleccionado.Servidor}:{ServidorSeleccionado.Puerto}...";
            _logger.LogInformation("Conectando al DX Cluster {Servidor}:{Puerto} como {Indicativo}",
                ServidorSeleccionado.Servidor, ServidorSeleccionado.Puerto, IndicativoPropio);

            await _dxCluster.ConectarAsync(
                ServidorSeleccionado.Servidor,
                ServidorSeleccionado.Puerto,
                IndicativoPropio,
                ct);

            Conectado = true;
            TextoBotonConexion = "Desconectar";
            TextoEstado = $"Conectado a {ServidorSeleccionado.Servidor}:{ServidorSeleccionado.Puerto}";
            _logger.LogInformation("Conectado exitosamente al DX Cluster");
        }
        catch (OperationCanceledException)
        {
            TextoEstado = "Conexión cancelada";
            _logger.LogInformation("Conexión al DX Cluster cancelada por el usuario");
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error: {ex.Message}";
            _logger.LogError(ex, "Error al conectar al DX Cluster");
        }
    }

    /// <summary>
    /// Desconecta del servidor DX Cluster actual.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand(CanExecute = nameof(PuedeDesconectar))]
    private async Task DesconectarAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Desconectando del DX Cluster");
            await _dxCluster.DesconectarAsync(ct);

            Conectado = false;
            TextoBotonConexion = "Conectar";
            TextoEstado = "Desconectado";
            _logger.LogInformation("Desconectado del DX Cluster");
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error al desconectar: {ex.Message}";
            _logger.LogError(ex, "Error al desconectar del DX Cluster");
        }
    }

    /// <summary>
    /// Alterna entre conectar y desconectar según el estado actual.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand]
    private async Task AlternarConexionAsync(CancellationToken ct)
    {
        if (Conectado)
        {
            await DesconectarAsync(ct);
        }
        else
        {
            await ConectarAsync(ct);
        }
    }

    /// <summary>
    /// Sintoniza la radio en la frecuencia del spot seleccionado.
    /// Preparado para integración futura con IControlRig.
    /// </summary>
    /// <param name="spot">Spot DX cuya frecuencia se quiere sintonizar.</param>
    [RelayCommand]
    private void SintonizarSpot(SpotDxVm? spot)
    {
        if (spot is null)
        {
            return;
        }

        _logger.LogInformation("Sintonizar solicitado: {Dx} en {Frecuencia}",
            spot.IndicativoDx, spot.FrecuenciaDisplay);

        // TODO: Integrar con IControlRig cuando esté disponible
        // await _controlRig.EstablecerFrecuenciaAsync(spot.FrecuenciaOriginal);
        TextoEstado = $"Sintonizar: {spot.IndicativoDx} — {spot.FrecuenciaDisplay}";
    }

    /// <summary>
    /// Limpia todos los spots del panel.
    /// </summary>
    [RelayCommand]
    private void LimpiarSpots()
    {
        _todosLosSpots.Clear();
        SpotsVisibles.Clear();
        TotalSpots = 0;
        _logger.LogInformation("Spots limpiados manualmente");
    }

    /// <summary>
    /// Determina si se puede conectar al cluster.
    /// </summary>
    private bool PuedeConectar => !Conectado
                                  && ServidorSeleccionado is not null
                                  && !string.IsNullOrWhiteSpace(IndicativoPropio);

    /// <summary>
    /// Determina si se puede desconectar del cluster.
    /// </summary>
    private bool PuedeDesconectar => Conectado;

    partial void OnConectadoChanged(bool value)
    {
        ConectarCommand.NotifyCanExecuteChanged();
        DesconectarCommand.NotifyCanExecuteChanged();
    }

    partial void OnServidorSeleccionadoChanged(ServidorDxClusterVm? value)
    {
        ConectarCommand.NotifyCanExecuteChanged();
    }

    partial void OnIndicativoPropioChanged(string value)
    {
        ConectarCommand.NotifyCanExecuteChanged();
    }

    partial void OnFiltroIndicativoChanged(string value)
    {
        AplicarFiltros();
    }

    partial void OnFiltroBandaChanged(string value)
    {
        AplicarFiltros();
    }

    /// <summary>
    /// Carga la lista de servidores DX Cluster conocidos.
    /// </summary>
    private void CargarServidores()
    {
        foreach ((string servidor, int puerto, string descripcion) in ConfiguracionDxCluster.ServidoresConocidos)
        {
            Servidores.Add(new ServidorDxClusterVm
            {
                Servidor = servidor,
                Puerto = puerto,
                Descripcion = descripcion
            });
        }

        if (Servidores.Count > 0)
        {
            ServidorSeleccionado = Servidores[0];
        }
    }

    /// <summary>
    /// Suscribe los eventos del cliente DX Cluster.
    /// </summary>
    private void SuscribirEventos()
    {
        _dxCluster.SpotRecibido += AlRecibirSpot;
        _dxCluster.ConexionPerdida += AlPerderConexion;
        _dxCluster.Reconectado += AlReconectar;
    }

    /// <summary>
    /// Desuscribe los eventos del cliente DX Cluster.
    /// </summary>
    private void DesuscribirEventos()
    {
        _dxCluster.SpotRecibido -= AlRecibirSpot;
        _dxCluster.ConexionPerdida -= AlPerderConexion;
        _dxCluster.Reconectado -= AlReconectar;
    }

    private void AlRecibirSpot(object? sender, SpotDx spot)
    {
        SpotDxVm spotVm = ConvertirAVm(spot);

        Dispatcher.UIThread.Post(() =>
        {
            _todosLosSpots.Insert(0, spotVm);

            // FIFO: mantener máximo 200 spots
            while (_todosLosSpots.Count > MaximoSpots)
            {
                _todosLosSpots.RemoveAt(_todosLosSpots.Count - 1);
            }

            TotalSpots = _todosLosSpots.Count;

            if (CumpleFiltro(spotVm))
            {
                SpotsVisibles.Insert(0, spotVm);

                // Mantener la lista visible también acotada
                while (SpotsVisibles.Count > MaximoSpots)
                {
                    SpotsVisibles.RemoveAt(SpotsVisibles.Count - 1);
                }
            }
        });
    }

    private void AlPerderConexion(object? sender, string motivo)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Conectado = false;
            TextoBotonConexion = "Conectar";
            TextoEstado = $"Conexión perdida: {motivo}";
        });

        _logger.LogWarning("Conexión con DX Cluster perdida: {Motivo}", motivo);
    }

    private void AlReconectar(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Conectado = true;
            TextoBotonConexion = "Desconectar";
            TextoEstado = "Reconectado exitosamente";
        });

        _logger.LogInformation("Reconectado al DX Cluster exitosamente");
    }

    /// <summary>
    /// Convierte un SpotDx del dominio a un SpotDxVm para la vista.
    /// </summary>
    private static SpotDxVm ConvertirAVm(SpotDx spot)
    {
        BandaRadio? banda = spot.Frecuencia.ObtenerBanda();
        string modoInferido = InferirModo(spot.Comentario);

        return new SpotDxVm
        {
            Hora = spot.Hora.ToString("HH:mm"),
            IndicativoSpotter = spot.Spotteador.Valor,
            IndicativoDx = spot.Dx.Valor,
            FrecuenciaDisplay = spot.Frecuencia.ToString(),
            Banda = banda?.ObtenerNombre() ?? "Desconocida",
            Modo = modoInferido,
            Comentario = spot.Comentario,
            FrecuenciaOriginal = spot.Frecuencia
        };
    }

    /// <summary>
    /// Intenta inferir el modo de operación a partir del comentario del spot.
    /// </summary>
    private static string InferirModo(string comentario)
    {
        if (string.IsNullOrWhiteSpace(comentario))
        {
            return string.Empty;
        }

        string comentarioUpper = comentario.ToUpperInvariant();

        if (comentarioUpper.Contains("FT8"))
        {
            return "FT8";
        }

        if (comentarioUpper.Contains("FT4"))
        {
            return "FT4";
        }

        if (comentarioUpper.Contains("CW"))
        {
            return "CW";
        }

        if (comentarioUpper.Contains("SSB") || comentarioUpper.Contains("LSB") || comentarioUpper.Contains("USB"))
        {
            return "SSB";
        }

        if (comentarioUpper.Contains("RTTY"))
        {
            return "RTTY";
        }

        if (comentarioUpper.Contains("PSK"))
        {
            return "PSK";
        }

        if (comentarioUpper.Contains("JT65"))
        {
            return "JT65";
        }

        if (comentarioUpper.Contains("JT9"))
        {
            return "JT9";
        }

        if (comentarioUpper.Contains("WSPR"))
        {
            return "WSPR";
        }

        if (comentarioUpper.Contains("FM"))
        {
            return "FM";
        }

        if (comentarioUpper.Contains("AM"))
        {
            return "AM";
        }

        if (comentarioUpper.Contains("DIGI") || comentarioUpper.Contains("DATA"))
        {
            return "Digital";
        }

        return string.Empty;
    }

    /// <summary>
    /// Aplica los filtros de indicativo y banda a la lista visible.
    /// </summary>
    private void AplicarFiltros()
    {
        SpotsVisibles.Clear();

        foreach (SpotDxVm spot in _todosLosSpots)
        {
            if (CumpleFiltro(spot))
            {
                SpotsVisibles.Add(spot);
            }
        }
    }

    /// <summary>
    /// Determina si un spot cumple con los filtros activos.
    /// </summary>
    private bool CumpleFiltro(SpotDxVm spot)
    {
        // Filtro por indicativo
        if (!string.IsNullOrWhiteSpace(FiltroIndicativo))
        {
            string filtro = FiltroIndicativo.ToUpperInvariant();
            bool coincide = spot.IndicativoDx.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                            || spot.IndicativoSpotter.Contains(filtro, StringComparison.OrdinalIgnoreCase);

            if (!coincide)
            {
                return false;
            }
        }

        // Filtro por banda
        if (!string.IsNullOrWhiteSpace(FiltroBanda))
        {
            if (!spot.Banda.Contains(FiltroBanda, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Libera los recursos y desuscribe eventos.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        DesuscribirEventos();
        _disposed = true;
    }
}
