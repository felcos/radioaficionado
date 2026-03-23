using FluentAssertions;
using RadioAficionado.Dominio.Contests;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.Contests;

/// <summary>
/// Tests del motor de evaluación de concursos de radioaficionado.
/// </summary>
public class MotorContestTests
{
    private readonly MotorContest _motor = new();

    private static ReglaContest CrearReglaCqWw()
    {
        return MotorContest.RegistroDeReglas[TipoContest.CqWw];
    }

    private static ReglaContest CrearReglaCqWpx()
    {
        return MotorContest.RegistroDeReglas[TipoContest.CqWpx];
    }

    private static ReglaContest CrearReglaMonoModo()
    {
        return MotorContest.RegistroDeReglas[TipoContest.ArrlDx];
    }

    private static Qso CrearQso(
        string indicativoPropio,
        string indicativoContacto,
        double frecuenciaMHz,
        ModoOperacion modo,
        DateTimeOffset? fecha = null)
    {
        DateTimeOffset fechaQso = fecha ?? DateTimeOffset.UtcNow.AddHours(-1);
        return Qso.Crear(
            new Indicativo(indicativoPropio),
            new Indicativo(indicativoContacto),
            fechaQso,
            Frecuencia.DesdeMHz(frecuenciaMHz),
            modo,
            "59");
    }

    // --- CalcularPuntuacion ---

    [Fact]
    public void CalcularPuntuacion_ConCeroQsos_RetornaResultadoVacio()
    {
        // Arrange
        List<Qso> qsos = new();
        ReglaContest regla = CrearReglaCqWw();

        // Act
        ResultadoContest resultado = _motor.CalcularPuntuacion(qsos, regla);

        // Assert
        resultado.QsosValidos.Should().Be(0);
        resultado.Puntos.Should().Be(0);
        resultado.Multiplicadores.Should().Be(0);
        resultado.PuntuacionFinal.Should().Be(0);
        resultado.QsosDuplicados.Should().Be(0);
        resultado.QsosInvalidos.Should().Be(0);
    }

