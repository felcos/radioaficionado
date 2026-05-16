using Microsoft.ML;
using Microsoft.ML.Data;
using RadioAficionado.Dominio.IA;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Propagacion;

namespace RadioAficionado.IA;

/// <summary>
/// Dato de entrenamiento para el modelo de prediccion de propagacion.
/// Cada registro representa una combinacion de indices solares y banda con su probabilidad de apertura conocida.
/// </summary>
internal sealed class DatoEntrenamientoPropagacion
{
    /// <summary>Indice de flujo solar (60-300).</summary>
    public float Sfi { get; set; }

    /// <summary>Indice K planetario (0-9).</summary>
    public float IndiceK { get; set; }

    /// <summary>Indice A planetario.</summary>
    public float IndiceA { get; set; }

    /// <summary>Numero de manchas solares.</summary>
    public float ManchasSolares { get; set; }

    /// <summary>Hora UTC actual (0-23).</summary>
    public float HoraUtc { get; set; }

    /// <summary>Mes del anio (1-12).</summary>
    public float MesDelAnio { get; set; }

    /// <summary>Numero identificador de la banda (frecuencia central aproximada en MHz).</summary>
    public float NumeroBanda { get; set; }

    /// <summary>Probabilidad de apertura objetivo (0.0 a 1.0).</summary>
    public float ProbabilidadApertura { get; set; }
}

/// <summary>
/// Resultado de la prediccion del modelo ML.NET de propagacion.
/// </summary>
internal sealed class PrediccionPropagacionSalida
{
    /// <summary>Valor predicho de probabilidad de apertura.</summary>
    [ColumnName("Score")]
    public float ProbabilidadApertura { get; set; }
}

/// <summary>
/// Implementacion del analizador de propagacion usando ML.NET con regresion FastTree.
/// Entrena un modelo con datos sinteticos basados en relaciones conocidas entre indices solares y apertura de bandas HF.
/// Thread-safe mediante lock para el PredictionEngine.
/// </summary>
public sealed class AnalizadorPropagacionMlNet : IAnalizadorPropagacion
{
    private readonly MLContext _contextoMl;
    private readonly PredictionEngine<DatoEntrenamientoPropagacion, PrediccionPropagacionSalida> _motorPrediccion;
    private readonly object _bloqueo = new();

    /// <summary>
    /// Bandas HF que el modelo puede evaluar, con su frecuencia central aproximada en MHz.
    /// </summary>
    private static readonly IReadOnlyDictionary<BandaRadio, float> _bandasHf = new Dictionary<BandaRadio, float>
    {
        { BandaRadio.Banda160m, 1.9f },
        { BandaRadio.Banda80m, 3.75f },
        { BandaRadio.Banda60m, 5.35f },
        { BandaRadio.Banda40m, 7.15f },
        { BandaRadio.Banda30m, 10.125f },
        { BandaRadio.Banda20m, 14.175f },
        { BandaRadio.Banda17m, 18.118f },
        { BandaRadio.Banda15m, 21.225f },
        { BandaRadio.Banda12m, 24.94f },
        { BandaRadio.Banda10m, 28.85f }
    };

    /// <summary>
    /// Inicializa el analizador entrenando el modelo de regresion con datos sinteticos.
    /// </summary>
    public AnalizadorPropagacionMlNet()
    {
        _contextoMl = new MLContext(seed: 42);

        List<DatoEntrenamientoPropagacion> datosEntrenamiento = GenerarDatosSinteticos();

        IDataView vistaEntrenamiento = _contextoMl.Data.LoadFromEnumerable(datosEntrenamiento);

        IEstimator<ITransformer> pipeline = _contextoMl.Transforms.NormalizeMinMax("Sfi")
            .Append(_contextoMl.Transforms.NormalizeMinMax("IndiceK"))
            .Append(_contextoMl.Transforms.NormalizeMinMax("IndiceA"))
            .Append(_contextoMl.Transforms.NormalizeMinMax("ManchasSolares"))
            .Append(_contextoMl.Transforms.NormalizeMinMax("HoraUtc"))
            .Append(_contextoMl.Transforms.NormalizeMinMax("MesDelAnio"))
            .Append(_contextoMl.Transforms.NormalizeMinMax("NumeroBanda"))
            .Append(_contextoMl.Transforms.Concatenate("Features",
                "Sfi", "IndiceK", "IndiceA", "ManchasSolares", "HoraUtc", "MesDelAnio", "NumeroBanda"))
            .Append(_contextoMl.Transforms.CopyColumns("Label", "ProbabilidadApertura"))
            .Append(_contextoMl.Regression.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 20,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.2));

