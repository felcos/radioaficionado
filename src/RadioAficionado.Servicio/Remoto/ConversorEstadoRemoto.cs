using RadioAficionado.Compartido.Contratos;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.Servicio.Remoto;

/// <summary>
/// Convierte el estado local del rig al DTO compartido para comunicacion remota.
/// </summary>
public static class ConversorEstadoRemoto
{
    /// <summary>
    /// Convierte un <see cref="EstadoRig"/> local junto con el estado de conexion
    /// a un <see cref="EstadoRigRemotoDto"/> compartido para envio via SignalR.
    /// </summary>
    /// <param name="estadoLocal">Estado actual del rig obtenido de IControlRig.</param>
    /// <param name="conectado">Indica si el rig esta conectado.</param>
    /// <returns>DTO compartido con el estado del rig para envio remoto.</returns>
    public static EstadoRigRemotoDto ConvertirARemoto(EstadoRig estadoLocal, bool conectado)
    {
        ArgumentNullException.ThrowIfNull(estadoLocal);

        return new EstadoRigRemotoDto(
            FrecuenciaHz: estadoLocal.Frecuencia.Hz,
            FrecuenciaDisplay: estadoLocal.Frecuencia.ToString(),
            Modo: estadoLocal.Modo.ToString(),
            Banda: estadoLocal.Frecuencia.ObtenerBanda()?.ToString() ?? string.Empty,
            NivelSenal: estadoLocal.NivelSenal,
            Transmitiendo: estadoLocal.Transmitiendo,
            VfoActivo: estadoLocal.VfoActivo,
            PotenciaVatios: estadoLocal.PotenciaVatios,
            SplitActivo: estadoLocal.SplitActivo,
            Conectado: conectado,
            Swr: estadoLocal.Swr,
            Alc: estadoLocal.Alc);
    }
}
