using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RadioAficionado.Dominio.Aprs;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// Elemento visual que representa un paquete APRS recibido en la lista del panel.
/// </summary>
public sealed class PaqueteAprsVm
{
    /// <summary>Marca de tiempo formateada (HH:mm:ss).</summary>
    public string Hora { get; init; } = string.Empty;

    /// <summary>Indicativo de origen del paquete.</summary>
    public string Origen { get; init; } = string.Empty;

    /// <summary>Tipo de paquete APRS.</summary>
    public string Tipo { get; init; } = string.Empty;

    /// <summary>Contenido crudo del paquete.</summary>
    public string Contenido { get; init; } = string.Empty;

    /// <summary>Ruta de digipeaters formateada.</summary>
    public string Ruta { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel del panel de APRS (Automatic Packet Reporting System).
/// Permite conectarse a un servidor APRS-IS, recibir paquetes en tiempo real,
/// filtrar la información y enviar reportes de posición.
/// </summary>
public partial class PanelAprsViewModel : ViewModelBase, IDisposable
{
    private const int MaximoPaquetes = 100;

    private readonly IServicioAprs _servicioAprs;
    private readonly ILogger<PanelAprsViewModel> _logger;
    private bool _disposed;

    /// <summary>
    /// Colección observable de paquetes APRS recibidos (FIFO, máximo 100).
    /// </summary>
    public ObservableCollection<PaqueteAprsVm> PaquetesRecibidos { get; } = [];

    /// <summary>
    /// Indica si el cliente está conectado al servidor APRS-IS.
    /// </summary>
    [ObservableProperty]
    private bool _conectado;

    /// <summary>
    /// Dirección del servidor APRS-IS.
    /// </summary>
    [ObservableProperty]
    private string _servidorAprs = "rotate.aprs2.net";

    /// <summary>
    /// Puerto TCP del servidor APRS-IS.
    /// </summary>
    [ObservableProperty]
    private int _puertoAprs = 14580;

    /// <summary>
    /// Indicativo propio para autenticarse en APRS-IS.
    /// </summary>
    [ObservableProperty]
    private string _indicativoPropio = string.Empty;

    /// <summary>
    /// Código de verificación APRS (passcode).
    /// </summary>
    [ObservableProperty]
    private int _passcode = -1;

    /// <summary>
    /// Filtro de servidor APRS-IS (ej: "r/40.41/-3.70/200").
    /// </summary>
    [ObservableProperty]
    private string _filtro = string.Empty;

    /// <summary>
    /// Texto de filtrado local para buscar en los paquetes recibidos.
    /// </summary>
    [ObservableProperty]
    private string _filtroLocal = string.Empty;

    /// <summary>
    /// Texto del botón de conexión (Conectar/Desconectar).
    /// </summary>
    [ObservableProperty]
    private string _textoBotonConexion = "Conectar";

    /// <summary>
    /// Texto de estado del panel.
    /// </summary>
    [ObservableProperty]
    private string _textoEstado = "Desconectado";

    /// <summary>
    /// Latitud del observador para envío de posición.
    /// </summary>
    [ObservableProperty]
    private double _latitudObservador = 40.4168;

    /// <summary>
    /// Longitud del observador para envío de posición.
    /// </summary>
    [ObservableProperty]
    private double _longitudObservador = -3.7038;

    /// <summary>
    /// Comentario para el envío de posición.
    /// </summary>
    [ObservableProperty]
    private string _comentarioPosicion = "RadioAficionado App";

    /// <summary>
    /// Cantidad total de paquetes recibidos.
    /// </summary>
    [ObservableProperty]
    private int _totalPaquetes;

    /// <summary>
    /// Crea el ViewModel del panel APRS con las dependencias inyectadas.
    /// </summary>
    /// <param name="servicioAprs">Servicio de comunicación APRS-IS.</param>
    /// <param name="logger">Logger para registro de eventos.</param>
    public PanelAprsViewModel(
        IServicioAprs servicioAprs,
        ILogger<PanelAprsViewModel> logger)
    {
        _servicioAprs = servicioAprs ?? throw new ArgumentNullException(nameof(servicioAprs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _servicioAprs.PaqueteRecibido += AlRecibirPaquete;
    }

    /// <summary>
    /// Conecta al servidor APRS-IS con la configuración actual.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand(CanExecute = nameof(PuedeConectar))]
    private async Task ConectarAsync(CancellationToken ct)
    {
        try
        {
            TextoEstado = $"Conectando a {ServidorAprs}:{PuertoAprs}...";
            _logger.LogInformation("Conectando a APRS-IS {Servidor}:{Puerto} como {Indicativo}",
                ServidorAprs, PuertoAprs, IndicativoPropio);

            ConfiguracionAprs configuracion = new ConfiguracionAprs(
                ServidorAprs,
                PuertoAprs,
                IndicativoPropio,
                Passcode,
                string.IsNullOrWhiteSpace(Filtro) ? null : Filtro);

            await _servicioAprs.ConectarAsync(configuracion, ct);

            Conectado = true;
            TextoBotonConexion = "Desconectar";
            TextoEstado = $"Conectado a {ServidorAprs}:{PuertoAprs}";
            _logger.LogInformation("Conectado exitosamente a APRS-IS");
        }
        catch (OperationCanceledException)
        {
            TextoEstado = "Conexión cancelada";
            _logger.LogInformation("Conexión a APRS-IS cancelada por el usuario");
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error: {ex.Message}";
            _logger.LogError(ex, "Error al conectar a APRS-IS");
        }
    }

    /// <summary>
    /// Desconecta del servidor APRS-IS.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand(CanExecute = nameof(PuedeDesconectar))]
    private async Task DesconectarAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Desconectando de APRS-IS");
            await _servicioAprs.DesconectarAsync();

            Conectado = false;
            TextoBotonConexion = "Conectar";
            TextoEstado = "Desconectado";
            _logger.LogInformation("Desconectado de APRS-IS");
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error al desconectar: {ex.Message}";
            _logger.LogError(ex, "Error al desconectar de APRS-IS");
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
    /// Envía un reporte de posición al servidor APRS-IS.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    [RelayCommand(CanExecute = nameof(PuedeEnviarPosicion))]
    private async Task EnviarPosicionAsync(CancellationToken ct)
    {
        try
        {
            Coordenadas coordenadas = new Coordenadas(LatitudObservador, LongitudObservador);

            _logger.LogInformation("Enviando posición APRS: {Coordenadas} — {Comentario}",
                coordenadas, ComentarioPosicion);

            await _servicioAprs.EnviarPosicionAsync(coordenadas, ComentarioPosicion, ct);

            TextoEstado = $"Posición enviada: {coordenadas}";
            _logger.LogInformation("Posición APRS enviada exitosamente");
        }
        catch (OperationCanceledException)
        {
            TextoEstado = "Envío cancelado";
        }
        catch (Exception ex)
        {
            TextoEstado = $"Error al enviar posición: {ex.Message}";
            _logger.LogError(ex, "Error al enviar posición APRS");
        }
    }

    /// <summary>
    /// Limpia todos los paquetes recibidos.
    /// </summary>
    [RelayCommand]
    private void LimpiarPaquetes()
    {
        PaquetesRecibidos.Clear();
        TotalPaquetes = 0;
        _logger.LogInformation("Paquetes APRS limpiados manualmente");
    }

    /// <summary>
    /// Determina si se puede conectar al servidor APRS-IS.
    /// </summary>
    private bool PuedeConectar => !Conectado
                                   && !string.IsNullOrWhiteSpace(ServidorAprs)
                                   && !string.IsNullOrWhiteSpace(IndicativoPropio);

    /// <summary>
    /// Determina si se puede desconectar del servidor APRS-IS.
    /// </summary>
    private bool PuedeDesconectar => Conectado;

    /// <summary>
    /// Determina si se puede enviar una posición.
    /// </summary>
    private bool PuedeEnviarPosicion => Conectado;

    /// <summary>
    /// Reacciona al cambio de estado de conexión notificando los comandos.
    /// </summary>
    partial void OnConectadoChanged(bool value)
    {
        ConectarCommand.NotifyCanExecuteChanged();
        DesconectarCommand.NotifyCanExecuteChanged();
        EnviarPosicionCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Reacciona al cambio de indicativo notificando el comando de conexión.
    /// </summary>
    partial void OnIndicativoPropioChanged(string value)
    {
        ConectarCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Reacciona al cambio de servidor notificando el comando de conexión.
    /// </summary>
    partial void OnServidorAprsChanged(string value)
    {
        ConectarCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Maneja la recepción de un paquete APRS desde el servicio.
    /// </summary>
    /// <param name="paquete">Paquete APRS recibido.</param>
    private void AlRecibirPaquete(PaqueteAprs paquete)
    {
        PaqueteAprsVm paqueteVm = ConvertirAVm(paquete);

        Dispatcher.UIThread.Post(() =>
        {
            // Filtro local: si hay filtro activo, solo mostrar si coincide
            if (!string.IsNullOrWhiteSpace(FiltroLocal))
            {
                if (!CumpleFiltroLocal(paqueteVm))
                {
                    return;
                }
            }

            PaquetesRecibidos.Insert(0, paqueteVm);

            // FIFO: mantener máximo 100 paquetes
            while (PaquetesRecibidos.Count > MaximoPaquetes)
            {
                PaquetesRecibidos.RemoveAt(PaquetesRecibidos.Count - 1);
            }

            TotalPaquetes = PaquetesRecibidos.Count;
        });
    }

    /// <summary>
    /// Determina si un paquete cumple con el filtro local.
    /// </summary>
    /// <param name="paquete">Paquete a verificar.</param>
    /// <returns>True si cumple el filtro.</returns>
    private bool CumpleFiltroLocal(PaqueteAprsVm paquete)
    {
        if (string.IsNullOrWhiteSpace(FiltroLocal))
        {
            return true;
        }

        return paquete.Origen.Contains(FiltroLocal, StringComparison.OrdinalIgnoreCase)
               || paquete.Contenido.Contains(FiltroLocal, StringComparison.OrdinalIgnoreCase)
               || paquete.Tipo.Contains(FiltroLocal, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Convierte un PaqueteAprs del dominio a un PaqueteAprsVm para la vista.
    /// </summary>
    /// <param name="paquete">Paquete del dominio.</param>
    /// <returns>ViewModel del paquete para la vista.</returns>
    private static PaqueteAprsVm ConvertirAVm(PaqueteAprs paquete)
    {
        return new PaqueteAprsVm
        {
            Hora = paquete.Timestamp.ToString("HH:mm:ss"),
            Origen = paquete.Origen.Valor,
            Tipo = ObtenerNombreTipo(paquete.TipoPaquete),
            Contenido = paquete.Contenido,
            Ruta = string.Join(",", paquete.Ruta)
        };
    }

    /// <summary>
    /// Obtiene el nombre legible del tipo de paquete APRS.
    /// </summary>
    /// <param name="tipo">Tipo de paquete.</param>
    /// <returns>Nombre legible en español.</returns>
    private static string ObtenerNombreTipo(TipoPaqueteAprs tipo)
    {
        return tipo switch
        {
            TipoPaqueteAprs.Posicion => "Posición",
            TipoPaqueteAprs.Mensaje => "Mensaje",
            TipoPaqueteAprs.Objeto => "Objeto",
            TipoPaqueteAprs.Estacion => "Estación",
            TipoPaqueteAprs.Telemetria => "Telemetría",
            TipoPaqueteAprs.Estado => "Estado",
            TipoPaqueteAprs.Consulta => "Consulta",
            _ => "Desconocido"
        };
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

        _servicioAprs.PaqueteRecibido -= AlRecibirPaquete;
        _disposed = true;
    }
}
