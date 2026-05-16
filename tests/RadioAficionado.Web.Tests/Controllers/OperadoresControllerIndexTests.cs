using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RadioAficionado.Dominio.Entidades;
using RadioAficionado.Dominio.Interfaces;
using RadioAficionado.Dominio.ObjetosDeValor;
using RadioAficionado.Web.Controllers;
using RadioAficionado.Web.ViewModels;
using Xunit;

namespace RadioAficionado.Web.Tests.Controllers;

/// <summary>
/// Tests unitarios para la acción Index de <see cref="OperadoresController"/>.
/// </summary>
public class OperadoresControllerIndexTests
{
    private readonly Mock<IRepositorioQso> _mockRepositorio;
    private readonly Mock<ILogger<OperadoresController>> _mockLogger;

    /// <summary>
    /// Constructor que inicializa los mocks comunes.
    /// </summary>
    public OperadoresControllerIndexTests()
    {
        _mockRepositorio = new Mock<IRepositorioQso>();
        _mockLogger = new Mock<ILogger<OperadoresController>>();
    }

    /// <summary>
    /// Crea un UserManager con un conjunto de usuarios en memoria para testear.
    /// </summary>
    /// <param name="usuarios">Lista de usuarios a incluir en el queryable.</param>
    /// <returns>Mock de UserManager configurado.</returns>
    private Mock<UserManager<UsuarioRadio>> CrearMockUserManager(List<UsuarioRadio> usuarios)
    {
        Mock<IUserStore<UsuarioRadio>> mockUserStore = new();
        Mock<UserManager<UsuarioRadio>> mockUserManager = new(
            mockUserStore.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<UsuarioRadio>>().Object,
            Array.Empty<IUserValidator<UsuarioRadio>>(),
            Array.Empty<IPasswordValidator<UsuarioRadio>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<UsuarioRadio>>>().Object);

        IQueryable<UsuarioRadio> queryable = usuarios.AsQueryable();
        TestAsyncQueryProvider<UsuarioRadio> proveedorAsync = new(queryable.Provider);

        Mock<DbSet<UsuarioRadio>> mockDbSet = new();
        mockDbSet.As<IAsyncEnumerable<UsuarioRadio>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<UsuarioRadio>(usuarios.GetEnumerator()));
        mockDbSet.As<IQueryable<UsuarioRadio>>()
            .Setup(m => m.Provider)
            .Returns(proveedorAsync);
        mockDbSet.As<IQueryable<UsuarioRadio>>()
            .Setup(m => m.Expression)
            .Returns(queryable.Expression);
        mockDbSet.As<IQueryable<UsuarioRadio>>()
            .Setup(m => m.ElementType)
            .Returns(queryable.ElementType);
        mockDbSet.As<IQueryable<UsuarioRadio>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => usuarios.GetEnumerator());

        mockUserManager.Setup(um => um.Users).Returns(mockDbSet.Object);

        return mockUserManager;
    }

    /// <summary>
    /// Crea una instancia del controlador con los mocks dados.
    /// </summary>
    /// <param name="mockUserManager">Mock del UserManager.</param>
    /// <returns>Instancia de OperadoresController.</returns>
    private OperadoresController CrearControlador(Mock<UserManager<UsuarioRadio>> mockUserManager)
    {
        return new OperadoresController(
            mockUserManager.Object,
            _mockRepositorio.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Index_PaginaInvalida_UsaPagina1()
    {
        // Arrange
        List<UsuarioRadio> usuarios = new()
        {
            new UsuarioRadio { Indicativo = "EA4ABC", Nombre = "Operador Uno", FechaRegistro = DateTime.UtcNow }
        };

        Mock<UserManager<UsuarioRadio>> mockUserManager = CrearMockUserManager(usuarios);
        OperadoresController controlador = CrearControlador(mockUserManager);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso>());

        // Act
        IActionResult resultado = await controlador.Index(pagina: -5, busqueda: null, ct: CancellationToken.None);

        // Assert
        ViewResult vistaResultado = resultado.Should().BeOfType<ViewResult>().Subject;
        OperadoresIndexViewModel viewModel = vistaResultado.Model.Should().BeOfType<OperadoresIndexViewModel>().Subject;
        viewModel.PaginaActual.Should().Be(1);
    }

    [Fact]
    public async Task Index_ConBusqueda_FiltraResultados()
    {
        // Arrange
        List<UsuarioRadio> usuarios = new()
        {
            new UsuarioRadio { Indicativo = "EA4ABC", Nombre = "Juan Pérez", FechaRegistro = DateTime.UtcNow },
            new UsuarioRadio { Indicativo = "W1AW", Nombre = "ARRL HQ", FechaRegistro = DateTime.UtcNow },
            new UsuarioRadio { Indicativo = "EA7XYZ", Nombre = "María López", FechaRegistro = DateTime.UtcNow }
        };

        Mock<UserManager<UsuarioRadio>> mockUserManager = CrearMockUserManager(usuarios);
        OperadoresController controlador = CrearControlador(mockUserManager);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso>());

        // Act
        IActionResult resultado = await controlador.Index(pagina: 1, busqueda: "EA", ct: CancellationToken.None);

        // Assert
        ViewResult vistaResultado = resultado.Should().BeOfType<ViewResult>().Subject;
        OperadoresIndexViewModel viewModel = vistaResultado.Model.Should().BeOfType<OperadoresIndexViewModel>().Subject;
        viewModel.Operadores.Should().HaveCount(2);
        viewModel.Operadores.Should().OnlyContain(o => o.Indicativo.Contains("EA"));
        viewModel.Busqueda.Should().Be("EA");
    }

    [Fact]
    public async Task Index_SinResultados_RetornaVistaConListaVacia()
    {
        // Arrange
        List<UsuarioRadio> usuarios = new()
        {
            new UsuarioRadio { Indicativo = "EA4ABC", Nombre = "Operador Uno", FechaRegistro = DateTime.UtcNow }
        };

        Mock<UserManager<UsuarioRadio>> mockUserManager = CrearMockUserManager(usuarios);
        OperadoresController controlador = CrearControlador(mockUserManager);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso>());

        // Act
        IActionResult resultado = await controlador.Index(pagina: 1, busqueda: "ZZZZNOEXISTE", ct: CancellationToken.None);

        // Assert
        ViewResult vistaResultado = resultado.Should().BeOfType<ViewResult>().Subject;
        OperadoresIndexViewModel viewModel = vistaResultado.Model.Should().BeOfType<OperadoresIndexViewModel>().Subject;
        viewModel.Operadores.Should().BeEmpty();
        viewModel.TotalElementos.Should().Be(0);
    }

    [Fact]
    public async Task Index_PaginaPorDefecto_RetornaVista()
    {
        // Arrange
        List<UsuarioRadio> usuarios = new();
        Mock<UserManager<UsuarioRadio>> mockUserManager = CrearMockUserManager(usuarios);
        OperadoresController controlador = CrearControlador(mockUserManager);

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso>());

        // Act
        IActionResult resultado = await controlador.Index(ct: CancellationToken.None);

        // Assert
        ViewResult vistaResultado = resultado.Should().BeOfType<ViewResult>().Subject;
        OperadoresIndexViewModel viewModel = vistaResultado.Model.Should().BeOfType<OperadoresIndexViewModel>().Subject;
        viewModel.PaginaActual.Should().Be(1);
        viewModel.TamanoPagina.Should().Be(20);
    }

    [Fact]
    public async Task Index_ConOperadores_RetornaViewModelConDatos()
    {
        // Arrange
        List<UsuarioRadio> usuarios = new()
        {
            new UsuarioRadio { Indicativo = "EA4ABC", Nombre = "Juan Pérez", Localizador = "IN80dk", FechaRegistro = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc) },
            new UsuarioRadio { Indicativo = "W1AW", Nombre = "ARRL HQ", Localizador = "FN31pr", FechaRegistro = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc) }
        };

        Mock<UserManager<UsuarioRadio>> mockUserManager = CrearMockUserManager(usuarios);
        OperadoresController controlador = CrearControlador(mockUserManager);

        Qso qsoEa4 = Qso.Crear(
            indicativoPropio: new Indicativo("EA4ABC"),
            indicativoContacto: new Indicativo("DL1ABC"),
            fechaHoraInicio: DateTimeOffset.UtcNow,
            frecuencia: Frecuencia.DesdeHz(14074000),
            modo: ModoOperacion.FT8,
            senalEnviada: "59");

        _mockRepositorio
            .Setup(r => r.ObtenerTodosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Qso> { qsoEa4 });

        // Act
        IActionResult resultado = await controlador.Index(pagina: 1, busqueda: null, ct: CancellationToken.None);

        // Assert
        ViewResult vistaResultado = resultado.Should().BeOfType<ViewResult>().Subject;
        OperadoresIndexViewModel viewModel = vistaResultado.Model.Should().BeOfType<OperadoresIndexViewModel>().Subject;
        viewModel.Operadores.Should().HaveCount(2);
        viewModel.TotalElementos.Should().Be(2);

        OperadorResumenViewModel operadorEa4 = viewModel.Operadores.First(o => o.Indicativo == "EA4ABC");
        operadorEa4.Nombre.Should().Be("Juan Pérez");
        operadorEa4.Localizador.Should().Be("IN80dk");
        operadorEa4.TotalQsos.Should().Be(1);

        OperadorResumenViewModel operadorW1 = viewModel.Operadores.First(o => o.Indicativo == "W1AW");
        operadorW1.TotalQsos.Should().Be(0);
    }
}

