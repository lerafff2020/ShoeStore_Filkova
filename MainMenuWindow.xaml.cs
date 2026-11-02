using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Path = System.IO.Path;   // алиас, иначе конфликт с System.Windows.Shapes.Path

namespace ShoeStore_Filkova
{
    public partial class MainMenuWindow : Window
    {
        // Роль и ФИО храним в полях — нужны вне конструктора
        private string _role;
        private string _userName;

        // Кэш полного списка товаров
        private List<Products> _allProducts;

        // Флаг: открыто ли сейчас окно редактирования товара.
        // static — общий для всех экземпляров MainMenuWindow,
        // поэтому второе окно редактирования не откроется.
        private static bool _editWindowOpen = false;

        public MainMenuWindow(string role, string userName)
        {
            InitializeComponent();

            _role = role;
            _userName = userName;

            // ФИО в правом верхнем углу
            TbUserInfo.Text = role == "Гость" ? "Гость" : userName;

            // Кнопки добавления/удаления товара видны только администратору
            if (role == "Администратор")
            {
                BtnAddProduct.Visibility = Visibility.Visible;
                BtnDeleteProduct.Visibility = Visibility.Visible;
            }

            // Кнопка "Заказы" — для Менеджера и Администратора
            if (role == "Менеджер" || role == "Администратор")
            {
                BtnOrders.Visibility = Visibility.Visible;
            }

            LoadData();
            LoadSuppliers();
        }

        //  ЗАГРУЗКА ДАННЫХ

        private void LoadData()
        {
            using (var context = ShoeStore_FilkovaEntities1.GetContext())
            {
                _allProducts = context.Products
                    .Include("Categories")
                    .Include("Manufacturers")
                    .Include("Suppliers")
                    .Include("Product_Names")
                    .ToList();
            }

            ListTovar.ItemsSource = _allProducts;
        }

        private void LoadSuppliers()
        {
            using (var context = ShoeStore_FilkovaEntities1.GetContext())
            {
                var suppliers = context.Suppliers.ToList();

                // Первый элемент — "Все поставщики"
                var allOption = new Suppliers { ID = -1, Name = "Все поставщики" };
                suppliers.Insert(0, allOption);

                CmbSupplier.ItemsSource = suppliers;
                CmbSupplier.SelectedIndex = 0;
            }
        }

        //  ФИЛЬТРАЦИЯ 

        private void Filtr()
        {
            if (_allProducts == null) return;

            var result = _allProducts.AsEnumerable();

            // 1. Фильтр по поставщику
            if (CmbSupplier.SelectedItem is Suppliers selectedSupplier && selectedSupplier.ID != -1)
            {
                result = result.Where(p => p.SupplierID == selectedSupplier.ID);
            }

            // 2. Поиск по всем текстовым полям
            string search = TbSearch.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                result = result.Where(p =>
                    (p.Article != null && p.Article.ToLower().Contains(search)) ||
                    (p.Unit != null && p.Unit.ToLower().Contains(search)) ||
                    (p.Description != null && p.Description.ToLower().Contains(search)) ||
                    (p.Categories != null && p.Categories.Name.ToLower().Contains(search)) ||
                    (p.Manufacturers != null && p.Manufacturers.Name.ToLower().Contains(search)) ||
                    (p.Suppliers != null && p.Suppliers.Name.ToLower().Contains(search)) ||
                    (p.Product_Names != null && p.Product_Names.Name.ToLower().Contains(search))
                );
            }

            // 3. Сортировка по количеству на складе
            if (BtnSortAsc.Tag?.ToString() == "active")
                result = result.OrderBy(p => p.StockQuantity);
            else if (BtnSortDesc.Tag?.ToString() == "active")
                result = result.OrderByDescending(p => p.StockQuantity);

            ListTovar.ItemsSource = result.ToList();
        }

        //  ОБРАБОТЧИКИ ФИЛЬТРОВ

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            Filtr();
        }

        private void BtnSortAsc_Click(object sender, RoutedEventArgs e)
        {
            BtnSortAsc.Tag = "active";
            BtnSortDesc.Tag = null;
            Filtr();
        }

        private void BtnSortDesc_Click(object sender, RoutedEventArgs e)
        {
            BtnSortDesc.Tag = "active";
            BtnSortAsc.Tag = null;
            Filtr();
        }

        private void CmbSupplier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Filtr();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            TbSearch.Text = "";
            CmbSupplier.SelectedIndex = 0;
            BtnSortAsc.Tag = null;
            BtnSortDesc.Tag = null;
            Filtr();
        }

        //  ВЫХОД ИЗ СИСТЕМЫ 

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var auth = new AuthorizationWindow();
            auth.Show();
            this.Close();
        }

        // ДОБАВЛЕНИЕ ТОВАРА 

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (_editWindowOpen)
            {
                MessageBox.Show("Окно редактирования уже открыто!");
                return;
            }

            _editWindowOpen = true;
            try
            {
                var addWindow = new AddEditTovarWindow(null);
                addWindow.Owner = this;

                if (addWindow.ShowDialog() == true)
                {
                    LoadData();
                    Filtr();
                }
            }
            finally
            {
                _editWindowOpen = false;
            }
        }

        //  РЕДАКТИРОВАНИЕ (ДВОЙНОЙ КЛИК)

        private void ListTovar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_role != "Администратор") return;

            if (_editWindowOpen)
            {
                MessageBox.Show("Окно редактирования уже открыто!");
                return;
            }

            var selectedTovar = ListTovar.SelectedItem as Products;
            if (selectedTovar == null) return;

            _editWindowOpen = true;
            try
            {
                var editWindow = new AddEditTovarWindow(selectedTovar);
                editWindow.Owner = this;

                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                    Filtr();
                }
            }
            finally
            {
                _editWindowOpen = false;
            }
        }

        //  УДАЛЕНИЕ ТОВАРА 

        private void BtnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            var selectedTovar = ListTovar.SelectedItem as Products;
            if (selectedTovar == null)
            {
                MessageBox.Show("Выберите товар для удаления!");
                return;
            }

            // Проверяем, есть ли товар в заказах
            using (var context = ShoeStore_FilkovaEntities1.GetContext())
            {
                bool inOrder = context.Order_Items.Any(oi => oi.ProductID == selectedTovar.ID);
                if (inOrder)
                {
                    MessageBox.Show("Нельзя удалить товар, который присутствует в заказе!");
                    return;
                }
            }

            // Подтверждение
            if (MessageBox.Show($"Удалить товар \"{selectedTovar.Product_Names?.Name}\"?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes)
                return;

            try
            {
                // Удаляем фото из папки ProductImages, если оно там лежит
                string imagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductImages");
                if (!string.IsNullOrEmpty(selectedTovar.Photo) &&
                    File.Exists(selectedTovar.Photo) &&
                    selectedTovar.Photo.StartsWith(imagesFolder, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(selectedTovar.Photo);
                }

                using (var context = ShoeStore_FilkovaEntities1.GetContext())
                {
                    context.Products.Attach(selectedTovar);
                    context.Products.Remove(selectedTovar);
                    context.SaveChanges();
                }

                MessageBox.Show("Товар удалён!");

                LoadData();
                Filtr();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}");
            }
        }

        // ЗАКАЗЫ (Работа 5) 

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            var ordersWindow = new OrdersWindow(_role, _userName);
            ordersWindow.Owner = this;
            ordersWindow.ShowDialog();

            // После закрытия окна заказов обновляем список товаров —
            // количество на складе могло измениться
            LoadData();
            Filtr();
        }
    }
}