using DocumentFormat.OpenXml.Office2010.Excel;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Для Brushes

namespace RestaurantWorkApp.Controls
{
    public partial class DishIngredientsControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public DishIngredientsControl()
        {
            InitializeComponent();
            Loaded += DishIngredientsControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void DishIngredientsControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Dish_ingredients")) // Предполагается, что в AccessControl есть проверка для "Dish_ingredients"
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgDishIngredients.IsReadOnly = true;
                dgDishIngredients.CanUserAddRows = false;
                dgDishIngredients.CanUserDeleteRows = false;
                txtIngredientId.IsEnabled = false;
                txtDishId.IsEnabled = false;
                txtQuantity.IsEnabled = false;
                txtUnit.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    // Загружаем все поля таблицы
                    string query = "SELECT * FROM Dish_Ingredients ORDER BY id_Dish_Ingredients ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgDishIngredients.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message} ");
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Dish_ingredients"))
            {
                MessageBox.Show("У вас нет прав для добавления ингредиент блюда!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string txtIng = txtIngredientId.Text.Trim();
            string txtDish = txtDishId.Text.Trim();
            string txtQty = txtQuantity.Text.Trim();
            string txtUnt = txtUnit.Text.Trim();

            // Базовая валидация
            if (string.IsNullOrEmpty(txtIng) || string.IsNullOrEmpty(txtDish))
            {
                MessageBox.Show("ID ингредиента и ID блюда обязательны. ");
                return;
            }

            // Проверка форматов данных
            if (!int.TryParse(txtIng, out int idIngredients))
            {
                MessageBox.Show("ID ингредиента должно быть целым числом. ");
                return;
            }
            if (!int.TryParse(txtDish, out int idDish))
            {
                MessageBox.Show("ID блюда должно быть целым числом. ");
                return;
            }
            if (!decimal.TryParse(txtQty, out decimal qty) && !string.IsNullOrEmpty(txtQty))
            {
                MessageBox.Show("Количество должно быть числом. ");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "INSERT INTO Dish_Ingredients (id_ingredients, id_dish, quantity, unit) VALUES (@ingId, @dishId, @qty, @unit) ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ingId ", idIngredients);
                    cmd.Parameters.AddWithValue("@dishId ", idDish);
                    cmd.Parameters.AddWithValue("@qty ", string.IsNullOrEmpty(txtQty) ? (object)DBNull.Value : qty);
                    cmd.Parameters.AddWithValue("@unit ", string.IsNullOrEmpty(txtUnt) ? (object)DBNull.Value : txtUnt);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Очистка полей
                    txtIngredientId.Clear();
                    txtDishId.Clear();
                    txtQuantity.Clear();
                    txtUnit.Clear();

                    LoadData();
                    MessageBox.Show("Ингредиент добавлен успешно! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message} ");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Dish_ingredients"))
            {
                MessageBox.Show("У вас нет прав для удаления ингредиент блюда!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgDishIngredients.SelectedItem == null)
            {
                MessageBox.Show( "Выберите запись для удаления ");
                return;
            }

            DataRowView selectedRow = dgDishIngredients.SelectedItem as DataRowView;
            int id = Convert.ToInt32(selectedRow["id_Dish_Ingredients "]);

            var result = MessageBox.Show($"Удалить запись ID: {id}? ",
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
                                string deleteQuery = "DELETE FROM Dish_Ingredients WHERE id_Dish_Ingredients = @id ";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id ", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Dish_Ingredients) as max_id FROM Dish_Ingredients";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Dish_Ingredients AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Запись удалена успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Запись не найдена или не была удалена.");
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

        private void dgDishIngredients_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Dish_ingredients"))
            {
                MessageBox.Show("У вас нет прав для редактирования ингредиент блюда!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                // Получаем ID записи
                int id = Convert.ToInt32(rowView["id_Dish_Ingredients "]);
                string header = e.Column.Header as string;
                string newValue = (e.EditingElement as TextBox)?.Text.Trim();

                if (string.IsNullOrEmpty(header)) return;

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string query = " ";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id ", id);

                        // Определяем, какой столбец редактируется
                        switch (header)
                        {
                            case "ID Ингредиента ":
                                if (!int.TryParse(newValue, out int valIng))
                                {
                                    MessageBox.Show("Неверный формат ID ");
                                    e.Cancel = true;
                                    return;
                                }
                                query = "UPDATE Dish_Ingredients SET id_ingredients = @val WHERE id_Dish_Ingredients = @id ";
                                cmd.Parameters.AddWithValue("@val ", valIng);
                                break;

                            case "ID Блюда ":
                                if (!int.TryParse(newValue, out int valDish))
                                {
                                    MessageBox.Show("Неверный формат ID ");
                                    e.Cancel = true;
                                    return;
                                }
                                query = "UPDATE Dish_Ingredients SET id_dish = @val WHERE id_Dish_Ingredients = @id ";
                                cmd.Parameters.AddWithValue("@val ", valDish);
                                break;

                            case "Количество ":
                                decimal valQty = 0;
                                // Разрешаем пустое значение (NULL), если нужно, или требуем число
                                if (string.IsNullOrEmpty(newValue))
                                {
                                    query = "UPDATE Dish_Ingredients SET quantity = NULL WHERE id_Dish_Ingredients = @id ";
                                }
                                else if (decimal.TryParse(newValue, out valQty))
                                {
                                    query = "UPDATE Dish_Ingredients SET quantity = @val WHERE id_Dish_Ingredients = @id ";
                                    cmd.Parameters.AddWithValue("@val ", valQty);
                                }
                                else
                                {
                                    MessageBox.Show("Количество должно быть числом ");
                                    e.Cancel = true;
                                    return;
                                }
                                break;

                            case "Ед. изм. ":
                                query = "UPDATE Dish_Ingredients SET unit = @val WHERE id_Dish_Ingredients = @id ";
                                cmd.Parameters.AddWithValue("@val ", string.IsNullOrEmpty(newValue) ? (object)DBNull.Value : newValue);
                                break;

                            default:
                                return; // ID  записи не редактируем
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
                    LoadData(); // Перезагружаем данные, чтобы отменить неверное изменение
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}