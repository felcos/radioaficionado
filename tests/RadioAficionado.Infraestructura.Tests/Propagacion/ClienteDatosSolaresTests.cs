using System.Net;
using System.Net.Http;
using FluentAssertions;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.Propagacion;
using RadioAficionado.Infraestructura.Propagacion;
using Serilog;

namespace RadioAficionado.Infraestructura.Tests.Propagacion;

public class ClienteDatosSolaresTests
{
    private readonly Mock<ILogger> _loggerMock = new();

    // ─── Parseo JSON de NOAA ─────────────────────────────────────────

    [Fact]
    public void ParsearFlujo10cm_ConJsonValido_RetornaValorCorrecto()
    {
        // Arrange
        string json = """{"flux":101,"time_tag":"2026-05-15T20:00:00"}""";

        // Act
        int resultado = ClienteDatosSolares.ParsearFlujo10cm(json);

        // Assert
        resultado.Should().Be(101);
    }

    [Fact]
    public void ParsearFlujo10cm_ConArrayJson_RetornaValorDelPrimerElemento()
    {
        // Arrange
        string json = """[{"flux":135,"time_tag":"2026-05-15T20:00:00"}]""";

        // Act
        int resultado = ClienteDatosSolares.ParsearFlujo10cm(json);

        // Assert
        resultado.Should().Be(135);
    }

    [Fact]
    public void ParsearFlujo10cm_ConFlujoComoString_ParseaCorrectamente()
    {
        // Arrange
        string json = """{"flux":"108","time_tag":"2026-05-15T20:00:00"}""";

        // Act
        int resultado = ClienteDatosSolares.ParsearFlujo10cm(json);

        // Assert
        resultado.Should().Be(108);
    }

    [Fact]
    public void ParsearVelocidadVientoSolar_ConJsonValido_RetornaVelocidad()
    {
        // Arrange
        string json = """{"proton_speed":639,"time_tag":"2026-05-15T20:00:00"}""";

        // Act
        double resultado = ClienteDatosSolares.ParsearVelocidadVientoSolar(json);

        // Assert
        resultado.Should().Be(639);
    }

    [Fact]
    public void ParsearBt_ConJsonValido_RetornaBt()
    {
        // Arrange
        string json = """{"bt":5.2,"bz_gsm":-1.3,"time_tag":"2026-05-15T20:00:00"}""";

        // Act
        double resultado = ClienteDatosSolares.ParsearBt(json);

        // Assert
        resultado.Should().Be(5.2);
    }

    [Fact]
    public void ParsearBzGsm_ConJsonValido_RetornaBzNegativo()
    {
        // Arrange
        string json = """{"bt":5,"bz_gsm":-1.5,"time_tag":"2026-05-15T20:00:00"}""";

        // Act
        double resultado = ClienteDatosSolares.ParsearBzGsm(json);

        // Assert
        resultado.Should().Be(-1.5);
    }

    [Fact]
    public void ParsearUltimoKp_ConFormatoArrayDeArrays_RetornaUltimoValor()
    {
        // Arrange — formato real NOAA: primer elemento es header, luego datos como arrays
        string json = """
        [
            ["time_tag","Kp","a_running","station_count"],
            ["2026-05-14 00:00:00","1.33","5","8"],
            ["2026-05-14 03:00:00","2.00","7","8"],
            ["2026-05-15 21:00:00","3.67","15","8"]
        ]
        """;

        // Act
        double resultado = ClienteDatosSolares.ParsearUltimoKp(json);

        // Assert
        resultado.Should().Be(3.67);
    }

    [Fact]
    public void ParsearEscalas_ConJsonValido_RetornaEscalasCorrectas()
    {
        // Arrange
        string json = """
        [
            {
                "DateStamp":"2026-05-15",
                "R":{"Scale":"1","MinorProb":"40","MajorProb":"5"},
                "S":{"Scale":"0","Prob":"5"},
                "G":{"Scale":"2","Text":"moderate"}
            }
        ]
        """;

        // Act
        EscalasEspaciales resultado = ClienteDatosSolares.ParsearEscalas(json);

        // Assert
        resultado.EscalaR.Should().Be("1");
        resultado.EscalaS.Should().Be("0");
        resultado.EscalaG.Should().Be("2");
        resultado.ProbRadiacionMenor.Should().Be(40);
        resultado.ProbRadiacionMayor.Should().Be(5);
        resultado.ProbTormentaSolar.Should().Be(5);
    }