// ── Helpers para mockear IQueryable async con EF Core ──────────────────

/// <summary>
/// Proveedor de queries async para tests unitarios.
/// Permite que las operaciones async de EF Core funcionen con colecciones en memoria.
/// Elimina automáticamente las llamadas a métodos de EF Core (AsNoTracking, etc.)
/// que no existen en LINQ to Objects.
/// </summary>
internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _proveedorInterno;

    /// <summary>
    /// Constructor del proveedor de queries async.
    /// </summary>
    /// <param name="proveedorInterno">Proveedor de queries LINQ estándar (de EnumerableQuery).</param>
    internal TestAsyncQueryProvider(IQueryProvider proveedorInterno)
    {
        _proveedorInterno = proveedorInterno;
    }

    /// <inheritdoc />
    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(_proveedorInterno.CreateQuery<TEntity>(LimpiarExpresion(expression)));
    }

    /// <inheritdoc />
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        Expression expresionLimpia = LimpiarExpresion(expression);
        IQueryable<TElement> queryInterna = _proveedorInterno.CreateQuery<TElement>(expresionLimpia);
        return new TestAsyncEnumerable<TElement>(queryInterna);
    }

    /// <inheritdoc />
    public object? Execute(Expression expression)
    {
        return _proveedorInterno.Execute(LimpiarExpresion(expression));
    }

    /// <inheritdoc />
    public TResult Execute<TResult>(Expression expression)
    {
        return _proveedorInterno.Execute<TResult>(LimpiarExpresion(expression));
    }

    /// <inheritdoc />
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        Type tipoResultado = typeof(TResult).GetGenericArguments()[0];

        Expression expresionLimpia = LimpiarExpresion(expression);
        object? resultadoEjecutado = _proveedorInterno.Execute(expresionLimpia);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(tipoResultado)
            .Invoke(null, new[] { resultadoEjecutado })!;
    }

    /// <summary>
    /// Elimina llamadas a métodos de EF Core que no existen en LINQ to Objects
    /// (como AsNoTracking, AsTracking, TagWith, etc.).
    /// </summary>
    /// <param name="expression">Expresión original.</param>
    /// <returns>Expresión limpia sin métodos de EF Core.</returns>
    private static Expression LimpiarExpresion(Expression expression)
    {
        return new EfCoreMethodRemover().Visit(expression);
    }
}

