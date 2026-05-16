using FluentAssertions;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Servicio.Servicios;

namespace RadioAficionado.Servicio.Tests.Servicios;

/// <summary>
/// Tests para ColoreadorIndicativos.
/// </summary>
public sealed class ColoreadorIndicativosTests
{
    private readonly ColoreadorIndicativos _coloreador;

    public ColoreadorIndicativosTests()
    {
        Mock<IRepositorioQso> mockRepo = new();
        _coloreador = new ColoreadorIndicativos(mockRepo.Object, "EA1ABC");
    }

    [Fact]
    public void DeterminarColor_MensajeCQ_RetornaRojo()
    {
        // Act
        string color = _coloreador.DeterminarColor("CQ W1AW FN31", "W1AW", null, "20m");

        // Assert
        color.Should().Be(ColoreadorIndicativos.ColorCq);
    }

    [Fact]
    public void DeterminarColor_MensajeDirigidoAMi_RetornaBlanco()
    {
        // Act
        string color = _coloreador.DeterminarColor("EA1ABC W1AW -15", "W1AW", "EA1ABC", "20m");

        // Assert
        color.Should().Be(ColoreadorIndicativos.ColorDirigido);
    }

    [Fact]
    public void DeterminarColor_MensajeGenerico_RetornaPorDefecto()
    {
        // Act
        string color = _coloreador.DeterminarColor("W1AW K2ABC -10", "K2ABC", "W1AW", "20m");

        // Assert
        color.Should().Be(ColoreadorIndicativos.ColorPorDefecto);
    }
}
