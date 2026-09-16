using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace RestaurantWorkApp
{
    public partial class DolhControl : UserControl
    {
        private string connectionString = "server=localhost;port=3306;username=root;password=root;database=restaurant_db";
        private DataTable dolhTable;
        private List<RoleItem> rolesList;

        public DolhControl()
        {
            InitializeComponent();
            Loaded += DolhControl_Loaded; // Проверка прав доступа при загрузке
        }

        private void DolhControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRoles();
            LoadData();

            if (!AccessControl.CanDoOperation("Dolh"))
            {
                btnAdd.IsEnabled = false;
                btnDelete.IsEnabled = false;
                dgDolh.IsReadOnly = true;
                dgDolh.CanUserAddRows = false;
                dgDolh.CanUserDeleteRows = false;
                txtDolhName.IsEnabled = false;
                cmbRole.IsEnabled = false;
                txtDolhName.ToolTip = "У вас нет прав на редактирование";
                cmbRole.ToolTip = "У вас нет прав на редактирование";
            }
        }

        // Класс для хранения информации о роли (используется в ComboBox)
        public class RoleItem
        {
            public int id_Rule { get; set; }
            public string Name_R { get; set; }
        }

        // Загрузка списка ролей для ComboBox
        private void LoadRoles()
        {
            rolesList = new List<RoleItem>();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = "SELECT id_Rule, Name_R FROM Rule ORDER BY Name_R";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    connection.Open();
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        rolesList.Add(new RoleItem
                        {
                            id_Rule = reader.GetInt32("id_Rule"),
                            Name_R = reader.GetString("Name_R")
                        });
                    }
                }

                cmbRole.ItemsSource = rolesList;
                cmbRole.DisplayMemberPath = "Name_R";
                cmbRole.SelectedValuePath = "id_Rule";

                this.Resources.Add("Roles", rolesList);
                this.DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}");
            }
        }

        // Свойство для доступа к ролям в XAML (через DataContext)
        public List<RoleItem> Roles => rolesList;

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Dolh"))
            {
                MessageBox.Show("У вас нет прав для добавления должности!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string dolhName = txtDolhName.Text.Trim();
            int? selectedRoleId = cmbRole.SelectedValue as int?;

            if (string.IsNullOrEmpty(dolhName))
            {
                MessageBox.Show("Введите название должности");
                return;
            }

            if (!selectedRoleId.HasValue)
            {
                MessageBox.Show("Выберите роль");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = "INSERT INTO Dolh (Name_Dolh, id_rule) VALUES (@name, @ruleId)";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@name", dolhName);
                    command.Parameters.AddWithValue("@ruleId", selectedRoleId.Value);

                    connection.Open();
                    command.ExecuteNonQuery();

                    txtDolhName.Clear();
                    cmbRole.SelectedIndex = -1;
                    LoadData();
                    MessageBox.Show("Должность добавлена успешно!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении должности: {ex.Message}");
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        // Загрузка данных в DataGrid (с названием роли для отображения)
        private void LoadData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = @"
                        SELECT d.id_Dolh, d.Name_Dolh, d.id_rule, r.Name_R AS RoleName
                        FROM Dolh d
                        LEFT JOIN Rule r ON d.id_rule = r.id_Rule
                        ORDER BY d.id_Dolh";
                    MySqlCommand command = new MySqlCommand(query, connection);

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    dolhTable = new DataTable();
                    adapter.Fill(dolhTable);

                    if (!dolhTable.Columns.Contains("RoleName"))
                        dolhTable.Columns.Add("RoleName", typeof(string));

                    dgDolh.ItemsSource = dolhTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}");
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Dolh"))
            {
                MessageBox.Show("У вас нет прав для удаления должности!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dgDolh.SelectedItem == null)
            {
                MessageBox.Show("Выберите должность для удаления");
                return;
            }

            DataRowView selectedRow = dgDolh.SelectedItem as DataRowView;
            int id_dolh = Convert.ToInt32(selectedRow["id_Dolh"]);
            string name = selectedRow["Name_Dolh"].ToString();

            var result = MessageBox.Show($"Вы уверены, что хотите удалить должность '{name}'? ",
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
                                string deleteQuery = "DELETE FROM Dolh WHERE id_Dolh = @id";
                                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                                deleteCmd.Parameters.AddWithValue("@id", id_dolh);
                                int rowsAffected = deleteCmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // 2. Сброс AUTO_INCREMENT
                                    string getMaxIdQuery = "SELECT MAX(id_Dolh) as max_id FROM Dolh";
                                    MySqlCommand getMaxIdCmd = new MySqlCommand(getMaxIdQuery, conn, transaction);
                                    object maxIdResult = getMaxIdCmd.ExecuteScalar();

                                    int newAutoIncrementValue = 1;
                                    if (maxIdResult != null && maxIdResult != DBNull.Value)
                                    {
                                        newAutoIncrementValue = Convert.ToInt32(maxIdResult) + 1;
                                    }

                                    string alterQuery = "ALTER TABLE Dolh AUTO_INCREMENT = @new_auto_increment_value";
                                    MySqlCommand alterCmd = new MySqlCommand(alterQuery, conn, transaction);
                                    alterCmd.Parameters.AddWithValue("@new_auto_increment_value", newAutoIncrementValue);
                                    alterCmd.ExecuteNonQuery();

                                    transaction.Commit();
                                    LoadData();
                                    MessageBox.Show("Должность удалена успешно!");
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Ошибка: Должность не найдена или не была удалена.");
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
                    MessageBox.Show($"Ошибка при удалении должности: {ex.Message}");
                }
            }
        }


        // Обработка завершения редактирования ячейки
        private void dgDolh_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!AccessControl.CanDoOperation("Dolh"))
            {
                MessageBox.Show("У вас нет прав для редактирования должности!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView == null) return;

                int id_dolh = Convert.ToInt32(rowView["id_Dolh"]);
                string columnHeader = e.Column.Header.ToString();

                if (columnHeader == "Название должности")
                {
                    string newName = (e.EditingElement as TextBox)?.Text.Trim();
                    if (string.IsNullOrEmpty(newName))
                    {
                        MessageBox.Show("Название должности не может быть пустым");
                        e.Cancel = true;
                        return;
                    }
                    UpdateDolhField(id_dolh, "Name_Dolh", newName);
                }
                else if (columnHeader == "Роль")
                {
                    if (e.EditingElement is ComboBox comboBox)
                    {
                        int newRoleId = (int)comboBox.SelectedValue;
                        UpdateDolhField(id_dolh, "id_rule", newRoleId);
                    }
                }
            }
        }

        // Подготовка к редактированию ComboBox в ячейке (установка текущего значения)
        private void dgDolh_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.Column.Header.ToString() == "Роль" && e.EditingElement is ComboBox comboBox)
            {
                DataRowView rowView = e.Row.Item as DataRowView;
                if (rowView != null)
                {
                    int currentRoleId = Convert.ToInt32(rowView["id_rule"]);
                    comboBox.SelectedValue = currentRoleId;
                }
            }
        }

        // Обновление одного поля в БД
        private void UpdateDolhField(int id, string fieldName, object value)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    string query = $"UPDATE Dolh SET {fieldName} = @value WHERE id_Dolh = @id";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@value", value);
                    command.Parameters.AddWithValue("@id", id);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении: {ex.Message}");
                LoadData();
            }
        }
    }
}