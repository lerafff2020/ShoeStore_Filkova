using Microsoft.Win32;
using System;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Path = System.IO.Path;   // ← алиас, иначе конфликт с System.Windows.Shapes.Path

namespace ShoeStore_Filkova
{
    public partial class AddEditTovarWindow : Window
    {
        // Текущий товар. Если ID == 0 — добавление, иначе — редактирование.
        private Products _currentTovar;

        // Папка с картинками товаров — рядом с exe (bin/Debug/ProductImages)
        private readonly string _imagesFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductImages");

        /// <summary>
        /// Конструктор окна.
        /// </summary>
        /// <param name="selectedTovar">
        /// null — режим добавления нового товара;
        /// объект Products — режим редактирования.
        /// </param>
        public AddEditTovarWindow(Products selectedTovar)
        {
            InitializeComponent();

            // Создаём папку для картинок, если её ещё нет
            if (!Directory.Exists(_imagesFolder))
                Directory.CreateDirectory(_imagesFolder);

            // Справочники загружаем ДО установки DataContext —
            // иначе ComboBox может не найти нужный элемент при первом рендере
            LoadComboBoxData();

            if (selectedTovar != null)
            {
                // РЕЖИМ РЕДАКТИРОВАНИЯ 
                _currentTovar = selectedTovar;
                TbID.Text = _currentTovar.ID.ToString();
            }
            else
            {
                // РЕЖИМ ДОБАВЛЕНИЯ 
                _currentTovar = new Products();

                // ID = MAX(ID) + 1, так как в БД столбец ID не IDENTITY
                using (var context = ShoeStore_FilkovaEntities1.GetContext())
                {
                    int maxId = context.Products.Any()
                        ? context.Products.Max(p => p.ID)
                        : 0;
                    _currentTovar.ID = maxId + 1;
                }

                TbID.Text = _currentTovar.ID.ToString();

                // Значения по умолчанию
                _currentTovar.Price = 0;
                _currentTovar.StockQuantity = 0;
                _currentTovar.Discount = 0;
            }

            // Привязка данных: все поля XAML через {Binding ...}
            // автоматически читают/пишут в _currentTovar
            DataContext = _currentTovar;
        }

        /// <summary>
        /// Загрузка справочников для ComboBox'ов.
        /// </summary>
        private void LoadComboBoxData()
        {
            using (var context = ShoeStore_FilkovaEntities1.GetContext())
            {
                CmbCategory.ItemsSource = context.Categories.ToList();
                CmbManufacturer.ItemsSource = context.Manufacturers.ToList();
                CmbSupplier.ItemsSource = context.Suppliers.ToList();
                CmbProductName.ItemsSource = context.Product_Names.ToList();
            }
        }

        // PreviewTextInput срабатывает ДО попадания символа в TextBox.
        // e.Handled = true отменяет ввод.

