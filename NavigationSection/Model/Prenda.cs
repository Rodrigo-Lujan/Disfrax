using Microsoft.UI.Xaml.Media.Imaging;
using NavigationSection.Images;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavigationSection.Model
{
    public class Prenda
    {
        public int IdPrenda { get; set; }
        public string? ImageFullPath { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }

        public string? Genero { get; set; }

        //Obtener el id de la categoria
        public int? IdCategoria { get; set; }

        //Si cambia el ImageFullPath, no cambia, no es reactivo, se necesita INotifyProperty
        public BitmapImage? Imagen
        {
            get
            {
                if (string.IsNullOrEmpty(ImageFullPath))
                {
                    return null;
                }
                return ImageResolver.ToBitMap(ImageFullPath,"prendas");
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

        public List<TallaStock> stockPorTalla;
    }
}
