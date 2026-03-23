namespace RadioAficionado.Dominio.Propagacion;

/// <summary>
/// Nivel de calidad de propagacion para una banda HF.
/// </summary>
public enum NivelPropagacion
{
    /// <summary>Sin propagacion posible en esta banda.</summary>
    Nulo = 0,

    /// <summary>Propagacion muy debil, senales apenas audibles.</summary>
    Pobre = 1,

    /// <summary>Propagacion intermitente, contactos posibles con paciencia.</summary>
    Regular = 2,

    /// <summary>Propagacion confiable, buenas senales.</summary>
    Bueno = 3,

    /// <summary>Propagacion excelente, senales fuertes y estables.</summary>
    Excelente = 4
}
