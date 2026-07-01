using System.Windows;
using System.Windows.Input;

namespace Login
{
    public partial class OpcionSesion : Window
    {
        private readonly string _correoUsuario;
        private bool _navegando;

        public OpcionSesion(string correo)
        {
            InitializeComponent();
            _correoUsuario = string.IsNullOrWhiteSpace(correo) ? string.Empty : correo.Trim();

            if (string.IsNullOrEmpty(_correoUsuario))
            {
                MessageBox.Show(
                    "No se recibió un correo válido para continuar con la verificación.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            }
            catch (InvalidOperationException)
            { }
        }

        private void Navegar<T>(Func<T> crear) where T : Window
        {
            if (_navegando) return;
            _navegando = true;

            T ventanaNueva = null;

            try
            {
                ventanaNueva = crear();
                ventanaNueva.Show();

                if (!IsClosed(this))
                    this.Close();
            }
            catch (Exception ex)
            {
                TryCerrar(ventanaNueva);

                _navegando = false;
                MessageBox.Show(
                    "No se pudo abrir la siguiente ventana:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool IsClosed(Window w)
        {
            try
            {
                _ = w.IsVisible;
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static void TryCerrar(Window w)
        {
            if (w is null) return;
            try { w.Close(); }
            catch
            { }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MainWindow());

        private void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_correoUsuario))
            {
                MessageBox.Show(
                    "No es posible enviar el código: no hay un correo asociado a esta sesión.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Navegar(() => new Verificacion2FA(_correoUsuario));
        }

        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_correoUsuario))
            {
                MessageBox.Show(
                    "No es posible iniciar el reconocimiento facial: no hay un correo asociado a esta sesión.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Navegar(() => new ReconocimientoFacial(_correoUsuario));
        }
    }
}