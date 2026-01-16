using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
using NavigationSection.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NavigationSection
{
    public sealed partial class VerPrendaWindow : Window
    {
        public Prenda Prenda { get; set; }
        //De parametro tengo el disfraz al mostrar esto 
        public VerPrendaWindow(Prenda prenda)
        {
            Prenda = prenda;
            Prenda.stockPorTalla = ObtenerTallas();
            InitializeComponent();
        }


        //Obtener las prendas del disfraz
        private List<TallaStock> ObtenerTallas()
        {
            string sql = @"SELECT
	                        talla,
                            stock
                        FROM stock_prenda
                        WHERE id_prenda = @id_prenda";
            var parametros = new MySqlParameter[]
            {
                new MySqlParameter("@id_prenda",Prenda.IdPrenda)
            };
            //Definir un reader y con ello construir la ruta de cada prenda
            //arbol de categorias de las prendas 
            return DataBase.DataBase.DoQuery(sql,
                reader => new TallaStock
                {
                    Talla = reader.GetInt32("talla"),
                    Stock = reader.GetInt32("stock"),
                },
                parametros
            );
        }

        private async void EditarTalla_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as TallaStock;
            if (item == null) return;

            var dialog = new ContentDialog
            {
                Title = "Editar talla",
                XamlRoot = this.Content.XamlRoot,
                PrimaryButtonText = "Guardar",
                CloseButtonText = "Cancelar"
            };

            var stockBox = new NumberBox
            {
                Header = "Stock",
                Value = item.Stock,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };

            dialog.Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = $"Talla: {item.Talla}" },
                    stockBox
                }
            };

            var result = await dialog.ShowAsync();
            //Le da click en ok, osea 1
            if (result == ContentDialogResult.Primary)
            {
                await ActualizarStock(item, (int)stockBox.Value);
            }
        }

        private async Task ActualizarStock(TallaStock item, int nuevoStock)
        {
            var loading = new ContentDialog
            {
                Title = "Actualizando...",
                XamlRoot = this.Content.XamlRoot,
                Content = new ProgressRing { IsActive = true },
                CloseButtonText = ""
            };

            _ = loading.ShowAsync();

            await Task.Run(() =>
            {
                string sql = @"UPDATE stock_prenda 
                       SET stock = @stock 
                       WHERE id_prenda = @id_prenda AND talla = @talla";

                var parametros = new MySqlParameter[]
                {
            new MySqlParameter("@stock", nuevoStock),
            new MySqlParameter("@id_prenda", Prenda.IdPrenda),
            new MySqlParameter("@talla", item.Talla)
                };

                DataBase.DataBase.Execute(sql, parametros);
            });

            loading.Hide();

            item.Stock = nuevoStock;
            var index = Prenda.stockPorTalla.IndexOf(item);
            if (index >= 0)
            {
                Prenda.stockPorTalla[index] = item;
            }

            TablaStock.ItemsSource = null;
            TablaStock.ItemsSource = Prenda.stockPorTalla;

        }

        private async void DeleteTalla_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as TallaStock;
            if (item == null)
            {
                return;
            }
            //Mostrar dialogo de confirmacion
            var dialog = new ContentDialog
            {
                Title = "Confirmar borrar talla",
                XamlRoot = this.Content.XamlRoot,
                PrimaryButtonText = "Confirmo",
                CloseButtonText = "Cancelar"
            };

            var result = await dialog.ShowAsync();
            //Le da click en ok, osea 1
            if (result == ContentDialogResult.Primary)
            {
                await BorrarTalla(item);
            }
        }

        private async Task BorrarTalla(TallaStock item)
        {
            var loading = new ContentDialog
            {
                Title = "Actualizando...",
                XamlRoot = this.Content.XamlRoot,
                Content = new ProgressRing { IsActive = true },
                CloseButtonText = ""
            };

            _ = loading.ShowAsync();

            await Task.Run(() =>
            {
                string sql = @"DELETE FROM stock_prenda  
                       WHERE id_prenda = @id_prenda AND talla = @talla";

                var parametros = new MySqlParameter[]
                {
            new MySqlParameter("@id_prenda", Prenda.IdPrenda),
            new MySqlParameter("@talla", item.Talla)
                };

                DataBase.DataBase.Execute(sql, parametros);
            });

            loading.Hide();

            Prenda.stockPorTalla.Remove(item);
            TablaStock.ItemsSource = null;
            TablaStock.ItemsSource = Prenda.stockPorTalla;
        }

        private async void AgregarTalla_Click(object sender, RoutedEventArgs e)
        {
            var tallaBox = new NumberBox
            {
                Header = "Talla",
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Minimum = 1
            };

            var stockBox = new NumberBox
            {
                Header = "Stock",
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                Minimum = 0
            };

            var dialog = new ContentDialog
            {
                Title = "Agregar talla",
                XamlRoot = this.Content.XamlRoot,
                PrimaryButtonText = "Agregar",
                CloseButtonText = "Cancelar",
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children =
            {
                tallaBox,
                stockBox
            }
                }
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                int talla = (int)tallaBox.Value;
                int stock = (int)stockBox.Value;

                if (talla <= 0)
                    return;

                await AgregarTallaBD(talla, stock);
            }
        }

        private async Task AgregarTallaBD(int talla, int stock)
        {
            var loading = new ContentDialog
            {
                Title = "Agregando...",
                XamlRoot = this.Content.XamlRoot,
                Content = new ProgressRing { IsActive = true },
                CloseButtonText = ""
            };

            _ = loading.ShowAsync();

            await Task.Run(() =>
            {
                string sql = @"INSERT INTO stock_prenda (id_prenda, talla, stock)
                       VALUES (@id_prenda, @talla, @stock)";

                var parametros = new MySqlParameter[]
                {
            new MySqlParameter("@id_prenda", Prenda.IdPrenda),
            new MySqlParameter("@talla", talla),
            new MySqlParameter("@stock", stock)
                };

                DataBase.DataBase.Execute(sql, parametros);
            });

            loading.Hide();

            // Agregar a la lista en memoria
            Prenda.stockPorTalla.Add(new TallaStock
            {
                Talla = talla,
                Stock = stock
            });

            // Refrescar tabla
            TablaStock.ItemsSource = null;
            TablaStock.ItemsSource = Prenda.stockPorTalla;
        }

    }
}