using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura.Confirmaciones;

namespace RadioAficionado.Infraestructura.Tests.Confirmaciones;

/// <summary>
/// Tests unitarios para el cliente de Club Log.
/// Verifica el parseo de respuestas sin hacer llamadas HTTP reales.
/// </summary>
public class ClienteClubLogTests
{
    [Fact]
    public void ParsearRespuestaSubida_ConExitoYConteos_DebeExtraerNumeros()
    {
        // Arrange
        string respuesta = "Upload complete: 25 inserted, 0 rejected.";
        int codigoHttp = 200;

        // Act
        ResultadoSubida resultado = ClienteClubLog.ParsearRespuestaSubida(respuesta, codigoHttp);

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.QsosSubidos.Should().Be(25);
        resultado.QsosRechazados.Should().Be(0);
        resultado.Servicio.Should().Be(ServicioExterno.ClubLog);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConCredencialesInvalidas_DebeRetornarNoExitoso()
    {
        // Arrange
        string respuesta = "Unauthorized: invalid password.";
        int codigoHttp = 401;

        // Act
        ResultadoSubida resultado = ClienteClubLog.ParsearRespuestaSubida(respuesta, codigoHttp);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Contain("Credenciales inválidas");
        resultado.Servicio.Should().Be(ServicioExterno.ClubLog);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConApiKeyInvalida_DebeRetornarMensajeClaro()
    {
        // Arrange
        string respuesta = "Invalid API key provided.";
        int codigoHttp = 403;

        // Act
        ResultadoSubida resultado = ClienteClubLog.ParsearRespuestaSubida(respuesta, codigoHttp);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Contain("Clave de API inválida");
        resultado.Servicio.Should().Be(ServicioExterno.ClubLog);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConIndicativoNoEncontrado_DebeRetornarMensajeClaro()
    {
        // Arrange
        string respuesta = "Callsign not found in database.";
        int codigoHttp = 449;

        // Act
        ResultadoSubida resultado = ClienteClubLog.ParsearRespuestaSubida(respuesta, codigoHttp);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Contain("Indicativo no encontrado");
        resultado.Servicio.Should().Be(ServicioExterno.ClubLog);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConRespuestaVaciaYCodigo200_DebeRetornarExitoso()
    {
        // Arrange
        string respuesta = "";
        int codigoHttp = 200;

        // Act
        ResultadoSubida resultado = ClienteClubLog.ParsearRespuestaSubida(respuesta, codigoHttp);

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.Mensaje.Should().Contain("procesado");
        resultado.Servicio.Should().Be(ServicioExterno.ClubLog);
    }

    [Fact]
    public void ParsearRespuestaSubida_ConRespuestaVaciaYCodigoError_DebeRetornarNoExitoso()
    {
        // Arrange
        string respuesta = "";
        int codigoHttp = 500;

        // Act
        ResultadoSubida resultado = ClienteClubLog.ParsearRespuestaSubida(respuesta, codigoHttp);

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Servicio.Should().Be(ServicioExterno.ClubLog);
    }
}
