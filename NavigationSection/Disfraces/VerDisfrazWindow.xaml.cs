using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
using NavigationSection.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NavigationSection
{
    public sealed partial class VerDisfrazWindow : Window
    {
        private Dictionary<int, ExplorerItem> diccionario;
        public ObservableCollection<ExplorerItem> CategoriasPrendas { get; set; }
        public ObservableCollection<Prenda> PrendasDisfraz { get; set; }
        public Disfraz Disfraz { get; set; }
        //De parametro tengo el disfraz al mostrar esto 
        public VerDisfrazWindow(Disfraz disfraz)
        {
            InitializeComponent();
            Disfraz = disfraz;
            PrendasDisfraz = new ObservableCollection<Prenda>();
            CargarPrendasDisfraz();
            //mostrar el componente al final 
            CategoriasPrendas = GetCategoriasPrendas();
            Debug.WriteLine("Cargue las categorias al inicio");
        }

        private void CargarPrendasDisfraz()
        {
            PrendasDisfraz.Clear();

            var prendas = ObtenerPrendas();
            foreach (var prenda in prendas)
                PrendasDisfraz.Add(prenda);
        }


        //Obtener las prendas del disfraz
        private List<Prenda> ObtenerPrendas()
        {
            string sql = @"SELECT
	                        p.id_prenda as 'id_prenda',
                            p.id_categoria as 'categoria',
	                        p.nombre as 'nombre',
                            p.imageName as 'imageName'
                        FROM prendas p
                        INNER JOIN prenda_disfraz pd
	                        ON p.id_prenda = pd.id_prenda
                        WHERE pd.id_disfraz = @id_disfraz";
            var parametros = new MySqlParameter[]
            {
                new MySqlParameter("@id_disfraz",Disfraz.IdDisfraz)
            };
            //Definir un reader y con ello construir la ruta de cada prenda
            //arbol de categorias de las prendas 
            return DataBase.DataBase.DoQuery(sql,
                reader => new Prenda
                {
                    IdPrenda = reader.GetInt32("id_prenda"),
                    Nombre = reader.GetString("nombre"),
                    IdCategoria = reader.GetInt32("categoria"),
                    ImageFullPath = CategoriasPage.ConstruirPathPrenda(
                        reader.GetInt32("categoria"),
                        reader.IsDBNull(reader.GetOrdinal("imageName"))
                            ? null
                            : reader.GetString("imageName")
                    )
                },
                parametros
            );
        }

        private ObservableCollection<ExplorerItem> GetCategoriasPrendas()
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

        private void Click_AgregarPrenda(object sender, RoutedEventArgs e)
        {
            //Hacer busqueda de prenda
            if (CategoriasPrendas == null)
            {
                CategoriasPrendas = GetCategoriasPrendas();
                Debug.WriteLine("Cargue las categorias PORQUE DI CLICK EN AGREGAR TIENDA");
            }
            //Mostra la lista de prendas
            ListaPrendasBusqueda.ItemsSource = null;
            //Cambiar de pantalla
            VistaDetalle.Visibility = Visibility.Collapsed;
            VistaAgregarPrenda.Visibility = Visibility.Visible;
        }

        private List<Prenda> ObtenerPrendas(int idCategoria)
        {
            string sql = @"
                        SELECT id_prenda,
                               nombre,
                               imageName,
                               descripcion
                        FROM prendas
                        WHERE id_categoria = @idCategoria";

            var parametros = new MySqlParameter[]
            {
                new MySqlParameter("@idCategoria",
                idCategoria)
            };

            return DataBase.DataBase.DoQuery(sql,
                reader => new Prenda
                {
                    IdPrenda = reader.GetInt32("id_prenda"),
                    Nombre = reader.GetString("nombre"),
                    Descripcion = reader.GetString("descripcion"),
                    ImageFullPath = CategoriasPage.ConstruirPathPrenda(
                        idCategoria,
                        reader.IsDBNull(reader.GetOrdinal("imageName"))
                            ? null
                            : reader.GetString("imageName")
                    )
                },
                parametros
            );
        }

        private void Click_Regresar(object sender, RoutedEventArgs e)
        {
            VistaAgregarPrenda.Visibility = Visibility.Collapsed;
            VistaDetalle.Visibility = Visibility.Visible;
        }

        private void CategoriaPrenda_SelectionChanged(
            TreeView sender,
            TreeViewSelectionChangedEventArgs args)
        {
            Debug.WriteLine("Seleccione un elemento");
            if (args.AddedItems.Count == 0)
                return;

            if (args.AddedItems[0] is ExplorerItem item)
            {
                var prendas = ObtenerPrendas((int)item.IdCategoria);
                  ListaPrendasBusqueda.ItemsSource = prendas;
            }
        }

        //Simula agregar la prenda (en realidad hacer INSERT)
        private async void ListaPrendasBusqueda_ItemClick(
            object sender,
            ItemClickEventArgs e)
        {
            Debug.WriteLine("Le di click a una prenda");
            if (e.ClickedItem is Prenda prenda)
            {
                bool exito = await InsertarPrenda(prenda);

                if (exito)
                {
                    // 🔄 refrescar lista
                    CargarPrendasDisfraz();

                    // 🔙 volver a la vista anterior
                    VistaAgregarPrenda.Visibility = Visibility.Collapsed;
                    VistaDetalle.Visibility = Visibility.Visible;
                }
                else
                {
                    this.Close();
                }
            }
        }

        public async Task<bool> InsertarPrenda(Prenda prenda) 
        {
            string sql = @"
                insert into prenda_disfraz(id_disfraz,id_prenda)
                values(@id_disfraz,@id_prenda);
            ";
            var parametros = new MySqlParameter[]
            {
                new MySqlParameter("@id_disfraz",Disfraz.IdDisfraz),
                new MySqlParameter("@id_prenda",prenda.IdPrenda)
            };
            int idGenerado = DataBase.DataBase.Execute(sql, parametros);
            ContentDialog dialog;
            bool exito;
            if (idGenerado == 0)
            {
                dialog = new ContentDialog
                {
                    Title = "Error al Guardar",
                    Content = "No se pudo registrar la prenda correctamente",
                    CloseButtonText = "Cerrar",
                    XamlRoot = RootGrid.XamlRoot
                };
                exito = false;
            }
            else
            {
                //Crer un dialogo que muestre el resultado
                dialog = new ContentDialog
                {
                    Title = "Guardado",
                    Content = "Prenda registrada correctamente",
                    CloseButtonText = "OK",
                    XamlRoot = RootGrid.XamlRoot
                };
                exito = true;
            }
            //Mostrar un dialogo
            await dialog.ShowAsync();
            return exito;
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