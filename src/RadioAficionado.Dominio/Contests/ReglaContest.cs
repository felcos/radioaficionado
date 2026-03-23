using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Contests;

/// <summary>
/// Define las reglas completas de un concurso de radioaficionado.
/// Incluye bandas permitidas, modos, tipo de intercambio, multiplicadores y duración.
/// </summary>
public class ReglaContest
{
    /// <summary>
    /// Tipo de contest que estas reglas definen.
    /// </summary>
    public TipoContest Tipo { get; }

    /// <summary>
    /// Nombre completo del contest.
    /// </summary>
    public string Nombre { get; }

    /// <summary>
    /// Abreviatura estándar del contest (usada en archivos Cabrillo).
    /// </summary>
    public string Abreviatura { get; }

    /// <summary>
    /// Bandas de radio permitidas en este contest.
    /// </summary>
    public IReadOnlyList<BandaRadio> BandasPermitidas { get; }

    /// <summary>
    /// Modos de operación permitidos en este contest.
    /// </summary>
    public IReadOnlyList<ModoOperacion> ModosPermitidos { get; }

    /// <summary>
    /// Tipo de intercambio requerido para validar cada QSO.
    /// </summary>
    public TipoIntercambio TipoIntercambio { get; }

    /// <summary>
    /// Método utilizado para calcular los multiplicadores.
    /// </summary>
    public MetodoMultiplicador MetodoMultiplicador { get; }

    /// <summary>
    /// Duración total del contest en horas.
    /// </summary>
    public int DuracionHoras { get; }

    /// <summary>
    /// Mes típico en que se celebra el contest (1-12).
    /// </summary>
    public int FechaTipicaMes { get; }

    /// <summary>
    /// Crea una nueva regla de contest con todos sus parámetros.
    /// </summary>
    /// <param name="tipo">Tipo de contest.</param>
    /// <param name="nombre">Nombre completo.</param>
    /// <param name="abreviatura">Abreviatura Cabrillo.</param>
    /// <param name="bandasPermitidas">Bandas permitidas.</param>
    /// <param name="modosPermitidos">Modos permitidos.</param>
    /// <param name="tipoIntercambio">Tipo de intercambio requerido.</param>
    /// <param name="metodoMultiplicador">Método de cálculo de multiplicadores.</param>
    /// <param name="duracionHoras">Duración en horas.</param>
    /// <param name="fechaTipicaMes">Mes típico (1-12).</param>
    /// <exception cref="ArgumentException">Si algún parámetro es inválido.</exception>
    public ReglaContest(
        TipoContest tipo,
        string nombre,
        string abreviatura,
        IReadOnlyList<BandaRadio> bandasPermitidas,
        IReadOnlyList<ModoOperacion> modosPermitidos,
        TipoIntercambio tipoIntercambio,
        MetodoMultiplicador metodoMultiplicador,
        int duracionHoras,
        int fechaTipicaMes)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del contest no puede estar vacío.", nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(abreviatura))
        {
            throw new ArgumentException("La abreviatura del contest no puede estar vacía.", nameof(abreviatura));
        }

        if (bandasPermitidas is null || bandasPermitidas.Count == 0)
        {
            throw new ArgumentException("Debe haber al menos una banda permitida.", nameof(bandasPermitidas));
        }

        if (modosPermitidos is null || modosPermitidos.Count == 0)
        {
            throw new ArgumentException("Debe haber al menos un modo permitido.", nameof(modosPermitidos));
        }

        if (duracionHoras <= 0)
        {
            throw new ArgumentException("La duración debe ser positiva.", nameof(duracionHoras));
        }

        if (fechaTipicaMes < 1 || fechaTipicaMes > 12)
        {
            throw new ArgumentException("El mes típico debe estar entre 1 y 12.", nameof(fechaTipicaMes));
        }

        Tipo = tipo;
        Nombre = nombre;
        Abreviatura = abreviatura;
        BandasPermitidas = bandasPermitidas;
        ModosPermitidos = modosPermitidos;
        TipoIntercambio = tipoIntercambio;
        MetodoMultiplicador = metodoMultiplicador;
        DuracionHoras = duracionHoras;
        FechaTipicaMes = fechaTipicaMes;
    }
}
