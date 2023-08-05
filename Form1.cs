using System.Drawing;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WinFormsAppTest
{
    //class Layer
    //{
    //    List<Coordinate> listCoordinates = new List<Coordinate>();
    //    string M702 = "";
    //    string M704 = "";
    //}
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
                get_laser_config(input);
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
        string m702contur = "";
        string m704contur = "";
        string m702infill = "";
        string m704infill = "";
        void get_laser_config(string inputString)
        {
            string[] blocks = Regex.Split(inputString, @"\n(?=contour|infill|support)");

            // Обрабатываем каждый блок для извлечения значений M702 и M704
            //foreach (string block in blocks)
            //{
            for (int i = 1; i < 3; i++)
            {
                string pattern = @"M702\s+(\d+(\.\d+)?)\s+M704\s+(\d+(\.\d+)?)";
                Match match = Regex.Match(blocks[i], pattern);

                if (match.Success && match.Groups.Count >= 5)
                {
                    if (i == 1)
                    {
                        m702contur = match.Groups[1].Value;
                        m704contur = match.Groups[3].Value;
                    }
                    else
                    {
                        m702infill = match.Groups[1].Value;
                        m704infill = match.Groups[3].Value;
                    }
                }
            }
            label3.Text = "M702 : " + m702contur;
            label4.Text = "M704 : " + m704contur;
        }

        public class Layer
        {
            public int Index { get; set; }
            public List<Part> Parts { get; set; }
        }

        public class Part
        {
            public int Index { get; set; }
            public List<Coordinates> Coordinates { get; set; }
        }

        public class Coordinates
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        public static async Task<List<Layer>> ParseLayersAsync(string input)
        {
            List<Layer> layers = new List<Layer>();
            string layerPattern = @";Layer (\d+)([\s\S]*?)(?=;Layer|$)";
            MatchCollection layerMatches = Regex.Matches(input, layerPattern, RegexOptions.Singleline);

            foreach (Match layerMatch in layerMatches)
            {
                int layerIndex = int.Parse(layerMatch.Groups[1].Value);
                string layerContent = layerMatch.Groups[2].Value.Trim();
                List<Part> parts = await ParsePartsAsync(layerContent);

                layers.Add(new Layer { Index = layerIndex, Parts = parts });
            }

            return layers;
        }

        public static async Task<List<Part>> ParsePartsAsync(string input)
        {
            List<Part> parts = new List<Part>();
            string[] partStrings = input.Split(new[] { ";Layer" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string partString in partStrings)
            {
                int partIndex = GetPartIndex(partString);
                List<Coordinates> coordinates = await ExtractCoordinatesAsync(partString);

                parts.Add(new Part { Index = partIndex, Coordinates = coordinates });
            }

            return parts;
        }

        private static int GetPartIndex(string partString)
        {
            // Используем регулярное выражение для поиска индекса части
            string indexPattern = @"Part (\d+)";
            Match indexMatch = Regex.Match(partString, indexPattern);

            if (indexMatch.Success)
            {
                int index = int.Parse(indexMatch.Groups[1].Value);
                return index;
            }

            return -1; // Возврат значения по умолчанию, если индекс не найден
        }

        public static async Task<List<Coordinates>> ExtractCoordinatesAsync(string input)
        {
            List<Coordinates> coordinates = new List<Coordinates>();

            string pattern = @"G[01]\s+X(\-?\d+(\.\d+)?)\s+Y(\-?\d+(\.\d+)?)"; // Регулярное выражение для поиска координат X и Y
            await Task.Run(() =>
            {
                MatchCollection matches = Regex.Matches(input, pattern);

                foreach (Match match in matches)
                {
                    double x = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double y = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                    coordinates.Add(new Coordinates { X=x,Y= y });
                }
            });

            return coordinates;
        }


        public static async Task<List<Coordinates>> ParseCoordinatesAsync(string input)
        {
            List<Coordinates> coordinates = new List<Coordinates>();

            string coordinatePattern = @"G\d\s+X([\d\.\-]+)\s+Y([\d\.\-]+)";
            MatchCollection coordinateMatches = Regex.Matches(input, coordinatePattern);

            foreach (Match coordinateMatch in coordinateMatches)
            {
                double x, y;
                if (double.TryParse(coordinateMatch.Groups[1].Value, out x) && double.TryParse(coordinateMatch.Groups[2].Value, out y))
                {
                    coordinates.Add(new Coordinates { X = x, Y = y });
                }
                else
                {
                    // Обработка ошибок при разборе, если необходимо
                }
            }

            return coordinates;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                //флаг для выбора папок пользователем
                openFileDialog.ValidateNames = false;
                openFileDialog.CheckFileExists = false;
                openFileDialog.CheckPathExists = true;

                // Показать диалоговое окно выбора файла или папки
                DialogResult result1 = openFileDialog.ShowDialog();

                if (result1 == DialogResult.OK)
                {
                    // Получить выбранный путь к файлу
                    loadedFilePath = openFileDialog.FileName;
                    label1.Text = loadedFilePath;
                    label1.Visible = true;
                    fileLoaded = true;
                    try
                    {
                        // Прочитать содержимое файла асинхронно
                        string fileContent = await ReadFileAsync(loadedFilePath);

                        layers = await ParseLayersAsync(fileContent);

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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка чтения файла: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async Task<string> ReadFileAsync(string filePath)
        {
            using (var streamReader = new StreamReader(filePath))
            {
                return await streamReader.ReadToEndAsync();
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

        private int selectedLayerIndex = -1;
        private List<Layer> layers;
        private async void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Проверяем, выбран ли какой-либо элемент в ListBox
            if (listBox1.SelectedItem != null)
            {
                // Получаем индекс выбранного слоя
                selectedLayerIndex = (int)listBox1.SelectedItem;

                // Перерисовываем PictureBox с выбранным слоем
                pictureBox1.Invalidate();

                /*
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
                //selected Layer
                var selectedLayer = ReadContentBetweenLines(loadedFilePath, value1, value2);

                //var pointsBody = GetStringBetween(content, "M3", "contour");
                //var pointsContur = GetStringBetween(content, "contour\r\nM3", "\r\nM5");
                var pointsBody = await Task.Run(() => ExtractCoordinatesAsync(selectedLayer));
                //Point[] pointsArray = points.Select(c => new Point(c.X, c.Y)).ToArray();

                lstBody = pointsBody;
                //lstContur = pointsContur;

                // Перезапускаем отрисовку с начала
                //timer = new System.Windows.Forms.Timer();
                //timer.Interval = 5; // Задержка между отрисовкой (в миллисекундах)
                //timer.Tick += Timer_Tick;
                //currentIndex = 0;
                //timer.Start();

                pictureBox1.Invalidate();
                pictureBox1.Refresh();
                */
            }
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (selectedLayerIndex >= 0)
            {
                // Отрисовка выбранного слоя
                DrawLayer(e.Graphics, layers[selectedLayerIndex - 1]);
            }
        }

        private void DrawLayer(Graphics g, Layer layer)
        {
            // Ваша логика для отображения точек и линий выбранного слоя
            // Используйте layer.Parts для получения частей слоя
            // Каждая Part содержит список координат, которые можно отобразить точками и линиями

            // Пример:
            Pen pen = new Pen(Color.Red, 2);

            foreach (var part in layer.Parts)
            {
                PointF prevPoint = PointF.Empty;

                foreach (var point in part.Coordinates)
                {
                    PointF currentPoint = MapCoordinateToPoint(point);

                    if (!prevPoint.IsEmpty)
                    {
                        g.DrawLine(pen, prevPoint, currentPoint);
                    }

                    g.FillEllipse(Brushes.Black, currentPoint.X - 2, currentPoint.Y - 2, 4, 4);

                    prevPoint = currentPoint;
                }
            }

            pen.Dispose();
        }

        private PointF MapCoordinateToPoint(Coordinates coordinate)
        {
            // Ваша логика для масштабирования и отображения координат на PictureBox
            // Верните PointF с координатами для рисования точек и линий

            // Пример:
            float scale = pictureBox1.Width / 300f; // Замените 200f на нужный масштаб

            float x = (float)(coordinate.X * scale + pictureBox1.Width / 2);
            float y = (float)(-coordinate.Y * scale + pictureBox1.Height / 2);

            return new PointF(x, y);
        }

        private int currentIndex;
        private System.Windows.Forms.Timer timer;
        private float scale = 30f; // Масштаб
        private float pointSize = 1f; // Размер точки
        List<Coordinate> lstBody = new List<Coordinate>();
        List<Coordinate> lstContur = new List<Coordinate>();
        //static async Task<List<Coordinate>> ExtractCoordinatesAsync(string input)
        //{
        //    List<Coordinate> coordinates = new List<Coordinate>();

        //    string pattern = @"G[01]\s+X(\-?\d+(\.\d+)?)\s+Y(\-?\d+(\.\d+)?)"; // Regular expression to find coordinates X and Y
        //    await Task.Run(() =>
        //    {
        //        MatchCollection matches = Regex.Matches(input, pattern);

        //        foreach (Match match in matches)
        //        {
        //            double x;
        //            double y;
        //            if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
        //                double.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
        //            {
        //                coordinates.Add(new Coordinate(x, y));
        //            }
        //        }
        //    });

        //    return coordinates;
        //}
        static List<Coordinate> ExtractCoordinates(string input)
        {
            List<Coordinate> coordinates = new List<Coordinate>();
            string pattern = @"[XY]-?\d+(\.\d+)?"; // Регулярное выражение для поиска координат X и Y
            string[] lines = input.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                MatchCollection matches = Regex.Matches(line, pattern);

                double xValue = 0;
                double yValue = 0;

                foreach (Match match in matches)
                {
                    if (double.TryParse(match.Value.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out double coordinateValue))
                    {
                        if (match.Value.StartsWith("X"))
                        {
                            xValue = coordinateValue;
                        }
                        else if (match.Value.StartsWith("Y"))
                        {
                            yValue = coordinateValue;
                        }
                    }
                }

                coordinates.Add(new Coordinate(xValue, yValue));
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
        /*
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.Red, 2f);

            g.DrawLine(Pens.Black, 0, pictureBox1.Height / 2, pictureBox1.Width, pictureBox1.Height / 2);
            g.DrawLine(Pens.Black, pictureBox1.Width / 2, 0, pictureBox1.Width / 2, pictureBox1.Height);

            float gridSize = scale;
            while (gridSize < pictureBox1.Width || gridSize < pictureBox1.Height)
            {
                g.DrawLine(Pens.LightGray, pictureBox1.Width / 2 + gridSize, 0, pictureBox1.Width / 2 + gridSize, pictureBox1.Height);
                g.DrawLine(Pens.LightGray, pictureBox1.Width / 2 - gridSize, 0, pictureBox1.Width / 2 - gridSize, pictureBox1.Height);
                g.DrawLine(Pens.LightGray, 0, pictureBox1.Height / 2 + gridSize, pictureBox1.Width, pictureBox1.Height / 2 + gridSize);
                g.DrawLine(Pens.LightGray, 0, pictureBox1.Height / 2 - gridSize, pictureBox1.Width, pictureBox1.Height / 2 - gridSize);
                gridSize += scale;
            }

            // Отрисовка точек
            DrawPoints(g, lstBody, Brushes.Black);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Если отрисовка точек закончена, останавливаем таймер и начинаем отрисовку линий
            if (currentIndex >= lstBody.Count)
            {
                timer.Stop();
                currentIndex = 0; // Сбрасываем индекс для начала отрисовки линий
                timer.Interval = 5; // Задержка между отрисовкой линий (в миллисекундах)
                timer.Tick -= Timer_Tick; // Удаляем обработчик события Timer_Tick
                timer.Tick += Timer_DrawLines; // Добавляем обработчик события Timer_DrawLines
                timer.Start();
                return;
            }

            pictureBox1.Refresh(); // Обновляем PictureBox для отрисовки новых точек
            currentIndex++; // Увеличиваем индекс для следующей отрисовки
        }

        private void Timer_DrawLines(object sender, EventArgs e)
        {
            // Если отрисовка линий закончена, останавливаем таймер
            if (currentIndex >= lstContur.Count)
            {
                timer.Stop();
                return;
            }

            pictureBox1.Refresh(); // Обновляем PictureBox для отрисовки новых линий
            //DrawPoints(pictureBox1.CreateGraphics(), lstBody, lstBody.Count, Brushes.Black); // Отрисовываем все точки

            // Отрисовка линий до текущего индекса
            PointF prevPoint = PointF.Empty;
            Graphics g = pictureBox1.CreateGraphics(); // Создаем объект Graphics для рисования
            Pen pen = new Pen(Color.Red, 0.01f); // Создаем объект Pen для рисования линий

            for (int i = 0; i < currentIndex; i++)
            {
                PointF currentPoint = MapCoordinateToPoint(lstContur[i]);
                if (!prevPoint.IsEmpty)
                {
                    g.DrawLine(pen, prevPoint, currentPoint);
                }
                prevPoint = currentPoint;
            }

            g.Dispose(); // Освобождаем ресурсы объекта Graphics
            pen.Dispose(); // Освобождаем ресурсы объекта Pen

            currentIndex++; // Увеличиваем индекс для следующей отрисовки линий
        }


        private void DrawPoints(Graphics g, List<Coordinate> points, Brush brush)
        {
            for (int i = 0; i <  points.Count; i++)
            {
                PointF point = MapCoordinateToPoint(points[i]);
                g.FillEllipse(brush, point.X - pointSize / 2, point.Y - pointSize / 2, pointSize, pointSize);
            }
        }
        /*
        private void DrawLines(Graphics g, List<Coordinate> points, int endIndex, Pen pen)
        {
            PointF prevPoint = PointF.Empty;
            for (int i = 0; i < Math.Min(endIndex, points.Count); i++)
            {
                PointF currentPoint = MapCoordinateToPoint(points[i]);
                if (!prevPoint.IsEmpty)
                {
                    g.DrawLine(pen, prevPoint, currentPoint);
                }
                prevPoint = currentPoint;
            }
        }
        

        private PointF MapCoordinateToPoint(Coordinate coordinate)
        {
            float x = (float)(coordinate.X * scale + pictureBox1.Width / 2);
            float y = (float)(-coordinate.Y * scale + pictureBox1.Height / 2);
            return new PointF(x, y);
        }
        */
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
    /*
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
    */
}