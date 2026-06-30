using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Login.Clases
{
    internal class RecuperarContrasenia
    {
        private readonly clsConsultasBD _db = new();
        private string _correoRecuperacion = string.Empty;
        private readonly MainWindow _mainWindow;

        private Grid _overlayGrid;
        private Border _panelCentral;
        private StackPanel _contenidoPanel;

        public RecuperarContrasenia(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void IniciarFlujo()
        {
            ConstruirOverlay();
            MostrarPasoCorreo();
        }

        private void ConstruirOverlay()
        {
            _overlayGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _panelCentral = new Border
            {
                Width = 420,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1E2A3A")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(30),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _contenidoPanel = new StackPanel();
            _panelCentral.Child = _contenidoPanel;
            _overlayGrid.Children.Add(_panelCentral);

            if (_mainWindow.Content is Grid rootGrid)
            {
                rootGrid.Children.Add(_overlayGrid);
            }
            else
            {
                var contenidoActual = _mainWindow.Content as UIElement;
                var nuevoGrid = new Grid();
                _mainWindow.Content = null;
                if (contenidoActual != null)
                    nuevoGrid.Children.Add(contenidoActual);
                nuevoGrid.Children.Add(_overlayGrid);
                _mainWindow.Content = nuevoGrid;
            }
        }

        private void CerrarOverlay()
        {
            if (_mainWindow.Content is Grid rootGrid)
                rootGrid.Children.Remove(_overlayGrid);
        }

        private TextBlock CrearTitulo(string texto)
        {
            return new TextBlock
            {
                Text = texto,
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private TextBlock CrearMensaje(string texto)
        {
            return new TextBlock
            {
                Text = texto,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#94A3B8")),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            };
        }

        private TextBox CrearCampoTexto()
        {
            return new TextBox
            {
                Height = 38,
                FontSize = 13,
                Padding = new Thickness(10, 0, 10, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16),
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#0F172A")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#334155")),
                CaretBrush = Brushes.White
            };
        }

        private Button CrearBoton(string texto, string colorHex, double width = 120)
        {
            return new Button
            {
                Content = texto,
                Width = width,
                Height = 36,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(colorHex)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }

        private TextBlock CrearMensajeError(string texto)
        {
            return new TextBlock
            {
                Text = texto,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#F87171")),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, -10, 0, 12),
                Visibility = Visibility.Collapsed
            };
        }

        // PASO 1: Correo
        private void MostrarPasoCorreo()
        {
            _contenidoPanel.Children.Clear();

            var txtCorreo = CrearCampoTexto();
            var lblError = CrearMensajeError("⚠ Correo inválido o no encontrado.");

            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnCancelar = CrearBoton("Cancelar", "#475569", 100);
            var btnSiguiente = CrearBoton("Siguiente", "#2563EB", 110);

            btnCancelar.Click += (s, e) => CerrarOverlay();

            btnSiguiente.Click += async (s, e) =>
            {
                string correo = txtCorreo.Text.Trim();
                lblError.Visibility = Visibility.Collapsed;

                if (!correo.Contains("@") || !correo.Contains("."))
                {
                    lblError.Text = "⚠ El formato del correo no es válido.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                btnSiguiente.IsEnabled = false;

                try
                {
                    bool existe = await Task.Run(() => _db.ExisteCorreoLogin(correo));
                    if (!existe)
                    {
                        lblError.Text = "⚠ No encontramos una cuenta con ese correo.";
                        lblError.Visibility = Visibility.Visible;
                        return;
                    }

                    bool enviado = await Task.Run(() =>
                    {
                        string codigo = _db.GenerarCodigoOTP(correo);
                        return _db.EnviarCorreoOTP(correo, codigo);
                    });

                    if (!enviado)
                    {
                        lblError.Text = "⚠ No se pudo enviar el correo. Intenta nuevamente.";
                        lblError.Visibility = Visibility.Visible;
                        return;
                    }

                    _correoRecuperacion = correo;
                    MostrarPasoOTP();
                }
                catch (Exception ex)
                {
                    lblError.Text = "⚠ " + ex.Message;
                    lblError.Visibility = Visibility.Visible;
                }
                finally
                {
                    btnSiguiente.IsEnabled = true;
                }
            };

            var spacer = new Border { Width = 8 };
            btnRow.Children.Add(btnCancelar);
            btnRow.Children.Add(spacer);
            btnRow.Children.Add(btnSiguiente);

            _contenidoPanel.Children.Add(CrearTitulo("Recuperar contraseña"));
            _contenidoPanel.Children.Add(CrearMensaje("Ingresa el correo con el que te registraste y te enviaremos un código de verificación."));
            _contenidoPanel.Children.Add(txtCorreo);
            _contenidoPanel.Children.Add(lblError);
            _contenidoPanel.Children.Add(btnRow);

            txtCorreo.Focus();
        }
        private void MostrarPasoOTP()
        {
            _contenidoPanel.Children.Clear();

            var txtOTP = CrearCampoTexto();
            txtOTP.MaxLength = 6;
            var lblError = CrearMensajeError("");

            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnAtras = CrearBoton("← Atrás", "#475569", 100);
            var btnVerificar = CrearBoton("Verificar", "#2563EB", 110);

            btnAtras.Click += (s, e) => MostrarPasoCorreo();

            btnVerificar.Click += async (s, e) =>
            {
                string otp = txtOTP.Text.Trim();
                lblError.Visibility = Visibility.Collapsed;

                var (esValido, mensaje) = clsValidacionCodigo2FA.ValidarCodigo(otp);
                if (!esValido)
                {
                    lblError.Text = mensaje;
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                btnVerificar.IsEnabled = false;

                try
                {
                    bool valido = await Task.Run(() => _db.ValidarCodigoOTP(_correoRecuperacion, otp));
                    if (!valido)
                    {
                        lblError.Text = "⚠ Código incorrecto, expirado o superaste los 3 intentos.";
                        lblError.Visibility = Visibility.Visible;
                        return;
                    }

                    MostrarPasoNuevaContrasena();
                }
                catch (Exception ex)
                {
                    lblError.Text = "⚠ " + ex.Message;
                    lblError.Visibility = Visibility.Visible;
                }
                finally
                {
                    btnVerificar.IsEnabled = true;
                }
            };

            var spacer = new Border { Width = 8 };
            btnRow.Children.Add(btnAtras);
            btnRow.Children.Add(spacer);
            btnRow.Children.Add(btnVerificar);

            _contenidoPanel.Children.Add(CrearTitulo("Código de verificación"));
            _contenidoPanel.Children.Add(CrearMensaje($"Ingresa el código de 6 dígitos enviado a {_correoRecuperacion}.\nExpira en 5 minutos."));
            _contenidoPanel.Children.Add(txtOTP);
            _contenidoPanel.Children.Add(lblError);
            _contenidoPanel.Children.Add(btnRow);

            txtOTP.Focus();
        }

        private void MostrarPasoNuevaContrasena()
        {
            _contenidoPanel.Children.Clear();

            var txtNueva = CrearCampoTexto();
            var txtConfirmar = CrearCampoTexto();
            var lblError = CrearMensajeError("");

            AgregarPlaceholder(txtNueva, "Nueva contraseña (mín. 6 caracteres)");
            AgregarPlaceholder(txtConfirmar, "Confirmar contraseña");

            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnAtras = CrearBoton("← Atrás", "#475569", 100);
            var btnGuardar = CrearBoton("Guardar", "#16A34A", 110);

            btnAtras.Click += (s, e) => MostrarPasoOTP();

            btnGuardar.Click += async (s, e) =>
            {
                string nueva = txtNueva.Text.Trim();
                string confirmar = txtConfirmar.Text.Trim();
                lblError.Visibility = Visibility.Collapsed;

                if (nueva.Length < 6)
                {
                    lblError.Text = "⚠ La contraseña debe tener al menos 6 caracteres.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                if (nueva != confirmar)
                {
                    lblError.Text = "⚠ Las contraseñas no coinciden.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                btnGuardar.IsEnabled = false;

                try
                {
                    bool actualizado = await Task.Run(
                        () => _db.ActualizarContrasenaLogin(_correoRecuperacion, nueva));

                    if (!actualizado)
                    {
                        lblError.Text = "⚠ Error al guardar. Intenta nuevamente.";
                        lblError.Visibility = Visibility.Visible;
                        return;
                    }

                    MostrarExito();
                }
                catch (Exception ex)
                {
                    lblError.Text = "⚠ " + ex.Message;
                    lblError.Visibility = Visibility.Visible;
                }
                finally
                {
                    btnGuardar.IsEnabled = true;
                }
            };

            var spacer = new Border { Width = 8 };
            btnRow.Children.Add(btnAtras);
            btnRow.Children.Add(spacer);
            btnRow.Children.Add(btnGuardar);

            _contenidoPanel.Children.Add(CrearTitulo("Nueva contraseña"));
            _contenidoPanel.Children.Add(CrearMensaje("Elige una contraseña segura para tu cuenta."));
            _contenidoPanel.Children.Add(txtNueva);
            _contenidoPanel.Children.Add(txtConfirmar);
            _contenidoPanel.Children.Add(lblError);
            _contenidoPanel.Children.Add(btnRow);

            txtNueva.Focus();
        }

        private void MostrarExito()
        {
            _contenidoPanel.Children.Clear();

            var icono = new TextBlock
            {
                Text = "✅",
                FontSize = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var btnCerrar = CrearBoton("Listo", "#2563EB", 120);
            btnCerrar.HorizontalAlignment = HorizontalAlignment.Center;
            btnCerrar.Margin = new Thickness(0, 16, 0, 0);
            btnCerrar.Click += (s, e) =>
            {
                _correoRecuperacion = string.Empty;
                CerrarOverlay();
            };

            _contenidoPanel.Children.Add(icono);
            _contenidoPanel.Children.Add(CrearTitulo("¡Contraseña actualizada!"));
            _contenidoPanel.Children.Add(CrearMensaje("Ya puedes iniciar sesión con tu nueva contraseña."));
            _contenidoPanel.Children.Add(btnCerrar);
        }

        private void AgregarPlaceholder(TextBox txt, string placeholder)
        {
            txt.Text = placeholder;
            txt.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#64748B"));

            txt.GotFocus += (s, e) =>
            {
                if (txt.Text == placeholder)
                {
                    txt.Text = string.Empty;
                    txt.Foreground = Brushes.White;
                }
            };

            txt.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    txt.Text = placeholder;
                    txt.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#64748B"));
                }
            };
        }
    }
}