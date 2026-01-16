using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using NavigationSection.Images;
using NavigationSection.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


//TASK PARA OBTENER CATEGORIAS_PRENDAS Y SEA STATIC PARA TODAS LAS PRENDAS DE CADA DISFRAZ
namespace NavigationSection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CategoriasPage : Page
    {
        //Encapsula un metodo con un parametro string
        public event Action<string>? CambiarTitulo;

        //Categoria de prendas
        public static ObservableCollection<ExplorerItem> CategoriasPrendas { get; set; }
        public static Dictionary<int, ExplorerItem> diccionario;

        //Tiene de argumento la funcion que cambiara el titulo
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Action<string> callback)
            {
                CambiarTitulo += callback;
            }
        }


        public CategoriasPage()
        {
            InitializeComponent();
            //El Inicio cuando se monta CategoriasPage y tiene de idCategoria -1 como referencia
            BreadcrumbBar2.ItemsSource = new ObservableCollection<Folder>{
                new Folder { Name = "Inicio", IdCategoria=-1},
                };
            BreadcrumbBar2.ItemClicked += BreadcrumbBar2_ItemClicked;
            Loaded += CategoriasPage_Loaded;
        }

        //Lanzar 2 hilos para mostrar el compoente y en paralelo la consulta de categorias
        private async void CategoriasPage_Loaded(object sender,RoutedEventArgs e)
        {
            Task<ObservableCollection<ExplorerItem>> categoriasTask = GetCategoriasPrendas();
            Task setCategoriasTask = SetCategorias();

            await Task.WhenAll(categoriasTask, setCategoriasTask);

            CategoriasPrendas = await categoriasTask;
        }

        private async Task<ObservableCollection<ExplorerItem>> GetCategoriasPrendas()
        {
            //Obtener las categorias
            string sql = @"SELECT id_categoria_prenda, nombre, parent_id FROM categoria_prendas";
            var categorias = DataBase.DataBase.DoQuery(sql,
                reader => new Categoria
                {
                    IdCategoria = reader.GetInt32("id_categoria_prenda"),
                    Nombre = reader.GetString("nombre"),
                    ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id"))
                                ? null
                                : reader.GetInt32("parent_id"),
                    ImagePath = null
                });

            // Diccionario: IdCategoria -> ExplorerItem
            diccionario = new Dictionary<int, ExplorerItem>();

            // Crear un ExplorerItem por cada categoría
            foreach (var cat in categorias)
            {
                diccionario[cat.IdCategoria] = new ExplorerItem
                {
                    IdCategoria = cat.IdCategoria,
                    ParentId = cat.ParentId,
                    Name = cat.Nombre,
                    Type = ExplorerItem.ExplorerItemType.Folder // una categoría puede tener hijos
                };
            }

            // Lista de raíces
            ObservableCollection<ExplorerItem> raices = new();

            // Armar la jerarquía
            foreach (var cat in categorias)
            {
                var itemActual = diccionario[cat.IdCategoria];

                if (cat.ParentId == null)
                {
                    // Es raíz
                    raices.Add(itemActual);
                }
                else if (diccionario.TryGetValue(cat.ParentId.Value, out var padre))
                {
                    // Agregar al padre
                    padre.Children.Add(itemActual);
                }
            }

            return raices;
        }

        public static string ConstruirPathPrenda(int idCategoria, string? imageFullName)
        {
            //Buscar el explorer item con el de la categoria
            diccionario.TryGetValue(idCategoria, out var item);
            List<string> partes = new();
            //Poner el nombre de la imagen
            partes.Add(imageFullName);
            partes.Add("prendas");
            while (item != null)
            {
                partes.Add(item.Name);

                if (item.ParentId.HasValue &&
                    diccionario.TryGetValue(item.ParentId.Value, out var parent))
                {
                    item = parent;
                }
                else
                {
                    break;
                }
            }
            // Invertir porque se armó desde el hijo al padre
            partes.Reverse();
            // Unir correctamente como ruta
            string relativePath = Path.Combine(partes.ToArray());
            Debug.WriteLine("Esta es la ruta de la prenda " + relativePath);
            return relativePath;
        }

        private void MostrarDisfracesCategoria(List<Disfraz> disfraces)
        {
            if (disfraces.Any())
            {
                ListaDisfraces.ItemsSource = disfraces;
                ListaDisfraces.Visibility = Visibility.Visible;
                TextSinDisfraces.Visibility = Visibility.Collapsed;
            }
            else
            {
                ListaDisfraces.ItemsSource = null;
                ListaDisfraces.Visibility = Visibility.Collapsed;
                TextSinDisfraces.Visibility = Visibility.Visible;
            }
        }

        private List<Disfraz> ObtenerDisfraces(int? idCategoria)
        {
            string sql = @"
                        SELECT id_disfraz,
                               nombre,
                               imageName,
                               descripcion,
                               genero,
                               precioAlquiler,
                               precioVenta
                        FROM disfraces
                        WHERE id_categoria = @idCategoria";

            var parametros = new MySqlParameter[]
            {
                new MySqlParameter("@idCategoria",
                idCategoria.HasValue ? idCategoria.Value : DBNull.Value)
            };

            return DataBase.DataBase.DoQuery(sql,
                reader => new Disfraz
                {
                    IdDisfraz = reader.GetInt32("id_disfraz"),
                    Nombre = reader.GetString("nombre"),
                    Descripcion = reader.GetString("descripcion"),
                    Genero = reader.GetString("genero"),
                    PrecioAlquiler = reader.GetDouble("precioAlquiler"),
                    PrecioVenta = reader.GetDouble("precioVenta"),
                    ImageFullPath = ResolveImagePath(
                        reader.IsDBNull(reader.GetOrdinal("imageName"))
                            ? null
                            : reader.GetString("imageName"),
                        true
                    )
                },
                parametros
            );
        }


        //La primera vez, para el rango mas alto de las categorias que no tiene disfraces
        private async Task SetCategorias()
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
                sql = @"SELECT id_categoria, nombre, parent_id, image_path
                        FROM categoria
                        WHERE parent_id IS NULL";
            }
            else
            {
                sql = @"SELECT id_categoria, nombre, parent_id, image_path
                        FROM categoria
                        WHERE parent_id = " + parentId;
            }

            return DataBase.DataBase.DoQuery(sql,
                reader => {
                    string? imageFullName = reader.IsDBNull(reader.GetOrdinal("image_path"))
                        ? null
                        : reader.GetString("image_path");

                    return new Categoria
                    {
                        IdCategoria = reader.GetInt32("id_categoria"),
                        Nombre = reader.GetString("nombre"),
                        ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id"))
                                    ? null
                                    : reader.GetInt32("parent_id"),
                        ImagePath = ResolveImagePath(imageFullName,false),
                        esDifraz = true
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
                segments.Add("disfraces");
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
            var disfraces = ObtenerDisfraces(categoria.IdCategoria);
            MostrarDisfracesCategoria(disfraces);
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
            var disfraces = ObtenerDisfraces(idCategoriaPadre);
            MostrarDisfracesCategoria(disfraces);  

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
            Frame.Navigate(typeof(NuevaCategoria));
        }

        private void Click_NuevoDisfraz(object sender, RoutedEventArgs e)
        {
            CambiarTitulo?.Invoke("Nuevo disfraz o accesorio");
            Frame.Navigate(typeof(NuevoDisfraz));
        }

        //Tengo en el contexto la imagen
        //loading asincrono cargando imagenes de las prendas asociadas

        private void ListaDisfraces_ItemClick(object sender, ItemClickEventArgs e)
        {
            var disfrazInfo = (Disfraz)e.ClickedItem;
            //Crear un ContentDialog
            if (disfrazInfo == null)
            {
                return;
            }
            var window = new VerDisfrazWindow(disfrazInfo);
            window.SetWindowSize(850, 600);
            window.Activate();
        }

    }
}
