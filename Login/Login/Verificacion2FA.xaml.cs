using Login.Clases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Login
{
    public partial class Verificacion2FA : Window
    {
        private readonly string _correoUsuario;
        private readonly clsAutenticacion _autenticacion = new();
        private readonly DispatcherTimer _timer = new();
        private int _segundos = 300; // 5 minutos

        private TextBox[] _cajas;

        // ════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ════════════════════════════════════════════════════════════

        public Verificacion2FA(string correo)
        {
            InitializeComponent();
            _correoUsuario = correo;

            _cajas = new[] { d1, d2, d3, d4, d5, d6 };
            ConfigurarCajas();
            IniciarTimer();
        }

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // ════════════════════════════════════════════════════════════
        // CAJAS DE DÍGITOS
        // ════════════════════════════════════════════════════════════

        private void ConfigurarCajas()
        {
            foreach (var caja in _cajas)
            {
                caja.PreviewTextInput += Caja_PreviewTextInput;
                caja.TextChanged += Caja_TextChanged;
                caja.KeyDown += Caja_KeyDown;
                caja.GotFocus += (s, e) => ((TextBox)s).SelectAll();
            }
        }

        private void Caja_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo dígitos
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void Caja_TextChanged(object sender, TextChangedEventArgs e)
        {
            OcultarError();

            var caja = (TextBox)sender;
            int index = Array.IndexOf(_cajas, caja);

            if (caja.Text.Length == 1 && index < _cajas.Length - 1)
                _cajas[index + 1].Focus();
        }

        private void Caja_KeyDown(object sender, KeyEventArgs e)
        {
            var caja = (TextBox)sender;
            int index = Array.IndexOf(_cajas, caja);

            if (e.Key == Key.Back && string.IsNullOrEmpty(caja.Text) && index > 0)
            {
                _cajas[index - 1].Focus();
                _cajas[index - 1].Clear();
                e.Handled = true;
            }
        }

        private string ObtenerCodigo()
            => string.Concat(_cajas.Select(c => c.Text));

        private void LimpiarCajas()
        {
            foreach (var c in _cajas) c.Clear();
            _cajas[0].Focus();
        }

        // ════════════════════════════════════════════════════════════
        // TIMER
        // ════════════════════════════════════════════════════════════

        private void IniciarTimer()
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _segundos--;
            int min = _segundos / 60;
            int seg = _segundos % 60;
            runTimer.Text = $"{min:D2}:{seg:D2}";

            if (_segundos <= 0)
            {
                _timer.Stop();
                runTimer.Text = "00:00";
                btnVerificar.IsEnabled = false;
                MostrarError("⚠ El código ha expirado. Reenvíalo para continuar.");
            }
        }

        // ════════════════════════════════════════════════════════════
        // VERIFICAR
        // ════════════════════════════════════════════════════════════

        private void BtnVerificar_Click(object sender, RoutedEventArgs e)
        {
            string codigo = ObtenerCodigo();

            if (codigo.Length < 6)
            {
                MostrarError("⚠ Completa los 6 dígitos del código.");
                return;
            }

            if (_autenticacion.ValidarCodigo(_correoUsuario, codigo))
            {
                _timer.Stop();
                new Dasboard_Prueba.MenuPrincipal().Show();
                this.Close();
            }
            else
            {
                MostrarError("⚠ Código incorrecto o expirado. Intenta nuevamente.");
                LimpiarCajas();
            }
        }

        // ════════════════════════════════════════════════════════════
        // REENVIAR
        // ════════════════════════════════════════════════════════════

        private async void BtnReenviar_Click(object sender, RoutedEventArgs e)
        {
            btnReenviar.IsEnabled = false;

            string codigo = _autenticacion.GenerarCodigo(_correoUsuario);
            bool enviado = await Task.Run(
                () => _autenticacion.EnviarCorreo(_correoUsuario, codigo));

            if (enviado)
            {
                // Reiniciar timer
                _timer.Stop();
                _segundos = 300;
                runTimer.Text = "05:00";
                btnVerificar.IsEnabled = true;
                _timer.Start();

                LimpiarCajas();
                OcultarError();

                MessageBox.Show("✅ Código reenviado a tu correo.", "Código enviado",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("⚠ No se pudo reenviar el código. Intenta nuevamente.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            btnReenviar.IsEnabled = true;
        }

        // ════════════════════════════════════════════════════════════
        // REGRESAR
        // ════════════════════════════════════════════════════════════

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            new OpcionSesion(_correoUsuario).Show();
            this.Close();
        }

        // ════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════

        private void MostrarError(string msg)
        {
            txtErrorCodigo.Text = msg;
            txtErrorCodigo.Visibility = Visibility.Visible;
        }

        private void OcultarError()
            => txtErrorCodigo.Visibility = Visibility.Collapsed;

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}