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

                        //чтение слоев и заполнения структуры
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
   
}