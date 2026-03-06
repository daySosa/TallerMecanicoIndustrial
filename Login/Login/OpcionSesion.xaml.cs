using System.Windows;

namespace Login
{
    public partial class OpcionSesion : Window
    {
        private string correoUsuario;

        public OpcionSesion(string correo)
        {
            InitializeComponent();
            correoUsuario = correo;
        }

        private void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
        {
            Verificacion2FA ventana = new Verificacion2FA(correoUsuario);
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