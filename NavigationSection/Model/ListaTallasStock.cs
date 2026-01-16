using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavigationSection.Model
{
    public class ListaTallasStock
    {
        public ObservableCollection<TallaStock> Tallas { get; set; }
            = new();

        public ListaTallasStock()
        {
            //Ejemplo de tallas
            Tallas.Add(new TallaStock { Talla = 6, Stock = 10 });
            Tallas.Add(new TallaStock { Talla = 7, Stock = 5 });
        }
    }
}
