using FluentAssertions;
using RadioAficionado.Dominio.Compliance;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Infraestructura.Compliance;

namespace RadioAficionado.Infraestructura.Tests.Compliance;

/// <summary>
/// Tests unitarios para <see cref="ServicioCompliance"/>.
/// Valida la verificación de compliance regulatorio para distintas combinaciones
/// de frecuencia, modo, región ITU y nivel de licencia.
/// </summary>
public class ServicioComplianceTests
{
    private readonly IServicioCompliance _servicio = new ServicioCompliance();

    // =============================================
    // Helpers para crear licencias de prueba
    // =============================================

    private static LicenciaOperador CrearLicencia(
        NivelLicencia nivel,
        RegionItu region,
        string indicativo = "EA4ABC",
        string pais = "ES",
        double potenciaMaxima = 1500.0)
    {
        return new LicenciaOperador(
            new Indicativo(indicativo),
            pais,
            nivel,
            region,
            potenciaMaxima);
    }

    // =============================================
    // Verificar Transmisión — Casos Permitidos
    // =============================================

    [Fact]
    public void Verificar_FrecuenciaEnBanda20mCw_Permitido()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.025);
        ModoOperacion modo = ModoOperacion.CW;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region1);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeTrue();
        resultado.Violacion.Should().Be(TipoViolacion.Ninguna);
    }

    [Fact]
    public void Verificar_FrecuenciaEnBanda20mSsb_Permitido()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.200);
        ModoOperacion modo = ModoOperacion.SSB;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region1);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeTrue();
    }

    [Fact]
    public void Verificar_Ft8EnSegmentoDigital20mRegion2_Permitido()
    {
        // Arrange — 14.074 es la frecuencia estándar de FT8 en 20m
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.074);
        ModoOperacion modo = ModoOperacion.FT8;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region2, "W1AW", "US");

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeTrue();
    }

    [Fact]
    public void Verificar_CwEnBanda40mRegion3_Permitido()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(7.010);
        ModoOperacion modo = ModoOperacion.CW;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region3, "JA1ABC", "JP");

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeTrue();
    }

    [Fact]
    public void Verificar_LicenciaAvanzada_SegmentoAvanzado20mRegion2_Permitido()
    {
        // Arrange — 14.010 MHz requiere Avanzado en Region 2
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.010);
        ModoOperacion modo = ModoOperacion.CW;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region2, "W1AW", "US");

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeTrue();
    }

    [Fact]
    public void Verificar_SsbEnBanda15mRegion1_Permitido()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(21.300);
        ModoOperacion modo = ModoOperacion.SSB;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region1);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeTrue();
    }

    [Fact]
    public void Verificar_CwEnBanda80mRegion1_Permitido()
    {
        // Arrange — 3.565 MHz está en el segmento CW 3.560-3.570 (NivelBasico)
        Frecuencia frecuencia = Frecuencia.DesdeMHz(3.565);
        ModoOperacion modo = ModoOperacion.CW;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region1);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeTrue();
    }

    [Fact]
    public void Verificar_CwEnBanda10mRegion2_Permitido()
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(28.050);
        ModoOperacion modo = ModoOperacion.CW;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region2, "LU1ABC", "AR");

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeTrue();
    }

    // =============================================
    // Verificar Transmisión — Fuera de banda
    // =============================================

    [Fact]
    public void Verificar_FrecuenciaFueraDeBandas_NoPermitido()
    {
        // Arrange — 10 MHz no es banda de aficionados
        Frecuencia frecuencia = Frecuencia.DesdeMHz(10.0);
        ModoOperacion modo = ModoOperacion.CW;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region1);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.FueraDeBanda);
    }

    [Fact]
    public void Verificar_FrecuenciaEnBandaComercial_NoPermitido()
    {
        // Arrange — 5.0 MHz es frecuencia comercial
        Frecuencia frecuencia = Frecuencia.DesdeMHz(5.0);
        ModoOperacion modo = ModoOperacion.SSB;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region2, "W1AW", "US");

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.FueraDeBanda);
    }

    // =============================================
    // Verificar Transmisión — Modo no permitido
    // =============================================

    [Fact]
    public void Verificar_SsbEnSegmentoCw_NoPermitido()
    {
        // Arrange — 14.025 MHz es segmento solo CW en Region 1
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.025);
        ModoOperacion modo = ModoOperacion.SSB;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region1);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.ModoNoPermitido);
    }

    [Fact]
    public void Verificar_FmEnSegmentoCw40m_NoPermitido()
    {
        // Arrange — 7.010 MHz es segmento CW en todas las regiones
        Frecuencia frecuencia = Frecuencia.DesdeMHz(7.010);
        ModoOperacion modo = ModoOperacion.FM;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region1);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.ModoNoPermitido);
    }

    [Fact]
    public void Verificar_SsbEnSegmentoCw80mRegion2_NoPermitido()
    {
        // Arrange — 3.510 es CW en Region 2
        Frecuencia frecuencia = Frecuencia.DesdeMHz(3.510);
        ModoOperacion modo = ModoOperacion.SSB;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region2, "W1AW", "US");

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.ModoNoPermitido);
    }

    // =============================================
    // Verificar Transmisión — Licencia insuficiente
    // =============================================

    [Fact]
    public void Verificar_LicenciaNovato_BandaRestringida_NoPermitido()
    {
        // Arrange — 14.010 MHz requiere Intermedio en Region 1
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.010);
        ModoOperacion modo = ModoOperacion.CW;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region1);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.LicenciaInsuficiente);
    }

    [Fact]
    public void Verificar_LicenciaBasica_Segmento160mAvanzado_NoPermitido()
    {
        // Arrange — 160m requiere Avanzado en los segmentos iniciales
        Frecuencia frecuencia = Frecuencia.DesdeMHz(1.820);
        ModoOperacion modo = ModoOperacion.CW;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region1);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.LicenciaInsuficiente);
    }

    [Fact]
    public void Verificar_LicenciaBasica_SegmentoAvanzado20mRegion2_NoPermitido()
    {
        // Arrange — 14.010 requiere Avanzado en Region 2
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.010);
        ModoOperacion modo = ModoOperacion.CW;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region2, "W1AW", "US");

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.LicenciaInsuficiente);
    }

    // =============================================
    // Verificar Transmisión — Potencia excedida
    // =============================================

    [Fact]
    public void Verificar_PotenciaExcedeLimiteLicencia_NoPermitido()
    {
        // Arrange — Licencia con máximo 100W, intenta transmitir con 500W
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.200);
        ModoOperacion modo = ModoOperacion.SSB;
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region1, potenciaMaxima: 100.0);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia, potenciaVatios: 500.0);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.PotenciaExcedida);
    }

    // =============================================
    // ObtenerPlanDeBanda
    // =============================================

    [Fact]
    public void ObtenerPlanDeBanda_Region1_20m_TieneSegmentos()
    {
        // Act
        PlanDeBanda? plan = _servicio.ObtenerPlanDeBanda(BandaRadio.Banda20m, RegionItu.Region1);

        // Assert
        plan.Should().NotBeNull();
        plan!.Segmentos.Should().NotBeEmpty();
        plan.Banda.Should().Be(BandaRadio.Banda20m);
        plan.Region.Should().Be(RegionItu.Region1);
    }

    [Fact]
    public void ObtenerPlanDeBanda_Region2_40m_TieneSegmentos()
    {
        // Act
        PlanDeBanda? plan = _servicio.ObtenerPlanDeBanda(BandaRadio.Banda40m, RegionItu.Region2);

        // Assert
        plan.Should().NotBeNull();
        plan!.Segmentos.Should().NotBeEmpty();
        plan.Banda.Should().Be(BandaRadio.Banda40m);
        plan.Region.Should().Be(RegionItu.Region2);
    }

    [Fact]
    public void ObtenerPlanDeBanda_Region3_80m_TieneSegmentos()
    {
        // Act
        PlanDeBanda? plan = _servicio.ObtenerPlanDeBanda(BandaRadio.Banda80m, RegionItu.Region3);

        // Assert
        plan.Should().NotBeNull();
        plan!.Segmentos.Should().NotBeEmpty();
        plan.Region.Should().Be(RegionItu.Region3);
    }

    [Fact]
    public void ObtenerPlanDeBanda_Region1_160m_TieneSegmentos()
    {
        // Act
        PlanDeBanda? plan = _servicio.ObtenerPlanDeBanda(BandaRadio.Banda160m, RegionItu.Region1);

        // Assert
        plan.Should().NotBeNull();
        plan!.Segmentos.Should().NotBeEmpty();
    }

    [Fact]
    public void ObtenerPlanDeBanda_Region2_15m_TieneSegmentos()
    {
        // Act
        PlanDeBanda? plan = _servicio.ObtenerPlanDeBanda(BandaRadio.Banda15m, RegionItu.Region2);

        // Assert
        plan.Should().NotBeNull();
        plan!.Segmentos.Should().NotBeEmpty();
    }

    [Fact]
    public void ObtenerPlanDeBanda_Region3_10m_TieneSegmentos()
    {
        // Act
        PlanDeBanda? plan = _servicio.ObtenerPlanDeBanda(BandaRadio.Banda10m, RegionItu.Region3);

        // Assert
        plan.Should().NotBeNull();
        plan!.Segmentos.Should().NotBeEmpty();
    }

    [Fact]
    public void ObtenerPlanDeBanda_BandaSinPlan_RetornaNull()
    {
        // Arrange — Banda 6m no tiene plan definido aún en PlanDeBandaItu
        // Act
        PlanDeBanda? plan = _servicio.ObtenerPlanDeBanda(BandaRadio.Banda6m, RegionItu.Region1);

        // Assert
        plan.Should().BeNull();
    }

    // =============================================
    // EstaCercaDelBordeDeBanda
    // =============================================

    [Fact]
    public void EstaCercaDelBordeDeBanda_FrecuenciaCercaDelBorde_RetornaTrue()
    {
        // Arrange — 14.000500 MHz está a 500 Hz del inicio de banda 20m
        Frecuencia frecuencia = Frecuencia.DesdeHz(14_000_500);
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region1);

        // Act
        bool resultado = _servicio.EstaCercaDelBordeDeBanda(frecuencia, licencia, margenHz: 1000);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    public void EstaCercaDelBordeDeBanda_FrecuenciaCentral_RetornaFalse()
    {
        // Arrange — 14.200 MHz está lejos de cualquier borde
        Frecuencia frecuencia = Frecuencia.DesdeMHz(14.200);
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Basico, RegionItu.Region1);

        // Act
        bool resultado = _servicio.EstaCercaDelBordeDeBanda(frecuencia, licencia, margenHz: 1000);

        // Assert
        resultado.Should().BeFalse();
    }

    [Fact]
    public void EstaCercaDelBordeDeBanda_FrecuenciaFueraDeBanda_RetornaFalse()
    {
        // Arrange — 10 MHz no es banda de aficionados
        Frecuencia frecuencia = Frecuencia.DesdeMHz(10.0);
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region1);

        // Act
        bool resultado = _servicio.EstaCercaDelBordeDeBanda(frecuencia, licencia);

        // Assert
        resultado.Should().BeFalse();
    }

    // =============================================
    // ObtenerSegmentosPermitidos (vía casting)
    // =============================================

    [Fact]
    public void ObtenerSegmentosPermitidos_LicenciaAvanzada_IncluyeTodosLosSegmentos()
    {
        // Arrange
        LicenciaOperador licencia = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region1);
        ServicioCompliance servicioConcreto = new();

        // Act
        IReadOnlyList<SegmentoBanda> segmentos = servicioConcreto.ObtenerSegmentosPermitidos(licencia);

        // Assert
        segmentos.Should().NotBeEmpty();

        // Un operador avanzado debe tener acceso a todos los segmentos de todas las bandas de su región
        IReadOnlyList<PlanDeBanda> todosLosPlanes = PlanDeBandaItu.ObtenerPlanesPorRegion(RegionItu.Region1);
        int totalSegmentos = todosLosPlanes.Sum(p => p.Segmentos.Count);
        segmentos.Should().HaveCount(totalSegmentos);
    }

    [Fact]
    public void ObtenerSegmentosPermitidos_LicenciaBasica_MenosSegmentosQueAvanzada()
    {
        // Arrange
        LicenciaOperador licenciaBasica = CrearLicencia(NivelLicencia.Basico, RegionItu.Region2, "W1AW", "US");
        LicenciaOperador licenciaAvanzada = CrearLicencia(NivelLicencia.Avanzado, RegionItu.Region2, "W1AW", "US");
        ServicioCompliance servicioConcreto = new();

        // Act
        IReadOnlyList<SegmentoBanda> segmentosBasica = servicioConcreto.ObtenerSegmentosPermitidos(licenciaBasica);
        IReadOnlyList<SegmentoBanda> segmentosAvanzada = servicioConcreto.ObtenerSegmentosPermitidos(licenciaAvanzada);

        // Assert
        segmentosBasica.Count.Should().BeLessThan(segmentosAvanzada.Count);
    }

    // =============================================
    // Tests de integración entre componentes
    // =============================================

    [Theory]
    [InlineData(14.074, "FT8", RegionItu.Region1, NivelLicencia.Basico, true)]
    [InlineData(14.074, "FT8", RegionItu.Region2, NivelLicencia.Basico, true)]
    [InlineData(14.074, "FT8", RegionItu.Region3, NivelLicencia.Basico, true)]
    [InlineData(7.010, "CW", RegionItu.Region1, NivelLicencia.Basico, true)]
    [InlineData(7.010, "CW", RegionItu.Region2, NivelLicencia.Avanzado, true)]
    [InlineData(7.010, "SSB", RegionItu.Region1, NivelLicencia.Avanzado, false)]
    [InlineData(21.300, "SSB", RegionItu.Region1, NivelLicencia.Basico, true)]
    [InlineData(28.500, "SSB", RegionItu.Region2, NivelLicencia.Basico, true)]
    public void Verificar_MultiplesCombinaciones_ResultadoEsperado(
        double frecuenciaMhz,
        string modoTexto,
        RegionItu region,
        NivelLicencia nivel,
        bool debeSerPermitido)
    {
        // Arrange
        Frecuencia frecuencia = Frecuencia.DesdeMHz(frecuenciaMhz);
        ModoOperacionExtensiones.IntentarDesdeAdif(modoTexto, out ModoOperacion modo);
        string indicativo = region == RegionItu.Region2 ? "W1AW" : "EA4ABC";
        string pais = region == RegionItu.Region2 ? "US" : "ES";
        LicenciaOperador licencia = CrearLicencia(nivel, region, indicativo, pais);

        // Act
        ResultadoCompliance resultado = _servicio.Verificar(frecuencia, modo, licencia);

        // Assert
        resultado.EsPermitido.Should().Be(
            debeSerPermitido,
            $"Frecuencia {frecuenciaMhz} MHz, modo {modoTexto}, región {region}, nivel {nivel}");
    }
}
