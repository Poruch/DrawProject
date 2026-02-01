using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;

namespace DrawProject.Services
{
    internal class SaveService
    {
        public static bool SaveBitmapToPng(BitmapSource bitmap, Window parentWindow = null)
        {
            if (bitmap == null)
            {
                MessageBox.Show("Нет изображения для сохранения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                // Создаем диалог сохранения файла
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|BMP Image|*.bmp|All Files|*.*",
                    FilterIndex = 1,
                    DefaultExt = ".png",
                    AddExtension = true,
                    Title = "Сохранить изображение",
                    FileName = $"drawing_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                // Показываем диалог
                bool? result = parentWindow != null
                    ? saveFileDialog.ShowDialog(parentWindow)
                    : saveFileDialog.ShowDialog();

                if (result == true)
                {
                    // Сохраняем в выбранном формате
                    return SaveBitmapToFile(bitmap, saveFileDialog.FileName);
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }



        /// <summary>
        /// Сохраняет BitmapSource в файл
        /// </summary>
        private static bool SaveBitmapToFile(BitmapSource bitmap, string filePath)
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLower();
                BitmapEncoder encoder = extension switch
                {
                    ".png" => new PngBitmapEncoder(),
                    ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 90 },
                    ".bmp" => new BmpBitmapEncoder(),
                    _ => new PngBitmapEncoder()
                };

                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                // Показываем успешное сообщение
                MessageBox.Show($"Изображение успешно сохранено:\n{filePath}",
                    "Сохранено", MessageBoxButton.OK, MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }



    }
}
