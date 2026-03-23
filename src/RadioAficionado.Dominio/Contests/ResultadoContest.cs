namespace RadioAficionado.Dominio.Contests;

/// <summary>
/// Resultado calculado de la participación en un contest.
/// Contiene el desglose completo de puntuación, multiplicadores y estadísticas.
/// </summary>
/// <param name="QsosValidos">Número de QSOs que cumplen las reglas del contest.</param>
/// <param name="Puntos">Puntos brutos acumulados por los QSOs válidos.</param>
/// <param name="Multiplicadores">Total de multiplicadores únicos obtenidos.</param>
/// <param name="PuntuacionFinal">Puntuación final = Puntos × Multiplicadores.</param>
/// <param name="QsosDuplicados">Número de QSOs duplicados detectados.</param>
/// <param name="QsosInvalidos">Número de QSOs que no cumplen las reglas (banda/modo incorrecto, etc.).</param>
public record ResultadoContest(
    int QsosValidos,
    int Puntos,
    int Multiplicadores,
    long PuntuacionFinal,
    int QsosDuplicados,
    int QsosInvalidos)
{
    /// <summary>
    /// Crea un resultado vacío (sin QSOs procesados).
    /// </summary>
    /// <returns>Un <see cref="ResultadoContest"/> con todos los valores en cero.</returns>
    public static ResultadoContest Vacio()
    {
        return new ResultadoContest(
            QsosValidos: 0,
            Puntos: 0,
            Multiplicadores: 0,
            PuntuacionFinal: 0,
            QsosDuplicados: 0,
            QsosInvalidos: 0);
    }
}
