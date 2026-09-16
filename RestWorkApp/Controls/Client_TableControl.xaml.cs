using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Для Brushes

namespace RestaurantWorkApp.Controls
{
    public partial class Client_TableControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public Client_TableControl()
        {
            InitializeComponent();
            Loaded += ClientTableControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void ClientTableControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Client_table")) // Предполагается, что в AccessControl есть проверка для "Client_table"
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgTables.IsReadOnly = true;
                dgTables.CanUserAddRows = false;
                dgTables.CanUserDeleteRows = false;
                txtNumber_T.IsEnabled = false;
                txtSeats.IsEnabled = false;
                cbStatus_T.IsEnabled = false;
                txtIdZone.IsEnabled = false;
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Client_table"))
            {
                MessageBox.Show("У вас нет прав для добавления стола!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string numberStr = txtNumber_T.Text.Trim();
            string seatsStr = txtSeats.Text.Trim();
            string status = cbStatus_T.SelectedItem?.ToString(); // получаем выбранный статус
            string zoneIdStr = txtIdZone.Text.Trim();

            if (string.IsNullOrEmpty(numberStr) || !int.TryParse(numberStr, out int number))
            {
                MessageBox.Show("Введите корректный номер стола (целое число) ");
                return;
            }
            if (string.IsNullOrEmpty(seatsStr) || !int.TryParse(seatsStr, out int seats))
            {
                MessageBox.Show("Введите корректное количество мест (целое число) ");
                return;
            }
            if (string.IsNullOrEmpty(status))
            {
                MessageBox.Show("Выберите статус стола ");
                return;
            }
            if (string.IsNullOrEmpty(zoneIdStr) || !int.TryParse(zoneIdStr, out int zoneId))
            {
                MessageBox.Show("Введите корректный ID зоны (целое число) ");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = @"INSERT INTO Client_Table (Number_T, Seats, Status_T, id_zone) 
                                     VALUES(@number, @seats, @status, @zoneId) ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@number ", number);
                    cmd.Parameters.AddWithValue("@seats ", seats);
                    cmd.Parameters.AddWithValue("@status ", status);
                    cmd.Parameters.AddWithValue("@zoneId ", zoneId);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    txtNumber_T.Clear();
                    txtSeats.Clear();
                    cbStatus_T.SelectedIndex = 1; // сброс на значение по умолчанию (F)
                    txtIdZone.Clear();

                    LoadData();
                    MessageBox.Show("Стол добавлен успешно! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении стола: {ex.Message} ");
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Client_table"))
            {
                MessageBox.Show("У вас нет прав для удаления стола!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgTables.SelectedItem == null)
            {
                MessageBox.Show("Выберите стол для удаления ");
                return;
            }

            DataRowView selectedRow = dgTables.SelectedItem as DataRowView;
            int id = Convert.ToInt32(selectedRow["id_Table "]);
            int number = Convert.ToInt32(selectedRow["Number_T "]);

            var result = MessageBox.Show($"Удалить стол №{number}? ",
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
                                string deleteQuery = "DELETE FROM Client_Table WHERE id_Table = @id ";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id ", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Table) as max_id FROM Client_Table";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Client_Table AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Стол удалён успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Стол не найден или не был удалён.");
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
                    MessageBox.Show($"Ошибка при удалении стола: {ex.Message} ");
                }
            }
        }

        private void dgTables_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Client_table"))
            {
                MessageBox.Show("У вас нет прав для редактирования стола!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                int id = Convert.ToInt32(rowView["id_Table "]);
                string columnHeader = (e.Column as DataGridColumn)?.Header.ToString();

                if (string.IsNullOrEmpty(columnHeader)) return;

                // Получаем новое  значение из самой строки (оно уже обновлено после редактирования)
                object newValueObj = null;
                string fieldName = " ";

                switch (columnHeader)
                {
                    case "Номер ":
                        fieldName = "Number_T ";
                        newValueObj = rowView[fieldName];
                        if (newValueObj == DBNull.Value || !int.TryParse(newValueObj.ToString(), out _))
                        {
                            MessageBox.Show("Номер стола должен быть целым числом ");
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case "Места ":
                        fieldName = "Seats ";
                        newValueObj = rowView[fieldName];
                        if (newValueObj == DBNull.Value || !int.TryParse(newValueObj.ToString(), out _))
                        {
                            MessageBox.Show("Количество мест должно быть целым числом ");
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case "Статус ":
                        fieldName = "Status_T ";
                        newValueObj = rowView[fieldName];
                        if (newValueObj == DBNull.Value || string.IsNullOrEmpty(newValueObj.ToString()))
                        {
                            MessageBox.Show("Статус не может быть пустым ");
                            e.Cancel = true;
                            return;
                        }
                        // Дополнительно можно проверить, что статус входит в допусти мый список,
                        // но это не обязательно, если используется ComboBoxColumn с ограниченным выбором.
                        break;
                    case "ID зоны ":
                        fieldName = "id_zone ";
                        newValueObj = rowView[fieldName];
                        if (newValueObj == DBNull.Value || !int.TryParse(newValueObj.ToString(), out _))
                        {
                            MessageBox.Show("ID зоны должен быть целым числом ");
                            e.Cancel = true;
                            return;
                        }
                        break;
                    default:
                        // Не ред актируемый столбец (например, ID) – игнорируем
                        return;
                }

                // Если значение не изменилось (можно не проверять, но для экономии запросов м ожно)
                // Здесь мы просто выполняем обновление, так как событие сработало при коммите.

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string query = $"UPDATE Client_Table SET {fieldName} = @value WHERE id_Table = @id ";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@value ", newValueObj);
                        cmd.Parameters.AddWithValue("@id ", id);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении: {ex.Message} ");
                    LoadData(); // перезагружаем данные в случае ошибки
                }
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Client_Table ORDER BY id_Table ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgTables.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message} ");
            }
        }
    }
}