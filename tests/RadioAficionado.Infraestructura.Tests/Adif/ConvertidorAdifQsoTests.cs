using FluentAssertions;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.Adif;

namespace RadioAficionado.Infraestructura.Tests.Adif;

/// <summary>
/// Tests unitarios para <see cref="ConvertidorAdifQso"/>.
/// Valida la conversión bidireccional entre registros ADIF y entidades Qso del dominio.
/// </summary>
public class ConvertidorAdifQsoTests
{
    [Fact]
    public void ConvertirAQso_RegistroCompleto_CreaQsoCorrectamente()
    {
        // Arrange
        RegistroAdif registro = new RegistroAdif();
        registro.Indicativo = "EA4AB";
        registro.IndicativoPropio = "W1AW";
        registro.FechaQso = "20250322";
        registro.HoraInicio = "143015";
        registro.Frecuencia = "14.074";
        registro.Modo = "FT8";
        registro.SenalEnviada = "-10";
        registro.SenalRecibida = "-15";
        registro.Potencia = "50";
        registro.Localizador = "IN80";

        // Act
        Qso? qso = ConvertidorAdifQso.ConvertirAQso(registro);

        // Assert
        qso.Should().NotBeNull();
        qso!.IndicativoContacto.Valor.Should().Be("EA4AB");
        qso.IndicativoPropio.Valor.Should().Be("W1AW");
        qso.Modo.Should().Be(ModoOperacion.FT8);
        qso.Potencia.Should().Be(50.0);
        qso.LocalizadorContacto.Should().NotBeNull();
        qso.LocalizadorContacto!.Value.Valor.Should().Be("IN80");
    }

    [Fact]
    public void ConvertirAAdif_QsoCompleto_CreaRegistroConCamposCorrectos()
    {
        // Arrange
        Qso qso = Qso.Crear(
            new Indicativo("W1AW"),
            new Indicativo("EA4AB"),
            new DateTimeOffset(2025, 3, 22, 14, 30, 0, TimeSpan.Zero),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "-10",
            potencia: 50.0,
            localizadorContacto: new Localizador("IN80"));

        // Act
        RegistroAdif registro = ConvertidorAdifQso.ConvertirAAdif(qso);

        // Assert
        registro.Indicativo.Should().Be("EA4AB");
        registro.IndicativoPropio.Should().Be("W1AW");
        registro.Modo.Should().Be("FT8");
        registro.Frecuencia.Should().Be("14.074000");
        registro.Potencia.Should().Be("50");
        registro.Localizador.Should().Be("IN80");
    }

    [Fact]
    public void ConvertirAQso_SinIndicativo_DevuelveNull()
    {
        // Arrange
        RegistroAdif registro = new RegistroAdif();
        registro.Modo = "FT8";

        // Act
        Qso? qso = ConvertidorAdifQso.ConvertirAQso(registro);

        // Assert
        qso.Should().BeNull();
    }

    [Fact]
    public void ConvertirAQso_SinModo_UsaModoPorDefectoSsb()
    {
        // Arrange
        RegistroAdif registro = new RegistroAdif();
        registro.Indicativo = "EA4AB";
        registro.FechaQso = "20250322";
        registro.HoraInicio = "1430";
        registro.SenalEnviada = "59";

        // Act
        Qso? qso = ConvertidorAdifQso.ConvertirAQso(registro);

        // Assert
        qso.Should().NotBeNull();
        qso!.Modo.Should().Be(ModoOperacion.SSB);
    }

    [Fact]
    public void ConvertirAQso_SinIndicativoPropio_UsaN0Call()
    {
        // Arrange
        RegistroAdif registro = new RegistroAdif();
        registro.Indicativo = "EA4AB";
        registro.FechaQso = "20250322";
        registro.HoraInicio = "1430";
        registro.SenalEnviada = "59";

        // Act
        Qso? qso = ConvertidorAdifQso.ConvertirAQso(registro);

        // Assert
        qso.Should().NotBeNull();
        qso!.IndicativoPropio.Valor.Should().Be("N0CALL");
    }

