using FluentAssertions;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Dominio.Qsl;
using RadioAficionado.Infraestructura.Qsl;

namespace RadioAficionado.Infraestructura.Tests.Qsl;

/// <summary>
/// Tests para el generador de tarjetas QSL digitales basado en SkiaSharp.
/// </summary>
public class GeneradorQslSkiaTests
{
    private readonly GeneradorQslSkia _generador = new();

    /// <summary>
    /// Crea datos de prueba válidos para las tarjetas QSL.
    /// </summary>
    private static DatosQsl CrearDatosValidos()
    {
        Qso qso = Qso.Crear(
            indicativoPropio: new Indicativo("EA4ABC"),
            indicativoContacto: new Indicativo("W1AW"),
            fechaHoraInicio: DateTimeOffset.UtcNow.AddHours(-1),
            frecuencia: Frecuencia.DesdeMHz(14.074),
            modo: ModoOperacion.FT8,
            senalEnviada: "-10",
            potencia: 100,
            localizadorContacto: null,
            notas: "Buen contacto DX");

        qso.Completar(DateTimeOffset.UtcNow.AddMinutes(-58), "-12");

        return new DatosQsl(
            Contacto: qso,
            IndicativoPropio: "EA4ABC",
            NombreOperador: "Felipe",
            Localizador: "IN80fk",
            Ciudad: "Madrid",
            Pais: "España");
    }

    /// <summary>
    /// Obtiene la plantilla clásica predefinida.
    /// </summary>
    private static PlantillaQsl ObtenerPlantillaClasica()
    {
        return new PlantillaQsl(
            "Clasica",
            AnchoPixeles: 800,
            AltoPixeles: 500,
            ColorFondo: "#1A237E",
            ColorTexto: "#FFFFFF");
    }

    [Fact]
    public async Task Generar_ConDatosValidos_RetornaBytes()
    {
        // Arrange
        DatosQsl datos = CrearDatosValidos();
        PlantillaQsl plantilla = ObtenerPlantillaClasica();

        // Act
        byte[] resultado = await _generador.GenerarAsync(datos, plantilla, FormatoExportacion.Png);

        // Assert
        resultado.Should().NotBeNullOrEmpty("la imagen generada debe contener datos");
    }

    [Fact]
    public async Task Generar_FormatoPng_TieneCabeceraPng()
    {
        // Arrange
        DatosQsl datos = CrearDatosValidos();
        PlantillaQsl plantilla = ObtenerPlantillaClasica();

        // Los primeros 8 bytes de un PNG son: 137 80 78 71 13 10 26 10
        byte[] cabeceraPng = { 137, 80, 78, 71, 13, 10, 26, 10 };

        // Act
        byte[] resultado = await _generador.GenerarAsync(datos, plantilla, FormatoExportacion.Png);

        // Assert
        resultado.Length.Should().BeGreaterThan(8);
        resultado[..8].Should().BeEquivalentTo(cabeceraPng, "los primeros 8 bytes deben ser la firma PNG");
    }

    [Fact]
    public async Task Generar_FormatoJpg_TieneCabeceraJpg()
    {
        // Arrange
        DatosQsl datos = CrearDatosValidos();
        PlantillaQsl plantilla = ObtenerPlantillaClasica();

        // Los primeros 2 bytes de un JPEG son: 0xFF 0xD8
        byte cabecera1 = 0xFF;
        byte cabecera2 = 0xD8;

        // Act
        byte[] resultado = await _generador.GenerarAsync(datos, plantilla, FormatoExportacion.Jpg);

        // Assert
        resultado.Length.Should().BeGreaterThan(2);
        resultado[0].Should().Be(cabecera1, "el primer byte de un JPEG debe ser 0xFF");
        resultado[1].Should().Be(cabecera2, "el segundo byte de un JPEG debe ser 0xD8");
    }

    [Fact]
    public async Task Generar_PlantillaClasica_FuncionaCorrectamente()
    {
        // Arrange
        DatosQsl datos = CrearDatosValidos();
        PlantillaQsl plantilla = ObtenerPlantillaClasica();

        // Act
        byte[] resultado = await _generador.GenerarAsync(datos, plantilla, FormatoExportacion.Png);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Length.Should().BeGreaterThan(1000, "una imagen QSL debe tener un tamaño razonable");
    }

