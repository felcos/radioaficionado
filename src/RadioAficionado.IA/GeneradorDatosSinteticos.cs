using Microsoft.ML.Data;

namespace RadioAficionado.IA;

/// <summary>
/// Generador de datos sinteticos realistas para entrenar modelos de IA de radioaficionado.
/// Produce espectros de senales con ruido gaussiano a multiples niveles de SNR,
/// y datos de propagacion basados en un modelo fisico simplificado.
/// </summary>
internal static class GeneradorDatosSinteticos
{
    /// <summary>
    /// Numero de bins del espectro reducido.
    /// </summary>
    private const int TamanioEspectro = 64;

    /// <summary>
    /// Genera espectros sinteticos de senales CW (codigo Morse).
    /// Pico estrecho (2-3 bins) a frecuencia variable con ruido gaussiano.
    /// </summary>
    /// <param name="cantidad">Numero de muestras a generar.</param>
    /// <param name="rng">Generador de numeros aleatorios.</param>
    /// <returns>Lista de datos de entrenamiento con etiqueta "CW".</returns>
    internal static List<DatoEntrenamientoSenal> GenerarEspectrosCw(int cantidad, Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        List<DatoEntrenamientoSenal> datos = new(cantidad);

        for (int i = 0; i < cantidad; i++)
        {
            float[] espectro = CrearEspectroBase(rng);
            float snr = 0.3f + (float)(rng.NextDouble() * 0.7f);

            int posicion = rng.Next(8, 56);
            espectro[posicion] += snr * (0.85f + (float)(rng.NextDouble() * 0.15));
            if (posicion - 1 >= 0)
                espectro[posicion - 1] += snr * (0.25f + (float)(rng.NextDouble() * 0.15));
            if (posicion + 1 < TamanioEspectro)
                espectro[posicion + 1] += snr * (0.25f + (float)(rng.NextDouble() * 0.15));

            // Ocasionalmente agregar un segundo armonico debil
            if (rng.NextDouble() > 0.6 && posicion * 2 < TamanioEspectro)
            {
                espectro[posicion * 2] += snr * 0.15f;
            }

            datos.Add(new DatoEntrenamientoSenal { Espectro = espectro, Modo = "CW" });
        }

        return datos;
    }

    /// <summary>
    /// Genera espectros sinteticos de senales SSB (Single Sideband).
    /// Banda ancha de ~2.4 kHz (aprox 10 bins) con forma de campana y ruido gaussiano.
    /// </summary>
    /// <param name="cantidad">Numero de muestras a generar.</param>
    /// <param name="rng">Generador de numeros aleatorios.</param>
    /// <returns>Lista de datos de entrenamiento con etiqueta "SSB".</returns>
    internal static List<DatoEntrenamientoSenal> GenerarEspectrosSsb(int cantidad, Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        List<DatoEntrenamientoSenal> datos = new(cantidad);

        for (int i = 0; i < cantidad; i++)
        {
            float[] espectro = CrearEspectroBase(rng);
            float snr = 0.3f + (float)(rng.NextDouble() * 0.7f);
            int inicio = rng.Next(3, 42);
            int ancho = 8 + rng.Next(0, 5); // 8-12 bins para simular 2.0-3.0 kHz

            for (int j = 0; j < ancho && inicio + j < TamanioEspectro; j++)
            {
                // Forma de campana gaussiana
                float centro = ancho / 2.0f;
                float distancia = Math.Abs(j - centro) / centro;
                float amplitud = snr * (0.35f + (float)(rng.NextDouble() * 0.3)) * (1.0f - distancia * 0.5f);
                espectro[inicio + j] += amplitud;
            }

            datos.Add(new DatoEntrenamientoSenal { Espectro = espectro, Modo = "SSB" });
        }

        return datos;
    }

