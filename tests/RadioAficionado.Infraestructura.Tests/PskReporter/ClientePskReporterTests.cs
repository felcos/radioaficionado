using System.Xml.Linq;
using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.PskReporter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RadioAficionado.Infraestructura.Tests.PskReporter;

/// <summary>
/// Tests unitarios para <see cref="ClientePskReporter"/>.
/// Valida la generación de XML, el parseo de respuestas JSON y el comportamiento del buffer.
/// </summary>
public sealed class ClientePskReporterTests : IDisposable
{
    private readonly ConfiguracionPskReporter _configuracion;
    private readonly ClientePskReporter _cliente;

    public ClientePskReporterTests()
    {
        _configuracion = new ConfiguracionPskReporter
        {
            IndicativoPropio = "EA4ABC",
            Localizador = "IN80",
            SoftwareId = "RadioAficionado",
            VersionSoftware = "1.0",
            IntervaloEnvioSegundos = 300
        };

        ILogger<ClientePskReporter> logger = NullLogger<ClientePskReporter>.Instance;
        HttpClient clienteHttp = new();
        _cliente = new ClientePskReporter(logger, clienteHttp, _configuracion);
    }

    public void Dispose()
    {
        _cliente.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Fact]
    public void GenerarXml_SpotUnico_FormatoValido()
    {
        // Arrange
        List<SpotPsk> spots = new()
        {
            new SpotPsk(
                new Indicativo("EA4ABC"),
                new Indicativo("JA1XYZ"),
                Frecuencia.DesdeHz(14_076_000),
                ModoOperacion.FT8,
                -15,
                new Localizador("IN80"),
                new Localizador("PM95"),
                new DateTime(2026, 3, 23, 12, 30, 0, DateTimeKind.Utc))
        };

        // Act
        string xml = _cliente.GenerarXml(spots);

        // Assert
        XDocument documento = XDocument.Parse(xml);
        XElement? raiz = documento.Root;

        raiz.Should().NotBeNull();
        raiz!.Name.LocalName.Should().Be("receptionReports");
        raiz.Element("receiverCallsign")!.Attribute("value")!.Value.Should().Be("EA4ABC");
        raiz.Element("receiverLocator")!.Attribute("value")!.Value.Should().Be("IN80");
        raiz.Element("receiverDecoderSoftware")!.Attribute("value")!.Value.Should().Be("RadioAficionado 1.0");

        XElement? reporte = raiz.Element("receptionReport");
        reporte.Should().NotBeNull();
        reporte!.Attribute("senderCallsign")!.Value.Should().Be("JA1XYZ");
        reporte.Attribute("frequency")!.Value.Should().Be("14076000");
        reporte.Attribute("mode")!.Value.Should().Be("FT8");
        reporte.Attribute("sNR")!.Value.Should().Be("-15");
        reporte.Attribute("senderLocator")!.Value.Should().Be("PM95");
    }

    [Fact]
    public void GenerarXml_MultiplesSpots_TodosIncluidos()
    {
        // Arrange
        List<SpotPsk> spots = new()
        {
            new SpotPsk(
                new Indicativo("EA4ABC"),
                new Indicativo("JA1XYZ"),
                Frecuencia.DesdeHz(14_076_000),
                ModoOperacion.FT8,
                -15,
                new Localizador("IN80"),
                new Localizador("PM95"),
                new DateTime(2026, 3, 23, 12, 30, 0, DateTimeKind.Utc)),
            new SpotPsk(
                new Indicativo("EA4ABC"),
                new Indicativo("W1AW"),
                Frecuencia.DesdeHz(7_074_000),
                ModoOperacion.FT4,
                5,
                new Localizador("IN80"),
                new Localizador("FN31"),
                new DateTime(2026, 3, 23, 12, 31, 0, DateTimeKind.Utc)),
            new SpotPsk(
                new Indicativo("EA4ABC"),
                new Indicativo("VK2ABC"),
                Frecuencia.DesdeHz(21_076_000),
                ModoOperacion.FT8,
                -22,
                new Localizador("IN80"),
                null,
                new DateTime(2026, 3, 23, 12, 32, 0, DateTimeKind.Utc))
        };

        // Act
        string xml = _cliente.GenerarXml(spots);

        // Assert
        XDocument documento = XDocument.Parse(xml);
        IReadOnlyList<XElement> reportes = documento.Root!.Elements("receptionReport").ToList();

        reportes.Should().HaveCount(3);
        reportes[0].Attribute("senderCallsign")!.Value.Should().Be("JA1XYZ");
        reportes[1].Attribute("senderCallsign")!.Value.Should().Be("W1AW");
        reportes[2].Attribute("senderCallsign")!.Value.Should().Be("VK2ABC");
    }

