using RadioAficionado.Nativo.Dsp.Interfaces;

namespace RadioAficionado.Nativo.Dsp;

/// <summary>
/// Fabrica para crear instancias de <see cref="ITransformadaFourier"/>.
/// Intenta usar FFTW3 nativa para maximo rendimiento y cae automaticamente
/// a la implementacion managed Cooley-Tukey si los binarios nativos no estan disponibles.
/// </summary>
public static class FabricaTransformadaFourier
{
    private static bool? _fftw3Disponible;
    private static readonly object _bloqueo = new();

    /// <summary>
    /// Crea una instancia de <see cref="ITransformadaFourier"/> con la mejor implementacion disponible.
    /// Usa FFTW3 nativa si esta disponible; de lo contrario, usa Cooley-Tukey managed.
    /// </summary>
    /// <param name="tamano">Tamano de la FFT. Debe ser potencia de 2 y mayor o igual a 2.</param>
    /// <returns>Instancia de la mejor implementacion disponible.</returns>
    public static ITransformadaFourier Crear(int tamano)
    {
        if (Fftw3EstaDisponible())
        {
            try
            {
                return new TransformadaFftw3(tamano);
            }
            catch
            {
                // Si falla la creacion del plan, caer a managed
                return new TransformadaCooleyTukey(tamano);
            }
        }

        return new TransformadaCooleyTukey(tamano);
    }

    /// <summary>
    /// Verifica si la libreria nativa FFTW3 esta disponible en el sistema.
    /// El resultado se cachea para evitar verificaciones repetidas.
    /// </summary>
    /// <returns>true si FFTW3 esta disponible; false en caso contrario.</returns>
    public static bool Fftw3EstaDisponible()
    {
        if (_fftw3Disponible.HasValue)
        {
            return _fftw3Disponible.Value;
        }

        lock (_bloqueo)
        {
            if (_fftw3Disponible.HasValue)
            {
                return _fftw3Disponible.Value;
            }

            _fftw3Disponible = Fftw3Nativo.EstaDisponible();
            return _fftw3Disponible.Value;
        }
    }

    /// <summary>
    /// Obtiene el nombre de la implementacion que se usaria al crear una nueva instancia.
    /// Util para diagnostico y logging.
    /// </summary>
    /// <returns>Nombre de la implementacion activa.</returns>
    public static string ObtenerNombreImplementacion()
    {
        return Fftw3EstaDisponible()
            ? "FFTW3 (nativa)"
            : "Cooley-Tukey (managed)";
    }
}