    /// <summary>
    /// Genera espectros sinteticos de senales FM (Frequency Modulation).
    /// Portadora central fuerte con bandas laterales decrecientes y ruido gaussiano.
    /// </summary>
    /// <param name="cantidad">Numero de muestras a generar.</param>
    /// <param name="rng">Generador de numeros aleatorios.</param>
    /// <returns>Lista de datos de entrenamiento con etiqueta "FM".</returns>
    internal static List<DatoEntrenamientoSenal> GenerarEspectrosFm(int cantidad, Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        List<DatoEntrenamientoSenal> datos = new(cantidad);

        for (int i = 0; i < cantidad; i++)
        {
            float[] espectro = CrearEspectroBase(rng);
            float snr = 0.4f + (float)(rng.NextDouble() * 0.6f);
            int posicion = rng.Next(15, 49);

            // Portadora central fuerte
            espectro[posicion] += snr * (0.90f + (float)(rng.NextDouble() * 0.10));

            // Bandas laterales por modulacion FM (decrecen segun funcion de Bessel simplificada)
            int numLaterales = 3 + rng.Next(0, 4); // 3-6 pares de bandas laterales
            for (int d = 1; d <= numLaterales; d++)
            {
                float amplitud = snr * (0.4f / d) * (0.8f + (float)(rng.NextDouble() * 0.4));
                if (posicion - d >= 0)
                    espectro[posicion - d] += amplitud;
                if (posicion + d < TamanioEspectro)
                    espectro[posicion + d] += amplitud;
            }

            datos.Add(new DatoEntrenamientoSenal { Espectro = espectro, Modo = "FM" });
        }

        return datos;
    }

    /// <summary>
    /// Genera espectros sinteticos de senales FT8.
    /// 8 tonos espaciados ~6.25 Hz (representados como bins separados) con ruido gaussiano.
    /// </summary>
    /// <param name="cantidad">Numero de muestras a generar.</param>
    /// <param name="rng">Generador de numeros aleatorios.</param>
    /// <returns>Lista de datos de entrenamiento con etiqueta "FT8".</returns>
    internal static List<DatoEntrenamientoSenal> GenerarEspectrosFt8(int cantidad, Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        List<DatoEntrenamientoSenal> datos = new(cantidad);

        for (int i = 0; i < cantidad; i++)
        {
            float[] espectro = CrearEspectroBase(rng);
            float snr = 0.25f + (float)(rng.NextDouble() * 0.75f);
            int inicio = rng.Next(3, 38);

            // 8 tonos con espaciado variable simulando 6.25 Hz
            for (int tono = 0; tono < 8; tono++)
            {
                int posicion = inicio + (int)(tono * 1.5) + rng.Next(0, 2);
                if (posicion < TamanioEspectro)
                {
                    float amplitud = snr * (0.55f + (float)(rng.NextDouble() * 0.35));
                    espectro[posicion] += amplitud;
                }
            }

            datos.Add(new DatoEntrenamientoSenal { Espectro = espectro, Modo = "FT8" });
        }

        return datos;
    }

    /// <summary>
    /// Genera espectros sinteticos de senales AM (Amplitude Modulation).
    /// Portadora central fuerte con bandas laterales simetricas y envolvente.
    /// </summary>
    /// <param name="cantidad">Numero de muestras a generar.</param>
    /// <param name="rng">Generador de numeros aleatorios.</param>
    /// <returns>Lista de datos de entrenamiento con etiqueta "AM".</returns>
    internal static List<DatoEntrenamientoSenal> GenerarEspectrosAm(int cantidad, Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        List<DatoEntrenamientoSenal> datos = new(cantidad);

        for (int i = 0; i < cantidad; i++)
        {
            float[] espectro = CrearEspectroBase(rng);
            float snr = 0.4f + (float)(rng.NextDouble() * 0.6f);
            int posicion = rng.Next(12, 52);

            // Portadora central dominante
            espectro[posicion] += snr * (0.90f + (float)(rng.NextDouble() * 0.10));

            // Bandas laterales simetricas (envolvente de audio ~5 kHz cada lado)
            int anchoLateral = 3 + rng.Next(0, 3); // 3-5 bins
            for (int d = 1; d <= anchoLateral; d++)
            {
                float amplitud = snr * (0.30f - d * 0.05f) * (0.8f + (float)(rng.NextDouble() * 0.4));
                amplitud = Math.Max(amplitud, 0.02f);
                if (posicion - d >= 0)
                    espectro[posicion - d] += amplitud;
                if (posicion + d < TamanioEspectro)
                    espectro[posicion + d] += amplitud;
            }

            datos.Add(new DatoEntrenamientoSenal { Espectro = espectro, Modo = "AM" });
        }

        return datos;
    }