    [Fact]
    public void ConvertirAQso_SinSenalEnviada_UsaValorPorDefecto59()
    {
        // Arrange
        RegistroAdif registro = new RegistroAdif();
        registro.Indicativo = "EA4AB";
        registro.FechaQso = "20250322";
        registro.HoraInicio = "1430";

        // Act
        Qso? qso = ConvertidorAdifQso.ConvertirAQso(registro);

        // Assert
        qso.Should().NotBeNull();
        qso!.SenalEnviada.Should().Be("59");
    }

    [Fact]
    public void ConvertirAAdif_QsoConFechaHora_GeneraFormatoAdifCorrecto()
    {
        // Arrange
        Qso qso = Qso.Crear(
            new Indicativo("W1AW"),
            new Indicativo("EA4AB"),
            new DateTimeOffset(2025, 3, 22, 14, 30, 15, TimeSpan.Zero),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "-10");

        // Act
        RegistroAdif registro = ConvertidorAdifQso.ConvertirAAdif(qso);

        // Assert
        registro.FechaQso.Should().Be("20250322");
        registro.HoraInicio.Should().Be("143015");
    }

    [Fact]
    public void ConvertirAAdif_QsoConBanda20m_GeneraBandaCorrecta()
    {
        // Arrange
        Qso qso = Qso.Crear(
            new Indicativo("W1AW"),
            new Indicativo("EA4AB"),
            new DateTimeOffset(2025, 3, 22, 14, 30, 0, TimeSpan.Zero),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "-10");

        // Act
        RegistroAdif registro = ConvertidorAdifQso.ConvertirAAdif(qso);

        // Assert
        registro.Banda.Should().Be("20m");
    }

    [Fact]
    public void RoundTrip_QsoAAdifYVuelta_MantieneDataEsencial()
    {
        // Arrange
        Qso original = Qso.Crear(
            new Indicativo("W1AW"),
            new Indicativo("EA4AB"),
            new DateTimeOffset(2025, 3, 22, 14, 30, 15, TimeSpan.Zero),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "-10",
            potencia: 50.0);

        // Act
        RegistroAdif adif = ConvertidorAdifQso.ConvertirAAdif(original);
        Qso? reconvertido = ConvertidorAdifQso.ConvertirAQso(adif);

        // Assert
        reconvertido.Should().NotBeNull();
        reconvertido!.IndicativoContacto.Valor.Should().Be("EA4AB");
        reconvertido.IndicativoPropio.Valor.Should().Be("W1AW");
        reconvertido.Modo.Should().Be(ModoOperacion.FT8);
        reconvertido.Potencia.Should().Be(50.0);
    }

    [Fact]
    public void ConvertirAQso_RegistroNulo_LanzaArgumentNullException()
    {
        // Arrange & Act
        Action accion = () => ConvertidorAdifQso.ConvertirAQso(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConvertirAAdif_QsoNulo_LanzaArgumentNullException()
    {
        // Arrange & Act
        Action accion = () => ConvertidorAdifQso.ConvertirAAdif(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConvertirAQso_ConSenalRecibida_CompletaQso()
    {
        // Arrange
        RegistroAdif registro = new RegistroAdif();
        registro.Indicativo = "EA4AB";
        registro.IndicativoPropio = "W1AW";
        registro.FechaQso = "20250322";
        registro.HoraInicio = "143015";
        registro.HoraFin = "143115";
        registro.Frecuencia = "14.074";
        registro.Modo = "FT8";
        registro.SenalEnviada = "-10";
        registro.SenalRecibida = "-15";

        // Act
        Qso? qso = ConvertidorAdifQso.ConvertirAQso(registro);

        // Assert
        qso.Should().NotBeNull();
        qso!.SenalRecibida.Should().Be("-15");
        qso.FechaHoraFin.Should().NotBeNull();
    }

    [Fact]
    public void ConvertirAAdif_QsoSinPotencia_NoIncluyeCampoPotencia()
    {
        // Arrange
        Qso qso = Qso.Crear(
            new Indicativo("W1AW"),
            new Indicativo("EA4AB"),
            new DateTimeOffset(2025, 3, 22, 14, 30, 0, TimeSpan.Zero),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "-10");

        // Act
        RegistroAdif registro = ConvertidorAdifQso.ConvertirAAdif(qso);

        // Assert
        registro.Potencia.Should().BeNull();
    }
}
