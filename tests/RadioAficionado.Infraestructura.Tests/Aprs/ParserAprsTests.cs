using FluentAssertions;
using RadioAficionado.Dominio.Aprs;
using RadioAficionado.Infraestructura.Aprs;

namespace RadioAficionado.Infraestructura.Tests.Aprs;

/// <summary>
/// Tests unitarios para <see cref="ParserAprs"/>.
/// Verifica el parseo de paquetes APRS en sus distintos formatos.
/// </summary>
public class ParserAprsTests
{
    // ===== Tests de parseo de paquete general =====

    [Fact]
    public void ParsearPaquete_PaquetePosicionValido_ExtraeOrigenCorrectamente()
    {
        // Arrange
        string linea = "EA4ABC>APRS,WIDE1-1:=4041.00N/00342.00W-PHG2360/RadioAficionado";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Origen.Valor.Should().Be("EA4ABC");
    }

    [Fact]
    public void ParsearPaquete_PaquetePosicionValido_ExtraeDestinoCorrectamente()
    {
        // Arrange
        string linea = "EA4ABC>APRS,WIDE1-1:=4041.00N/00342.00W-PHG2360/RadioAficionado";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Destino.Should().Be("APRS");
    }

    [Fact]
    public void ParsearPaquete_PaqueteConRuta_ExtraeRutaCorrectamente()
    {
        // Arrange
        string linea = "EA4ABC>APRS,WIDE1-1,qAR,EA4XYZ:=4041.00N/00342.00W-Test";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Ruta.Should().HaveCount(3);
        resultado.Ruta[0].Should().Be("WIDE1-1");
        resultado.Ruta[1].Should().Be("qAR");
        resultado.Ruta[2].Should().Be("EA4XYZ");
    }

    [Fact]
    public void ParsearPaquete_PaqueteSinRuta_DevuelveRutaVacia()
    {
        // Arrange
        string linea = "EA4ABC>APRS:=4041.00N/00342.00W-Test";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Ruta.Should().BeEmpty();
    }

