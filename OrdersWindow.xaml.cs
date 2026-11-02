using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ShoeStore_Filkova
{
    public partial class OrdersWindow : Window
    {
        private string _role;
        private string _userName;
        private List<Orders> _allOrders;

        // Общий флаг — не даём открыть два окна редактирования заказа
        private static bool _editWindowOpen = false;

        public OrdersWindow(string role, string userName)
        {
            InitializeComponent();

            _role = role;
            _userName = userName;

            TbUserInfo.Text = role == "Гость" ? "Гость" : userName;

            // Кнопки добавления/удаления — только администратору
            if (role == "Администратор")
            {
                BtnAddOrder.Visibility = Visibility.Visible;
                BtnDeleteOrder.Visibility = Visibility.Visible;
            }

            LoadOrders();
        }

        // Загрузка списка заказов из БД
        private void LoadOrders()
        {
            using (var context = ShoeStore_FilkovaEntities1.GetContext())
            {
                _allOrders = context.Orders
                    .Include("Order_Status")
                    .Include("pickup_points")
                    .Include("Users")
                    .ToList();
            }

            ListOrders.ItemsSource = _allOrders;
        }

        // ================ ДОБАВЛЕНИЕ ================

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_editWindowOpen)
            {
                MessageBox.Show("Окно редактирования заказа уже открыто!");
                return;
            }

            _editWindowOpen = true;
            try
            {
                // null — режим добавления. Передаём текущего пользователя,
                // чтобы проставить UserID у нового заказа.
                var addWindow = new AddEditOrderWindow(null, _userName);
                addWindow.Owner = this;

                if (addWindow.ShowDialog() == true)
                    LoadOrders();
            }
            finally { _editWindowOpen = false; }
        }

        // ================ РЕДАКТИРОВАНИЕ ================

        private void ListOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Редактировать может только администратор
            if (_role != "Администратор") return;

            if (_editWindowOpen)
            {
                MessageBox.Show("Окно редактирования заказа уже открыто!");
                return;
            }

            var selectedOrder = ListOrders.SelectedItem as Orders;
            if (selectedOrder == null) return;

            _editWindowOpen = true;
            try
            {
                var editWindow = new AddEditOrderWindow(selectedOrder, _userName);
                editWindow.Owner = this;

                if (editWindow.ShowDialog() == true)
                    LoadOrders();
            }
            finally { _editWindowOpen = false; }
        }

        // УДАЛЕНИЕ 

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = ListOrders.SelectedItem as Orders;
            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ для удаления!");
                return;
            }

            // Подтверждение
            if (MessageBox.Show($"Удалить заказ с кодом \"{selectedOrder.PickupCode}\"?\n" +
                                "Все товары в заказе также будут удалены.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes)
                return;

            try
            {
                using (var context = ShoeStore_FilkovaEntities1.GetContext())
                {
                    // 1. Проверяем, что заказ вообще есть в БД
                    var orderToDelete = context.Orders.FirstOrDefault(o => o.ID == selectedOrder.ID);
                    if (orderToDelete == null)
                    {
                        MessageBox.Show($"Заказ с ID = {selectedOrder.ID} не найден в базе.\n" +
                                        "Возможно, он уже удалён. Обновляю список...");
                        LoadOrders();
                        return;
                    }

                    // 2. Сначала удаляем все позиции заказа (Order_Items)
                    var orderItems = context.Order_Items
                        .Where(oi => oi.OrderID == selectedOrder.ID)
                        .ToList();

                    foreach (var item in orderItems)
                    {
                        context.Order_Items.Remove(item);
                    }

                    // 3. Удаляем сам заказ
                    context.Orders.Remove(orderToDelete);
                    context.SaveChanges();
                }

                MessageBox.Show("Заказ удалён!");
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}");
            }
        }
    }
}