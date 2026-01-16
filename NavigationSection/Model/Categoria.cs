using Microsoft.UI.Xaml.Media.Imaging;
using NavigationSection.Images;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavigationSection.Model
{
    //Modelo en BD de Categoria
    public class Categoria
    {
        public required int IdCategoria { get; set; }
        public required string Nombre { get; set; }
        public int? ParentId { get; set; }
        public required string? ImagePath { get; set; }
        public bool esDifraz { get; set; }

        //cada vez que accedo
        public BitmapImage? Imagen
        {
            get
            {
                if (string.IsNullOrEmpty(ImagePath))
                {
                    return null;
                }
                //CATEGORIA DE DISFRACES
                if (esDifraz)
                {
                    return ImageResolver.ToBitMap(ImagePath, "categorias");
                }
                //CATEGORIA DE PRENDAS
                return ImageResolver.ToBitMap(ImagePath,"prendas");
            }
        }

        public List<Categoria> Hijos { get; set; } = new();
    }
}
