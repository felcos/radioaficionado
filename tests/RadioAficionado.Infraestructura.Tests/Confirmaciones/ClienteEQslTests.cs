using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura.Confirmaciones;

namespace RadioAficionado.Infraestructura.Tests.Confirmaciones;

/// <summary>
/// Tests unitarios para el cliente de eQSL.
/// Verifica el parseo de respuestas sin hacer llamadas HTTP reales.
/// </summary>
public class ClienteEQslTests
{
    [Fact]
    public void ParsearRespuestaSubida_ConRegistrosAgregados_DebeExtraerConteo()
    {
        // Arrange
        string respuesta = "Result: 10 records added successfully.";

        // Act
        ResultadoSubida resultado = ClienteEQsl.ParsearRespuestaSubida(respuesta);

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.QsosSubidos.Should().Be(10);
        resultado.Servicio.Should().Be(ServicioExterno.EQsl);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConDuplicados_DebeExtraerAmbosCont()
    {
        // Arrange
        string respuesta = "Result: 8 records added, 2 duplicates found.";

        // Act
        ResultadoSubida resultado = ClienteEQsl.ParsearRespuestaSubida(respuesta);

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.QsosSubidos.Should().Be(8);
        resultado.QsosRechazados.Should().Be(2);
        resultado.Servicio.Should().Be(ServicioExterno.EQsl);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConError_DebeRetornarNoExitoso()
    {
        // Arrange
        string respuesta = "Error: Invalid username or password.";

        // Act
        ResultadoSubida resultado = ClienteEQsl.ParsearRespuestaSubida(respuesta);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.QsosSubidos.Should().Be(0);
        resultado.Mensaje.Should().Contain("Invalid username or password");
        resultado.Servicio.Should().Be(ServicioExterno.EQsl);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConRespuestaVacia_DebeRetornarNoExitoso()
    {
        // Arrange
        string respuesta = "";

        // Act
        ResultadoSubida resultado = ClienteEQsl.ParsearRespuestaSubida(respuesta);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Contain("vacía");
        resultado.Servicio.Should().Be(ServicioExterno.EQsl);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConUnSoloRegistro_DebeExtraerCorrectamente()
    {
        // Arrange
        string respuesta = "Result: 1 record added.";

        // Act
        ResultadoSubida resultado = ClienteEQsl.ParsearRespuestaSubida(respuesta);

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.QsosSubidos.Should().Be(1);
        resultado.Servicio.Should().Be(ServicioExterno.EQsl);
    }
}
