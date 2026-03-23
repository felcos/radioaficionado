using System.Globalization;
using RadioAficionado.Dominio.Aprs;
using RadioAficionado.Dominio.ObjetosDeValor;

namespace RadioAficionado.Infraestructura.Aprs;

/// <summary>
/// Parser de paquetes APRS (Automatic Packet Reporting System).
/// Convierte líneas de texto del protocolo APRS-IS en objetos del dominio.
/// Soporta formatos de posición comprimido y sin comprimir, mensajes y objetos.
/// </summary>
public static class ParserAprs
{
    /// <summary>
    /// Caracteres base91 usados en el formato de posición comprimida APRS.
    /// </summary>
    private const int Base91Offset = 33;

    /// <summary>
    /// Parsea una línea cruda de texto APRS-IS y devuelve un <see cref="PaqueteAprs"/>.
    /// </summary>
    /// <param name="lineaCruda">Línea de texto recibida del servidor APRS-IS.</param>
    /// <returns>El paquete APRS parseado, o null si la línea no es un paquete válido.</returns>
    public static PaqueteAprs? ParsearPaquete(string lineaCruda)
    {
        if (string.IsNullOrWhiteSpace(lineaCruda))
        {
            return null;
        }

        // Ignorar comentarios del servidor (empiezan con #)
        if (lineaCruda.StartsWith('#'))
        {
            return null;
        }

        // Formato: ORIGEN>DESTINO,RUTA:CONTENIDO
        int indiceDosPuntos = lineaCruda.IndexOf(':');
        if (indiceDosPuntos < 0)
        {
            return null;
        }

        string cabecera = lineaCruda[..indiceDosPuntos];
        string contenido = lineaCruda[(indiceDosPuntos + 1)..];

        // Separar origen del resto
        int indiceFlechaMayor = cabecera.IndexOf('>');
        if (indiceFlechaMayor < 0)
        {
            return null;
        }

        string origenTexto = cabecera[..indiceFlechaMayor].Trim();
        string restoRuta = cabecera[(indiceFlechaMayor + 1)..];

        if (string.IsNullOrWhiteSpace(origenTexto))
        {
            return null;
        }

        // Separar destino y ruta
        string[] partesRuta = restoRuta.Split(',');
        string destino = partesRuta[0].Trim();
        List<string> ruta = new();

        for (int i = 1; i < partesRuta.Length; i++)
        {
            string segmento = partesRuta[i].Trim();
            if (!string.IsNullOrWhiteSpace(segmento))
            {
                ruta.Add(segmento);
            }
        }

        TipoPaqueteAprs tipoPaquete = IdentificarTipoPaquete(contenido);

        Indicativo origen;
        try
        {
            origen = new Indicativo(origenTexto);
        }
        catch (ArgumentException)
        {
            return null;
        }

        return new PaqueteAprs(
            origen,
            destino,
            ruta.AsReadOnly(),
            tipoPaquete,
            contenido,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Identifica el tipo de paquete APRS basándose en el primer carácter del campo de información.
    /// </summary>
    /// <param name="contenido">Campo de información del paquete APRS.</param>
    /// <returns>El tipo de paquete identificado.</returns>
    public static TipoPaqueteAprs IdentificarTipoPaquete(string contenido)
    {
        if (string.IsNullOrEmpty(contenido))
        {
            return TipoPaqueteAprs.Estado;
        }

        char identificador = contenido[0];

        return identificador switch
        {
            '!' or '=' or '/' or '@' => TipoPaqueteAprs.Posicion,
            ':' => ContieneFormatoMensaje(contenido) ? TipoPaqueteAprs.Mensaje : TipoPaqueteAprs.Estado,
            ';' => TipoPaqueteAprs.Objeto,
            ')' => TipoPaqueteAprs.Estacion,
            'T' => TipoPaqueteAprs.Telemetria,
            '>' => TipoPaqueteAprs.Estado,
            '?' => TipoPaqueteAprs.Consulta,
            _ => TipoPaqueteAprs.Estado
        };
    }

    /// <summary>
    /// Parsea la información de posición de un paquete APRS.
    /// Soporta tanto formato sin comprimir como formato comprimido.
    /// </summary>
    /// <param name="contenido">Campo de información del paquete con datos de posición.</param>
    /// <returns>La posición parseada, o null si no se puede interpretar.</returns>
    public static PosicionAprs? ParsearPosicion(string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido) || contenido.Length < 2)
        {
            return null;
        }

        char identificador = contenido[0];

        // Saltar el identificador de tipo y opcionalmente el timestamp
        string datoPosicion;
        if (identificador == '/' || identificador == '@')
        {
            // Con timestamp: /HHMMSSh o @HHMMSSz seguido de posición
            if (contenido.Length < 8)
            {
                return null;
            }
            datoPosicion = contenido[8..];
        }
        else
        {
            // Sin timestamp: ! o = seguido directamente de posición
            datoPosicion = contenido[1..];
        }

        if (string.IsNullOrEmpty(datoPosicion))
        {
            return null;
        }

        // Detectar formato comprimido: empieza con un carácter de tabla de símbolos seguido de 4 chars base91
        if (datoPosicion.Length >= 13 && (datoPosicion[0] == '/' || datoPosicion[0] == '\\'))
        {
            bool esComprimido = true;
            for (int i = 1; i <= 8; i++)
            {
                if (datoPosicion[i] < '!' || datoPosicion[i] > '{')
                {
                    esComprimido = false;
                    break;
                }
            }

            if (esComprimido)
            {
                return ParsearPosicionComprimida(datoPosicion);
            }
        }

        return ParsearPosicionSinComprimir(datoPosicion);
    }

