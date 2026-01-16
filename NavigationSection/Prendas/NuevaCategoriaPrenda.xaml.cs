using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using NavigationSection.Images;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using NavigationSection.Model;

namespace NavigationSection
{
    public sealed partial class NuevaCategoriaPrendas : Page
    {
        public ObservableCollection<ExplorerItem> DataSource { get; set; }
        public string[] pathCategoriaPadreNav;
 
        public NuevaCategoriaPrendas()
        {
            this.InitializeComponent();
            DataSource = GetData();
            this.DataContext = this;
        }

        //Obtengo de parametros el string[] con la ruta (para futura implementacion
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is string[] ruta)
            {
                pathCategoriaPadreNav = ruta;
            }
        }

        private Dictionary<int, ExplorerItem> diccionario;

        // Poner las categorias en un árbol de ExplorerItem
        private ObservableCollection<ExplorerItem> GetData()
        {
            //Obtener las categorias
            string sql = @"SELECT id_categoria_prenda, nombre, parent_id FROM categoria_prendas";
            var categorias = DataBase.DataBase.DoQuery(sql,
                reader => new Categoria
                {
                    IdCategoria = reader.GetInt32("id_categoria_prenda"),
                    Nombre = reader.GetString("nombre"),
                    ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id"))
                                ? 0
                                : reader.GetInt32("parent_id"),
                    ImagePath = null,
                    esDifraz=false
                });

            // Diccionario: IdCategoria -> ExplorerItem
            diccionario = new Dictionary<int, ExplorerItem>();

            // Crear un ExplorerItem por cada categoría
            diccionario[0] = new ExplorerItem
            {
                IdCategoria = 0,
                ParentId = null,
                Name = "Inicio",
                Type = ExplorerItem.ExplorerItemType.Folder
            };
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
            raices.Add(diccionario[0]);
            foreach (var cat in categorias)
            {
                var itemActual = diccionario[cat.IdCategoria];
                diccionario.TryGetValue(cat.ParentId.Value, out var padre);
                padre.Children.Add(itemActual);
            }

            return raices;
        }

        private string _imagenSeleccionada;
        private StorageFile? _image;
        private async void BtnElegirImagen_Click(object sender, RoutedEventArgs e)
        {
            //la API de .NET para elejir del explorador de archivos
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");

            // Corregido: pasar la instancia de la ventana actual como argumento
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker,
                WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));

            //elejir un solo archivo
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _imagenSeleccionada = file.Path;
                _image = file; //Incluye la extension o tipo de archivo
                PreviewImage.Source = new BitmapImage(new Uri(file.Path));
            }
        }

        //Cuando suelto imagen
        private async void Image_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0 && items[0] is StorageFile file)
                {
                    _imagenSeleccionada = file.Path;
                    _image = file; //Incluye la extension o tipo de archivo
                    PreviewImage.Source = new BitmapImage(new Uri(file.Path));
                }
            }
        }

        private void Image_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            ImageDropBorder.BorderBrush =
                new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 212)); // azul
        }

        private void Image_DragLeave(object sender, DragEventArgs e)
        {
            ImageDropBorder.BorderBrush =
                (Brush)Application.Current.Resources["SystemControlForegroundBaseLowBrush"];
        }


        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var itemSeleccionado = (TreeCategorias.SelectedItem as ExplorerItem);
            //Uso parametros para evitar SQL Injection
            string sql = @"
            INSERT INTO categoria_prendas (nombre, parent_id, image_path)
            VALUES (@nombre, @parent_id, @image_path)";
            //Pasar parametros
            var parametros = new MySqlParameter[]
            {
                new MySqlParameter("@nombre",TxtNombre.Text),
                new MySqlParameter("@parent_id",
                        itemSeleccionado != null && itemSeleccionado.IdCategoria != 0
                        ? itemSeleccionado.IdCategoria
                        : (object)DBNull.Value
                ),
                new MySqlParameter("@image_path",
                    TxtNombre.Text + _image?.FileType
                )
            };
            int idGenerado = DataBase.DataBase.Execute(sql,parametros);
            //Guardar la imagen, pasar la ruta (EXPLORER ITEM padre e SELECCIONADO) y la imagen
            bool exito = await ImageResolver.GuardarImagenCategoriaPrenda(ConstruirPathExplorerItem(itemSeleccionado),_image, TxtNombre.Text);
            //Mostrar un popup
            ContentDialog dialog;
            if (idGenerado == 0 || !exito)
            {
                dialog = new ContentDialog
                {
                    Title = "Error al Guardar",
                    Content = "No se pudo registrar la categoria correctamente",
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.XamlRoot
                };

            }
            else
            {
                //Crer un dialogo que muestre el resultado
                dialog = new ContentDialog
                {
                    Title = "Guardado",
                    Content = "Categoría registrada correctamente",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
            }
            //Mostrar un dialogo
            await dialog.ShowAsync();
            Frame.Navigate(typeof(CategoriasPrendas));
        }

        private string ConstruirPathExplorerItem(ExplorerItem item)
        {
            List<string> partes = new();

            if (item.ParentId != null)
            {
                while (item.ParentId != null)
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
            }

            // Unir correctamente como ruta
            return Path.Combine(partes.ToArray());
        }

    }
}
