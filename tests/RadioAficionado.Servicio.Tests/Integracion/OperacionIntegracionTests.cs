using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura.Persistencia;
using RadioAficionado.Servicio.Dtos;
using RadioAficionado.Servicio.Servicios;

namespace RadioAficionado.Servicio.Tests.Integracion;

/// <summary>
/// Tests de integracion para endpoints HTTP del servicio.
/// Usa WebApplicationFactory reemplazando servicios nativos (audio, rig, SDR)
/// con mocks y la BD SQLite con InMemory para evitar dependencias de hardware
/// y las limitaciones de SQLite con DateTimeOffset.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OperacionIntegracionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _cliente;

    public OperacionIntegracionTests(WebApplicationFactory<Program> factory)
    {
        _cliente = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Eliminar TODO lo relacionado con EF Core SQLite para evitar
                // conflicto de proveedores y problemas con DateTimeOffset
                List<ServiceDescriptor> descriptoresEf = services
                    .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                             || d.ServiceType == typeof(DbContextOptions<ContextoRadioAficionado>)
                             || d.ServiceType == typeof(DbContextOptions)
                             || d.ImplementationType?.FullName?.Contains("Sqlite") == true)
                    .ToList();
                foreach (ServiceDescriptor descriptor in descriptoresEf)
                {
                    services.Remove(descriptor);
                }
                services.RemoveAll<ContextoRadioAficionado>();

                // Re-registrar con InMemory
                services.AddDbContext<ContextoRadioAficionado>(opciones =>
                {
                    opciones.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString("N"));
                });

                // Reemplazar servicios nativos que dependen de hardware
                services.RemoveAll<IAudioPipeline>();
                services.AddSingleton(new Mock<IAudioPipeline>().Object);

                services.RemoveAll<IServicioWaterfall>();
                services.AddSingleton(new Mock<IServicioWaterfall>().Object);

                services.RemoveAll<IControlRotador>();
                services.AddSingleton(new Mock<IControlRotador>().Object);

                // Eliminar el HostedService que abre un socket UDP
                services.RemoveAll<IHostedService>();
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ================================================================
    // VISTAS MVC
    // ================================================================

    [Fact]
    public async Task GetOperacion_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/Operacion");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperacionLogbook_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/Operacion/Logbook");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperacionDxCluster_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/Operacion/DxCluster");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperacionDxcc_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/Operacion/Dxcc");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperacionPropagacion_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/Operacion/Propagacion");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperacionActivaciones_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/Operacion/Activaciones");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperacionContest_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/Operacion/Contest");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOperacionSatelites_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/Operacion/Satelites");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/health");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ================================================================
    // APIs REST — GET
    // ================================================================

    [Fact]
    public async Task GetApiLogbook_RetornaOkConEstructuraCorrecta()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/logbook");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument json = await JsonDocument.ParseAsync(
            await respuesta.Content.ReadAsStreamAsync());
        json.RootElement.TryGetProperty("total", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("qsos", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetApiLogbook_ConBusqueda_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/logbook?busqueda=W1AW");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApiDxcc_RetornaOkConEstructuraCorrecta()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/dxcc");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument json = await JsonDocument.ParseAsync(
            await respuesta.Content.ReadAsStreamAsync());
        json.RootElement.TryGetProperty("trabajados", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("confirmados", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("total", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("entidades", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetApiDxcc_ConFiltroBanda_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/dxcc?banda=20m");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApiDxcc_ConFiltroEstado_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/dxcc?estado=necesitado");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApiPropagacion_RetornaOkConIndicesSolares()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/propagacion");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument json = await JsonDocument.ParseAsync(
            await respuesta.Content.ReadAsStreamAsync());
        json.RootElement.TryGetProperty("sfi", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("sn", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("bandas", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetApiActivaciones_RetornaOkConActivaciones()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/activaciones");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument json = await JsonDocument.ParseAsync(
            await respuesta.Content.ReadAsStreamAsync());
        json.RootElement.TryGetProperty("activaciones", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetApiActivaciones_ConTipoPota_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/activaciones?tipo=pota");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApiActivaciones_ConTipoSota_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/activaciones?tipo=sota");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApiContest_SinContest_RetornaOkConInactivo()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/contest");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument json = await JsonDocument.ParseAsync(
            await respuesta.Content.ReadAsStreamAsync());
        json.RootElement.GetProperty("activo").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GetApiContest_ConContest_RetornaOkConDatos()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/contest?contest=cqww-ssb");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument json = await JsonDocument.ParseAsync(
            await respuesta.Content.ReadAsStreamAsync());
        json.RootElement.TryGetProperty("contest", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("qsos", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetApiSatelites_RetornaOkConPases()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/satelites");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonDocument json = await JsonDocument.ParseAsync(
            await respuesta.Content.ReadAsStreamAsync());
        json.RootElement.TryGetProperty("pases", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetApiSatelites_ConFiltro_RetornaOk()
    {
        // Arrange & Act
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/satelites?satelite=ISS");

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ================================================================
    // APIs REST — POST
    // ================================================================

    [Fact]
    public async Task PostApiLogbookRegistrar_ConDatosValidos_RetornaOk()
    {
        // Arrange
        RegistroQsoDto registro = new(
            Indicativo: "W1AW",
            FrecuenciaHz: 14074000,
            Modo: "FT8",
            RstEnviado: "-15",
            RstRecibido: "-12",
            Grid: "FN31",
            Nombre: null,
            Comentario: null);

        // Act
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/logbook/registrar", registro);

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostApiLogbookRegistrar_SinIndicativo_RetornaBadRequest()
    {
        // Arrange
        RegistroQsoDto registro = new(
            Indicativo: "",
            FrecuenciaHz: 14074000,
            Modo: "FT8",
            RstEnviado: "-15",
            RstRecibido: "-12",
            Grid: null,
            Nombre: null,
            Comentario: null);

        // Act
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/logbook/registrar", registro);

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostApiLogbookRegistrar_ModoInvalido_RetornaBadRequest()
    {
        // Arrange
        RegistroQsoDto registro = new(
            Indicativo: "W1AW",
            FrecuenciaHz: 14074000,
            Modo: "INVENTADO",
            RstEnviado: "-15",
            RstRecibido: "-12",
            Grid: null,
            Nombre: null,
            Comentario: null);

        // Act
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/logbook/registrar", registro);

        // Assert
        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
