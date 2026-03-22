using FluentAssertions;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.Entidades;

/// <summary>
/// Tests unitarios para la entidad <see cref="Qso"/>.
/// Valida la creación con datos válidos e inválidos, y el método Completar
/// con sus distintos escenarios de éxito y error.
/// </summary>
public class QsoTests
{
    [Fact]
    public void Crear_DatosValidos_CreaQsoCorrectamente()
    {
        // Arrange
        Indicativo indicativoPropio = new Indicativo("EA4ABC");
        Indicativo indicativoContacto = new Indicativo("W1AW");
        DateTimeOffset fechaInicio = DateTimeOffset.UtcNow.AddMinutes(-10);
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.074);
        ModoOperacion modo = ModoOperacion.FT8;
        string senalEnviada = "59";

        // Act
        Qso qso = Qso.Crear(indicativoPropio, indicativoContacto, fechaInicio, frecuencia, modo, senalEnviada);

        // Assert
        qso.Id.Should().NotBeEmpty();
        qso.IndicativoPropio.Valor.Should().Be("EA4ABC");
        qso.IndicativoContacto.Valor.Should().Be("W1AW");
        qso.Frecuencia.Hz.Should().Be(frecuencia.Hz);
        qso.Modo.Should().Be(ModoOperacion.FT8);
        qso.SenalEnviada.Should().Be("59");
        qso.FechaHoraFin.Should().BeNull();
    }

    [Fact]
    public void Crear_SenalEnviadaVacia_LanzaArgumentException()
    {
        // Arrange
        Indicativo indicativoPropio = new Indicativo("EA4ABC");
        Indicativo indicativoContacto = new Indicativo("W1AW");
        DateTimeOffset fechaInicio = DateTimeOffset.UtcNow.AddMinutes(-10);
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.074);

        // Act
        Action accion = () => Qso.Crear(indicativoPropio, indicativoContacto, fechaInicio, frecuencia, ModoOperacion.FT8, "");

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Crear_PotenciaNegativa_LanzaArgumentException()
    {
        // Arrange
        Indicativo indicativoPropio = new Indicativo("EA4ABC");
        Indicativo indicativoContacto = new Indicativo("W1AW");
        DateTimeOffset fechaInicio = DateTimeOffset.UtcNow.AddMinutes(-10);
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.074);

        // Act
        Action accion = () => Qso.Crear(indicativoPropio, indicativoContacto, fechaInicio, frecuencia, ModoOperacion.FT8, "59", potencia: -10.0);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Completar_QsoNuevo_EstableceFinYSenalRecibida()
    {
        // Arrange
        Qso qso = Qso.Crear(
            new Indicativo("EA4ABC"),
            new Indicativo("W1AW"),
            DateTimeOffset.UtcNow.AddMinutes(-10),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "59");
        DateTimeOffset fechaFin = DateTimeOffset.UtcNow;

        // Act
        qso.Completar(fechaFin, "59");

        // Assert
        qso.FechaHoraFin.Should().Be(fechaFin);
        qso.SenalRecibida.Should().Be("59");
    }

    [Fact]
    public void Completar_QsoYaCompletado_LanzaInvalidOperationException()
    {
        // Arrange
        Qso qso = Qso.Crear(
            new Indicativo("EA4ABC"),
            new Indicativo("W1AW"),
            DateTimeOffset.UtcNow.AddMinutes(-10),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "59");
        qso.Completar(DateTimeOffset.UtcNow, "59");

        // Act
        Action accion = () => qso.Completar(DateTimeOffset.UtcNow, "59");

        // Assert
        accion.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Completar_FechaFinAnteriorAInicio_LanzaArgumentException()
    {
        // Arrange
        DateTimeOffset fechaInicio = DateTimeOffset.UtcNow.AddMinutes(-10);
        Qso qso = Qso.Crear(
            new Indicativo("EA4ABC"),
            new Indicativo("W1AW"),
            fechaInicio,
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "59");
        DateTimeOffset fechaFinAnterior = fechaInicio.AddMinutes(-5);

        // Act
        Action accion = () => qso.Completar(fechaFinAnterior, "59");

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Completar_SenalRecibidaVacia_LanzaArgumentException()
    {
        // Arrange
        Qso qso = Qso.Crear(
            new Indicativo("EA4ABC"),
            new Indicativo("W1AW"),
            DateTimeOffset.UtcNow.AddMinutes(-10),
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            "59");

        // Act
        Action accion = () => qso.Completar(DateTimeOffset.UtcNow, "");

        // Assert
        accion.Should().Throw<ArgumentException>();
    }
}
