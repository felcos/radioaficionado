namespace RadioAficionado.Compartido.Contratos;

/// <summary>
/// Comando enviado desde la Web al Servicio local para controlar el rig remotamente.
/// Se serializa como JSON a traves del tunel SignalR.
/// </summary>
/// <param name="Id">Identificador unico del comando para correlacionar respuestas.</param>
/// <param name="Tipo">Tipo de comando a ejecutar.</param>
/// <param name="UsuarioId">ID del usuario que origina el comando (validacion de aislamiento).</param>
/// <param name="Payload">Datos del comando serializados como diccionario clave-valor.</param>
/// <param name="FechaCreacion">Timestamp UTC de cuando se creo el comando.</param>
public sealed record ComandoRemotoRig(
    Guid Id,
    TipoComandoRig Tipo,
    string UsuarioId,
    IReadOnlyDictionary<string, string> Payload,
    DateTime FechaCreacion)
{
    /// <summary>
    /// Crea un comando con ID y fecha generados automaticamente.
    /// </summary>
    public static ComandoRemotoRig Crear(TipoComandoRig tipo, string usuarioId, Dictionary<string, string>? payload = null)
    {
        return new ComandoRemotoRig(
            Guid.NewGuid(),
            tipo,
            usuarioId,
            payload ?? new Dictionary<string, string>(),
            DateTime.UtcNow);
    }
}
