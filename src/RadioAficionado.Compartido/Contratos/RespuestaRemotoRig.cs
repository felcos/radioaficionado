namespace RadioAficionado.Compartido.Contratos;

/// <summary>
/// Respuesta del Servicio local a un comando remoto del rig.
/// Se envia de vuelta al browser a traves del tunel SignalR Web.
/// </summary>
/// <param name="ComandoId">ID del comando al que responde.</param>
/// <param name="Exitoso">Si el comando se ejecuto correctamente.</param>
/// <param name="MensajeError">Mensaje de error si no fue exitoso.</param>
/// <param name="Datos">Datos de respuesta (ej: estado actual del rig) como diccionario.</param>
/// <param name="FechaRespuesta">Timestamp UTC de la respuesta.</param>
public sealed record RespuestaRemotoRig(
    Guid ComandoId,
    bool Exitoso,
    string? MensajeError,
    IReadOnlyDictionary<string, string>? Datos,
    DateTime FechaRespuesta)
{
    /// <summary>
    /// Crea una respuesta exitosa.
    /// </summary>
    public static RespuestaRemotoRig Exito(Guid comandoId, Dictionary<string, string>? datos = null)
    {
        return new RespuestaRemotoRig(comandoId, true, null, datos, DateTime.UtcNow);
    }

    /// <summary>
    /// Crea una respuesta de error.
    /// </summary>
    public static RespuestaRemotoRig Error(Guid comandoId, string mensajeError)
    {
        return new RespuestaRemotoRig(comandoId, false, mensajeError, null, DateTime.UtcNow);
    }
}
