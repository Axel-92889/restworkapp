using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Для Brushes

namespace RestaurantWorkApp.Controls
{
    public partial class ReservControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public ReservControl()
        {
            InitializeComponent();
            Loaded += ReservControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void ReservControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Reserv")) // Предполагается, что в AccessControl есть проверка для "Reserv"
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgReserv.IsReadOnly = true;
                dgReserv.CanUserAddRows = false;
                dgReserv.CanUserDeleteRows = false;
                txtDataR.IsEnabled = false;
                txtGuests.IsEnabled = false;
                txtStatus.IsEnabled = false;
                txtClientId.IsEnabled = false;
                txtTableId.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Reserv ORDER BY Data_R DESC, id_Reserv ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgReserv.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message} ");
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Reserv"))
            {
                MessageBox.Show("У вас нет прав для добавления бронирования!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string dateStr = txtDataR.Text.Trim();
            string guestsStr = txtGuests.Text.Trim();
            string status = txtStatus.Text.Trim().ToUpper();
            string clientIdStr = txtClientId.Text.Trim();
            string tableIdStr = txtTableId.Text.Trim();

            // Валидация даты
            if (!DateTime.TryParse(dateStr, out DateTime dateR))
            {
                MessageBox.Show("Введите корректную дату (например, 2024-12-31) ");
                return;
            }

            // Валидация гостей
            if (!int.TryParse(guestsStr, out int guests) || guests < 1)
            {
                MessageBox.Show("Количество гостей должно быть больше 0 ");
                return;
            }

            // Валидация статуса
            if (string.IsNullOrEmpty(status) || status.Length != 1)
            {
                MessageBox.Show("Статус должен состоять из одного символа (A, C, P) ");
                return;
            }

            // Внешние ключи (nullable)
            int? idClient = null;
            if (!string.IsNullOrEmpty(clientIdStr))
            {
                if (!int.TryParse(clientIdStr, out int cId)) { MessageBox.Show("Неверный формат ID клиента "); return; }
                idClient = cId;
            }

            int? idTable = null;
            if (!string.IsNullOrEmpty(tableIdStr))
            {
                if (!int.TryParse(tableIdStr, out int tId)) { MessageBox.Show("Неверный формат ID стола "); return; }
                idTable = tId;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "INSERT INTO Reserv (Data_R, Number_Guests, Status_R, id_client, id_table) VALUES (@date, @guests, @status, @client, @table) ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@date ", dateR);
                    cmd.Parameters.AddWithValue("@guests ", guests);
                    cmd.Parameters.AddWithValue("@status ", status);
                    cmd.Parameters.AddWithValue("@client ", idClient.HasValue ? idClient.Value : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@table ", idTable.HasValue ? idTable.Value : (object)DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Очистка
                    txtDataR.Clear();
                    txtGuests.Clear();
                    txtStatus.Text = "A ";
                    txtClientId.Clear();
                    txtTableId.Clear();

                    LoadData();
                    MessageBox.Show("Бронирование создано успешно! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message} ");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Reserv"))
            {
                MessageBox.Show("У вас нет прав для удаления бронирования!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgReserv.SelectedItem == null)
            {
                MessageBox.Show("Выберите бронирование для удаления ");
                return;
            }

            DataRowView row = dgReserv.SelectedItem as DataRowView;
            int id = Convert.ToInt32(row["id_Reserv "]);

            var result = MessageBox.Show($"Удалить бронирование ID: {id}? ",
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
                                string deleteQuery = "DELETE FROM Reserv WHERE id_Reserv = @id ";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id ", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Reserv) as max_id FROM Reserv";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Reserv AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Бронирование удалено успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Бронирование не найдено или не было удалено.");
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

        private void dgReserv_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Reserv"))
            {
                MessageBox.Show("У вас нет прав для редактирования бронирования!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView row = e.Row.Item as DataRowView;
                if (row == null) return;

                int id = Convert.ToInt32(row["id_Reserv "]);
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
                            case "Дата ":
                                if (!DateTime.TryParse(newValue, out DateTime d)) { MessageBox.Show("Неверный формат даты "); e.Cancel = true; return; }
                                query = "UPDATE Reserv SET Data_R = @val WHERE id_Reserv = @id ";
                                cmd.Parameters.AddWithValue("@val ", d);
                                break;

                            case "Гостей ":
                                if (!int.TryParse(newValue, out int g) || g < 1) { MessageBox.Show("Гостей должно быть  >= 1 "); e.Cancel = true; return; }
                                query = "UPDATE Reserv SET Number_Guests = @val WHERE id_Reserv = @id ";
                                cmd.Parameters.AddWithValue("@val ", g);
                                break;

                            case "Статус ":
                                string st = newValue?.ToUpper();
                                if (string.IsNullOrEmpty(st) || st.Length != 1) { MessageBox.Show("Статус: 1 символ "); e.Cancel = true; return; }
                                query = "UPDATE Reserv SET Status_R = @val WHERE id_Reserv = @id ";
                                cmd.Parameters.AddWithValue("@val ", st);
                                break;

                            case "ID Клиента ":
                                if (string.IsNullOrEmpty(newValue)) query = "UPDATE Reserv SET id_client = NULL WHERE id_Reserv = @id ";
                                else if (int.TryParse(newValue, out int c)) { query = "UPDATE Reserv SET id_client = @val WHERE id_Reserv = @id "; cmd.Parameters.AddWithValue("@val ", c); }
                                else { MessageBox.Show("Неверный формат "); e.Cancel = true; return; }
                                break;

                            case "ID Стола ":
                                if (string.IsNullOrEmpty(newValue)) query = "UPDATE Reserv SET id_table = NULL WHERE id_Reserv = @id ";
                                else if (int.TryParse(newValue, out int t)) { query = "UPDATE Reserv SET id_table = @val WHERE id_Reserv = @id "; cmd.Parameters.AddWithValue("@val ", t); }
                                else { MessageBox.Show("Неверный формат "); e.Cancel = true; return; }
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