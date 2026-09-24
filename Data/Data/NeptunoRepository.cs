using System.Data;
using Microsoft.Data.SqlClient;

namespace WPF_SP.Data;

/// <summary>Acceso a Neptuno. Las lecturas usan DataSet desconectado; escrituras usan procedimientos y ExecuteNonQuery.</summary>
public sealed class NeptunoRepository(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public DataTable Listar(string entidad, string? filtro1 = null, string? filtro2 = null)
    {
        var procedure = entidad switch
        {
            "Productos" => "dbo.usp_Productos_Listar",
            "Categorias" => "dbo.usp_Categorias_Listar",
            "Proveedores" => "dbo.usp_Proveedores_Buscar",
            "Pedidos" => "dbo.usp_Pedidos_Listar",
            _ => throw new ArgumentOutOfRangeException(nameof(entidad))
        };
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(procedure, connection) { CommandType = CommandType.StoredProcedure };
        if (entidad == "Proveedores")
        {
            command.Parameters.Add("@NombreContacto", SqlDbType.NVarChar, 100).Value = (object?)filtro1 ?? DBNull.Value;
            command.Parameters.Add("@Ciudad", SqlDbType.NVarChar, 50).Value = (object?)filtro2 ?? DBNull.Value;
        }
        using var adapter = new SqlDataAdapter(command);
        var data = new DataSet();
        adapter.Fill(data);
        return data.Tables[0];
    }

    public DataTable Reporte(DateTime desde, DateTime hasta)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.usp_Reporte_DetallesPedido", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add("@Desde", SqlDbType.Date).Value = desde.Date;
        command.Parameters.Add("@Hasta", SqlDbType.Date).Value = hasta.Date;
        using var adapter = new SqlDataAdapter(command);
        var data = new DataSet();
        adapter.Fill(data);
        return data.Tables[0];
    }

    public int Ejecutar(string procedure, params (string Name, SqlDbType Type, object? Value)[] values)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.usp_" + procedure, connection) { CommandType = CommandType.StoredProcedure };
        foreach (var (name, type, value) in values)
            command.Parameters.Add(name, type).Value = value ?? DBNull.Value;
        connection.Open();
        return command.ExecuteNonQuery();
    }

    public void GuardarFila(string entidad, DataRow row, bool insertar)
    {
        var values = entidad switch
        {
            "Productos" => insertar
                ? new[] { P("@NombreProducto", SqlDbType.NVarChar, V(row,"NombreProducto")), P("@ProveedorID",SqlDbType.Int,V(row,"ProveedorID")), P("@CategoriaID",SqlDbType.Int,V(row,"CategoriaID")), P("@CantidadPorUnidad",SqlDbType.NVarChar,V(row,"CantidadPorUnidad")), P("@PrecioUnidad",SqlDbType.Decimal,V(row,"PrecioUnidad")), P("@UnidadesEnExistencia",SqlDbType.SmallInt,V(row,"UnidadesEnExistencia")), P("@NivelDeReorden",SqlDbType.SmallInt,VOrDefault(row,"NivelDeReorden",(short)0)), P("@Descontinuado",SqlDbType.Bit,VOrDefault(row,"Descontinuado",false)) }
                : new[] { P("@ProductoID",SqlDbType.Int,V(row,"ProductoID")), P("@NombreProducto",SqlDbType.NVarChar,V(row,"NombreProducto")), P("@ProveedorID",SqlDbType.Int,V(row,"ProveedorID")), P("@CategoriaID",SqlDbType.Int,V(row,"CategoriaID")), P("@CantidadPorUnidad",SqlDbType.NVarChar,V(row,"CantidadPorUnidad")), P("@PrecioUnidad",SqlDbType.Decimal,V(row,"PrecioUnidad")), P("@UnidadesEnExistencia",SqlDbType.SmallInt,V(row,"UnidadesEnExistencia")), P("@NivelDeReorden",SqlDbType.SmallInt,VOrDefault(row,"NivelDeReorden",(short)0)), P("@Descontinuado",SqlDbType.Bit,VOrDefault(row,"Descontinuado",false)) },
            "Categorias" => insertar
                ? new[] { P("@NombreCategoria",SqlDbType.NVarChar,V(row,"NombreCategoria")), P("@Descripcion",SqlDbType.NVarChar,V(row,"Descripcion")) }
                : new[] { P("@CategoriaID",SqlDbType.Int,V(row,"CategoriaID")), P("@NombreCategoria",SqlDbType.NVarChar,V(row,"NombreCategoria")), P("@Descripcion",SqlDbType.NVarChar,V(row,"Descripcion")) },
            "Proveedores" => insertar
                ? new[] { P("@CompaniaNombre",SqlDbType.NVarChar,V(row,"CompaniaNombre")), P("@NombreContacto",SqlDbType.NVarChar,V(row,"NombreContacto")), P("@Ciudad",SqlDbType.NVarChar,V(row,"Ciudad")), P("@Pais",SqlDbType.NVarChar,V(row,"Pais")) }
                : new[] { P("@ProveedorID",SqlDbType.Int,V(row,"ProveedorID")), P("@CompaniaNombre",SqlDbType.NVarChar,V(row,"CompaniaNombre")), P("@NombreContacto",SqlDbType.NVarChar,V(row,"NombreContacto")), P("@Ciudad",SqlDbType.NVarChar,V(row,"Ciudad")), P("@Pais",SqlDbType.NVarChar,V(row,"Pais")) },
            "Pedidos" => insertar
                ? new[] { P("@ClienteID",SqlDbType.Int,V(row,"ClienteID")), P("@EmpleadoID",SqlDbType.Int,V(row,"EmpleadoID")), P("@FechaPedido",SqlDbType.Date,V(row,"FechaPedido")), P("@Destinatario",SqlDbType.NVarChar,V(row,"Destinatario")), P("@CiudadDestino",SqlDbType.NVarChar,V(row,"CiudadDestino")) }
                : new[] { P("@PedidoID",SqlDbType.Int,V(row,"PedidoID")), P("@ClienteID",SqlDbType.Int,V(row,"ClienteID")), P("@EmpleadoID",SqlDbType.Int,V(row,"EmpleadoID")), P("@FechaPedido",SqlDbType.Date,V(row,"FechaPedido")), P("@Destinatario",SqlDbType.NVarChar,V(row,"Destinatario")), P("@CiudadDestino",SqlDbType.NVarChar,V(row,"CiudadDestino")) },
            _ => throw new ArgumentOutOfRangeException(nameof(entidad))
        };
        var verb = insertar ? "Insertar" : "Actualizar";
        Ejecutar($"{entidad}_{verb}", values);
    }

    public void Eliminar(string entidad, int id)
    {
        var (parameter, type) = entidad switch
        {
            "Productos" => ("@ProductoID", SqlDbType.Int),
            "Categorias" => ("@CategoriaID", SqlDbType.Int),
            "Proveedores" => ("@ProveedorID", SqlDbType.Int),
            "Pedidos" => ("@PedidoID", SqlDbType.Int),
            _ => throw new ArgumentOutOfRangeException(nameof(entidad))
        };
        Ejecutar($"{entidad}_Eliminar", (parameter, type, id));
    }

    private static object? V(DataRow row, string column) => row.Table.Columns.Contains(column) && !row.IsNull(column) ? row[column] : null;
    private static object VOrDefault(DataRow row, string column, object defaultValue) => V(row, column) ?? defaultValue;
    private static (string Name, SqlDbType Type, object? Value) P(string name, SqlDbType type, object? value) => (name, type, value);
}
