using FluentAssertions;
using RadioAficionado.Compartido.Excepciones;

namespace RadioAficionado.Infraestructura.Tests.Compartido;

/// <summary>
/// Tests unitarios para <see cref="ExcepcionDeValidacion"/>.
/// Verifica constructores, herencia y comportamiento estándar de la excepción.
/// </summary>
public class ExcepcionDeValidacionTests
{
    [Fact]
    public void Constructor_ConMensaje_EstableceMensajeCorrectamente()
    {
        // Arrange
        string mensaje = "El campo es obligatorio";

        // Act
        ExcepcionDeValidacion excepcion = new ExcepcionDeValidacion(mensaje);

        // Assert
        excepcion.Message.Should().Be(mensaje);
    }

    [Fact]
    public void Constructor_ConMensajeYExcepcionInterna_EstableceAmbos()
    {
        // Arrange
        string mensaje = "Error de validación";
        InvalidOperationException excepcionInterna = new InvalidOperationException("Detalle interno");

        // Act
        ExcepcionDeValidacion excepcion = new ExcepcionDeValidacion(mensaje, excepcionInterna);

        // Assert
        excepcion.Message.Should().Be(mensaje);
        excepcion.InnerException.Should().BeSameAs(excepcionInterna);
    }

    [Fact]
    public void ExcepcionDeValidacion_HeredaDeException()
    {
        // Arrange & Act
        ExcepcionDeValidacion excepcion = new ExcepcionDeValidacion("test");

        // Assert
        excepcion.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_ConMensajeVacio_NoLanzaExcepcion()
    {
        // Arrange & Act
        ExcepcionDeValidacion excepcion = new ExcepcionDeValidacion(string.Empty);

        // Assert
        excepcion.Message.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ConExcepcionInternaNula_InnerExceptionEsNull()
    {
        // Arrange & Act
        ExcepcionDeValidacion excepcion = new ExcepcionDeValidacion("mensaje", null!);

        // Assert
        excepcion.InnerException.Should().BeNull();
    }
}