    /// <summary>
    /// Parsea una posición APRS en formato sin comprimir.
    /// Formato: "DDMM.MMN/DDDMM.MMW-" donde la tabla y símbolo están entre lat y lon.
    /// </summary>
    /// <param name="dato">Cadena de datos de posición sin el identificador de tipo.</param>
    /// <returns>La posición parseada, o null si el formato no es válido.</returns>
    public static PosicionAprs? ParsearPosicionSinComprimir(string dato)
    {
        // Formato mínimo: 4041.00N/00342.00W- (19 caracteres)
        if (dato.Length < 19)
        {
            return null;
        }

        // Parsear latitud: DDMM.MMN
        string latitudTexto = dato[..8];
        char hemisferioNs = latitudTexto[7];

        if (hemisferioNs != 'N' && hemisferioNs != 'S')
        {
            return null;
        }

        if (!double.TryParse(latitudTexto[..2], NumberStyles.Integer, CultureInfo.InvariantCulture, out double latitudGrados))
        {
            return null;
        }

        if (!double.TryParse(latitudTexto[2..7], NumberStyles.Float, CultureInfo.InvariantCulture, out double latitudMinutos))
        {
            return null;
        }

        double latitud = latitudGrados + (latitudMinutos / 60.0);
        if (hemisferioNs == 'S')
        {
            latitud = -latitud;
        }

        // Tabla de símbolos (carácter entre latitud y longitud)
        char tabla = dato[8];

        // Parsear longitud: DDDMM.MMW
        string longitudTexto = dato[9..18];
        char hemisferioEw = longitudTexto[8];

        if (hemisferioEw != 'E' && hemisferioEw != 'W')
        {
            return null;
        }

        if (!double.TryParse(longitudTexto[..3], NumberStyles.Integer, CultureInfo.InvariantCulture, out double longitudGrados))
        {
            return null;
        }

        if (!double.TryParse(longitudTexto[3..8], NumberStyles.Float, CultureInfo.InvariantCulture, out double longitudMinutos))
        {
            return null;
        }

        double longitud = longitudGrados + (longitudMinutos / 60.0);
        if (hemisferioEw == 'W')
        {
            longitud = -longitud;
        }

        // Símbolo APRS
        char simbolo = dato.Length > 18 ? dato[18] : '-';

        // Parsear extensiones opcionales (velocidad/rumbo) y comentario
        double? velocidad = null;
        double? rumbo = null;
        double? altitud = null;
        string? comentario = null;

        if (dato.Length > 19)
        {
            string extension = dato[19..];
            (velocidad, rumbo, altitud, comentario) = ParsearExtensiones(extension);
        }

        Coordenadas coordenadas = new(latitud, longitud);

        return new PosicionAprs(coordenadas, simbolo, tabla, velocidad, rumbo, altitud, comentario);
    }

