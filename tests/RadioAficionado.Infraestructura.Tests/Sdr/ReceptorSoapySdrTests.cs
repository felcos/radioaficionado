using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Sdr;
using RadioAficionado.Nativo.Sdr;

namespace RadioAficionado.Infraestructura.Tests.Sdr;

/// <summary>
/// Tests unitarios para <see cref="ReceptorSoapySdr"/>.
/// Verifican el comportamiento del receptor sin hardware SDR conectado.
/// </summary>
public sealed class ReceptorSoapySdrTests : IDisposable
{
    private readonly Mock<ILogger<ReceptorSoapySdr>> _loggerMock;
    private readonly ConfiguracionSdr _configuracion;
    private readonly ReceptorSoapySdr _receptor;

    public ReceptorSoapySdrTests()
    {
        _loggerMock = new Mock<ILogger<ReceptorSoapySdr>>();
        _configuracion = new ConfiguracionSdr(
            FrecuenciaCentralHz: 145_000_000,
            TasaDeMuestreoHz: 2_048_000,
            AnchoDeBandaHz: 200_000,
            GananciaDb: 40.0);
        _receptor = new ReceptorSoapySdr(_loggerMock.Object, _configuracion);
    }

    [Fact]
    public void Crear_SinSoapySdr_NoLanzaExcepcion()
    {
        // Arrange & Act
        ReceptorSoapySdr receptor = new(_loggerMock.Object, _configuracion);

        // Assert
        receptor.Should().NotBeNull();
        receptor.Dispose();
    }

    [Fact]
    public void ObtenerDispositivosDisponibles_SinHardware_RetornaListaVacia()
    {
        // Arrange — sin hardware SDR conectado

        // Act
        IReadOnlyList<DispositivoSdr> dispositivos = _receptor.ObtenerDispositivosDisponibles();

        // Assert
        dispositivos.Should().NotBeNull();
        dispositivos.Should().BeEmpty();
    }

    [Fact]
    public async Task ConectarAsync_DispositivoNulo_LanzaExcepcion()
    {
        // Arrange
        string dispositivo = null!;

        // Act
        Func<Task> accion = () => _receptor.ConectarAsync(dispositivo);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dispositivo");
    }

    [Fact]
    public async Task ConectarAsync_DispositivoVacio_LanzaExcepcion()
    {
        // Arrange
        string dispositivo = "   ";

        // Act
        Func<Task> accion = () => _receptor.ConectarAsync(dispositivo);

        // Assert
        await accion.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dispositivo");
    }

    [Fact]
    public async Task DesconectarAsync_SinConectar_NoLanzaExcepcion()
    {
        // Arrange — receptor no conectado

        // Act
        Func<Task> accion = () => _receptor.DesconectarAsync();

        // Assert
        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConfigurarFrecuencia_SinConectar_LanzaExcepcion()
    {
        // Arrange
        double frecuencia = 145_500_000;

        // Act
        Func<Task> accion = () => _receptor.ConfigurarFrecuenciaAsync(frecuencia);

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no está conectado*");
    }

    [Fact]
    public async Task ConfigurarGanancia_SinConectar_LanzaExcepcion()
    {
        // Arrange
        double ganancia = 30.0;

        // Act
        Func<Task> accion = () => _receptor.ConfigurarGananciaAsync(ganancia);

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no está conectado*");
    }

    [Fact]
    public async Task ConfigurarAnchoDeBanda_SinConectar_LanzaExcepcion()
    {
        // Arrange
        double anchoBanda = 100_000;

        // Act
        Func<Task> accion = () => _receptor.ConfigurarAnchoDeBandaAsync(anchoBanda);

        // Assert
        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no está conectado*");
    }

    [Fact]
    public void EstaConectado_Inicial_EsFalso()
    {
        // Arrange & Act — estado inicial

        // Assert
        _receptor.EstaConectado.Should().BeFalse();
    }

    [Fact]
    public void DispositivoActual_Inicial_EsNull()
    {
        // Arrange & Act — estado inicial

        // Assert
        _receptor.DispositivoActual.Should().BeNull();
    }

    public void Dispose()
    {
        _receptor.Dispose();
    }
}
