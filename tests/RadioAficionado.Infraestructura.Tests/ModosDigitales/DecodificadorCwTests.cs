using FluentAssertions;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Nativo.ModosDigitales.Cw;

namespace RadioAficionado.Infraestructura.Tests.ModosDigitales;

/// <summary>
/// Tests unitarios para el decodificador CW.
/// Genera audio sintetico con patron Morse para verificar la decodificacion.
/// </summary>
public class DecodificadorCwTests
{
    private const int FrecuenciaMuestreo = 12000;
    private const double FrecuenciaTono = 700.0;
    private const int WpmPrueba = 20;

    /// <summary>
    /// Duracion de un dit en muestras a la velocidad de prueba.
    /// Formula PARIS: 1 dit = 1200ms / WPM. A 20 WPM = 60ms = 720 muestras a 12000 Hz.
    /// </summary>
    private static readonly int MuestrasPorDit = FrecuenciaMuestreo * 1200 / (WpmPrueba * 1000);

    /// <summary>
    /// Genera muestras de tono CW (mark) a la frecuencia especificada.
    /// </summary>
    private static short[] GenerarMark(int duracionMuestras)
    {
        short[] muestras = new short[duracionMuestras];
        for (int i = 0; i < duracionMuestras; i++)
        {
            double valor = 16000.0 * Math.Sin(2.0 * Math.PI * FrecuenciaTono * i / FrecuenciaMuestreo);
            muestras[i] = (short)Math.Clamp(valor, short.MinValue, short.MaxValue);
        }
        return muestras;
    }

    /// <summary>
    /// Genera muestras de silencio (space).
    /// </summary>
    private static short[] GenerarSpace(int duracionMuestras)
    {
        return new short[duracionMuestras];
    }

    /// <summary>
    /// Concatena multiples arreglos de muestras en uno solo.
    /// </summary>
    private static short[] Concatenar(params short[][] bloques)
    {
        int total = 0;
        foreach (short[] bloque in bloques)
        {
            total += bloque.Length;
        }

        short[] resultado = new short[total];
        int posicion = 0;
        foreach (short[] bloque in bloques)
        {
            Array.Copy(bloque, 0, resultado, posicion, bloque.Length);
            posicion += bloque.Length;
        }
        return resultado;
    }

    /// <summary>
    /// Genera la secuencia de audio para un patron Morse (e.g., ".-" para A).
    /// </summary>
    private static short[] GenerarPatronMorse(string patron)
    {
        List<short[]> segmentos = new();

        for (int i = 0; i < patron.Length; i++)
        {
            if (i > 0)
            {
                // Espacio entre elementos: 1 dit
                segmentos.Add(GenerarSpace(MuestrasPorDit));
            }

            if (patron[i] == '.')
            {
                segmentos.Add(GenerarMark(MuestrasPorDit));
            }
            else if (patron[i] == '-')
            {
                segmentos.Add(GenerarMark(MuestrasPorDit * 3));
            }
        }

        return Concatenar(segmentos.ToArray());
    }

    /// <summary>
    /// Genera audio para una secuencia de letras Morse con espacios entre letras y palabras.
    /// Usa '/' para separar palabras.
    /// </summary>
    private static short[] GenerarSecuenciaMorse(string texto)
    {
        List<short[]> segmentos = new();
        bool primerCaracter = true;

        foreach (char caracter in texto.ToUpperInvariant())
        {
            if (caracter == ' ' || caracter == '/')
            {
                // Espacio entre palabras: 7 dits
                segmentos.Add(GenerarSpace(MuestrasPorDit * 7));
                primerCaracter = true;
                continue;
            }

            string? patron = TablaMorse.ConvertirAMorse(caracter);
            if (patron is null)
            {
                continue;
            }

            if (!primerCaracter)
            {
                // Espacio entre letras: 3 dits
                segmentos.Add(GenerarSpace(MuestrasPorDit * 3));
            }

            segmentos.Add(GenerarPatronMorse(patron));
            primerCaracter = false;
        }

        // Silencio largo al final para forzar la emision del mensaje
        segmentos.Add(GenerarSpace(MuestrasPorDit * 10));

        return Concatenar(segmentos.ToArray());
    }

