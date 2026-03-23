using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RadioAficionado.Dominio.Configuracion;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.Configuracion;

namespace RadioAficionado.Infraestructura.Tests.Configuracion;

/// <summary>
/// Tests para ServicioConfiguracionJson: persistencia, carga, valores por defecto y manejo de errores.
/// </summary>
public sealed class ServicioConfiguracionJsonTests : IDisposable
{
    private readonly string _directorioTemporal;
    private readonly string _rutaArchivo;
    private readonly ILogger<ServicioConfiguracionJson> _logger;

    public ServicioConfiguracionJsonTests()
    {
        _directorioTemporal = Path.Combine(Path.GetTempPath(), "RadioAficionadoTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directorioTemporal);
        _rutaArchivo = Path.Combine(_directorioTemporal, "configuracion.json");
        _logger = NullLogger<ServicioConfiguracionJson>.Instance;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directorioTemporal))
        {
            Directory.Delete(_directorioTemporal, recursive: true);
        }
    }

    private ServicioConfiguracionJson CrearServicio()
    {
        return new ServicioConfiguracionJson(_rutaArchivo, _logger);
    }

    [Fact]
    public async Task CargarAsync_ArchivoNoExiste_DevuelveValoresPorDefecto()
    {
        // Arrange
        ServicioConfiguracionJson servicio = CrearServicio();

        // Act
        ConfiguracionCompleta resultado = await servicio.CargarAsync();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Estacion.IndicativoPropio.Should().BeEmpty();
        resultado.Rig.Host.Should().Be("localhost");
        resultado.Rig.Puerto.Should().Be(4532);
        resultado.Rotador.Puerto.Should().Be(4533);
        resultado.DxCluster.Servidor.Should().Be("dxc.ve7cc.net");
        resultado.General.IdiomaInterfaz.Should().Be("es");
    }

    [Fact]
    public async Task GuardarAsync_YCargarAsync_PersistenConfiguracionCompleta()
    {
        // Arrange
        ServicioConfiguracionJson servicio = CrearServicio();
        ConfiguracionCompleta configuracion = new ConfiguracionCompleta
        {
            Estacion = new ConfiguracionEstacion
            {
                IndicativoPropio = "EA4TEST",
                Localizador = "IN80DK",
                RegionItu = RegionItu.Region1,
                NivelLicencia = NivelLicencia.Avanzado,
                PotenciaMaximaVatios = 1500,
                Nombre = "Operador Test"
            },
            Rig = new ConfiguracionRigDto
            {
                Host = "192.168.1.100",
                Puerto = 4533,
                IntervaloPollingMs = 250,
                PotenciaMaximaVatios = 200.0,
                TimeoutMs = 3000
            },
            Rotador = new ConfiguracionRotadorDto
            {
                Host = "192.168.1.101",
                Puerto = 4534,
                IntervaloPollingMs = 2000,
                UmbralCambioGrados = 1.0,
                TimeoutMs = 8000
            },
            Audio = new ConfiguracionAudio
            {
                DispositivoEntrada = "Micrófono USB",
                DispositivoSalida = "Altavoz HDMI",
                FrecuenciaMuestreo = 96000
            },
            DxCluster = new ConfiguracionDxClusterDto
            {
                Servidor = "dxc.ea4abc.net",
                Puerto = 7301,
                IndicativoPropio = "EA4TEST",
                TimeoutMs = 15000,
                RetrasoReconexionMs = 10000,
                MaxIntentosReconexion = 10
            },
            General = new ConfiguracionGeneral
            {
                RutaBaseDatos = "/tmp/test.db",
                IdiomaInterfaz = "en",
                IniciarMinimizado = true,
                MostrarNotificaciones = false
            }
        };

        // Act
        await servicio.GuardarAsync(configuracion);
        ConfiguracionCompleta resultado = await servicio.CargarAsync();

        // Assert
        resultado.Estacion.IndicativoPropio.Should().Be("EA4TEST");
        resultado.Estacion.Localizador.Should().Be("IN80DK");
        resultado.Estacion.RegionItu.Should().Be(RegionItu.Region1);
        resultado.Estacion.NivelLicencia.Should().Be(NivelLicencia.Avanzado);
        resultado.Estacion.PotenciaMaximaVatios.Should().Be(1500);
        resultado.Estacion.Nombre.Should().Be("Operador Test");

        resultado.Rig.Host.Should().Be("192.168.1.100");
        resultado.Rig.Puerto.Should().Be(4533);
        resultado.Rig.IntervaloPollingMs.Should().Be(250);
        resultado.Rig.PotenciaMaximaVatios.Should().Be(200.0);

        resultado.Rotador.Host.Should().Be("192.168.1.101");
        resultado.Rotador.UmbralCambioGrados.Should().Be(1.0);

        resultado.Audio.DispositivoEntrada.Should().Be("Micrófono USB");
        resultado.Audio.FrecuenciaMuestreo.Should().Be(96000);

        resultado.DxCluster.Servidor.Should().Be("dxc.ea4abc.net");
        resultado.DxCluster.MaxIntentosReconexion.Should().Be(10);

        resultado.General.IdiomaInterfaz.Should().Be("en");
        resultado.General.IniciarMinimizado.Should().BeTrue();
        resultado.General.MostrarNotificaciones.Should().BeFalse();
    }

    [Fact]
    public async Task GuardarAsync_CreaDirectorioSiNoExiste()
    {
        // Arrange
        string rutaEnSubdirectorio = Path.Combine(_directorioTemporal, "sub", "config", "configuracion.json");
        ServicioConfiguracionJson servicio = new ServicioConfiguracionJson(rutaEnSubdirectorio, _logger);
        ConfiguracionCompleta configuracion = new ConfiguracionCompleta();

        // Act
        await servicio.GuardarAsync(configuracion);

        // Assert
        File.Exists(rutaEnSubdirectorio).Should().BeTrue();
    }

    [Fact]
    public async Task CargarAsync_ArchivoVacio_DevuelveValoresPorDefecto()
    {
        // Arrange
        await File.WriteAllTextAsync(_rutaArchivo, string.Empty);
        ServicioConfiguracionJson servicio = CrearServicio();

        // Act
        ConfiguracionCompleta resultado = await servicio.CargarAsync();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Estacion.IndicativoPropio.Should().BeEmpty();
        resultado.Rig.Puerto.Should().Be(4532);
    }

    [Fact]
    public async Task CargarAsync_JsonInvalido_DevuelveValoresPorDefecto()
    {
        // Arrange
        await File.WriteAllTextAsync(_rutaArchivo, "{ esto no es json válido !!!");
        ServicioConfiguracionJson servicio = CrearServicio();

        // Act
        ConfiguracionCompleta resultado = await servicio.CargarAsync();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Estacion.IndicativoPropio.Should().BeEmpty();
    }

    [Fact]
    public async Task CargarAsync_JsonParcial_RellenaCamposFaltantesConDefecto()
    {
        // Arrange — solo se incluye la sección de estación, el resto debería ser por defecto
        string jsonParcial = """
        {
            "estacion": {
                "indicativoPropio": "W1AW",
                "localizador": "FN31pr"
            }
        }
        """;
        await File.WriteAllTextAsync(_rutaArchivo, jsonParcial);
        ServicioConfiguracionJson servicio = CrearServicio();

        // Act
        ConfiguracionCompleta resultado = await servicio.CargarAsync();

        // Assert
        resultado.Estacion.IndicativoPropio.Should().Be("W1AW");
        resultado.Estacion.Localizador.Should().Be("FN31pr");
        resultado.Estacion.PotenciaMaximaVatios.Should().Be(100); // valor por defecto
        resultado.Rig.Host.Should().Be("localhost"); // valor por defecto
        resultado.Rotador.Puerto.Should().Be(4533); // valor por defecto
    }

    [Fact]
    public async Task GuardarAsync_SobreescribeArchivoExistente()
    {
        // Arrange
        ServicioConfiguracionJson servicio = CrearServicio();
        ConfiguracionCompleta primera = new ConfiguracionCompleta();
        primera.Estacion.IndicativoPropio = "EA1FIRST";
        await servicio.GuardarAsync(primera);

        ConfiguracionCompleta segunda = new ConfiguracionCompleta();
        segunda.Estacion.IndicativoPropio = "EA2SECOND";

        // Act
        await servicio.GuardarAsync(segunda);
        ConfiguracionCompleta resultado = await servicio.CargarAsync();

        // Assert
        resultado.Estacion.IndicativoPropio.Should().Be("EA2SECOND");
    }

    [Fact]
    public async Task GuardarAsync_ConfiguracionNula_LanzaExcepcion()
    {
        // Arrange
        ServicioConfiguracionJson servicio = CrearServicio();

        // Act
        Func<Task> accion = () => servicio.GuardarAsync(null!);

        // Assert
        await accion.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GuardarAsync_GeneraJsonLegibleConIndentacion()
    {
        // Arrange
        ServicioConfiguracionJson servicio = CrearServicio();
        ConfiguracionCompleta configuracion = new ConfiguracionCompleta();
        configuracion.Estacion.IndicativoPropio = "EA4JSON";

        // Act
        await servicio.GuardarAsync(configuracion);
        string contenido = await File.ReadAllTextAsync(_rutaArchivo);

        // Assert
        contenido.Should().Contain("\"indicativoPropio\"");
        contenido.Should().Contain("EA4JSON");
        contenido.Should().Contain("\n"); // JSON indentado tiene saltos de línea
    }

    [Fact]
    public async Task CargarAsync_EnumsSerializadosComoTexto_SeDeserializanCorrectamente()
    {
        // Arrange
        ServicioConfiguracionJson servicio = CrearServicio();
        ConfiguracionCompleta configuracion = new ConfiguracionCompleta();
        configuracion.Estacion.RegionItu = RegionItu.Region3;
        configuracion.Estacion.NivelLicencia = NivelLicencia.Intermedio;
        await servicio.GuardarAsync(configuracion);

        // Act
        ConfiguracionCompleta resultado = await servicio.CargarAsync();

        // Assert
        resultado.Estacion.RegionItu.Should().Be(RegionItu.Region3);
        resultado.Estacion.NivelLicencia.Should().Be(NivelLicencia.Intermedio);

        // Verificar que en el archivo JSON los enums están como texto, no como número
        string contenido = await File.ReadAllTextAsync(_rutaArchivo);
        contenido.Should().Contain("region3");
        contenido.Should().Contain("intermedio");
    }

    [Fact]
    public void Constructor_RutaVacia_LanzaExcepcion()
    {
        // Act
        Action accion = () => new ServicioConfiguracionJson(string.Empty, _logger);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_LoggerNulo_LanzaExcepcion()
    {
        // Act
        Action accion = () => new ServicioConfiguracionJson(_rutaArchivo, null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }
}
