using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NewTechnology
{
    public partial class Calculation : Window
    {
        private NewTechnologyEntities db = new NewTechnologyEntities();

        public Calculation()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProductTypes();
            LoadMaterialTypes();
        }

        private void LoadProductTypes()
        {
            try
            {
                var productTypes = db.ТипПродукции
                    .OrderBy(t => t.Наименование)
                    .ToList();

                productTypeComboBox.ItemsSource = productTypes;
                productTypeComboBox.DisplayMemberPath = "Наименование";
                productTypeComboBox.SelectedValuePath = "Код";

                if (productTypes.Any())
                    productTypeComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов продукции: {ex.Message}");
            }
        }

        private void LoadMaterialTypes()
        {
            try
            {
                var materialTypes = db.ТипМатериалов
                    .OrderBy(t => t.Наименование)
                    .ToList();

                materialTypeComboBox.ItemsSource = materialTypes;
                materialTypeComboBox.DisplayMemberPath = "Наименование";
                materialTypeComboBox.SelectedValuePath = "Код";

                if (materialTypes.Any())
                    materialTypeComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов материалов: {ex.Message}");
            }
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка выбора типов
                if (productTypeComboBox.SelectedValue == null ||
                    materialTypeComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Выберите тип продукции и тип материала");
                    return;
                }

                // Парсим значения
                if (!int.TryParse(materialAmountTextBox.Text, out int materialAmount) ||
                    materialAmount <= 0)
                {
                    MessageBox.Show("Введите корректное количество материала");
                    return;
                }

                if (!double.TryParse(param1TextBox.Text, out double param1) ||
                    param1 <= 0)
                {
                    MessageBox.Show("Введите корректное значение параметра 1");
                    return;
                }

                if (!double.TryParse(param2TextBox.Text, out double param2) ||
                    param2 <= 0)
                {
                    MessageBox.Show("Введите корректное значение параметра 2");
                    return;
                }

                // Получаем ID типов
                int productTypeId = (int)productTypeComboBox.SelectedValue;
                int materialTypeId = (int)materialTypeComboBox.SelectedValue;

                // Вызываем метод расчета
                int result = CalculateProductQuantity(
                    productTypeId,
                    materialTypeId,
                    materialAmount,
                    param1,
                    param2
                );

                // Отображаем результат
                if (result == -1)
                {
                    resultTextBlock.Text = "Ошибка";
                    resultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    resultTextBlock.Text = result.ToString();
                    resultTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете: {ex.Message}");
            }
        }

        // Метод расчета количества продукции
        public int CalculateProductQuantity(int productTypeId, int materialTypeId, int rawMaterialAmount,
                                          double parameter1, double parameter2)
        {
            try
            {
                // Проверка входных параметров
                if (rawMaterialAmount <= 0 || parameter1 <= 0 || parameter2 <= 0)
                    return -1;

                // Получение коэффициента типа продукции
                var productType = db.ТипПродукции.FirstOrDefault(p => p.Код == productTypeId);
                if (productType == null)
                    return -1;

                double productTypeCoefficient = productType.КоэфТипаПродукции ?? 1.0;

                // Получение процента брака материала
                var materialType = db.ТипМатериалов.FirstOrDefault(m => m.Код == materialTypeId);
                if (materialType == null)
                    return -1;

                double materialLossPercentage = materialType.ПроцентБракаМатериала ?? 0.0;

                // Расчет количества сырья на одну единицу продукции
                double rawMaterialPerUnit = parameter1 * parameter2 * productTypeCoefficient;

                if (rawMaterialPerUnit <= 0)
                    return -1;

                // Учет потерь сырья
                double rawMaterialWithLosses = rawMaterialPerUnit * (1 + materialLossPercentage / 100);

                // Расчет количества продукции
                double productQuantity = rawMaterialAmount / rawMaterialWithLosses;

                // Округление до целого числа в меньшую сторону
                int finalQuantity = (int)Math.Floor(productQuantity);

                return finalQuantity >= 0 ? finalQuantity : -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}