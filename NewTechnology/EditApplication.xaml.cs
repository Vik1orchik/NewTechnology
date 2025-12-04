using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NewTechnology
{
    public partial class EditApplication : Window
    {
        private NewTechnologyEntities db = new NewTechnologyEntities();
        private int _partnerId;
        private bool _isEditMode;

        public EditApplication(int partnerId = 0)
        {
            InitializeComponent();
            _partnerId = partnerId;
            _isEditMode = partnerId > 0;
            Loaded += Window_Loaded;
        }

        // Универсальный класс модели для продукции
        public class ProductItemViewModel
        {
            public int Код { get; set; }
            public string Наименование { get; set; }
            public int Количество { get; set; }
            public bool Выбрано { get; set; }
        }

        private void LoadPartnerData()
        {
            try
            {
                var partner = db.Партнеры.FirstOrDefault(p => p.Код == _partnerId);
                if (partner != null)
                {
                    // Загружаем типы партнеров
                    var types = db.ТипПартнеров.ToList();

                    // Настраиваем ComboBox
                    typeComboBox.ItemsSource = types;
                    typeComboBox.DisplayMemberPath = "Наимнование";
                    typeComboBox.SelectedValuePath = "Код";

                    // Устанавливаем выбранный тип
                    if (partner.КодТипПартнера.HasValue)
                    {
                        // Ждем немного, чтобы ComboBox успел загрузить данные
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            typeComboBox.SelectedValue = partner.КодТипПартнера.Value;
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }

                    // Заполняем остальные поля
                    nameTextBox.Text = partner.Наименование;
                    lastNameTextBox.Text = partner.Фамилия;
                    firstNameTextBox.Text = partner.Имя;
                    middleNameTextBox.Text = partner.Отчество;
                    addressTextBox.Text = partner.ЮрАдрес;
                    phoneTextBox.Text = partner.Телефон;
                    emailTextBox.Text = partner.Почта;
                    ratingTextBox.Text = (partner.Рейтинг ?? 5).ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем типы партнеров
            var types = db.ТипПартнеров.ToList();
            typeComboBox.ItemsSource = types;
            typeComboBox.DisplayMemberPath = "Наимнование";
            typeComboBox.SelectedValuePath = "Код";

            if (_isEditMode)
            {
                LoadPartnerData();
                LoadPartnerProducts();
            }
            else
            {
                if (types.Any())
                {
                    typeComboBox.SelectedIndex = 0;
                }
                ratingTextBox.Text = "5";
                LoadAllProducts();
            }
        }

        private void LoadAllProducts()
        {
            try
            {
                // Загружаем всю продукцию
                var allProducts = db.Продукция
                    .Select(p => new ProductItemViewModel
                    {
                        Код = p.Код,
                        Наименование = p.Наименование,
                        Количество = 0,
                        Выбрано = false
                    })
                    .OrderBy(p => p.Наименование)
                    .ToList();

                productsDataGrid.ItemsSource = allProducts;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продукции: " + ex.Message);
            }
        }

        private void LoadPartnerProducts()
        {
            try
            {
                if (!_isEditMode) return;

                // Получаем продукцию партнера из заявок
                var partnerProducts = (from za in db.Заявка
                                       join pr in db.Продукция on za.КодПродукции equals pr.Код
                                       where za.КодПартнера == _partnerId
                                       select new
                                       {
                                           Код = pr.Код,
                                           Наименование = pr.Наименование,
                                           Количество = za.КоличествоПродукции ?? 0
                                       }).Distinct().ToList();

                // Получаем все продукты
                var allProducts = db.Продукция
                    .Select(p => new ProductItemViewModel
                    {
                        Код = p.Код,
                        Наименование = p.Наименование,
                        Количество = 0,
                        Выбрано = false
                    })
                    .OrderBy(p => p.Наименование)
                    .ToList();

                // Помечаем продукты партнера как выбранные и устанавливаем количество
                foreach (var product in allProducts)
                {
                    var partnerProduct = partnerProducts.FirstOrDefault(pp => pp.Код == product.Код);
                    if (partnerProduct != null)
                    {
                        product.Выбрано = true;
                        product.Количество = partnerProduct.Количество;
                    }
                }

                productsDataGrid.ItemsSource = allProducts;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продукции: " + ex.Message);
            }
        }

        private bool ValidateForm()
        {
            string errors = "";

            if (typeComboBox.SelectedValue == null)
                errors += "Выберите тип партнера\n";

            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                errors += "Введите название компании\n";

            if (string.IsNullOrWhiteSpace(lastNameTextBox.Text))
                errors += "Введите фамилию\n";

            if (string.IsNullOrWhiteSpace(firstNameTextBox.Text))
                errors += "Введите имя\n";

            if (string.IsNullOrWhiteSpace(addressTextBox.Text))
                errors += "Введите адрес\n";

            if (string.IsNullOrWhiteSpace(phoneTextBox.Text))
                errors += "Введите телефон\n";
            else if (!Regex.IsMatch(phoneTextBox.Text, @"^[\d\s\(\)\-+]+$"))
                errors += "Неверный формат телефона\n";

            if (!int.TryParse(ratingTextBox.Text, out int rating) || rating < 0 || rating > 10)
                errors += "Рейтинг от 0 до 10\n";

            // Проверяем, есть ли выбранные продукты
            if (productsDataGrid.ItemsSource is List<ProductItemViewModel> products)
            {
                if (!products.Any(p => p.Выбрано))
                {
                    errors += "Выберите хотя бы один продукт\n";
                }
                else
                {
                    // Проверяем, что у всех выбранных продуктов указано количество > 0
                    var invalidProducts = products.Where(p => p.Выбрано && p.Количество <= 0).ToList();
                    if (invalidProducts.Any())
                    {
                        errors += "У выбранных продуктов должно быть указано количество больше 0\n";
                    }
                }
            }

            if (!string.IsNullOrEmpty(errors))
            {
                return false;
            }

            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                MessageBox.Show("Исправьте ошибки", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_isEditMode)
                {
                    UpdatePartner();
                }
                else
                {
                    CreatePartner();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private void CreatePartner()
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Создаем партнера
                    var newPartner = new Партнеры
                    {
                        КодТипПартнера = (int)typeComboBox.SelectedValue,
                        Наименование = nameTextBox.Text,
                        Фамилия = lastNameTextBox.Text,
                        Имя = firstNameTextBox.Text,
                        Отчество = middleNameTextBox.Text,
                        ЮрАдрес = addressTextBox.Text,
                        Телефон = phoneTextBox.Text,
                        Почта = emailTextBox.Text,
                        Рейтинг = int.Parse(ratingTextBox.Text)
                    };

                    db.Партнеры.Add(newPartner);
                    db.SaveChanges(); // Сохраняем, чтобы получить ID

                    // Создаем заявки для выбранной продукции
                    if (productsDataGrid.ItemsSource is List<ProductItemViewModel> products)
                    {
                        foreach (var product in products.Where(p => p.Выбрано && p.Количество > 0))
                        {
                            var новаяЗаявка = new Заявка
                            {
                                КодПартнера = newPartner.Код,
                                КодПродукции = product.Код,
                                КоличествоПродукции = product.Количество,
                                ДатаЗаявки = DateTime.Now
                            };
                            db.Заявка.Add(новаяЗаявка);
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    MessageBox.Show("Партнер и заявки добавлены");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Ошибка сохранения: " + ex.Message);
                    throw;
                }
            }
        }

        private void UpdatePartner()
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var partner = db.Партнеры.FirstOrDefault(p => p.Код == _partnerId);
                    if (partner != null)
                    {
                        // Обновляем данные партнера
                        partner.КодТипПартнера = (int)typeComboBox.SelectedValue;
                        partner.Наименование = nameTextBox.Text;
                        partner.Фамилия = lastNameTextBox.Text;
                        partner.Имя = firstNameTextBox.Text;
                        partner.Отчество = middleNameTextBox.Text;
                        partner.ЮрАдрес = addressTextBox.Text;
                        partner.Телефон = phoneTextBox.Text;
                        partner.Почта = emailTextBox.Text;
                        partner.Рейтинг = int.Parse(ratingTextBox.Text);

                        db.SaveChanges();

                        // Получаем текущие заявки партнера
                        var existingApplications = db.Заявка.Where(z => z.КодПартнера == _partnerId).ToList();

                        // Обрабатываем продукты из DataGrid
                        if (productsDataGrid.ItemsSource is List<ProductItemViewModel> products)
                        {
                            // Создаем словарь для быстрого поиска заявок по коду продукции
                            var appDict = existingApplications.ToDictionary(a => a.КодПродукции);

                            foreach (var product in products)
                            {
                                if (product.Выбрано && product.Количество > 0)
                                {
                                    // Проверяем, есть ли уже заявка для этой продукции
                                    if (appDict.TryGetValue(product.Код, out var existingApp))
                                    {
                                        // Обновляем существующую заявку
                                        existingApp.КоличествоПродукции = product.Количество;
                                    }
                                    else
                                    {
                                        // Создаем новую заявку
                                        var новаяЗаявка = new Заявка
                                        {
                                            КодПартнера = _partnerId,
                                            КодПродукции = product.Код,
                                            КоличествоПродукции = product.Количество,
                                            ДатаЗаявки = DateTime.Now
                                        };
                                        db.Заявка.Add(новаяЗаявка);
                                    }
                                }
                                else
                                {
                                    // Удаляем заявку, если продукт не выбран
                                    if (appDict.TryGetValue(product.Код, out var appToRemove))
                                    {
                                        db.Заявка.Remove(appToRemove);
                                    }
                                }
                            }
                        }

                        db.SaveChanges();
                        transaction.Commit();
                        MessageBox.Show("Данные обновлены");
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Ошибка обновления: " + ex.Message);
                    throw;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

   
    }
}