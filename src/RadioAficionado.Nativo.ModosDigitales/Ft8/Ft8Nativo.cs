using System.Runtime.InteropServices;

namespace RadioAficionado.Nativo.ModosDigitales.Ft8;

/// <summary>
/// Estructura nativa que representa un mensaje FT8 decodificado por la librería ft8_lib.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct Ft8MensajeNativo
{
    /// <summary>
    /// Texto del mensaje decodificado (máximo 35 caracteres + nulo).
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 36)]
    public string Texto;

    /// <summary>
    /// Frecuencia de audio en Hz dentro de la banda pasante.
    /// </summary>
    public int FrecuenciaHz;

    /// <summary>
    /// Relación señal/ruido en dB.
    /// </summary>
    public int Snr;

    /// <summary>
    /// Delta de tiempo en milisegundos respecto al inicio de la ventana.
    /// </summary>
    public int DeltaTiempoMs;
}

/// <summary>
/// Estructura nativa con los tonos generados para transmisión FT8.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Ft8TonosNativo
{
    /// <summary>
    /// Array de 79 tonos (valores 0-7) que componen la señal FT8.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 79)]
    public byte[] Tonos;

    /// <summary>
    /// Número de tonos válidos en el array.
    /// </summary>
    public int NumeroTonos;
}

/// <summary>
/// Wrapper P/Invoke para la librería nativa ft8_lib.
/// Proporciona acceso a las funciones de codificación y decodificación FT8.
/// </summary>
public static class Ft8Nativo
{
    private const string NombreLibreria = "ft8_lib";

    /// <summary>
    /// Decodifica un buffer de audio PCM float de 15 segundos en mensajes FT8.
    /// </summary>
    /// <param name="muestras">Puntero al buffer de audio PCM float (mono, 12000 Hz).</param>
    /// <param name="numeroMuestras">Número de muestras en el buffer.</param>
    /// <param name="mensajes">Puntero al array de salida para los mensajes decodificados.</param>
    /// <param name="maximoMensajes">Tamaño máximo del array de mensajes.</param>
    /// <returns>Número de mensajes decodificados.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ft8_decode")]
    public static extern int Decodificar(
        [In] float[] muestras,
        int numeroMuestras,
        [Out] Ft8MensajeNativo[] mensajes,
        int maximoMensajes);

    /// <summary>
    /// Codifica un mensaje de texto en los 79 tonos FT8 para transmisión.
    /// </summary>
    /// <param name="mensaje">Texto del mensaje a codificar (máximo 13 caracteres para estándar).</param>
    /// <param name="tonos">Estructura de salida con los tonos generados.</param>
    /// <returns>0 si la codificación fue exitosa, código de error en caso contrario.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ft8_encode")]
    public static extern int Codificar(
        [MarshalAs(UnmanagedType.LPStr)] string mensaje,
        out Ft8TonosNativo tonos);

    /// <summary>
    /// Verifica si la librería nativa ft8_lib está disponible en el sistema.
    /// Intenta cargar la librería y devuelve true si se encuentra.
    /// </summary>
    /// <returns>True si la librería nativa está disponible y puede cargarse.</returns>
    public static bool EstaDisponible()
    {
        try
        {
            bool cargada = NativeLibrary.TryLoad(NombreLibreria, out IntPtr handle);
            if (cargada && handle != IntPtr.Zero)
            {
                NativeLibrary.Free(handle);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
