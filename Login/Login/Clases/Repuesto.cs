using System.ComponentModel;

namespace Login.Clases
{
    public class Repuesto : INotifyPropertyChanged
    {
        private int _producto_Cantidad_Actual;

        public int Producto_ID { get; set; }
        public string? Producto_Nombre { get; set; }
        public string? Producto_Categoria { get; set; }
        public string? Producto_Marca { get; set; }
        public string? Producto_Modelo { get; set; }
        public int Producto_Cantidad_Minima { get; set; }
        public decimal Producto_Precio { get; set; }

        public int Producto_Cantidad_Actual
        {
            get => _producto_Cantidad_Actual;
            set
            {
                _producto_Cantidad_Actual = value;
                OnPropertyChanged(nameof(Producto_Cantidad_Actual));
                OnPropertyChanged(nameof(StockBajo));
            }
        }

        public bool StockBajo => Producto_Cantidad_Actual < Producto_Cantidad_Minima;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}