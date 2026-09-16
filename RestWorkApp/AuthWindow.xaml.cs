using System;
using System.Data;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using RestaurantWorkApp.Models;

namespace RestaurantWorkApp
{
    public partial class AuthWindow : Window
    {
        private static string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public AuthWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => txtLogin.Focus();
            txtLogin.KeyDown += TxtLogin_KeyDown;
            txtPassword.KeyDown += TxtPassword_KeyDown;
        }

        private void TxtLogin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) txtPassword.Focus();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnLogin_Click(sender, e);
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string loginUser = txtLogin.Text.Trim();
            string loginPassword = txtPassword.Password;

            if (string.IsNullOrEmpty(loginUser) || string.IsNullOrEmpty(loginPassword))
            {
                ShowStatus("⚠️ Введите логин и пароль", System.Windows.Media.Colors.OrangeRed);
                return;
            }

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // 1. Попытка авторизации как работник (Rabotnik)
                    var workerQuery = @"
                SELECT 
                    r.id_Rabotnik, 
                    r.FIO_R, 
                    r.login, 
                    r.id_Rule, 
                    rl.Name_R
                FROM Rabotnik r
                LEFT JOIN Rule rl ON r.id_Rule = rl.id_Rule
                WHERE r.login = @login 
                  AND r.password = AES_ENCRYPT(@password, 'nfkuser8w4')";

                    using (var cmd = new MySqlCommand(workerQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@login", loginUser);
                        cmd.Parameters.AddWithValue("@password", loginPassword);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int roleId = Convert.ToInt32(reader["id_Rule"]);

                                // Проверка: роль должна быть 1–4 для работника
                                if (roleId >= 1 && roleId <= 4)
                                {
                                    CurrentUser.SetUserData(
                                        id: Convert.ToInt32(reader["id_Rabotnik"]),
                                        login: reader["login"].ToString(),
                                        fullName: reader["FIO_R"].ToString(),
                                        roleId: roleId,
                                        roleName: reader["Name_R"].ToString(),
                                        workerId: Convert.ToInt32(reader["id_Rabotnik"])
                                    );

                                    ShowStatus($"✅ Добро пожаловать, {CurrentUser.FullName}!",
                                              System.Windows.Media.Colors.LimeGreen);

                                    // Запуск главного окна через таймер
                                    var timer = new System.Windows.Threading.DispatcherTimer();
                                    timer.Interval = TimeSpan.FromMilliseconds(600);
                                    timer.Tick += (s, args) =>
                                    {
                                        timer.Stop();
                                        MainWindow mainWindow = new MainWindow();
                                        mainWindow.Show();
                                        this.Close();
                                    };
                                    timer.Start();
                                    return;
                                }
                            }
                        }
                    }

                    // 2. Если не работник — пробуем как клиент (Client)
                    var clientQuery = @"
                SELECT 
                    c.id_Client, 
                    c.FIO_C, 
                    c.login, 
                    c.id_Rule, 
                    rl.Name_R
                FROM Client c
                LEFT JOIN Rule rl ON c.id_Rule = rl.id_Rule
                WHERE c.login = @login 
                  AND c.password = AES_ENCRYPT(@password, 'nfkuser8w4')";

                    using (var cmd = new MySqlCommand(clientQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@login", loginUser);
                        cmd.Parameters.AddWithValue("@password", loginPassword);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int roleId = Convert.ToInt32(reader["id_Rule"]);

                                // Роль клиента должна быть 5
                                if (roleId == 5)
                                {
                                    CurrentUser.SetUserData(
                                        id: Convert.ToInt32(reader["id_Client"]),
                                        login: reader["login"].ToString(),
                                        fullName: reader["FIO_C"].ToString(),
                                        roleId: roleId,
                                        roleName: reader["Name_R"].ToString(),
                                        clientId: Convert.ToInt32(reader["id_Client"])
                                    );

                                    ShowStatus($"✅ Добро пожаловать, {CurrentUser.FullName}!",
                                              System.Windows.Media.Colors.LimeGreen);

                                    var timer = new System.Windows.Threading.DispatcherTimer();
                                    timer.Interval = TimeSpan.FromMilliseconds(600);
                                    timer.Tick += (s, args) =>
                                    {
                                        timer.Stop();
                                        MainWindow mainWindow = new MainWindow();
                                        mainWindow.Show();
                                        this.Close();
                                    };
                                    timer.Start();
                                    return;
                                }
                            }
                        }
                    }

                    // Ни один пользователь не найден
                    ShowStatus("❌ Неверный логин или пароль", System.Windows.Media.Colors.OrangeRed);
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Ошибка: {ex.Message}", System.Windows.Media.Colors.Red);
                // Для отладки можно добавить:
                // MessageBox.Show($"Детали ошибки:\n{ex.ToString()}");
            }
        }

        private void ShowStatus(string message, System.Windows.Media.Color color)
        {
            txtStatus.Text = message;
            txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(color);

            statusBorder.Opacity = 0;
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(250)
            };
            statusBorder.BeginAnimation(UIElement.OpacityProperty, anim);
        }
    }
}