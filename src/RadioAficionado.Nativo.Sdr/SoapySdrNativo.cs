using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace RadioAficionado.Nativo.Sdr;

/// <summary>
/// Declaraciones P/Invoke para la librería nativa SoapySDR.
/// Proporciona acceso directo a las funciones C de SoapySDR para
/// enumeración, configuración y lectura de dispositivos SDR.
/// </summary>
public static class SoapySdrNativo
{
    /// <summary>
    /// Nombre de la librería nativa SoapySDR.
    /// </summary>
    private const string NombreLibreria = "SoapySDR";

    // ─── Enumeración de dispositivos ───

    /// <summary>
    /// Enumera los dispositivos SDR disponibles en el sistema.
    /// </summary>
    /// <param name="argumentos">Argumentos de filtrado (puede ser null).</param>
    /// <param name="longitud">Recibe el número de dispositivos encontrados.</param>
    /// <returns>Puntero a un array de SoapySDRKwargs con la información de cada dispositivo.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_enumerate(IntPtr argumentos, out nint longitud);

    /// <summary>
    /// Libera la memoria de la lista de dispositivos enumerados.
    /// </summary>
    /// <param name="kwargs">Puntero al array de kwargs retornado por enumerate.</param>
    /// <param name="longitud">Número de elementos en el array.</param>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SoapySDRKwargs_clear(IntPtr kwargs);

    /// <summary>
    /// Libera la memoria de una lista de kwargs.
    /// </summary>
    /// <param name="kwargs">Puntero al array de kwargs.</param>
    /// <param name="longitud">Número de elementos.</param>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SoapySDRKwargsList_clear(IntPtr kwargs, nint longitud);

    // ─── Creación y destrucción de dispositivos ───

    /// <summary>
    /// Crea una instancia de dispositivo SoapySDR con los argumentos dados.
    /// </summary>
    /// <param name="argumentos">Puntero a SoapySDRKwargs con los argumentos de conexión.</param>
    /// <returns>Puntero al dispositivo creado, o IntPtr.Zero en caso de error.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_make(IntPtr argumentos);

