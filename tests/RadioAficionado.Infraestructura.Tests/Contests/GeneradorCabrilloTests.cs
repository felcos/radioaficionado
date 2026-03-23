using FluentAssertions;
using RadioAficionado.Dominio.Contests;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.Contests;

namespace RadioAficionado.Infraestructura.Tests.Contests;

/// <summary>
/// Tests del generador de archivos Cabrillo 3.0.
/// </summary>
public class GeneradorCabrilloTests
{
    private readonly GeneradorCabrillo _generador = new();

    private static ConfiguracionContest CrearConfiguracion()
    {
        return new ConfiguracionContest(
            Indicativo: new Indicativo("EA4ABC"),
            CategoriaOperador: "SINGLE-OP",
            CategoriaBanda: "ALL",
            CategoriaModo: "SSB",
            CategoriaPotencia: "HIGH",
            NombreOperador: "Juan Garcia",
            Club: "URE",
            Ubicacion: "DX");
    }

    private static ReglaContest CrearReglaCqWw()
    {
        return MotorContest.RegistroDeReglas[TipoContest.CqWw];
    }

    private static Qso CrearQso(
        string indicativoContacto,
        double frecuenciaMHz,
        ModoOperacion modo,
        DateTimeOffset? fecha = null)
    {
        DateTimeOffset fechaQso = fecha ?? new DateTimeOffset(2024, 10, 26, 12, 34, 0, TimeSpan.Zero);
        return Qso.Crear(
            new Indicativo("EA4ABC"),
            new Indicativo(indicativoContacto),
            fechaQso,
            Frecuencia.DesdeMHz(frecuenciaMHz),
            modo,
            "59");
    }

    [Fact]
    public void GenerarCabrillo_ContieneStartOfLog()
    {
        // Arrange
        List<Qso> qsos = new();
        ReglaContest regla = CrearReglaCqWw();
        ConfiguracionContest configuracion = CrearConfiguracion();

        // Act
        string cabrillo = _generador.GenerarCabrillo(qsos, regla, configuracion);

        // Assert
        cabrillo.Should().StartWith("START-OF-LOG: 3.0");
    }

    [Fact]
    public void GenerarCabrillo_ContieneEndOfLog()
    {
        // Arrange
        List<Qso> qsos = new();
        ReglaContest regla = CrearReglaCqWw();
        ConfiguracionContest configuracion = CrearConfiguracion();

        // Act
        string cabrillo = _generador.GenerarCabrillo(qsos, regla, configuracion);

        // Assert
        cabrillo.Should().Contain("END-OF-LOG:");
    }

    [Fact]
    public void GenerarCabrillo_ContieneContest()
    {
        // Arrange
        List<Qso> qsos = new();
        ReglaContest regla = CrearReglaCqWw();
        ConfiguracionContest configuracion = CrearConfiguracion();

        // Act
        string cabrillo = _generador.GenerarCabrillo(qsos, regla, configuracion);

        // Assert
        cabrillo.Should().Contain("CONTEST: CQ-WW-SSB");
    }

    [Fact]
    public void GenerarCabrillo_ContieneIndicativo()
    {
        // Arrange
        List<Qso> qsos = new();
        ReglaContest regla = CrearReglaCqWw();
        ConfiguracionContest configuracion = CrearConfiguracion();

        // Act
        string cabrillo = _generador.GenerarCabrillo(qsos, regla, configuracion);

        // Assert
        cabrillo.Should().Contain("CALLSIGN: EA4ABC");
    }

    [Fact]
    public void GenerarCabrillo_ContieneCategorias()
    {
        // Arrange
        List<Qso> qsos = new();
        ReglaContest regla = CrearReglaCqWw();
        ConfiguracionContest configuracion = CrearConfiguracion();

        // Act
        string cabrillo = _generador.GenerarCabrillo(qsos, regla, configuracion);

        // Assert
        cabrillo.Should().Contain("CATEGORY-OPERATOR: SINGLE-OP");
        cabrillo.Should().Contain("CATEGORY-BAND: ALL");
        cabrillo.Should().Contain("CATEGORY-MODE: SSB");
        cabrillo.Should().Contain("CATEGORY-POWER: HIGH");
    }

