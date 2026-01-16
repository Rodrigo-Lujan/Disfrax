using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NavigationSection.Images
{
    public static class ImageResolver
    {
        public static readonly string BasePath = @"C:\Users\rodri\OneDrive\Pictures\TitaDisfraces\";

        //la imagen en Bits para los elementos de la UI
        //tipo es "categoria o prendas"
        public static BitmapImage? ToBitMap(string path,string tipo)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            var fullPath = System.IO.Path.Combine(BasePath,tipo, path);
            Debug.WriteLine(fullPath);
            return new BitmapImage(new Uri(fullPath));
        }

        //Construir Ruta Completa
        public static async Task<bool> GuardarImagenCategoria(
            string rutaRelativa,
            StorageFile imagen,
            string imageName)
        {
            try
            {
                //Construir ruta completa
                var fullPath = System.IO.Path.Combine(BasePath + "/categorias/", rutaRelativa);
                //Crear carpeta si no existe
                Directory.CreateDirectory(fullPath);
                //Ruta final del archivo
                fullPath = Path.Combine(fullPath,imageName + imagen.FileType);
                //Guardar imagen con streams (cadenas binarias): primero leo 
                using var sourceStream = await imagen.OpenStreamForReadAsync();
                using var targetStream = File.Create(fullPath);
                await sourceStream.CopyToAsync(targetStream);   
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error guardando imagen: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> GuardarImagenDisfraz(
            string rutaRelativa,
            StorageFile imagen,
            string imageName)
        {
            try
            {
                //Construir ruta completa
                var fullPath = System.IO.Path.Combine(BasePath + "/categorias/", rutaRelativa, "disfraces");
                //Crear carpeta si no existe
                Directory.CreateDirectory(fullPath);
                //Ruta final del archivo + /disfraces/
                fullPath = Path.Combine(fullPath, imageName + imagen.FileType);
                //Guardar imagen con streams (cadenas binarias): primero leo 
                using var sourceStream = await imagen.OpenStreamForReadAsync();
                using var targetStream = File.Create(fullPath);
                await sourceStream.CopyToAsync(targetStream);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error guardando imagen: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> GuardarImagenCategoriaPrenda(
            string rutaRelativa,
            StorageFile imagen,
            string imageName)
        {
            try
            {
                //Construir ruta completa
                var fullPath = System.IO.Path.Combine(BasePath + "/prendas/", rutaRelativa);
                Debug.WriteLine("La ruta de la carpeta es: " + fullPath);
                //Crear carpeta si no existe
                Directory.CreateDirectory(fullPath);
                //Ruta final del archivo + /disfraces/
                fullPath = Path.Combine(fullPath, imageName + imagen.FileType);
                Debug.WriteLine("La ruta de la carpeta con la imagen es: " + fullPath);
                //Guardar imagen con streams (cadenas binarias): primero leo 
                using var sourceStream = await imagen.OpenStreamForReadAsync();
                using var targetStream = File.Create(fullPath);
                await sourceStream.CopyToAsync(targetStream);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error guardando imagen: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> GuardarImagenPrenda(
            string rutaRelativa,
            StorageFile imagen,
            string imageName)
        {
            try
            {
                //Construir ruta completa
                var fullPath = System.IO.Path.Combine(BasePath + "/prendas/", rutaRelativa, "prendas");
                //Crear carpeta si no existe
                Directory.CreateDirectory(fullPath);
                //Ruta final del archivo + /disfraces/
                fullPath = Path.Combine(fullPath, imageName + imagen.FileType);
                //Guardar imagen con streams (cadenas binarias): primero leo 
                using var sourceStream = await imagen.OpenStreamForReadAsync();
                using var targetStream = File.Create(fullPath);
                await sourceStream.CopyToAsync(targetStream);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error guardando imagen: {ex.Message}");
                return false;
            }
        }
    }
}
