#nullable enable
using System.ComponentModel;

namespace Login.Clases
{
    /// <summary>
    /// Representa un producto del inventario del taller, con notificación de cambios
    /// para enlace a datos (WPF Binding) y validación de reglas de negocio integrada.
    /// </summary>
    public sealed class ValidadorInventario : INotifyPropertyChanged
    {
        #region Constantes de validación

        private const int CantidadMinimaPermitida = 0;
        private const decimal PrecioMinimoPermitido = 0.01m;
        private const int LongitudMaximaNombre = 100;
        private const int LongitudMaximaCategoria = 50;

        #endregion

        #region Campos privados

        private int _producto_Cantidad_Actual;
        private int _producto_Cantidad_Minima;
        private decimal _producto_Precio;
        private string? _producto_Nombre;
        private string? _producto_Categoria;

        #endregion

        #region Propiedades simples (sin notificación necesaria)

        public int Producto_ID { get; set; }
        public string? Producto_Marca { get; set; }
        public string? Producto_Modelo { get; set; }

        #endregion

        #region Propiedades con notificación

        public string? Producto_Nombre
        {
            get => _producto_Nombre;
            set
            {
                if (_producto_Nombre == value) return;
                _producto_Nombre = value;
                OnPropertyChanged(nameof(Producto_Nombre));
            }
        }

        public string? Producto_Categoria
        {
            get => _producto_Categoria;
            set
            {
                if (_producto_Categoria == value) return;
                _producto_Categoria = value;
                OnPropertyChanged(nameof(Producto_Categoria));
            }
        }

        public decimal Producto_Precio
        {
            get => _producto_Precio;
            set
            {
                if (_producto_Precio == value) return;
                _producto_Precio = value;
                OnPropertyChanged(nameof(Producto_Precio));
            }
        }

        public int Producto_Cantidad_Actual
        {
            get => _producto_Cantidad_Actual;
            set
            {
                if (_producto_Cantidad_Actual == value) return;
                _producto_Cantidad_Actual = value;
                OnPropertyChanged(nameof(Producto_Cantidad_Actual));
                OnPropertyChanged(nameof(StockBajo));
            }
        }

        public int Producto_Cantidad_Minima
        {
            get => _producto_Cantidad_Minima;
            set
            {
                if (_producto_Cantidad_Minima == value) return;
                _producto_Cantidad_Minima = value;
                OnPropertyChanged(nameof(Producto_Cantidad_Minima));
                OnPropertyChanged(nameof(StockBajo));
            }
        }

        /// <summary>
        /// Indica si la cantidad actual está por debajo de la cantidad mínima definida.
        /// Se recalcula automáticamente al cambiar cualquiera de las dos cantidades.
        /// </summary>
        public bool StockBajo => _producto_Cantidad_Actual < _producto_Cantidad_Minima;

        #endregion

        #region Validación

        /// <summary>
        /// Valida todos los campos del producto y retorna la lista de errores encontrados.
        /// Retorna una lista vacía si el producto es válido.
        /// </summary>
        public List<string> Validar()
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(Producto_Nombre))
                errores.Add("El nombre del producto es obligatorio.");
            else if (Producto_Nombre.Length > LongitudMaximaNombre)
                errores.Add($"El nombre del producto no puede superar los {LongitudMaximaNombre} caracteres.");

            if (string.IsNullOrWhiteSpace(Producto_Categoria))
                errores.Add("La categoría del producto es obligatoria.");
            else if (Producto_Categoria.Length > LongitudMaximaCategoria)
                errores.Add($"La categoría no puede superar los {LongitudMaximaCategoria} caracteres.");

            if (Producto_Precio < PrecioMinimoPermitido)
                errores.Add($"El precio debe ser mayor o igual a {PrecioMinimoPermitido:C}.");

            if (Producto_Cantidad_Actual < CantidadMinimaPermitida)
                errores.Add("La cantidad actual no puede ser negativa.");

            if (Producto_Cantidad_Minima < CantidadMinimaPermitida)
                errores.Add("La cantidad mínima no puede ser negativa.");

            return errores;
        }

        /// <summary>
        /// Indica si el producto cumple con todas las reglas de validación.
        /// </summary>
        public bool EsValido() => Validar().Count == 0;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string nombrePropiedad) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));

        #endregion
    }
}