        private void TbPrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем цифры, точку и запятую (для копеек)
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.,]+$");
        }

        private void TbStockQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        private void TbDiscount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        //  СОХРАНЕНИЕ ФОТО
        private void BtnSelectImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Выберите изображение товара"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                // Уникальное имя файла, чтобы не перезаписать чужое
                string newFileName = $"tovar_{_currentTovar.ID}_{DateTime.Now.Ticks}.jpg";
                string newPath = Path.Combine(_imagesFolder, newFileName);

                // Ресайз до 300×200 (требование задания)
                ResizeImage(dialog.FileName, newPath, 300, 200);

                // Удаляем старое фото, если оно в нашей папке
                DeleteOldPhotoIfInAppFolder();

                // Обновляем путь в товаре
                _currentTovar.Photo = newPath;

                // Обновляем превью
                ImgPhoto.Source = new BitmapImage(new Uri(newPath));

                MessageBox.Show("Изображение установлено!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
            }
        }

        /// <summary>
        /// Ресайз изображения до width×height с высоким качеством.
        /// </summary>
        private void ResizeImage(string sourcePath, string destPath, int width, int height)
        {
            using (var original = System.Drawing.Image.FromFile(sourcePath))
            {
                var dest = new Bitmap(width, height);
                using (var g = Graphics.FromImage(dest))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(original, 0, 0, width, height);
                }
                dest.Save(destPath, ImageFormat.Jpeg);
                dest.Dispose();
            }
        }

        /// <summary>
        /// Удаляет старое фото, если оно находится в папке приложения.
        /// Файлы с диска D: (например, из Работы 2) не трогаем.
        /// </summary>
        private void DeleteOldPhotoIfInAppFolder()
        {
            if (string.IsNullOrEmpty(_currentTovar.Photo)) return;

            try
            {
                if (File.Exists(_currentTovar.Photo) &&
                    _currentTovar.Photo.StartsWith(_imagesFolder, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(_currentTovar.Photo);
                }
            }
            catch
            {
                // Не критично — игнорируем
            }
        }


        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // --- Валидация обязательных полей ---
            if (string.IsNullOrWhiteSpace(_currentTovar.Article))
            {
                MessageBox.Show("Введите артикул!"); return;
            }
            if (_currentTovar.ProductNameID == 0)
            {
                MessageBox.Show("Выберите наименование товара!"); return;
            }
            if (_currentTovar.CategoryID == 0)
            {
                MessageBox.Show("Выберите категорию!"); return;
            }
            if (_currentTovar.ManufacturerID == 0)
            {
                MessageBox.Show("Выберите производителя!"); return;
            }
            if (_currentTovar.SupplierID == 0)
            {
                MessageBox.Show("Выберите поставщика!"); return;
            }
            if (string.IsNullOrWhiteSpace(_currentTovar.Unit))
            {
                MessageBox.Show("Введите единицу измерения!"); return;
            }
            if (_currentTovar.Price < 0)
            {
                MessageBox.Show("Цена не может быть отрицательной!"); return;
            }
            if (_currentTovar.StockQuantity < 0)
            {
                MessageBox.Show("Количество не может быть отрицательным!"); return;
            }

            // --- Собираем "чистый" Products только из скалярных полей и FK ---
            // ВАЖНО: не копируем навигационные свойства (Categories, Manufacturers и т.д.),
            // иначе EF попытается вставить эти связанные сущности в БД как новые.
            var tovarToSave = new Products
            {
                ID = _currentTovar.ID,
                Article = _currentTovar.Article,
                ProductNameID = _currentTovar.ProductNameID,
                CategoryID = _currentTovar.CategoryID,
                ManufacturerID = _currentTovar.ManufacturerID,
                SupplierID = _currentTovar.SupplierID,
                Unit = _currentTovar.Unit,
                Price = _currentTovar.Price,
                Discount = _currentTovar.Discount ?? 0,
                StockQuantity = _currentTovar.StockQuantity,
                Description = _currentTovar.Description,
                Photo = _currentTovar.Photo
            };

            using (var context = ShoeStore_FilkovaEntities1.GetContext())
            {
                // Проверяем, существует ли товар с таким ID
                bool exists = context.Products.Any(p => p.ID == tovarToSave.ID);

                if (exists)
                {
                    // Редактирование: прикрепляем и помечаем как Modified
                    context.Products.Attach(tovarToSave);
                    context.Entry(tovarToSave).State = EntityState.Modified;
                }
                else
                {
                    // Добавление: добавляем как новый
                    context.Products.Add(tovarToSave);
                }

                try
                {
                    context.SaveChanges();
                    MessageBox.Show("Данные сохранены!");
                    this.DialogResult = true;
                    this.Close();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // Расшифровка ошибок валидации EF — видно, какое поле не так
                    var errors = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");
                    MessageBox.Show("Ошибки валидации:\n" + string.Join("\n", errors));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}");
                }
            }
        }
    }
}