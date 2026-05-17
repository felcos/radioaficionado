namespace RadioAficionado.Compartido.Contratos;

/// <summary>
/// Tipos de comando que se pueden enviar al rig de forma remota.
/// </summary>
public enum TipoComandoRig
{
    /// <summary>Cambiar frecuencia del VFO activo.</summary>
    CambiarFrecuencia,

    /// <summary>Cambiar modo de operacion (USB, LSB, FT8, etc.).</summary>
    CambiarModo,

    /// <summary>Cambiar banda activa.</summary>
    CambiarBanda,

    /// <summary>Activar o desactivar PTT.</summary>
    CambiarPtt,

    /// <summary>Cambiar VFO activo (A/B).</summary>
    CambiarVfo,

    /// <summary>Conectar al radio con configuracion especifica.</summary>
    Conectar,

    /// <summary>Desconectar del radio.</summary>
    Desconectar,

    /// <summary>Solicitar estado actual del rig.</summary>
    ObtenerEstado,

    /// <summary>Cambiar potencia de transmision.</summary>
    CambiarPotencia,

    /// <summary>Activar o desactivar modo split.</summary>
    CambiarSplit
}