    /// <summary>
    /// Crea una instancia de dispositivo SoapySDR a partir de una cadena de argumentos.
    /// </summary>
    /// <param name="argumentos">Cadena con los argumentos (ej: "driver=rtlsdr").</param>
    /// <returns>Puntero al dispositivo creado, o IntPtr.Zero en caso de error.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_makeStrArgs(
        [MarshalAs(UnmanagedType.LPStr)] string argumentos);

    /// <summary>
    /// Destruye una instancia de dispositivo SoapySDR y libera sus recursos.
    /// </summary>
    /// <param name="dispositivo">Puntero al dispositivo a destruir.</param>
    /// <returns>0 en éxito, código de error en caso contrario.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_unmake(IntPtr dispositivo);

    // ─── Configuración de parámetros ───

    /// <summary>
    /// Configura la tasa de muestreo del dispositivo.
    /// </summary>
    /// <param name="dispositivo">Puntero al dispositivo.</param>
    /// <param name="direccion">Dirección (0 = RX, 1 = TX).</param>
    /// <param name="canal">Número de canal.</param>
    /// <param name="tasa">Tasa de muestreo en Hz.</param>
    /// <returns>0 en éxito, código de error en caso contrario.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_setSampleRate(
        IntPtr dispositivo, int direccion, nint canal, double tasa);

    /// <summary>
    /// Configura la frecuencia central del dispositivo.
    /// </summary>
    /// <param name="dispositivo">Puntero al dispositivo.</param>
    /// <param name="direccion">Dirección (0 = RX, 1 = TX).</param>
    /// <param name="canal">Número de canal.</param>
    /// <param name="frecuencia">Frecuencia en Hz.</param>
    /// <param name="argumentos">Argumentos adicionales (puede ser IntPtr.Zero).</param>
    /// <returns>0 en éxito, código de error en caso contrario.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_setFrequency(
        IntPtr dispositivo, int direccion, nint canal, double frecuencia, IntPtr argumentos);

    /// <summary>
    /// Configura la ganancia global del dispositivo.
    /// </summary>
    /// <param name="dispositivo">Puntero al dispositivo.</param>
    /// <param name="direccion">Dirección (0 = RX, 1 = TX).</param>
    /// <param name="canal">Número de canal.</param>
    /// <param name="ganancia">Ganancia en dB.</param>
    /// <returns>0 en éxito, código de error en caso contrario.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_setGain(
        IntPtr dispositivo, int direccion, nint canal, double ganancia);

    /// <summary>
    /// Configura el ancho de banda del filtro analógico del dispositivo.
    /// </summary>
    /// <param name="dispositivo">Puntero al dispositivo.</param>
    /// <param name="direccion">Dirección (0 = RX, 1 = TX).</param>
    /// <param name="canal">Número de canal.</param>
    /// <param name="anchoBanda">Ancho de banda en Hz.</param>
    /// <returns>0 en éxito, código de error en caso contrario.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_setBandwidth(
        IntPtr dispositivo, int direccion, nint canal, double anchoBanda);

    // ─── Streaming ───

    /// <summary>
    /// Configura un stream de recepción o transmisión.
    /// </summary>
    /// <param name="dispositivo">Puntero al dispositivo.</param>
    /// <param name="direccion">Dirección (0 = RX, 1 = TX).</param>
    /// <param name="formato">Formato de las muestras (ej: "CF32").</param>
    /// <param name="canales">Array de canales a usar.</param>
    /// <param name="numCanales">Número de canales.</param>
    /// <param name="argumentos">Argumentos adicionales (puede ser IntPtr.Zero).</param>
    /// <returns>Puntero al stream configurado, o IntPtr.Zero en caso de error.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SoapySDRDevice_setupStream(
        IntPtr dispositivo, int direccion,
        [MarshalAs(UnmanagedType.LPStr)] string formato,
        IntPtr canales, nint numCanales, IntPtr argumentos);

    /// <summary>
    /// Activa un stream previamente configurado.
    /// </summary>
    /// <param name="dispositivo">Puntero al dispositivo.</param>
    /// <param name="stream">Puntero al stream.</param>
    /// <param name="flags">Flags de activación.</param>
    /// <param name="tiempoNs">Tiempo en nanosegundos (0 para inmediato).</param>
    /// <param name="numElementos">Número de elementos (0 para continuo).</param>
    /// <returns>0 en éxito, código de error en caso contrario.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_activateStream(
        IntPtr dispositivo, IntPtr stream, int flags, long tiempoNs, nint numElementos);

    /// <summary>
    /// Desactiva un stream activo.
    /// </summary>
    /// <param name="dispositivo">Puntero al dispositivo.</param>
    /// <param name="stream">Puntero al stream.</param>
    /// <param name="flags">Flags de desactivación.</param>
    /// <param name="tiempoNs">Tiempo en nanosegundos.</param>
    /// <returns>0 en éxito, código de error en caso contrario.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_deactivateStream(
        IntPtr dispositivo, IntPtr stream, int flags, long tiempoNs);

    /// <summary>
    /// Cierra y libera un stream.
    /// </summary>
    /// <param name="dispositivo">Puntero al dispositivo.</param>
    /// <param name="stream">Puntero al stream a cerrar.</param>
    /// <returns>0 en éxito, código de error en caso contrario.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_closeStream(IntPtr dispositivo, IntPtr stream);

    /// <summary>
    /// Lee muestras del stream de recepción.
    /// </summary>
    /// <param name="dispositivo">Puntero al dispositivo.</param>
    /// <param name="stream">Puntero al stream.</param>
    /// <param name="buffers">Array de punteros a los buffers de salida.</param>
    /// <param name="numElementos">Número de elementos a leer.</param>
    /// <param name="flags">Recibe flags de estado de la lectura.</param>
    /// <param name="tiempoNs">Recibe la marca de tiempo en nanosegundos.</param>
    /// <param name="timeoutUs">Timeout en microsegundos.</param>
    /// <returns>Número de elementos leídos, o código de error negativo.</returns>
    [DllImport(NombreLibreria, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SoapySDRDevice_readStream(
        IntPtr dispositivo, IntPtr stream, IntPtr[] buffers,
        nint numElementos, out int flags, out long tiempoNs, long timeoutUs);

    // ─── Utilidades ───

    /// <summary>
    /// Dirección de recepción (RX) en la API de SoapySDR.
    /// </summary>
    public const int DireccionRx = 0;

    /// <summary>
    /// Formato de muestras complejas flotantes de 32 bits (I + Q como float32).
    /// </summary>
    public const string FormatoComplejo32 = "CF32";

    /// <summary>
    /// Timeout por defecto para lectura de stream en microsegundos (100ms).
    /// </summary>
    public const long TimeoutLecturaUs = 100_000;

    /// <summary>
    /// Verifica si la librería nativa SoapySDR está disponible en el sistema.
    /// Intenta cargar la librería sin ejecutar ninguna función.
    /// </summary>
    /// <returns>True si la librería se puede cargar, false en caso contrario.</returns>
    public static bool EstaDisponible()
    {
        try
        {
            bool cargada = NativeLibrary.TryLoad(NombreLibreria, out IntPtr handle);
            if (cargada && handle != IntPtr.Zero)
            {
                NativeLibrary.Free(handle);
            }
            return cargada;
        }
        catch
        {
            return false;
        }
    }
}