        ITransformer modeloEntrenado = pipeline.Fit(vistaEntrenamiento);

        _motorPrediccion = _contextoMl.Model.CreatePredictionEngine<DatoEntrenamientoPropagacion, PrediccionPropagacionSalida>(modeloEntrenado);
    }

    /// <inheritdoc />
    public Task<PrediccionPropagacionIa> PredecirAsync(
        IndicesSolares indices,
        BandaRadio banda,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(indices);
        tokenCancelacion.ThrowIfCancellationRequested();

        PrediccionPropagacionIa resultado = RealizarPrediccion(indices, banda);
        return Task.FromResult(resultado);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PrediccionPropagacionIa>> PredecirTodasLasBandasAsync(
        IndicesSolares indices,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(indices);
        tokenCancelacion.ThrowIfCancellationRequested();

        List<PrediccionPropagacionIa> predicciones = new();

        foreach (KeyValuePair<BandaRadio, float> banda in _bandasHf)
        {
            PrediccionPropagacionIa prediccion = RealizarPrediccion(indices, banda.Key);
            predicciones.Add(prediccion);
        }

        return Task.FromResult<IReadOnlyList<PrediccionPropagacionIa>>(predicciones.AsReadOnly());
    }

    /// <summary>
    /// Realiza la prediccion para una banda especifica usando el motor de prediccion.
    /// </summary>
    private PrediccionPropagacionIa RealizarPrediccion(IndicesSolares indices, BandaRadio banda)
    {
        float numeroBanda = ObtenerNumeroBanda(banda);
        DateTime ahora = DateTime.UtcNow;

        DatoEntrenamientoPropagacion entrada = new()
        {
            Sfi = indices.Sfi,
            IndiceK = indices.Kp,
            IndiceA = indices.Ap,
            ManchasSolares = (float)indices.NumeroManchasSolares,
            HoraUtc = ahora.Hour,
            MesDelAnio = ahora.Month,
            NumeroBanda = numeroBanda
        };

        float probabilidad;
        lock (_bloqueo)
        {
            PrediccionPropagacionSalida salida = _motorPrediccion.Predict(entrada);
            probabilidad = Math.Clamp(salida.ProbabilidadApertura, 0f, 1f);
        }

        double nivelConfianza = CalcularConfianza(indices, banda);
        TimeOnly? horaOptima = EstimarHoraOptima(banda);
        double? mufEstimado = EstimarMuf(indices);

        return new PrediccionPropagacionIa(
            banda,
            Math.Round(probabilidad, 4),
            Math.Round(nivelConfianza, 4),
            horaOptima,
            mufEstimado);
    }

    /// <summary>
    /// Obtiene la frecuencia central de la banda en MHz como identificador numerico.
    /// </summary>
    private static float ObtenerNumeroBanda(BandaRadio banda)
    {
        if (_bandasHf.TryGetValue(banda, out float frecuencia))
        {
            return frecuencia;
        }

        // Para bandas no HF, devolver un valor por defecto
        return 14.175f;
    }

    /// <summary>
    /// Calcula un nivel de confianza basado en la calidad de los indices solares.
    /// Indices extremos o perturbaciones geomagneticas reducen la confianza.
    /// </summary>
    private static double CalcularConfianza(IndicesSolares indices, BandaRadio banda)
    {
        double confianza = 0.85;

        // Perturbaciones geomagneticas reducen la confianza
        if (indices.CondicionesPerturbadas)
        {
            confianza -= 0.15;
        }

        // Indices fuera de rango tipico reducen confianza
        if (indices.Sfi < 65 || indices.Sfi > 250)
        {
            confianza -= 0.10;
        }

        // Bandas extremas (160m, 10m) son mas dificiles de predecir
        if (banda is BandaRadio.Banda160m or BandaRadio.Banda10m)
        {
            confianza -= 0.05;
        }

        return Math.Clamp(confianza, 0.3, 0.95);
    }

    /// <summary>
    /// Estima la hora optima de operacion para la banda dada.
    /// Bandas altas son mejores de dia, bandas bajas de noche.
    /// </summary>
    private static TimeOnly? EstimarHoraOptima(BandaRadio banda)
    {
        return banda switch
        {
            BandaRadio.Banda160m => new TimeOnly(3, 0),
            BandaRadio.Banda80m => new TimeOnly(2, 0),
            BandaRadio.Banda60m => new TimeOnly(22, 0),
            BandaRadio.Banda40m => new TimeOnly(21, 0),
            BandaRadio.Banda30m => new TimeOnly(20, 0),
            BandaRadio.Banda20m => new TimeOnly(14, 0),
            BandaRadio.Banda17m => new TimeOnly(13, 0),
            BandaRadio.Banda15m => new TimeOnly(12, 0),
            BandaRadio.Banda12m => new TimeOnly(12, 0),
            BandaRadio.Banda10m => new TimeOnly(11, 0),
            _ => null
        };
    }

    /// <summary>
    /// Estima la MUF (frecuencia maxima utilizable) a partir del SFI.
    /// Relacion aproximada: MUF ~ SFI * 0.12 + 5.
    /// </summary>
    private static double? EstimarMuf(IndicesSolares indices)
    {
        double muf = indices.Sfi * 0.12 + 5.0;
        return Math.Round(Math.Clamp(muf, 3.0, 50.0), 1);
    }

    /// <summary>
    /// Genera datos sinteticos de entrenamiento basados en relaciones conocidas
    /// entre indices solares y apertura de bandas HF.
    /// </summary>
    private static List<DatoEntrenamientoPropagacion> GenerarDatosSinteticos()
    {
        List<DatoEntrenamientoPropagacion> datos = new();
        Random aleatorio = new(42);

        // Rangos de SFI para generar variedad
        int[] valoresSfi = [65, 75, 85, 95, 105, 120, 135, 150, 170, 200, 250];
        int[] valoresKp = [0, 1, 2, 3, 4, 5, 7, 9];
        float[] horas = [0, 3, 6, 9, 12, 15, 18, 21];

        foreach (int sfi in valoresSfi)
        {
            foreach (int kp in valoresKp)
            {
                foreach (float hora in horas)
                {
                    int ap = kp * 4 + aleatorio.Next(0, 5);
                    float manchas = sfi * 0.8f + aleatorio.Next(-10, 10);

                    foreach (KeyValuePair<BandaRadio, float> banda in _bandasHf)
                    {
                        float probabilidad = CalcularProbabilidadSintetica(
                            sfi, kp, hora, banda.Value, aleatorio);

                        datos.Add(new DatoEntrenamientoPropagacion
                        {
                            Sfi = sfi,
                            IndiceK = kp,
                            IndiceA = ap,
                            ManchasSolares = Math.Max(0, manchas),
                            HoraUtc = hora,
                            MesDelAnio = aleatorio.Next(1, 13),
                            NumeroBanda = banda.Value,
                            ProbabilidadApertura = probabilidad
                        });
                    }
                }
            }
        }

        return datos;
    }

    /// <summary>
    /// Calcula la probabilidad sintetica de apertura basada en fisica de propagacion conocida.
    /// </summary>
    private static float CalcularProbabilidadSintetica(
        int sfi, int kp, float hora, float frecuenciaMhz, Random aleatorio)
    {
        float probabilidad;
        bool esDeDia = hora >= 6 && hora <= 18;
        bool esDeNoche = !esDeDia;

        // Bandas altas (>21 MHz): necesitan SFI alto, mejor de dia
        if (frecuenciaMhz >= 21.0f)
        {
            probabilidad = sfi >= 150 ? 0.85f :
                           sfi >= 120 ? 0.55f :
                           sfi >= 100 ? 0.25f : 0.05f;

            if (esDeDia) probabilidad *= 1.2f;
            else probabilidad *= 0.3f;
        }
        // Bandas medias (14-20 MHz): SFI moderado, flexibles
        else if (frecuenciaMhz >= 14.0f)
        {
            probabilidad = sfi >= 120 ? 0.90f :
                           sfi >= 80 ? 0.70f :
                           sfi >= 65 ? 0.40f : 0.20f;

            if (esDeDia) probabilidad *= 1.1f;
            else probabilidad *= 0.7f;
        }
        // Bandas bajas (<14 MHz): siempre abiertas, mejor de noche
        else
        {
            probabilidad = 0.80f;

            if (esDeNoche) probabilidad *= 1.15f;
            else probabilidad *= 0.85f;

            // SFI alto puede mejorar ligeramente bandas bajas
            if (sfi >= 120) probabilidad *= 1.05f;
        }

        // Perturbaciones geomagneticas degradan todas las bandas
        if (kp >= 4)
        {
            float factorDegradacion = 1.0f - (kp - 3) * 0.12f;
            probabilidad *= Math.Max(factorDegradacion, 0.1f);
        }

        // Agregar algo de ruido
        probabilidad += (float)(aleatorio.NextDouble() * 0.08 - 0.04);

        return Math.Clamp(probabilidad, 0.0f, 1.0f);
    }
}
