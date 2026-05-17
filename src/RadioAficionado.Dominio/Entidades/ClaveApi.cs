namespace RadioAficionado.Dominio.Entidades;

/// <summary>
/// Clave API que permite a un RadioAficionado.Servicio local autenticarse
/// contra RadioAficionado.Web para establecer el tunel de control remoto.
/// Cada usuario puede tener multiples claves (ej: una por estacion).
/// </summary>
public class ClaveApi
{
    /// <summary>Identificador unico de la clave.</summary>
    public Guid Id { get; set; }

    /// <summary>ID del usuario propietario de la clave.</summary>
    public string UsuarioId { get; set; } = string.Empty;

    /// <summary>Nombre descriptivo de la clave (ej: "Estacion casa", "Portatil").</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Hash SHA-256 de la clave API (nunca se almacena en texto plano).</summary>
    public string HashClave { get; set; } = string.Empty;

    /// <summary>Salt usado para el hash.</summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>Prefijo de la clave (primeros 8 caracteres) para identificacion visual.</summary>
    public string Prefijo { get; set; } = string.Empty;

    /// <summary>Si la clave esta activa. Claves desactivadas no pueden autenticarse.</summary>
    public bool Activa { get; set; } = true;

    /// <summary>Fecha de creacion de la clave (UTC).</summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>Fecha del ultimo uso exitoso (UTC). Null si nunca se ha usado.</summary>
    public DateTime? FechaUltimoUso { get; set; }

    /// <summary>Fecha de expiracion (UTC). Null si no expira.</summary>
    public DateTime? FechaExpiracion { get; set; }

    /// <summary>Navegacion al usuario propietario.</summary>
    public UsuarioRadio? Usuario { get; set; }
}
