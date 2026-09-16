using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RestaurantWorkApp
{
    public partial class ProgressWindow : Window
    {
        private DispatcherTimer progressTimer;

        public ProgressWindow()
        {
            InitializeComponent();

            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromMilliseconds(15);
            progressTimer.Tick += ProgressTimer_Tick;

            // Запускаем прогресс
            progressTimer.Start();
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            // Увеличиваем прогресс
            progressBar.Value += 1;
            txtPercent.Text = $"{progressBar.Value}%";

            // Обновляем статус
            UpdateStatus();

            // Проверяем завершение
            if (progressBar.Value >= progressBar.Maximum)
            {
                progressTimer.Stop();

                Task.Delay(400).ContinueWith(t =>
                {
                    // Возвращаемся в UI поток для открытия окна
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OpenAuthWindow();
                    });
                });
            }
        }

        private void UpdateStatus()
        { 
            if (progressBar.Value < 20)
                txtStatus.Text = "Инициализация ядра системы...";
            else if (progressBar.Value < 40)
                txtStatus.Text = "Подключение к базе данных...";
            else if (progressBar.Value < 60)
                txtStatus.Text = "Загрузка меню и справочников...";
            else if (progressBar.Value < 80)
                txtStatus.Text = "Инициализация модулей...";
            else if (progressBar.Value < 95)
                txtStatus.Text = "Подготовка интерфейса...";
            else
                txtStatus.Text = "Система готова!";
        }

        private void OpenAuthWindow()
        {
            // Создаем окно авторизации
            AuthWindow authWindow = new AuthWindow();

            // Показываем окно авторизации
            authWindow.Show();

            // Закрываем текущее окно
            this.Close();
        }
    }
}