    /// <summary>
    /// Genera espectros sinteticos de ruido (sin senal).
    /// Energia distribuida uniformemente con variacion gaussiana en todo el espectro.
    /// </summary>
    /// <param name="cantidad">Numero de muestras a generar.</param>
    /// <param name="rng">Generador de numeros aleatorios.</param>
    /// <returns>Lista de datos de entrenamiento con etiqueta "Ruido".</returns>
    internal static List<DatoEntrenamientoSenal> GenerarEspectrosRuido(int cantidad, Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        List<DatoEntrenamientoSenal> datos = new(cantidad);

        for (int i = 0; i < cantidad; i++)
        {
            float[] espectro = new float[TamanioEspectro];
            float nivelBase = 0.10f + (float)(rng.NextDouble() * 0.15);

            for (int j = 0; j < TamanioEspectro; j++)
            {
                // Ruido gaussiano centrado en el nivel base
                float ruidoGaussiano = (float)GenerarGaussiano(rng, 0, 0.05);
                espectro[j] = Math.Max(0, nivelBase + ruidoGaussiano + (float)(rng.NextDouble() * 0.10));
            }

            datos.Add(new DatoEntrenamientoSenal { Espectro = espectro, Modo = "Ruido" });
        }

        return datos;
    }

    /// <summary>
    /// Genera datos sinteticos de propagacion HF basados en un modelo fisico simplificado.
    /// Relaciona indices solares (SFI, Kp, Ap, manchas solares), hora UTC, mes y banda
    /// con la probabilidad de apertura de la banda.
    /// </summary>
    /// <param name="cantidad">Numero de muestras a generar.</param>
    /// <param name="rng">Generador de numeros aleatorios.</param>
    /// <returns>Lista de datos de entrenamiento para regresion de propagacion.</returns>
    internal static List<DatoEntrenamientoPropagacion> GenerarDatosPropagacion(int cantidad, Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        List<DatoEntrenamientoPropagacion> datos = new(cantidad);

        float[] frecuenciasBandas = [1.9f, 3.75f, 5.35f, 7.15f, 10.125f, 14.175f, 18.118f, 21.225f, 24.94f, 28.85f];

        for (int i = 0; i < cantidad; i++)
        {
            float sfi = 60 + (float)(rng.NextDouble() * 240); // 60-300
            float kp = (float)(rng.NextDouble() * 9); // 0-9
            float ap = kp * 4.5f + (float)(rng.NextDouble() * 5); // Correlacionado con Kp
            float manchas = sfi * 0.8f + (float)(rng.NextDouble() * 30 - 15); // Correlacionado con SFI
            manchas = Math.Max(0, manchas);
            float horaUtc = (float)(rng.NextDouble() * 24); // 0-24
            float mes = 1 + rng.Next(0, 12); // 1-12
            float banda = frecuenciasBandas[rng.Next(0, frecuenciasBandas.Length)];

            float probabilidad = CalcularProbabilidadFisica(sfi, kp, horaUtc, banda, mes, rng);

            datos.Add(new DatoEntrenamientoPropagacion
            {
                Sfi = sfi,
                IndiceK = kp,
                IndiceA = ap,
                ManchasSolares = manchas,
                HoraUtc = horaUtc,
                MesDelAnio = mes,
                NumeroBanda = banda,
                ProbabilidadApertura = probabilidad
            });
        }

        return datos;
    }

