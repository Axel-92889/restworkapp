using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RestaurantWorkApp.Controls
{
    public partial class DishControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public DishControl()
        {
            InitializeComponent();
            Loaded += DishControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void DishControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Dish"))
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgDishes.IsReadOnly = true;
                dgDishes.CanUserAddRows = false;
                dgDishes.CanUserDeleteRows = false;
                txtNameDish.IsEnabled = false;
                txtPrice.IsEnabled = false;
                txtStatus.IsEnabled = false;
                txtImageURL.IsEnabled = false;
                txtCategoryId.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    // JOIN с Category для отображения названия категории (опционально)
                    string query = @"
                        SELECT d.id_Dish, d.Name_Dish, d.Price_D, d.Status_D, d.Image_URL, d.id_category, c.Name_C as Category_Name
                        FROM Dish d
                        LEFT JOIN Category c ON d.id_category = c.id_Category
                        ORDER BY d.id_Dish ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgDishes.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message} ");
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Dish"))
            {
                MessageBox.Show("У вас нет прав для добавления блюда!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nameDish = txtNameDish.Text.Trim();
            string priceStr = txtPrice.Text.Trim();
            string status = txtStatus.Text.Trim();
            string imageURL = txtImageURL.Text.Trim();
            string catIdStr = txtCategoryId.Text.Trim();

            // Валидация обязательных полей
            if (string.IsNullOrEmpty(nameDish))
            {
                MessageBox.Show("Введите название блюда ");
                return;
            }
            if (!decimal.TryParse(priceStr, out decimal price))
            {
                MessageBox.Show("Введите корректную цену ");
                return;
            }
            if (!int.TryParse(catIdStr, out int categoryId))
            {
                MessageBox.Show("Введите корректный ID категории ");
                return;
            }

            // Статус по умолчанию
            if (string.IsNullOrEmpty(status)) status = "A";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "INSERT INTO Dish (Name_Dish, Price_D, Status_D, Image_URL, id_category) VALUES (@name, @price, @status, @img, @catId)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@name", nameDish);
                    cmd.Parameters.AddWithValue("@price", price);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@img", string.IsNullOrEmpty(imageURL) ? (object)DBNull.Value : imageURL);
                    cmd.Parameters.AddWithValue("@catId", categoryId);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Очистка полей
                    txtNameDish.Clear();
                    txtPrice.Clear();
                    txtStatus.Text = "A";
                    txtImageURL.Clear();
                    txtCategoryId.Clear();

                    LoadData();
                    MessageBox.Show("Блюдо добавлено успешно! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message} ");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Dish"))
            {
                MessageBox.Show("У вас нет прав для удаления блюда!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgDishes.SelectedItem == null)
            {
                MessageBox.Show("Выберите блюдо для удаления ");
                return;
            }

            DataRowView selectedRow = dgDishes.SelectedItem as DataRowView;
            int id = Convert.ToInt32(selectedRow["id_Dish"]);
            string name = selectedRow["Name_Dish"].ToString();

            var result = MessageBox.Show($"Удалить блюдо '{name}'? ",
                                           "Подтверждение удаления ",
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
                                string deleteQuery = "DELETE FROM Dish WHERE id_Dish = @id";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Dish) as max_id FROM Dish";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Dish AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Блюдо удалено успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Блюдо не найдено или не было удалено.");
                                }
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message} ");
                }
            }
        }

        private void dgDishes_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Dish"))
            {
                MessageBox.Show("У вас нет прав для редактирования блюда!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                int id = Convert.ToInt32(rowView["id_Dish"]);
                string header = e.Column.Header as string;
                string newValue = (e.EditingElement as TextBox)?.Text.Trim();

                if (string.IsNullOrEmpty(header)) return;

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string query = " ";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", id);

                        switch (header)
                        {
                            case "Название":
                                if (string.IsNullOrEmpty(newValue))
                                {
                                    MessageBox.Show("Название не может быть пустым ");
                                    e.Cancel = true;
                                    return;
                                }
                                query = "UPDATE Dish SET Name_Dish = @val WHERE id_Dish = @id";
                                cmd.Parameters.AddWithValue("@val", newValue);
                                break;

                            case "Цена":
                                if (!decimal.TryParse(newValue, out decimal price))
                                {
                                    MessageBox.Show("Неверный формат цены ");
                                    e.Cancel = true;
                                    return;
                                }
                                query = "UPDATE Dish SET Price_D = @val WHERE id_Dish = @id";
                                cmd.Parameters.AddWithValue("@val", price);
                                break;

                            case "Статус":
                                if (!string.IsNullOrEmpty(newValue) && newValue != "A" && newValue != "D")
                                {
                                    MessageBox.Show("Статус должен быть 'A' или 'D'");
                                    e.Cancel = true;
                                    return;
                                }
                                query = "UPDATE Dish SET Status_D = @val WHERE id_Dish = @id";
                                cmd.Parameters.AddWithValue("@val", string.IsNullOrEmpty(newValue) ? "A" : newValue);
                                break;

                            case "Изображение":
                                query = "UPDATE Dish SET Image_URL = @val WHERE id_Dish = @id";
                                cmd.Parameters.AddWithValue("@val", string.IsNullOrEmpty(newValue) ? (object)DBNull.Value : newValue);
                                break;

                            case "ID категории":
                                if (!int.TryParse(newValue, out int catId))
                                {
                                    MessageBox.Show("Неверный формат ID категории ");
                                    e.Cancel = true;
                                    return;
                                }
                                query = "UPDATE Dish SET id_category = @val WHERE id_Dish = @id";
                                cmd.Parameters.AddWithValue("@val", catId);
                                break;

                            default:
                                return; // ID не редактируем
                        }

                        if (!string.IsNullOrEmpty(query))
                        {
                            cmd.CommandText = query;
                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении: {ex.Message} ");
                    LoadData();
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}