    [Fact]
    public void GenerarXml_ConSnrNegativo_FormateaCorrectamente()
    {
        // Arrange
        List<SpotPsk> spots = new()
        {
            new SpotPsk(
                new Indicativo("EA4ABC"),
                new Indicativo("JA1XYZ"),
                Frecuencia.DesdeHz(14_076_000),
                ModoOperacion.FT8,
                -24,
                new Localizador("IN80"),
                null,
                new DateTime(2026, 3, 23, 12, 30, 0, DateTimeKind.Utc))
        };

        // Act
        string xml = _cliente.GenerarXml(spots);

        // Assert
        XDocument documento = XDocument.Parse(xml);
        XElement? reporte = documento.Root!.Element("receptionReport");

        reporte!.Attribute("sNR")!.Value.Should().Be("-24");
    }

    [Fact]
    public void GenerarXml_SinLocalizador_OmiteCampo()
    {
        // Arrange
        List<SpotPsk> spots = new()
        {
            new SpotPsk(
                new Indicativo("EA4ABC"),
                new Indicativo("JA1XYZ"),
                Frecuencia.DesdeHz(14_076_000),
                ModoOperacion.FT8,
                -10,
                null,
                null,
                new DateTime(2026, 3, 23, 12, 30, 0, DateTimeKind.Utc))
        };

        // Act
        string xml = _cliente.GenerarXml(spots);

        // Assert
        XDocument documento = XDocument.Parse(xml);
        XElement? reporte = documento.Root!.Element("receptionReport");

        reporte!.Attribute("senderLocator").Should().BeNull();
    }

    [Fact]
    public async Task GenerarXml_SinLocalizadorReceptor_OmiteCampoEnCabecera()
    {
        // Arrange
        ConfiguracionPskReporter configuracionSinLoc = new()
        {
            IndicativoPropio = "EA4ABC",
            Localizador = "",
            SoftwareId = "RadioAficionado",
            VersionSoftware = "1.0"
        };

        ILogger<ClientePskReporter> logger = NullLogger<ClientePskReporter>.Instance;
        using HttpClient clienteHttp = new();
        ClientePskReporter clienteSinLoc = new(logger, clienteHttp, configuracionSinLoc);

        List<SpotPsk> spots = new()
        {
            new SpotPsk(
                new Indicativo("EA4ABC"),
                new Indicativo("JA1XYZ"),
                Frecuencia.DesdeHz(14_076_000),
                ModoOperacion.FT8,
                -10,
                null,
                null,
                new DateTime(2026, 3, 23, 12, 30, 0, DateTimeKind.Utc))
        };

        // Act
        string xml = clienteSinLoc.GenerarXml(spots);

        // Assert
        XDocument documento = XDocument.Parse(xml);
        documento.Root!.Element("receiverLocator").Should().BeNull();

        await clienteSinLoc.DisposeAsync();
    }

    [Fact]
    public void GenerarXml_ConSnrPositivo_FormateaCorrectamente()
    {
        // Arrange
        List<SpotPsk> spots = new()
        {
            new SpotPsk(
                new Indicativo("EA4ABC"),
                new Indicativo("W1AW"),
                Frecuencia.DesdeHz(7_074_000),
                ModoOperacion.FT4,
                12,
                new Localizador("IN80"),
                new Localizador("FN31"),
                new DateTime(2026, 3, 23, 12, 30, 0, DateTimeKind.Utc))
        };

        // Act
        string xml = _cliente.GenerarXml(spots);

        // Assert
        XDocument documento = XDocument.Parse(xml);
        XElement? reporte = documento.Root!.Element("receptionReport");

        reporte!.Attribute("sNR")!.Value.Should().Be("12");
    }

