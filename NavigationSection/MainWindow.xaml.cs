using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NavigationSection
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            //TestConnectionMySql();
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // ¿Se presiono Configuracion?
            if (!args.IsSettingsSelected)
            {
                var itemSeleccionado = (NavigationViewItem)args.SelectedItem;

                HeaderTitle.Text = itemSeleccionado.Tag?.ToString();

                switch (itemSeleccionado.Content?.ToString())
                {
                    case "Disfraces":
                        //Action encapsula un metodo con un parametro que no retorna nada (void)
                        HeaderEditableDos.Text = "Mis Disfraces";
                        ContentFrame.Navigate(
                            typeof(CategoriasPage),
                            new Action<string>(titulo => HeaderTitle.Text = titulo)
                        );
                        break;
                    case "Prendas":
                        HeaderEditableDos.Text = "Mis Prendas";
                        ContentFrame.Navigate(typeof(CategoriasPrendas),
                            new Action<string>(titulo => HeaderTitle.Text = titulo));
                        break;
                }
            }
            else
            {
                HeaderEditableDos.Text = "Configuracion";
                ContentFrame.Navigate(typeof(SettingsPage));
            }
        }
    }
}
