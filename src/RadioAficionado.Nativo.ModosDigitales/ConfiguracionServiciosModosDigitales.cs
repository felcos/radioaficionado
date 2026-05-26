using Microsoft.Extensions.DependencyInjection;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Nativo.ModosDigitales.Cw;
using RadioAficionado.Nativo.ModosDigitales.Ft4;
using RadioAficionado.Nativo.ModosDigitales.Ft8;
using RadioAficionado.Nativo.ModosDigitales.Js8;
using RadioAficionado.Nativo.ModosDigitales.Jt65;
using RadioAficionado.Nativo.ModosDigitales.Jt9;
using RadioAficionado.Nativo.ModosDigitales.Olivia;
using RadioAficionado.Nativo.ModosDigitales.Psk31;
using RadioAficionado.Nativo.ModosDigitales.Rtty;
using RadioAficionado.Nativo.ModosDigitales.Sstv;
using RadioAficionado.Nativo.ModosDigitales.Wspr;
using RadioAficionado.Nativo.ModosDigitales.Ft2;
using RadioAficionado.Nativo.ModosDigitales.Q65;
using RadioAficionado.Nativo.ModosDigitales.Psk250;
using RadioAficionado.Nativo.ModosDigitales.Mfsk128;
using RadioAficionado.Nativo.ModosDigitales.Thor;
using RadioAficionado.Nativo.ModosDigitales.DominoEx;
using RadioAficionado.Nativo.ModosDigitales.Fsq;

namespace RadioAficionado.Nativo.ModosDigitales;

/// <summary>
/// Extensiones para registrar los servicios de modos digitales en el contenedor de DI.
/// </summary>
public static class ConfiguracionServiciosModosDigitales
{
    /// <summary>
    /// Registra los decodificadores de modos digitales en el contenedor de DI.
    /// </summary>
    /// <param name="servicios">Coleccion de servicios.</param>
    /// <param name="configuracionFt8">Configuracion opcional para FT8. Si es null, usa valores por defecto.</param>
    /// <param name="configuracionFt4">Configuracion opcional para FT4. Si es null, usa valores por defecto.</param>
    /// <param name="configuracionRtty">Configuracion opcional para RTTY. Si es null, usa valores por defecto.</param>
    /// <param name="configuracionPsk31">Configuracion opcional para PSK31. Si es null, usa valores por defecto.</param>
    /// <param name="configuracionJs8">Configuracion opcional para JS8. Si es null, usa valores por defecto.</param>
    /// <param name="configuracionJt65">Configuracion opcional para JT65. Si es null, usa valores por defecto.</param>
    /// <param name="configuracionJt9">Configuracion opcional para JT9. Si es null, usa valores por defecto.</param>
    /// <param name="configuracionOlivia">Configuracion opcional para Olivia. Si es null, usa valores por defecto (32/1000).</param>
    /// <param name="configuracionSstv">Configuracion opcional para SSTV. Si es null, usa valores por defecto (Scottie 1).</param>
    /// <param name="configuracionCw">Configuracion opcional para CW. Si es null, usa valores por defecto.</param>
    /// <param name="configuracionWspr">Configuracion opcional para WSPR. Si es null, usa valores por defecto.</param>
    /// <param name="configuracionFt2">Configuracion opcional para FT2. Si es null, usa valores por defecto.</param>
    /// <param name="configuracionQ65">Configuracion opcional para Q65. Si es null, usa valores por defecto (submodo A).</param>
    /// <returns>La coleccion de servicios para encadenar llamadas.</returns>
    public static IServiceCollection AgregarModosDigitales(
        this IServiceCollection servicios,
        ConfiguracionFt8? configuracionFt8 = null,
        ConfiguracionFt4? configuracionFt4 = null,
        ConfiguracionRtty? configuracionRtty = null,
        ConfiguracionPsk31? configuracionPsk31 = null,
        ConfiguracionJs8? configuracionJs8 = null,
        ConfiguracionJt65? configuracionJt65 = null,
        ConfiguracionJt9? configuracionJt9 = null,
        ConfiguracionOlivia? configuracionOlivia = null,
        ConfiguracionSstv? configuracionSstv = null,
        ConfiguracionCw? configuracionCw = null,
        ConfiguracionWspr? configuracionWspr = null,
        ConfiguracionFt2? configuracionFt2 = null,
        ConfiguracionQ65? configuracionQ65 = null)
    {
        // Configuraciones
        servicios.AddSingleton(configuracionFt8 ?? new ConfiguracionFt8());
        servicios.AddSingleton(configuracionFt4 ?? new ConfiguracionFt4());
        servicios.AddSingleton(configuracionRtty ?? new ConfiguracionRtty());
        servicios.AddSingleton(configuracionPsk31 ?? new ConfiguracionPsk31());
        servicios.AddSingleton(configuracionJs8 ?? new ConfiguracionJs8());
        servicios.AddSingleton(configuracionJt65 ?? new ConfiguracionJt65());
        servicios.AddSingleton(configuracionJt9 ?? new ConfiguracionJt9());
        servicios.AddSingleton(configuracionOlivia ?? new ConfiguracionOlivia());
        servicios.AddSingleton(configuracionSstv ?? new ConfiguracionSstv());
        servicios.AddSingleton(configuracionCw ?? new ConfiguracionCw());
        servicios.AddSingleton(configuracionWspr ?? new ConfiguracionWspr());
        servicios.AddSingleton(configuracionFt2 ?? new ConfiguracionFt2());
        servicios.AddSingleton(configuracionQ65 ?? new ConfiguracionQ65());
        servicios.AddSingleton(new ConfiguracionPsk250());
        servicios.AddSingleton(new ConfiguracionMfsk128());
        servicios.AddSingleton(new ConfiguracionThor());
        servicios.AddSingleton(new ConfiguracionDominoEx());
        servicios.AddSingleton(new ConfiguracionFsq());

        // Decodificadores
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorFt8>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorFt4>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorRtty>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorPsk31>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorJs8>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorJt65>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorJt9>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorOlivia>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorSstv>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorCw>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorWspr>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorFt2>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorQ65>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorPsk250>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorMfsk128>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorThor>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorDominoEx>();
        servicios.AddSingleton<IDecodificadorDigital, DecodificadorFsq>();

        // Registro central
        servicios.AddSingleton<IRegistroDecodificadores>(sp =>
        {
            IEnumerable<IDecodificadorDigital> decodificadores = sp.GetServices<IDecodificadorDigital>();
            return new RegistroDecodificadores(decodificadores);
        });

        return servicios;
    }
}
