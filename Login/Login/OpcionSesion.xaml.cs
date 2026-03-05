using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Login
{
<<<<<<< Updated upstream
=======
    /// <summary>
    /// Lógica de interacción para OpcionSesion.xaml
    /// </summary>
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
            // Verificacion2FA ventana = new Verificacion2FA(correoUsuario);
            //ventana.Show();
           // this.Close();
        }

        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
        {
            ReconocimientoFacial ventana = new ReconocimientoFacial();
            ventana.Show();
            this.Close();
<<<<<<< Updated upstream
        }

        private void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
        {
            Verificacion2FA ventana = new Verificacion2FA("correo@ejemplo.com");
=======
            InterfazIngresarCodigoV ventana = new InterfazIngresarCodigoV();
>>>>>>> Stashed changes
            ventana.Show();
            this.Close();
        }

        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
        {
            ReconocimientoFacial ventana = new ReconocimientoFacial();
            ventana.Show();
            this.Close();
<<<<<<< Updated upstream
=======
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes
        }
    }
}