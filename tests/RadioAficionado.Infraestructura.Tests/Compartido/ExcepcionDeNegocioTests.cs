using FluentAssertions;
using RadioAficionado.Compartido.Excepciones;

namespace RadioAficionado.Infraestructura.Tests.Compartido;

/// <summary>
/// Tests unitarios para <see cref="ExcepcionDeNegocio"/>.
/// Verifica constructores, herencia y comportamiento estándar de la excepción.
/// </summary>
public class ExcepcionDeNegocioTests
{
    [Fact]
    public void Constructor_ConMensaje_EstableceMensajeCorrectamente()
    {
        // Arrange
        string mensaje = "Regla de negocio violada";

        // Act
        ExcepcionDeNegocio excepcion = new ExcepcionDeNegocio(mensaje);

        // Assert
        excepcion.Message.Should().Be(mensaje);
    }

    [Fact]
    public void Constructor_ConMensajeYExcepcionInterna_EstableceAmbos()
    {
        // Arrange
        string mensaje = "Error de negocio";
        ArgumentException excepcionInterna = new ArgumentException("Argumento inválido");

        // Act
        ExcepcionDeNegocio excepcion = new ExcepcionDeNegocio(mensaje, excepcionInterna);

        // Assert
        excepcion.Message.Should().Be(mensaje);
        excepcion.InnerException.Should().BeSameAs(excepcionInterna);
    }

    [Fact]
    public void ExcepcionDeNegocio_HeredaDeException()
    {
        // Arrange & Act
        ExcepcionDeNegocio excepcion = new ExcepcionDeNegocio("test");

        // Assert
        excepcion.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_ConMensajeVacio_NoLanzaExcepcion()
    {
        // Arrange & Act
        ExcepcionDeNegocio excepcion = new ExcepcionDeNegocio(string.Empty);

        // Assert
        excepcion.Message.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ConExcepcionInternaNula_InnerExceptionEsNull()
    {
        // Arrange & Act
        ExcepcionDeNegocio excepcion = new ExcepcionDeNegocio("mensaje", null!);

        // Assert
        excepcion.InnerException.Should().BeNull();
    }
}
