using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Órdenes_de_Trabajo
{
    public partial class MenúPrincipalOrdenes : Window
    {
        public class OrdenItem
        {
            public int NumeroOrden { get; set; }
            public string NombreCliente { get; set; } = string.Empty;
            public string Placa { get; set; } = string.Empty;
            public DateTime FechaOrden { get; set; }
            public string Estado { get; set; } = string.Empty;
            public string Prioridad { get; set; } = string.Empty;
            public decimal Precio { get; set; }
        }

        private List<OrdenItem> _todasLasOrdenes = new();
        private ObservableCollection<OrdenItem> _ordenesFiltradas = new();
        private string _filtroEstadoActual = "Todos";

        public MenúPrincipalOrdenes()
        {
            InitializeComponent();
            dgOrdenes.ItemsSource = _ordenesFiltradas;
            CargarDatosEjemplo();
        }

        private void CargarDatosEjemplo()
        {
            _todasLasOrdenes = new List<OrdenItem>
            {
                new OrdenItem { NumeroOrden=1, NombreCliente="Carlos Mendoza", Placa="ABC-123", FechaOrden=DateTime.Today,            Estado="En Espera", Prioridad="Normal",  Precio=1500 },
                new OrdenItem { NumeroOrden=2, NombreCliente="María López",    Placa="XYZ-456", FechaOrden=DateTime.Today.AddDays(-1), Estado="Reparando", Prioridad="Alta",    Precio=3200 },
                new OrdenItem { NumeroOrden=3, NombreCliente="Juan Pérez",     Placa="DEF-789", FechaOrden=DateTime.Today.AddDays(-2), Estado="Finalizado",Prioridad="Normal",  Precio=800  },
                new OrdenItem { NumeroOrden=4, NombreCliente="Ana Sosa",       Placa="GHI-321", FechaOrden=DateTime.Today.AddDays(-3), Estado="En Espera", Prioridad="Urgente", Precio=5000 },
                new OrdenItem { NumeroOrden=5, NombreCliente="Luis Torres",    Placa="JKL-654", FechaOrden=DateTime.Today.AddDays(-4), Estado="Reparando", Prioridad="Alta",    Precio=2100 },
            };

            AplicarFiltros();
            ActualizarNotificaciones();
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
            => AplicarFiltros();

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            var opciones = new[] { "Todos", "En Espera", "Reparando", "Finalizado" };

            var ventana = new Window
            {
                Title = "Filtrar por Estado",
                Width = 260,
                Height = 200,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252836")),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(20) };

            var label = new TextBlock
            {
                Text = "Filtrar por estado:",
                Foreground = Brushes.White,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var cmb = new ComboBox { Height = 36, Margin = new Thickness(0, 0, 0, 16) };
            foreach (var op in opciones) cmb.Items.Add(op);
            cmb.SelectedItem = _filtroEstadoActual;

            var btn = new Button
            {
                Content = "Aplicar",
                Height = 36,
                Foreground = Brushes.White,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D7EFF")),
                BorderThickness = new Thickness(0)
            };

            btn.Click += (s, ev) =>
            {
                _filtroEstadoActual = cmb.SelectedItem?.ToString() ?? "Todos";
                AplicarFiltros();
                ventana.Close();
            };

            stack.Children.Add(label);
            stack.Children.Add(cmb);
            stack.Children.Add(btn);
            ventana.Content = stack;
            ventana.ShowDialog();
        }

        private void AplicarFiltros()
        {
            string buscar = txtBuscar?.Text?.Trim().ToLower() ?? string.Empty;

            var resultado = _todasLasOrdenes.AsEnumerable();

            if (!string.IsNullOrEmpty(buscar))
                resultado = resultado.Where(o =>
                    o.NombreCliente.ToLower().Contains(buscar) ||
                    o.Placa.ToLower().Contains(buscar));

            if (_filtroEstadoActual != "Todos")
                resultado = resultado.Where(o => o.Estado == _filtroEstadoActual);

            _ordenesFiltradas.Clear();
            foreach (var item in resultado)
                _ordenesFiltradas.Add(item);

            if (txtContador != null)
                txtContador.Text = $"{_ordenesFiltradas.Count} orden(es)";
        }

        private void ActualizarNotificaciones()
        {
            int count = _todasLasOrdenes.Count(o =>
                o.Prioridad == "Urgente" || o.Estado == "En Espera");
            badgeNotif.Badge = count > 0 ? count.ToString() : "0";
        }

        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            var pendientes = _todasLasOrdenes
                .Where(o => o.Prioridad == "Urgente" || o.Estado == "En Espera")
                .ToList();

            if (pendientes.Count == 0)
            {
                MessageBox.Show("No hay notificaciones pendientes.", "Notificaciones",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string msg = "⚠️ Órdenes que requieren atención:\n\n";
            foreach (var o in pendientes)
                msg += $"• #{o.NumeroOrden} — {o.NombreCliente} ({o.Placa}) — {o.Estado} [{o.Prioridad}]\n";

            MessageBox.Show(msg, $"Notificaciones ({pendientes.Count})",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void BtnNuevaOrden_Click(object sender, RoutedEventArgs e)
        {
            MainWindow ventana = new MainWindow();
            ventana.Show();
            this.Close();
        }
    }
}