/// <summary>
/// Enumerable async para tests unitarios.
/// Permite iterar colecciones en memoria de forma asíncrona.
/// Almacena la referencia al IQueryable interno y el provider async.
/// </summary>
internal class TestAsyncEnumerable<T> : IAsyncEnumerable<T>, IQueryable<T>, IOrderedQueryable<T>
{
    private readonly IQueryable<T> _queryableInterno;
    private readonly TestAsyncQueryProvider<T> _proveedor;

    /// <summary>
    /// Constructor a partir de un IQueryable existente.
    /// </summary>
    /// <param name="queryableInterno">IQueryable LINQ estándar subyacente.</param>
    public TestAsyncEnumerable(IQueryable<T> queryableInterno)
    {
        _queryableInterno = queryableInterno;
        _proveedor = new TestAsyncQueryProvider<T>(queryableInterno.Provider);
    }

    /// <summary>
    /// Constructor a partir de una colección en memoria.
    /// </summary>
    /// <param name="enumerable">Colección en memoria.</param>
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : this(enumerable.AsQueryable())
    {
    }

    /// <inheritdoc />
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(_queryableInterno.GetEnumerator());
    }

    /// <inheritdoc />
    Type IQueryable.ElementType => _queryableInterno.ElementType;

    /// <inheritdoc />
    Expression IQueryable.Expression => _queryableInterno.Expression;

    /// <inheritdoc />
    IQueryProvider IQueryable.Provider => _proveedor;

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _queryableInterno.GetEnumerator();

    /// <inheritdoc />
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _queryableInterno.GetEnumerator();
}

/// <summary>
/// Visitor que elimina llamadas a métodos de EF Core del expression tree.
/// Permite que las queries funcionen con LINQ to Objects en tests.
/// </summary>
internal class EfCoreMethodRemover : ExpressionVisitor
{
    private static readonly HashSet<string> _metodosAEliminar = new()
    {
        "AsNoTracking",
        "AsNoTrackingWithIdentityResolution",
        "AsTracking",
        "TagWith",
        "IgnoreQueryFilters",
        "IgnoreAutoIncludes"
    };

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (_metodosAEliminar.Contains(node.Method.Name) && node.Arguments.Count >= 1)
        {
            // Estos métodos tienen la forma: Extension(source, ...) - devolvemos el source
            return Visit(node.Arguments[0]);
        }

        return base.VisitMethodCall(node);
    }
}

/// <summary>
/// Enumerador async para tests unitarios.
/// Envuelve un enumerador síncrono con la interfaz async.
/// </summary>
internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _enumeradorInterno;

    /// <summary>
    /// Constructor del enumerador async.
    /// </summary>
    /// <param name="enumeradorInterno">Enumerador síncrono a envolver.</param>
    public TestAsyncEnumerator(IEnumerator<T> enumeradorInterno)
    {
        _enumeradorInterno = enumeradorInterno;
    }

    /// <inheritdoc />
    public T Current => _enumeradorInterno.Current;

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_enumeradorInterno.MoveNext());
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _enumeradorInterno.Dispose();
        return ValueTask.CompletedTask;
    }
}
