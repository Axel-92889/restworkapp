using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Для Brushes

namespace RestaurantWorkApp.Controls
{
    public partial class ZakazControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public ZakazControl()
        {
            InitializeComponent();
            Loaded += ZakazControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void ZakazControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Zakaz")) // Предполагается, что в AccessControl есть проверка для "Zakaz"
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgZakaz.IsReadOnly = true;
                dgZakaz.CanUserAddRows = false;
                dgZakaz.CanUserDeleteRows = false;
                txtDataZ.IsEnabled = false;
                txtStartTime.IsEnabled = false;
                txtEndTime.IsEnabled = false;
                txtStatusZ.IsEnabled = false;
                txtClientId.IsEnabled = false;
                txtRabotnikId.IsEnabled = false;
                txtPromoId.IsEnabled = false;
                txtPriceZ.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Zakaz ORDER BY id_Zakaz DESC ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgZakaz.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message} ");
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Zakaz"))
            {
                MessageBox.Show("У вас нет прав для добавления заказа!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string dataZ = txtDataZ.Text.Trim();
            string startT = txtStartTime.Text.Trim();
            string endT = txtEndTime.Text.Trim();
            string status = txtStatusZ.Text.Trim().ToUpper();
            string cliId = txtClientId.Text.Trim();
            string rabId = txtRabotnikId.Text.Trim();
            string promoId = txtPromoId.Text.Trim();
            string priceStr = txtPriceZ.Text.Trim();

            // Валидация обязательных полей
            if (string.IsNullOrEmpty(dataZ)) { MessageBox.Show("Укажите дату и время заказа "); return; }
            if (!DateTime.TryParse(dataZ, out DateTime dtZ)) { MessageBox.Show("Неверный формат даты заказа "); return; }

            if (!int.TryParse(cliId, out int idClient)) { MessageBox.Show("Введите корректный ID клиента "); return; }
            if (!int.TryParse(rabId, out int idRab)) { MessageBox.Show("Введите корректный ID сотрудника "); return; }

            if (status != "N " && status != "P " && status != "C ") { MessageBox.Show("Статус должен быть: N (Новый), P (В работе), C (Завершен) "); return; }

            // Опциональные поля
            DateTime? start = null;
            if (!string.IsNullOrEmpty(startT)) { if (!DateTime.TryParse(startT, out DateTime s)) { MessageBox.Show("Неверный формат времени начала "); return; } start = s; }

            DateTime? end = null;
            if (!string.IsNullOrEmpty(endT)) { if (!DateTime.TryParse(endT, out DateTime en)) { MessageBox.Show("Неверный формат времени окончания "); return; } end = en; }

            int? idPromo = null;
            if (!string.IsNullOrEmpty(promoId)) { if (!int.TryParse(promoId, out int p)) { MessageBox.Show("Неверный формат ID акции "); return; } idPromo = p; }

            decimal price = 0.00m;
            if (!string.IsNullOrEmpty(priceStr)) { if (!decimal.TryParse(priceStr, out price)) { MessageBox.Show("Неверный формат суммы "); return; } }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = @"INSERT INTO Zakaz (Data_Z, Start_Time, End_Time, Status_Z, id_client, id_rabotnik, id_promotion, Price_Z)  VALUES(@dataZ, @start, @end, @status, @cli, @rab, @promo, @price) ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@dataZ ", dtZ);
                    cmd.Parameters.AddWithValue("@start ", start.HasValue ? start.Value : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@end ", end.HasValue ? end.Value : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@status ", status);
                    cmd.Parameters.AddWithValue("@cli ", idClient);
                    cmd.Parameters.AddWithValue("@rab ", idRab);
                    cmd.Parameters.AddWithValue("@promo ", idPromo.HasValue ? idPromo.Value : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@price ", price);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Очистка
                    txtDataZ.Clear();
                    txtStartTime.Clear();
                    txtEndTime.Clear();
                    txtStatusZ.Text = "N ";
                    txtClientId.Clear();
                    txtRabotnikId.Clear();
                    txtPromoId.Clear();
                    txtPriceZ.Text = "0.00 ";

                    LoadData();
                    MessageBox.Show("Заказ успешно создан! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении заказа: {ex.Message} ");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Zakaz"))
            {
                MessageBox.Show("У вас нет прав для удаления заказа!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgZakaz.SelectedItem == null)
            {
                MessageBox.Show("Выберите заказ для удаления ");
                return;
            }

            DataRowView selectedRow = dgZakaz.SelectedItem as DataRowView;
            int id = Convert.ToInt32(selectedRow["id_Zakaz "]);

            var result = MessageBox.Show($"Удалить заказ ID: {id}?\nВнимание: удалятся все позиции заказа и связанные чеки. ",
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
                                string deleteQuery = "DELETE FROM Zakaz WHERE id_Zakaz = @id ";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id ", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Zakaz) as max_id FROM Zakaz";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Zakaz AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Заказ удалён успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Заказ не найден или не был удалён.");
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

        private void dgZakaz_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Zakaz"))
            {
                MessageBox.Show("У вас нет прав для редактирования заказа!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                int id = Convert.ToInt32(rowView["id_Zakaz "]);
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

                        switch (header)
                        {
                            case "Дата заказа ":
                                if (!DateTime.TryParse(newValue, out DateTime val)) { MessageBox.Show("Неверный формат даты "); e.Cancel = true; return; }
                                query = "UPDATE Zakaz SET Data_Z = @val WHERE id_Zakaz = @id ";
                                cmd.Parameters.AddWithValue("@val ", val);
                                break;

                            case "Начало ":
                                if (string.IsNullOrEmpty(newValue)) query = "UPDATE Zakaz SET Start_Time = NULL WHERE id_Zakaz = @id ";
                                else if (DateTime.TryParse(newValue, out DateTime s)) { query = "UPDATE Zakaz SET Start_Time = @val WHERE id_Zakaz = @id "; cmd.Parameters.AddWithValue("@val ", s); }
                                else { MessageBox.Show("Неверный формат времени "); e.Cancel = true; return; }
                                break;

                            case "Окончание ":
                                if (string.IsNullOrEmpty(newValue)) query = "UPDATE Zakaz SET End_Time = NULL WHERE id_Zakaz = @id ";
                                else if (DateTime.TryParse(newValue, out DateTime en)) { query = "UPDATE Zakaz SET End_Time = @val WHERE id_Zakaz = @id "; cmd.Parameters.AddWithValue("@val ", en); }
                                else { MessageBox.Show("Неверный формат времени "); e.Cancel = true; return; }
                                break;

                            case "Статус ":
                                string st = newValue?.ToUpper();
                                if (st != "N " && st != "P " && st != "C ") { MessageBox.Show("Статус: N, P или C "); e.Cancel = true; return; }
                                query = "UPDATE Zakaz SET Status_Z = @val WHERE id_Zakaz = @id ";
                                cmd.Parameters.AddWithValue("@val ", st);
                                break;

                            case "Клиент ":
                                if (!int.TryParse(newValue, out int cli)) { MessageBox.Show("Неверный ID клиента "); e.Cancel = true; return; }
                                query = "UPDATE Zakaz SET id_client = @val WHERE id_Zakaz = @id ";
                                cmd.Parameters.AddWithValue("@val ", cli);
                                break;

                            case "Сотрудник ":
                                if (!int.TryParse(newValue, out int rab)) { MessageBox.Show("Неверный ID сотрудника "); e.Cancel = true; return; }
                                query = "UPDATE Zakaz SET id_rabotnik = @val WHERE id_Zakaz = @id ";
                                cmd.Parameters.AddWithValue("@val ", rab);
                                break;

                            case "Акция ":
                                if (string.IsNullOrEmpty(newValue)) query = "UPDATE Zakaz SET id_promotion = NULL WHERE id_Zakaz = @id ";
                                else if (int.TryParse(newValue, out int pro)) { query = "UPDATE Zakaz SET id_promotion = @val WHERE id_Zakaz = @id "; cmd.Parameters.AddWithValue("@val ", pro); }
                                else { MessageBox.Show("Неверный ID акции "); e.Cancel = true; return; }
                                break;

                            case "Сумма ":
                                if (!decimal.TryParse(newValue, out decimal pr)) { MessageBox.Show("Неверная сумма "); e.Cancel = true; return; }
                                query = "UPDATE Zakaz SET Price_Z = @val WHERE id_Zakaz = @id ";
                                cmd.Parameters.AddWithValue("@val ", pr);
                                break;

                            default: return; // ID не редактируем
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