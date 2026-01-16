using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using NavigationSection.Images;
using NavigationSection.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WinUIEx;
using NavigationSection.Model;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NavigationSection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CategoriasPrendas : Page
    {
        //Encapsula un metodo con un parametro string
        public event Action<string>? CambiarTitulo;

        //Tiene de argumento la funcion que cambiara el titulo
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Action<string> callback)
            {
                CambiarTitulo += callback;
            }
        }


        public CategoriasPrendas()
        {
            InitializeComponent();
            //El Inicio cuando se monta CategoriasPage y tiene de idCategoria -1 como referencia
            BreadcrumbBar2.ItemsSource = new ObservableCollection<Folder>{
                new Folder { Name = "Inicio", IdCategoria=-1},
                };
            BreadcrumbBar2.ItemClicked += BreadcrumbBar2_ItemClicked;
            //Al final buscar las caategorias
            this.SetCategorias();

        }

        private void MostrarPrendasCategoria(List<Prenda> prendas)
        {
            if (prendas.Any())
            {
                ListaPrendas.ItemsSource = prendas;
                ListaPrendas.Visibility = Visibility.Visible;
                TextSinPrendas.Visibility = Visibility.Collapsed;
            }
            else
            {
                ListaPrendas.ItemsSource = null;
                ListaPrendas.Visibility = Visibility.Collapsed;
                TextSinPrendas.Visibility = Visibility.Visible;
            }
        }

        private List<Prenda> ObtenerPrendas(int? idCategoria)
        {
            string sql = @"
                        SELECT id_prenda,
                               nombre,
                               imageName,
                               descripcion,
                               genero
                        FROM prendas
                        WHERE id_categoria = @idCategoria";

            var parametros = new MySqlParameter[]
            {
                new MySqlParameter("@idCategoria",
                idCategoria.HasValue ? idCategoria.Value : DBNull.Value)
            };

            return DataBase.DataBase.DoQuery(sql,
                reader => new Prenda
                {
                    IdPrenda = reader.GetInt32("id_prenda"),
                    Nombre = reader.GetString("nombre"),
                    Descripcion = reader.GetString("descripcion"),
                    ImageFullPath = ResolveImagePath(
                        reader.IsDBNull(reader.GetOrdinal("imageName"))
                            ? null
                            : reader.GetString("imageName"),
                        true
                    ),
                    Genero = reader.GetString("genero")
                },
                parametros
            );
        }


        //La primera vez, para el rango mas alto de las categorias que no tiene disfraces
        private void SetCategorias()
        {
            var categorias = ObtenerCategorias(null);
            MostrarCategorias(categorias);
        }


        private void MostrarCategorias(List<Categoria> categorias)
        {
            if (categorias.Any())
            {
                Debug.WriteLine("Si hay categorias");
                StyledGrid.ItemsSource = categorias;
                StyledGrid.Visibility = Visibility.Visible;

                TextSinCategorias.Visibility = Visibility.Collapsed;
            }
            else
            {
                Debug.WriteLine("No hay categorias");
                StyledGrid.ItemsSource = null;
                StyledGrid.Visibility = Visibility.Collapsed;

                TextSinCategorias.Visibility = Visibility.Visible;
            }
        }


        // Obtener categorías (base o hijas)
        public List<Categoria> ObtenerCategorias(int? parentId)
        {
            string sql;

            if (parentId == null)
            {
                sql = @"SELECT id_categoria_prenda, nombre, parent_id, image_path
                        FROM categoria_prendas
                        WHERE parent_id IS NULL";
            }
            else
            {
                sql = @"SELECT id_categoria_prenda, nombre, parent_id, image_path
                        FROM categoria_prendas
                        WHERE parent_id = " + parentId;
            }

            return DataBase.DataBase.DoQuery(sql,
                reader => {
                    string? imageFullName = reader.IsDBNull(reader.GetOrdinal("image_path"))
                        ? null
                        : reader.GetString("image_path");

                    return new Categoria
                    {
                        IdCategoria = reader.GetInt32("id_categoria_prenda"),
                        Nombre = reader.GetString("nombre"),
                        ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id"))
                                    ? null
                                    : reader.GetInt32("parent_id"),
                        ImagePath = ResolveImagePath(imageFullName, false),
                        esDifraz = false
                    };
                });
        }

        //ex. Nacional/Sierra/Ejemplo.jpg
        private string? ResolveImagePath(string? imageFullName,bool esDisfraz)
        {
            if (string.IsNullOrWhiteSpace(imageFullName))
                return null;

            var items = BreadcrumbBar2.ItemsSource as ObservableCollection<Folder>;

            // Tomar los nombres del breadcrumb
            var parts = items
                .ToArray()
                .Skip(1) //No toma en cuenta el primero que es "Inicio"
                .Select(i => i.Name)
                .ToArray();

            // Combinar baseDir + partes + dbPath
            var segments = new List<string>();
            segments.AddRange(parts);
            if (esDisfraz)
            {
                segments.Add("prendas");
            }
            segments.Add(imageFullName);

            string combined = Path.Combine(segments.ToArray());
            Debug.WriteLine("La ruta relativa: " + combined);
            return combined;
        }

        private void Click_Categoria(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not Categoria categoria)
                return;
            //Notificar cambio de titulo
            CambiarTitulo?.Invoke(categoria.Nombre);
            //Agregar en el Breadcrumber
            var items = BreadcrumbBar2.ItemsSource as ObservableCollection<Folder>;
            items.Add(new Folder { Name = categoria.Nombre, IdCategoria = categoria.IdCategoria });
            // Cargar subcategorias al final
            var subcategorias = ObtenerCategorias(categoria.IdCategoria);
            MostrarCategorias(subcategorias);
            var disfraces = ObtenerPrendas(categoria.IdCategoria);
            MostrarPrendasCategoria(disfraces);
        }


        private void BreadcrumbBar2_ItemClicked(
            BreadcrumbBar sender,
            BreadcrumbBarItemClickedEventArgs args)
        {
            var items = BreadcrumbBar2.ItemsSource as ObservableCollection<Folder>;

            // Limpiar breadcrumb hasta el nivel clickeado
            for (int i = items.Count - 1; i >= args.Index + 1; i--)
            {
                items.RemoveAt(i);
            }

            var folderSeleccionado = items[args.Index];

            // 🔁 Manejo correcto del nivel raíz
            int? idCategoriaPadre =
                folderSeleccionado.IdCategoria == -1
                    ? null
                    : folderSeleccionado.IdCategoria;

            var subcategorias = ObtenerCategorias(idCategoriaPadre);
            MostrarCategorias(subcategorias);
            var disfraces = ObtenerPrendas(idCategoriaPadre);
            MostrarPrendasCategoria(disfraces);  

            CambiarTitulo?.Invoke(folderSeleccionado.Name ?? string.Empty);
        }


        /* PARA RETROCEDER:
            if (Frame.CanGoBack)
                Frame.GoBack();
        */
        private void Click_NuevaCategoria(object sender, RoutedEventArgs e)
        {
            CambiarTitulo?.Invoke("Nueva Categoria");
            //Cambiar el frame actual, puedo pasar parametros que obtendre con override OnNavigate
            Frame.Navigate(typeof(NuevaCategoriaPrendas));
        }

        private void Click_NuevaPrenda(object sender, RoutedEventArgs e)
        {
            CambiarTitulo?.Invoke("Nuevo prenda");
            Frame.Navigate(typeof(NuevaPrendas));
        }

        private void ListaPrenda_Click(object sender, ItemClickEventArgs e)
        {
            //Que hace ValidationResult
            var prendaInfo = (Prenda)e.ClickedItem;
            if (prendaInfo == null)
            {
                return;
            }
            var window = new VerPrendaWindow(prendaInfo);
            window.SetWindowSize(600, 650);
            window.Activate();
        }

    }
}
