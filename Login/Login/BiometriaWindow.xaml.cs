using Login.Clases;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace InterfazClientes
{
    public partial class VentanaBiometria : Window
    {
        private readonly clsConsultasBD _db = new();
        private DataTable _usuariosCache = new();
        private int _usuarioIdSeleccionado = -1;
        private byte[]? _rostroCapturado = null;

        // cámara (usa AForge, OpenCvSharp, o lo que uses en tu proyecto)
        // Si no tienes cámara aún, los métodos quedan como stubs.

        public VentanaBiometria()
        {
            InitializeComponent();
            CargarUsuarios();
        }

        // ── MOVER VENTANA ────────────────────────────────────────────

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // ── USUARIOS ─────────────────────────────────────────────────

        private void CargarUsuarios()
        {
            try
            {
                _usuariosCache = _db.ObtenerUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar usuarios: " + ex.Message);
            }
        }

        private void txtBuscarUsuario_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscarUsuario.Text.Trim().Replace("'", "''");

            if (_usuariosCache == null) return;

            _usuariosCache.DefaultView.RowFilter = string.IsNullOrWhiteSpace(texto)
                ? string.Empty
                : $"Usuario_Nombre LIKE '%{texto}%' OR Usuario_Correo LIKE '%{texto}%'";

            // Si hay exactamente un resultado, seleccionarlo automáticamente
            if (_usuariosCache.DefaultView.Count == 1)
                SeleccionarUsuario(_usuariosCache.DefaultView[0].Row);
            else
                LimpiarSeleccion();
        }

        private void SeleccionarUsuario(DataRow row)
        {
            _usuarioIdSeleccionado = Convert.ToInt32(row["Usuario_ID"]);
            string nombre = $"{row["Usuario_Nombre"]} {row["Usuario_Apellido"]}";
            txtUsuarioSeleccionado.Text = nombre;
            txtUsuarioSeleccionado.Foreground = Pincel("#FFFFFF");
            ActualizarBotonesCaptura();
        }

        private void LimpiarSeleccion()
        {
            _usuarioIdSeleccionado = -1;
            txtUsuarioSeleccionado.Text = "Sin usuario seleccionado";
            txtUsuarioSeleccionado.Foreground = Pincel("#353a58");
            ActualizarBotonesCaptura();
        }

        // ── CÁMARA ───────────────────────────────────────────────────

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: inicializar tu componente de cámara aquí
                // Ejemplo con AForge:
                // _videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                // _videoSource.NewFrame += NuevoFrame;
                // _videoSource.Start();

                panelSinCamara.Visibility = Visibility.Collapsed;
                btnCapturar.IsEnabled = true;

                // Placeholder hasta integrar cámara real
                MessageBox.Show("Cámara activada (integra tu librería de cámara aquí).",
                    "Cámara", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al iniciar cámara: " + ex.Message);
            }
        }

        private void btnCapturar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: capturar frame actual de la cámara
                // Ejemplo: _rostroCapturado = CapturarFrameComoBytes();

                // Placeholder: simula captura exitosa
                _rostroCapturado = Array.Empty<byte>();

                txtEstadoRegistro.Text = "✔ Rostro capturado";
                txtEstadoDeteccion.Text = "Rostro detectado correctamente";
                txtEstadoDeteccion.Foreground = Pincel("#4CAF50");
                iconDeteccion.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline;
                iconDeteccion.Foreground = Pincel("#4CAF50");

                ActualizarBotonesCaptura();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al capturar: " + ex.Message);
            }
        }

        // ── GUARDAR / ELIMINAR ───────────────────────────────────────

        private void btnGuardarBiometria_Click(object sender, RoutedEventArgs e)
        {
            if (_usuarioIdSeleccionado == -1)
            {
                MessageBox.Show("Selecciona un usuario primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_rostroCapturado == null || _rostroCapturado.Length == 0)
            {
                MessageBox.Show("Captura el rostro antes de guardar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // TODO: llamar a _db.GuardarBiometria(_usuarioIdSeleccionado, _rostroCapturado);
                MessageBox.Show("✅ Biometría guardada correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        private void btnEliminarBiometria_Click(object sender, RoutedEventArgs e)
        {
            if (_usuarioIdSeleccionado == -1)
            {
                MessageBox.Show("Selecciona un usuario primero.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "¿Estás seguro de eliminar el registro biométrico de este usuario?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                // TODO: llamar a _db.EliminarBiometria(_usuarioIdSeleccionado);
                MessageBox.Show("✅ Registro biométrico eliminado.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarSeleccion();
                _rostroCapturado = null;
                ActualizarBotonesCaptura();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar: " + ex.Message);
            }
        }

        // ── CERRAR ───────────────────────────────────────────────────

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            // TODO: detener cámara si está activa
            // _videoSource?.SignalToStop();
            this.Close();
        }

        // ── HELPERS ──────────────────────────────────────────────────

        private void ActualizarBotonesCaptura()
        {
            bool hayUsuario = _usuarioIdSeleccionado != -1;
            bool hayCaptura = _rostroCapturado != null && _rostroCapturado.Length > 0;

            btnGuardarBiometria.IsEnabled = hayUsuario && hayCaptura;
            btnEliminarBiometria.IsEnabled = hayUsuario;
        }

        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}