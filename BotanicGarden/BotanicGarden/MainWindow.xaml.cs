using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlClient;

namespace BotanicGarden
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=AZAZI;Database=Plants;Trusted_Connection=True;";
        private List<Plant> Plants;


        public MainWindow()
        {
            InitializeComponent();
            LoadPlantGroups();
            LoadPlants();
        }
        private void LoadPlantGroups()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT GroupID, GroupName FROM PlantGroups";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    var groups = new List<PlantGroup>();
                    {
                        new PlantGroup { GroupID = 0, GroupName = "Все" };
                    }
                    while (reader.Read())
                    {
                        groups.Add(new PlantGroup
                        {
                            GroupID = reader.GetInt32(0),
                            GroupName = reader.GetString(1)
                        });
                    }
                    PlantGroupsComboBox.ItemsSource = groups;
                    PlantGroupsComboBox.DisplayMemberPath = "GroupName";
                    PlantGroupsComboBox.SelectedValuePath = "GroupID";
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }
        private void LoadPlants()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT p.PlantID, p.GroupID, p.Name, p.Description, p.IsAvailableForPurchase, ph.PhotoPath
                FROM Plants p
                LEFT JOIN PlantPhotos ph ON p.PlantID = ph.PlantID";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    Plants = new List<Plant>();
                    while (reader.Read())
                    {
                        Plants.Add(new Plant
                        {
                            PlantID = reader.GetInt32(0),
                            GroupID = reader.GetInt32(1),
                            Name = reader.GetString(2),
                            Description = reader.GetString(3),
                            IsAvailableForPurchase = reader.GetBoolean(4),
                            PhotoPath = reader.IsDBNull(5) ? null : reader.GetString(5)
                        });
                    }

                    // Установите начальный источник данных для ListBox
                    PlantsListBox.ItemsSource = Plants;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных растений: {ex.Message}");
            }
        }
        // Обработчик выбора растения
        private void PlantsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPlant = (Plant)PlantsListBox.SelectedItem;
            if (selectedPlant == null)
            {
                PlantNameTextBlock.Text = "Выберите растение, чтобы увидеть детали.";
                PlantDescriptionTextBlock.Text = "";
                PlantNameTextBlock.Text = "";
                PlantImage.Source = null;
                return;
            }
            PlantNameTextBlock.Text = selectedPlant.Name;
            PlantDescriptionTextBlock.Text = selectedPlant.Description;
            if (!string.IsNullOrEmpty(selectedPlant.PhotoPath))
            {
                try
                {
                    PlantImage.Source = new BitmapImage(new Uri(selectedPlant.PhotoPath));
                }
                catch
                {
                    PlantImage.Source = null;
                    MessageBox.Show("Ошибка загрузки изображения.");
                }
            }
            else
            {
                PlantImage.Source = null;
            }
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT SoldAs, Quantity, Price, DeliveryTime FROM PlantSalesDetails WHERE PlantID = @PlantID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PlantID", selectedPlant.PlantID);

                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    if (!selectedPlant.IsAvailableForPurchase)
                    {
                        string soldAs = reader.GetString(0);
                        int quantity = reader.GetInt32(1);
                        decimal price = reader.GetDecimal(2);
                        string deliveryTime = reader.GetString(3);

                        PlantAvailabilityTextBlock.Text = $"Продается как: {soldAs}\n" +
                                                          $"Количество: {quantity}\n" +
                                                          $"Цена: {price:C}\n" +
                                                          $"Сроки доставки: {deliveryTime}";
                    }
                    else
                    {
                        PlantAvailabilityTextBlock.Text = "Нет в наличии";
                    }
                }
            }

        }

        private void PlantGroupsComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (PlantGroupsComboBox.SelectedValue != null)
            {
                var selectedGroupID = (int)PlantGroupsComboBox.SelectedValue;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query;

                    if (selectedGroupID == 9)
                    {
                        query = @"
                SELECT p.PlantID, p.Name, p.Description, p.IsAvailableForPurchase, ph.PhotoPath
                FROM Plants p
                LEFT JOIN PlantPhotos ph ON p.PlantID = ph.PlantID";
                    }
                    else
                    {
                        query = @"
                SELECT p.PlantID, p.Name, p.Description, p.IsAvailableForPurchase, ph.PhotoPath
                FROM Plants p
                LEFT JOIN PlantPhotos ph ON p.PlantID = ph.PlantID
                WHERE p.GroupID = @GroupID";
                    }

                    SqlCommand command = new SqlCommand(query, connection);

                    if (selectedGroupID != 9) // 9 — условие для всех других групп
                    {
                        command.Parameters.AddWithValue("@GroupID", selectedGroupID);
                    }

                    SqlDataReader reader = command.ExecuteReader();

                    var plants = new List<Plant>();
                    while (reader.Read())
                    {
                        plants.Add(new Plant
                        {
                            PlantID = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            IsAvailableForPurchase = reader.GetBoolean(3),
                            PhotoPath = reader.IsDBNull(4) ? null : reader.GetString(4)
                        });
                    }

                    PlantsListBox.ItemsSource = plants;
                }

                // Очищаем все текстовые блоки при смене группы
                PlantAvailabilityTextBlock.Text = "";
                PlantDescriptionTextBlock.Text = "";
                PlantNameTextBlock.Text = "Выберите растение, чтобы увидеть детали.";
            }
        }


        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchQuery = SearchTextBox.Text.Trim().ToLower();
            int selectedGroupID = (int)PlantGroupsComboBox.SelectedValue;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query;

                if (selectedGroupID == 9) // Группа "Все"
                {
                    query = @"
                SELECT p.PlantID, p.Name, p.Description, p.IsAvailableForPurchase, ph.PhotoPath
                FROM Plants p
                LEFT JOIN PlantPhotos ph ON p.PlantID = ph.PlantID
                WHERE LOWER(p.Name) LIKE @SearchQuery";
                }
                else
                {
                    query = @"
                SELECT p.PlantID, p.Name, p.Description, p.IsAvailableForPurchase, ph.PhotoPath
                FROM Plants p
                LEFT JOIN PlantPhotos ph ON p.PlantID = ph.PlantID
                WHERE p.GroupID = @GroupID AND LOWER(p.Name) LIKE @SearchQuery";
                }

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");

                if (selectedGroupID != 9)
                {
                    command.Parameters.AddWithValue("@GroupID", selectedGroupID);
                }

                SqlDataReader reader = command.ExecuteReader();

                var plants = new List<Plant>();
                while (reader.Read())
                {
                    plants.Add(new Plant
                    {
                        PlantID = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        IsAvailableForPurchase = reader.GetBoolean(3),
                        PhotoPath = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }

                PlantsListBox.ItemsSource = plants;
            }

            // Очищаем текстовые блоки после поиска
            PlantAvailabilityTextBlock.Text = "";
            PlantDescriptionTextBlock.Text = "";
            PlantNameTextBlock.Text = "Выберите растение, чтобы увидеть детали.";
        }



        public class PlantGroup
        {
            public int GroupID { get; set; }
            public string GroupName { get; set; }
        }

        public class Plant
        {
            public int PlantID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public bool IsAvailableForPurchase { get; set; }
            public string SoldAs { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public string DeliveryTime { get; set; }
            public int GroupID { get; set; }
            public string PhotoPath { get; set; }

        }

        private void AddOrderButton_Click(object sender, RoutedEventArgs e)
        {
            Window1 createOrderWindow = new Window1();
            createOrderWindow.Show();
        }

        private void ViewOrdersButton_Click(object sender, RoutedEventArgs e)
        {
            Window2 createOrderWindow2 = new Window2();
            createOrderWindow2.Show();
        }

        private void ViewStatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Получаем список популярных растений
                string popularPlantsQuery = @"
        SELECT TOP 5 p.Name, SUM(od.Quantity) AS TotalQuantity
        FROM OrderDetails od
        JOIN Plants p ON od.PlantID = p.PlantID
        GROUP BY p.Name
        ORDER BY TotalQuantity DESC";

                var popularPlants = new StringBuilder("Популярные растения:\n");
                using (SqlCommand command = new SqlCommand(popularPlantsQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string plantName = reader.GetString(0);
                            int totalQuantity = reader.GetInt32(1);
                            popularPlants.AppendLine($"{plantName}: {totalQuantity} заказов");
                        }
                    } // DataReader закрывается здесь
                }

                // Получаем наиболее активный регион
                string activeRegionQuery = @"
        SELECT TOP 1 PostalAddress, COUNT(*) AS OrderCount
        FROM Orders
        GROUP BY PostalAddress
        ORDER BY OrderCount DESC";

                var activeRegion = "Не удалось определить активный регион.";
                using (SqlCommand command = new SqlCommand(activeRegionQuery, connection))
                {
                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        activeRegion = result.ToString();
                    }
                }

                // Собираем статистику
                string message = popularPlants.ToString();
                message += $"\nНаиболее активный регион: {activeRegion}";

                // Показываем статистику в MessageBox
                MessageBox.Show(message, "Статистика заказов", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

