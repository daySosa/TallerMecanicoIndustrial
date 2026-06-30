using System.Windows;
using System.Windows.Input;

namespace Login
{
    public partial class OpcionSesion : Window
    {
        private readonly string _correoUsuario;

        public OpcionSesion(string correo)
        {
            InitializeComponent();
            _correoUsuario = correo;
        }

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
        private void Navegar<T>(Func<T> crear) where T : Window
        {
            crear().Show();
            this.Close();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MainWindow());

        private void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Verificacion2FA(_correoUsuario));

        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new ReconocimientoFacial(_correoUsuario));
    }
}