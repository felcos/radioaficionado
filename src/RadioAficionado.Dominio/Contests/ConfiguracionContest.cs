using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Contests;

/// <summary>
/// Configuración de la participación de una estación en un contest.
/// Contiene datos del operador necesarios para generar el archivo Cabrillo.
/// </summary>
/// <param name="Indicativo">Indicativo del operador participante.</param>
/// <param name="CategoriaOperador">Categoría de operador (SINGLE-OP, MULTI-OP, CHECKLOG).</param>
/// <param name="CategoriaBanda">Categoría de banda (ALL, 160M, 80M, 40M, 20M, 15M, 10M).</param>
/// <param name="CategoriaModo">Categoría de modo (SSB, CW, RTTY, MIXED).</param>
/// <param name="CategoriaPotencia">Categoría de potencia (HIGH, LOW, QRP).</param>
/// <param name="NombreOperador">Nombre del operador.</param>
/// <param name="Club">Club al que pertenece el operador. Null si no aplica.</param>
/// <param name="Ubicacion">Ubicación/localizador de la estación.</param>
public record ConfiguracionContest(
    Indicativo Indicativo,
    string CategoriaOperador,
    string CategoriaBanda,
    string CategoriaModo,
    string CategoriaPotencia,
    string NombreOperador,
    string? Club = null,
    string? Ubicacion = null);
