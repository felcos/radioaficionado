using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Infraestructura.Backup;

namespace RadioAficionado.Infraestructura.Tests.Backup;

/// <summary>
/// Tests unitarios para <see cref="ServicioBackup"/>.
/// </summary>
public sealed class ServicioBackupTests : IDisposable
{
    private readonly string _carpetaDatos;
    private readonly string _carpetaBackups;
    private readonly ServicioBackup _servicio;

    public ServicioBackupTests()
    {
        string raiz = Path.Combine(Path.GetTempPath(), "radio_backup_test_" + Guid.NewGuid().ToString("N")[..8]);
        _carpetaDatos = Path.Combine(raiz, "datos");
        _carpetaBackups = Path.Combine(raiz, "backups");
        Directory.CreateDirectory(_carpetaDatos);

        Mock<ILogger<ServicioBackup>> mockLogger = new();
        _servicio = new ServicioBackup(_carpetaDatos, _carpetaBackups, mockLogger.Object);
    }

    [Fact]
    public async Task CrearBackupAsync_ConArchivos_CopiaCorrectamente()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_carpetaDatos, "configuracion.json"), "{\"test\":true}");
        await File.WriteAllTextAsync(Path.Combine(_carpetaDatos, "radio.db"), "datos_sqlite");

        // Act
        ResultadoBackup resultado = await _servicio.CrearBackupAsync();

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.RutaArchivo.Should().NotBeNullOrWhiteSpace();
        Directory.Exists(resultado.RutaArchivo).Should().BeTrue();
        File.Exists(Path.Combine(resultado.RutaArchivo, "configuracion.json")).Should().BeTrue();
        File.Exists(Path.Combine(resultado.RutaArchivo, "radio.db")).Should().BeTrue();
    }

    [Fact]
    public async Task CrearBackupAsync_SinArchivos_RetornaFallo()
    {
        // Act
        ResultadoBackup resultado = await _servicio.CrearBackupAsync();

        // Assert
        resultado.Exitoso.Should().BeFalse();
        resultado.Mensaje.Should().Contain("No se encontraron");
    }

    [Fact]
    public async Task LimpiarBackupsAntiguosAsync_MantieneSoloNRecientes()
    {
        // Arrange — crear 5 backups
        Directory.CreateDirectory(_carpetaBackups);
        for (int i = 0; i < 5; i++)
        {
            string carpeta = Path.Combine(_carpetaBackups, $"backup_2026010{i}_120000");
            Directory.CreateDirectory(carpeta);
        }

        // Act — retener solo 2
        int eliminados = await _servicio.LimpiarBackupsAntiguosAsync(2);

        // Assert
        eliminados.Should().Be(3);
        Directory.GetDirectories(_carpetaBackups, "backup_*").Should().HaveCount(2);
    }

    [Fact]
    public void LimpiarBackupsAntiguosAsync_MaxRetenerMenorA1_LanzaExcepcion()
    {
        // Act
        Func<Task> accion = () => _servicio.LimpiarBackupsAntiguosAsync(0);

        // Assert
        accion.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ObtenerBackupsDisponibles_SinCarpeta_RetornaVacio()
    {
        // Act
        IReadOnlyList<string> backups = _servicio.ObtenerBackupsDisponibles();

        // Assert
        backups.Should().BeEmpty();
    }

    [Fact]
    public void ObtenerBackupsDisponibles_ConBackups_RetornaOrdenado()
    {
        // Arrange
        Directory.CreateDirectory(_carpetaBackups);
        Directory.CreateDirectory(Path.Combine(_carpetaBackups, "backup_20260101_100000"));
        Directory.CreateDirectory(Path.Combine(_carpetaBackups, "backup_20260103_100000"));
        Directory.CreateDirectory(Path.Combine(_carpetaBackups, "backup_20260102_100000"));

        // Act
        IReadOnlyList<string> backups = _servicio.ObtenerBackupsDisponibles();

        // Assert
        backups.Should().HaveCount(3);
        Path.GetFileName(backups[0]).Should().Be("backup_20260103_100000"); // mas reciente primero
    }

    [Fact]
    public async Task CrearBackupAsync_SoloConfiguracion_CopiaCorrectamente()
    {
        // Arrange — solo configuracion, sin BD
        await File.WriteAllTextAsync(Path.Combine(_carpetaDatos, "configuracion.json"), "{}");

        // Act
        ResultadoBackup resultado = await _servicio.CrearBackupAsync();

        // Assert
        resultado.Exitoso.Should().BeTrue();
        resultado.Mensaje.Should().Contain("1 archivo");
    }

    [Fact]
    public void Constructor_CarpetaDatosNula_LanzaExcepcion()
    {
        // Act
        Action accion = () => new ServicioBackup("", _carpetaBackups, new Mock<ILogger<ServicioBackup>>().Object);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_CarpetaBackupsNula_LanzaExcepcion()
    {
        // Act
        Action accion = () => new ServicioBackup(_carpetaDatos, "", new Mock<ILogger<ServicioBackup>>().Object);

        // Assert
        accion.Should().Throw<ArgumentException>();
    }

    public void Dispose()
    {
        // Limpiar archivos temporales
        string? raiz = Path.GetDirectoryName(_carpetaDatos);
        if (raiz is not null && Directory.Exists(raiz))
        {
            try { Directory.Delete(raiz, true); }
            catch { /* ignorar si falla limpieza */ }
        }
    }
}