    [Fact]
    public void GenerarXml_ConSnrCero_FormateaCorrectamente()
    {
        // Arrange
        List<SpotPsk> spots = new()
        {
            new SpotPsk(
                new Indicativo("EA4ABC"),
                new Indicativo("W1AW"),
                Frecuencia.DesdeHz(7_074_000),
                ModoOperacion.FT8,
                0,
                null,
                null,
                new DateTime(2026, 3, 23, 12, 30, 0, DateTimeKind.Utc))
        };

        // Act
        string xml = _cliente.GenerarXml(spots);

        // Assert
        XDocument documento = XDocument.Parse(xml);
        XElement? reporte = documento.Root!.Element("receptionReport");

        reporte!.Attribute("sNR")!.Value.Should().Be("0");
    }

    [Fact]
    public void ParsearRespuestaJson_ConSpotsValidos_DevuelveListaCorrecta()
    {
        // Arrange
        long horaUnix = new DateTimeOffset(2026, 3, 23, 12, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        string json = $$"""
        {
            "receptionReport": [
                {
                    "receiverCallsign": "EA4ABC",
                    "senderCallsign": "JA1XYZ",
                    "frequency": 14076000,
                    "mode": "FT8",
                    "sNR": -15,
                    "receiverLocator": "IN80",
                    "senderLocator": "PM95",
                    "flowStartSeconds": {{horaUnix}}
                }
            ]
        }
        """;

        // Act
        IReadOnlyList<SpotPsk> spots = _cliente.ParsearRespuestaJson(json);

        // Assert
        spots.Should().HaveCount(1);
        spots[0].Receptor.Valor.Should().Be("EA4ABC");
        spots[0].Transmisor.Valor.Should().Be("JA1XYZ");
        spots[0].Frecuencia.Hz.Should().Be(14_076_000);
        spots[0].Modo.Should().Be(ModoOperacion.FT8);
        spots[0].Snr.Should().Be(-15);
        spots[0].LocalizadorReceptor!.Value.Valor.Should().Be("IN80");
        spots[0].LocalizadorTransmisor!.Value.Valor.Should().Be("PM95");
    }

    [Fact]
    public void ParsearRespuestaJson_ConJsonVacio_DevuelveListaVacia()
    {
        // Arrange
        string json = "";

        // Act
        IReadOnlyList<SpotPsk> spots = _cliente.ParsearRespuestaJson(json);

        // Assert
        spots.Should().BeEmpty();
    }

    [Fact]
    public void ParsearRespuestaJson_SinReceptionReport_DevuelveListaVacia()
    {
        // Arrange
        string json = """
        {
            "otherProperty": "value"
        }
        """;

        // Act
        IReadOnlyList<SpotPsk> spots = _cliente.ParsearRespuestaJson(json);

        // Assert
        spots.Should().BeEmpty();
    }

    [Fact]
    public void ParsearRespuestaJson_ConModoDesconocido_IgnoraSpot()
    {
        // Arrange
        string json = """
        {
            "receptionReport": [
                {
                    "receiverCallsign": "EA4ABC",
                    "senderCallsign": "JA1XYZ",
                    "frequency": 14076000,
                    "mode": "MODO_INEXISTENTE",
                    "sNR": -15,
                    "flowStartSeconds": 1711193400
                }
            ]
        }
        """;

        // Act
        IReadOnlyList<SpotPsk> spots = _cliente.ParsearRespuestaJson(json);

        // Assert
        spots.Should().BeEmpty();
    }

    [Fact]
    public void ParsearRespuestaJson_ConFrecuenciaComoString_ParseaCorrectamente()
    {
        // Arrange
        long horaUnix = new DateTimeOffset(2026, 3, 23, 12, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        string json = $$"""
        {
            "receptionReport": [
                {
                    "receiverCallsign": "EA4ABC",
                    "senderCallsign": "W1AW",
                    "frequency": "7074000",
                    "mode": "FT4",
                    "sNR": "5",
                    "flowStartSeconds": "{{horaUnix}}"
                }
            ]
        }
        """;

        // Act
        IReadOnlyList<SpotPsk> spots = _cliente.ParsearRespuestaJson(json);

        // Assert
        spots.Should().HaveCount(1);
        spots[0].Frecuencia.Hz.Should().Be(7_074_000);
        spots[0].Snr.Should().Be(5);
    }

    [Fact]
    public void ParsearRespuestaJson_SinLocalizadores_DevuelveNullEnLocalizadores()
    {
        // Arrange
        long horaUnix = new DateTimeOffset(2026, 3, 23, 12, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        string json = $$"""
        {
            "receptionReport": [
                {
                    "receiverCallsign": "EA4ABC",
                    "senderCallsign": "JA1XYZ",
                    "frequency": 14076000,
                    "mode": "FT8",
                    "sNR": -10,
                    "flowStartSeconds": {{horaUnix}}
                }
            ]
        }
        """;

        // Act
        IReadOnlyList<SpotPsk> spots = _cliente.ParsearRespuestaJson(json);

        // Assert
        spots.Should().HaveCount(1);
        spots[0].LocalizadorReceptor.Should().BeNull();
        spots[0].LocalizadorTransmisor.Should().BeNull();
    }

    [Fact]
    public void ParsearRespuestaJson_MultiplesSpots_DevuelveTodos()
    {
        // Arrange
        long horaUnix1 = new DateTimeOffset(2026, 3, 23, 12, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        long horaUnix2 = new DateTimeOffset(2026, 3, 23, 12, 31, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        string json = $$"""
        {
            "receptionReport": [
                {
                    "receiverCallsign": "EA4ABC",
                    "senderCallsign": "JA1XYZ",
                    "frequency": 14076000,
                    "mode": "FT8",
                    "sNR": -15,
                    "flowStartSeconds": {{horaUnix1}}
                },
                {
                    "receiverCallsign": "EA4ABC",
                    "senderCallsign": "W1AW",
                    "frequency": 7074000,
                    "mode": "FT4",
                    "sNR": 5,
                    "flowStartSeconds": {{horaUnix2}}
                }
            ]
        }
        """;

        // Act
        IReadOnlyList<SpotPsk> spots = _cliente.ParsearRespuestaJson(json);

        // Assert
        spots.Should().HaveCount(2);
        spots[0].Transmisor.Valor.Should().Be("JA1XYZ");
        spots[1].Transmisor.Valor.Should().Be("W1AW");
    }

    [Fact]
    public void GenerarXml_ListaVacia_GeneraXmlSinReportes()
    {
        // Arrange
        List<SpotPsk> spots = new();

        // Act
        string xml = _cliente.GenerarXml(spots);

        // Assert
        XDocument documento = XDocument.Parse(xml);
        documento.Root!.Elements("receptionReport").Should().BeEmpty();
        documento.Root.Element("receiverCallsign")!.Attribute("value")!.Value.Should().Be("EA4ABC");
    }

    [Fact]
    public void GenerarXml_FrecuenciaAlta_FormateaSinSeparadores()
    {
        // Arrange
        List<SpotPsk> spots = new()
        {
            new SpotPsk(
                new Indicativo("EA4ABC"),
                new Indicativo("JA1XYZ"),
                Frecuencia.DesdeHz(144_174_000),
                ModoOperacion.FT8,
                -5,
                null,
                null,
                new DateTime(2026, 3, 23, 12, 30, 0, DateTimeKind.Utc))
        };

        // Act
        string xml = _cliente.GenerarXml(spots);

        // Assert
        XDocument documento = XDocument.Parse(xml);
        XElement? reporte = documento.Root!.Element("receptionReport");

        reporte!.Attribute("frequency")!.Value.Should().Be("144174000");
    }
}
