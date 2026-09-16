using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Для Brushes

namespace RestaurantWorkApp.Controls
{
    public partial class DeliveryControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";

        public DeliveryControl()
        {
            InitializeComponent();
            Loaded += DeliveryControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void DeliveryControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();

            if (!AccessControl.CanDoOperation("Delivery")) // Предполагается, что в AccessControl есть проверка для "Delivery"
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgDeliveries.IsReadOnly = true;
                dgDeliveries.CanUserAddRows = false;
                dgDeliveries.CanUserDeleteRows = false;
                txtAddress.IsEnabled = false;
                dpDeliveryDate.IsEnabled = false;
                txtDeliveryTime.IsEnabled = false;
                cbStatusDelivery.IsEnabled = false;
                txtIdZakaz.IsEnabled = false;
                txtIdRabotnik.IsEnabled = false;
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Delivery"))
            {
                MessageBox.Show("У вас нет прав для добавления доставки!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string address = txtAddress.Text.Trim();
            DateTime? deliveryDate = dpDeliveryDate.SelectedDate;
            string timeStr = txtDeliveryTime.Text.Trim();
            string status = cbStatusDelivery.SelectedItem?.ToString(); // Получаем выбранный статус
            string idZakazStr = txtIdZakaz.Text.Trim();
            string idRabotnikStr = txtIdRabotnik.Text.Trim();

            if (string.IsNullOrEmpty(address))
            {
                MessageBox.Show("Введите адрес доставки ");
                return;
            }
            if (deliveryDate == null)
            {
                MessageBox.Show("Выберите дату доставки ");
                return;
            }
            if (string.IsNullOrEmpty(timeStr))
            {
                MessageBox.Show("Введите время доставки ");
                return;
            }
            if (!DateTime.TryParse($"{deliveryDate.Value:yyyy-MM-dd} {timeStr} ", out DateTime deliveryTime))
            {
                MessageBox.Show("Некорректный формат времени. Используйте ЧЧ:ММ ");
                return;
            }
            if (string.IsNullOrEmpty(status))
            {
                MessageBox.Show("Выберите статус доставки ");
                return;
            }
            if (string.IsNullOrEmpty(idZakazStr) || !int.TryParse(idZakazStr, out int idZakaz))
            {
                MessageBox.Show("Введите корректный ID заказа (целое число) ");
                return;
            }
            // idRabotnik может быть NULL, поэтому необязательное поле
            int? idRabotnik = null;
            if (!string.IsNullOrEmpty(idRabotnikStr))
            {
                if (!int.TryParse(idRabotnikStr, out int tmp))
                {
                    MessageBox.Show("ID работника должно быть целым числом ");
                    return;
                }
                idRabotnik = tmp;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = @"INSERT INTO Delivery (Address, Delivery_Time, Status_Delivery, id_zakaz, id_rabotnik) 
                                     VALUES(@address, @deliveryTime, @status, @idZakaz, @idRabotnik) ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@address ", address);
                    cmd.Parameters.AddWithValue("@deliveryTime ", deliveryTime);
                    cmd.Parameters.AddWithValue("@status ", status);
                    cmd.Parameters.AddWithValue("@idZakaz ", idZakaz);
                    cmd.Parameters.AddWithValue("@idRabotnik ", idRabotnik.HasValue ? (object)idRabotnik.Value : DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    // Очистка полей
                    txtAddress.Clear();
                    dpDeliveryDate.Text = " ";
                    txtDeliveryTime.Text = "12:00 ";
                    cbStatusDelivery.SelectedIndex = 0; // Сброс на первый элемент
                    txtIdZakaz.Clear();
                    txtIdRabotnik.Clear();

                    LoadData();
                    MessageBox.Show("Доставка добавлена успешно! ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении доставки: {ex.Message} ");
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Delivery"))
            {
                MessageBox.Show("У вас нет прав для удаления доставки!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgDeliveries.SelectedItem == null)
            {
                MessageBox.Show("Выберите доставку для удаления ");
                return;
            }

            DataRowView selectedRow = dgDeliveries.SelectedItem as DataRowView;
            int id = Convert.ToInt32(selectedRow["id_Delivery "]);
            string address = selectedRow["Address "].ToString();

            var result = MessageBox.Show($"Удалить доставку по адресу '{address}'? ",
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
                                string deleteQuery = "DELETE FROM Delivery WHERE id_Delivery = @id ";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id ", id);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Delivery) as max_id FROM Delivery";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Delivery AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Доставка удалена успешно! ");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Доставка не найдена или не была удалена.");
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
                    MessageBox.Show($"Ошибка при удалении доставки: {ex.Message} ");
                }
            }
        }

        private void dgDeliveries_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Delivery"))
            {
                MessageBox.Show("У вас нет прав для редактирования доставки!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                int id = Convert.ToInt32(rowView["id_Delivery "]);
                string columnHeader = (e.Column as DataGridColumn)?.Header.ToString();

                if (string.IsNullOrEmpty(columnHeader)) return;

                // Для ComboBoxCol umn значение уже обновлено в строке, получаем его из rowView
                object newValueObj = null;
                string fieldName = " ";

                switch (columnHeader)
                {
                    case "Адрес ":
                        fieldName = "Address ";
                        newValueObj = rowView[fieldName];
                        if (newValueObj == DBNull.Value || string.IsNullOrEmpty(newValueObj.ToString()))
                        {
                            MessageBox.Show("Адрес не может быть пустым ");
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case "Время доставки ":
                        fieldName = "Delivery_Time ";
                        newValueObj = rowView[fieldName];
                        if (newValueObj == DBNull.Value)
                        {
                            MessageBox.Show("Время доставки не может быть пустым ");
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case "Статус ":
                        fieldName = "Status_Delivery ";
                        newValueObj = rowView[fieldName];
                        if (newValueObj == DBNull.Value || string.IsNullOrEmpty(newValueObj.ToString()))
                        {
                            MessageBox.Show("Статус не может быть пустым ");
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case "ID заказа ":
                        fieldName = "id_zakaz ";
                        newValueObj = rowView[fieldName];
                        if (newValueObj == DBNull.Value || !int.TryParse(newValueObj.ToString(), out _))
                        {
                            MessageBox.Show("ID заказа должен быть целым числом ");
                            e.Cancel = true;
                            return;
                        }
                        break;
                    case "ID работника ":
                        fieldName = "id_rabotnik ";
                        newValueObj = rowView[fieldName];
                        // Может быть NULL, допустимо
                        break;
                    default:
                        return;
                }

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        string query = $"UPDATE Delivery SET {fieldName} = @value WHERE id_Delivery = @id ";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@value ", newValueObj ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@id ", id);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении: {ex.Message} ");
                    LoadData();
                }
            }
        }

        private void LoadData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string query = "SELECT * FROM Delivery ORDER BY id_Delivery ";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgDeliveries.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message} ");
            }
        }
    }
}