    [Fact]
    public void ParsearAlertas_ConArrayDeAlertas_RetornaAlertasParseadas()
    {
        // Arrange
        string json = """
        [
            {
                "product_id":"K05A",
                "issue_datetime":"2026-05-15T12:00:00Z",
                "message":"ALERT: Geomagnetic K-index of 5"
            },
            {
                "product_id":"K06W",
                "issue_datetime":"2026-05-15T13:00:00Z",
                "message":"WARNING: Geomagnetic K-index expected to reach 6"
            }
        ]
        """;

        // Act
        IReadOnlyList<AlertaSolar> resultado = ClienteDatosSolares.ParsearAlertas(json);

        // Assert
        resultado.Should().HaveCount(2);
        resultado[0].Codigo.Should().Be("K05A");
        resultado[0].Mensaje.Should().Contain("K-index of 5");
        resultado[1].Codigo.Should().Be("K06W");
    }

    [Fact]
    public void ParsearAlertas_ConArrayVacio_RetornaListaVacia()
    {
        // Arrange
        string json = "[]";

        // Act
        IReadOnlyList<AlertaSolar> resultado = ClienteDatosSolares.ParsearAlertas(json);

        // Assert
        resultado.Should().BeEmpty();
    }

    // ─── Parseo XML de N0NBH ─────────────────────────────────────────

    [Fact]
    public void ParsearXmlN0nbh_ConXmlCompleto_RetornaIndicesCorrectos()
    {
        // Arrange
        string xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <solar>
            <solardata>
                <solarflux>135</solarflux>
                <aindex>8</aindex>
                <kindex>2</kindex>
                <xray>C1.2</xray>
                <sunspots>120</sunspots>
                <solarwind>450.5</solarwind>
                <protonflux>1.2</protonflux>
                <electflux>0.5</electflux>
                <magneticfield>4.3</magneticfield>
                <geomagfield>quiet</geomagfield>
                <signalnoise>S2</signalnoise>
                <calculatedconditions>
                    <band name="80m-40m" time="day">Good</band>
                    <band name="80m-40m" time="night">Fair</band>
                    <band name="30m-20m" time="day">Excellent</band>
                    <band name="30m-20m" time="night">Good</band>
                </calculatedconditions>
                <calculatedvhfconditions>
                    <phenomenon name="vhf_aurora" location="high_latitudes">Band Closed</phenomenon>
                    <phenomenon name="E-Skip" location="europe">50MHz - MUF</phenomenon>
                    <phenomenon name="E-Skip" location="north_america">Band Closed</phenomenon>
                </calculatedvhfconditions>
            </solardata>
        </solar>
        """;

        // Act
        ClienteDatosSolares.DatosN0nbh resultado = ClienteDatosSolares.ParsearXmlN0nbh(xml);

        // Assert
        resultado.Sfi.Should().Be(135);
        resultado.Ap.Should().Be(8);
        resultado.Kp.Should().Be(2);
        resultado.RayosX.Should().Be("C1.2");
        resultado.NumeroManchasSolares.Should().Be(120);
        resultado.VelocidadVientoSolar.Should().Be(450.5);
        resultado.FlujoProtones.Should().Be(1.2);
        resultado.FlujoElectrones.Should().Be(0.5);
        resultado.CampoGeomagnetico.Should().Be("quiet");
        resultado.RuidoSenal.Should().Be("S2");
    }

    [Fact]
    public void ParsearXmlN0nbh_ConBandasHf_RetornaCondicionesCorrectas()
    {
        // Arrange
        string xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <solar>
            <solardata>
                <solarflux>100</solarflux>
                <aindex>5</aindex>
                <kindex>1</kindex>
                <xray>B5.0</xray>
                <sunspots>80</sunspots>
                <solarwind>400</solarwind>
                <protonflux>0.8</protonflux>
                <electflux>0.3</electflux>
                <magneticfield>3.0</magneticfield>
                <geomagfield>quiet</geomagfield>
                <signalnoise>S1</signalnoise>
                <calculatedconditions>
                    <band name="80m-40m" time="day">Good</band>
                    <band name="80m-40m" time="night">Poor</band>
                    <band name="30m-20m" time="day">Excellent</band>
                    <band name="30m-20m" time="night">Good</band>
                    <band name="17m-15m" time="day">Good</band>
                    <band name="17m-15m" time="night">Fair</band>
                </calculatedconditions>
                <calculatedvhfconditions>
                    <phenomenon name="vhf_aurora" location="high_latitudes">Band Closed</phenomenon>
                    <phenomenon name="E-Skip" location="europe">Band Closed</phenomenon>
                    <phenomenon name="E-Skip" location="north_america">Band Closed</phenomenon>
                </calculatedvhfconditions>
            </solardata>
        </solar>
        """;

        // Act
        ClienteDatosSolares.DatosN0nbh resultado = ClienteDatosSolares.ParsearXmlN0nbh(xml);

        // Assert
        resultado.CondicionesHf.Should().NotBeNull();
        resultado.CondicionesHf!.Should().HaveCount(3);
        resultado.CondicionesHf[0].Banda.Should().Be("80m-40m");
        resultado.CondicionesHf[0].Dia.Should().Be("Good");
        resultado.CondicionesHf[0].Noche.Should().Be("Poor");
        resultado.CondicionesHf[1].Banda.Should().Be("30m-20m");
        resultado.CondicionesHf[1].Dia.Should().Be("Excellent");
    }

