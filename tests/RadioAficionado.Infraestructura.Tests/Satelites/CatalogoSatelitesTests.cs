using FluentAssertions;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Satelites;
using RadioAficionado.Infraestructura.Satelites;

namespace RadioAficionado.Infraestructura.Tests.Satelites;

/// <summary>
/// Tests unitarios para <see cref="CatalogoSatelites"/>.
/// Valida que el catálogo de satélites amateur esté correctamente poblado.
/// </summary>
public sealed class CatalogoSatelitesTests
{
    [Fact]
    public void ObtenerTodos_DebeRetornarListaNoVacia()
    {
        // Act
        IReadOnlyList<SateliteAmateur> satelites = CatalogoSatelites.ObtenerTodos();

        // Assert
        satelites.Should().NotBeEmpty();
        satelites.Count.Should().BeGreaterThanOrEqualTo(15,
            "el catálogo debe contener al menos 15 satélites amateur populares");
    }

    [Fact]
    public void ObtenerTodos_DebeContenerLaIss()
    {
        // Act
        IReadOnlyList<SateliteAmateur> satelites = CatalogoSatelites.ObtenerTodos();

        // Assert
        SateliteAmateur? iss = satelites.FirstOrDefault(s => s.NumeroNorad == 25544);
        iss.Should().NotBeNull("la ISS (NORAD 25544) debe estar en el catálogo");
        iss!.Nombre.Should().Contain("ISS");
        iss.Indicativo.Should().Be("RS0ISS");
        iss.Activo.Should().BeTrue();
    }

    [Fact]
    public void ObtenerTodos_TodosLosSatelitesDebenTenerTranspondersValidos()
    {
        // Act
        IReadOnlyList<SateliteAmateur> satelites = CatalogoSatelites.ObtenerTodos();

        // Assert
        foreach (SateliteAmateur satelite in satelites)
        {
            satelite.Transponders.Should().NotBeNull(
                $"{satelite.Nombre} debe tener lista de transponders");
            satelite.Transponders.Count.Should().BeGreaterThan(0,
                $"{satelite.Nombre} debe tener al menos un transponder");

            foreach (TransponderSatelite transponder in satelite.Transponders)
            {
                transponder.Nombre.Should().NotBeNullOrWhiteSpace(
                    $"transponder de {satelite.Nombre} debe tener nombre");
                transponder.EnlaceSubida.Hz.Should().BeGreaterThan(0,
                    $"transponder {transponder.Nombre} de {satelite.Nombre} debe tener frecuencia de subida válida");
                transponder.EnlaceBajada.Hz.Should().BeGreaterThan(0,
                    $"transponder {transponder.Nombre} de {satelite.Nombre} debe tener frecuencia de bajada válida");
            }
        }
    }

    [Fact]
    public void BuscarPorNorad_ConNoradExistente_DebeRetornarSatelite()
    {
        // Act
        SateliteAmateur? resultado = CatalogoSatelites.BuscarPorNorad(25544);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Nombre.Should().Contain("ISS");
    }

    [Fact]
    public void BuscarPorNorad_ConNoradInexistente_DebeRetornarNull()
    {
        // Act
        SateliteAmateur? resultado = CatalogoSatelites.BuscarPorNorad(99999);

        // Assert
        resultado.Should().BeNull();
    }
}
