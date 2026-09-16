using RestaurantWorkApp.Controls;
using RestaurantWorkApp.Models;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace RestaurantWorkApp
{
    public partial class MainWindow : Window
    {
        private bool directoriesPanelVisible = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверяем авторизацию
            if (!CurrentUser.IsAuthenticated)
            {
                MessageBox.Show("Требуется авторизация!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ReturnToAuth();
                return;
            }

            // Проверяем, имеет ли пользователь доступ к админ-панели (роли 1-4)
            if (AccessControl.IsClient())
            {
                MessageBox.Show("Клиенты не имеют доступа к административной панели!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ReturnToAuth();
                return;
            }

            if (!AccessControl.HasAdminAccess())
            {
                MessageBox.Show("Недостаточно прав для доступа к административной панели!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ReturnToAuth();
                return;
            }

            // Отображаем данные пользователя
            txtUserName.Text = CurrentUser.FullName;
            txtUserRole.Text = CurrentUser.RoleName;

            // Меняем цвет бейджа в зависимости от роли
            UpdateRoleBadgeColor();

            // Приветствие
            txtWelcome.Text = $"Добро пожаловать, {CurrentUser.FullName}!";

            // Загружаем статистику
            LoadStatistics();

            // Можно также скрыть/отключить кнопки, к которым нет доступа
            UpdateUIAccess();
        }

        private void UpdateUIAccess()
        {
            // Отключаем кнопки, к которым нет доступа, вместо того чтобы показывать сообщение об ошибке
            btnDirectories.IsEnabled = AccessControl.HasSectionAccess("rabotnik") ||
                                       AccessControl.HasSectionAccess("dolh") ||
                                       AccessControl.HasSectionAccess("client") ||
                                       AccessControl.HasSectionAccess("dish") ||
                                       AccessControl.HasSectionAccess("zakaz") ||
                                       AccessControl.HasSectionAccess("reserv") ||
                                       AccessControl.HasSectionAccess("category") ||
                                       AccessControl.HasSectionAccess("ingredients") ||
                                       AccessControl.HasSectionAccess("delivery") ||
                                       AccessControl.HasSectionAccess("promotions") ||
                                       AccessControl.HasSectionAccess("feedback") ||
                                       AccessControl.HasSectionAccess("checkzakaz") ||
                                       AccessControl.HasSectionAccess("zone") ||
                                       AccessControl.HasSectionAccess("client_table") ||
                                       AccessControl.HasSectionAccess("dish_ingredients") ||
                                       AccessControl.HasSectionAccess("zakaz_items");

            // Аналогично для других кнопок, если они есть
            btnReports.IsEnabled = AccessControl.HasSectionAccess("zakaz") || AccessControl.HasSectionAccess("dish"); // Пример
        }

        private void UpdateRoleBadgeColor()
        {
            // Находим Border с ролью в визуальном дереве
            var roleBadge = FindRoleBadge();
            if (roleBadge != null)
            {
                switch (CurrentUser.RoleId)
                {
                    case 1: // Администратор
                        roleBadge.Background = new SolidColorBrush(Color.FromRgb(255, 107, 107)); // #FF6B6B
                        break;
                    case 2: // Менеджер
                        roleBadge.Background = new SolidColorBrush(Color.FromRgb(78, 205, 196)); // #4ECDC4
                        break;
                    case 3: // Кассир
                        roleBadge.Background = new SolidColorBrush(Color.FromRgb(253, 203, 110)); // #FDCB6E
                        break;
                    case 4: // Работник
                        roleBadge.Background = new SolidColorBrush(Color.FromRgb(0, 184, 148)); // #00B894
                        break;
                    default:
                        roleBadge.Background = new SolidColorBrush(Color.FromRgb(99, 110, 114)); // #636E72
                        break;
                }
            }
        }

        private Border FindRoleBadge()
        {
            // Ищем Border с ролью (можно упростить если дать имя в XAML)
            return FindChild<Border>(this, "roleBadge");
        }

        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is T result && (child as FrameworkElement)?.Name == childName)
                        return result;

                    var childOfChild = FindChild<T>(child, childName);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void LoadStatistics()
        {
            try
            {
                using (var conn = new MySql.Data.MySqlClient.MySqlConnection(
                    "server=localhost;port=3306;username=root;password=root;database=restaurant_db"))
                {
                    conn.Open();

                    var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                        "SELECT COUNT(*) FROM Category", conn);
                    txtStatDishes.Text = cmd.ExecuteScalar().ToString();

                    cmd.CommandText = "SELECT COUNT(*) FROM Client";
                    txtStatClients.Text = cmd.ExecuteScalar().ToString();

                    cmd.CommandText = "SELECT COUNT(*) FROM Client_Table";
                    txtStatTables.Text = cmd.ExecuteScalar().ToString();

                    cmd.CommandText = "SELECT COUNT(*) FROM Zakaz WHERE Status_Z = 'P'";
                    txtStatOrders.Text = cmd.ExecuteScalar().ToString();
                }
            }
            catch
            {
                txtStatDishes.Text = "42";
                txtStatOrders.Text = "18";
                txtStatTables.Text = "24";
                txtStatClients.Text = "156";
            }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            ShowMainContent();
            ShowStatus("Вы находитесь на главной странице", Colors.LimeGreen);
        }

        private void btnDirectories_Click(object sender, RoutedEventArgs e)
        {
            directoriesPanelVisible = !directoriesPanelVisible;
            directoriesPanel.Visibility = directoriesPanelVisible ?
                Visibility.Visible : Visibility.Collapsed;

            btnDirectories.Content = directoriesPanelVisible ? "▲ Справочники" : "▼ Справочники";

            if (!directoriesPanelVisible)
                ShowMainContent();
        }

        private void ShowMainContent()
        {
            mainContent.Visibility = Visibility.Visible;
            directoryContent.Visibility = Visibility.Collapsed;
        }

        private void ShowDirectoryContent()
        {
            mainContent.Visibility = Visibility.Collapsed;
            directoryContent.Visibility = Visibility.Visible;
        }

        // Справочники
        // Справочники
        private void btnRulesDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("rule"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new RuleControl();
            ShowStatus("Открыт справочник Роли пользователей", Colors.Orange);
        }

        private void btnDolhDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("dolh"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new DolhControl();
            ShowStatus("Открыт справочник Должности", Colors.Orange);
        }

        private void btnCategoryDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("category"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new CategoryControl();
            ShowStatus("Открыт справочник Категории блюд", Colors.Orange);
        }

        private void btnIngredientsDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("ingredients"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new IngredientsControl();
            ShowStatus("Открыт справочник Ингредиенты", Colors.Orange);
        }

        private void btnDishDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("dish"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new DishControl();
            ShowStatus("Открыт справочник Блюда", Colors.Orange);
        }

        private void btnDishIngredientsDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("dish_ingredients"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new DishIngredientsControl();
            ShowStatus("Открыт справочник Состав блюд", Colors.Orange);
        }

        private void btnPromotionsDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("promotions"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new PromotionsControl();
            ShowStatus("Открыт справочник Акции", Colors.Orange);
        }

        private void btnClientDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("client"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new ClientControl();
            ShowStatus("Открыт справочник Клиенты", Colors.Orange);
        }

        private void btnRabotnikDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("rabotnik"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new RabotnikControl();
            ShowStatus("Открыт справочник Работники", Colors.Orange);
        }

        private void btnTableDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("client_table"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new Client_TableControl();
            ShowStatus("Открыт справочник Столы", Colors.Orange);
        }

        private void btnZoneDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("zone"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new ZoneControl();
            ShowStatus("Открыт справочник Зоны", Colors.Orange);
        }

        private void btnZakazDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("zakaz"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new ZakazControl();
            ShowStatus("Открыт справочник Заказы", Colors.Orange);
        }

        private void btnZakazItemsDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("zakaz_items"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new ZakazItemsControl();
            ShowStatus("Открыт справочник Элементы заказов", Colors.Orange);
        }

        private void btnCheckDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("checkzakaz"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new CheckZakazControl();
            ShowStatus("Открыт справочник Чеки", Colors.Orange);
        }

        private void btnDeliveryDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("delivery"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new DeliveryControl();
            ShowStatus("Открыт справочник Доставка", Colors.Orange);
        }

        private void btnReservDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("reserv"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new ReservControl();
            ShowStatus("Открыт справочник Бронирования", Colors.Orange);
        }

        private void btnFeedbackDir_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.HasSectionAccess("feedback"))
            {
                MessageBox.Show("У вас нет доступа к этому разделу!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowDirectoryContent();
            directoryContentControl.Content = new FeedbackControl();
            ShowStatus("Открыт справочник Отзывы", Colors.Orange);
        }

        // Основные кнопки
        private void btnOrders_Click(object sender, RoutedEventArgs e)
        {
            ShowStatus("Раздел Заказы в разработке", Colors.Cyan);
            MessageBox.Show("Раздел Заказы в разработке", "Информация",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnReservations_Click(object sender, RoutedEventArgs e)
        {
            ShowStatus("Раздел Бронирование в разработке", Colors.Goldenrod);
            MessageBox.Show("Раздел Бронирование в разработке", "Информация",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnReports_Click(object sender, RoutedEventArgs e)
        {
            ReportsWindow reportsWindow = new ReportsWindow();
            reportsWindow.Show();
        }

        private void btnWordLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Docs",
                    "Руководство программиста.docx");

                if (System.IO.File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Файл не найден: " + filePath, "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThemeToggle_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ThemeManager.IsDarkTheme = !ThemeManager.IsDarkTheme;
            ThemeManager.ApplyTheme(Application.Current);
            
            // Анимация кружка переключателя
            var toggleCircle = FindName("ToggleCircle") as Border;
            if (toggleCircle != null)
            {
                var transform = toggleCircle.RenderTransform as TranslateTransform;
                if (transform != null)
                {
                    var animation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        To = ThemeManager.IsDarkTheme ? 100 : 0,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
                    };
                    transform.BeginAnimation(TranslateTransform.XProperty, animation);
                }
            }
            
            ShowStatus(ThemeManager.IsDarkTheme ? "Тёмная тема включена" : "Светлая тема включена", Colors.LimeGreen);
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Вы уверены, что хотите выйти, {CurrentUser.FullName}?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CurrentUser.Clear();
                ReturnToAuth();
            }
        }

        private void ReturnToAuth()
        {
            AuthWindow authWindow = new AuthWindow();
            authWindow.Show();
            this.Close();
        }

        private void ShowStatus(string message, Color color)
        {
            txtWelcome.Text = message;
            txtWelcome.Foreground = new SolidColorBrush(color);
        }
    }
}