    [Fact]
    public void ParsearXmlN0nbh_ConVhf_RetornaCondicionesVhfCorrectas()
    {
        // Arrange
        string xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <solar>
            <solardata>
                <solarflux>100</solarflux>
                <aindex>5</aindex>
                <kindex>1</kindex>
                <xray>B5.0</xray>
                <sunspots>80</sunspots>
                <solarwind>400</solarwind>
                <protonflux>0.8</protonflux>
                <electflux>0.3</electflux>
                <magneticfield>3.0</magneticfield>
                <geomagfield>quiet</geomagfield>
                <signalnoise>S1</signalnoise>
                <calculatedconditions />
                <calculatedvhfconditions>
                    <phenomenon name="vhf_aurora" location="high_latitudes">Aurora Possible</phenomenon>
                    <phenomenon name="E-Skip" location="europe">50MHz - MUF</phenomenon>
                    <phenomenon name="E-Skip" location="north_america">Band Closed</phenomenon>
                </calculatedvhfconditions>
            </solardata>
        </solar>
        """;

        // Act
        ClienteDatosSolares.DatosN0nbh resultado = ClienteDatosSolares.ParsearXmlN0nbh(xml);

        // Assert
        resultado.CondicionesVhf.Should().NotBeNull();
        resultado.CondicionesVhf!.AuroraVhf.Should().Be("Aurora Possible");
        resultado.CondicionesVhf.ESkipEuropa.Should().Be("50MHz - MUF");
        resultado.CondicionesVhf.ESkipNorteamerica.Should().Be("Band Closed");
    }

    // ─── Parseo de históricos ────────────────────────────────────────

    [Fact]
    public void ParsearHistoricoSfi_ConFormatoArrayDeArrays_RetornaPuntosCorrectos()
    {
        // Arrange
        string json = """
        [
            ["time_tag","flux"],
            ["2026-05-01 00:00:00",108],
            ["2026-05-02 00:00:00",112],
            ["2026-05-03 00:00:00",105]
        ]
        """;

        // Act
        IReadOnlyList<PuntoHistoricoSfi> resultado = ClienteDatosSolares.ParsearHistoricoSfi(json);

        // Assert
        resultado.Should().HaveCount(3);
        resultado[0].Sfi.Should().Be(108);
        resultado[1].Sfi.Should().Be(112);
        resultado[2].Sfi.Should().Be(105);
    }

    [Fact]
    public void ParsearHistoricoSfi_ConFormatoObjeto_RetornaPuntosCorrectos()
    {
        // Arrange — formato alternativo con objetos
        string json = """
        [
            {"header": true},
            {"time_tag":"2026-05-10","flux":120},
            {"time_tag":"2026-05-11","flux":115}
        ]
        """;

        // Act
        IReadOnlyList<PuntoHistoricoSfi> resultado = ClienteDatosSolares.ParsearHistoricoSfi(json);

        // Assert
        resultado.Should().HaveCount(2);
        resultado[0].Sfi.Should().Be(120);
        resultado[1].Sfi.Should().Be(115);
    }

    [Fact]
    public void ParsearHistoricoKp_ConFormatoArrayDeArrays_RetornaPuntosCorrectos()
    {
        // Arrange
        string json = """
        [
            ["time_tag","Kp","a_running","station_count"],
            ["2026-05-14 00:00:00","1.33","5","8"],
            ["2026-05-14 03:00:00","2.67","9","8"],
            ["2026-05-14 06:00:00","3.00","15","8"]
        ]
        """;

        // Act
        IReadOnlyList<PuntoHistoricoKp> resultado = ClienteDatosSolares.ParsearHistoricoKp(json);

        // Assert
        resultado.Should().HaveCount(3);
        resultado[0].Kp.Should().Be(1.33);
        resultado[1].Kp.Should().Be(2.67);
        resultado[2].Kp.Should().Be(3.00);
    }

    // ─── Caché ───────────────────────────────────────────────────────

    [Fact]
    public async Task ObtenerHistoricoSfiAsync_SegundaLlamadaDentroDelTtl_RetornaDatosDesdeCache()
    {
        // Arrange
        string jsonHistorico = """
        [
            ["time_tag","flux"],
            ["2026-05-01 00:00:00",100]
        ]
        """;

        HttpMessageHandler handler = CrearHandlerConRespuestas(new Dictionary<string, string>
        {
            { "10cm-flux-30-day", jsonHistorico }
        });

        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://services.swpc.noaa.gov") };
        ClienteDatosSolares cliente = new(httpClient, _loggerMock.Object);

        // Act — primera llamada (va al servidor)
        IReadOnlyList<PuntoHistoricoSfi> resultado1 = await cliente.ObtenerHistoricoSfiAsync();

        // Segunda llamada (debería venir de caché)
        IReadOnlyList<PuntoHistoricoSfi> resultado2 = await cliente.ObtenerHistoricoSfiAsync();

        // Assert
        resultado1.Should().HaveCount(1);
        resultado2.Should().HaveCount(1);
        resultado1.Should().BeSameAs(resultado2);
    }

    [Fact]
    public async Task ObtenerHistoricoKpAsync_CuandoFallaElServidor_RetornaListaVacia()
    {
        // Arrange
        HttpMessageHandler handler = CrearHandlerQueDevuelveError();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://services.swpc.noaa.gov") };
        ClienteDatosSolares cliente = new(httpClient, _loggerMock.Object);

        // Act
        IReadOnlyList<PuntoHistoricoKp> resultado = await cliente.ObtenerHistoricoKpAsync();

        // Assert
        resultado.Should().BeEmpty();
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private static HttpMessageHandler CrearHandlerConRespuestas(Dictionary<string, string> respuestasPorUrlParcial)
    {
        FakeHandler fakeHandler = new(respuestasPorUrlParcial);
        return fakeHandler;
    }

    private static HttpMessageHandler CrearHandlerQueDevuelveError()
    {
        FakeHandler fakeHandler = new(new Dictionary<string, string>(), devuelveError: true);
        return fakeHandler;
    }

    /// <summary>
    /// Handler HTTP falso para tests que intercepta peticiones y devuelve respuestas predefinidas.
    /// </summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _respuestas;
        private readonly bool _devuelveError;

        public FakeHandler(Dictionary<string, string> respuestas, bool devuelveError = false)
        {
            _respuestas = respuestas;
            _devuelveError = devuelveError;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_devuelveError)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            string url = request.RequestUri?.ToString() ?? "";

            foreach (KeyValuePair<string, string> par in _respuestas)
            {
                if (url.Contains(par.Key, StringComparison.OrdinalIgnoreCase))
                {
                    HttpResponseMessage respuesta = new(HttpStatusCode.OK)
                    {
                        Content = new StringContent(par.Value, System.Text.Encoding.UTF8, "application/json")
                    };
                    return Task.FromResult(respuesta);
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
