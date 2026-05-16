using System.Runtime.InteropServices;

namespace RadioAficionado.Nativo.Dsp;

/// <summary>
/// Declaraciones P/Invoke para la libreria nativa FFTW3.
/// FFTW (Fastest Fourier Transform in the West) es el estandar de la industria
/// para transformadas de Fourier de alto rendimiento.
/// </summary>
internal static class Fftw3Nativo
{
    /// <summary>
    /// Nombre de la libreria nativa segun la plataforma.
    /// Windows: libfftw3-3.dll, Linux: libfftw3.so.3, macOS: libfftw3.3.dylib
    /// </summary>
    private const string NombreLibreria = "libfftw3-3";

    /// <summary>
    /// Flag para estimar el plan rapidamente sin medir todas las opciones.
    /// </summary>
    public const uint FFTW_ESTIMATE = 64;

    /// <summary>
    /// Flag para medir el plan probando diferentes algoritmos (mas lento de crear, mas rapido de ejecutar).
    /// </summary>
    public const uint FFTW_MEASURE = 0;

    /// <summary>
    /// Crea un plan para una FFT real a compleja de 1 dimension.
    /// </summary>
    /// <param name="n">Tamano de la transformada.</param>
    /// <param name="entrada">Puntero al buffer de entrada (n doubles reales).</param>
    /// <param name="salida">Puntero al buffer de salida (n/2+1 pares complejos, cada uno 2 doubles).</param>
    /// <param name="flags">Flags de planificacion (FFTW_ESTIMATE o FFTW_MEASURE).</param>
    /// <returns>Handle del plan creado.</returns>
    [DllImport(NombreLibreria, EntryPoint = "fftw_plan_dft_r2c_1d", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr PlanearRealAComplejo(int n, IntPtr entrada, IntPtr salida, uint flags);

    /// <summary>
    /// Ejecuta un plan previamente creado.
    /// </summary>
    /// <param name="plan">Handle del plan a ejecutar.</param>
    [DllImport(NombreLibreria, EntryPoint = "fftw_execute", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Ejecutar(IntPtr plan);

    /// <summary>
    /// Destruye un plan y libera los recursos asociados.
    /// </summary>
    /// <param name="plan">Handle del plan a destruir.</param>
    [DllImport(NombreLibreria, EntryPoint = "fftw_destroy_plan", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestruirPlan(IntPtr plan);

    /// <summary>
    /// Asigna memoria alineada para buffers FFTW (alineacion SIMD optima).
    /// </summary>
    /// <param name="tamanoBytes">Tamano en bytes a asignar.</param>
    /// <returns>Puntero a la memoria asignada.</returns>
    [DllImport(NombreLibreria, EntryPoint = "fftw_malloc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr AsignarMemoria(IntPtr tamanoBytes);

    /// <summary>
    /// Libera memoria asignada con fftw_malloc.
    /// </summary>
    /// <param name="puntero">Puntero a la memoria a liberar.</param>
    [DllImport(NombreLibreria, EntryPoint = "fftw_free", CallingConvention = CallingConvention.Cdecl)]
    public static extern void LiberarMemoria(IntPtr puntero);

    /// <summary>
    /// Limpieza global de FFTW. Libera toda la sabiduria acumulada y planes internos.
    /// Llamar solo al final del programa.
    /// </summary>
    [DllImport(NombreLibreria, EntryPoint = "fftw_cleanup", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Limpiar();

    /// <summary>
    /// Verifica si la libreria nativa FFTW3 esta disponible en el sistema.
    /// </summary>
    /// <returns>true si la libreria se puede cargar; false en caso contrario.</returns>
    public static bool EstaDisponible()
    {
        try
        {
            Limpiar();
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}
