using System;
using System.Linq;
using System.Windows;

namespace ShoeStore_Filkova
{
    public partial class AddEditOrderWindow : Window
    {
        // Текущий заказ. ID == 0 — добавление, ID != 0 — редактирование.
        private Orders _currentOrder;

        // Имя пользователя (для UserID при создании нового заказа)
        private string _userName;

        public AddEditOrderWindow(Orders selectedOrder, string userName)
        {
            InitializeComponent();

            _userName = userName;

            LoadComboBoxData();

            if (selectedOrder != null)
            {
                // Режим редактирования
                _currentOrder = selectedOrder;
                TbID.Text = _currentOrder.ID.ToString();
            }
            else
            {
                // Режим добавления
                _currentOrder = new Orders();

                // ID = MAX(ID) + 1
                using (var context = ShoeStore_FilkovaEntities1.GetContext())
                {
                    int maxId = context.Orders.Any()
                        ? context.Orders.Max(o => o.ID)
                        : 0;
                    _currentOrder.ID = maxId + 1;
                }

                TbID.Text = _currentOrder.ID.ToString();

                // Даты по умолчанию: сегодня и +7 дней
                _currentOrder.OrderDate = DateTime.Today;
                _currentOrder.DeliveryDate = DateTime.Today.AddDays(7);

                // Код получения по умолчанию
                _currentOrder.PickupCode = "000";

                // UserID — по имени пользователя (если нашли — берём ID).
                // Если не нашли (вдруг гостя как-то сюда занесло) — ставим 1.
                using (var context = ShoeStore_FilkovaEntities1.GetContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.FullName == _userName);
                    _currentOrder.UserID = user?.ID ?? 1;
                }
            }

            // DataContext — все поля формы читают/пишут в _currentOrder
            DataContext = _currentOrder;
        }

        // Загрузка справочников
        private void LoadComboBoxData()
        {
            using (var context = ShoeStore_FilkovaEntities1.GetContext())
            {
                CmbStatus.ItemsSource = context.Order_Status.ToList();
                CmbPickupPoint.ItemsSource = context.pickup_points.ToList();
            }
        }

        // Сохранение
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // --- Валидация ---
            if (string.IsNullOrWhiteSpace(_currentOrder.PickupCode))
            {
                MessageBox.Show("Введите артикул заказа!"); return;
            }
            if (_currentOrder.StatusID == 0)
            {
                MessageBox.Show("Выберите статус заказа!"); return;
            }
            if (_currentOrder.PickupPointID == 0)
            {
                MessageBox.Show("Выберите пункт выдачи!"); return;
            }
            if (_currentOrder.OrderDate == null)
            {
                MessageBox.Show("Укажите дату заказа!"); return;
            }
            if (_currentOrder.DeliveryDate == null)
            {
                MessageBox.Show("Укажите дату выдачи!"); return;
            }
            if (_currentOrder.DeliveryDate < _currentOrder.OrderDate)
            {
                MessageBox.Show("Дата выдачи не может быть раньше даты заказа!");
                return;
            }

            // --- Собираем "чистый" Orders только из скалярных полей и FK ---
            var orderToSave = new Orders
            {
                ID = _currentOrder.ID,
                OrderDate = _currentOrder.OrderDate,
                DeliveryDate = _currentOrder.DeliveryDate,
                PickupPointID = _currentOrder.PickupPointID,
                UserID = _currentOrder.UserID,
                PickupCode = _currentOrder.PickupCode,
                StatusID = _currentOrder.StatusID
            };

            using (var context = ShoeStore_FilkovaEntities1.GetContext())
            {
                bool exists = context.Orders.Any(o => o.ID == orderToSave.ID);

                if (exists)
                {
                    context.Orders.Attach(orderToSave);
                    context.Entry(orderToSave).State = System.Data.EntityState.Modified;
                }
                else
                {
                    context.Orders.Add(orderToSave);
                }

                try
                {
                    context.SaveChanges();
                    MessageBox.Show("Заказ сохранён!");
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}");
                }
            }
        }
    }
}