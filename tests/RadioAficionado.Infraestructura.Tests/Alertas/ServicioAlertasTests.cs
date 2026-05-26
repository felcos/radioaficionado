using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Alertas;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.Alertas;

namespace RadioAficionado.Infraestructura.Tests.Alertas;

public class ServicioAlertasTests
{
    private readonly ServicioAlertas _servicio;
    private readonly Mock<ILogger<ServicioAlertas>> _loggerMock;

    public ServicioAlertasTests()
    {
        _loggerMock = new Mock<ILogger<ServicioAlertas>>();
        _servicio = new ServicioAlertas(_loggerMock.Object);
    }

    [Fact]
    public void AgregarRegla_ReglaValida_SeAgregaCorrectamente()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "DXCC nuevas",
            Tipo = TipoAlerta.DxccNueva
        };

        // Act
        _servicio.AgregarRegla(regla);

        // Assert
        IReadOnlyList<ReglaAlerta> reglas = _servicio.ObtenerReglas();
        reglas.Should().HaveCount(1);
        reglas[0].Nombre.Should().Be("DXCC nuevas");
    }

    [Fact]
    public void AgregarRegla_ReglaNula_LanzaExcepcion()
    {
        // Act
        Action accion = () => _servicio.AgregarRegla(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EliminarRegla_ReglaExistente_DevuelveTrue()
    {
        // Arrange
        ReglaAlerta regla = new() { Nombre = "Test" };
        _servicio.AgregarRegla(regla);

        // Act
        bool resultado = _servicio.EliminarRegla(regla.Id);

        // Assert
        resultado.Should().BeTrue();
        _servicio.ObtenerReglas().Should().BeEmpty();
    }

    [Fact]
    public void EliminarRegla_ReglaInexistente_DevuelveFalse()
    {
        // Act
        bool resultado = _servicio.EliminarRegla(Guid.NewGuid());

        // Assert
        resultado.Should().BeFalse();
    }

    [Fact]
    public void CambiarEstadoRegla_DesactivarRegla_NoEvalua()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "Banda 20m",
            Tipo = TipoAlerta.Banda,
            Banda = BandaRadio.Banda20m
        };
        _servicio.AgregarRegla(regla);
        _servicio.CambiarEstadoRegla(regla.Id, false);

        // Act — spot en 20m, pero regla desactivada
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "JA1XYZ", 14_074_000, "FT8 -12dB", DateTime.UtcNow);

        // Assert
        alertas.Should().BeEmpty();
    }

    [Fact]
    public void EvaluarSpot_AlertaBanda_DisparaEnBandaCorrecta()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "Spots en 20m",
            Tipo = TipoAlerta.Banda,
            Banda = BandaRadio.Banda20m
        };
        _servicio.AgregarRegla(regla);

        // Act — 14.074 MHz = banda 20m
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "JA1XYZ", 14_074_000, "FT8", DateTime.UtcNow);

        // Assert
        alertas.Should().HaveCount(1);
        alertas[0].Mensaje.Should().Contain("20 metros");
    }

    [Fact]
    public void EvaluarSpot_AlertaBanda_NoDisparaEnOtraBanda()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "Spots en 20m",
            Tipo = TipoAlerta.Banda,
            Banda = BandaRadio.Banda20m
        };
        _servicio.AgregarRegla(regla);

        // Act — 7.074 MHz = banda 40m
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "JA1XYZ", 7_074_000, "FT8", DateTime.UtcNow);

        // Assert
        alertas.Should().BeEmpty();
    }

    [Fact]
    public void EvaluarSpot_AlertaModo_DisparaEnModoCorrecto()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "Spots CW",
            Tipo = TipoAlerta.Modo,
            Modo = "CW"
        };
        _servicio.AgregarRegla(regla);

        // Act
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "JA1XYZ", 14_030_000, "CW 599", DateTime.UtcNow);

        // Assert
        alertas.Should().HaveCount(1);
        alertas[0].Mensaje.Should().Contain("CW");
    }

    [Fact]
    public void EvaluarSpot_AlertaModo_NoDisparaEnOtroModo()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "Spots CW",
            Tipo = TipoAlerta.Modo,
            Modo = "CW"
        };
        _servicio.AgregarRegla(regla);

        // Act
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "JA1XYZ", 14_074_000, "FT8 -12dB", DateTime.UtcNow);

        // Assert
        alertas.Should().BeEmpty();
    }

    [Fact]
    public void EvaluarSpot_AlertaIndicativo_DisparaCuandoCoincide()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "Buscar JA1",
            Tipo = TipoAlerta.Indicativo,
            Indicativo = "JA1"
        };
        _servicio.AgregarRegla(regla);

        // Act
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "JA1XYZ", 14_074_000, "FT8", DateTime.UtcNow);

        // Assert
        alertas.Should().HaveCount(1);
        alertas[0].Mensaje.Should().Contain("JA1XYZ");
    }

    [Fact]
    public void EvaluarSpot_AlertaIndicativo_NoDisparaCuandoNoCoincide()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "Buscar JA1",
            Tipo = TipoAlerta.Indicativo,
            Indicativo = "JA1"
        };
        _servicio.AgregarRegla(regla);

        // Act
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "W1AW", 14_074_000, "FT8", DateTime.UtcNow);

        // Assert
        alertas.Should().BeEmpty();
    }

    [Fact]
    public void EvaluarSpot_AlertaBandaYModo_DisparaCuandoAmbasCoinciden()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "20m CW",
            Tipo = TipoAlerta.BandaYModo,
            Banda = BandaRadio.Banda20m,
            Modo = "CW"
        };
        _servicio.AgregarRegla(regla);

        // Act — 14.030 MHz = 20m, comentario CW
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "JA1XYZ", 14_030_000, "CW 599", DateTime.UtcNow);

        // Assert
        alertas.Should().HaveCount(1);
        alertas[0].Mensaje.Should().Contain("20 metros").And.Contain("CW");
    }

    [Fact]
    public void EvaluarSpot_AlertaBandaYModo_NoDisparaCuandoSoloBandaCoincide()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "20m CW",
            Tipo = TipoAlerta.BandaYModo,
            Banda = BandaRadio.Banda20m,
            Modo = "CW"
        };
        _servicio.AgregarRegla(regla);

        // Act — 20m pero FT8, no CW
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "JA1XYZ", 14_074_000, "FT8 -12dB", DateTime.UtcNow);

        // Assert
        alertas.Should().BeEmpty();
    }

    [Fact]
    public void EvaluarSpot_DxccNueva_DisparaCuandoEntidadNoTrabajada()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "DXCC nuevas",
            Tipo = TipoAlerta.DxccNueva
        };
        _servicio.AgregarRegla(regla);
        // No actualizar entidades trabajadas = todas son nuevas
        _servicio.ActualizarEntidadesTrabajadas([]);

        // Act — W1AW = Estados Unidos (DXCC 291)
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "W1AW", 14_074_000, "FT8", DateTime.UtcNow);

        // Assert
        alertas.Should().HaveCount(1);
        alertas[0].Mensaje.Should().Contain("DXCC nueva");
        alertas[0].EntidadDxcc.Should().NotBeNull();
    }

    [Fact]
    public void EvaluarSpot_DxccNueva_NoDisparaCuandoEntidadYaTrabajada()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "DXCC nuevas",
            Tipo = TipoAlerta.DxccNueva
        };
        _servicio.AgregarRegla(regla);
        // Marcar EEUU (291) como trabajada
        _servicio.ActualizarEntidadesTrabajadas([291]);

        // Act — W1AW = Estados Unidos (DXCC 291)
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "W1AW", 14_074_000, "FT8", DateTime.UtcNow);

        // Assert
        alertas.Should().BeEmpty();
    }

    [Fact]
    public void EvaluarSpot_DxccNuevaConFiltroBanda_SoloDisparaEnBandaCorrecta()
    {
        // Arrange
        ReglaAlerta regla = new()
        {
            Nombre = "DXCC nuevas en 20m",
            Tipo = TipoAlerta.DxccNueva,
            Banda = BandaRadio.Banda20m
        };
        _servicio.AgregarRegla(regla);
        _servicio.ActualizarEntidadesTrabajadas([]);

        // Act — W1AW en 40m (7 MHz)
        IReadOnlyList<ResultadoAlerta> alertas40m = _servicio.EvaluarSpot(
            "EA1ABC", "W1AW", 7_074_000, "FT8", DateTime.UtcNow);

        // Act — W1AW en 20m (14 MHz)
        IReadOnlyList<ResultadoAlerta> alertas20m = _servicio.EvaluarSpot(
            "EA1ABC", "W1AW", 14_074_000, "FT8", DateTime.UtcNow);

        // Assert
        alertas40m.Should().BeEmpty();
        alertas20m.Should().HaveCount(1);
    }

    [Fact]
    public void EvaluarSpot_MultiplesReglas_DisparaTodasLasQueCoinciden()
    {
        // Arrange
        _servicio.AgregarRegla(new ReglaAlerta
        {
            Nombre = "20m",
            Tipo = TipoAlerta.Banda,
            Banda = BandaRadio.Banda20m
        });
        _servicio.AgregarRegla(new ReglaAlerta
        {
            Nombre = "FT8",
            Tipo = TipoAlerta.Modo,
            Modo = "FT8"
        });

        // Act — spot en 20m FT8 cumple ambas reglas
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "JA1XYZ", 14_074_000, "FT8 -12dB", DateTime.UtcNow);

        // Assert
        alertas.Should().HaveCount(2);
    }

    [Fact]
    public void EvaluarSpot_DxVacio_NoDispara()
    {
        // Arrange
        _servicio.AgregarRegla(new ReglaAlerta
        {
            Nombre = "20m",
            Tipo = TipoAlerta.Banda,
            Banda = BandaRadio.Banda20m
        });

        // Act
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "", 14_074_000, "FT8", DateTime.UtcNow);

        // Assert
        alertas.Should().BeEmpty();
    }

    [Fact]
    public void EvaluarSpot_AlertaDisparada_EventoSeEmite()
    {
        // Arrange
        ResultadoAlerta? alertaRecibida = null;
        _servicio.AlertaDisparada += (_, alerta) => alertaRecibida = alerta;
        _servicio.AgregarRegla(new ReglaAlerta
        {
            Nombre = "20m",
            Tipo = TipoAlerta.Banda,
            Banda = BandaRadio.Banda20m
        });

        // Act
        _servicio.EvaluarSpot("EA1ABC", "JA1XYZ", 14_074_000, "FT8", DateTime.UtcNow);

        // Assert
        alertaRecibida.Should().NotBeNull();
        alertaRecibida!.Regla.Nombre.Should().Be("20m");
    }

    [Fact]
    public void ActualizarEntidadesTrabajadas_Nulo_LanzaExcepcion()
    {
        // Act
        Action accion = () => _servicio.ActualizarEntidadesTrabajadas(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ObtenerReglas_SinReglas_DevuelveListaVacia()
    {
        // Act
        IReadOnlyList<ReglaAlerta> reglas = _servicio.ObtenerReglas();

        // Assert
        reglas.Should().BeEmpty();
    }

    [Fact]
    public void CambiarEstadoRegla_ReglaInexistente_DevuelveFalse()
    {
        // Act
        bool resultado = _servicio.CambiarEstadoRegla(Guid.NewGuid(), true);

        // Assert
        resultado.Should().BeFalse();
    }

    [Fact]
    public void EvaluarSpot_InferirModoSsb_NormalizaLsbUsb()
    {
        // Arrange
        _servicio.AgregarRegla(new ReglaAlerta
        {
            Nombre = "SSB",
            Tipo = TipoAlerta.Modo,
            Modo = "SSB"
        });

        // Act — comentario dice USB pero el modo se normaliza a SSB
        IReadOnlyList<ResultadoAlerta> alertas = _servicio.EvaluarSpot(
            "EA1ABC", "JA1XYZ", 14_200_000, "USB 59+10", DateTime.UtcNow);

        // Assert
        alertas.Should().HaveCount(1);
    }
}
