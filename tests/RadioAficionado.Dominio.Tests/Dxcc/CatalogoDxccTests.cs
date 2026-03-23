using FluentAssertions;
using RadioAficionado.Dominio.Dxcc;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Dominio.Tests.Dxcc;

/// <summary>
/// Tests unitarios para <see cref="CatalogoDxcc"/>.
/// Valida búsqueda por prefijo, por indicativo, listado completo y entidades eliminadas.
/// </summary>
public class CatalogoDxccTests
{
    [Fact]
    public void ObtenerTodas_DevuelveListaNoVacia()
    {
        // Act
        IReadOnlyList<EntidadDxcc> todas = CatalogoDxcc.ObtenerTodas();

        // Assert
        todas.Should().NotBeEmpty();
        todas.Count.Should().BeGreaterThan(50);
    }

    [Fact]
    public void ObtenerActivas_DevuelveSoloEntidadesNoEliminadas()
    {
        // Act
        IReadOnlyList<EntidadDxcc> activas = CatalogoDxcc.ObtenerActivas();

        // Assert
        activas.Should().NotBeEmpty();
        activas.Should().AllSatisfy(e => e.Eliminada.Should().BeFalse());
    }

    [Theory]
    [InlineData("K", "Estados Unidos")]
    [InlineData("JA", "Japón")]
    [InlineData("DL", "Alemania")]
    [InlineData("EA", "España")]
    [InlineData("G", "Inglaterra")]
    [InlineData("F", "Francia")]
    [InlineData("I", "Italia")]
    [InlineData("PY", "Brasil")]
    [InlineData("LU", "Argentina")]
    [InlineData("XE", "México")]
    [InlineData("VE", "Canadá")]
    [InlineData("VK", "Australia")]
    [InlineData("UA", "Rusia")]
    [InlineData("BY", "China")]
    [InlineData("HL", "Corea del Sur")]
    [InlineData("VU", "India")]
    public void ObtenerPorPrefijo_PrefijoPrincipal_DevuelveEntidadCorrecta(string prefijo, string nombreEsperado)
    {
        // Act
        EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorPrefijo(prefijo);

        // Assert
        entidad.Should().NotBeNull();
        entidad!.Nombre.Should().Be(nombreEsperado);
    }

    [Theory]
    [InlineData("W", "Estados Unidos")]
    [InlineData("N", "Estados Unidos")]
    [InlineData("RA", "Rusia")]
    [InlineData("DA", "Alemania")]
    [InlineData("EB", "España")]
    [InlineData("PP", "Brasil")]
    [InlineData("VA", "Canadá")]
    public void ObtenerPorPrefijo_PrefijoAlternativo_DevuelveEntidadCorrecta(string prefijo, string nombreEsperado)
    {
        // Act
        EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorPrefijo(prefijo);

        // Assert
        entidad.Should().NotBeNull();
        entidad!.Nombre.Should().Be(nombreEsperado);
    }

    [Fact]
    public void ObtenerPorPrefijo_PrefijoNulo_DevuelveNull()
    {
        // Act
        EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorPrefijo(null!);

        // Assert
        entidad.Should().BeNull();
    }

    [Fact]
    public void ObtenerPorPrefijo_PrefijoVacio_DevuelveNull()
    {
        // Act
        EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorPrefijo("");

        // Assert
        entidad.Should().BeNull();
    }

    [Fact]
    public void ObtenerPorPrefijo_PrefijoInexistente_DevuelveNull()
    {
        // Act — "QQ" no es prefijo de ningún país real y no se reduce a algo válido
        EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorPrefijo("QQ");

        // Assert
        entidad.Should().BeNull();
    }

    [Theory]
    [InlineData("EA4ABC", "España")]
    [InlineData("W1AW", "Estados Unidos")]
    [InlineData("JA1ABC", "Japón")]
    [InlineData("DL1ABC", "Alemania")]
    [InlineData("VK2ABC", "Australia")]
    [InlineData("PY2ABC", "Brasil")]
    [InlineData("LU1ABC", "Argentina")]
    public void ObtenerPorIndicativo_IndicativoValido_DevuelveEntidadCorrecta(string indicativoStr, string nombreEsperado)
    {
        // Arrange
        Indicativo indicativo = new Indicativo(indicativoStr);

        // Act
        EntidadDxcc? entidad = CatalogoDxcc.ObtenerPorIndicativo(indicativo);

        // Assert
        entidad.Should().NotBeNull();
        entidad!.Nombre.Should().Be(nombreEsperado);
    }

    [Fact]
    public void ObtenerTodas_ContieneEntidadesEliminadas()
    {
        // Act
        IReadOnlyList<EntidadDxcc> todas = CatalogoDxcc.ObtenerTodas();

        // Assert
        todas.Should().Contain(e => e.Eliminada);
    }

    [Fact]
    public void ObtenerTodas_EntidadesEliminadas_TienenDatosCorrectos()
    {
        // Act
        IReadOnlyList<EntidadDxcc> todas = CatalogoDxcc.ObtenerTodas();
        List<EntidadDxcc> eliminadas = todas.Where(e => e.Eliminada).ToList();

        // Assert
        eliminadas.Should().NotBeEmpty();
        eliminadas.Should().Contain(e => e.Nombre == "URSS");
    }

    [Fact]
    public void ObtenerTodas_CadaEntidad_TieneContinenteValido()
    {
        // Arrange
        HashSet<string> continentesValidos = new() { "AF", "AN", "AS", "EU", "NA", "OC", "SA" };

        // Act
        IReadOnlyList<EntidadDxcc> todas = CatalogoDxcc.ObtenerTodas();

        // Assert
        todas.Should().AllSatisfy(e => continentesValidos.Should().Contain(e.Continente));
    }

    [Fact]
    public void ObtenerTodas_CadaEntidad_TieneZonaCqValida()
    {
        // Act
        IReadOnlyList<EntidadDxcc> todas = CatalogoDxcc.ObtenerTodas();

        // Assert
        todas.Should().AllSatisfy(e => e.ZonaCq.Should().BeInRange(1, 40));
    }

    [Fact]
    public void ObtenerTodas_CadaEntidad_TieneZonaItuValida()
    {
        // Act
        IReadOnlyList<EntidadDxcc> todas = CatalogoDxcc.ObtenerTodas();

        // Assert
        todas.Should().AllSatisfy(e => e.ZonaItu.Should().BeInRange(1, 90));
    }

    [Fact]
    public void ObtenerPorPrefijo_BusquedaInsensibleAMayusculas()
    {
        // Act
        EntidadDxcc? entidadMayus = CatalogoDxcc.ObtenerPorPrefijo("EA");
        EntidadDxcc? entidadMinus = CatalogoDxcc.ObtenerPorPrefijo("ea");

        // Assert
        entidadMayus.Should().NotBeNull();
        entidadMinus.Should().NotBeNull();
        entidadMayus.Should().Be(entidadMinus);
    }

    [Fact]
    public void ObtenerActivas_TienenMenosEntidadesQueObtenerTodas()
    {
        // Act
        IReadOnlyList<EntidadDxcc> todas = CatalogoDxcc.ObtenerTodas();
        IReadOnlyList<EntidadDxcc> activas = CatalogoDxcc.ObtenerActivas();

        // Assert
        activas.Count.Should().BeLessThan(todas.Count);
    }
}
