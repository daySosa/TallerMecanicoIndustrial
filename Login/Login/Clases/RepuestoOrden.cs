using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Login.Clases
{
    public class RepuestoOrden : INotifyPropertyChanged
    {
        public int Numero { get; set; }
        public int ProductoID { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public bool Incluido { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
