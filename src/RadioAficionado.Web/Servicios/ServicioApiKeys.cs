using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Web.Data;

namespace RadioAficionado.Web.Servicios;

/// <summary>
/// Contrato para la gestion de claves de API utilizadas por el servicio local.
/// </summary>
public interface IServicioApiKeys
{
    /// <summary>
    /// Genera una nueva clave de API para el usuario especificado.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario propietario.</param>
    /// <param name="nombre">Nombre descriptivo de la clave.</param>
    /// <returns>Tupla con la clave en texto plano (mostrar una sola vez) y la entidad persistida.</returns>
    Task<(string ClaveTextoPlano, ClaveApi Entidad)> GenerarClaveAsync(string usuarioId, string nombre);

    /// <summary>
    /// Valida una clave de API en texto plano y devuelve la entidad si es valida.
    /// </summary>
    /// <param name="claveTextoPlano">Clave en texto plano recibida en el header.</param>
    /// <returns>La entidad ClaveApi si la clave es valida y activa, null en caso contrario.</returns>
    Task<ClaveApi?> ValidarClaveAsync(string claveTextoPlano);

    /// <summary>
    /// Desactiva una clave de API existente.
    /// </summary>
    /// <param name="claveId">Identificador de la clave a desactivar.</param>
    /// <param name="usuarioId">Identificador del usuario propietario (verificacion de seguridad).</param>
    Task DesactivarClaveAsync(Guid claveId, string usuarioId);

    /// <summary>
    /// Obtiene todas las claves de API de un usuario.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario.</param>
    /// <returns>Lista de claves del usuario (sin incluir el hash ni el salt).</returns>
    Task<IReadOnlyList<ClaveApi>> ObtenerClavesUsuarioAsync(string usuarioId);
}

/// <summary>
/// Implementacion del servicio de gestion de claves de API.
/// Utiliza SHA-256 con salt aleatorio para almacenar los hashes de forma segura.
/// </summary>
public class ServicioApiKeys(
    ContextoIdentidadRadioAficionado _contexto,
    ILogger<ServicioApiKeys> _logger) : IServicioApiKeys
{
    private const int TamanoSaltBytes = 32;
    private const int TamanoClaveBytes = 48;
    private const int LongitudPrefijo = 8;

    /// <inheritdoc />
    public async Task<(string ClaveTextoPlano, ClaveApi Entidad)> GenerarClaveAsync(string usuarioId, string nombre)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            throw new ArgumentException("El identificador de usuario no puede estar vacio.", nameof(usuarioId));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la clave no puede estar vacio.", nameof(nombre));
        }

        byte[] bytesAleatorios = RandomNumberGenerator.GetBytes(TamanoClaveBytes);
        string claveTextoPlano = Convert.ToBase64String(bytesAleatorios)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        string prefijo = claveTextoPlano[..LongitudPrefijo];

        byte[] saltBytes = RandomNumberGenerator.GetBytes(TamanoSaltBytes);
        string saltBase64 = Convert.ToBase64String(saltBytes);

        string hashBase64 = CalcularHash(claveTextoPlano, saltBytes);

        ClaveApi entidad = new()
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Nombre = nombre.Trim(),
            HashClave = hashBase64,
            Salt = saltBase64,
            Prefijo = prefijo,
            Activa = true,
            FechaCreacion = DateTime.UtcNow,
            FechaUltimoUso = null,
            FechaExpiracion = null
        };

        _contexto.ClavesApi.Add(entidad);
        await _contexto.SaveChangesAsync();

        _logger.LogInformation(
            "Clave de API generada para usuario {UsuarioId} con prefijo {Prefijo}",
            usuarioId,
            prefijo);

        return (claveTextoPlano, entidad);
    }

    /// <inheritdoc />
    public async Task<ClaveApi?> ValidarClaveAsync(string claveTextoPlano)
    {
        if (string.IsNullOrWhiteSpace(claveTextoPlano))
        {
            return null;
        }

        if (claveTextoPlano.Length < LongitudPrefijo)
        {
            return null;
        }

        string prefijo = claveTextoPlano[..LongitudPrefijo];

        List<ClaveApi> candidatas = await _contexto.ClavesApi
            .Include(c => c.Usuario)
            .Where(c => c.Prefijo == prefijo && c.Activa)
            .Where(c => !c.FechaExpiracion.HasValue || c.FechaExpiracion > DateTime.UtcNow)
            .ToListAsync();

        foreach (ClaveApi candidata in candidatas)
        {
            byte[] saltBytes = Convert.FromBase64String(candidata.Salt);
            string hashCalculado = CalcularHash(claveTextoPlano, saltBytes);

            if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hashCalculado),
                Encoding.UTF8.GetBytes(candidata.HashClave)))
            {
                candidata.FechaUltimoUso = DateTime.UtcNow;
                await _contexto.SaveChangesAsync();

                _logger.LogDebug(
                    "Clave de API validada exitosamente para usuario {UsuarioId}, prefijo {Prefijo}",
                    candidata.UsuarioId,
                    candidata.Prefijo);

                return candidata;
            }
        }

        _logger.LogWarning("Intento de validacion de clave de API fallido con prefijo {Prefijo}", prefijo);
        return null;
    }

    /// <inheritdoc />
    public async Task DesactivarClaveAsync(Guid claveId, string usuarioId)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            throw new ArgumentException("El identificador de usuario no puede estar vacio.", nameof(usuarioId));
        }

        ClaveApi? clave = await _contexto.ClavesApi
            .FirstOrDefaultAsync(c => c.Id == claveId && c.UsuarioId == usuarioId);

        if (clave is null)
        {
            _logger.LogWarning(
                "Intento de desactivar clave inexistente {ClaveId} por usuario {UsuarioId}",
                claveId,
                usuarioId);
            return;
        }

        clave.Activa = false;
        await _contexto.SaveChangesAsync();

        _logger.LogInformation(
            "Clave de API {ClaveId} desactivada por usuario {UsuarioId}",
            claveId,
            usuarioId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClaveApi>> ObtenerClavesUsuarioAsync(string usuarioId)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            throw new ArgumentException("El identificador de usuario no puede estar vacio.", nameof(usuarioId));
        }

        List<ClaveApi> claves = await _contexto.ClavesApi
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.FechaCreacion)
            .ToListAsync();

        return claves.AsReadOnly();
    }

    /// <summary>
    /// Calcula el hash SHA-256 de la clave con el salt proporcionado.
    /// </summary>
    private static string CalcularHash(string claveTextoPlano, byte[] saltBytes)
    {
        byte[] claveBytes = Encoding.UTF8.GetBytes(claveTextoPlano);
        byte[] combinado = new byte[saltBytes.Length + claveBytes.Length];
        Buffer.BlockCopy(saltBytes, 0, combinado, 0, saltBytes.Length);
        Buffer.BlockCopy(claveBytes, 0, combinado, saltBytes.Length, claveBytes.Length);

        byte[] hashBytes = SHA256.HashData(combinado);
        return Convert.ToBase64String(hashBytes);
    }
}
