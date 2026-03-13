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
                this.DragMove();
        }

        private void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
        {
            Verificacion2FA ventana = new Verificacion2FA(_correoUsuario);
            ventana.Show();
            this.Close();
        }

        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
        {
            ReconocimientoFacial ventana = new ReconocimientoFacial();
            ventana.Show();
            this.Close();
        }
    }
}