namespace RadioAficionado.Nativo.ModosDigitales.Ft8;

/// <summary>
/// Convierte los 79 tonos FT8 a audio PCM 16-bit para transmision.
/// FT8 usa 8-GFSK con spacing de 6.25 Hz y duracion de tono de ~0.16 segundos.
/// La señal completa dura 12.64 segundos (79 tonos * 0.16s).
/// </summary>
public static class GeneradorAudioFt8
{
    /// <summary>Numero de tonos en una señal FT8.</summary>
    public const int NumeroTonos = 79;

    /// <summary>Separacion entre tonos en Hz.</summary>
    public const double EspaciadoHz = 6.25;

    /// <summary>Duracion de cada tono en segundos.</summary>
    public const double DuracionTonoPorSegundo = 0.16;

    /// <summary>Tasa de muestreo para la generacion de audio.</summary>
    public const int TasaDeMuestreoHz = 48000;

    /// <summary>Duracion total de la señal en segundos.</summary>
    public const double DuracionTotalSegundos = NumeroTonos * DuracionTonoPorSegundo; // 12.64s

    /// <summary>Duracion de la rampa de subida/bajada en segundos para evitar clicks.</summary>
    public const double DuracionRampaSegundos = 0.002;

    /// <summary>
    /// Genera audio PCM 16-bit a partir de los 79 tonos FT8.
    /// </summary>
    /// <param name="tonos">Array de 79 tonos (valores 0-7).</param>
    /// <param name="frecuenciaBaseHz">Frecuencia base de audio en Hz (offset dentro de la banda).</param>
    /// <returns>Array de muestras PCM 16-bit mono a 48000 Hz.</returns>
    /// <exception cref="ArgumentException">Si tonos es null o no tiene 79 elementos.</exception>
    public static short[] GenerarAudio(byte[] tonos, double frecuenciaBaseHz)
    {
        if (tonos is null || tonos.Length != NumeroTonos)
        {
            throw new ArgumentException(
                $"Se requieren exactamente {NumeroTonos} tonos. Recibidos: {tonos?.Length ?? 0}.",
                nameof(tonos));
        }

        if (frecuenciaBaseHz < 200 || frecuenciaBaseHz > 4900)
        {
            throw new ArgumentException(
                "La frecuencia base debe estar entre 200 y 4900 Hz.",
                nameof(frecuenciaBaseHz));
        }

        int muestrasPorTono = (int)(DuracionTonoPorSegundo * TasaDeMuestreoHz);
        int totalMuestras = muestrasPorTono * NumeroTonos;
        short[] audio = new short[totalMuestras];

        int muestrasRampa = (int)(DuracionRampaSegundos * TasaDeMuestreoHz);
        double fase = 0.0;

        for (int t = 0; t < NumeroTonos; t++)
        {
            double frecuenciaTono = frecuenciaBaseHz + (tonos[t] * EspaciadoHz);
            double incrementoFase = 2.0 * Math.PI * frecuenciaTono / TasaDeMuestreoHz;

            for (int m = 0; m < muestrasPorTono; m++)
            {
                int indiceMuestra = (t * muestrasPorTono) + m;
                double muestra = Math.Sin(fase);

                // Aplicar rampa de suavizado al inicio y final de la señal completa
                double envolvente = 1.0;

                if (indiceMuestra < muestrasRampa)
                {
                    // Rampa de subida al inicio
                    envolvente = (double)indiceMuestra / muestrasRampa;
                }
                else if (indiceMuestra >= totalMuestras - muestrasRampa)
                {
                    // Rampa de bajada al final
                    envolvente = (double)(totalMuestras - 1 - indiceMuestra) / muestrasRampa;
                }

                muestra *= envolvente;

                // Convertir a PCM 16-bit (rango -32767 a +32767)
                audio[indiceMuestra] = (short)(muestra * 32767.0);

                fase += incrementoFase;

                // Mantener fase en rango para evitar perdida de precision
                if (fase > 2.0 * Math.PI)
                {
                    fase -= 2.0 * Math.PI;
                }
            }
        }

        return audio;
    }

    /// <summary>
    /// Genera audio PCM 16-bit a partir de un mensaje de texto.
    /// Codifica el mensaje con ft8_lib y genera el audio correspondiente.
    /// </summary>
    /// <param name="mensaje">Texto del mensaje FT8 (max 13 caracteres estandar).</param>
    /// <param name="frecuenciaBaseHz">Frecuencia base de audio en Hz.</param>
    /// <returns>Array de muestras PCM 16-bit, o null si la codificacion falla.</returns>
    public static short[]? GenerarAudioDesdeMensaje(string mensaje, double frecuenciaBaseHz)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return null;
        }

        if (!Ft8Nativo.EstaDisponible())
        {
            return null;
        }

        int resultado = Ft8Nativo.Codificar(mensaje, out Ft8TonosNativo tonosNativo);
        if (resultado != 0 || tonosNativo.Tonos is null || tonosNativo.NumeroTonos != NumeroTonos)
        {
            return null;
        }

        return GenerarAudio(tonosNativo.Tonos, frecuenciaBaseHz);
    }
}