    [Fact]
    public void GenerarCabrillo_ConQsos_ContieneLineasQso()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("JA1XYZ", 14.200, ModoOperacion.SSB)
        };
        ReglaContest regla = CrearReglaCqWw();
        ConfiguracionContest configuracion = CrearConfiguracion();

        // Act
        string cabrillo = _generador.GenerarCabrillo(qsos, regla, configuracion);

        // Assert
        cabrillo.Should().Contain("QSO:");
        cabrillo.Should().Contain("14200");
        cabrillo.Should().Contain("PH"); // SSB = PH en Cabrillo
        cabrillo.Should().Contain("2024-10-26");
        cabrillo.Should().Contain("1234");
        cabrillo.Should().Contain("EA4ABC");
        cabrillo.Should().Contain("JA1XYZ");
    }

    [Fact]
    public void GenerarCabrillo_CwUsaModoCW()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("JA1XYZ", 14.050, ModoOperacion.CW)
        };
        ReglaContest regla = CrearReglaCqWw();
        ConfiguracionContest configuracion = CrearConfiguracion();

        // Act
        string cabrillo = _generador.GenerarCabrillo(qsos, regla, configuracion);

        // Assert
        cabrillo.Should().Contain("QSO:");
        cabrillo.Should().Contain(" CW ");
    }

    [Fact]
    public void GenerarCabrillo_ContieneNombreYClub()
    {
        // Arrange
        List<Qso> qsos = new();
        ReglaContest regla = CrearReglaCqWw();
        ConfiguracionContest configuracion = CrearConfiguracion();

        // Act
        string cabrillo = _generador.GenerarCabrillo(qsos, regla, configuracion);

        // Assert
        cabrillo.Should().Contain("NAME: Juan Garcia");
        cabrillo.Should().Contain("CLUB: URE");
    }

    [Fact]
    public void GenerarCabrillo_ParametroNulo_LanzaExcepcion()
    {
        // Arrange
        List<Qso> qsos = new();
        ReglaContest regla = CrearReglaCqWw();
        ConfiguracionContest configuracion = CrearConfiguracion();

        // Act & Assert
        Action accionQsosNulo = () => _generador.GenerarCabrillo(null!, regla, configuracion);
        Action accionReglaNula = () => _generador.GenerarCabrillo(qsos, null!, configuracion);
        Action accionConfigNula = () => _generador.GenerarCabrillo(qsos, regla, null!);

        accionQsosNulo.Should().Throw<ArgumentNullException>();
        accionReglaNula.Should().Throw<ArgumentNullException>();
        accionConfigNula.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerarCabrillo_MultiplesQsos_GeneraLineasEnOrden()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("JA1XYZ", 14.200, ModoOperacion.SSB, new DateTimeOffset(2024, 10, 26, 12, 0, 0, TimeSpan.Zero)),
            CrearQso("W1AW", 14.250, ModoOperacion.SSB, new DateTimeOffset(2024, 10, 26, 12, 15, 0, TimeSpan.Zero)),
            CrearQso("VK2ABC", 21.300, ModoOperacion.SSB, new DateTimeOffset(2024, 10, 26, 12, 30, 0, TimeSpan.Zero))
        };
        ReglaContest regla = CrearReglaCqWw();
        ConfiguracionContest configuracion = CrearConfiguracion();

        // Act
        string cabrillo = _generador.GenerarCabrillo(qsos, regla, configuracion);

        // Assert
        int posJa = cabrillo.IndexOf("JA1XYZ");
        int posW = cabrillo.IndexOf("W1AW");
        int posVk = cabrillo.IndexOf("VK2ABC");
        posJa.Should().BeLessThan(posW);
        posW.Should().BeLessThan(posVk);
    }
}
