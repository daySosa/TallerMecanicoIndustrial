using Login.Clases;
using System.Data.SqlClient;
using System.Windows;

namespace Login.Clases
{
    /// <summary>
    /// Maneja el flujo completo de recuperación de contraseña desde VentanaLogin.
    /// No necesita ventana propia — guía al usuario paso a paso con diálogos.
    /// </summary>
    internal class RecuperarContrasenia
    {
        private clsAutenticacion _auth = new clsAutenticacion();
        private clsConexion _conexion = new clsConexion();
        private string _correoRecuperacion = string.Empty;
        private VentanaLogin ventanaLogin;

        public RecuperarContrasenia(VentanaLogin ventanaLogin)
        {
            this.ventanaLogin = ventanaLogin;
        }

  
        // Metodo principal: llama desde el boton de olvidar contrasenia
        
        public void IniciarFlujo()
        {
            // PASO 1: Pedir correo al usuario
            string correo = MostrarInputDialog(
                "Ingresa tu correo",
                "Escribe el correo electrónico con el que te registraste:");

            if (string.IsNullOrWhiteSpace(correo)) return; // Usuario cancelo

            if (!correo.Contains("@") || !correo.Contains("."))
            {
                MessageBox.Show("⚠ El formato del correo no es valido.",
                    "Correo invalido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!CorreoExisteEnBD(correo))
            {
                MessageBox.Show("⚠ No encontramos una cuenta registrada con ese correo.",
                    "Correo no encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Generar y enviar OTP
            string codigo = _auth.GenerarCodigo(correo);
            bool enviado = _auth.EnviarCorreo(correo, codigo);

            if (!enviado)
            {
                MessageBox.Show("⚠ No se pudo enviar el correo. Intenta nuevamente.",
                    "Error de envio", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _correoRecuperacion = correo;
            MessageBox.Show($"✅ Codigo enviado a {correo}.\n\nRevisa tu bandeja. El codigo expira en 5 minutos.",
                "Codigo enviado", MessageBoxButton.OK, MessageBoxImage.Information);

            // PASO 2: Pedir OTP
            string otp = MostrarInputDialog(
                "Ingresa el codigo",
                "Escribe el codigo de 6 digitos que recibiste en tu correo:");

            if (string.IsNullOrWhiteSpace(otp)) return; // Usuario canceló

            if (otp.Length != 6)
            {
                MessageBox.Show("⚠ El codigo debe tener exactamente 6 digitos.",
                    "Codigo invalido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool otpValido = _auth.ValidarCodigo(_correoRecuperacion, otp);

            if (!otpValido)
            {
                MessageBox.Show("⚠ Codigo incorrecto, expirado o superaste los 3 intentos.\n\nInicia el proceso de nuevo.",
                    "Codigo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // PASO 3: Nueva contraseña
            string nueva = MostrarInputDialog(
                "Nueva contraseña",
                "Escribe tu nueva contraseña (Al menos 6 caracteres)");

            if (string.IsNullOrWhiteSpace(nueva)) return; // Usuario cancelo

            if (nueva.Length < 6)
            {
                MessageBox.Show("⚠ La contraseña debe tener al menos 6 caracteres.",
                    "Contraseña muy corta", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string confirmar = MostrarInputDialog(
                "Paso 3 de 3 — Confirma tu contraseña",
                "Escribe nuevamente tu nueva contraseña:");

            if (string.IsNullOrWhiteSpace(confirmar)) return;

            if (nueva != confirmar)
            {
                MessageBox.Show("⚠ Las contraseñas no coinciden. Intenta de nuevo.",
                    "No coinciden", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool actualizado = ActualizarContrasenaEnBD(_correoRecuperacion, nueva);

            if (actualizado)
            {
                MessageBox.Show("✅ ¡Contraseña actualizada correctamente!\n\nYa puedes iniciar sesion.",
                    "Exito", MessageBoxButton.OK, MessageBoxImage.Information);
                _correoRecuperacion = string.Empty;
            }
            else
            {
                MessageBox.Show("⚠ Error al guardar la contraseña. Intenta nuevamente.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private string MostrarInputDialog(string titulo, string mensaje)
        {
            var dialog = new System.Windows.Window
            {
                Title = titulo,
                Width = 420,
                Height = 200,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E2A3A")),
                WindowStyle = System.Windows.WindowStyle.ToolWindow
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(20) };

            var lbl = new System.Windows.Controls.TextBlock
            {
                Text = mensaje,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 13,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new System.Windows.Thickness(0, 0, 0, 12)
            };

            var txt = new System.Windows.Controls.TextBox
            {
                Height = 36,
                FontSize = 13,
                Padding = new System.Windows.Thickness(8, 0, 8, 0),
                VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new System.Windows.Thickness(0, 0, 0, 16)
            };

            var btnPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            var btnCancelar = new System.Windows.Controls.Button
            {
                Content = "Cancelar",
                Width = 90,
                Height = 34,
                Margin = new System.Windows.Thickness(0, 0, 8, 0),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new System.Windows.Thickness(0)
            };

            var btnAceptar = new System.Windows.Controls.Button
            {
                Content = "Aceptar",
                Width = 90,
                Height = 34,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2563EB")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new System.Windows.Thickness(0)
            };

            string resultado = string.Empty;

            btnAceptar.Click += (s, e) => { resultado = txt.Text.Trim(); dialog.Close(); };
            btnCancelar.Click += (s, e) => { dialog.Close(); };

            btnPanel.Children.Add(btnCancelar);
            btnPanel.Children.Add(btnAceptar);
            panel.Children.Add(lbl);
            panel.Children.Add(txt);
            panel.Children.Add(btnPanel);
            dialog.Content = panel;

            txt.Focus();
            dialog.ShowDialog();

            return resultado;
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
