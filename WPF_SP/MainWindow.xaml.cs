using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPF_SP.Data;

namespace WPF_SP;

public partial class MainWindow : Window
{
    private readonly NeptunoRepository _repository = new(DbConfig.ConnectionString);
    private readonly HashSet<string> _loaded = new();

    public MainWindow()
    {
        InitializeComponent();
        DesdePicker.SelectedDate = DateTime.Today.AddDays(-30);
        HastaPicker.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await CargarActualAsync();
    }

    private string Entidad => (Tabs.SelectedItem as TabItem)?.Tag?.ToString() ?? "Productos";
    private DataGrid? Grid => Entidad switch
    {
        "Productos" => ProductosGrid,
        "Categorias" => CategoriasGrid,
        "Proveedores" => ProveedoresGrid,
        "Pedidos" => PedidosGrid,
        "Reporte" => ReporteGrid,
        _ => null
    };

    private async Task CargarActualAsync()
    {
        if (Entidad == "Reporte" || Grid is null || _loaded.Contains(Entidad)) return;
        try
        {
            var entidad = Entidad;
            var contacto = entidad == "Proveedores" ? ContactoBox.Text.Trim() : null;
            var ciudad = entidad == "Proveedores" ? CiudadBox.Text.Trim() : null;
            var table = await Task.Run(() => _repository.Listar(entidad,
                string.IsNullOrEmpty(contacto) ? null : contacto,
                string.IsNullOrEmpty(ciudad) ? null : ciudad));
            var grid = Grid;
            if (grid is null) return;
            grid.ItemsSource = table.DefaultView;
            var editable = entidad switch
            {
                "Productos" => new[] { "NombreProducto", "ProveedorID", "CategoriaID", "CantidadPorUnidad", "PrecioUnidad", "UnidadesEnExistencia" },
                "Categorias" => new[] { "NombreCategoria", "Descripcion" },
                "Proveedores" => new[] { "CompaniaNombre", "NombreContacto", "Ciudad", "Pais" },
                "Pedidos" => new[] { "ClienteID", "EmpleadoID", "FechaPedido", "Destinatario", "CiudadDestino" },
                _ => Array.Empty<string>()
            };
            foreach (var column in grid.Columns)
            {
                var propertyName = (column as DataGridBoundColumn)?.Binding is Binding binding
                    ? binding.Path.Path
                    : column.Header?.ToString();
                column.IsReadOnly = !editable.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
                if (propertyName is not null && ColumnTitles.TryGetValue(propertyName, out var title))
                    column.Header = title;
            }
            _loaded.Add(entidad);
            StatusText.Text = table.Rows.Count == 0
                ? $"{entidad}: no hay registros para mostrar."
                : $"{table.Rows.Count} registros cargados en {entidad}.";
        }
        catch (Exception ex) { Error("No se pudieron cargar los datos", ex); }
    }

    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source == Tabs) await CargarActualAsync();
    }

    private async void Reload_Click(object sender, RoutedEventArgs e)
    {
        _loaded.Remove(Entidad);
        if (Grid is not null) Grid.ItemsSource = null;
        await CargarActualAsync();
    }

    private async void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        ContactoBox.Clear(); CiudadBox.Clear(); _loaded.Remove("Proveedores");
        ProveedoresGrid.ItemsSource = null; await CargarActualAsync();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var grid = Grid;
        if (grid?.ItemsSource is not DataView view || view.Table is not DataTable table) return;
        var row = table.NewRow();
        table.Rows.Add(row);
        var rowView = view.Cast<DataRowView>().FirstOrDefault(item => item.Row == row);
        if (rowView is null) return;
        grid.ScrollIntoView(rowView);
        grid.SelectedItem = rowView;
        var firstEditableColumn = grid.Columns.FirstOrDefault(column => !column.IsReadOnly);
        if (firstEditableColumn is not null)
            grid.CurrentCell = new DataGridCellInfo(rowView, firstEditableColumn);
        grid.BeginEdit();
        StatusText.Text = "Completa los campos de la fila nueva y guarda los cambios.";
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var grid = Grid;
        if (grid is null || grid.ItemsSource is not DataView view || view.Table is not DataTable table) return;
        grid.CommitEdit(DataGridEditingUnit.Cell, true);
        grid.CommitEdit(DataGridEditingUnit.Row, true);
        try
        {
            foreach (DataRow row in table.Rows.Cast<DataRow>().ToArray())
            {
                if (row.RowState is not (DataRowState.Added or DataRowState.Modified)) continue;
                await Task.Run(() => _repository.GuardarFila(Entidad, row, row.RowState == DataRowState.Added));
                row.AcceptChanges();
            }
            MessageBox.Show(this, "Los cambios se guardaron correctamente.", "Neptuno", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Cambios guardados correctamente.";
            await RecargarEntidadAsync();
        }
        catch (Exception ex) { Error("No se pudieron guardar los cambios. Revisa los campos obligatorios", ex); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid?.SelectedItem is not DataRowView selected) { MessageBox.Show(this, "Selecciona una fila."); return; }
        var idName = Entidad switch { "Productos" => "ProductoID", "Categorias" => "CategoriaID", "Proveedores" => "ProveedorID", _ => "PedidoID" };
        if (selected.Row.RowState == DataRowState.Added) { selected.Row.Delete(); return; }
        if (MessageBox.Show(this, "¿Dar de baja este registro? Se conservará en la base de datos.", "Confirmar baja lógica", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            var entity = Entidad;
            var id = Convert.ToInt32(selected.Row[idName]);
            await Task.Run(() => _repository.Eliminar(entity, id));
            selected.Row.Delete();
            StatusText.Text = "Registro dado de baja lógicamente.";
            MessageBox.Show(this, "Registro dado de baja lógicamente.", "Neptuno", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { Error("No se pudo dar de baja el registro", ex); }
    }

    private async void Report_Click(object sender, RoutedEventArgs e)
    {
        if (DesdePicker.SelectedDate is not DateTime desde || HastaPicker.SelectedDate is not DateTime hasta || desde > hasta)
        {
            MessageBox.Show(this, "Selecciona un intervalo de fechas válido.", "Fechas requeridas", MessageBoxButton.OK, MessageBoxImage.Warning); return;
        }
        try
        {
            var table = await Task.Run(() => _repository.Reporte(desde, hasta));
            ReporteGrid.ItemsSource = table.DefaultView;
            foreach (var column in ReporteGrid.Columns)
            {
                var propertyName = (column as DataGridBoundColumn)?.Binding is Binding binding
                    ? binding.Path.Path
                    : column.Header?.ToString();
                if (propertyName is not null && ColumnTitles.TryGetValue(propertyName, out var title))
                    column.Header = title;
            }
            StatusText.Text = table.Rows.Count == 0 ? "El intervalo no tiene detalles de pedidos." : $"{table.Rows.Count} detalles encontrados.";
        }
        catch (Exception ex) { Error("No se pudo generar el reporte", ex); }
    }

    private async Task RecargarEntidadAsync()
    {
        _loaded.Remove(Entidad);
        if (Grid is not null) Grid.ItemsSource = null;
        await CargarActualAsync();
    }

    private void Error(string title, Exception ex)
    {
        StatusText.Text = $"{title}: {ex.Message}";
        MessageBox.Show(this, $"{title}:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static readonly Dictionary<string, string> ColumnTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProductoID"] = "Código", ["NombreProducto"] = "Producto", ["ProveedorID"] = "Proveedor",
        ["CategoriaID"] = "Categoría", ["CantidadPorUnidad"] = "Presentación", ["PrecioUnidad"] = "Precio unitario",
        ["UnidadesEnExistencia"] = "En existencia", ["UnidadesEnPedido"] = "En pedido",
        ["NivelDeReorden"] = "Nivel de reorden", ["Descontinuado"] = "Descontinuado",
        ["NombreCategoria"] = "Categoría", ["Descripcion"] = "Descripción",
        ["CompaniaNombre"] = "Empresa", ["NombreContacto"] = "Contacto",
        ["CargoContacto"] = "Cargo", ["Direccion"] = "Dirección", ["Ciudad"] = "Ciudad",
        ["CodigoPostal"] = "Código postal", ["Pais"] = "País", ["Telefono"] = "Teléfono", ["Fax"] = "Fax",
        ["PedidoID"] = "Pedido", ["ClienteID"] = "Cliente", ["EmpleadoID"] = "Empleado",
        ["FechaPedido"] = "Fecha del pedido", ["FechaRequerida"] = "Fecha requerida", ["FechaEnvio"] = "Fecha de envío",
        ["TransportistaID"] = "Transportista", ["Destinatario"] = "Destinatario", ["CiudadDestino"] = "Ciudad de destino",
        ["PaisDestino"] = "País de destino", ["Cantidad"] = "Cantidad", ["Descuento"] = "Descuento"
    };
}
