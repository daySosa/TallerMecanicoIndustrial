using Login.Clases;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Login.Clases
{
    internal class RecuperarContrasenia
    {
        private clsAutenticacion _auth = new clsAutenticacion();
        private clsConexion _conexion = new clsConexion();
        private string _correoRecuperacion = string.Empty;
        private MainWindow _mainWindow;

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

            // Funciona si el root es un Grid 
            if (_mainWindow.Content is Grid rootGrid)
            {
                rootGrid.Children.Add(_overlayGrid);
            }
            else
            {
                // Si no es Grid, envolvemos el contenido actual en uno
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

        private TextBox CrearCampoTexto(bool esPassword = false)
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

        //  PASO 1: Correo 
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

            btnSiguiente.Click += (s, e) =>
            {
                string correo = txtCorreo.Text.Trim();
                lblError.Visibility = Visibility.Collapsed;

                if (!correo.Contains("@") || !correo.Contains("."))
                {
                    lblError.Text = "⚠ El formato del correo no es válido.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                if (!CorreoExisteEnBD(correo))
                {
                    lblError.Text = "⚠ No encontramos una cuenta con ese correo.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                string codigo = _auth.GenerarCodigo(correo);
                bool enviado = _auth.EnviarCorreo(correo, codigo);

                if (!enviado)
                {
                    lblError.Text = "⚠ No se pudo enviar el correo. Intenta nuevamente.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                _correoRecuperacion = correo;
                MostrarPasoOTP();
            };

            btnRow.Children.Add(btnCancelar);
            btnRow.Children.Add(new UIElement());
            var spacer = new Border { Width = 8 };
            btnRow.Children.Add(spacer);
            btnRow.Children.Add(btnSiguiente);

            _contenidoPanel.Children.Add(CrearTitulo("Recuperar contraseña"));
            _contenidoPanel.Children.Add(CrearMensaje("Ingresa el correo con el que te registraste y te enviaremos un código de verificación."));
            _contenidoPanel.Children.Add(txtCorreo);
            _contenidoPanel.Children.Add(lblError);
            _contenidoPanel.Children.Add(btnRow);

            txtCorreo.Focus();
        }

        // PASO 2: OTP 
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
            var btnSiguiente = CrearBoton("Verificar", "#2563EB", 110);

            btnAtras.Click += (s, e) => MostrarPasoCorreo();

            btnSiguiente.Click += (s, e) =>
            {
                string otp = txtOTP.Text.Trim();
                lblError.Visibility = Visibility.Collapsed;

                if (otp.Length != 6)
                {
                    lblError.Text = "⚠ El código debe tener exactamente 6 dígitos.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                if (!_auth.ValidarCodigo(_correoRecuperacion, otp))
                {
                    lblError.Text = "⚠ Código incorrecto, expirado o superaste los 3 intentos.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                MostrarPasoNuevaContrasena();
            };

            var spacer = new Border { Width = 8 };
            btnRow.Children.Add(btnAtras);
            btnRow.Children.Add(spacer);
            btnRow.Children.Add(btnSiguiente);

            _contenidoPanel.Children.Add(CrearTitulo("Código de verificación"));
            _contenidoPanel.Children.Add(CrearMensaje($"Ingresa el código de 6 dígitos enviado a {_correoRecuperacion}.\nExpira en 5 minutos."));
            _contenidoPanel.Children.Add(txtOTP);
            _contenidoPanel.Children.Add(lblError);
            _contenidoPanel.Children.Add(btnRow);

            txtOTP.Focus();
        }

        //PASO 3: Nueva contraseña 
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

            btnGuardar.Click += (s, e) =>
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

                if (!ActualizarContrasenaEnBD(_correoRecuperacion, nueva))
                {
                    lblError.Text = "⚠ Error al guardar. Intenta nuevamente.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                MostrarExito();
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

        private bool CorreoExisteEnBD(string correo)
        {
            try
            {
                _conexion.Abrir();
                string query = "SELECT COUNT(1) FROM LOGIN WHERE Usuario_Email = @Correo";
                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Correo", correo);
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al verificar correo: " + ex.Message);
                return false;
            }
            finally
            {
                _conexion.Cerrar();
            }
        }

        private bool ActualizarContrasenaEnBD(string correo, string nuevaContrasena)
        {
            try
            {
                _conexion.Abrir();
                string query = "UPDATE LOGIN SET Usuario_Contraseña = @Nueva WHERE Usuario_Email = @Correo";
                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Nueva", nuevaContrasena);
                cmd.Parameters.AddWithValue("@Correo", correo);
                int filas = cmd.ExecuteNonQuery();
                return filas > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar contraseña: " + ex.Message);
                return false;
            }
            finally
            {
                _conexion.Cerrar();
            }
        }
    }
}