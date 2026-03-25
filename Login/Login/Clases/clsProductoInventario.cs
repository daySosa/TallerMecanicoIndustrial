using System;
using System.Collections.Generic;
using System.Text;

namespace Login.Clases
{
    public class clsProductoInventario
    {
        public int Producto_ID { get; set; }
        public string Producto_Nombre { get; set; }
        public string Producto_Categoria { get; set; }
        public int Producto_Cantidad_Actual { get; set; }
        public decimal Producto_Precio { get; set; }
    }
}
