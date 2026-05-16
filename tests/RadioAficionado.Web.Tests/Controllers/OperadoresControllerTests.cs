using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.Controllers;
using RadioAficionado.Web.ViewModels;
using Xunit;

namespace RadioAficionado.Web.Tests.Controllers;

/// <summary>
/// Tests unitarios para <see cref="OperadoresController"/>.
/// </summary>
public class OperadoresControllerTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly Mock<UserManager<UsuarioRadio>> _mockUserManager;
    private readonly Mock<ILogger<OperadoresController>> _mockLogger;
    private readonly OperadoresController _controlador;

    public OperadoresControllerTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _mockLogger = new Mock<ILogger<OperadoresController>>();

        Mock<IUserStore<UsuarioRadio>> mockUserStore = new();
        _mockUserManager = new Mock<UserManager<UsuarioRadio>>(
            mockUserStore.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<UsuarioRadio>>().Object,
            Array.Empty<IUserValidator<UsuarioRadio>>(),
            Array.Empty<IPasswordValidator<UsuarioRadio>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<UsuarioRadio>>>().Object);

        _controlador = new OperadoresController(
            _mockUserManager.Object,
            _mockRepositorio.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Perfil_IndicativoVacio_RetornaNotFound()
    {
        // Act
        IActionResult resultado = await _controlador.Perfil("", CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Perfil_IndicativoNulo_RetornaNotFound()
    {
        // Act
        IActionResult resultado = await _controlador.Perfil(null!, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MapaDatosOperador_IndicativoVacio_RetornaJsonVacio()
    {
        // Act
        IActionResult resultado = await _controlador.MapaDatosOperador("", CancellationToken.None);

        // Assert
        JsonResult json = resultado.Should().BeOfType<JsonResult>().Subject;
        json.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task MapaDatosOperador_IndicativoNulo_RetornaJsonVacio()
    {
        // Act
        IActionResult resultado = await _controlador.MapaDatosOperador(null!, CancellationToken.None);

        // Assert
        resultado.Should().BeOfType<JsonResult>();
    }

    [Fact]
    public async Task MapaDatosOperador_ConQsosConLocalizador_RetornaMarcadores()
    {
        // Arrange
        string indicativo = "EA4ABC";
        Qso qsoConLocalizador = Qso.Crear(
            indicativoPropio: new Indicativo(indicativo),
            indicativoContacto: new Indicativo("W1AW"),
            fechaHoraInicio: DateTimeOffset.UtcNow.AddHours(-1),
            frecuencia: Frecuencia.DesdeHz(14074000),
            modo: ModoOperacion.FT8,
            senalEnviada: "59",
            localizadorContacto: new Localizador("FN31pr"));

        Qso qsoSinLocalizador = Qso.Crear(
            indicativoPropio: new Indicativo(indicativo),
            indicativoContacto: new Indicativo("DL1ABC"),
            fechaHoraInicio: DateTimeOffset.UtcNow.AddHours(-2),
            frecuencia: Frecuencia.DesdeHz(7074000),
            modo: ModoOperacion.FT8,
            senalEnviada: "59");

        _mockRepositorio
            .Setup(r => r.BuscarPorIndicativoAsync(It.IsAny<Indicativo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso> { qsoConLocalizador, qsoSinLocalizador });

        // Act
        IActionResult resultado = await _controlador.MapaDatosOperador(indicativo, CancellationToken.None);

        // Assert
        JsonResult json = resultado.Should().BeOfType<JsonResult>().Subject;
        IReadOnlyList<MapaContactoViewModel> marcadores = json.Value.Should().BeAssignableTo<IReadOnlyList<MapaContactoViewModel>>().Subject;
        marcadores.Should().HaveCount(1);
        marcadores[0].Indicativo.Should().Be("W1AW");
    }
}
