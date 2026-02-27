using System.Collections;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Login
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (txtUsuario.Text == null || txtContra.Text == null) 
                MessageBox.Show("Necesita llenar ambos campos", "Error");
            else
            {
                string usuario = txtUsuario.Text.Trim();
                string password = txtContra.Text.Trim(); 
                login(usuario, password);
            }
        }

        public void login(string usuario, string password)
        {
            try
            {
                clsConexion conexion = new clsConexion();
                conexion.Abrir();

                string consulta = "SELECT * FROM LOGIN WHERE Usuario_Email = @usuario AND Usuario_Contraseña = @password";

                SqlCommand cmd = new SqlCommand(consulta, conexion.SqlC);
               
                cmd.Parameters.AddWithValue("@usuario", usuario);
                cmd.Parameters.AddWithValue("@password", password);

                SqlDataReader rd = cmd.ExecuteReader();

                if (rd.Read())
                {
                    MessageBox.Show("Inicio de sesión exitoso", "Éxito");
                    /*MainWindow mainWindow = new MainWindow();
                    mainWindow.ShowDialog();*/

                }
                else
                {
                    MessageBox.Show("Usuario o contraseña incorrectos", "Error");
                }
                
                rd.Close();
                conexion.Cerrar();
            }
                catch (Exception ex)
            {
                MessageBox.Show("Error al conectar a la base de datos: " + ex.Message, "Error");
                
            }
        }

        private void txtUsuario_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}