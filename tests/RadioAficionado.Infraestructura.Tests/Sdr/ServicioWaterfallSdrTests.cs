using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.Sdr;
using RadioAficionado.Nativo.Sdr;

namespace RadioAficionado.Infraestructura.Tests.Sdr;

/// <summary>
/// Tests unitarios para <see cref="ServicioWaterfallSdr"/>.
/// Verifican el ciclo de vida, suscripción al SDR y generación de espectro.
/// </summary>
public sealed class ServicioWaterfallSdrTests : IAsyncDisposable
{
    private readonly Mock<IConvertidorIqAAudio> _convertidorMock;
    private readonly Mock<ILogger<ServicioWaterfallSdr>> _loggerMock;
    private readonly ServicioWaterfallSdr _servicio;

    public ServicioWaterfallSdrTests()
    {
        _convertidorMock = new Mock<IConvertidorIqAAudio>();
        _convertidorMock.SetupProperty(c => c.GananciaDigital, 1.0);
        _loggerMock = new Mock<ILogger<ServicioWaterfallSdr>>();
        _servicio = new ServicioWaterfallSdr(_convertidorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_NoLanzaExcepcion()
    {
        // Arrange & Act
        ServicioWaterfallSdr servicio = new(
            _convertidorMock.Object,
            _loggerMock.Object);

        // Assert
        servicio.Should().NotBeNull();
    }

    [Fact]
    public void FuenteDeDatos_Inicial_EsNinguna()
    {
        // Arrange & Act — estado inicial

        // Assert
        _servicio.FuenteDeDatos.Should().Be(FuenteDeDatosWaterfall.Ninguna);
    }

    [Fact]
    public async Task IniciarConSdrAsync_SdrNulo_LanzaExcepcion()
    {
        // Arrange
        IReceptorSdr? receptorNulo = null;

        // Act
        Func<Task> accion = () => _servicio.IniciarConSdrAsync(receptorNulo!);

        // Assert
        await accion.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("receptor");
    }

    [Fact]
    public async Task IniciarConSdrAsync_CambiaFuenteASdr()
    {
        // Arrange
        Mock<IReceptorSdr> receptorMock = CrearReceptorMock();

        // Act
        await _servicio.IniciarConSdrAsync(receptorMock.Object);

        // Assert
        _servicio.FuenteDeDatos.Should().Be(FuenteDeDatosWaterfall.Sdr);
    }

    [Fact]
    public async Task DetenerAsync_CambiaFuenteANinguna()
    {
        // Arrange
        Mock<IReceptorSdr> receptorMock = CrearReceptorMock();
        await _servicio.IniciarConSdrAsync(receptorMock.Object);

        // Act
        await _servicio.DetenerAsync();

        // Assert
        _servicio.FuenteDeDatos.Should().Be(FuenteDeDatosWaterfall.Ninguna);
    }

    [Fact]
    public void EstaActivo_Inicial_EsFalso()
    {
        // Arrange & Act — estado inicial

        // Assert
        _servicio.EstaActivo.Should().BeFalse();
    }

    [Fact]
    public async Task IniciarConSdrAsync_EstaActivoEsTrue()
    {
        // Arrange
        Mock<IReceptorSdr> receptorMock = CrearReceptorMock();

        // Act
        await _servicio.IniciarConSdrAsync(receptorMock.Object);

        // Assert
        _servicio.EstaActivo.Should().BeTrue();
    }

    [Fact]
    public void ConfigurarConvertidor_CambiaGanancia()
    {
        // Arrange
        double nuevaGanancia = 2.5;

        // Act
        _servicio.ConfigurarConvertidor(nuevaGanancia);

        // Assert
        _convertidorMock.Object.GananciaDigital.Should().Be(nuevaGanancia);
    }

    [Fact]
    public void TamanoFft_PorDefecto_Es1024()
    {
        // Arrange & Act — estado inicial

        // Assert
        _servicio.TamanoFft.Should().Be(1024);
    }

    [Fact]
    public async Task Dispose_MultiplesVeces_NoLanzaExcepcion()
    {
        // Arrange
        Mock<IReceptorSdr> receptorMock = CrearReceptorMock();
        await _servicio.IniciarConSdrAsync(receptorMock.Object);

        // Act
        Func<Task> accion = async () =>
        {
            await _servicio.DisposeAsync();
            await _servicio.DisposeAsync();
            await _servicio.DisposeAsync();
        };

        // Assert
        await accion.Should().NotThrowAsync();
    }

    /// <summary>
    /// Crea un mock de IReceptorSdr con valores por defecto para las pruebas.
    /// </summary>
    private static Mock<IReceptorSdr> CrearReceptorMock()
    {
        Mock<IReceptorSdr> receptorMock = new();
        receptorMock.Setup(r => r.TasaDeMuestreoHz).Returns(2_048_000);
        receptorMock.Setup(r => r.EstaConectado).Returns(true);
        receptorMock.Setup(r => r.FrecuenciaCentralHz).Returns(145_000_000);
        return receptorMock;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _servicio.DisposeAsync();
    }
}
