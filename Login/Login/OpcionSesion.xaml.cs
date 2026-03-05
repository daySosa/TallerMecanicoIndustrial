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
    /// <summary>
    /// Lógica de interacción para OpcionSesion.xaml
    /// </summary>
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
            InterfazIngresarCodigoV ventana = new InterfazIngresarCodigoV();
            ventana.Show();
            this.Close();
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
            InterfazIngresarCodigoV ventana = new InterfazIngresarCodigoV();
            ventana.Show();
            this.Close();
        }

        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
        {
            ReconocimientoFacial ventana = new ReconocimientoFacial();
            ventana.Show();
            this.Close();
=======
>>>>>>> Stashed changes
        }
    }
}