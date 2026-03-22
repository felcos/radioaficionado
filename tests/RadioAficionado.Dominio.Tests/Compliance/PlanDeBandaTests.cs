using FluentAssertions;
using RadioAficionado.Dominio.Compliance;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.Compliance;

/// <summary>
/// Tests unitarios para <see cref="SegmentoBanda"/>, <see cref="PlanDeBanda"/> y <see cref="ResultadoCompliance"/>.
/// Valida la contención de frecuencia, permisos de modo, y verificación de compliance
/// incluyendo operaciones permitidas, fuera de banda, licencia insuficiente,
/// modo no permitido y potencia excedida.
/// </summary>
public class PlanDeBandaTests
{
    [Fact]
    public void SegmentoBanda_ContieneFrecuencia_DentroDeRango_DevuelveTrue()
    {
        // Arrange
        SegmentoBanda segmento = new SegmentoBanda(
            Frecuencia.DesdeMHz(14.0),
            Frecuencia.DesdeMHz(14.07),
            TipoSegmento.CwYDigitalEstrecho);

        // Act
        bool contiene = segmento.ContieneFrecuencia(Frecuencia.DesdeMHz(14.035));

        // Assert
        contiene.Should().BeTrue();
    }

    [Fact]
    public void SegmentoBanda_ContieneFrecuencia_FueraDeRango_DevuelveFalse()
    {
        // Arrange
        SegmentoBanda segmento = new SegmentoBanda(
            Frecuencia.DesdeMHz(14.0),
            Frecuencia.DesdeMHz(14.07),
            TipoSegmento.CwYDigitalEstrecho);

        // Act
        bool contiene = segmento.ContieneFrecuencia(Frecuencia.DesdeMHz(14.2));

        // Assert
        contiene.Should().BeFalse();
    }

    [Fact]
    public void SegmentoBanda_ModoPermitido_CwEnSegmentoCw_DevuelveTrue()
    {
        // Arrange
        SegmentoBanda segmento = new SegmentoBanda(
            Frecuencia.DesdeMHz(14.0),
            Frecuencia.DesdeMHz(14.07),
            TipoSegmento.CwYDigitalEstrecho);

        // Act
        bool permitido = segmento.ModoPermitido(ModoOperacion.CW);

        // Assert
        permitido.Should().BeTrue();
    }

    [Fact]
    public void SegmentoBanda_ModoPermitido_SsbEnSegmentoCw_DevuelveFalse()
    {
        // Arrange
        SegmentoBanda segmento = new SegmentoBanda(
            Frecuencia.DesdeMHz(14.0),
            Frecuencia.DesdeMHz(14.07),
            TipoSegmento.CwYDigitalEstrecho);

        // Act
        bool permitido = segmento.ModoPermitido(ModoOperacion.SSB);

        // Assert
        permitido.Should().BeFalse();
    }

    [Fact]
    public void PlanDeBanda_VerificarCompliance_OperacionPermitida_DevuelvePermitido()
    {
        // Arrange
        PlanDeBanda plan = CrearPlanDePrueba();

        // Act
        ResultadoCompliance resultado = plan.VerificarCompliance(
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            NivelLicencia.Intermedio);

        // Assert
        resultado.EsPermitido.Should().BeTrue();
        resultado.Violacion.Should().Be(TipoViolacion.Ninguna);
    }

    [Fact]
    public void PlanDeBanda_VerificarCompliance_FueraDeBanda_DevuelveFueraDeBanda()
    {
        // Arrange
        PlanDeBanda plan = CrearPlanDePrueba();

        // Act
        ResultadoCompliance resultado = plan.VerificarCompliance(
            Frecuencia.DesdeMHz(14.5),
            ModoOperacion.SSB,
            NivelLicencia.Avanzado);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.FueraDeBanda);
    }

    [Fact]
    public void PlanDeBanda_VerificarCompliance_LicenciaInsuficiente_DevuelveLicenciaInsuficiente()
    {
        // Arrange
        PlanDeBanda plan = CrearPlanDePrueba();

        // Act
        ResultadoCompliance resultado = plan.VerificarCompliance(
            Frecuencia.DesdeMHz(14.074),
            ModoOperacion.FT8,
            NivelLicencia.Basico);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.LicenciaInsuficiente);
    }

    [Fact]
    public void PlanDeBanda_VerificarCompliance_ModoNoPermitido_DevuelveModoNoPermitido()
    {
        // Arrange
        PlanDeBanda plan = CrearPlanDePrueba();

        // Act
        ResultadoCompliance resultado = plan.VerificarCompliance(
            Frecuencia.DesdeMHz(14.2),
            ModoOperacion.FT8,
            NivelLicencia.Intermedio);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.ModoNoPermitido);
    }

    [Fact]
    public void PlanDeBanda_VerificarCompliance_PotenciaExcedida_DevuelvePotenciaExcedida()
    {
        // Arrange
        PlanDeBanda plan = CrearPlanDePrueba();

        // Act
        ResultadoCompliance resultado = plan.VerificarCompliance(
            Frecuencia.DesdeMHz(14.2),
            ModoOperacion.SSB,
            NivelLicencia.Intermedio,
            potenciaVatios: 2000.0);

        // Assert
        resultado.EsPermitido.Should().BeFalse();
        resultado.Violacion.Should().Be(TipoViolacion.PotenciaExcedida);
    }

    /// <summary>
    /// Crea un plan de banda de prueba con segmentos CW/digital estrecho, digital y fonía.
    /// </summary>
    private static PlanDeBanda CrearPlanDePrueba()
    {
        PlanDeBanda plan = new PlanDeBanda(BandaRadio.Banda20m, RegionItu.Region2);

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.0),
            Frecuencia.DesdeMHz(14.07),
            TipoSegmento.CwYDigitalEstrecho,
            NivelLicencia.Intermedio));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.07),
            Frecuencia.DesdeMHz(14.15),
            TipoSegmento.Digital,
            NivelLicencia.Intermedio));

        plan.AgregarSegmento(new SegmentoBanda(
            Frecuencia.DesdeMHz(14.15),
            Frecuencia.DesdeMHz(14.35),
            TipoSegmento.Fonia,
            NivelLicencia.Intermedio,
            potenciaMaximaVatios: 1500.0));

        return plan;
    }
}