    private static ConfiguracionCw CrearConfiguracion()
    {
        return new ConfiguracionCw
        {
            FrecuenciaTono = FrecuenciaTono,
            FrecuenciaMuestreo = FrecuenciaMuestreo,
            VelocidadInicialWpm = WpmPrueba,
            TamanoBloqueMilisegundos = 10,
            UmbralDeteccion = 0.3,
            FactorSuavizadoUmbral = 0.95
        };
    }

    private static async Task<string> DecodificarAudio(short[] audio, ConfiguracionCw? configuracion = null)
    {
        using DecodificadorCw decodificador = new(configuracion ?? CrearConfiguracion());
        await decodificador.IniciarAsync();

        MuestraAudio muestra = new MuestraAudio(
            datos: new ReadOnlyMemory<short>(audio),
            tasaDeMuestreoHz: FrecuenciaMuestreo,
            marcaDeTiempo: DateTimeOffset.UtcNow
        );

        IReadOnlyList<MensajeDecodificado> mensajes = await decodificador.ProcesarAudioAsync(muestra);
        await decodificador.DetenerAsync();

        // Combinar texto de mensajes emitidos + buffer residual
        string textoMensajes = string.Join("", mensajes.Select(m => m.Texto));
        string textoBuffer = decodificador.ObtenerTextoEnBuffer();

        return (textoMensajes + textoBuffer).Trim();
    }

    [Fact]
    public void Constructor_ConfiguracionPorDefecto_NoLanzaExcepcion()
    {
        // Act
        Action accion = () => { using DecodificadorCw _ = new(); };

        // Assert
        accion.Should().NotThrow();
    }

    [Fact]
    public void Modo_DevuelveCW()
    {
        // Arrange
        using DecodificadorCw decodificador = new();

        // Assert
        decodificador.Modo.Should().Be(ModoOperacion.CW);
    }

    [Fact]
    public void SubModosSoportados_ContienePCW()
    {
        // Arrange
        using DecodificadorCw decodificador = new();

        // Assert
        decodificador.SubModosSoportados.Should().Contain(SubModoOperacion.PCW);
    }

    [Fact]
    public async Task IniciarAsync_EstableceDEstaActivo()
    {
        // Arrange
        using DecodificadorCw decodificador = new();

        // Act
        await decodificador.IniciarAsync();

        // Assert
        decodificador.EstaActivo.Should().BeTrue();
    }

