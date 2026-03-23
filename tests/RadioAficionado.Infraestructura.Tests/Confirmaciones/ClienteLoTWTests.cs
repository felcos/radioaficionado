using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura.Confirmaciones;

namespace RadioAficionado.Infraestructura.Tests.Confirmaciones;

/// <summary>
/// Tests unitarios para el cliente de LoTW.
/// Verifica el parseo de respuestas sin hacer llamadas HTTP reales.
/// </summary>
public class ClienteLoTWTests
{
    [Fact]
    public void ParsearRespuestaSubida_ConMensajeExito_DebeRetornarExitoso()
    {
        // Arrange
        string respuesta = "Your file has been queued for processing.";

        // Act
        ResultadoSubida resultado = ClienteLoTW.ParsearRespuestaSubida(respuesta);

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.Servicio.Should().Be(ServicioExterno.LoTW);
        resultado.Mensaje.Should().Contain("encolado");
    }

    [Fact]
    public void ParsearRespuestaSubida_ConConteos_DebeExtraerNumerosCorrectamente()
    {
        // Arrange
        string respuesta = "Upload complete: 15 accepted, 3 rejected.";

        // Act
        ResultadoSubida resultado = ClienteLoTW.ParsearRespuestaSubida(respuesta);

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.QsosSubidos.Should().Be(15);
        resultado.QsosRechazados.Should().Be(3);
        resultado.Servicio.Should().Be(ServicioExterno.LoTW);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConError_DebeRetornarNoExitoso()
    {
        // Arrange
        string respuesta = "Error: incorrect password for user EA4ABC.";

        // Act
        ResultadoSubida resultado = ClienteLoTW.ParsearRespuestaSubida(respuesta);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.QsosSubidos.Should().Be(0);
        resultado.QsosRechazados.Should().Be(0);
        resultado.Servicio.Should().Be(ServicioExterno.LoTW);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConRespuestaVacia_DebeRetornarNoExitoso()
    {
        // Arrange
        string respuesta = "";

        // Act
        ResultadoSubida resultado = ClienteLoTW.ParsearRespuestaSubida(respuesta);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Contain("vacía");
        resultado.Servicio.Should().Be(ServicioExterno.LoTW);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConMensajeFailed_DebeRetornarNoExitoso()
    {
        // Arrange
        string respuesta = "Upload failed: certificate expired.";

        // Act
        ResultadoSubida resultado = ClienteLoTW.ParsearRespuestaSubida(respuesta);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Servicio.Should().Be(ServicioExterno.LoTW);
    }
}
