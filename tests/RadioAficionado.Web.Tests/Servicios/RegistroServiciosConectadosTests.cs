using FluentAssertions;
using RadioAficionado.Web.Servicios;

namespace RadioAficionado.Web.Tests.Servicios;

/// <summary>
/// Tests unitarios para <see cref="RegistroServiciosConectados"/>.
/// Verifica el registro thread-safe de conexiones de usuarios.
/// </summary>
public class RegistroServiciosConectadosTests
{
    [Fact]
    public void Registrar_NuevoUsuario_EstaConectadoTrue()
    {
        // Arrange
        RegistroServiciosConectados registro = new();
        string usuarioId = "usuario-001";
        string connectionId = "conn-abc-123";

        // Act
        registro.Registrar(usuarioId, connectionId);

        // Assert
        registro.EstaConectado(usuarioId).Should().BeTrue();
    }

    [Fact]
    public void ObtenerConnectionId_UsuarioRegistrado_RetornaConnectionId()
    {
        // Arrange
        RegistroServiciosConectados registro = new();
        string usuarioId = "usuario-002";
        string connectionId = "conn-def-456";
        registro.Registrar(usuarioId, connectionId);

        // Act
        string? resultado = registro.ObtenerConnectionId(usuarioId);

        // Assert
        resultado.Should().Be("conn-def-456");
    }

    [Fact]
    public void ObtenerConnectionId_UsuarioNoRegistrado_RetornaNull()
    {
        // Arrange
        RegistroServiciosConectados registro = new();

        // Act
        string? resultado = registro.ObtenerConnectionId("usuario-inexistente");

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void Eliminar_UsuarioRegistrado_EstaConectadoFalse()
    {
        // Arrange
        RegistroServiciosConectados registro = new();
        string usuarioId = "usuario-003";
        registro.Registrar(usuarioId, "conn-ghi-789");

        // Act
        registro.Eliminar(usuarioId);

        // Assert
        registro.EstaConectado(usuarioId).Should().BeFalse();
        registro.ObtenerConnectionId(usuarioId).Should().BeNull();
    }

    [Fact]
    public void Registrar_MismoUsuarioDosVeces_SobreescribeConnectionId()
    {
        // Arrange
        RegistroServiciosConectados registro = new();
        string usuarioId = "usuario-004";
        registro.Registrar(usuarioId, "conn-primera");

        // Act
        registro.Registrar(usuarioId, "conn-segunda");

        // Assert
        string? resultado = registro.ObtenerConnectionId(usuarioId);
        resultado.Should().Be("conn-segunda");
    }

    [Fact]
    public void Concurrencia_MultiplesHilos_NoLanzaExcepcion()
    {
        // Arrange
        RegistroServiciosConectados registro = new();
        List<string> usuarioIds = Enumerable.Range(1, 100)
            .Select(i => $"usuario-concurrente-{i}")
            .ToList();

        // Act
        Action accion = () =>
        {
            Parallel.ForEach(usuarioIds, usuarioId =>
            {
                registro.Registrar(usuarioId, $"conn-{usuarioId}");
                registro.EstaConectado(usuarioId);
                registro.ObtenerConnectionId(usuarioId);
                registro.Eliminar(usuarioId);
                registro.Registrar(usuarioId, $"conn-{usuarioId}-nueva");
            });
        };

        // Assert
        accion.Should().NotThrow();
    }
}