    [Fact]
    public async Task DetenerAsync_DesactivaElDecodificador()
    {
        // Arrange
        using DecodificadorCw decodificador = new();
        await decodificador.IniciarAsync();

        // Act
        await decodificador.DetenerAsync();

        // Assert
        decodificador.EstaActivo.Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarAudioAsync_Silencio_NoGeneraMensajes()
    {
        // Arrange
        using DecodificadorCw decodificador = new(CrearConfiguracion());
        await decodificador.IniciarAsync();
        short[] silencio = GenerarSpace(FrecuenciaMuestreo); // 1 segundo de silencio

        MuestraAudio muestra = new MuestraAudio(
            datos: new ReadOnlyMemory<short>(silencio),
            tasaDeMuestreoHz: FrecuenciaMuestreo,
            marcaDeTiempo: DateTimeOffset.UtcNow
        );

        // Act
        IReadOnlyList<MensajeDecodificado> mensajes = await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        mensajes.Should().BeEmpty("el silencio no debe generar mensajes");
    }

    [Fact]
    public async Task ProcesarAudioAsync_SinIniciar_DevuelveListaVacia()
    {
        // Arrange
        using DecodificadorCw decodificador = new(CrearConfiguracion());
        short[] audio = GenerarMark(MuestrasPorDit);

        MuestraAudio muestra = new MuestraAudio(
            datos: new ReadOnlyMemory<short>(audio),
            tasaDeMuestreoHz: FrecuenciaMuestreo,
            marcaDeTiempo: DateTimeOffset.UtcNow
        );

        // Act
        IReadOnlyList<MensajeDecodificado> mensajes = await decodificador.ProcesarAudioAsync(muestra);

        // Assert
        mensajes.Should().BeEmpty("el decodificador no esta activo");
    }

    [Fact]
    public void TablaMorse_ConvertirE_DevuelvePunto()
    {
        // E = .  (un dit)
        char? resultado = TablaMorse.ConvertirACaracter(".");

        resultado.Should().Be('E');
    }

    [Fact]
    public void TablaMorse_ConvertirT_DevuelveRaya()
    {
        // T = - (un dah)
        char? resultado = TablaMorse.ConvertirACaracter("-");

        resultado.Should().Be('T');
    }

    [Fact]
    public void TablaMorse_ConvertirSOS_PatronesCorrectos()
    {
        // S = ..., O = ---, S = ...
        TablaMorse.ConvertirACaracter("...").Should().Be('S');
        TablaMorse.ConvertirACaracter("---").Should().Be('O');
    }

    [Fact]
    public void TablaMorse_PatronInvalido_DevuelveNull()
    {
        char? resultado = TablaMorse.ConvertirACaracter("........");

        resultado.Should().BeNull();
    }

    [Fact]
    public void TablaMorse_ConvertirAMorse_LetraA_DevuelvePuntoRaya()
    {
        string? resultado = TablaMorse.ConvertirAMorse('A');

        resultado.Should().Be(".-");
    }

    [Fact]
    public void TablaMorse_EsPatronValido_PatronCorrecto_DevuelveTrue()
    {
        bool resultado = TablaMorse.EsPatronValido(".-");

        resultado.Should().BeTrue();
    }

    [Fact]
    public void TablaMorse_EsPatronValido_PatronIncorrecto_DevuelveFalse()
    {
        bool resultado = TablaMorse.EsPatronValido("........");

        resultado.Should().BeFalse();
    }

    [Fact]
    public void TablaMorse_TodosLosDigitos_SonValidos()
    {
        // Verificar que todos los digitos 0-9 tienen patron Morse
        for (char digito = '0'; digito <= '9'; digito++)
        {
            string? patron = TablaMorse.ConvertirAMorse(digito);
            patron.Should().NotBeNull($"el digito '{digito}' debe tener un patron Morse");
        }
    }

    [Fact]
    public void TablaMorse_TodasLasLetras_SonValidas()
    {
        // Verificar que todas las letras A-Z tienen patron Morse
        for (char letra = 'A'; letra <= 'Z'; letra++)
        {
            string? patron = TablaMorse.ConvertirAMorse(letra);
            patron.Should().NotBeNull($"la letra '{letra}' debe tener un patron Morse");
        }
    }

    [Fact]
    public async Task DecodificadorCw_Dispuesto_LanzaExcepcion()
    {
        // Arrange
        DecodificadorCw decodificador = new(CrearConfiguracion());
        decodificador.Dispose();

        // Act
        Func<Task> accion = () => decodificador.IniciarAsync();

        // Assert
        await accion.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void ObtenerVelocidadEstimadaWpm_ValorInicial_DevuelveVelocidadConfigurada()
    {
        // Arrange
        ConfiguracionCw config = CrearConfiguracion();
        using DecodificadorCw decodificador = new(config);

        // Act
        double wpm = decodificador.ObtenerVelocidadEstimadaWpm();

        // Assert
        wpm.Should().BeApproximately(WpmPrueba, 5.0, "la velocidad inicial debe estar cerca de la configurada");
    }

    [Fact]
    public void TasaDeMuestreoRequeridaHz_DevuelveFrecuenciaConfigurada()
    {
        // Arrange
        ConfiguracionCw config = CrearConfiguracion();
        using DecodificadorCw decodificador = new(config);

        // Assert
        decodificador.TasaDeMuestreoRequeridaHz.Should().Be(FrecuenciaMuestreo);
    }

    [Fact]
    public async Task FiltroGoertzel_DistingueTonoDeSilencio_EnContextoDecodificador()
    {
        // Arrange: generar un tono y un silencio
        short[] tono = GenerarMark(120); // 10ms de bloque a 12000 Hz
        short[] silencio = GenerarSpace(120);

        // Act
        double magnitudTono = FiltroGoertzel.CalcularMagnitud(tono, FrecuenciaTono, FrecuenciaMuestreo);
        double magnitudSilencio = FiltroGoertzel.CalcularMagnitud(silencio, FrecuenciaTono, FrecuenciaMuestreo);

        // Assert
        magnitudTono.Should().BeGreaterThan(magnitudSilencio * 100.0,
            "el filtro Goertzel debe distinguir claramente tono de silencio");

        await Task.CompletedTask;
    }
}
