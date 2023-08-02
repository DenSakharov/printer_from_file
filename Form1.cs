using System.Drawing;
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
                //������� ������ ���������� ������� IndexTab
                indexTab = matches[0].Value;
                get_laser_config(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine("������: " + ex.Message);
            }
            int i = 0;

            //������� ��� ������ ������� ����� ��� ������ ����
            var array = GetNumbersInsideBrackets(indexTab);

            foreach (int numberStr in array)
            {
                i++;
                // � ������ �������, ������ ������� ����� ���� �����,
                // � ��������� ����� ��� �� �����, �� ���������� �� 10 (������ ��� ������������)
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

            // ������������ ������ ���� ��� ���������� �������� M702 � M704
            //foreach (string block in blocks)
            //{
            for (int i=1;i<3;i++) { 
                string pattern = @"M702\s+(\d+(\.\d+)?)\s+M704\s+(\d+(\.\d+)?)";
                Match match = Regex.Match(blocks[i], pattern);

                if (match.Success && match.Groups.Count >= 5)
                {
                    if (i==1) {
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
        private void button1_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                //���� ��� ������ ����� �������������
                openFileDialog.ValidateNames = false;
                openFileDialog.CheckFileExists = false;
                openFileDialog.CheckPathExists = true;

                // �������� ���������� ���� ������ ����� ��� �����
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // �������� ��������� ���� � �����
                    loadedFilePath = openFileDialog.FileName;
                    label1.Text = loadedFilePath;
                    label1.Visible = true;
                    fileLoaded = true;
                    try
                    {
                        // ��������� ���������� �����
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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("������ ������ �����: " + ex.Message, "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        public static int[] GetNumbersInsideBrackets(string inputString)
        {
            // ������� ���������� ��������� ��� ������ ����� ������ ���������� ������
            Regex regex = new Regex(@"\[(\d+(?:\s*,\s*\d+)*)\]");

            // ���� ���������� � ���������� ���������� � �������� ������
            Match match = regex.Match(inputString);

            // ���������, ������� �� ����������
            if (match.Success)
            {
                // �������� �������� ������ ���������� ������ (��� �������� � �������)
                string insideBrackets = match.Groups[1].Value;

                // ��������� ������ �� ����� �� �������
                string[] numberStrings = insideBrackets.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // ����������� ������ � ����� � ������� ������ �����
                int[] numbers = new int[numberStrings.Length];
                for (int i = 0; i < numberStrings.Length; i++)
                {
                    int.TryParse(numberStrings[i], out numbers[i]);
                }

                return numbers;
            }
            else
            {
                // ���� ���������� �� �������, ���������� ������ ������
                return new int[0];
            }
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // ���������, ������ �� �����-���� ������� � ListBox
            if (listBox1.SelectedItem != null)
            {
                // �������� ��������� �������
                int selectedItem = Convert.ToInt32(listBox1.SelectedItem.ToString());
                if (numberDictionary.TryGetValue(selectedItem, out int value1))
                {
                    //MessageBox.Show($" ��������: {value}");
                }
                if (numberDictionary.TryGetValue(selectedItem + 1, out int value2))
                {
                    //MessageBox.Show($" ��������: {value}");
                }
                var content = ReadContentBetweenLines(loadedFilePath, value1, value2);
                var pointsBody = GetStringBetween(content, "M3", "contour");
                var pointsContur = GetStringBetween(content, "contour\r\nM3", "\r\nM5");
                //Point[] pointsArray = points.Select(c => new Point(c.X, c.Y)).ToArray();

                lstBody = pointsBody;
                lstContur = pointsContur;
                // ������������� ��������� � ������
                timer = new System.Windows.Forms.Timer();
                timer.Interval = 5; // �������� ����� ���������� (� �������������)
                timer.Tick += Timer_Tick;
                currentIndex = 0;
                timer.Start();
                pictureBox1.Invalidate();
            }
        }

        private int currentIndex;
        private System.Windows.Forms.Timer timer;
        private float scale = 30f; // �������
        private float pointSize = 2f; // ������ �����
        List<Coordinate> lstBody = new List<Coordinate>();
        List<Coordinate> lstContur = new List<Coordinate>();
        static List<Coordinate> GetStringBetween(string input, string startString, string endString)
        {
            int startIndex = input.IndexOf(startString);
            int endIndex = input.IndexOf(endString, startIndex + startString.Length);

            startIndex += startString.Length;
            int length = endIndex - startIndex;

            string result = input.Substring(startIndex, length);
            var res = ExtractCoordinates(result);
            return res;
        }
        static List<Coordinate> ExtractCoordinates(string input)
        {
            List<Coordinate> coordinates = new List<Coordinate>();
            string pattern = @"[XY]-?\d+(\.\d+)?"; // ���������� ��������� ��� ������ ��������� X � Y
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
                Console.WriteLine($"���� '{filePath}' �� ������.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ��� ������ �����: {ex.Message}");
            }

            return content;
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.Red, 0.01f);

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

            // ��������� �����
            DrawPoints(g, lstBody, currentIndex, Brushes.Black);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // ���� ��������� ����� ���������, ������������� ������ � �������� ��������� �����
            if (currentIndex >= lstBody.Count)
            {
                timer.Stop();
                currentIndex = 0; // ���������� ������ ��� ������ ��������� �����
                timer.Interval = 5; // �������� ����� ���������� ����� (� �������������)
                timer.Tick -= Timer_Tick; // ������� ���������� ������� Timer_Tick
                timer.Tick += Timer_DrawLines; // ��������� ���������� ������� Timer_DrawLines
                timer.Start();
                return;
            }

            pictureBox1.Refresh(); // ��������� PictureBox ��� ��������� ����� �����
            currentIndex++; // ����������� ������ ��� ��������� ���������
        }

        private void Timer_DrawLines(object sender, EventArgs e)
        {
            // ���� ��������� ����� ���������, ������������� ������
            if (currentIndex >= lstContur.Count)
            {
                timer.Stop();
                return;
            }

            pictureBox1.Refresh(); // ��������� PictureBox ��� ��������� ����� �����
            DrawPoints(pictureBox1.CreateGraphics(), lstBody, lstBody.Count, Brushes.Black); // ������������ ��� �����

            // ��������� ����� �� �������� �������
            PointF prevPoint = PointF.Empty;
            Graphics g = pictureBox1.CreateGraphics(); // ������� ������ Graphics ��� ���������
            Pen pen = new Pen(Color.Red, 0.01f); // ������� ������ Pen ��� ��������� �����

            for (int i = 0; i < currentIndex; i++)
            {
                PointF currentPoint = MapCoordinateToPoint(lstContur[i]);
                if (!prevPoint.IsEmpty)
                {
                    g.DrawLine(pen, prevPoint, currentPoint);
                }
                prevPoint = currentPoint;
            }

            g.Dispose(); // ����������� ������� ������� Graphics
            pen.Dispose(); // ����������� ������� ������� Pen

            currentIndex++; // ����������� ������ ��� ��������� ��������� �����
        }


        private void DrawPoints(Graphics g, List<Coordinate> points, int endIndex, Brush brush)
        {
            for (int i = 0; i < Math.Min(endIndex, points.Count); i++)
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
        */

        private PointF MapCoordinateToPoint(Coordinate coordinate)
        {
            float x = (float)(coordinate.X * scale + pictureBox1.Width / 2);
            float y = (float)(-coordinate.Y * scale + pictureBox1.Height / 2);
            return new PointF(x, y);
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

            // ��������� ������� � ��������� ����
            Width = 1200;
            Height = 1200;
            CenterToScreen();

            // ������������� � ��������� �������
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 500; // �������� � ������������� ����� ���������� �����
            timer.Tick += Timer_Tick;

            // ��������� ������
            timer.Start();

            // ������������� �� ������� Paint ��� ���������
            Paint += DrawingForm_Paint;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // ���� ������� ������ ������ ���������� ����� ��� ���������,
            // ����������� ������ � �������� ����������� �����.
            if (currentPointIndex < pointsToDraw.Length)
            {
                currentPointIndex++;
                Invalidate(); // �������� ������� Paint ��� ����������� �����
            }
            else
            {
                // ���� ��� ����� ��� ����������, ������������� ������.
                timer.Stop();
            }
        }

        private void DrawingForm_Paint(object sender, PaintEventArgs e)
        {
            // �������� ������ Graphics ��� ��������� �� ����������� �����
            Graphics g = e.Graphics;

            // ������ ���� � ������� ����� (�����)
            int radius = 5; // ������ ����� (�����)
            int diameter = radius * 2;

            // ������ ���� (�����) ��� ������ �� ��� ������������ �����
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
            // ��������� ������� ���� � ���������
            Width = 1000;
            Height = 1000;
            Text = "������������ �����";
            shapePoints = shapePoint;
            // ������������ ���������� ������� Paint
            Paint += CoordinateGridForm_Paint;

            // ������� � ����������� ������
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // �������� � ������������� (1 �������)
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            // ���� �� �� ����� �� ��������� �����, ��������� ��������� ����� � ������
            if (currentPointIndex < shapePoints.Length)
            {
                drawnPoints.Add(shapePoints[currentPointIndex]);
                currentPointIndex++;

                // �������������� �����
                Invalidate();
            }
            else
            {
                // ���� ����� �� ��������� �����, ������������� ������
                timer.Stop();
            }
        }

        private void CoordinateGridForm_Paint(object sender, PaintEventArgs e)
        {
            // �������� ������ Graphics ��� ��������� �� �����
            Graphics g = e.Graphics;

            // ������ ��� X � Y (��� � ���������� �������)
            DrawCoordinateGrid(g);

            // ������ ������ �� ������ �� ������ drawnPoints
            if (drawnPoints.Count >= 2)
            {
                Pen shapePen = new Pen(Color.Red, 2); // ������� ����, ������� ����� 2 �������
                g.DrawLines(shapePen, drawnPoints.ToArray());
                shapePen.Dispose();
            }
        }
        private void DrawCoordinateGrid(Graphics g)
        {
            // ������� �����
            int formWidth = ClientSize.Width;
            int formHeight = ClientSize.Height;

            // ����� ��� ���������
            Pen axisPen = new Pen(Color.Black);
            Pen gridPen = new Pen(Color.LightGray);

            // ������ ��� X � Y
            g.DrawLine(axisPen, 0, formHeight / 2, formWidth, formHeight / 2); // ��� X
            g.DrawLine(axisPen, formWidth / 2, 0, formWidth / 2, formHeight); // ��� Y

            // ��� ������� �� ����
            int step = 100;

            // ������ ������� �� ��� X
            for (int x = formWidth / 2 + step; x < formWidth; x += step)
            {
                g.DrawLine(gridPen, x, 0, x, formHeight);
            }
            for (int x = formWidth / 2 - step; x > 0; x -= step)
            {
                g.DrawLine(gridPen, x, 0, x, formHeight);
            }

            // ������ ������� �� ��� Y
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