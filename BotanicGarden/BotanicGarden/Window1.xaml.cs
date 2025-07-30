using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static BotanicGarden.MainWindow;


namespace BotanicGarden
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private string connectionString = "Server=AZAZI;Database=Plants;Trusted_Connection=True;";
        public List<Plant> Plants { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
        public Window1()
        {
            InitializeComponent();
            OrderDetails = new List<OrderDetail>();
            LoadPlants();
        }

        private void SubmitOrderButton_Click(object sender, RoutedEventArgs e)
        {
            decimal totalPrice = 0;
            bool isEligibleForDiscount = false;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Получаем ID клиента из данных заказа
                int customerId = GetCustomerIdByEmail(CustomerEmailTextBox.Text, connection);
                if (customerId == -1)
                {
                    // Если клиента с таким email нет, создаем нового клиента
                    customerId = CreateNewCustomer(CustomerEmailTextBox.Text, CustomerNameTextBox.Text, CustomerPhoneTextBox.Text, connection);
                }
                string customerName = CustomerNameTextBox.Text;
                string PostalAddress = CustomerAddressTextBox.Text;
                string email = CustomerEmailTextBox.Text;
                string phone = CustomerPhoneTextBox.Text;
                var selectedPaymentType = PaymentTypeComboBox.SelectedItem as ComboBoxItem;

                // Преобразуем Content в число
                int paymentTypeId = 0; // Значение по умолчанию, если ничего не выбрано
                if (PaymentTypeComboBox.SelectedItem.ToString() == "Предоплата")
                {
                    paymentTypeId = 2;
                }
                else
                {
                    paymentTypeId = 1;
                }

                if (customerId != -1)
                {
                    // Проверяем, имеет ли клиент право на скидку
                    string checkDiscountQuery = @"
                SELECT EligibleForDiscount 
                FROM CustomerStatistics 
                WHERE CustomerID = @CustomerID";

                    using (SqlCommand discountCmd = new SqlCommand(checkDiscountQuery, connection))
                    {
                        discountCmd.Parameters.AddWithValue("@CustomerID", customerId);
                        object discountresult = discountCmd.ExecuteScalar();
                        if (discountresult != null && discountresult is bool discountEligible)
                        {
                            isEligibleForDiscount = discountEligible;
                        }
                    }
                }

                foreach (var orderDetail in OrderDetails)
                {
                    totalPrice += orderDetail.Price * orderDetail.Quantity;
                }

                if (isEligibleForDiscount)
                {
                    totalPrice *= 0.8m; // Применяем скидку 20%
                }

                // Сохраняем заказ в базе данных
                SaveOrder(customerId, PostalAddress, paymentTypeId, totalPrice, connection);

                MessageBox.Show($"Ваш заказ успешно создан! Итоговая цена: {totalPrice:C}", "Подтверждение заказа", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }
        private int CreateNewCustomer(string email, string name, string phone, SqlConnection connection)
        {
            string insertCustomerQuery = @"
    INSERT INTO Customers (Email, Name, Phone)
    VALUES (@Email, @Name, @Phone);
    SELECT CAST(SCOPE_IDENTITY() AS INT);"; // Получаем ID только что вставленного клиента

            using (SqlCommand command = new SqlCommand(insertCustomerQuery, connection))
            {
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Phone", phone);


                return (int)command.ExecuteScalar(); // Возвращаем новый CustomerID
            }
        }
        private int GetCustomerIdByEmail(string email, SqlConnection connection)
        {
            string query = "SELECT CustomerID FROM Customers WHERE Email = @Email";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Email", email);
                object result = command.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : -1;
            }
        }


        private void SaveOrder(int customerId, string postalAddress, int paymentTypeId, decimal totalPrice, SqlConnection connection)
        {
            // SQL-запрос для вставки данных в таблицу Orders
            string insertOrderQuery = @"
    INSERT INTO Orders (CustomerID, PostalAddress, PaymentTypeID, TotalPrice, OrderDate)
    VALUES (@CustomerID, @PostalAddress, @PaymentTypeID, @TotalPrice, GETDATE());";

            using (SqlCommand command = new SqlCommand(insertOrderQuery, connection))
            {
                // Параметры для вставки
                command.Parameters.AddWithValue("@CustomerID", customerId);
                command.Parameters.AddWithValue("@PostalAddress", postalAddress);
                command.Parameters.AddWithValue("@PaymentTypeID", paymentTypeId);
                command.Parameters.AddWithValue("@TotalPrice", totalPrice);

                // Выполнение запроса
                command.ExecuteNonQuery();
            }
        }


        private void CustomerNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только буквы (регулярное выражение для букв и пробела)
            var regex = new System.Text.RegularExpressions.Regex("[^a-zA-Zа-яА-ЯёЁ ]");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void CustomerPhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            var regex = new System.Text.RegularExpressions.Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void LoadPlants()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                       SELECT p.PlantID, p.GroupID, p.Name, p.Description, p.IsAvailableForPurchase, 
                        ps.SoldAs, ps.Quantity, ps.Price, ps.DeliveryTime
                        FROM Plants p
                        LEFT JOIN PlantSalesDetails ps ON p.PlantID = ps.PlantID
                        WHERE p.IsAvailableForPurchase = 1";
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
                            SoldAs = reader.GetString(5),
                            Quantity = reader.GetInt32(6),
                            Price = reader.GetDecimal(7),
                            DeliveryTime = reader.GetString(8),

                        });
                    }

                    // Установите начальный источник данных для ComboBox
                    InitialPlantComboBox.ItemsSource = Plants;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных растений: {ex.Message}");
            }
        }


        private void AddPlantButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlant = (Plant)InitialPlantComboBox.SelectedItem;
            var selectedQuantity = (int)InitialCountComboBox.SelectedItem;

            if (selectedPlant != null && selectedQuantity > 0)
            {
                var orderDetail = new OrderDetail
                {
                    PlantID = selectedPlant.PlantID,
                    Quantity = selectedQuantity,
                    Price = selectedPlant.Price
                };
                OrderDetails.Add(orderDetail);

                var orderPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 10, 0, 10)
                };

                var plantNameTextBlock = new TextBlock
                {
                    Text = selectedPlant.Name,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var quantityTextBlock = new TextBlock
                {
                    Text = " x " + selectedQuantity.ToString(),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var priceTextBlock = new TextBlock
                {
                    Text = " Цена: " + (selectedPlant.Price * selectedQuantity).ToString("C"),
                    VerticalAlignment = VerticalAlignment.Center
                };

                orderPanel.Children.Add(plantNameTextBlock);
                orderPanel.Children.Add(quantityTextBlock);
                orderPanel.Children.Add(priceTextBlock);

                OrderItemsStackPanel.Children.Add(orderPanel);
            }
        }
        public class OrderDetail
        {
            public int PlantID { get; set; }
            public string PlantName { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }

        private void InitialPlantComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InitialPlantComboBox.SelectedItem is Plant selectedPlant)
            {
                // Ограничиваем количество доступных значений в InitialCountComboBox
                var availableCount = selectedPlant.Quantity;

                // Создаем список с диапазоном от 1 до доступного количества
                var countOptions = new List<int>();
                for (int i = 1; i <= availableCount; i++)
                {
                    countOptions.Add(i);
                }

                // Привязываем список доступных значений к ComboBox
                InitialCountComboBox.ItemsSource = countOptions;
                InitialCountComboBox.SelectedIndex = 0; // Устанавливаем первый элемент как выбранный
            }
        }

        private void InitialCountComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }

        private void PaymentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