    [Fact]
    public void CalcularPuntuacion_ConQsosValidos_RetornaPuntosCorrectos()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB),
            CrearQso("EA4ABC", "W1AW", 14.250, ModoOperacion.SSB),
            CrearQso("EA4ABC", "VK2ABC", 21.300, ModoOperacion.SSB)
        };
        ReglaContest regla = CrearReglaCqWw();

        // Act
        ResultadoContest resultado = _motor.CalcularPuntuacion(qsos, regla);

        // Assert
        resultado.QsosValidos.Should().Be(3);
        resultado.Puntos.Should().Be(3); // SSB = 1 punto cada uno
        resultado.QsosDuplicados.Should().Be(0);
        resultado.QsosInvalidos.Should().Be(0);
    }

    [Fact]
    public void CalcularPuntuacion_ConQsosDuplicados_DetectaDuplicados()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB, DateTimeOffset.UtcNow.AddHours(-3)),
            CrearQso("EA4ABC", "JA1XYZ", 14.250, ModoOperacion.SSB, DateTimeOffset.UtcNow.AddHours(-2)), // Duplicado: mismo indicativo, misma banda (20m)
            CrearQso("EA4ABC", "W1AW", 14.300, ModoOperacion.SSB, DateTimeOffset.UtcNow.AddHours(-1))
        };
        ReglaContest regla = CrearReglaCqWw();

        // Act
        ResultadoContest resultado = _motor.CalcularPuntuacion(qsos, regla);

        // Assert
        resultado.QsosValidos.Should().Be(2);
        resultado.QsosDuplicados.Should().Be(1);
    }

    [Fact]
    public void CalcularPuntuacion_ConQsosInvalidos_BandaNoPermitida_ExcluyeQsos()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB),
            CrearQso("EA4ABC", "W1AW", 144.300, ModoOperacion.SSB) // VHF - no permitido en HF contest
        };
        ReglaContest regla = CrearReglaCqWw();

        // Act
        ResultadoContest resultado = _motor.CalcularPuntuacion(qsos, regla);

        // Assert
        resultado.QsosValidos.Should().Be(1);
        resultado.QsosInvalidos.Should().Be(1);
    }

    [Fact]
    public void CalcularPuntuacion_ConQsosInvalidos_ModoNoPermitido_ExcluyeQsos()
    {
        // Arrange - ARRL DX solo permite SSB
        List<Qso> qsos = new()
        {
            CrearQso("EA4ABC", "W1AW", 14.200, ModoOperacion.SSB),
            CrearQso("EA4ABC", "JA1XYZ", 14.050, ModoOperacion.CW) // CW no permitido en ARRL DX SSB
        };
        ReglaContest regla = CrearReglaMonoModo();

        // Act
        ResultadoContest resultado = _motor.CalcularPuntuacion(qsos, regla);

        // Assert
        resultado.QsosValidos.Should().Be(1);
        resultado.QsosInvalidos.Should().Be(1);
    }

    [Fact]
    public void CalcularPuntuacion_PuntuacionFinal_EsProductoDePuntosPorMultiplicadores()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB),
            CrearQso("EA4ABC", "W1AW", 14.250, ModoOperacion.SSB),
            CrearQso("EA4ABC", "VK2ABC", 21.300, ModoOperacion.SSB)
        };
        ReglaContest regla = CrearReglaCqWw();

        // Act
        ResultadoContest resultado = _motor.CalcularPuntuacion(qsos, regla);

        // Assert
        resultado.PuntuacionFinal.Should().Be((long)resultado.Puntos * resultado.Multiplicadores);
    }

    [Fact]
    public void CalcularPuntuacion_CwValeMasQueSsb()
    {
        // Arrange
        List<Qso> qsosSsb = new() { CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB) };
        List<Qso> qsosCw = new() { CrearQso("EA4ABC", "JA1XYZ", 14.050, ModoOperacion.CW) };
        ReglaContest regla = CrearReglaCqWw();

        // Act
        ResultadoContest resultadoSsb = _motor.CalcularPuntuacion(qsosSsb, regla);
        ResultadoContest resultadoCw = _motor.CalcularPuntuacion(qsosCw, regla);

        // Assert
        resultadoCw.Puntos.Should().BeGreaterThan(resultadoSsb.Puntos);
    }

    // --- EsDuplicado ---

    [Fact]
    public void EsDuplicado_MismoIndicativoMismaBanda_RetornaTrue()
    {
        // Arrange
        Qso qso1 = CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB, DateTimeOffset.UtcNow.AddHours(-2));
        Qso qso2 = CrearQso("EA4ABC", "JA1XYZ", 14.250, ModoOperacion.SSB, DateTimeOffset.UtcNow.AddHours(-1));
        List<Qso> anteriores = new() { qso1 };
        ReglaContest regla = CrearReglaCqWw();

        // Act
        bool esDuplicado = _motor.EsDuplicado(qso2, anteriores, regla);

        // Assert
        esDuplicado.Should().BeTrue();
    }

    [Fact]
    public void EsDuplicado_MismoIndicativoDiferenteBanda_RetornaFalse()
    {
        // Arrange
        Qso qso1 = CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB, DateTimeOffset.UtcNow.AddHours(-2));
        Qso qso2 = CrearQso("EA4ABC", "JA1XYZ", 21.300, ModoOperacion.SSB, DateTimeOffset.UtcNow.AddHours(-1));
        List<Qso> anteriores = new() { qso1 };
        ReglaContest regla = CrearReglaCqWw();

        // Act
        bool esDuplicado = _motor.EsDuplicado(qso2, anteriores, regla);

        // Assert
        esDuplicado.Should().BeFalse();
    }

    [Fact]
    public void EsDuplicado_DiferenteIndicativoMismaBanda_RetornaFalse()
    {
        // Arrange
        Qso qso1 = CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB, DateTimeOffset.UtcNow.AddHours(-2));
        Qso qso2 = CrearQso("EA4ABC", "W1AW", 14.250, ModoOperacion.SSB, DateTimeOffset.UtcNow.AddHours(-1));
        List<Qso> anteriores = new() { qso1 };
        ReglaContest regla = CrearReglaCqWw();

        // Act
        bool esDuplicado = _motor.EsDuplicado(qso2, anteriores, regla);

        // Assert
        esDuplicado.Should().BeFalse();
    }

    [Fact]
    public void EsDuplicado_MismoIndicativoMismaBandaDiferenteModo_ContestMultiModo_RetornaFalse()
    {
        // Arrange - CQ WW permite SSB y CW (multimodo)
        Qso qso1 = CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB, DateTimeOffset.UtcNow.AddHours(-2));
        Qso qso2 = CrearQso("EA4ABC", "JA1XYZ", 14.050, ModoOperacion.CW, DateTimeOffset.UtcNow.AddHours(-1));
        List<Qso> anteriores = new() { qso1 };
        ReglaContest regla = CrearReglaCqWw();

        // Act
        bool esDuplicado = _motor.EsDuplicado(qso2, anteriores, regla);

        // Assert
        esDuplicado.Should().BeFalse();
    }

    [Fact]
    public void EsDuplicado_SinQsosAnteriores_RetornaFalse()
    {
        // Arrange
        Qso qso = CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB);
        List<Qso> anteriores = new();
        ReglaContest regla = CrearReglaCqWw();

        // Act
        bool esDuplicado = _motor.EsDuplicado(qso, anteriores, regla);

        // Assert
        esDuplicado.Should().BeFalse();
    }

    // --- CalcularMultiplicadores ---

    [Fact]
    public void CalcularMultiplicadores_PorPrefijo_CuentaPrefijosUnicos()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB),
            CrearQso("EA4ABC", "JA2ZZZ", 14.250, ModoOperacion.SSB), // Mismo prefijo JA
            CrearQso("EA4ABC", "W1AW", 21.300, ModoOperacion.SSB)    // Prefijo diferente W
        };
        ReglaContest regla = CrearReglaCqWpx();

        // Act
        int multiplicadores = _motor.CalcularMultiplicadores(qsos, regla);

        // Assert
        multiplicadores.Should().Be(2); // JA y W
    }

    [Fact]
    public void CalcularMultiplicadores_SinQsos_RetornaCero()
    {
        // Arrange
        List<Qso> qsos = new();
        ReglaContest regla = CrearReglaCqWw();

        // Act
        int multiplicadores = _motor.CalcularMultiplicadores(qsos, regla);

        // Assert
        multiplicadores.Should().Be(0);
    }

    [Fact]
    public void CalcularMultiplicadores_PorDxcc_CuentaPorBanda()
    {
        // Arrange - mismo prefijo en diferentes bandas = diferentes multiplicadores
        List<Qso> qsos = new()
        {
            CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB),
            CrearQso("EA4ABC", "JA2ZZZ", 21.300, ModoOperacion.SSB) // Mismo prefijo JA pero diferente banda
        };
        ReglaContest regla = CrearReglaMonoModo(); // ARRL DX usa DXCC

        // Act
        int multiplicadores = _motor.CalcularMultiplicadores(qsos, regla);

        // Assert
        multiplicadores.Should().Be(2); // JA en 20m y JA en 15m
    }

    // --- ObtenerTasaQsos ---

    [Fact]
    public void ObtenerTasaQsos_ConQsosEnVentana_RetornaTasaCorrecta()
    {
        // Arrange
        DateTimeOffset ahora = DateTimeOffset.UtcNow.AddHours(-1);
        List<Qso> qsos = new()
        {
            CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB, ahora.AddMinutes(-50)),
            CrearQso("EA4ABC", "W1AW", 14.250, ModoOperacion.SSB, ahora.AddMinutes(-30)),
            CrearQso("EA4ABC", "VK2ABC", 14.300, ModoOperacion.SSB, ahora)
        };
        TimeSpan ventana = TimeSpan.FromHours(1);

        // Act
        double tasa = _motor.ObtenerTasaQsos(qsos, ventana);

        // Assert
        tasa.Should().Be(3.0); // 3 QSOs en 1 hora
    }

    [Fact]
    public void ObtenerTasaQsos_SinQsos_RetornaCero()
    {
        // Arrange
        List<Qso> qsos = new();
        TimeSpan ventana = TimeSpan.FromHours(1);

        // Act
        double tasa = _motor.ObtenerTasaQsos(qsos, ventana);

        // Assert
        tasa.Should().Be(0.0);
    }

    [Fact]
    public void ObtenerTasaQsos_VentanaCero_RetornaCero()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("EA4ABC", "JA1XYZ", 14.200, ModoOperacion.SSB)
        };
        TimeSpan ventana = TimeSpan.Zero;

        // Act
        double tasa = _motor.ObtenerTasaQsos(qsos, ventana);

        // Assert
        tasa.Should().Be(0.0);
    }

    // --- RegistroDeReglas ---

    [Fact]
    public void RegistroDeReglas_ContieneAlMenos5Contests()
    {
        // Act
        IReadOnlyDictionary<TipoContest, ReglaContest> registro = MotorContest.RegistroDeReglas;

        // Assert
        registro.Count.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void RegistroDeReglas_CqWw_TieneReglasCorrectas()
    {
        // Act
        ReglaContest regla = MotorContest.RegistroDeReglas[TipoContest.CqWw];

        // Assert
        regla.Nombre.Should().Contain("CQ World Wide");
        regla.Abreviatura.Should().Contain("CQ-WW");
        regla.DuracionHoras.Should().Be(48);
        regla.TipoIntercambio.Should().Be(TipoIntercambio.RstZona);
        regla.MetodoMultiplicador.Should().Be(MetodoMultiplicador.PorZonaCq);
        regla.BandasPermitidas.Should().NotBeEmpty();
        regla.ModosPermitidos.Should().NotBeEmpty();
    }

    // --- Null checks ---

    [Fact]
    public void CalcularPuntuacion_QsosNulo_LanzaArgumentNullException()
    {
        // Arrange
        ReglaContest regla = CrearReglaCqWw();

        // Act
        Action accion = () => _motor.CalcularPuntuacion(null!, regla);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CalcularPuntuacion_ReglaNula_LanzaArgumentNullException()
    {
        // Arrange
        List<Qso> qsos = new();

        // Act
        Action accion = () => _motor.CalcularPuntuacion(qsos, null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }
}
