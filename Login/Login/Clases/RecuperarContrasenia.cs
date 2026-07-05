using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Login.Clases
{
    /// <summary>
    /// Flujo de recuperación de contraseña (correo → OTP → nueva contraseña) mostrado
    /// como overlay sobre MainWindow. Incluye protecciones anti abuso: cooldown de
    /// reenvío de OTP, límite de intentos, validación robusta de contraseña e
    /// indicador de fortaleza en vivo.
    /// </summary>
    internal class RecuperarContrasenia(MainWindow mainWindow)
    {
        private const int SegundosCooldownReenvioOtp = 60;
        private const int MaxIntentosOtpCliente = 3;

        private static readonly TimeSpan DuracionHover = TimeSpan.FromMilliseconds(140);

        // Cooldown de reenvío compartido entre todas las instancias del flujo,
        // para que no se pueda "resetear" el límite abriendo el overlay de nuevo.
        private static readonly ConcurrentDictionary<string, DateTime> _ultimoEnvioOtp = new();

        private readonly RepositorioSql _repositorio = new();

        private string _correoRecuperacion = string.Empty;
        private int _intentosOtpFallidos = 0;
        private bool _flujoActivo = false;

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

        // Colores del indicador de fortaleza (precomputados: nada de parsear
        // strings de color en cada tecla que el usuario escribe).
        private static readonly SolidColorBrush BrushFortalezaDebil = BrushError;
        private static readonly SolidColorBrush BrushFortalezaRegular = Congelar("#FBBF24");
        private static readonly SolidColorBrush BrushFortalezaBuena = Congelar("#38BDF8");
        private static readonly SolidColorBrush BrushFortalezaFuerte = BrushExito;

        private static readonly DropShadowEffect SombraPanel = CrearSombra();

        // Geometrías del ícono de ojo (mostrar/ocultar contraseña) y del candado,
        // parseadas y congeladas UNA sola vez. Antes se llamaba Geometry.Parse()
        // en cada clic del botón "ver contraseña"; ahora solo se intercambia la
        // referencia, sin volver a parsear el path SVG.
        private static readonly Geometry GeometriaCandado = CongelarGeometria(
            "M12,17A2,2 0 0,0 14,15C14,13.89 13.1,13 12,13A2,2 0 0,0 10,15A2,2 0 0,0 12,17M18,8A2,2 0 0,1 20,10V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V10C4,8.89 4.9,8 6,8H7V6A5,5 0 0,1 12,1A5,5 0 0,1 17,6V8H18M12,3A3,3 0 0,0 9,6V8H15V6A3,3 0 0,0 12,3Z");

        private static readonly Geometry GeometriaOjoAbierto = CongelarGeometria(
            "M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5Z");

        private static readonly Geometry GeometriaOjoCerrado = CongelarGeometria(
            "M11.83,9L15,12.16C15,12.11 15,12.05 15,12A3,3 0 0,0 12,9C11.94,9 11.89,9 11.83,9M7.53,9.8L9.08,11.35C9.03,11.56 9,11.77 9,12A3,3 0 0,0 12,15C12.22,15 12.44,15 12.65,14.92L14.2,16.47C13.53,16.8 12.79,17 12,17A5,5 0 0,1 7,12C7,11.21 7.2,10.47 7.53,9.8M2,4.27L4.28,6.55L4.73,7C3.08,8.3 1.78,10 1,12C2.73,16.39 7,19.5 12,19.5C13.55,19.5 15.03,19.2 16.38,18.66L16.81,19.09L19.73,22L21,20.73L3.27,3M12,7A5,5 0 0,1 17,12C17,12.64 16.87,13.26 16.64,13.82L19.57,16.75C21.07,15.5 22.27,13.86 23,12C21.27,7.61 17,4.5 12,4.5C10.6,4.5 9.26,4.75 8,5.2L10.17,7.35C10.74,7.13 11.35,7 12,7Z");

        private static SolidColorBrush Congelar(string hex, double? opacidad = null)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            if (opacidad.HasValue) brush.Opacity = opacidad.Value;
            brush.Freeze();
            return brush;
        }

        private static Geometry CongelarGeometria(string datos)
        {
            var geometria = Geometry.Parse(datos);
            geometria.Freeze();
            return geometria;
        }

        private static DropShadowEffect CrearSombra()
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

        public void IniciarFlujo()
        {
            // Evita apilar overlays si el usuario hace doble clic muy rápido
            // en "¿Olvidó su contraseña?" antes de que el primero termine de aparecer.
            if (_flujoActivo || ExisteOverlayActivo()) return;

            LimpiarCooldownsExpirados();

            _flujoActivo = true;
            ConstruirOverlay();
            ConstruirPasoCorreo();
            FadeIn(_overlayGrid, 180);
        }

        private bool ExisteOverlayActivo()
        {
            return mainWindow.Content is Grid rootGrid &&
                   rootGrid.Children.OfType<Grid>().Any(g => g.Tag as string == "OverlayRecuperacion");
        }

        private void ConstruirOverlay()
        {
            _overlayGrid = new Grid
            {
                Background = BrushFondoOverlay,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Opacity = 0,
                Tag = "OverlayRecuperacion"
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

            if (mainWindow.Content is Grid rootGrid)
            {
                rootGrid.Children.Add(_overlayGrid);
            }
            else
            {
                var contenidoActual = mainWindow.Content as UIElement;
                var nuevoGrid = new Grid();
                mainWindow.Content = null;
                if (contenidoActual is not null)
                    nuevoGrid.Children.Add(contenidoActual);
                nuevoGrid.Children.Add(_overlayGrid);
                mainWindow.Content = nuevoGrid;
            }
        }

        private void CerrarOverlay()
        {
            LimpiarDatosSensibles();
            _flujoActivo = false;

            FadeOut(_overlayGrid, 140, () =>
            {
                if (mainWindow.Content is Grid rootGrid)
                    rootGrid.Children.Remove(_overlayGrid);
            });
        }

        /// <summary>
        /// Borra correo y contraseñas capturadas en memoria una vez que el flujo
        /// termina o se cancela, en vez de dejarlas retenidas en los controles de UI.
        /// </summary>
        private void LimpiarDatosSensibles()
        {
            _correoRecuperacion = string.Empty;
            _intentosOtpFallidos = 0;
        }

        /// <summary>
        /// Descarta del diccionario de cooldown los correos cuyo período de espera
        /// ya venció hace rato. Se llama de forma oportunista al abrir el flujo,
        /// en vez de mantener un Timer en segundo plano solo para esto. Evita que
        /// el diccionario crezca indefinidamente durante la vida de la aplicación.
        /// </summary>
        private static void LimpiarCooldownsExpirados()
        {
            DateTime limite = DateTime.UtcNow.AddSeconds(-SegundosCooldownReenvioOtp * 2);

            foreach (var entrada in _ultimoEnvioOtp)
            {
                if (entrada.Value < limite)
                    _ultimoEnvioOtp.TryRemove(entrada.Key, out _);
            }
        }

        private static void FadeIn(UIElement el, int ms, Action alTerminar = null)
        {
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(ms))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            if (alTerminar is not null) anim.Completed += (s, e) => alTerminar();
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private static void FadeOut(UIElement el, int ms, Action alTerminar = null)
        {
            var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(ms))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            if (alTerminar is not null) anim.Completed += (s, e) => alTerminar();
            el.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void TransicionarA(Action construirNuevoPaso)
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

        private static TextBlock CrearTitulo(string texto) => new()
        {
            Text = texto,
            Foreground = BrushTexto,
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 6),
            TextWrapping = TextWrapping.Wrap
        };

        private static TextBlock CrearMensaje(string texto) => new()
        {
            Text = texto,
            Foreground = BrushSubtexto,
            FontSize = 12.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };

        private static TextBlock CrearEtiquetaCampo(string texto) => new()
        {
            Text = texto,
            Foreground = BrushSubtexto,
            FontSize = 11.5,
            Margin = new Thickness(2, 0, 0, 6)
        };

        private static void ActualizarFocoContenedor(Border contenedor, bool enfocado)
        {
            contenedor.BorderBrush = enfocado ? BrushPrimarioHover : BrushCampoBorde;
            contenedor.BorderThickness = new Thickness(enfocado ? 1.5 : 1);
        }

        private static Border CrearContenedorCampo(out TextBox campo)
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

            var campoLocal = campo;
            campoLocal.GotFocus += (s, e) => ActualizarFocoContenedor(contenedor, true);
            campoLocal.LostFocus += (s, e) => ActualizarFocoContenedor(contenedor, false);

            return contenedor;
        }

        /// <summary>
        /// Crea un campo de contraseña enmascarado con botón para mostrar/ocultar.
        /// El parámetro opcional <paramref name="alCambiar"/> se invoca en cada
        /// cambio de texto (útil para el indicador de fortaleza en vivo).
        /// </summary>
        private static Border CrearContenedorCampoContrasena(
    out Func<string> obtenerValor,
    out Action limpiar,
    Action<string> alCambiar = null)
        {
            var passwordBox = new PasswordBox
            {
                Background = BrushTransparente,
                BorderThickness = new Thickness(0),
                Foreground = BrushTexto,
                FontSize = 13,
                Padding = new Thickness(40, 0, 40, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            var campoVisible = new TextBox
            {
                Background = BrushTransparente,
                BorderThickness = new Thickness(0),
                Foreground = BrushTexto,
                FontSize = 13,
                Padding = new Thickness(40, 0, 40, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                CaretBrush = BrushTexto,
                Visibility = Visibility.Collapsed
            };

            bool visible = false;

            string ObtenerValorLocal() => visible ? campoVisible.Text : passwordBox.Password;

            // Ícono de candado (izquierda) — geometría precomputada y congelada.
            var iconoCandado = new System.Windows.Shapes.Path
            {
                Data = GeometriaCandado,
                Fill = BrushPlaceholder,
                Stretch = Stretch.Uniform,
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0),
                IsHitTestVisible = false
            };

            // Ícono de ojo (derecha) — alterna entre dos geometrías precomputadas,
            // sin volver a parsear el path SVG en cada clic.
            var iconoOjo = new System.Windows.Shapes.Path
            {
                Data = GeometriaOjoAbierto,
                Fill = BrushPlaceholder,
                Stretch = Stretch.Uniform,
                Width = 16,
                Height = 16
            };

            var botonOjo = new Button
            {
                Content = iconoOjo,
                Width = 34,
                Height = 34,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Background = BrushTransparente,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Template = ObtenerPlantillaBoton()
            };

            botonOjo.Click += (s, e) =>
            {
                visible = !visible;
                if (visible)
                {
                    campoVisible.Text = passwordBox.Password;
                    passwordBox.Visibility = Visibility.Collapsed;
                    campoVisible.Visibility = Visibility.Visible;
                    campoVisible.Focus();
                    campoVisible.CaretIndex = campoVisible.Text.Length;
                    iconoOjo.Data = GeometriaOjoCerrado;
                }
                else
                {
                    passwordBox.Password = campoVisible.Text;
                    campoVisible.Visibility = Visibility.Collapsed;
                    passwordBox.Visibility = Visibility.Visible;
                    passwordBox.Focus();
                    iconoOjo.Data = GeometriaOjoAbierto;
                }
            };

            passwordBox.PasswordChanged += (s, e) => alCambiar?.Invoke(ObtenerValorLocal());
            campoVisible.TextChanged += (s, e) => alCambiar?.Invoke(ObtenerValorLocal());

            var grid = new Grid();
            grid.Children.Add(iconoCandado);
            grid.Children.Add(passwordBox);
            grid.Children.Add(campoVisible);
            grid.Children.Add(botonOjo);

            var contenedor = new Border
            {
                Height = 44,
                CornerRadius = new CornerRadius(10),
                Background = BrushCampoFondo,
                BorderBrush = BrushCampoBorde,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 16),
                Child = grid
            };

            passwordBox.GotFocus += (s, e) => ActualizarFocoContenedor(contenedor, true);
            passwordBox.LostFocus += (s, e) => ActualizarFocoContenedor(contenedor, false);
            campoVisible.GotFocus += (s, e) => ActualizarFocoContenedor(contenedor, true);
            campoVisible.LostFocus += (s, e) => ActualizarFocoContenedor(contenedor, false);

            obtenerValor = ObtenerValorLocal;
            limpiar = () =>
            {
                passwordBox.Password = string.Empty;
                campoVisible.Text = string.Empty;
            };

            return contenedor;
        }

        /// <summary>
        /// Crea un brush independiente (no congelado) a partir de un color base,
        /// preservando tanto el Color como el Opacity original, para poder
        /// animar el color en el hover sin afectar los brushes compartidos y
        /// congelados usados como referencia de paleta.
        ///
        /// IMPORTANTE: SolidColorBrush.Opacity es una propiedad aparte de
        /// Color (multiplica la transparencia general del brush). Si solo se
        /// copia el Color y no el Opacity, un brush semitransparente como
        /// BrushSecundario (#FFFFFF al 8%) se vuelve accidentalmente opaco
        /// al 100%, mostrando un botón blanco sólido en vez de sutil.
        /// </summary>
        private static SolidColorBrush CrearBrushAnimable(SolidColorBrush origen) =>
            new(origen.Color) { Opacity = origen.Opacity };

        private static void AnimarColorBoton(SolidColorBrush brush, Color destino) =>
            brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(destino, DuracionHover));

        private static Button CrearBoton(string texto, bool esPrimario, double width = 118, bool esExito = false)
        {
            var fondoNormal = esExito ? BrushExito : (esPrimario ? BrushPrimario : BrushSecundario);
            var fondoHover = esExito ? BrushExitoHover : (esPrimario ? BrushPrimarioHover : BrushCampoFondo);

            // Brush propio y animable por botón: si dos botones estuvieran en
            // hover a la vez, cada uno transiciona de forma independiente.
            var brushFondo = CrearBrushAnimable(fondoNormal);

            var boton = new Button
            {
                Content = texto,
                Width = width,
                Height = 38,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushTexto,
                Cursor = Cursors.Hand,
                Background = brushFondo,
                BorderThickness = new Thickness(0),
                Template = ObtenerPlantillaBoton()
            };

            // El destino de la animación debe usar el mismo Opacity que el
            // brush de referencia (fondoHover/fondoNormal), no solo su Color,
            // por la misma razón explicada en CrearBrushAnimable.
            boton.MouseEnter += (s, e) => { if (boton.IsEnabled) AnimarColorBoton(brushFondo, fondoHover.Color); };
            boton.MouseLeave += (s, e) => { if (boton.IsEnabled) AnimarColorBoton(brushFondo, fondoNormal.Color); };

            // Ajuste fino: como ColorAnimation solo interpola el canal Color
            // (no el Opacity del Brush), y normal/hover en este diseño
            // comparten el mismo Opacity dentro de cada estado (0.08 y 0.07
            // respectivamente son casi iguales), no hace falta animar Opacity
            // por separado. Si en el futuro se usan colores con Opacity muy
            // distintos entre normal y hover, animar también BrushFondo.Opacity.

            return boton;
        }

        private static ControlTemplate _plantillaBotonCache;
        private static ControlTemplate ObtenerPlantillaBoton()
        {
            if (_plantillaBotonCache is not null) return _plantillaBotonCache;

            var plantilla = new ControlTemplate(typeof(Button));
            var borde = new FrameworkElementFactory(typeof(Border));
            borde.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            borde.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding(nameof(Button.Background))
            { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            borde.SetValue(Border.OpacityProperty, 1.0);

            var contenido = new FrameworkElementFactory(typeof(ContentPresenter));
            contenido.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contenido.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borde.AppendChild(contenido);

            plantilla.VisualTree = borde;

            var triggerDeshabilitado = new Trigger { Property = Button.IsEnabledProperty, Value = false };
            triggerDeshabilitado.Setters.Add(new Setter(Border.OpacityProperty, 0.55) { TargetName = null });
            plantilla.Triggers.Add(triggerDeshabilitado);

            plantilla.Seal();
            _plantillaBotonCache = plantilla;
            return plantilla;
        }

        private static TextBlock CrearMensajeError(string texto) => new()
        {
            Text = texto,
            Foreground = BrushError,
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(2, -10, 0, 12),
            Visibility = Visibility.Collapsed
        };

        private static void MostrarErrorLocal(TextBlock lbl, string mensaje)
        {
            lbl.Text = mensaje;
            lbl.Visibility = Visibility.Visible;
        }

        private static void OcultarErrorLocal(TextBlock lbl) => lbl.Visibility = Visibility.Collapsed;

        /// <summary>
        /// Restringe el TextBox del código OTP para que solo acepte dígitos,
        /// en vez de validar recién al enviar.
        /// </summary>
        private static void RestringirSoloDigitos(TextBox campo)
        {
            campo.PreviewTextInput += (s, e) => e.Handled = !e.Text.All(char.IsDigit);
            DataObject.AddPastingHandler(campo, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(DataFormats.Text) &&
                    e.DataObject.GetData(DataFormats.Text) is string texto &&
                    !texto.All(char.IsDigit))
                {
                    e.CancelCommand();
                }
            });
        }

        // ─────────────────────────────────────────────────────────────
        // PASO 1: CORREO
        // ─────────────────────────────────────────────────────────────

        private void ConstruirPasoCorreo()
        {
            _contenidoPanel.Children.Clear();

            var contenedorCorreo = CrearContenedorCampo(out var txtCorreo);
            txtCorreo.MaxLength = 100;
            var lblError = CrearMensajeError("");

            var btnCancelar = CrearBoton("Cancelar", esPrimario: false, width: 100);
            var btnSiguiente = CrearBoton("Siguiente", esPrimario: true, width: 112);

            btnCancelar.Click += (s, e) => CerrarOverlay();

            btnSiguiente.Click += async (s, e) =>
            {
                if (!btnSiguiente.IsEnabled) return; // guard anti doble-clic

                string correo = txtCorreo.Text.Trim().ToLowerInvariant();
                OcultarErrorLocal(lblError);

                if (!ValidacionesGenerales.EsRequerido(correo))
                {
                    MostrarErrorLocal(lblError, "⚠ Ingresa tu correo electrónico.");
                    return;
                }

                if (!ValidacionesGenerales.EsCorreoValido(correo))
                {
                    MostrarErrorLocal(lblError, "⚠ El correo no tiene un formato válido.");
                    return;
                }

                if (EstaEnCooldownDeReenvio(correo, out int segundosRestantes))
                {
                    MostrarErrorLocal(lblError,
                        $"⚠ Ya enviamos un código a este correo. Espera {segundosRestantes}s para reintentar.");
                    return;
                }

                btnSiguiente.IsEnabled = false;
                btnSiguiente.Content = "Verificando...";

                try
                {
                    bool existe = await Task.Run(() => _repositorio.ExisteCorreoLogin(correo));
                    if (!existe)
                    {
                        MostrarErrorLocal(lblError, "⚠ No encontramos una cuenta con ese correo.");
                        return;
                    }

                    bool enviado = await Task.Run(() =>
                    {
                        string codigo = _repositorio.GenerarCodigoOTP(correo);
                        return _repositorio.EnviarCorreoOTP(correo, codigo);
                    });

                    if (!enviado)
                    {
                        MostrarErrorLocal(lblError, "⚠ No se pudo enviar el correo. Intenta nuevamente.");
                        return;
                    }

                    RegistrarEnvioOtp(correo);
                    _correoRecuperacion = correo;
                    _intentosOtpFallidos = 0;
                    TransicionarA(ConstruirPasoOTP);
                }
                catch (Exception ex)
                {
                    MostrarErrorLocal(lblError, "⚠ " + ex.Message);
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

        private static bool EstaEnCooldownDeReenvio(string correo, out int segundosRestantes)
        {
            segundosRestantes = 0;
            if (!_ultimoEnvioOtp.TryGetValue(correo, out DateTime ultimoEnvio)) return false;

            double segundosTranscurridos = (DateTime.UtcNow - ultimoEnvio).TotalSeconds;
            if (segundosTranscurridos >= SegundosCooldownReenvioOtp) return false;

            segundosRestantes = (int)Math.Ceiling(SegundosCooldownReenvioOtp - segundosTranscurridos);
            return true;
        }

        private static void RegistrarEnvioOtp(string correo) =>
            _ultimoEnvioOtp[correo] = DateTime.UtcNow;

        // ─────────────────────────────────────────────────────────────
        // PASO 2: CÓDIGO OTP
        // ─────────────────────────────────────────────────────────────

        private void ConstruirPasoOTP()
        {
            _contenidoPanel.Children.Clear();

            var contenedorOTP = CrearContenedorCampo(out var txtOTP);
            txtOTP.MaxLength = 6;
            RestringirSoloDigitos(txtOTP);
            var lblError = CrearMensajeError("");

            var btnAtras = CrearBoton("← Atrás", esPrimario: false, width: 100);
            var btnVerificar = CrearBoton("Verificar", esPrimario: true, width: 112);
            var btnReenviar = CrearBoton("Reenviar código", esPrimario: false, width: 220);
            btnReenviar.FontSize = 11.5;
            btnReenviar.HorizontalAlignment = HorizontalAlignment.Center;
            btnReenviar.Margin = new Thickness(0, 4, 0, 0);

            btnAtras.Click += (s, e) => TransicionarA(ConstruirPasoCorreo);

            btnReenviar.Click += async (s, e) =>
            {
                if (!btnReenviar.IsEnabled) return;

                OcultarErrorLocal(lblError);

                if (EstaEnCooldownDeReenvio(_correoRecuperacion, out int segundosRestantes))
                {
                    MostrarErrorLocal(lblError, $"⚠ Espera {segundosRestantes}s antes de pedir otro código.");
                    return;
                }

                btnReenviar.IsEnabled = false;
                btnReenviar.Content = "Enviando...";

                try
                {
                    bool enviado = await Task.Run(() =>
                    {
                        string codigo = _repositorio.GenerarCodigoOTP(_correoRecuperacion);
                        return _repositorio.EnviarCorreoOTP(_correoRecuperacion, codigo);
                    });

                    if (enviado)
                    {
                        RegistrarEnvioOtp(_correoRecuperacion);
                        _intentosOtpFallidos = 0;
                        txtOTP.Clear();
                    }
                    else
                    {
                        MostrarErrorLocal(lblError, "⚠ No se pudo reenviar el código. Intenta nuevamente.");
                    }
                }
                catch (Exception ex)
                {
                    MostrarErrorLocal(lblError, "⚠ " + ex.Message);
                }
                finally
                {
                    btnReenviar.IsEnabled = true;
                    btnReenviar.Content = "Reenviar código";
                }
            };

            btnVerificar.Click += async (s, e) =>
            {
                if (!btnVerificar.IsEnabled) return;

                string otp = txtOTP.Text.Trim();
                OcultarErrorLocal(lblError);

                if (_intentosOtpFallidos >= MaxIntentosOtpCliente)
                {
                    MostrarErrorLocal(lblError, "⚠ Demasiados intentos fallidos. Solicita un nuevo código.");
                    return;
                }

                if (!ValidacionesGenerales.EsRequerido(otp))
                {
                    MostrarErrorLocal(lblError, "⚠ Ingresa el código de verificación.");
                    return;
                }

                if (!ValidacionesGenerales.TieneLongitudExacta(otp, 6) || !ValidacionesGenerales.EsSoloNumeros(otp))
                {
                    MostrarErrorLocal(lblError, "⚠ El código debe tener 6 dígitos numéricos.");
                    return;
                }

                btnVerificar.IsEnabled = false;
                btnVerificar.Content = "Verificando...";

                try
                {
                    bool valido = await Task.Run(() => _repositorio.ValidarCodigoOTP(_correoRecuperacion, otp));
                    if (!valido)
                    {
                        _intentosOtpFallidos++;
                        int restantes = MaxIntentosOtpCliente - _intentosOtpFallidos;
                        MostrarErrorLocal(lblError, restantes > 0
                            ? $"⚠ Código incorrecto. Te quedan {restantes} intento(s)."
                            : "⚠ Código incorrecto. Solicita un nuevo código para continuar.");
                        return;
                    }

                    _intentosOtpFallidos = 0;
                    TransicionarA(ConstruirPasoNuevaContrasena);
                }
                catch (Exception ex)
                {
                    MostrarErrorLocal(lblError, "⚠ " + ex.Message);
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
            _contenidoPanel.Children.Add(btnReenviar);

            txtOTP.Focus();
        }

        // ─────────────────────────────────────────────────────────────
        // PASO 3: NUEVA CONTRASEÑA
        // ─────────────────────────────────────────────────────────────

        private void ConstruirPasoNuevaContrasena()
        {
            _contenidoPanel.Children.Clear();

            // Indicador de fortaleza: 4 barras + etiqueta descriptiva, se actualiza
            // en vivo mientras el usuario escribe (sin crear brushes en cada tecla).
            var indicadorFortaleza = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 4)
            };

            List<Border> segmentos = [];
            for (int i = 0; i < 4; i++)
            {
                var segmento = new Border
                {
                    Width = 44,
                    Height = 4,
                    CornerRadius = new CornerRadius(2),
                    Background = BrushCampoBorde,
                    Margin = new Thickness(0, 0, 4, 0)
                };
                segmentos.Add(segmento);
                indicadorFortaleza.Children.Add(segmento);
            }

            var lblFortaleza = new TextBlock
            {
                Foreground = BrushSubtexto,
                FontSize = 10.5,
                Margin = new Thickness(0, 4, 0, 14),
                Text = "Escribe una contraseña"
            };

            void ActualizarFortaleza(string valor)
            {
                int nivel = ValidadorRecuperarContrasenia.CalcularFortaleza(valor);
                var colorNivel = nivel switch
                {
                    <= 1 => BrushFortalezaDebil,
                    2 => BrushFortalezaRegular,
                    3 => BrushFortalezaBuena,
                    _ => BrushFortalezaFuerte
                };

                for (int i = 0; i < segmentos.Count; i++)
                    segmentos[i].Background = i < nivel ? colorNivel : BrushCampoBorde;

                if (string.IsNullOrEmpty(valor))
                {
                    lblFortaleza.Text = "Escribe una contraseña";
                    lblFortaleza.Foreground = BrushSubtexto;
                }
                else
                {
                    lblFortaleza.Text = nivel switch
                    {
                        <= 1 => "Débil",
                        2 => "Regular",
                        3 => "Buena",
                        _ => "Fuerte"
                    };
                    lblFortaleza.Foreground = colorNivel;
                }
            }

            var contenedorNueva = CrearContenedorCampoContrasena(out var obtenerNueva, out var limpiarNueva, ActualizarFortaleza);
            var contenedorConfirmar = CrearContenedorCampoContrasena(out var obtenerConfirmar, out var limpiarConfirmar);
            var lblError = CrearMensajeError("");

            var btnAtras = CrearBoton("← Atrás", esPrimario: false, width: 100);
            var btnGuardar = CrearBoton("Guardar", esPrimario: false, width: 112, esExito: true);

            btnAtras.Click += (s, e) =>
            {
                limpiarNueva();
                limpiarConfirmar();
                TransicionarA(ConstruirPasoOTP);
            };

            btnGuardar.Click += async (s, e) =>
            {
                if (!btnGuardar.IsEnabled) return;

                string nueva = obtenerNueva();
                string confirmar = obtenerConfirmar();
                OcultarErrorLocal(lblError);

                string errorContrasena = ValidadorRecuperarContrasenia.Validar(nueva, _correoRecuperacion);
                if (errorContrasena is not null)
                {
                    MostrarErrorLocal(lblError, errorContrasena);
                    return;
                }

                if (!ValidacionesGenerales.EsRequerido(confirmar))
                {
                    MostrarErrorLocal(lblError, "⚠ Confirma tu nueva contraseña.");
                    return;
                }

                if (nueva != confirmar)
                {
                    MostrarErrorLocal(lblError, "⚠ Las contraseñas no coinciden.");
                    return;
                }

                btnGuardar.IsEnabled = false;
                btnGuardar.Content = "Validando...";

                try
                {
                    // La nueva contraseña no puede ser igual a la actual: si "loguea"
                    // con la contraseña nueva, es porque es idéntica a la guardada.
                    bool esIgualALaActual = await Task.Run(
                        () => _repositorio.ValidarLogin(_correoRecuperacion, nueva));

                    if (esIgualALaActual)
                    {
                        MostrarErrorLocal(lblError, "⚠ La nueva contraseña no puede ser igual a la actual.");
                        return;
                    }

                    btnGuardar.Content = "Guardando...";

                    bool actualizado = await Task.Run(
                        () => _repositorio.ActualizarContrasenaLogin(_correoRecuperacion, nueva));

                    if (!actualizado)
                    {
                        MostrarErrorLocal(lblError, "⚠ Error al guardar. Intenta nuevamente.");
                        return;
                    }

                    limpiarNueva();
                    limpiarConfirmar();
                    TransicionarA(ConstruirExito);
                }
                catch (Exception ex)
                {
                    MostrarErrorLocal(lblError, "⚠ " + ex.Message);
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
            _contenidoPanel.Children.Add(CrearEtiquetaCampo($"Nueva contraseña (mín. {ValidadorRecuperarContrasenia.LongitudMinima} caracteres)"));
            _contenidoPanel.Children.Add(contenedorNueva);
            _contenidoPanel.Children.Add(indicadorFortaleza);
            _contenidoPanel.Children.Add(lblFortaleza);
            _contenidoPanel.Children.Add(CrearEtiquetaCampo("Confirmar contraseña"));
            _contenidoPanel.Children.Add(contenedorConfirmar);
            _contenidoPanel.Children.Add(lblError);
            _contenidoPanel.Children.Add(btnRow);
        }

        // ─────────────────────────────────────────────────────────────
        // PASO 4: ÉXITO
        // ─────────────────────────────────────────────────────────────

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
            btnCerrar.Click += (s, e) => CerrarOverlay();

            _contenidoPanel.Children.Add(icono);
            _contenidoPanel.Children.Add(CrearTitulo("¡Contraseña actualizada!"));
            _contenidoPanel.Children.Add(CrearMensaje("Ya puedes iniciar sesión con tu nueva contraseña."));
            _contenidoPanel.Children.Add(btnCerrar);
        }
    }
}