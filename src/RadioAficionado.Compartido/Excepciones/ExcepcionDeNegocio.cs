namespace RadioAficionado.Compartido.Excepciones;

/// <summary>
/// Excepción lanzada cuando se viola una regla de negocio.
/// </summary>
public class ExcepcionDeNegocio : Exception
{
    /// <summary>
    /// Crea una nueva instancia de <see cref="ExcepcionDeNegocio"/>.
    /// </summary>
    /// <param name="mensaje">Mensaje descriptivo del error de negocio.</param>
    public ExcepcionDeNegocio(string mensaje) : base(mensaje)
    {
    }

    /// <summary>
    /// Crea una nueva instancia de <see cref="ExcepcionDeNegocio"/> con una excepción interna.
    /// </summary>
    /// <param name="mensaje">Mensaje descriptivo del error de negocio.</param>
    /// <param name="excepcionInterna">Excepción que causó este error.</param>
    public ExcepcionDeNegocio(string mensaje, Exception excepcionInterna)
        : base(mensaje, excepcionInterna)
    {
    }
}
