using System;
using System.Collections.Generic;
using System.IO;
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
    /// Lógica de interacción para Verificacion2FA.xaml
    /// </summary>
    public partial class Verificacion2FA : Window
    {
        private string userEmail; // Variable para almacenar el correo del usuario
        private clsAutenticacion autenticacion = new clsAutenticacion(); // Instancia de la clase de autenticación

        //Constructor para recibir el correo del usuario:
        public Verificacion2FA(string correo)
        {
            InitializeComponent();
            userEmail = correo;
        }

        //Botón para verificar el código ingresado por el usuario:
        private void BtnVerificar_Click(object sender, RoutedEventArgs e)
        {
            string codigoIngresado = txtCodigo.Text.Trim();

            if (string.IsNullOrWhiteSpace(codigoIngresado))
            {
                txtErrorCorreo.Text = "⚠ Ingresa el código.";
                txtErrorCorreo.Visibility = Visibility.Visible;
                return;
            }

            autenticacion.ValidarCodigo(userEmail, codigoIngresado);
        }

        //Botón para reenviar el código al correo del usuario:
        private void BtnReenviar_Click(object sender, RoutedEventArgs e)
        {
            string codigo = autenticacion.GenerarCodigo(userEmail);
            autenticacion.EnviarCorreo(userEmail, codigo);
            MessageBox.Show("✅ Código reenviado a tu correo.", "Código enviado",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }


    }
}
