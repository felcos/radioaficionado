using FluentAssertions;
using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.Dxcc;

/// <summary>
/// Tests unitarios para <see cref="EstadisticasDxcc"/>.
/// Valida el cálculo de entidades trabajadas, confirmadas, por banda, por modo y faltantes.
/// </summary>
public class EstadisticasDxccTests
{
    private readonly EstadisticasDxcc _estadisticas = new();

    private static Qso CrearQso(string indicativoContacto, double frecuenciaMhz, ModoOperacion modo)
    {
        return Qso.Crear(
            indicativoPropio: new Indicativo("EA4ABC"),
            indicativoContacto: new Indicativo(indicativoContacto),
            fechaHoraInicio: DateTimeOffset.UtcNow.AddMinutes(-10),
            frecuencia: Frecuencia.DesdeMHz(frecuenciaMhz),
            modo: modo,
            senalEnviada: "59");
    }

    [Fact]
    public void EntidadesTrabajadas_ListaVacia_DevuelveConjuntoVacio()
    {
        // Act
        HashSet<int> resultado = _estadisticas.EntidadesTrabajadas(Array.Empty<Qso>());

        // Assert
        resultado.Should().BeEmpty();
    }

    [Fact]
    public void EntidadesTrabajadas_QsosConDiferentesPaises_DevuelveEntidadesUnicas()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("W1AW", 14.074, ModoOperacion.FT8),
            CrearQso("JA1ABC", 14.074, ModoOperacion.FT8),
            CrearQso("DL1ABC", 14.074, ModoOperacion.FT8)
        };

        // Act
        HashSet<int> resultado = _estadisticas.EntidadesTrabajadas(qsos);

        // Assert
        resultado.Should().HaveCount(3);
    }

    [Fact]
    public void EntidadesTrabajadas_QsosDuplicadosMismoPais_DevuelveSoloUnNumero()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("W1AW", 14.074, ModoOperacion.FT8),
            CrearQso("N1ABC", 14.074, ModoOperacion.CW),
            CrearQso("K2ABC", 7.074, ModoOperacion.FT8)
        };

        // Act
        HashSet<int> resultado = _estadisticas.EntidadesTrabajadas(qsos);

        // Assert
        resultado.Should().HaveCount(1, "todos los indicativos son de Estados Unidos");
    }

    [Fact]
    public void EntidadesTrabajadas_ListaNula_LanzaArgumentNullException()
    {
        // Act
        Action accion = () => _estadisticas.EntidadesTrabajadas(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EntidadesConfirmadas_SoloDevuelveConfirmadas()
    {
        // Arrange
        Qso qsoUsa = CrearQso("W1AW", 14.074, ModoOperacion.FT8);
        Qso qsoJapon = CrearQso("JA1ABC", 14.074, ModoOperacion.FT8);
        Qso qsoAlemania = CrearQso("DL1ABC", 14.074, ModoOperacion.FT8);

        List<Qso> qsos = new() { qsoUsa, qsoJapon, qsoAlemania };

        List<ConfirmacionQso> confirmaciones = new()
        {
            new ConfirmacionQso(qsoUsa.Id, TipoConfirmacion.LoTW, DateTimeOffset.UtcNow),
            new ConfirmacionQso(qsoJapon.Id, TipoConfirmacion.QslFisica, DateTimeOffset.UtcNow)
        };

        // Act
        HashSet<int> resultado = _estadisticas.EntidadesConfirmadas(qsos, confirmaciones);

        // Assert
        resultado.Should().HaveCount(2, "solo USA y Japón están confirmados");
    }

    [Fact]
    public void EntidadesConfirmadas_SinConfirmaciones_DevuelveVacio()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("W1AW", 14.074, ModoOperacion.FT8)
        };

        // Act
        HashSet<int> resultado = _estadisticas.EntidadesConfirmadas(qsos, Array.Empty<ConfirmacionQso>());

        // Assert
        resultado.Should().BeEmpty();
    }

    [Fact]
    public void PorBanda_AgrupaPorBandaCorrectamente()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("W1AW", 14.074, ModoOperacion.FT8),     // 20m - USA
            CrearQso("JA1ABC", 14.074, ModoOperacion.FT8),   // 20m - Japón
            CrearQso("DL1ABC", 7.074, ModoOperacion.FT8),    // 40m - Alemania
        };

        // Act
        Dictionary<BandaRadio, HashSet<int>> resultado = _estadisticas.PorBanda(qsos);

        // Assert
        resultado.Should().ContainKey(BandaRadio.Banda20m);
        resultado[BandaRadio.Banda20m].Should().HaveCount(2);
        resultado.Should().ContainKey(BandaRadio.Banda40m);
        resultado[BandaRadio.Banda40m].Should().HaveCount(1);
    }

    [Fact]
    public void PorModo_AgrupaPorModoCorrectamente()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("W1AW", 14.074, ModoOperacion.FT8),
            CrearQso("JA1ABC", 14.074, ModoOperacion.CW),
            CrearQso("DL1ABC", 14.074, ModoOperacion.FT8),
        };

        // Act
        Dictionary<ModoOperacion, HashSet<int>> resultado = _estadisticas.PorModo(qsos);

        // Assert
        resultado.Should().ContainKey(ModoOperacion.FT8);
        resultado[ModoOperacion.FT8].Should().HaveCount(2);
        resultado.Should().ContainKey(ModoOperacion.CW);
        resultado[ModoOperacion.CW].Should().HaveCount(1);
    }

    [Fact]
    public void EntidadesFaltantes_ConQsos_DevuelveMenosEntidadesQueElTotal()
    {
        // Arrange
        List<Qso> qsos = new()
        {
            CrearQso("W1AW", 14.074, ModoOperacion.FT8),
            CrearQso("JA1ABC", 14.074, ModoOperacion.FT8),
        };

        // Act
        IReadOnlyList<EntidadDxcc> faltantes = _estadisticas.EntidadesFaltantes(qsos);
        IReadOnlyList<EntidadDxcc> activas = CatalogoDxcc.ObtenerActivas();

        // Assert
        faltantes.Count.Should().BeLessThan(activas.Count);
        faltantes.Should().NotContain(e => e.Nombre == "Estados Unidos");
        faltantes.Should().NotContain(e => e.Nombre == "Japón");
    }

    [Fact]
    public void EntidadesFaltantes_SinQsos_DevuelveTodasLasActivas()
    {
        // Act
        IReadOnlyList<EntidadDxcc> faltantes = _estadisticas.EntidadesFaltantes(Array.Empty<Qso>());
        IReadOnlyList<EntidadDxcc> activas = CatalogoDxcc.ObtenerActivas();

        // Assert
        faltantes.Count.Should().Be(activas.Count);
    }

    [Fact]
    public void GenerarResumen_DevuelveResumenCompleto()
    {
        // Arrange
        Qso qsoUsa = CrearQso("W1AW", 14.074, ModoOperacion.FT8);
        Qso qsoJapon = CrearQso("JA1ABC", 7.074, ModoOperacion.CW);

        List<Qso> qsos = new() { qsoUsa, qsoJapon };
        List<ConfirmacionQso> confirmaciones = new()
        {
            new ConfirmacionQso(qsoUsa.Id, TipoConfirmacion.LoTW, DateTimeOffset.UtcNow)
        };

        // Act
        ResumenDxcc resumen = _estadisticas.GenerarResumen(qsos, confirmaciones);

        // Assert
        resumen.TotalTrabajadas.Should().Be(2);
        resumen.TotalConfirmadas.Should().Be(1);
        resumen.PorBanda.Should().NotBeEmpty();
        resumen.PorModo.Should().NotBeEmpty();
        resumen.PorContinente.Should().NotBeEmpty();
    }

    [Fact]
    public void PorBanda_ListaNula_LanzaArgumentNullException()
    {
        // Act
        Action accion = () => _estadisticas.PorBanda(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PorModo_ListaNula_LanzaArgumentNullException()
    {
        // Act
        Action accion = () => _estadisticas.PorModo(null!);

        // Assert
        accion.Should().Throw<ArgumentNullException>();
    }
}
