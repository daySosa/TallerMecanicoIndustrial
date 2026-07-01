using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

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

        private static readonly SolidColorBrush BrushFondoOverlay = Congelar("#0B1120", 0.72);
        private static readonly SolidColorBrush BrushPanel = Congelar("#03002E", 0.92);
        private static readonly SolidColorBrush BrushBorde = Congelar("#FFFFFF", 0.12);
        private static readonly SolidColorBrush BrushCampoFondo = Congelar("#FFFFFF", 0.07);
        private static readonly SolidColorBrush BrushCampoBorde = Congelar("#FFFFFF", 0.14);
        private static readonly SolidColorBrush BrushTexto = Congelar("#FFFFFF");
        private static readonly SolidColorBrush BrushSubtexto = Congelar("#94A3B8");
        private static readonly SolidColorBrush BrushPlaceholder = Congelar("#64748B");
        private static readonly SolidColorBrush BrushError = Congelar("#F87171");
        private static readonly SolidColorBrush BrushPrimario = Congelar("#02006C");
        private static readonly SolidColorBrush BrushPrimarioHover = Congelar("#0284C7");
        private static readonly SolidColorBrush BrushSecundario = Congelar("#FFFFFF", 0.08);
        private static readonly SolidColorBrush BrushExito = Congelar("#16A34A");
        private static readonly SolidColorBrush BrushExitoHover = Congelar("#15803D");
        private static readonly SolidColorBrush BrushTransparente = Congelar("Transparent");

        private static readonly Effect SombraPanel = CrearSombra();

        private static SolidColorBrush Congelar(string hex, double? opacidad = null)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            if (opacidad.HasValue) brush.Opacity = opacidad.Value;
            brush.Freeze();
            return brush;
        }

        private static Effect CrearSombra()
        {
            var efecto = new DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.55,
                BlurRadius = 40,
                ShadowDepth = 0
            };
            efecto.Freeze();
            return efecto;
        }

        public RecuperarContrasenia(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void IniciarFlujo()
        {
            ConstruirOverlay();
            ConstruirPasoCorreo();
            FadeIn(_overlayGrid, 180);
        }

        private void ConstruirOverlay()
        {
            _overlayGrid = new Grid
            {
                Background = BrushFondoOverlay,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Opacity = 0
            };

            _panelCentral = new Border
            {
                Width = 420,
                Background = BrushPanel,
                BorderBrush = BrushBorde,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(32, 30, 32, 30),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = SombraPanel
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
            FadeOut(_overlayGrid, 140, () =>
            {
                if (_mainWindow.Content is Grid rootGrid)
                    rootGrid.Children.Remove(_overlayGrid);
            });
        }
        private static void FadeIn(UIElement el, int ms, System.Action alTerminar = null)
        {
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(ms))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            if (alTerminar != null) anim.Completed += (s, e) => alTerminar();
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private static void FadeOut(UIElement el, int ms, System.Action alTerminar = null)
        {
            var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(ms))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            if (alTerminar != null) anim.Completed += (s, e) => alTerminar();
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void TransicionarA(System.Action construirNuevoPaso)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(110))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };

            fadeOut.Completed += (s, e) =>
            {
                construirNuevoPaso();
                FadeIn(_contenidoPanel, 180);
            };

            _contenidoPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private TextBlock CrearTitulo(string texto) => new()
        {
            Text = texto,
            Foreground = BrushTexto,
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6),
            TextWrapping = TextWrapping.Wrap
        };

        private TextBlock CrearMensaje(string texto) => new()
        {
            Text = texto,
            Foreground = BrushSubtexto,
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };

        private Border CrearContenedorCampo(out TextBox campo)
        {
            campo = new TextBox
            {
                Background = BrushTransparente,
                BorderThickness = new Thickness(0),
                Foreground = BrushTexto,
                FontSize = 13,
                Padding = new Thickness(14, 0, 14, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                CaretBrush = BrushTexto
            };

            var contenedor = new Border
            {
                Height = 44,
                CornerRadius = new CornerRadius(10),
                Background = BrushCampoFondo,
                BorderBrush = BrushCampoBorde,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 16),
                Child = campo
            };

            campo.GotFocus += (s, e) =>
            {
                contenedor.BorderBrush = BrushPrimarioHover;
                contenedor.BorderThickness = new Thickness(1.5);
            };
            campo.LostFocus += (s, e) =>
            {
                contenedor.BorderBrush = BrushCampoBorde;
                contenedor.BorderThickness = new Thickness(1);
            };

            return contenedor;
        }

        private Button CrearBoton(string texto, bool esPrimario, double width = 118, bool esExito = false)
        {
            var fondoNormal = esExito ? BrushExito : (esPrimario ? BrushPrimario : BrushSecundario);
            var fondoHover = esExito ? BrushExitoHover : (esPrimario ? BrushPrimarioHover : BrushCampoFondo);

            var boton = new Button
            {
                Content = texto,
                Width = width,
                Height = 38,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushTexto,
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = fondoNormal,
                BorderThickness = new Thickness(0)
            };

            boton.Style = new Style(typeof(Button))
            {
                Setters =
                {
                    new Setter(Button.TemplateProperty, CrearPlantillaBoton())
                }
            };

            boton.Resources[SystemColors.HighlightBrushKey] = fondoHover;
            boton.MouseEnter += (s, e) => boton.Background = fondoHover;
            boton.MouseLeave += (s, e) => boton.Background = fondoNormal;

            return boton;
        }

        private static ControlTemplate _plantillaBotonCache;
        private static ControlTemplate CrearPlantillaBoton()
        {
            if (_plantillaBotonCache != null) return _plantillaBotonCache;

            var plantilla = new ControlTemplate(typeof(Button));
            var borde = new FrameworkElementFactory(typeof(Border));
            borde.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            borde.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background")
            { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });

            var contenido = new FrameworkElementFactory(typeof(ContentPresenter));
            contenido.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contenido.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borde.AppendChild(contenido);

            plantilla.VisualTree = borde;
            plantilla.Seal();
            _plantillaBotonCache = plantilla;
            return plantilla;
        }

        private TextBlock CrearMensajeError(string texto) => new()
        {
            Text = texto,
            Foreground = BrushError,
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(2, -10, 0, 12),
            Visibility = Visibility.Collapsed
        };

        private void AplicarPlaceholder(TextBox txt, string placeholder)
        {
            txt.Text = placeholder;
            txt.Foreground = BrushPlaceholder;
            txt.Tag = "placeholder";

            txt.GotFocus += (s, e) =>
            {
                if ((string)txt.Tag == "placeholder")
                {
                    txt.Text = string.Empty;
                    txt.Foreground = BrushTexto;
                    txt.Tag = null;
                }
            };

            txt.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    txt.Text = placeholder;
                    txt.Foreground = BrushPlaceholder;
                    txt.Tag = "placeholder";
                }
            };
        }

        private static string ValorReal(TextBox txt) =>
            (string)txt.Tag == "placeholder" ? string.Empty : txt.Text.Trim();

        private void ConstruirPasoCorreo()
        {
            _contenidoPanel.Children.Clear();

            var contenedorCorreo = CrearContenedorCampo(out var txtCorreo);
            var lblError = CrearMensajeError("");

            var btnCancelar = CrearBoton("Cancelar", esPrimario: false, width: 100);
            var btnSiguiente = CrearBoton("Siguiente", esPrimario: true, width: 112);

            btnCancelar.Click += (s, e) => CerrarOverlay();

            btnSiguiente.Click += async (s, e) =>
            {
                string correo = txtCorreo.Text.Trim();
                lblError.Visibility = Visibility.Collapsed;

                if (!correo.Contains('@') || !correo.Contains('.'))
                {
                    lblError.Text = "⚠ El formato del correo no es válido.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                btnSiguiente.IsEnabled = false;
                btnSiguiente.Content = "Verificando...";

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
                    TransicionarA(ConstruirPasoOTP);
                }
                catch (Exception ex)
                {
                    lblError.Text = "⚠ " + ex.Message;
                    lblError.Visibility = Visibility.Visible;
                }
                finally
                {
                    btnSiguiente.IsEnabled = true;
                    btnSiguiente.Content = "Siguiente";
                }
            };

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            btnRow.Children.Add(btnCancelar);
            btnRow.Children.Add(new Border { Width = 8 });
            btnRow.Children.Add(btnSiguiente);

            _contenidoPanel.Children.Add(CrearTitulo("Recuperar contraseña"));
            _contenidoPanel.Children.Add(CrearMensaje("Ingresa el correo con el que te registraste y te enviaremos un código de verificación."));
            _contenidoPanel.Children.Add(contenedorCorreo);
            _contenidoPanel.Children.Add(lblError);
            _contenidoPanel.Children.Add(btnRow);

            txtCorreo.Focus();
        }

        private void ConstruirPasoOTP()
        {
            _contenidoPanel.Children.Clear();

            var contenedorOTP = CrearContenedorCampo(out var txtOTP);
            txtOTP.MaxLength = 6;
            var lblError = CrearMensajeError("");

            var btnAtras = CrearBoton("← Atrás", esPrimario: false, width: 100);
            var btnVerificar = CrearBoton("Verificar", esPrimario: true, width: 112);

            btnAtras.Click += (s, e) => TransicionarA(ConstruirPasoCorreo);

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
                btnVerificar.Content = "Verificando...";

                try
                {
                    bool valido = await Task.Run(() => _db.ValidarCodigoOTP(_correoRecuperacion, otp));
                    if (!valido)
                    {
                        lblError.Text = "⚠ Código incorrecto, expirado o superaste los 3 intentos.";
                        lblError.Visibility = Visibility.Visible;
                        return;
                    }

                    TransicionarA(ConstruirPasoNuevaContrasena);
                }
                catch (Exception ex)
                {
                    lblError.Text = "⚠ " + ex.Message;
                    lblError.Visibility = Visibility.Visible;
                }
                finally
                {
                    btnVerificar.IsEnabled = true;
                    btnVerificar.Content = "Verificar";
                }
            };

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            btnRow.Children.Add(btnAtras);
            btnRow.Children.Add(new Border { Width = 8 });
            btnRow.Children.Add(btnVerificar);

            _contenidoPanel.Children.Add(CrearTitulo("Código de verificación"));
            _contenidoPanel.Children.Add(CrearMensaje($"Ingresa el código de 6 dígitos enviado a {_correoRecuperacion}.\nExpira en 5 minutos."));
            _contenidoPanel.Children.Add(contenedorOTP);
            _contenidoPanel.Children.Add(lblError);
            _contenidoPanel.Children.Add(btnRow);

            txtOTP.Focus();
        }

        private void ConstruirPasoNuevaContrasena()
        {
            _contenidoPanel.Children.Clear();

            var contenedorNueva = CrearContenedorCampo(out var txtNueva);
            var contenedorConfirmar = CrearContenedorCampo(out var txtConfirmar);
            var lblError = CrearMensajeError("");

            AplicarPlaceholder(txtNueva, "Nueva contraseña (mín. 6 caracteres)");
            AplicarPlaceholder(txtConfirmar, "Confirmar contraseña");

            var btnAtras = CrearBoton("← Atrás", esPrimario: false, width: 100);
            var btnGuardar = CrearBoton("Guardar", esPrimario: false, width: 112, esExito: true);

            btnAtras.Click += (s, e) => TransicionarA(ConstruirPasoOTP);

            btnGuardar.Click += async (s, e) =>
            {
                string nueva = ValorReal(txtNueva);
                string confirmar = ValorReal(txtConfirmar);
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
                btnGuardar.Content = "Guardando...";

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

                    TransicionarA(ConstruirExito);
                }
                catch (Exception ex)
                {
                    lblError.Text = "⚠ " + ex.Message;
                    lblError.Visibility = Visibility.Visible;
                }
                finally
                {
                    btnGuardar.IsEnabled = true;
                    btnGuardar.Content = "Guardar";
                }
            };

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            btnRow.Children.Add(btnAtras);
            btnRow.Children.Add(new Border { Width = 8 });
            btnRow.Children.Add(btnGuardar);

            _contenidoPanel.Children.Add(CrearTitulo("Nueva contraseña"));
            _contenidoPanel.Children.Add(CrearMensaje("Elige una contraseña segura para tu cuenta."));
            _contenidoPanel.Children.Add(contenedorNueva);
            _contenidoPanel.Children.Add(contenedorConfirmar);
            _contenidoPanel.Children.Add(lblError);
            _contenidoPanel.Children.Add(btnRow);

            txtNueva.Focus();
        }

        private void ConstruirExito()
        {
            _contenidoPanel.Children.Clear();

            var icono = new TextBlock
            {
                Text = "✅",
                FontSize = 38,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };

            var btnCerrar = CrearBoton("Listo", esPrimario: true, width: 120);
            btnCerrar.HorizontalAlignment = HorizontalAlignment.Center;
            btnCerrar.Margin = new Thickness(0, 18, 0, 0);
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
    }
}