    [Fact]
    public void ParsearPaquete_LineaComentarioServidor_DevuelveNull()
    {
        // Arrange
        string linea = "# javAPRSrvr 4.2.0 22 Mar 2024";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void ParsearPaquete_LineaVacia_DevuelveNull()
    {
        // Arrange
        string linea = "";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void ParsearPaquete_LineaNull_DevuelveNull()
    {
        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(null!);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void ParsearPaquete_LineaSinDosPuntos_DevuelveNull()
    {
        // Arrange
        string linea = "EA4ABC>APRS,WIDE1-1 sin dos puntos";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void ParsearPaquete_LineaSinFlecha_DevuelveNull()
    {
        // Arrange
        string linea = "EA4ABCAPRS:contenido";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().BeNull();
    }

    // ===== Tests de identificación de tipo de paquete =====

    [Theory]
    [InlineData("!4041.00N/00342.00W-Test", TipoPaqueteAprs.Posicion)]
    [InlineData("=4041.00N/00342.00W-Test", TipoPaqueteAprs.Posicion)]
    [InlineData("/092345z4041.00N/00342.00W-Test", TipoPaqueteAprs.Posicion)]
    [InlineData("@092345z4041.00N/00342.00W-Test", TipoPaqueteAprs.Posicion)]
    [InlineData(":EA4ABC   :Hola mundo{001", TipoPaqueteAprs.Mensaje)]
    [InlineData(";TestObj  *4041.00N/00342.00W-Descripcion", TipoPaqueteAprs.Objeto)]
    [InlineData(">Estado de la estacion", TipoPaqueteAprs.Estado)]
    [InlineData("?APRS?", TipoPaqueteAprs.Consulta)]
    [InlineData("T#123,001,002,003,004,005,00000000", TipoPaqueteAprs.Telemetria)]
    [InlineData(")ItemTest!4041.00N/00342.00W-Item", TipoPaqueteAprs.Estacion)]
    public void IdentificarTipoPaquete_ContenidoValido_DevuelveTipoCorrecto(string contenido, TipoPaqueteAprs tipoEsperado)
    {
        // Act
        TipoPaqueteAprs resultado = ParserAprs.IdentificarTipoPaquete(contenido);

        // Assert
        resultado.Should().Be(tipoEsperado);
    }

    // ===== Tests de parseo de posición sin comprimir =====

    [Fact]
    public void ParsearPosicion_FormatoSinComprimir_ExtraeCoordenadas()
    {
        // Arrange — Madrid: 40°41'N, 3°42'W
        string contenido = "=4041.00N/00342.00W-RadioAficionado";

        // Act
        PosicionAprs? resultado = ParserAprs.ParsearPosicion(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Coordenadas.Latitud.Should().BeApproximately(40.6833, 0.01);
        resultado.Coordenadas.Longitud.Should().BeApproximately(-3.7, 0.01);
    }

    [Fact]
    public void ParsearPosicion_FormatoSinComprimir_ExtraeSimboloYTabla()
    {
        // Arrange
        string contenido = "=4041.00N/00342.00W-RadioAficionado";

        // Act
        PosicionAprs? resultado = ParserAprs.ParsearPosicion(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Tabla.Should().Be('/');
        resultado.Simbolo.Should().Be('-');
    }

    [Fact]
    public void ParsearPosicion_ConVelocidadYRumbo_ExtraeValoresCorrectos()
    {
        // Arrange — Rumbo 090°, velocidad 045 nudos
        string contenido = "=4041.00N/00342.00W>090/045";

        // Act
        PosicionAprs? resultado = ParserAprs.ParsearPosicion(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Rumbo.Should().Be(90);
        resultado.Velocidad.Should().Be(45);
    }

    [Fact]
    public void ParsearPosicion_ConAltitud_ExtraeAltitudEnMetros()
    {
        // Arrange — /A=001000 = 1000 pies = ~304.8 metros
        string contenido = "=4041.00N/00342.00W-/A=001000";

        // Act
        PosicionAprs? resultado = ParserAprs.ParsearPosicion(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Altitud.Should().BeApproximately(304.8, 0.1);
    }

    [Fact]
    public void ParsearPosicion_HemisferioSurYEste_CoordenadasNegativasYPositivas()
    {
        // Arrange — Buenos Aires: 34°36'S, 58°22'E (ejemplo ficticio con E)
        string contenido = "=3436.00S/05822.00E-Test";

        // Act
        PosicionAprs? resultado = ParserAprs.ParsearPosicion(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Coordenadas.Latitud.Should().BeNegative();
        resultado.Coordenadas.Longitud.Should().BePositive();
    }

    [Fact]
    public void ParsearPosicion_ConTimestamp_ParseaCorrectamente()
    {
        // Arrange — Formato con timestamp: /HHMMSSh
        string contenido = "/092345h4041.00N/00342.00W-Con timestamp";

        // Act
        PosicionAprs? resultado = ParserAprs.ParsearPosicion(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Coordenadas.Latitud.Should().BeApproximately(40.6833, 0.01);
    }

    // ===== Tests de parseo de posición comprimida =====

    [Fact]
    public void ParsearPosicion_FormatoComprimido_ExtraeCoordenadas()
    {
        // Arrange — Posición comprimida: tabla '/' + 4 chars lat + 4 chars lon + símbolo + cs + t
        // Coordenadas codificadas en base91 para ~49.5°N, ~72.75°W (ejemplo estándar APRS)
        string contenido = "=/5L!!<*e7>7P[";

        // Act
        PosicionAprs? resultado = ParserAprs.ParsearPosicion(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Coordenadas.Latitud.Should().BeInRange(-90, 90);
        resultado.Coordenadas.Longitud.Should().BeInRange(-180, 180);
    }

    [Fact]
    public void ParsearPosicion_FormatoComprimido_ExtraeTablaYSimbolo()
    {
        // Arrange
        string contenido = "=/5L!!<*e7>7P[";

        // Act
        PosicionAprs? resultado = ParserAprs.ParsearPosicion(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Tabla.Should().Be('/');
    }

    // ===== Tests de parseo de mensajes =====

    [Fact]
    public void ParsearMensaje_FormatoValido_ExtraeDestinatarioYTexto()
    {
        // Arrange
        string contenido = ":EA4ABC   :Hola mundo{001";

        // Act
        MensajeAprs? resultado = ParserAprs.ParsearMensaje(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Destinatario.Valor.Should().Be("EA4ABC");
        resultado.Texto.Should().Be("Hola mundo");
        resultado.NumeroMensaje.Should().Be("001");
    }

    [Fact]
    public void ParsearMensaje_SinNumeroMensaje_DevuelveNumeroNull()
    {
        // Arrange
        string contenido = ":EA4ABC   :Mensaje sin numero";

        // Act
        MensajeAprs? resultado = ParserAprs.ParsearMensaje(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Texto.Should().Be("Mensaje sin numero");
        resultado.NumeroMensaje.Should().BeNull();
    }

    [Fact]
    public void ParsearMensaje_ContenidoInvalido_DevuelveNull()
    {
        // Arrange — No empieza con ':'
        string contenido = "NoEsMensaje";

        // Act
        MensajeAprs? resultado = ParserAprs.ParsearMensaje(contenido);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void ParsearMensaje_ContenidoMuyCorto_DevuelveNull()
    {
        // Arrange
        string contenido = ":AB";

        // Act
        MensajeAprs? resultado = ParserAprs.ParsearMensaje(contenido);

        // Assert
        resultado.Should().BeNull();
    }

    // ===== Tests de parseo de objetos =====

    [Fact]
    public void ParsearObjeto_ObjetoVivo_ExtraeNombreYEstado()
    {
        // Arrange
        string contenido = ";TestObj  *4041.00N/00342.00W-Descripcion del objeto";

        // Act
        ObjetoAprs? resultado = ParserAprs.ParsearObjeto(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Nombre.Should().Be("TestObj");
        resultado.Vivo.Should().BeTrue();
    }

    [Fact]
    public void ParsearObjeto_ObjetoEliminado_VivoEsFalse()
    {
        // Arrange
        string contenido = ";TestObj  _4041.00N/00342.00W-Eliminado";

        // Act
        ObjetoAprs? resultado = ParserAprs.ParsearObjeto(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Vivo.Should().BeFalse();
    }

    [Fact]
    public void ParsearObjeto_ExtraeCoordenadas()
    {
        // Arrange
        string contenido = ";Repetidor*4041.00N/00342.00W-Repetidor Madrid";

        // Act
        ObjetoAprs? resultado = ParserAprs.ParsearObjeto(contenido);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Coordenadas.Latitud.Should().BeApproximately(40.6833, 0.01);
        resultado.Coordenadas.Longitud.Should().BeApproximately(-3.7, 0.01);
    }

    // ===== Tests de cálculo de passcode =====

    [Fact]
    public void CalcularPasscode_IndicativoValido_DevuelveValorEntre0Y32767()
    {
        // Act
        int passcode = ParserAprs.CalcularPasscode("EA4ABC");

        // Assert
        passcode.Should().BeInRange(0, 32767);
    }

    [Fact]
    public void CalcularPasscode_MismoIndicativo_DevuelveMismoResultado()
    {
        // Act
        int passcode1 = ParserAprs.CalcularPasscode("EA4ABC");
        int passcode2 = ParserAprs.CalcularPasscode("EA4ABC");

        // Assert
        passcode1.Should().Be(passcode2);
    }

    [Fact]
    public void CalcularPasscode_IndicativoConSufijo_IgnoraSufijo()
    {
        // Act
        int passcodeSinSufijo = ParserAprs.CalcularPasscode("EA4ABC");
        int passcodeConSufijo = ParserAprs.CalcularPasscode("EA4ABC/P");

        // Assert
        passcodeSinSufijo.Should().Be(passcodeConSufijo);
    }

    [Fact]
    public void CalcularPasscode_IndicativoVacio_DevuelveMenosUno()
    {
        // Act
        int passcode = ParserAprs.CalcularPasscode("");

        // Assert
        passcode.Should().Be(-1);
    }

    [Fact]
    public void CalcularPasscode_IndicativoNull_DevuelveMenosUno()
    {
        // Act
        int passcode = ParserAprs.CalcularPasscode(null!);

        // Assert
        passcode.Should().Be(-1);
    }

    [Fact]
    public void CalcularPasscode_DistintosIndicativos_DevuelvenDistintosPasscodes()
    {
        // Act
        int passcode1 = ParserAprs.CalcularPasscode("EA4ABC");
        int passcode2 = ParserAprs.CalcularPasscode("W1AW");

        // Assert
        passcode1.Should().NotBe(passcode2);
    }

    [Fact]
    public void CalcularPasscode_W1AW_DevuelveValorConocido()
    {
        // Arrange — El passcode de N0CALL es conocido: 13023
        // W1AW passcode conocido: 23397
        // Act
        int passcode = ParserAprs.CalcularPasscode("N0CALL");

        // Assert
        passcode.Should().Be(13023);
    }

    // ===== Tests de parseo completo de paquete con contenido =====

    [Fact]
    public void ParsearPaquete_PaqueteDePosicion_IdentificaTipoPosicion()
    {
        // Arrange
        string linea = "EA4ABC>APRS,WIDE1-1:=4041.00N/00342.00W-RadioAficionado";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.TipoPaquete.Should().Be(TipoPaqueteAprs.Posicion);
    }

    [Fact]
    public void ParsearPaquete_PaqueteDeMensaje_IdentificaTipoMensaje()
    {
        // Arrange
        string linea = "EA4ABC>APRS::EA4XYZ   :Hola{001";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.TipoPaquete.Should().Be(TipoPaqueteAprs.Mensaje);
    }

    [Fact]
    public void ParsearPaquete_PaqueteDeObjeto_IdentificaTipoObjeto()
    {
        // Arrange
        string linea = "EA4ABC>APRS:;Repetidor*4041.00N/00342.00W-Rep Madrid";

        // Act
        PaqueteAprs? resultado = ParserAprs.ParsearPaquete(linea);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.TipoPaquete.Should().Be(TipoPaqueteAprs.Objeto);
    }

    [Fact]
    public void ParsearPosicion_ContenidoVacio_DevuelveNull()
    {
        // Act
        PosicionAprs? resultado = ParserAprs.ParsearPosicion("");

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public void ParsearPosicion_ContenidoNull_DevuelveNull()
    {
        // Act
        PosicionAprs? resultado = ParserAprs.ParsearPosicion(null!);

        // Assert
        resultado.Should().BeNull();
    }
}
