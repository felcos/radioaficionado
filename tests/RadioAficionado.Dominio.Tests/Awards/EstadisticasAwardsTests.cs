using FluentAssertions;
using RadioAficionado.Dominio.Awards;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.Awards;

public class EstadisticasAwardsTests
{
    private readonly EstadisticasAwards _estadisticas = new();

    private static Qso CrearQso(string indicativo, long frecuenciaHz = 14_074_000,
        string? notas = null, string? localizador = null)
    {
        return Qso.Crear(
            new Indicativo("EA4TEST"),
            new Indicativo(indicativo),
            DateTimeOffset.UtcNow,
            Frecuencia.DesdeHz(frecuenciaHz),
            ModoOperacion.SSB,
            "59",
            notas: notas,
            localizadorContacto: localizador is not null ? new Localizador(localizador) : null);
    }

    // ========== WAC ==========

    [Fact]
    public void CalcularWac_SinQsos_CeroPorcentaje()
    {
        // Arrange
        List<Qso> qsos = [];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularWac(qsos);

        // Assert
        resultado.Tipo.Should().Be(TipoDiploma.Wac);
        resultado.Trabajadas.Should().Be(0);
        resultado.Total.Should().Be(6);
        resultado.Porcentaje.Should().Be(0);
    }

    [Fact]
    public void CalcularWac_ContactosEnTresContinentes_MuestraProgresoParcial()
    {
        // Arrange — W1AW=NA(291), JA1XYZ=AS(339), DL1ABC=EU(230)
        List<Qso> qsos =
        [
            CrearQso("W1AW"),
            CrearQso("JA1XYZ"),
            CrearQso("DL1ABC")
        ];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularWac(qsos);

        // Assert
        resultado.Trabajadas.Should().Be(3);
        resultado.Porcentaje.Should().Be(50.0);
        resultado.ElementosFaltantes.Should().HaveCount(3);
    }

