using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using NavigationSection.Images;
using NavigationSection.Model;
using Org.BouncyCastle.Asn1.Mozilla;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.NumberFormatting;
using Windows.Storage;
using Windows.Storage.Pickers;
using static System.Runtime.InteropServices.JavaScript.JSType;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NavigationSection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NuevoDisfraz : Page
    {
        public ObservableCollection<ExplorerItem> DataSource { get; set; }
        public string[] pathCategoriaPadreNav;

        public ListaTallasStock ListaStock { get; } = new();

        public NuevoDisfraz()
        {
            this.InitializeComponent();
            DataSource = GetData();
            this.DataContext = this;
            //Poner datos hardcodeados de Tallas
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
            string sql = @"SELECT id_categoria, nombre, parent_id FROM categoria";
            var categorias = DataBase.DataBase.DoQuery(sql,
                reader => new Categoria
                {
                    IdCategoria = reader.GetInt32("id_categoria"),
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

        private void ValidarCampos()
        {
            /*Obtener data de los campos
            nombre = TxtNombre.Text,
                imageName = TxtNombre.Text + _image.FileType,
                categoria = item.IdCategoria,
                descripcion = TxtDescripcion.Text,
                genero = (RadioButtonGenero.SelectedItem as RadioButton).Tag.ToString(),
                precioAlquiler = NumberPrecioAlquiler.Value,
                precioVenta = NumberPrecioVenta.Value,
                tallas = ListaStock.Tallas.ToList(),*/
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            ValidarCampos();
            var itemSeleccionado = (TreeCategorias.SelectedItem as ExplorerItem);
            //un json con toda la data
            var jsonData = GetDataForm(itemSeleccionado);
            Debug.Write(jsonData);
            //Especificar para STORE PROCEDURE
            var pId = new MySqlParameter("@p_id_disfraz", MySqlDbType.Int32)
            {
                Direction = ParameterDirection.Output
            };
            var parametros = new MySqlParameter[]
            {
                new MySqlParameter("@disfrazData", jsonData),
                pId
            };
            DataBase.DataBase.ExecuteProcedure("InsertarDisfraz", parametros);
            int idGenerado = Convert.ToInt32(pId.Value);
            bool exito = await ImageResolver.GuardarImagenDisfraz(ConstruirPathExplorerItem(itemSeleccionado), _image, TxtNombre.Text);
            //Mostrar un popup
            ContentDialog dialog;
            Debug.WriteLine(exito + " " + idGenerado);
            Debug.WriteLine(jsonData);
            if (idGenerado > 0 && !exito)
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
                    Content = "Disfraz registrado correctamente",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
            }
            //Mostrar un dialogo
            await dialog.ShowAsync();
            Frame.Navigate(typeof(CategoriasPage));
        }

        private string GetDataForm(ExplorerItem item)
        {
            var data = new
            {
                nombre = TxtNombre.Text,
                imageName = TxtNombre.Text + _image.FileType,
                categoria = item.IdCategoria,
                descripcion = TxtDescripcion.Text,
                genero = (RadioButtonGenero.SelectedItem as RadioButton).Tag.ToString(),
                precioAlquiler = NumberPrecioAlquiler.Value,
                precioVenta = NumberPrecioVenta.Value,
                tallas= ListaStock.Tallas.ToList(),
            };
            return JsonSerializer.Serialize(data);
        }

        private string ConstruirPathExplorerItem(ExplorerItem item)
        {
            List<string> partes = new();

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
            return Path.Combine(partes.ToArray());
        }

        private void CmbTipoPrenda_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //NbTallaNumerica.Visibility = Visibility.Collapsed;
            //NbLargoPantalon.Visibility = Visibility.Collapsed;

            if (CmbTipoPrenda.SelectedItem is ComboBoxItem item)
            {
                string tipo = item.Content.ToString();

                switch (tipo)
                {
                    case "Polo":
                    case "Camisa":
                    case "Casaca":
                        //NbTallaNumerica.Visibility = Visibility.Visible;
                        break;

                    case "Pantalón":
                    case "Falda":
                        //NbLargoPantalon.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TallaStock item)
            {
                ListaStock.Tallas.Remove(item);
            }
        }

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TallaStock item)
            {
                Debug.WriteLine($"Editando talla {item.Talla} - stock {item.Stock}");
            }
        }

        private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            ListaStock.Tallas.Add(new TallaStock
            {
                Talla = 2,
                Stock = 0
            });
        }

    }
}
