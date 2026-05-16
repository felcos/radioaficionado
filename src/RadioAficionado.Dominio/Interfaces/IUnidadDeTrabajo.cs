namespace RadioAficionado.Dominio.Interfaces;

/// <summary>
/// Unidad de trabajo que coordina la persistencia de cambios en el repositorio.
/// </summary>
/// <remarks>
/// <para><b>Para qué sirve:</b> Garantiza que todos los cambios realizados en los repositorios se persistan de forma atómica en una sola transacción.</para>
/// <para><b>Cómo se usa:</b> Se inyecta por constructor junto con los repositorios. Tras realizar operaciones sobre los repositorios, se llama a <see cref="GuardarCambiosAsync"/> para persistir todos los cambios pendientes.</para>
/// <para><b>Implementaciones:</b> <c>RadioAficionado.Infraestructura.Persistencia.UnidadDeTrabajo</c> (EF Core).</para>
/// <para><b>Registro DI:</b> Registrada como Scoped en <c>RadioAficionado.Infraestructura.ConfiguracionServicios.AgregarCapaDeInfraestructura()</c>.</para>
/// <para><b>Configuración necesaria:</b> Requiere que el <c>ContextoRadioAficionado</c> (DbContext) esté registrado (SQLite para escritorio/mobile, PostgreSQL para web).</para>
/// <para><b>Dependencias:</b> <c>ContextoRadioAficionado</c> (DbContext de EF Core).</para>
/// </remarks>
public interface IUnidadDeTrabajo
{
    /// <summary>
    /// Persiste todos los cambios pendientes en el almacenamiento.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Número de entidades afectadas.</returns>
    Task<int> GuardarCambiosAsync(CancellationToken ct);
}
