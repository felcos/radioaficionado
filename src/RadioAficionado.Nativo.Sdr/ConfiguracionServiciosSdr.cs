using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.Sdr;

namespace RadioAficionado.Nativo.Sdr;

/// <summary>
/// Extensiones para registrar los servicios SDR en el contenedor de inyección de dependencias.
/// </summary>
public static class ConfiguracionServiciosSdr
{
    /// <summary>
    /// Registra los servicios de la capa SDR (SoapySDR) en el contenedor de DI.
    /// Incluye el receptor SDR como Singleton y la configuración proporcionada.
    /// </summary>
    /// <param name="servicios">Colección de servicios.</param>
    /// <param name="configuracion">Configuración del receptor SDR. Si es null, usa valores por defecto para RTL-SDR.</param>
    /// <returns>La colección de servicios para encadenar llamadas.</returns>
    public static IServiceCollection AgregarCapaDeSdr(
        this IServiceCollection servicios,
        ConfiguracionSdr? configuracion = null)
    {
        ConfiguracionSdr configuracionFinal = configuracion ?? new ConfiguracionSdr(
            FrecuenciaCentralHz: 145_000_000,
            TasaDeMuestreoHz: 2_048_000,
            AnchoDeBandaHz: 200_000,
            GananciaDb: 40.0);

        servicios.AddSingleton(configuracionFinal);
        servicios.AddSingleton<IReceptorSdr, ReceptorSoapySdr>();
        servicios.AddSingleton<IConvertidorIqAAudio, ConvertidorIqAAudio>();
        servicios.AddSingleton<IServicioWaterfallSdr, ServicioWaterfallSdr>();

        return servicios;
    }
}
