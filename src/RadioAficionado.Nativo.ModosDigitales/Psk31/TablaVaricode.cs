namespace RadioAficionado.Nativo.ModosDigitales.Psk31;

/// <summary>
/// Tabla de codificacion Varicode para PSK31.
/// Varicode es un codigo de longitud variable donde los caracteres mas frecuentes
/// tienen codigos mas cortos. Los caracteres se separan por dos o mas bits cero consecutivos.
/// </summary>
public static class TablaVaricode
{
    /// <summary>
    /// Tabla Varicode completa para ASCII 0-127.
    /// Cada entrada es un array de bools representando los bits del codigo.
    /// </summary>
    private static readonly bool[][] Tabla = new bool[128][];

    static TablaVaricode()
    {
        // Inicializar tabla con codigos Varicode estandar PSK31
        // Formato: MSB primero, sin los dos ceros separadores finales
        string[] codigos = new string[128];

        codigos[0] = "1010101011";     // NUL
        codigos[1] = "1011011011";     // SOH
        codigos[2] = "1011101101";     // STX
        codigos[3] = "1101110111";     // ETX
        codigos[4] = "1011101011";     // EOT
        codigos[5] = "1101011111";     // ENQ
        codigos[6] = "1011101111";     // ACK
        codigos[7] = "1011111101";     // BEL
        codigos[8] = "1011111111";     // BS
        codigos[9] = "11101111";       // HT
        codigos[10] = "11101";         // LF
        codigos[11] = "1101101111";    // VT
        codigos[12] = "1011011101";    // FF
        codigos[13] = "11111";         // CR
        codigos[14] = "1101110101";    // SO
        codigos[15] = "1110101011";    // SI
        codigos[16] = "1011110111";    // DLE
        codigos[17] = "1011110101";    // DC1
        codigos[18] = "1110101101";    // DC2
        codigos[19] = "1110101111";    // DC3
        codigos[20] = "1101011011";    // DC4
        codigos[21] = "1101101011";    // NAK
        codigos[22] = "1101101101";    // SYN
        codigos[23] = "1101010111";    // ETB
        codigos[24] = "1101111011";    // CAN
        codigos[25] = "1101111101";    // EM
        codigos[26] = "1110110111";    // SUB
        codigos[27] = "1101010101";    // ESC
        codigos[28] = "1101011101";    // FS
        codigos[29] = "1110111011";    // GS
        codigos[30] = "1011111011";    // RS
        codigos[31] = "1101111111";    // US
        codigos[32] = "1";             // SP (espacio)
        codigos[33] = "111111111";     // !
        codigos[34] = "101011111";     // "
        codigos[35] = "111110101";     // #
        codigos[36] = "111011011";     // $
        codigos[37] = "1011010101";    // %
        codigos[38] = "1010111011";    // &
        codigos[39] = "101111111";     // '
        codigos[40] = "11111011";      // (
        codigos[41] = "11110111";      // )
        codigos[42] = "101101111";     // *
        codigos[43] = "111011111";     // +
        codigos[44] = "1110101";       // ,
        codigos[45] = "110101";        // -
        codigos[46] = "1010111";       // .
        codigos[47] = "110101111";     // /
        codigos[48] = "10110111";      // 0
        codigos[49] = "10111101";      // 1
        codigos[50] = "11101101";      // 2
        codigos[51] = "11111111";      // 3
        codigos[52] = "101110111";     // 4
        codigos[53] = "101011011";     // 5
        codigos[54] = "101101011";     // 6
        codigos[55] = "110101101";     // 7
        codigos[56] = "110101011";     // 8
        codigos[57] = "110110111";     // 9
        codigos[58] = "11110101";      // :
        codigos[59] = "110111101";     // ;
        codigos[60] = "111101101";     // <
        codigos[61] = "1010101";       // =
        codigos[62] = "111010111";     // >
        codigos[63] = "1010101111";    // ?
        codigos[64] = "1010111101";    // @
        codigos[65] = "1111101";       // A
        codigos[66] = "11101011";      // B
        codigos[67] = "10101101";      // C
        codigos[68] = "10110101";      // D
        codigos[69] = "1110111";       // E
        codigos[70] = "11011011";      // F
        codigos[71] = "11111101";      // G
        codigos[72] = "101010101";     // H
        codigos[73] = "1111111";       // I
        codigos[74] = "111111101";     // J
        codigos[75] = "101111101";     // K
        codigos[76] = "11010111";      // L
        codigos[77] = "10111011";      // M
        codigos[78] = "11011101";      // N
        codigos[79] = "10101011";      // O
        codigos[80] = "11010101";      // P
        codigos[81] = "111011101";     // Q
        codigos[82] = "10101111";      // R
        codigos[83] = "1101111";       // S
        codigos[84] = "1101101";       // T
        codigos[85] = "101010111";     // U
        codigos[86] = "110110101";     // V
        codigos[87] = "101011101";     // W
        codigos[88] = "101110101";     // X
        codigos[89] = "101111011";     // Y
        codigos[90] = "1010101101";    // Z
        codigos[91] = "111110111";     // [
        codigos[92] = "111101111";     // backslash
        codigos[93] = "111111011";     // ]
        codigos[94] = "1010111111";    // ^
        codigos[95] = "101101101";     // _
        codigos[96] = "1011011111";    // `
        codigos[97] = "1011";          // a
        codigos[98] = "1011111";       // b
        codigos[99] = "101111";        // c
        codigos[100] = "101101";       // d
        codigos[101] = "11";           // e
        codigos[102] = "111101";       // f
        codigos[103] = "1011011";      // g
        codigos[104] = "101011";       // h
        codigos[105] = "1101";         // i
        codigos[106] = "111101011";    // j
        codigos[107] = "10111111";     // k
        codigos[108] = "11011";        // l
        codigos[109] = "111011";       // m
        codigos[110] = "1111";         // n
        codigos[111] = "111";          // o
        codigos[112] = "111111";       // p
        codigos[113] = "110111111";    // q
        codigos[114] = "10101";        // r
        codigos[115] = "10111";        // s
        codigos[116] = "101";          // t
        codigos[117] = "110111";       // u
        codigos[118] = "1111011";      // v
        codigos[119] = "1101011";      // w
        codigos[120] = "11011111";     // x
        codigos[121] = "1011101";      // y
        codigos[122] = "111010101";    // z
        codigos[123] = "1010110111";   // {
        codigos[124] = "110111011";    // |
        codigos[125] = "1010110101";   // }
        codigos[126] = "1011010111";   // ~
        codigos[127] = "1110110101";   // DEL

        for (int i = 0; i < 128; i++)
        {
            string codigo = codigos[i];
            Tabla[i] = new bool[codigo.Length];
            for (int j = 0; j < codigo.Length; j++)
            {
                Tabla[i][j] = codigo[j] == '1';
            }
        }
    }

