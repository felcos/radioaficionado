namespace RadioAficionado.Compartido.Excepciones;

/// <summary>
/// Excepción lanzada cuando una validación de datos falla.
/// </summary>
public class ExcepcionDeValidacion : Exception
{
    /// <summary>
    /// Crea una nueva instancia de <see cref="ExcepcionDeValidacion"/>.
    /// </summary>
    /// <param name="mensaje">Mensaje descriptivo del error de validación.</param>
    public ExcepcionDeValidacion(string mensaje) : base(mensaje)
    {
    }

    /// <summary>
    /// Crea una nueva instancia de <see cref="ExcepcionDeValidacion"/> con una excepción interna.
    /// </summary>
    /// <param name="mensaje">Mensaje descriptivo del error de validación.</param>
    /// <param name="excepcionInterna">Excepción que causó este error.</param>
    public ExcepcionDeValidacion(string mensaje, Exception excepcionInterna)
        : base(mensaje, excepcionInterna)
    {
    }
}
