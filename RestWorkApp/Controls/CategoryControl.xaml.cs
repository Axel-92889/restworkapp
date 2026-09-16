using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RestaurantWorkApp;

namespace RestaurantWorkApp.Controls
{
    public partial class CategoryControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public CategoryControl()
        {
            InitializeComponent();

            // Проверяем права доступа при загрузке
            Loaded += CategoryControl_Loaded;
        }

        private void CategoryControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем данные
            LoadData();

            // Проверяем права на редактирование
            if (!AccessControl.CanDoOperation("Category"))
            {
                // Отключаем возможность редактирования
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;

                // Делаем DataGrid только для чтения
                dgCategories.IsReadOnly = true;

                // Отключаем редактирование ячеек
                dgCategories.CanUserAddRows = false;
                dgCategories.CanUserDeleteRows = false;

                // Отключаем редактирование текстбокса
                txtCategoryName.IsEnabled = false;

                // Меняем стиль для визуального указания на ограничение
                txtCategoryName.ToolTip = "У вас нет прав на добавление новых записей";
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем права перед выполнением операции
            if (!AccessControl.CanDoOperation("Category"))
            {
                MessageBox.Show("У вас нет прав для добавления новой категории!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string categoryName = txtCategoryName.Text.Trim();

            if (string.IsNullOrEmpty(categoryName))
            {
                MessageBox.Show("Введите название категории");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "INSERT INTO Category (Name_C) VALUES (@name)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@name", categoryName);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    txtCategoryName.Clear();
                    LoadData(); // Перезагружаем данные после добавления
                    MessageBox.Show("Категория добавлена успешно!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении категории: {ex.Message}");
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void txtCategoryName_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            string value = tb?.Text.Trim();

            // Проверяем, есть ли право на редактирование перед валидацией
            if (!AccessControl.CanDoOperation("Category"))
            {
                tb.BorderBrush = Brushes.Gray;
                return;
            }

            bool isValid = !string.IsNullOrEmpty(value) &&
                           value.Length >= 2 &&
                           value.Length <= 50 &&
                           System.Text.RegularExpressions.Regex.IsMatch(value, @"^[A-Za-zА-Яа-яЁё0-9\s\-]+$");

            if (string.IsNullOrEmpty(value))
            {
                tb.BorderBrush = Brushes.LightGray;
            }
            else if (isValid)
            {
                tb.BorderBrush = Brushes.Green;
            }
            else
            {
                tb.BorderBrush = Brushes.Red;
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем права перед выполнением операции
            if (!AccessControl.CanDoOperation("Category"))
            {
                MessageBox.Show("У вас нет прав для удаления категории!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgCategories.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию для удаления");
                return;
            }

            DataRowView selectedRow = dgCategories.SelectedItem as DataRowView;
            int id = Convert.ToInt32(selectedRow["id_Category"]);
            string name = selectedRow["Name_C"].ToString();

            var result = MessageBox.Show($"Удалить категорию '{name}'?",
                                         "Подтверждение удаления",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                // 1. Удаление записи
                                string deleteQuery = "DELETE FROM Category WHERE id_Category = @id";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    // Получаем максимальный ID из оставшихся записей
                                    string getMaxIdQuery = "SELECT MAX(id_Category) as max_id FROM Category";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1; // Значение по умолчанию, если таблица пуста
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    // Устанавливаем новый AUTO_INCREMENT
                                    string alterQuery = "ALTER TABLE Category AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit(); // Фиксируем транзакцию
                                    LoadData(); // Обновляем UI
                                    MessageBox.Show("Категория удалена успешно!");
                                }
                                else
                                {
                                    transaction.Rollback(); // Откатываем, если запись не была удалена
                                    MessageBox.Show("Ошибка: Категория не найдена или не была удалена.");
                                }
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback(); // Откатываем при любой ошибке
                                throw ex; // Перебрасываем исключение для обработки в outer catch
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении категории: {ex.Message}");
                }
            }
        }


        private void dgCategories_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Проверяем права перед выполнением операции
            if (!AccessControl.CanDoOperation("Category"))
            {
                MessageBox.Show("У вас нет прав для редактирования категории!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                int id = Convert.ToInt32(rowView["id_Category"]);
                string newName = (e.EditingElement as TextBox)?.Text.Trim();

                if (string.IsNullOrEmpty(newName))
                {
                    MessageBox.Show("Название категории не может быть пустым");
                    e.Cancel = true;
                    return;
                }

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string query = "UPDATE Category SET Name_C = @name WHERE id_Category = @id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@name", newName);
                        cmd.Parameters.AddWithValue("@id", id);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении: {ex.Message}");
                    LoadData(); // Перезагружаем данные в случае ошибки
                }
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Category ORDER BY id_Category";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgCategories.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }
    }
}