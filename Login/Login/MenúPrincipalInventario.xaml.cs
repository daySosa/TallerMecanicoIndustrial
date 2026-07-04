#nullable enable
using Dasboard_Prueba;
using InterfazClientes;
using Login;
using Login.Clases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace InterfazInventario
{
    public partial class MenúPrincipalInventario : Window
    {
        private readonly RepositorioSql _db = new();
        private readonly ObservableCollection<ValidadorInventario> _listaRepuestos = new();
        private ICollectionView? _vistaRepuestos;

        private string? _filtroCategoria = null;
        private decimal _filtroPrecioMin = 0;
        private decimal _filtroPrecioMax = decimal.MaxValue;
        private bool _filtroStockBajo = false;

        private bool _cargandoDatos = false;

        public MenúPrincipalInventario()
        {
            InitializeComponent();
            AplicarPermisos();
            Loaded += MenúPrincipalInventario_Loaded;
        }

        private async void MenúPrincipalInventario_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarDatosAsync();
            await CargarNotificacionesAsync();
        }

        // ── MOVER VENTANA ────────────────────────────────────────────

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // ════════════════════════════════════════════════════════════
        // PERMISOS SEGÚN ROL
        // ════════════════════════════════════════════════════════════

        private void AplicarPermisos()
        {
            if (!SesionActual.EsAdministrador)
            {
                btnUsuarios.Visibility = Visibility.Collapsed;
                btnBitacora.Visibility = Visibility.Collapsed;
                expanderContabilidad.Visibility = Visibility.Collapsed;
                btnReportes.Visibility = Visibility.Collapsed;
            }
        }

        // ── NAVEGACIÓN ───────────────────────────────────────────────

        private void Navegar<T>(Func<T> crear) where T : Window
        {
            crear().Show();
            this.Close();
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenuPrincipal());

        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Vehículos.MenúPrincipalVehículos());

        private void btnClientes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalClientes());

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalOrdenes());

        private void btnEgresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Contabilidad.MenúPrincipalEgresos());

        private void btnIngresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Contabilidad.MenúPrincipalIngresos());

        private void btnUsuarios_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new InterfazClientes.MenúPrincipalUsuarios());

        private void btnBitacora_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalBitacora());

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                SesionActual.CerrarSesion();
                Navegar(() => new Login.MainWindow());
            }
        }

        // ── DATOS ────────────────────────────────────────────────────

        /// <summary>
        /// Carga el inventario desde la base de datos sin bloquear la interfaz.
        /// Se protege contra ejecuciones simultáneas con _cargandoDatos.
        /// </summary>
        private async Task CargarDatosAsync()
        {
            if (_cargandoDatos) return;
            _cargandoDatos = true;
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                List<ValidadorInventario> productos = await Task.Run(() =>
                {
                    var lista = new List<ValidadorInventario>();
                    foreach (var p in _db.ObtenerProductos())
                    {
                        lista.Add(new ValidadorInventario
                        {
                            Producto_ID = p.Producto_ID,
                            Producto_Nombre = p.Producto_Nombre,
                            Producto_Categoria = p.Producto_Categoria,
                            Producto_Marca = p.Producto_Marca,
                            Producto_Modelo = p.Producto_Modelo,
                            Producto_Cantidad_Minima = p.Producto_Cantidad_Minima,
                            Producto_Precio = p.Producto_Precio,
                            Producto_Cantidad_Actual = p.Producto_Cantidad_Actual
                        });
                    }
                    return lista;
                });

                _listaRepuestos.Clear();
                foreach (var p in productos)
                    _listaRepuestos.Add(p);

                if (_vistaRepuestos == null)
                {
                    _vistaRepuestos = CollectionViewSource.GetDefaultView(_listaRepuestos);
                    _vistaRepuestos.Filter = AplicarFiltros;
                    dgInventario.ItemsSource = _vistaRepuestos;
                }
                else
                {
                    _vistaRepuestos.Refresh();
                }

                ActualizarCategorias(productos);
                ActualizarContador();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar inventario:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                _cargandoDatos = false;
            }
        }

        /// <summary>
        /// Rellena el combo de categorías con los valores distintos presentes en el
        /// inventario actual, conservando la selección previa si sigue existiendo.
        /// </summary>
        private void ActualizarCategorias(IEnumerable<ValidadorInventario> productos)
        {
            string? seleccionActual = cmbCategoria.SelectedItem as string;

            var categorias = new List<string> { "Todas" };
            categorias.AddRange(
                productos
                    .Select(p => p.Producto_Categoria)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c)!);

            cmbCategoria.ItemsSource = categorias;

            cmbCategoria.SelectedItem = seleccionActual != null && categorias.Contains(seleccionActual)
                ? seleccionActual
                : categorias[0];
        }

        private bool AplicarFiltros(object item)
        {
            if (item is not ValidadorInventario r) return false;

            string texto = txtBuscar.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(texto))
            {
                bool coincide =
                    (r.Producto_Nombre ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Categoria ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Marca ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Modelo ?? "").ToLower().Contains(texto);
                if (!coincide) return false;
            }

            if (_filtroCategoria != null && _filtroCategoria != "Todas")
                if (r.Producto_Categoria != _filtroCategoria) return false;

            if (r.Producto_Precio < _filtroPrecioMin) return false;
            if (r.Producto_Precio > _filtroPrecioMax) return false;
            if (_filtroStockBajo && !r.StockBajo) return false;

            return true;
        }

        private void ActualizarContador()
        {
            int total = _vistaRepuestos?.Cast<object>().Count() ?? 0;
            tbTotalItems.Text = $"{total} item{(total != 1 ? "s" : "")}";
        }

        // ── BÚSQUEDA Y FILTROS ───────────────────────────────────────

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
            => popupFiltros.IsOpen = !popupFiltros.IsOpen;

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidacionesGenerales.ValidarRangoPrecios(txtPrecioMin.Text, txtPrecioMax.Text,
                    out decimal pMin, out decimal pMax))
                return;

            _filtroCategoria = cmbCategoria.SelectedItem as string;
            _filtroPrecioMin = pMin;
            _filtroPrecioMax = pMax;
            _filtroStockBajo = chkStockBajo.IsChecked == true;
            popupFiltros.IsOpen = false;
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCategoria.Items.Count > 0)
                cmbCategoria.SelectedIndex = 0;

            txtPrecioMin.Clear();
            txtPrecioMax.Clear();
            chkStockBajo.IsChecked = false;

            _filtroCategoria = null;
            _filtroPrecioMin = 0;
            _filtroPrecioMax = decimal.MaxValue;
            _filtroStockBajo = false;

            popupFiltros.IsOpen = false;
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }

        // ── DATAGRID ─────────────────────────────────────────────────

        private async void dgInventario_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgInventario.SelectedItem is not ValidadorInventario seleccionado) return;

            var ventana = new InventarioWindow();
            ventana.CargarProductoParaEditar(seleccionado);
            ventana.ShowDialog();

            dgInventario.SelectedItem = null;
            await CargarDatosAsync();
            await CargarNotificacionesAsync();
        }

        private async void btnAgregarRepuesto_Click(object sender, RoutedEventArgs e)
        {
            new InventarioWindow().ShowDialog();
            await CargarDatosAsync();
            await CargarNotificacionesAsync();
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
            => new ReportesWindow("Inventario").ShowDialog();

        // ── NOTIFICACIONES ───────────────────────────────────────────

        private async void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                await CargarNotificacionesEnPopupAsync();

            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

        private async Task CargarNotificacionesAsync()
        {
            try
            {
                int cantidad = await Task.Run(() => _db.ContarNotificacionesPendientes());
                badgeNotificaciones.Visibility = cantidad > 0 ? Visibility.Visible : Visibility.Collapsed;
                txtContadorNotificaciones.Text = cantidad > 99 ? "99+" : cantidad.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al cargar notificaciones: " + ex.Message);
            }
        }

        private async Task CargarNotificacionesEnPopupAsync()
        {
            panelNotificaciones.Children.Clear();
            try
            {
                DataTable dt = await Task.Run(() => _db.ObtenerNotificacionesPendientes());

                if (dt.Rows.Count == 0)
                {
                    var vacio = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 20)
                    };
                    vacio.Children.Add(new Label
                    {
                        Content = "🎉",
                        FontSize = 32,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Foreground = Brushes.White,
                        Padding = new Thickness(0)
                    });
                    vacio.Children.Add(new TextBlock
                    {
                        Text = "Sin notificaciones pendientes",
                        Foreground = Pincel("#6B7280"),
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 8, 0, 0)
                    });
                    panelNotificaciones.Children.Add(vacio);
                    badgeContadorPopup.Visibility = Visibility.Collapsed;
                    btnMarcarTodas.Visibility = Visibility.Collapsed;
                    return;
                }

                txtContadorPopup.Text = dt.Rows.Count > 99 ? "99+" : dt.Rows.Count.ToString();
                badgeContadorPopup.Visibility = Visibility.Visible;
                btnMarcarTodas.Visibility = Visibility.Visible;

                foreach (DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["Notificacion_ID"]);
                    string tipo = row["Tipo_Notificacion"].ToString() ?? "";
                    string msg = row["Mensaje"].ToString() ?? "";
                    panelNotificaciones.Children.Add(CrearTarjeta(id, tipo, msg));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
        }

        private Border CrearTarjeta(int id, string tipo, string mensaje)
        {
            bool esStock = tipo == "STOCK_BAJO";
            string colorBorde = esStock ? "#F0A500" : "#3D7EFF";
            string colorFondo = esStock ? "#1A1500" : "#0D1A2E";
            string labelTipo = esStock ? "Stock Bajo" : "Orden Finalizada";

            var badge = new Border
            {
                Background = Pincel(colorBorde + "33"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5),
                Child = new TextBlock
                {
                    Text = labelTipo,
                    Foreground = Pincel(colorBorde),
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold
                }
            };

            var contenido = new StackPanel();
            contenido.Children.Add(badge);
            contenido.Children.Add(new TextBlock
            {
                Text = mensaje,
                Foreground = Brushes.White,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 17
            });

            var btnLeida = new Button
            {
                Content = "✓",
                Foreground = Pincel("#6B7280"),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = Cursors.Hand,
                ToolTip = "Marcar como leída",
                Tag = id
            };
            btnLeida.Click += async (s, _) =>
            {
                try
                {
                    int notifId = (int)((Button)s).Tag;
                    await Task.Run(() => _db.MarcarNotificacionLeida(notifId));
                    await CargarNotificacionesEnPopupAsync();
                    await CargarNotificacionesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al marcar como leída: " + ex.Message);
                }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(contenido, 0);
            Grid.SetColumn(btnLeida, 1);
            grid.Children.Add(contenido);
            grid.Children.Add(btnLeida);

            return new Border
            {
                Background = Pincel(colorFondo),
                BorderBrush = Pincel(colorBorde),
                BorderThickness = new Thickness(0, 0, 0, 3),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 10, 12, 10),
                Child = grid
            };
        }

        private async void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Run(() => _db.MarcarNotificacionLeida(null));
                await CargarNotificacionesEnPopupAsync();
                await CargarNotificacionesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al marcar notificaciones: " + ex.Message);
            }
        }

        // ── HELPER ───────────────────────────────────────────────────

        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}