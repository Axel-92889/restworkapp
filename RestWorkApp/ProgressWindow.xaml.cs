using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace RestaurantWorkApp
{
    public partial class ProgressWindow : Window
    {
        private DispatcherTimer progressTimer;
        private int _currentProgress = 0;
        private readonly object _progressLock = new object();

        public ProgressWindow()
        {
            InitializeComponent();
            
            // Принудительно обновляем layout перед запуском
            this.LayoutUpdated += (s, e) => { };
            
            progressTimer = new DispatcherTimer();
            // Увеличенный интервал для более плавной работы на мощных системах
            progressTimer.Interval = TimeSpan.FromMilliseconds(30);
            progressTimer.Tick += ProgressTimer_Tick;

            // Запускаем прогресс
            progressTimer.Start();
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            lock (_progressLock)
            {
                // Увеличиваем прогресс с адаптивной скоростью
                int increment = 1;
                if (_currentProgress < 10) increment = 2;
                else if (_currentProgress < 30) increment = 1;
                else if (_currentProgress < 60) increment = 2;
                else if (_currentProgress < 80) increment = 1;
                else increment = 1;

                _currentProgress += increment;
                
                if (_currentProgress > 100) _currentProgress = 100;

                // Обновляем UI через Dispatcher
                Application.Current.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = _currentProgress;
                    txtPercent.Text = $"{_currentProgress}%";
                    UpdateStatus();
                });

                // Проверяем завершение
                if (_currentProgress >= 100)
                {
                    progressTimer.Stop();

                    Task.Delay(300).ContinueWith(t =>
                    {
                        // Возвращаемся в UI поток для открытия окна
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            OpenAuthWindow();
                        });
                    });
                }
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