    /// <summary>
    /// Parsea una posición APRS en formato comprimido (base91).
    /// Formato: "/YYYY XXXX CS T" donde cada grupo son caracteres base91.
    /// </summary>
    /// <param name="dato">Cadena de datos de posición comprimida.</param>
    /// <returns>La posición parseada, o null si el formato no es válido.</returns>
    public static PosicionAprs? ParsearPosicionComprimida(string dato)
    {
        if (dato.Length < 13)
        {
            return null;
        }

        char tabla = dato[0];

        // Decodificar latitud (4 caracteres base91)
        int lat1 = dato[1] - Base91Offset;
        int lat2 = dato[2] - Base91Offset;
        int lat3 = dato[3] - Base91Offset;
        int lat4 = dato[4] - Base91Offset;

        double latitud = 90.0 - ((lat1 * 753571) + (lat2 * 8281) + (lat3 * 91) + lat4) / 380926.0;

        // Decodificar longitud (4 caracteres base91)
        int lon1 = dato[5] - Base91Offset;
        int lon2 = dato[6] - Base91Offset;
        int lon3 = dato[7] - Base91Offset;
        int lon4 = dato[8] - Base91Offset;

        double longitud = -180.0 + ((lon1 * 753571) + (lon2 * 8281) + (lon3 * 91) + lon4) / 190463.0;

        char simbolo = dato[9];

        // Bytes de extensión comprimida
        char c = dato[10];
        char s = dato[11];
        char t = dato[12];

        double? velocidad = null;
        double? rumbo = null;
        double? altitud = null;

        int tipoByte = (t - Base91Offset) & 0x18;

        if (c != ' ' && s != ' ')
        {
            if (tipoByte == 0x18)
            {
                // Altitud
                int valorAltitud = ((c - Base91Offset) * 91) + (s - Base91Offset);
                altitud = Math.Pow(1.002, valorAltitud) * 0.3048; // pies a metros
            }
            else if (c >= '!' && c <= 'z')
            {
                // Rumbo y velocidad
                rumbo = (c - Base91Offset) * 4.0;
                velocidad = Math.Pow(1.08, s - Base91Offset) - 1.0;
            }
        }

        string? comentario = dato.Length > 13 ? dato[13..].Trim() : null;
        if (string.IsNullOrWhiteSpace(comentario))
        {
            comentario = null;
        }

        Coordenadas coordenadas = new(latitud, longitud);

        return new PosicionAprs(coordenadas, simbolo, tabla, velocidad, rumbo, altitud, comentario);
    }

