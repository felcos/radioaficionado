using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Nativo.Rotador;

namespace RadioAficionado.Infraestructura.Tests.Rotador;

/// <summary>
/// Tests unitarios para <see cref="ClienteRotctld"/>.
/// Verifica el estado inicial, manejo sin conexión y gestión de recursos.
/// </summary>
public class ClienteRotctldTests
{
    [Fact]
    public void EstaConectado_Inicial_EsFalso()
    {
        // Arrange
        ClienteRotctld cliente = new ClienteRotctld();

        // Act
        bool estaConectado = cliente.EstaConectado;

        // Assert
        estaConectado.Should().BeFalse();
    }

    [Fact]
    public void SoportaElevacion_Inicial_EsFalso()
    {
        // Arrange
        ClienteRotctld cliente = new ClienteRotctld();

        // Act
        bool soportaElevacion = cliente.SoportaElevacion;

        // Assert
        soportaElevacion.Should().BeFalse();
    }

    [Fact]
    public async Task ConectarAsync_ServidorNoDisponible_LanzaExcepcion()
    {
        // Arrange
        ClienteRotctld cliente = new ClienteRotctld();

        // Act
        Func<Task> accion = async () => await cliente.ConectarAsync("localhost", 59999);

        // Assert
        await accion.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DesconectarAsync_SinConectar_NoLanzaExcepcion()
    {
        // Arrange
        ClienteRotctld cliente = new ClienteRotctld();

        // Act
        Func<Task> accion = async () => await cliente.DesconectarAsync();

        // Assert
        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MoverAsync_SinConectar_LanzaInvalidOperationException()
    {
        // Arrange
        ClienteRotctld cliente = new ClienteRotctld();
        PosicionRotador posicion = new PosicionRotador(180.0, 45.0);

        // Act
        Func<Task> accion = async () => await cliente.MoverAsync(posicion);

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DetenerAsync_SinConectar_LanzaInvalidOperationException()
    {
        // Arrange
        ClienteRotctld cliente = new ClienteRotctld();

        // Act
        Func<Task> accion = async () => await cliente.DetenerAsync();

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DisposeAsync_MultiplesVeces_NoLanzaExcepcion()
    {
        // Arrange
        ClienteRotctld cliente = new ClienteRotctld();

        // Act
        Func<Task> accion = async () =>
        {
            await cliente.DisposeAsync();
            await cliente.DisposeAsync();
        };

        // Assert
        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_ConConfiguracion_NoLanzaExcepcion()
    {
        // Arrange
        ConfiguracionRotador configuracion = new ConfiguracionRotador
        {
            Host = "192.168.1.100",
            Puerto = 4534,
            IntervaloPollingMs = 2000,
            UmbralCambioGrados = 1.0,
            TimeoutMs = 3000
        };

        // Act
        ClienteRotctld cliente = new ClienteRotctld(configuracion);

        // Assert
        cliente.Should().NotBeNull();
        cliente.EstaConectado.Should().BeFalse();
    }
}