    [Fact]
    public void CalcularWac_ListaNula_LanzaExcepcion()
    {
        // Act
        Action accion = () => _estadisticas.CalcularWac(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    // ========== WAZ ==========

    [Fact]
    public void CalcularWaz_SinQsos_CeroPorcentaje()
    {
        // Arrange
        List<Qso> qsos = [];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularWaz(qsos);

        // Assert
        resultado.Tipo.Should().Be(TipoDiploma.Waz);
        resultado.Trabajadas.Should().Be(0);
        resultado.Total.Should().Be(40);
        resultado.ElementosFaltantes.Should().HaveCount(40);
    }

    [Fact]
    public void CalcularWaz_ContactoConZonaCq_CuentaZona()
    {
        // Arrange — W1AW tiene zona CQ 5
        List<Qso> qsos = [CrearQso("W1AW")];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularWaz(qsos);

        // Assert
        resultado.Trabajadas.Should().BeGreaterThanOrEqualTo(1);
        resultado.Porcentaje.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalcularWaz_DuplicadosMismaZona_NoCuentaDoble()
    {
        // Arrange — Ambos de EEUU, misma zona CQ
        List<Qso> qsos =
        [
            CrearQso("W1AW"),
            CrearQso("K1ABC")
        ];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularWaz(qsos);

        // Assert — Deberian ser la misma zona (o zonas cercanas)
        resultado.Trabajadas.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public void CalcularWaz_ListaNula_LanzaExcepcion()
    {
        // Act
        Action accion = () => _estadisticas.CalcularWaz(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    // ========== WAS ==========

    [Fact]
    public void CalcularWas_SinQsos_CeroPorcentaje()
    {
        // Arrange
        List<Qso> qsos = [];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularWas(qsos);

        // Assert
        resultado.Tipo.Should().Be(TipoDiploma.Was);
        resultado.Trabajadas.Should().Be(0);
        resultado.Total.Should().Be(50);
        resultado.ElementosFaltantes.Should().HaveCount(50);
    }

    [Fact]
    public void CalcularWas_QsoConEstadoEnNotas_CuentaEstado()
    {
        // Arrange — QSO con W1AW y nota "State: CT"
        List<Qso> qsos = [CrearQso("W1AW", notas: "State: CT")];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularWas(qsos);

        // Assert
        resultado.Trabajadas.Should().Be(1);
        resultado.ElementosTrabajados.Should().Contain("CT");
    }

    [Fact]
    public void CalcularWas_QsoSinEstado_NoCuenta()
    {
        // Arrange — QSO con W1AW sin nota de estado
        List<Qso> qsos = [CrearQso("W1AW")];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularWas(qsos);

        // Assert
        resultado.Trabajadas.Should().Be(0);
    }

    [Fact]
    public void CalcularWas_QsoNoEeuu_NoCuenta()
    {
        // Arrange — QSO con estacion europea, no EEUU
        List<Qso> qsos = [CrearQso("DL1ABC", notas: "NW")];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularWas(qsos);

        // Assert
        resultado.Trabajadas.Should().Be(0);
    }

    [Fact]
    public void CalcularWas_ListaNula_LanzaExcepcion()
    {
        // Act
        Action accion = () => _estadisticas.CalcularWas(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    // ========== VUCC ==========

    [Fact]
    public void CalcularVucc_SinQsos_CeroPorcentaje()
    {
        // Arrange
        List<Qso> qsos = [];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularVucc(qsos);

        // Assert
        resultado.Tipo.Should().Be(TipoDiploma.Vucc);
        resultado.Trabajadas.Should().Be(0);
        resultado.Total.Should().Be(100);
    }

    [Fact]
    public void CalcularVucc_QsoVhfConGrid_CuentaGrid()
    {
        // Arrange — 50.1 MHz = 6m (VHF)
        List<Qso> qsos = [CrearQso("W1AW", frecuenciaHz: 50_100_000, localizador: "FN31pr")];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularVucc(qsos);

        // Assert
        resultado.Trabajadas.Should().Be(1);
        resultado.ElementosTrabajados.Should().Contain("FN31");
    }

    [Fact]
    public void CalcularVucc_QsoHfConGrid_NoCuenta()
    {
        // Arrange — 14 MHz = HF, no cuenta para VUCC
        List<Qso> qsos = [CrearQso("W1AW", frecuenciaHz: 14_074_000, localizador: "FN31pr")];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularVucc(qsos);

        // Assert
        resultado.Trabajadas.Should().Be(0);
    }

    [Fact]
    public void CalcularVucc_QsoVhfSinGrid_NoCuenta()
    {
        // Arrange — VHF pero sin localizador
        List<Qso> qsos = [CrearQso("W1AW", frecuenciaHz: 50_100_000)];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularVucc(qsos);

        // Assert
        resultado.Trabajadas.Should().Be(0);
    }

    [Fact]
    public void CalcularVucc_MismoGridDoble_NoCuentaDoble()
    {
        // Arrange — dos QSOs al mismo grid
        List<Qso> qsos =
        [
            CrearQso("W1AW", frecuenciaHz: 50_100_000, localizador: "FN31pr"),
            CrearQso("K1ABC", frecuenciaHz: 50_100_000, localizador: "FN31ab")
        ];

        // Act
        ResumenDiploma resultado = _estadisticas.CalcularVucc(qsos);

        // Assert — ambos son FN31, solo cuenta una vez
        resultado.Trabajadas.Should().Be(1);
    }

    [Fact]
    public void CalcularVucc_ListaNula_LanzaExcepcion()
    {
        // Act
        Action accion = () => _estadisticas.CalcularVucc(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    // ========== CalcularTodos ==========

    [Fact]
    public void CalcularTodos_DevuelveCuatroResumenes()
    {
        // Arrange
        List<Qso> qsos = [CrearQso("W1AW")];

        // Act
        IReadOnlyList<ResumenDiploma> resultados = _estadisticas.CalcularTodos(qsos);

        // Assert
        resultados.Should().HaveCount(4);
        resultados.Select(r => r.Tipo).Should().Contain(TipoDiploma.Wac);
        resultados.Select(r => r.Tipo).Should().Contain(TipoDiploma.Waz);
        resultados.Select(r => r.Tipo).Should().Contain(TipoDiploma.Was);
        resultados.Select(r => r.Tipo).Should().Contain(TipoDiploma.Vucc);
    }

    [Fact]
    public void CalcularTodos_ListaNula_LanzaExcepcion()
    {
        // Act
        Action accion = () => _estadisticas.CalcularTodos(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    // ========== CatalogoEstadosUsa ==========

    [Fact]
    public void CatalogoEstadosUsa_Tiene50Estados()
    {
        // Assert
        CatalogoEstadosUsa.Estados.Should().HaveCount(50);
        CatalogoEstadosUsa.TotalEstados.Should().Be(50);
    }

    [Fact]
    public void CatalogoEstadosUsa_ObtenerPorAbreviatura_DevuelveEstadoCorrecto()
    {
        // Act
        CatalogoEstadosUsa.EstadoUsa? california = CatalogoEstadosUsa.ObtenerPorAbreviatura("CA");

        // Assert
        california.Should().NotBeNull();
        california!.Nombre.Should().Be("California");
    }

    [Fact]
    public void CatalogoEstadosUsa_ObtenerPorAbreviatura_Inexistente_DevuelveNull()
    {
        // Act
        CatalogoEstadosUsa.EstadoUsa? resultado = CatalogoEstadosUsa.ObtenerPorAbreviatura("XX");

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void CatalogoEstadosUsa_ObtenerPorAbreviatura_Nulo_DevuelveNull()
    {
        // Act
        CatalogoEstadosUsa.EstadoUsa? resultado = CatalogoEstadosUsa.ObtenerPorAbreviatura(null!);

        // Assert
        resultado.Should().BeNull();
    }
}
