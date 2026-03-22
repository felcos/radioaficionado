namespace RadioAficionado.Dominio.ObjetosDeValor;

/// <summary>
/// Bandas de radioaficionado desde LF hasta microondas.
/// </summary>
public enum BandaRadio
{
    /// <summary>Banda de 2200 metros (135.7 - 137.8 kHz).</summary>
    Banda2200m,

    /// <summary>Banda de 630 metros (472 - 479 kHz).</summary>
    Banda630m,

    /// <summary>Banda de 160 metros (1.8 - 2.0 MHz).</summary>
    Banda160m,

    /// <summary>Banda de 80 metros (3.5 - 4.0 MHz).</summary>
    Banda80m,

    /// <summary>Banda de 60 metros (5.3515 - 5.3665 MHz).</summary>
    Banda60m,

    /// <summary>Banda de 40 metros (7.0 - 7.3 MHz).</summary>
    Banda40m,

    /// <summary>Banda de 30 metros (10.1 - 10.15 MHz).</summary>
    Banda30m,

    /// <summary>Banda de 20 metros (14.0 - 14.35 MHz).</summary>
    Banda20m,

    /// <summary>Banda de 17 metros (18.068 - 18.168 MHz).</summary>
    Banda17m,

    /// <summary>Banda de 15 metros (21.0 - 21.45 MHz).</summary>
    Banda15m,

    /// <summary>Banda de 12 metros (24.89 - 24.99 MHz).</summary>
    Banda12m,

    /// <summary>Banda de 10 metros (28.0 - 29.7 MHz).</summary>
    Banda10m,

    /// <summary>Banda de 6 metros (50.0 - 54.0 MHz).</summary>
    Banda6m,

    /// <summary>Banda de 4 metros (70.0 - 70.5 MHz). Solo Region 1.</summary>
    Banda4m,

    /// <summary>Banda de 2 metros (144.0 - 148.0 MHz).</summary>
    Banda2m,

    /// <summary>Banda de 1.25 metros (219 - 225 MHz). Solo Region 2.</summary>
    Banda1_25m,

    /// <summary>Banda de 70 centimetros (420 - 450 MHz).</summary>
    Banda70cm,

    /// <summary>Banda de 33 centimetros (902 - 928 MHz). Solo Region 2.</summary>
    Banda33cm,

    /// <summary>Banda de 23 centimetros (1240 - 1300 MHz).</summary>
    Banda23cm,

    /// <summary>Banda de 13 centimetros (2300 - 2450 MHz).</summary>
    Banda13cm,

    /// <summary>Banda de 9 centimetros (3300 - 3500 MHz).</summary>
    Banda9cm,

    /// <summary>Banda de 5 centimetros (5650 - 5925 MHz).</summary>
    Banda5cm,

    /// <summary>Banda de 3 centimetros (10.0 - 10.5 GHz).</summary>
    Banda3cm,

    /// <summary>Banda de 1.2 centimetros (24.0 - 24.25 GHz).</summary>
    Banda1_2cm
}