    [Fact]
    public async Task Generar_PlantillaModerna_FuncionaCorrectamente()
    {
        // Arrange
        DatosQsl datos = CrearDatosValidos();
        PlantillaQsl plantilla = new(
            "Moderna",
            AnchoPixeles: 800,
            AltoPixeles: 500,
            ColorFondo: "#0D47A1",
            ColorTexto: "#E3F2FD");

        // Act
        byte[] resultado = await _generador.GenerarAsync(datos, plantilla, FormatoExportacion.Png);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task Generar_PlantillaMinimalista_FuncionaCorrectamente()
    {
        // Arrange
        DatosQsl datos = CrearDatosValidos();
        PlantillaQsl plantilla = new(
            "Minimalista",
            AnchoPixeles: 800,
            AltoPixeles: 500,
            ColorFondo: "#FFFFFF",
            ColorTexto: "#212121");

        // Act
        byte[] resultado = await _generador.GenerarAsync(datos, plantilla, FormatoExportacion.Png);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task Generar_SinLocalizador_NoFalla()
    {
        // Arrange
        Qso qso = Qso.Crear(
            indicativoPropio: new Indicativo("EA4ABC"),
            indicativoContacto: new Indicativo("VK2ABC"),
            fechaHoraInicio: DateTimeOffset.UtcNow.AddHours(-2),
            frecuencia: Frecuencia.DesdeMHz(7.074),
            modo: ModoOperacion.FT8,
            senalEnviada: "59");

        DatosQsl datos = new(
            Contacto: qso,
            IndicativoPropio: "EA4ABC",
            NombreOperador: "Felipe",
            Localizador: null,
            Ciudad: null,
            Pais: null);

        PlantillaQsl plantilla = ObtenerPlantillaClasica();

        // Act
        byte[] resultado = await _generador.GenerarAsync(datos, plantilla, FormatoExportacion.Png);

        // Assert
        resultado.Should().NotBeNullOrEmpty("debe generar la tarjeta aunque falten datos opcionales");
    }

    [Fact]
    public async Task ObtenerPlantillas_RetornaTresPlantillas()
    {
        // Act
        IReadOnlyList<PlantillaQsl> plantillas = await _generador.ObtenerPlantillasAsync();

        // Assert
        plantillas.Should().HaveCount(3, "deben existir exactamente 3 plantillas predefinidas");
    }

    [Fact]
    public async Task ObtenerPlantillas_ContieneClasicaModernaMinimalista()
    {
        // Act
        IReadOnlyList<PlantillaQsl> plantillas = await _generador.ObtenerPlantillasAsync();

        // Assert
        plantillas.Select(p => p.Nombre).Should().Contain("Clasica");
        plantillas.Select(p => p.Nombre).Should().Contain("Moderna");
        plantillas.Select(p => p.Nombre).Should().Contain("Minimalista");
    }

    [Fact]
    public async Task Generar_FormatoNoSoportado_LanzaNotSupportedException()
    {
        // Arrange
        DatosQsl datos = CrearDatosValidos();
        PlantillaQsl plantilla = ObtenerPlantillaClasica();

        // Act
        Func<Task> accion = async () => await _generador.GenerarAsync(datos, plantilla, FormatoExportacion.Pdf);

        // Assert
        await accion.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Pdf*no está soportado*");
    }

    [Fact]
    public async Task Generar_DatosNulos_LanzaArgumentNullException()
    {
        // Arrange
        PlantillaQsl plantilla = ObtenerPlantillaClasica();

        // Act
        Func<Task> accion = async () => await _generador.GenerarAsync(null!, plantilla, FormatoExportacion.Png);

        // Assert
        await accion.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Generar_PlantillaNula_LanzaArgumentNullException()
    {
        // Arrange
        DatosQsl datos = CrearDatosValidos();

        // Act
        Func<Task> accion = async () => await _generador.GenerarAsync(datos, null!, FormatoExportacion.Png);

        // Assert
        await accion.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Generar_DimensionesPersonalizadas_GeneraImagenCorrectamente()
    {
        // Arrange
        DatosQsl datos = CrearDatosValidos();
        PlantillaQsl plantilla = new(
            "Personalizada",
            AnchoPixeles: 1024,
            AltoPixeles: 768,
            ColorFondo: "#333333",
            ColorTexto: "#00FF00");

        // Act
        byte[] resultado = await _generador.GenerarAsync(datos, plantilla, FormatoExportacion.Png);

        // Assert
        resultado.Should().NotBeNullOrEmpty();
        resultado.Length.Should().BeGreaterThan(1000);
    }
}