    /// <summary>
    /// Decodifica una secuencia de bits Varicode a su caracter ASCII correspondiente.
    /// Los bits no deben incluir los ceros separadores.
    /// </summary>
    /// <param name="bits">Secuencia de bits del codigo Varicode.</param>
    /// <returns>El caracter decodificado, o null si no se encuentra coincidencia.</returns>
    public static char? DecodificarVaricode(IReadOnlyList<bool> bits)
    {
        if (bits is null || bits.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < 128; i++)
        {
            bool[] codigo = Tabla[i];
            if (codigo.Length != bits.Count)
            {
                continue;
            }

            bool coincide = true;
            for (int j = 0; j < codigo.Length; j++)
            {
                if (codigo[j] != bits[j])
                {
                    coincide = false;
                    break;
                }
            }

            if (coincide)
            {
                return (char)i;
            }
        }

        return null;
    }

    /// <summary>
    /// Obtiene el codigo Varicode para un caracter ASCII.
    /// </summary>
    /// <param name="caracter">Caracter ASCII (0-127).</param>
    /// <returns>Array de bits del codigo Varicode, o null si el caracter esta fuera de rango.</returns>
    public static bool[]? ObtenerCodigo(char caracter)
    {
        int indice = (int)caracter;
        if (indice < 0 || indice >= 128)
        {
            return null;
        }

        bool[] codigo = Tabla[indice];
        bool[] copia = new bool[codigo.Length];
        Array.Copy(codigo, copia, codigo.Length);
        return copia;
    }
}