/// <summary>
/// Metodos de extension para <see cref="BandaRadio"/>.
/// </summary>
public static class BandaRadioExtensiones
{
    /// <summary>
    /// Obtiene el rango de frecuencias (inicio y fin) de la banda especificada.
    /// Usa los rangos mas amplios (Region 2 para bandas que varian por region).
    /// </summary>
    /// <param name="banda">La banda de radio.</param>
    /// <returns>Tupla con la frecuencia de inicio y fin de la banda.</returns>
    public static (Frecuencia Inicio, Frecuencia Fin) ObtenerRangoFrecuencia(this BandaRadio banda)
    {
        return banda switch
        {
            BandaRadio.Banda2200m => (Frecuencia.DesdeKHz(135.7), Frecuencia.DesdeKHz(137.8)),
            BandaRadio.Banda630m => (Frecuencia.DesdeKHz(472.0), Frecuencia.DesdeKHz(479.0)),
            BandaRadio.Banda160m => (Frecuencia.DesdeMHz(1.8), Frecuencia.DesdeMHz(2.0)),
            BandaRadio.Banda80m => (Frecuencia.DesdeMHz(3.5), Frecuencia.DesdeMHz(4.0)),
            BandaRadio.Banda60m => (Frecuencia.DesdeMHz(5.3515), Frecuencia.DesdeMHz(5.3665)),
            BandaRadio.Banda40m => (Frecuencia.DesdeMHz(7.0), Frecuencia.DesdeMHz(7.3)),
            BandaRadio.Banda30m => (Frecuencia.DesdeMHz(10.1), Frecuencia.DesdeMHz(10.15)),
            BandaRadio.Banda20m => (Frecuencia.DesdeMHz(14.0), Frecuencia.DesdeMHz(14.35)),
            BandaRadio.Banda17m => (Frecuencia.DesdeMHz(18.068), Frecuencia.DesdeMHz(18.168)),
            BandaRadio.Banda15m => (Frecuencia.DesdeMHz(21.0), Frecuencia.DesdeMHz(21.45)),
            BandaRadio.Banda12m => (Frecuencia.DesdeMHz(24.89), Frecuencia.DesdeMHz(24.99)),
            BandaRadio.Banda10m => (Frecuencia.DesdeMHz(28.0), Frecuencia.DesdeMHz(29.7)),
            BandaRadio.Banda6m => (Frecuencia.DesdeMHz(50.0), Frecuencia.DesdeMHz(54.0)),
            BandaRadio.Banda4m => (Frecuencia.DesdeMHz(70.0), Frecuencia.DesdeMHz(70.5)),
            BandaRadio.Banda2m => (Frecuencia.DesdeMHz(144.0), Frecuencia.DesdeMHz(148.0)),
            BandaRadio.Banda1_25m => (Frecuencia.DesdeMHz(219.0), Frecuencia.DesdeMHz(225.0)),
            BandaRadio.Banda70cm => (Frecuencia.DesdeMHz(420.0), Frecuencia.DesdeMHz(450.0)),
            BandaRadio.Banda33cm => (Frecuencia.DesdeMHz(902.0), Frecuencia.DesdeMHz(928.0)),
            BandaRadio.Banda23cm => (Frecuencia.DesdeMHz(1240.0), Frecuencia.DesdeMHz(1300.0)),
            BandaRadio.Banda13cm => (Frecuencia.DesdeMHz(2300.0), Frecuencia.DesdeMHz(2450.0)),
            BandaRadio.Banda9cm => (Frecuencia.DesdeMHz(3300.0), Frecuencia.DesdeMHz(3500.0)),
            BandaRadio.Banda5cm => (Frecuencia.DesdeMHz(5650.0), Frecuencia.DesdeMHz(5925.0)),
            BandaRadio.Banda3cm => (Frecuencia.DesdeMHz(10000.0), Frecuencia.DesdeMHz(10500.0)),
            BandaRadio.Banda1_2cm => (Frecuencia.DesdeMHz(24000.0), Frecuencia.DesdeMHz(24250.0)),
            _ => throw new ArgumentOutOfRangeException(nameof(banda), banda, "Banda de radio no reconocida.")
        };
    }

    /// <summary>
    /// Obtiene el nombre legible de la banda.
    /// </summary>
    /// <param name="banda">La banda de radio.</param>
    /// <returns>Nombre descriptivo de la banda.</returns>
    public static string ObtenerNombre(this BandaRadio banda)
    {
        return banda switch
        {
            BandaRadio.Banda2200m => "2200 metros",
            BandaRadio.Banda630m => "630 metros",
            BandaRadio.Banda160m => "160 metros",
            BandaRadio.Banda80m => "80 metros",
            BandaRadio.Banda60m => "60 metros",
            BandaRadio.Banda40m => "40 metros",
            BandaRadio.Banda30m => "30 metros",
            BandaRadio.Banda20m => "20 metros",
            BandaRadio.Banda17m => "17 metros",
            BandaRadio.Banda15m => "15 metros",
            BandaRadio.Banda12m => "12 metros",
            BandaRadio.Banda10m => "10 metros",
            BandaRadio.Banda6m => "6 metros",
            BandaRadio.Banda4m => "4 metros",
            BandaRadio.Banda2m => "2 metros",
            BandaRadio.Banda1_25m => "1.25 metros",
            BandaRadio.Banda70cm => "70 centimetros",
            BandaRadio.Banda33cm => "33 centimetros",
            BandaRadio.Banda23cm => "23 centimetros",
            BandaRadio.Banda13cm => "13 centimetros",
            BandaRadio.Banda9cm => "9 centimetros",
            BandaRadio.Banda5cm => "5 centimetros",
            BandaRadio.Banda3cm => "3 centimetros",
            BandaRadio.Banda1_2cm => "1.2 centimetros",
            _ => throw new ArgumentOutOfRangeException(nameof(banda), banda, "Banda de radio no reconocida.")
        };
    }

