using System.Windows;
using System.Windows.Input;

namespace Login
{
    public partial class OpcionSesion : Window
    {
        private readonly string _correoUsuario;
        private ReconocimientoFacial? _ventanaReconocimiento;

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

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
            => this.Close();

        private void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
        {
            new Verificacion2FA(_correoUsuario).Show();
            this.Close();
        }

        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
        {
            _ventanaReconocimiento ??= new ReconocimientoFacial(_correoUsuario);
            _ventanaReconocimiento.Show();
            this.Close();
        }
    }
}