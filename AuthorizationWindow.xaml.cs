using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace ShoeStore_Filkova
{
    public partial class AuthorizationWindow : Window
    {
        public AuthorizationWindow()
        {
            InitializeComponent();
        }

        // Кнопка "Войти"
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();  // Читаем логин из текстового поля и убираем пробелы по краям
            string password = PbPassword.Password.Trim(); // Чтобы не просматривать пароль

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password)) // Проверка на пустые поля
            {
                MessageBox.Show("Введите логин и пароль!");
                return;
            }

            // using создаёт новый контекст 
            using (var context = ShoeStore_FilkovaEntities1.GetContext())
            {
                // Ищем в таблице Users пользователя, у которого логин и пароль совпадают с введёнными
                var user = context.Users
                    .FirstOrDefault(u => u.Login == login && u.Password == password);

                if (user == null)
                {
                    MessageBox.Show("Неверный логин или пароль!");
                    return;
                }

                string role = user.User_Roles.Name;
                OpenMainMenu(role, user.FullName);
            }
        }

        // Кнопка "Войти как гость"
        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            OpenMainMenu("Гость", "Гость");
        }

        // Вспомогательный метод: открывает MainMenuWindow и закрывает текущее окно
        private void OpenMainMenu(string role, string userName)
        {
            var main = new MainMenuWindow(role, userName);
            main.Show();
            this.Close();
        }
    }
}