    /// <summary>
    /// Indica si esta banda es exclusiva de una region ITU especifica.
    /// </summary>
    /// <param name="banda">La banda de radio.</param>
    /// <returns>True si la banda solo esta disponible en una region ITU.</returns>
    public static bool EsExclusivaDeRegion(this BandaRadio banda)
    {
        return banda is BandaRadio.Banda4m or BandaRadio.Banda1_25m or BandaRadio.Banda33cm;
    }

    /// <summary>
    /// Obtiene la categoria de la banda (HF, VHF, UHF, Microondas, LF/MF).
    /// </summary>
    /// <param name="banda">La banda de radio.</param>
    /// <returns>La <see cref="CategoriaBanda"/> correspondiente.</returns>
    public static CategoriaBanda ObtenerCategoria(this BandaRadio banda)
    {
        return banda switch
        {
            BandaRadio.Banda2200m or BandaRadio.Banda630m => CategoriaBanda.LfMf,
            BandaRadio.Banda160m or BandaRadio.Banda80m or BandaRadio.Banda60m or
            BandaRadio.Banda40m or BandaRadio.Banda30m or BandaRadio.Banda20m or
            BandaRadio.Banda17m or BandaRadio.Banda15m or BandaRadio.Banda12m or
            BandaRadio.Banda10m => CategoriaBanda.Hf,
            BandaRadio.Banda6m or BandaRadio.Banda4m or BandaRadio.Banda2m or
            BandaRadio.Banda1_25m => CategoriaBanda.Vhf,
            BandaRadio.Banda70cm or BandaRadio.Banda33cm or BandaRadio.Banda23cm => CategoriaBanda.Uhf,
            _ => CategoriaBanda.Microondas
        };
    }

    /// <summary>
    /// Determina a que banda de radioaficionado pertenece una frecuencia dada.
    /// </summary>
    /// <param name="frecuencia">La frecuencia a evaluar.</param>
    /// <returns>La banda correspondiente, o null si la frecuencia no pertenece a ninguna banda de radioaficionado.</returns>
    public static BandaRadio? DesdeFrecuencia(Frecuencia frecuencia)
    {
        foreach (BandaRadio banda in Enum.GetValues<BandaRadio>())
        {
            (Frecuencia inicio, Frecuencia fin) = banda.ObtenerRangoFrecuencia();
            if (frecuencia.Hz >= inicio.Hz && frecuencia.Hz <= fin.Hz)
            {
                return banda;
            }
        }

        return null;
    }
}

/// <summary>
/// Categoria de banda segun el rango de frecuencias.
/// </summary>
public enum CategoriaBanda
{
    /// <summary>Baja y media frecuencia (LF/MF) — por debajo de 3 MHz.</summary>
    LfMf,

    /// <summary>Alta frecuencia (HF) — 3 a 30 MHz.</summary>
    Hf,

    /// <summary>Muy alta frecuencia (VHF) — 30 a 300 MHz.</summary>
    Vhf,

    /// <summary>Ultra alta frecuencia (UHF) — 300 MHz a 3 GHz.</summary>
    Uhf,

    /// <summary>Microondas — por encima de 3 GHz.</summary>
    Microondas
}
