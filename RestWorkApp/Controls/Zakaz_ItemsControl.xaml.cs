using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Для Brushes

namespace RestaurantWorkApp.Controls
{
    public partial class ZakazItemsControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public ZakazItemsControl()
        {
            InitializeComponent();
            Loaded += ZakazItemsControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void ZakazItemsControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Zakaz_items")) // Предполагается, что в AccessControl есть проверка для "Zakaz_items"
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgZakazItems.IsReadOnly = true;
                dgZakazItems.CanUserAddRows = false;
                dgZakazItems.CanUserDeleteRows = false;
                txtIdDish.IsEnabled = false;
                txtIdZakaz.IsEnabled = false;
                txtQuantity.IsEnabled = false;
                txtPriceAtTime.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Zakaz_Items ORDER BY id_zakaz DESC, id_Zakaz_Items ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgZakazItems.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message} ");
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Zakaz_items"))
            {
                MessageBox.Show("У вас нет прав для добавления позиции заказа!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string dishStr = txtIdDish.Text.Trim();
            string zakazStr = txtIdZakaz.Text.Trim();
            string qtyStr = txtQuantity.Text.Trim();
            string priceStr = txtPriceAtTime.Text.Trim();

            // Валидация
            if (!int.TryParse(dishStr, out int idDish)) { MessageBox.Show("Введите корректный ID блюда "); return; }
            if (!int.TryParse(zakazStr, out int idZakaz)) { MessageBox.Show("Введите корректный ID заказа "); return; }
            if (!int.TryParse(qtyStr, out int qty)) { MessageBox.Show("Введите корректное количество (целое число) "); return; }
            if (!decimal.TryParse(priceStr, out decimal price)) { MessageBox.Show("Введите корректную цену "); return; }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    // Вставка в Zakaz_Items
                    string query = "INSERT INTO Zakaz_Items (id_dish, id_zakaz, quantity, price_at_time) VALUES (@dish, @zakaz, @qty, @price) ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@dish ", idDish);
                    cmd.Parameters.AddWithValue("@zakaz ", idZakaz);
                    cmd.Parameters.AddWithValue("@qty ", qty);
                    cmd.Parameters.AddWithValue("@price ", price);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Очистка формы
                    txtIdDish.Clear();
                    txtIdZakaz.Clear();
                    txtQuantity.Clear();
                    txtPriceAtTime.Clear();

                    LoadData();
                    MessageBox.Show("Позиция заказа добавлена успешно! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message} ");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Zakaz_items"))
            {
                MessageBox.Show("У вас нет прав для удаления позиции заказа!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgZakazItems.SelectedItem == null)
            {
                MessageBox.Show("Выберите позицию для удаления ");
                return;
            }

            DataRowView row = dgZakazItems.SelectedItem as DataRowView;
            int id = Convert.ToInt32(row["id_Zakaz_Items "]);
            int idZakaz = Convert.ToInt32(row["id_zakaz "]);

            var result = MessageBox.Show($"Удалить позицию ID: {id}?\nВнимание: Сумма заказа (ID: {idZakaz}) пересчитается автоматически. ",
                                           "Подтверждение удаления ",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning);

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
                                string deleteQuery = "DELETE FROM Zakaz_Items WHERE id_Zakaz_Items = @id ";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id ", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Zakaz_Items) as max_id FROM Zakaz_Items";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Zakaz_Items AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Позиция удалена успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Позиция не найдена или не была удалена.");
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

        private void dgZakazItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Zakaz_items"))
            {
                MessageBox.Show("У вас нет прав для редактирования позиции заказа!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView row = e.Row.Item as DataRowView;
                if (row == null) return;

                int id = Convert.ToInt32(row["id_Zakaz_Items "]);
                string header = e.Column.Header as string;
                string newValue = (e.EditingElement as TextBox)?.Text.Trim();

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string query = " ";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id ", id);

                        switch (header)
                        {
                            case "ID Блюда ":
                                if (!int.TryParse(newValue, out int dish)) { MessageBox.Show("Неверный формат "); e.Cancel = true; return; }
                                query = "UPDATE Zakaz_Items SET id_dish = @val WHERE id_Zakaz_Items = @id ";
                                cmd.Parameters.AddWithValue("@val ", dish);
                                break;

                            case "ID Заказа ":
                                if (!int.TryParse(newValue, out int zakaz)) { MessageBox.Show("Неверный формат "); e.Cancel = true; return; }
                                query = "UPDATE Zakaz_Items SET id_zakaz = @val WHERE id_Zakaz_Items = @id ";
                                cmd.Parameters.AddWithValue("@val ", zakaz);
                                break;

                            case "Кол-во ":
                                if (!int.TryParse(newValue, out int qty)) { MessageBox.Show("Неверный формат "); e.Cancel = true; return; }
                                query = "UPDATE Zakaz_Items SET quantity = @val WHERE id_Zakaz_Items = @id ";
                                cmd.Parameters.AddWithValue("@val ", qty);
                                break;

                            case "Цена ":
                                if (!decimal.TryParse(newValue, out decimal price)) { MessageBox.Show("Неверный формат "); e.Cancel = true; return; }
                                query = "UPDATE Zakaz_Items SET price_at_time = @val WHERE id_Zakaz_Items = @id ";
                                cmd.Parameters.AddWithValue("@val ", price);
                                break;

                            default: return; // ID записи не редактируем
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