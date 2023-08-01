using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WinFormsAppTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private string loadedFilePath;
        private bool fileLoaded;
        private string text_into;

        Dictionary<int, int> numberDictionary = new Dictionary<int, int>();

        private int parsseLinesCount(string input)
        {
            string pattern = @";IndexTab \[[^\]]*\]";
            string indexTab = "";
            try
            {
                Regex regex = new Regex(pattern);
                MatchCollection matches = regex.Matches(input);
                //находим первое совпадение правила IndexTab
                indexTab = matches[0].Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
            int i = 0;

            //функция для поиска номеров строк для начала слоя
            var array = GetNumbersInsideBrackets(indexTab);

            foreach (int numberStr in array)
            {
                i++;
                // В данном примере, ключом словаря будет само число,
                // а значением будет это же число, но умноженное на 10 (просто для демонстрации)
                numberDictionary.Add(i, numberStr);

            }
            i = 0;
            char separator = ',';
            pattern = separator.ToString();
            int count = Regex.Matches(indexTab, pattern).Count;
            i = count;

            return i;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                //флаг для выбора папок пользователем
                openFileDialog.ValidateNames = false;
                openFileDialog.CheckFileExists = false;
                openFileDialog.CheckPathExists = true;

                // Показать диалоговое окно выбора файла или папки
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Получить выбранный путь к файлу
                    loadedFilePath = openFileDialog.FileName;
                    label1.Text = loadedFilePath;
                    label1.Visible = true;
                    fileLoaded = true;
                    try
                    {
                        // Прочитать содержимое файла
                        string fileContent = File.ReadAllText(loadedFilePath);
                        int result = parsseLinesCount(fileContent);
                        List<int> linescount = new List<int>();
                        for (int i = 1; i <= result; i++)
                        {
                            linescount.Add(i);
                        }
                        foreach (int number in linescount)
                        {
                            listBox1.Items.Add(number);
                        }
                        text_into = fileContent;
                        //MessageBox.Show("test");
                        // Отобразить содержимое файла в MessageBox
                        //MessageBox.Show("Содержимое файла:\n" + fileContent, "Содержимое файла", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка чтения файла: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        public static int[] GetNumbersInsideBrackets(string inputString)
        {
            // Создаем регулярное выражение для поиска чисел внутри квадратных скобок
            Regex regex = new Regex(@"\[(\d+(?:\s*,\s*\d+)*)\]");

            // Ищем совпадение с регулярным выражением в исходной строке
            Match match = regex.Match(inputString);

            // Проверяем, найдено ли совпадение
            if (match.Success)
            {
                // Получаем значение внутри квадратных скобок (без пробелов и запятых)
                string insideBrackets = match.Groups[1].Value;

                // Разделяем строку на числа по запятой
                string[] numberStrings = insideBrackets.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Преобразуем строки в числа и создаем массив чисел
                int[] numbers = new int[numberStrings.Length];
                for (int i = 0; i < numberStrings.Length; i++)
                {
                    int.TryParse(numberStrings[i], out numbers[i]);
                }

                return numbers;
            }
            else
            {
                // Если совпадение не найдено, возвращаем пустой массив
                return new int[0];
            }
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Проверяем, выбран ли какой-либо элемент в ListBox
            if (listBox1.SelectedItem != null)
            {
                // Получаем выбранный элемент
                int selectedItem = Convert.ToInt32(listBox1.SelectedItem.ToString());
                if (numberDictionary.TryGetValue(selectedItem, out int value1))
                {
                    //MessageBox.Show($" Значение: {value}");
                }
                if (numberDictionary.TryGetValue(selectedItem + 1, out int value2))
                {
                    //MessageBox.Show($" Значение: {value}");
                }
                var content = ReadContentBetweenLines(loadedFilePath, value1, value2);
                var points = GetStringBetween(content, "M3", "contour");

                //Point[] pointsArray = points.Select(c => new Point(c.X, c.Y)).ToArray();

                lst = points;
                pictureBox1.Invalidate();
                // DrawingForm dr = new DrawingForm(points);
                //dr.ShowDialog();

                //CoordinateGridForm cf = new CoordinateGridForm(pointsArray);
                //cf.ShowDialog();
            }
        }
        List<Coordinate> lst = new List<Coordinate>();
        void DrawFigure1(List<Coordinate> coordinates)
        {
            PictureBox pictureBox = new PictureBox();
            pictureBox.Size = new Size(40, 40); // Размеры PictureBox
            pictureBox.Location = new Point(300, 10); // Расположение PictureBox на форме
            this.Controls.Add(pictureBox);

            // Настройки рисования на PictureBox
            pictureBox.Paint += (sender, e) =>
            {
                Graphics g = e.Graphics;
                Pen pen = new Pen(Color.Black, 2);

                // Преобразование в координаты PictureBox
                float scaleFactor = 100.0f; // Масштабный коэффициент для увеличения размеров
                float offsetX = 400.0f; // Смещение по X
                float offsetY = 300.0f; // Смещение по Y

                // Рисование линий между точками
                for (int i = 0; i < coordinates.Count - 1; i++)
                {
                    float x1 = (float)(coordinates[i].X * scaleFactor + offsetX);
                    float y1 = (float)(-coordinates[i].Y * scaleFactor + offsetY);
                    float x2 = (float)(coordinates[i + 1].X * scaleFactor + offsetX);
                    float y2 = (float)(-coordinates[i + 1].Y * scaleFactor + offsetY);

                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            };
        }

        static List<Coordinate> GetStringBetween(string input, string startString, string endString)
        {
            int startIndex = input.IndexOf(startString);
            if (startIndex == -1)
                return null;

            startIndex += startString.Length;

            int endIndex = input.IndexOf(endString, startIndex);
            if (endIndex == -1)
                return null;

            string result = input.Substring(startIndex, endIndex - startIndex);
            var res = ExtractCoordinates(result);
            return res;
        }
        static List<Coordinate> ExtractCoordinates(string input)
        {
            List<Coordinate> coordinates = new List<Coordinate>();

            string pattern = @"[XY](\d+(\.\d+)?)"; // Регулярное выражение для поиска координат X и Y
            MatchCollection matches = Regex.Matches(input, pattern);

            if (matches.Count % 2 == 0)
            {
                for (int i = 0; i < matches.Count; i += 2)
                {
                    double x = double.Parse(matches[i].Groups[1].Value, CultureInfo.InvariantCulture);
                    double y = double.Parse(matches[i + 1].Groups[1].Value, CultureInfo.InvariantCulture);
                    coordinates.Add(new Coordinate(x, y));
                }
            }

            return coordinates;
        }
        public static string ReadContentBetweenLines(string filePath, int startLine, int endLine)
        {
            string content = "";

            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    int currentLine = 1;
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (currentLine >= startLine && currentLine <= endLine)
                        {
                            content += line + Environment.NewLine;
                        }

                        if (currentLine > endLine)
                        {
                            break;
                        }

                        currentLine++;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Файл '{filePath}' не найден.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
            }

            return content;
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.Black, 2);

            // Преобразование в координаты pictureBox1
            float scaleFactor = 50.0f; // Масштабный коэффициент для увеличения размеров
            float offsetX = 200.0f; // Смещение по X
            float offsetY = 200.0f; // Смещение по Y

            // Рисование координатной сетки
            DrawCoordinateGrid(g, scaleFactor, offsetX, offsetY);

            // Рисование линий между точками
            for (int i = 0; i < lst.Count - 1; i++)
            {
                float x1 = (float)(lst[i].X * scaleFactor + offsetX);
                float y1 = (float)(-lst[i].Y * scaleFactor + offsetY);
                float x2 = (float)(lst[i + 1].X * scaleFactor + offsetX);
                float y2 = (float)(-lst[i + 1].Y * scaleFactor + offsetY);

                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }
        private void DrawCoordinateGrid(Graphics g, float scaleFactor, float offsetX, float offsetY)
        {
            Pen gridPen = new Pen(Color.LightGray, 1);

            // Рисование вертикальных линий координатной сетки
            for (int x = -10; x <= 10; x++)
            {
                float xPos = x * 50.0f * scaleFactor + offsetX;
                g.DrawLine(gridPen, xPos, -1000, xPos, 1000);
            }

            // Рисование горизонтальных линий координатной сетки
            for (int y = -10; y <= 10; y++)
            {
                float yPos = y * 50.0f * scaleFactor + offsetY;
                g.DrawLine(gridPen, -1000, yPos, 1000, yPos);
            }
        }
    }
    class Coordinate
    {
        public double X { get; private set; }
        public double Y { get; private set; }

        public Coordinate(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
    public class DrawingForm : Form
    {
        private Point[] pointsToDraw;
        private int currentPointIndex;
        private System.Windows.Forms.Timer timer;

        public DrawingForm(Point[] points)
        {
            pointsToDraw = points;
            currentPointIndex = 0;

            // Настройка размера и положения окна
            Width = 1200;
            Height = 1200;
            CenterToScreen();

            // Инициализация и настройка таймера
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 500; // Интервал в миллисекундах между отрисовкой точек
            timer.Tick += Timer_Tick;

            // Запускаем таймер
            timer.Start();

            // Подписываемся на событие Paint для рисования
            Paint += DrawingForm_Paint;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Если текущий индекс меньше количества точек для отрисовки,
            // увеличиваем индекс и вызываем перерисовку формы.
            if (currentPointIndex < pointsToDraw.Length)
            {
                currentPointIndex++;
                Invalidate(); // Вызывает событие Paint для перерисовки формы
            }
            else
            {
                // Если все точки уже отрисованы, останавливаем таймер.
                timer.Stop();
            }
        }

        private void DrawingForm_Paint(object sender, PaintEventArgs e)
        {
            // Получаем объект Graphics для рисования на поверхности формы
            Graphics g = e.Graphics;

            // Задаем цвет и толщину круга (точки)
            int radius = 5; // Радиус круга (точки)
            int diameter = radius * 2;

            // Рисуем круг (точку) для каждой из уже отрисованных точек
            using (Brush brush = new SolidBrush(Color.Black))
            {
                for (int i = 0; i < currentPointIndex; i++)
                {
                    Point point = pointsToDraw[i];
                    int x = point.X - radius;
                    int y = point.Y - radius;
                    g.FillEllipse(brush, x, y, diameter, diameter);
                }
            }
        }
    }
    public class CoordinateGridForm : Form
    {
        private Point[] shapePoints = { };

        private List<Point> drawnPoints = new List<Point>();
        private int currentPointIndex = 0;
        private System.Windows.Forms.Timer timer;

        public CoordinateGridForm(Point[] shapePoint)
        {
            // Настройка размера окна и заголовка
            Width = 1000;
            Height = 1000;
            Text = "Координатная сетка";
            shapePoints = shapePoint;
            // Регистрируем обработчик события Paint
            Paint += CoordinateGridForm_Paint;

            // Создаем и настраиваем таймер
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // Интервал в миллисекундах (1 секунда)
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Если мы не дошли до последней точки, добавляем следующую точку в список
            if (currentPointIndex < shapePoints.Length)
            {
                drawnPoints.Add(shapePoints[currentPointIndex]);
                currentPointIndex++;

                // Перерисовываем форму
                Invalidate();
            }
            else
            {
                // Если дошли до последней точки, останавливаем таймер
                timer.Stop();
            }
        }

        private void CoordinateGridForm_Paint(object sender, PaintEventArgs e)
        {
            // Получаем объект Graphics для рисования на форме
            Graphics g = e.Graphics;

            // Рисуем оси X и Y (как в предыдущем примере)
            DrawCoordinateGrid(g);

            // Рисуем фигуру по точкам из списка drawnPoints
            if (drawnPoints.Count >= 2)
            {
                Pen shapePen = new Pen(Color.Red, 2); // Красный цвет, толщина линии 2 пикселя
                g.DrawLines(shapePen, drawnPoints.ToArray());
                shapePen.Dispose();
            }
        }
        private void DrawCoordinateGrid(Graphics g)
        {
            // Размеры формы
            int formWidth = ClientSize.Width;
            int formHeight = ClientSize.Height;

            // Цветы для рисования
            Pen axisPen = new Pen(Color.Black);
            Pen gridPen = new Pen(Color.LightGray);

            // Рисуем оси X и Y
            g.DrawLine(axisPen, 0, formHeight / 2, formWidth, formHeight / 2); // Ось X
            g.DrawLine(axisPen, formWidth / 2, 0, formWidth / 2, formHeight); // Ось Y

            // Шаг делений по осям
            int step = 100;

            // Рисуем деления по оси X
            for (int x = formWidth / 2 + step; x < formWidth; x += step)
            {
                g.DrawLine(gridPen, x, 0, x, formHeight);
            }
            for (int x = formWidth / 2 - step; x > 0; x -= step)
            {
                g.DrawLine(gridPen, x, 0, x, formHeight);
            }

            // Рисуем деления по оси Y
            for (int y = formHeight / 2 + step; y < formHeight; y += step)
            {
                g.DrawLine(gridPen, 0, y, formWidth, y);
            }
            for (int y = formHeight / 2 - step; y > 0; y -= step)
            {
                g.DrawLine(gridPen, 0, y, formWidth, y);
            }
        }
    }
}