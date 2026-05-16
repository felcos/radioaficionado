using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Web.Controllers;
using RadioAficionado.Web.Data;
using RadioAficionado.Web.ViewModels;

namespace RadioAficionado.Web.Tests.Controllers;

/// <summary>
/// Tests unitarios para <see cref="ForoController"/>.
/// Verifica el comportamiento de las acciones del foro con base de datos en memoria.
/// </summary>
public class ForoControllerTests : IDisposable
{
    private readonly ContextoIdentidadRadioAficionado _contexto;
    private readonly Mock<ILogger<ForoController>> _mockLogger;
    private readonly ForoController _controlador;

    public ForoControllerTests()
    {
        DbContextOptions<ContextoIdentidadRadioAficionado> opciones =
            new DbContextOptionsBuilder<ContextoIdentidadRadioAficionado>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

        _contexto = new ContextoIdentidadRadioAficionado(opciones);
        _mockLogger = new Mock<ILogger<ForoController>>();
        _controlador = new ForoController(_contexto, _mockLogger.Object);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _controlador.Dispose();
    }

    [Fact]
    public async Task Index_SinHilos_RetornaVistaConListaVacia()
    {
        // Arrange — BD vacía

        // Act
        IActionResult resultado = await _controlador.Index();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        ForoIndexViewModel modelo = vista.Model.Should().BeOfType<ForoIndexViewModel>().Subject;
        modelo.Hilos.Should().BeEmpty();
        modelo.TotalElementos.Should().Be(0);
    }

    [Fact]
    public async Task Detalle_IdInexistente_RetornaNotFound()
    {
        // Arrange
        Guid idInexistente = Guid.NewGuid();

        // Act
        IActionResult resultado = await _controlador.Detalle(idInexistente);

        // Assert
        resultado.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void CrearHilo_Get_RetornaVistaConModeloVacio()
    {
        // Arrange — no necesita setup

        // Act
        IActionResult resultado = _controlador.CrearHilo();

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.Model.Should().BeOfType<CrearHiloViewModel>();
    }

    [Fact]
    public async Task CrearHilo_Post_ModeloInvalido_RetornaVista()
    {
        // Arrange
        CrearHiloViewModel modelo = new CrearHiloViewModel();
        _controlador.ModelState.AddModelError("Titulo", "El título es obligatorio.");

        // Act
        IActionResult resultado = await _controlador.CrearHilo(modelo);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        vista.Model.Should().BeSameAs(modelo);
    }

    [Fact]
    public async Task Responder_HiloNoExiste_RetornaNotFound()
    {
        // Arrange
        ConfigurarUsuarioAutenticado("user-1");
        ResponderHiloViewModel modelo = new ResponderHiloViewModel
        {
            HiloId = Guid.NewGuid(),
            Contenido = "Respuesta de prueba para el test"
        };

        // Act
        IActionResult resultado = await _controlador.Responder(modelo);

        // Assert
        resultado.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Index_ConPaginacion_RetornaVistaConPaginaCorrecta()
    {
        // Arrange
        UsuarioRadio autor = CrearUsuario("user-1", "EA4TEST");
        _contexto.Users.Add(autor);

        for (int i = 0; i < 25; i++)
        {
            _contexto.HilosForo.Add(new HiloForo
            {
                Id = Guid.NewGuid(),
                Titulo = $"Hilo de prueba {i}",
                Contenido = $"Contenido del hilo {i}",
                AutorId = autor.Id,
                FechaCreacion = DateTime.UtcNow.AddMinutes(-i),
                FechaUltimaRespuesta = DateTime.UtcNow.AddMinutes(-i),
                Categoria = CategoriaForo.General
            });
        }

        await _contexto.SaveChangesAsync();

        // Act
        IActionResult resultado = await _controlador.Index(pagina: 2);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        ForoIndexViewModel modelo = vista.Model.Should().BeOfType<ForoIndexViewModel>().Subject;
        modelo.PaginaActual.Should().Be(2);
        modelo.Hilos.Should().HaveCount(5);
        modelo.TotalElementos.Should().Be(25);
    }

    [Fact]
    public async Task Detalle_HiloExiste_RetornaVistaConDetalle()
    {
        // Arrange
        UsuarioRadio autor = CrearUsuario("user-2", "EA4ABC");
        _contexto.Users.Add(autor);

        Guid hiloId = Guid.NewGuid();
        _contexto.HilosForo.Add(new HiloForo
        {
            Id = hiloId,
            Titulo = "Hilo de prueba",
            Contenido = "Contenido del hilo de prueba",
            AutorId = autor.Id,
            FechaCreacion = DateTime.UtcNow,
            FechaUltimaRespuesta = DateTime.UtcNow,
            Categoria = CategoriaForo.Tecnico
        });

        await _contexto.SaveChangesAsync();

        // Act
        IActionResult resultado = await _controlador.Detalle(hiloId);

        // Assert
        ViewResult vista = resultado.Should().BeOfType<ViewResult>().Subject;
        HiloDetalleViewModel modelo = vista.Model.Should().BeOfType<HiloDetalleViewModel>().Subject;
        modelo.Id.Should().Be(hiloId);
        modelo.Titulo.Should().Be("Hilo de prueba");
    }

    [Fact]
    public async Task CrearHilo_Post_ModeloValido_Redirige()
    {
        // Arrange
        ConfigurarUsuarioAutenticado("user-3");
        CrearHiloViewModel modelo = new CrearHiloViewModel
        {
            Titulo = "Nuevo hilo de prueba",
            Contenido = "Contenido suficiente para pasar la validación",
            Categoria = CategoriaForo.General
        };

        // Act
        IActionResult resultado = await _controlador.CrearHilo(modelo);

        // Assert
        RedirectToActionResult redireccion = resultado.Should().BeOfType<RedirectToActionResult>().Subject;
        redireccion.ActionName.Should().Be("Detalle");
        redireccion.RouteValues.Should().ContainKey("id");
    }

    /// <summary>
    /// Configura un usuario autenticado en el controlador con el ID especificado.
    /// </summary>
    private void ConfigurarUsuarioAutenticado(string usuarioId)
    {
        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId)
        };

        ClaimsIdentity identidad = new ClaimsIdentity(claims, "TestAuth");
        ClaimsPrincipal principal = new ClaimsPrincipal(identidad);

        _controlador.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>
    /// Crea un usuario de prueba para los tests.
    /// </summary>
    private static UsuarioRadio CrearUsuario(string id, string indicativo)
    {
        return new UsuarioRadio
        {
            Id = id,
            Indicativo = indicativo,
            Nombre = $"Test {indicativo}",
            UserName = indicativo.ToLowerInvariant(),
            NormalizedUserName = indicativo.ToUpperInvariant(),
            Email = $"{indicativo.ToLowerInvariant()}@test.com",
            NormalizedEmail = $"{indicativo.ToUpperInvariant()}@TEST.COM",
            FechaRegistro = DateTime.UtcNow
        };
    }
}
