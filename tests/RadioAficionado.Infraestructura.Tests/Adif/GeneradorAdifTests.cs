using FluentAssertions;
using RadioAficionado.Infraestructura.Adif;

namespace RadioAficionado.Infraestructura.Tests.Adif;

/// <summary>
/// Tests unitarios para <see cref="GeneradorAdif"/>.
/// Valida la generación de cadenas ADIF con encabezado, registros y round-trip con el parser.
/// </summary>
public class GeneradorAdifTests
{
    [Fact]
    public void Generar_RegistroSimple_ContieneTagsCorrectos()
    {
        // Arrange
        RegistroAdif registro = new RegistroAdif();
        registro.Indicativo = "EA4AB";
        registro.Modo = "FT8";
        registro.Frecuencia = "14.074";

        // Act
        string adif = GeneradorAdif.Generar(new[] { registro });

        // Assert
        adif.Should().Contain("<CALL:5>EA4AB");
        adif.Should().Contain("<MODE:3>FT8");
        adif.Should().Contain("<FREQ:6>14.074");
        adif.Should().Contain("<EOR>");
        adif.Should().Contain("<EOH>");
    }

    [Fact]
    public void Generar_MultiplesRegistros_ContieneEorPorCadaUno()
    {
        // Arrange
        RegistroAdif reg1 = new RegistroAdif { Indicativo = "EA4AB" };
        RegistroAdif reg2 = new RegistroAdif { Indicativo = "W1AW" };

        // Act
        string adif = GeneradorAdif.Generar(new[] { reg1, reg2 });

        // Assert
        int conteoEor = adif.Split("<EOR>").Length - 1;
        conteoEor.Should().Be(2);
    }

    [Fact]
    public void Generar_YParsear_RoundTrip_MantieneDatos()
    {
        // Arrange
        RegistroAdif original = new RegistroAdif();
        original.Indicativo = "EA4AB";
        original.Modo = "FT8";
        original.Frecuencia = "14.074";
        original.SenalEnviada = "-10";
        original.SenalRecibida = "-15";
        original.FechaQso = "20250322";
        original.HoraInicio = "1430";

        // Act
        string adif = GeneradorAdif.Generar(new[] { original });
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().HaveCount(1);
        RegistroAdif parseado = resultado.Registros[0];
        parseado.Indicativo.Should().Be("EA4AB");
        parseado.Modo.Should().Be("FT8");
        parseado.Frecuencia.Should().Be("14.074");
        parseado.SenalEnviada.Should().Be("-10");
        parseado.SenalRecibida.Should().Be("-15");
    }

    [Fact]
    public void Generar_SinRegistros_SoloEncabezado()
    {
        // Arrange & Act
        string adif = GeneradorAdif.Generar(Array.Empty<RegistroAdif>());

        // Assert
        adif.Should().Contain("<EOH>");
        adif.Should().NotContain("<EOR>");
    }

    [Fact]
    public void Generar_ContieneVersionAdif_EnEncabezado()
    {
        // Arrange & Act
        string adif = GeneradorAdif.Generar(Array.Empty<RegistroAdif>());

        // Assert
        adif.Should().Contain("<ADIF_VER:5>3.1.4");
    }

    [Fact]
    public void Generar_ContieneNombrePrograma_EnEncabezado()
    {
        // Arrange & Act
        string adif = GeneradorAdif.Generar(Array.Empty<RegistroAdif>(), programa: "MiPrograma");

        // Assert
        adif.Should().Contain("<PROGRAMID:10>MiPrograma");
    }

    [Fact]
    public void Generar_RegistroConTodosLosCampos_GeneraTodosLosTags()
    {
        // Arrange
        RegistroAdif registro = new RegistroAdif();
        registro.Indicativo = "W1AW";
        registro.IndicativoPropio = "EA4AB";
        registro.FechaQso = "20250322";
        registro.HoraInicio = "143015";
        registro.HoraFin = "143115";
        registro.Banda = "20m";
        registro.Frecuencia = "14.074000";
        registro.Modo = "FT8";
        registro.SenalEnviada = "-10";
        registro.SenalRecibida = "-15";
        registro.Potencia = "50";
        registro.Localizador = "FN31PR";

        // Act
        string adif = GeneradorAdif.Generar(new[] { registro });

        // Assert
        adif.Should().Contain("<CALL:4>W1AW");
        adif.Should().Contain("<STATION_CALLSIGN:5>EA4AB");
        adif.Should().Contain("<QSO_DATE:8>20250322");
        adif.Should().Contain("<BAND:3>20m");
        adif.Should().Contain("<GRIDSQUARE:6>FN31PR");
    }

    [Fact]
    public void Generar_RegistrosNulos_LanzaArgumentNullException()
    {
        // Arrange & Act
        Action accion = () => GeneradorAdif.Generar(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Generar_ContieneMarcaDeTiempo_EnEncabezado()
    {
        // Arrange & Act
        string adif = GeneradorAdif.Generar(Array.Empty<RegistroAdif>());

        // Assert
        adif.Should().Contain("CREATED_TIMESTAMP");
    }

    [Fact]
    public void Generar_RoundTripMultiplesRegistros_MantieneTodos()
    {
        // Arrange
        RegistroAdif reg1 = new RegistroAdif { Indicativo = "EA4AB", Modo = "FT8" };
        RegistroAdif reg2 = new RegistroAdif { Indicativo = "W1AW", Modo = "CW" };
        RegistroAdif reg3 = new RegistroAdif { Indicativo = "VK2ABC", Modo = "SSB" };

        // Act
        string adif = GeneradorAdif.Generar(new[] { reg1, reg2, reg3 });
        ResultadoParserAdif resultado = ParserAdif.Parsear(adif);

        // Assert
        resultado.Registros.Should().HaveCount(3);
        resultado.Registros[0].Indicativo.Should().Be("EA4AB");
        resultado.Registros[1].Indicativo.Should().Be("W1AW");
        resultado.Registros[2].Indicativo.Should().Be("VK2ABC");
    }
}
