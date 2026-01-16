using Microsoft.UI.Xaml.Media.Imaging;
using NavigationSection.Images;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavigationSection.Model
{
    public class Disfraz
    {
        public int IdDisfraz { get; set; }
        public string? ImageFullPath { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Genero { get; set; }
        public double PrecioAlquiler { get; set; }
        public double PrecioVenta { get; set; }

        public BitmapImage? Imagen
        {
            get
            {
                if (string.IsNullOrEmpty(ImageFullPath))
                {
                    return null;
                }
                return ImageResolver.ToBitMap(ImageFullPath, "categorias");
            }
        }

        //genero 
        // Texto legible
        public string GeneroTexto =>
            Genero switch
            {
                "A" => "Ambos",
                "H" => "Hombre",
                "M" => "Mujer",
                _ => "No definido"
            };

        // Badge color según género
        public string GeneroColor =>
            Genero switch
            {
                "A" => "#6B7280", // gris
                "H" => "#2563EB", // azul
                "M" => "#DB2777", // rosado
                _ => "#9CA3AF"
            };

        //Se compone de una lista de prendas
        public List<Prenda>? partesVestimenta;
    }
}
