using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Nativo.ModosDigitales.Ft8;

namespace RadioAficionado.Nativo.ModosDigitales;

/// <summary>
/// Extensiones para registrar los servicios de modos digitales en el contenedor de DI.
/// </summary>
public static class ConfiguracionServiciosModosDigitales
{
    /// <summary>
    /// Registra los decodificadores de modos digitales en el contenedor de DI.
    /// </summary>
    /// <param name="servicios">Colección de servicios.</param>
    /// <param name="configuracionFt8">Configuración opcional para FT8. Si es null, usa valores por defecto.</param>
    /// <returns>La colección de servicios para encadenar llamadas.</returns>
    public static IServiceCollection AgregarModosDigitales(
        this IServiceCollection servicios,
        ConfiguracionFt8? configuracionFt8 = null)
    {
        ConfiguracionFt8 config = configuracionFt8 ?? new ConfiguracionFt8();
        servicios.AddSingleton(config);
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorFt8>();

        return servicios;
    }
}
