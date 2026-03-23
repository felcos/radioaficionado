using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Escritorio.ViewModels;

/// <summary>
/// ViewModel que expone el estado de sincronización a la interfaz de usuario.
/// Muestra indicador de estado, QSOs pendientes y permite sincronizar manualmente.
/// </summary>
public partial class EstadoSincronizacionViewModel : ViewModelBase
{
    private readonly IServicioSincronizacion _servicioSincronizacion;

    /// <summary>
    /// Texto descriptivo del estado actual de sincronización.
    /// Ejemplo: "Sincronizado", "Pendiente (5 QSOs)", "Error".
    /// </summary>
    [ObservableProperty]
    private string _textoEstado = "Sin configurar";

    /// <summary>
    /// Indica si la sincronización está en curso.
    /// </summary>
    [ObservableProperty]
    private bool _sincronizando;

    /// <summary>
    /// Última fecha de sincronización formateada para mostrar en la UI.
    /// </summary>
    [ObservableProperty]
    private string _ultimaSincronizacion = "Nunca";

    /// <summary>
    /// Indica si el servidor está accesible.
    /// </summary>
    [ObservableProperty]
    private bool _conexionActiva;

    /// <summary>
    /// Cantidad de QSOs pendientes de sincronizar.
    /// </summary>
    [ObservableProperty]
    private int _qsosPendientes;

    /// <summary>
    /// Último mensaje de error, si lo hubo.
    /// </summary>
    [ObservableProperty]
    private string? _ultimoError;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="EstadoSincronizacionViewModel"/>.
    /// </summary>
    /// <param name="servicioSincronizacion">Servicio de sincronización inyectado.</param>
    public EstadoSincronizacionViewModel(IServicioSincronizacion servicioSincronizacion)
    {
        ArgumentNullException.ThrowIfNull(servicioSincronizacion);
        _servicioSincronizacion = servicioSincronizacion;
    }

    /// <summary>
    /// Ejecuta una sincronización manual inmediata.
    /// </summary>
    [RelayCommand(CanExecute = nameof(PuedeSincronizar))]
    private async Task SincronizarAhoraAsync()
    {
        Sincronizando = true;
        UltimoError = null;
        TextoEstado = "Sincronizando...";

        try
        {
            ResultadoSincronizacion resultado = await _servicioSincronizacion.SincronizarAsync();

            if (resultado.Errores.Count > 0)
            {
                UltimoError = string.Join("; ", resultado.Errores);
                TextoEstado = "Error";
            }
            else
            {
                TextoEstado = "Sincronizado";
            }

            UltimaSincronizacion = resultado.FechaSincronizacion.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        }
        catch (Exception ex)
        {
            UltimoError = ex.Message;
            TextoEstado = "Error";
        }
        finally
        {
            Sincronizando = false;
            await ActualizarEstadoAsync();
        }
    }

    /// <summary>
    /// Indica si se puede ejecutar el comando de sincronización.
    /// </summary>
    /// <returns>True si no hay una sincronización en curso.</returns>
    private bool PuedeSincronizar() => !Sincronizando;

    /// <summary>
    /// Actualiza las propiedades de estado consultando el servicio de sincronización.
    /// </summary>
    public async Task ActualizarEstadoAsync()
    {
        try
        {
            EstadoSincronizacion estado = await _servicioSincronizacion.ObtenerEstadoAsync();

            ConexionActiva = estado.ConexionActiva;
            QsosPendientes = estado.QsosPendientesSincronizar;

            if (estado.UltimaSincronizacion.HasValue)
            {
                UltimaSincronizacion = estado.UltimaSincronizacion.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            }

            if (!Sincronizando)
            {
                if (estado.QsosPendientesSincronizar > 0)
                {
                    TextoEstado = $"Pendiente ({estado.QsosPendientesSincronizar} QSOs)";
                }
                else if (estado.UltimaSincronizacion.HasValue)
                {
                    TextoEstado = "Sincronizado";
                }
            }
        }
        catch (Exception ex)
        {
            UltimoError = ex.Message;
            TextoEstado = "Error";
        }
    }
}