    /// <summary>
    /// Crea un espectro base con ruido gaussiano de fondo.
    /// </summary>
    /// <param name="rng">Generador de numeros aleatorios.</param>
    /// <returns>Array de 64 bins con ruido de fondo.</returns>
    private static float[] CrearEspectroBase(Random rng)
    {
        float[] espectro = new float[TamanioEspectro];
        float nivelRuido = 0.02f + (float)(rng.NextDouble() * 0.06);

        for (int i = 0; i < TamanioEspectro; i++)
        {
            float ruidoGaussiano = (float)GenerarGaussiano(rng, 0, nivelRuido);
            espectro[i] = Math.Max(0, nivelRuido + ruidoGaussiano);
        }

        return espectro;
    }

    /// <summary>
    /// Calcula la probabilidad de apertura basada en un modelo fisico simplificado de propagacion HF.
    /// Considera la relacion entre MUF estimada, banda, hora del dia, actividad geomagnetica y estacion.
    /// </summary>
    private static float CalcularProbabilidadFisica(
        float sfi, float kp, float horaUtc, float frecuenciaMhz, float mes, Random rng)
    {
        // Estimar MUF simplificada: MUF ~ SFI * factor_hora
        bool esDeDia = horaUtc >= 6 && horaUtc <= 18;
        float factorDiaNoche = esDeDia ? 1.0f : 0.5f;

        // Factor estacional: equinoccios mejores para bandas altas
        float factorEstacional = 1.0f;
        if (mes >= 3 && mes <= 5 || mes >= 9 && mes <= 11) // Equinoccios
        {
            factorEstacional = 1.15f;
        }
        else if (mes >= 6 && mes <= 8) // Verano
        {
            factorEstacional = frecuenciaMhz < 10 ? 1.1f : 0.9f;
        }

        // MUF estimada simplificada
        float mufEstimada = sfi * 0.12f * factorDiaNoche * factorEstacional + 5.0f;

        // Probabilidad base: mas alta si la frecuencia esta por debajo de la MUF
        float relacionFrecMuf = frecuenciaMhz / mufEstimada;
        float probabilidad;

        if (relacionFrecMuf < 0.3f)
        {
            probabilidad = 0.85f + (float)(rng.NextDouble() * 0.10);
        }
        else if (relacionFrecMuf < 0.6f)
        {
            probabilidad = 0.65f + (float)(rng.NextDouble() * 0.15);
        }
        else if (relacionFrecMuf < 0.85f)
        {
            probabilidad = 0.40f + (float)(rng.NextDouble() * 0.15);
        }
        else if (relacionFrecMuf < 1.0f)
        {
            probabilidad = 0.15f + (float)(rng.NextDouble() * 0.15);
        }
        else
        {
            probabilidad = 0.02f + (float)(rng.NextDouble() * 0.08);
        }

        // Degradacion por perturbaciones geomagneticas
        if (kp >= 4)
        {
            float factorDegradacion = 1.0f - (kp - 3) * 0.10f;
            probabilidad *= Math.Max(factorDegradacion, 0.05f);
        }

        // Bandas bajas se benefician de la noche (NVIS)
        if (frecuenciaMhz < 8.0f && !esDeDia)
        {
            probabilidad = Math.Max(probabilidad, 0.60f + (float)(rng.NextDouble() * 0.20));
        }

        // Ruido gaussiano leve para variabilidad
        probabilidad += (float)GenerarGaussiano(rng, 0, 0.03);

        return Math.Clamp(probabilidad, 0.0f, 1.0f);
    }

    /// <summary>
    /// Genera un valor aleatorio con distribucion gaussiana usando el metodo Box-Muller.
    /// </summary>
    /// <param name="rng">Generador de numeros aleatorios.</param>
    /// <param name="media">Media de la distribucion.</param>
    /// <param name="desviacion">Desviacion estandar de la distribucion.</param>
    /// <returns>Valor aleatorio gaussiano.</returns>
    private static double GenerarGaussiano(Random rng, double media, double desviacion)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return media + desviacion * normal;
    }
}