    /// <summary>
    /// Parsea un mensaje APRS desde el campo de información.
    /// Formato: ":DEST     :Texto del mensaje{NNN"
    /// </summary>
    /// <param name="contenido">Campo de información completo del paquete.</param>
    /// <returns>El mensaje parseado, o null si el formato no es válido.</returns>
    public static MensajeAprs? ParsearMensaje(string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido) || contenido.Length < 11)
        {
            return null;
        }

        // El formato empieza con ':' seguido de 9 caracteres de destinatario y otro ':'
        if (contenido[0] != ':')
        {
            return null;
        }

        // El destinatario ocupa 9 caracteres (rellenado con espacios)
        if (contenido.Length < 11 || contenido[10] != ':')
        {
            return null;
        }

        string destinatarioTexto = contenido[1..10].Trim();

        if (string.IsNullOrWhiteSpace(destinatarioTexto))
        {
            return null;
        }

        string restoMensaje = contenido[11..];

        // Separar número de mensaje si existe (formato: texto{NNN)
        string? numeroMensaje = null;
        string texto = restoMensaje;

        int indiceLlave = restoMensaje.LastIndexOf('{');
        if (indiceLlave >= 0)
        {
            texto = restoMensaje[..indiceLlave];
            numeroMensaje = restoMensaje[(indiceLlave + 1)..];
        }

        Indicativo destinatario;
        try
        {
            destinatario = new Indicativo(destinatarioTexto);
        }
        catch (ArgumentException)
        {
            return null;
        }

        return new MensajeAprs(destinatario, texto, numeroMensaje);
    }

    /// <summary>
    /// Parsea un objeto APRS desde el campo de información.
    /// Formato: ";NombreObj*DDMM.MMN/DDDMM.MMW-comentario" (vivo) o ";NombreObj_..." (eliminado).
    /// </summary>
    /// <param name="contenido">Campo de información completo del paquete.</param>
    /// <returns>El objeto parseado, o null si el formato no es válido.</returns>
    public static ObjetoAprs? ParsearObjeto(string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido) || contenido.Length < 11)
        {
            return null;
        }

        if (contenido[0] != ';')
        {
            return null;
        }

        // Nombre del objeto: 9 caracteres
        string nombre = contenido[1..10].Trim();

        // Carácter vivo/eliminado
        char estadoObjeto = contenido[10];
        bool vivo = estadoObjeto == '*';

        if (estadoObjeto != '*' && estadoObjeto != '_')
        {
            return null;
        }

        // El resto contiene posición (opcionalmente con timestamp)
        string restoPosicion = contenido[11..];

        // Intentar parsear posición sin comprimir
        PosicionAprs? posicion = null;

        // Verificar si hay timestamp (7 chars: DDHHMMz o similar)
        if (restoPosicion.Length >= 7 && char.IsDigit(restoPosicion[0]))
        {
            // Saltar timestamp de 7 caracteres
            posicion = ParsearPosicionSinComprimir(restoPosicion[7..]);
        }

        posicion ??= ParsearPosicionSinComprimir(restoPosicion);

        if (posicion is null)
        {
            return null;
        }

        return new ObjetoAprs(nombre, posicion.Coordenadas, vivo, posicion.Comentario);
    }

    /// <summary>
    /// Calcula el passcode APRS a partir de un indicativo.
    /// El passcode es un hash de 15 bits del indicativo (sin modificadores /P, /M, etc.)
    /// usado para autenticarse en servidores APRS-IS.
    /// </summary>
    /// <param name="indicativo">Indicativo de radioaficionado.</param>
    /// <returns>El passcode APRS (valor entre 0 y 32767).</returns>
    public static int CalcularPasscode(string indicativo)
    {
        if (string.IsNullOrWhiteSpace(indicativo))
        {
            return -1;
        }

        // Usar solo la parte base del indicativo (sin /P, /M, etc.)
        string indicativoBase = indicativo.ToUpperInvariant();
        int indiceBarraOblicua = indicativoBase.IndexOf('-');
        if (indiceBarraOblicua >= 0)
        {
            indicativoBase = indicativoBase[..indiceBarraOblicua];
        }

        int indiceSlash = indicativoBase.IndexOf('/');
        if (indiceSlash >= 0)
        {
            indicativoBase = indicativoBase[..indiceSlash];
        }

        int hash = 0x73e2; // Semilla inicial del algoritmo APRS passcode
        bool esIndiceImpar = true;

        for (int i = 0; i < indicativoBase.Length; i++)
        {
            if (esIndiceImpar)
            {
                hash ^= indicativoBase[i] << 8;
            }
            else
            {
                hash ^= indicativoBase[i];
            }

            esIndiceImpar = !esIndiceImpar;
        }

        return hash & 0x7FFF;
    }

    /// <summary>
    /// Determina si un campo de información APRS contiene un mensaje dirigido.
    /// </summary>
    /// <param name="contenido">Campo de información del paquete.</param>
    /// <returns>True si el contenido tiene formato de mensaje APRS.</returns>
    private static bool ContieneFormatoMensaje(string contenido)
    {
        // Formato mensaje: ":DEST     :texto"
        // Mínimo 11 caracteres: ':' + 9 chars destinatario + ':'
        if (contenido.Length < 11)
        {
            return false;
        }

        return contenido[0] == ':' && contenido[10] == ':';
    }

    /// <summary>
    /// Parsea las extensiones opcionales de un paquete de posición (velocidad, rumbo, altitud, comentario).
    /// </summary>
    /// <param name="extension">Texto después del símbolo APRS en una posición sin comprimir.</param>
    /// <returns>Tupla con velocidad, rumbo, altitud y comentario extraídos.</returns>
    private static (double? Velocidad, double? Rumbo, double? Altitud, string? Comentario) ParsearExtensiones(string extension)
    {
        double? velocidad = null;
        double? rumbo = null;
        double? altitud = null;
        string? comentario = null;

        // Formato de velocidad/rumbo: CCC/SSS donde CCC=rumbo, SSS=velocidad en nudos
        if (extension.Length >= 7 && extension[3] == '/')
        {
            if (double.TryParse(extension[..3], NumberStyles.Integer, CultureInfo.InvariantCulture, out double rumboValor) &&
                double.TryParse(extension[4..7], NumberStyles.Integer, CultureInfo.InvariantCulture, out double velocidadValor))
            {
                rumbo = rumboValor;
                velocidad = velocidadValor;
                comentario = extension.Length > 7 ? extension[7..].Trim() : null;
            }
            else
            {
                comentario = extension.Trim();
            }
        }
        else
        {
            comentario = extension.Trim();
        }

        // Buscar altitud en el comentario: /A=NNNNNN
        if (!string.IsNullOrEmpty(comentario))
        {
            int indiceAltitud = comentario.IndexOf("/A=", StringComparison.Ordinal);
            if (indiceAltitud >= 0 && comentario.Length >= indiceAltitud + 9)
            {
                string altitudTexto = comentario[(indiceAltitud + 3)..(indiceAltitud + 9)];
                if (double.TryParse(altitudTexto, NumberStyles.Integer, CultureInfo.InvariantCulture, out double altitudPies))
                {
                    altitud = altitudPies * 0.3048; // Convertir pies a metros
                }
            }
        }

        if (string.IsNullOrWhiteSpace(comentario))
        {
            comentario = null;
        }

        return (velocidad, rumbo, altitud, comentario);
    }
}
