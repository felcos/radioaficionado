using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RadioAficionado.Dominio.IA;
using RadioAficionado.Dominio.Interfaces;

namespace RadioAficionado.IA;

/// <summary>
/// Extensiones para registrar los servicios de IA (ML.NET y ONNX Runtime) en el contenedor de DI.
/// </summary>
public static class ConfiguracionServiciosIa
{
    /// <summary>
    /// Registra los servicios de IA como Singleton en el contenedor de inyeccion de dependencias.
    /// Incluye el analizador de propagacion, el clasificador de senales (ML.NET),
    /// el motor de inferencia ONNX y la configuracion asociada.
    /// </summary>
    /// <param name="servicios">Coleccion de servicios.</param>
    /// <returns>La coleccion de servicios para encadenar llamadas.</returns>
    public static IServiceCollection AgregarCapaDeIa(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IAnalizadorPropagacion, AnalizadorPropagacionMlNet>();
        servicios.AddSingleton<IClasificadorSenales, ClasificadorSenalesMlNet>();

        // Registrar ConfiguracionOnnx con valores por defecto si no se ha registrado previamente
        servicios.TryAddSingleton(_ => new ConfiguracionOnnx());

        // Registrar el motor de inferencia ONNX como Singleton
        servicios.AddSingleton<IMotorInferenciaOnnx>(proveedor =>
        {
            ConfiguracionOnnx configuracion = proveedor.GetRequiredService<ConfiguracionOnnx>();
            return new MotorInferenciaOnnx(configuracion);
        });

        // Registrar el clasificador ML.NET como tipo concreto para que ClasificadorSenalesOnnx lo use como fallback
        servicios.AddSingleton<ClasificadorSenalesMlNet>();

        // Registrar el clasificador ONNX (disponible como tipo concreto para inyeccion directa)
        servicios.AddSingleton<ClasificadorSenalesOnnx>();

        // Registrar el entrenador de modelos IA como Transient (cada entrenamiento es independiente)
        servicios.AddTransient<IEntrenadorModelosIa, EntrenadorModelosIa>();

        return servicios;
    }
}
