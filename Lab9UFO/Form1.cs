using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Lab9UFO
{
    public partial class Form1 : Form
    {
        private PointF startPoint = new PointF(100, 200);
        private PointF endPoint = new PointF(500, 300);
        
        private float stepSize = 2;
        private int seriesTerms = 5;
        private float toleranceRadius = 10;
        
        private Button drawButton;
        private Button testButton;
        private NumericUpDown radiusInput;
        private NumericUpDown nInput;
        private Label radiusLabel;
        private Label nLabel;
        private Panel drawingPanel;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
            this.DoubleBuffered = true;
            this.ClientSize = new Size(800, 600);
        }

        private void InitializeCustomComponents()
        {
            drawingPanel = new Panel();
            drawingPanel.Location = new Point(0, 0);
            drawingPanel.Size = new Size(600, 600);
            drawingPanel.BackColor = Color.White;
            drawingPanel.Paint += DrawingPanel_Paint;
            this.Controls.Add(drawingPanel);

            int rightMargin = 620;
            int topMargin = 20;
            int controlWidth = 150;

            drawButton = new Button();
            drawButton.Text = "Draw";
            drawButton.Location = new Point(rightMargin, topMargin);
            drawButton.Size = new Size(controlWidth, 30);
            drawButton.Click += DrawButton_Click;
            this.Controls.Add(drawButton);

            nLabel = new Label();
            nLabel.Text = "Точность (n):";
            nLabel.Location = new Point(rightMargin, topMargin + 50);
            nLabel.Size = new Size(controlWidth, 20);
            this.Controls.Add(nLabel);

            nInput = new NumericUpDown();
            nInput.Minimum = 1;
            nInput.Maximum = 20;
            nInput.Value = seriesTerms;
            nInput.Location = new Point(rightMargin, topMargin + 80);
            nInput.Size = new Size(controlWidth, 20);
            nInput.ValueChanged += NInput_ValueChanged;
            this.Controls.Add(nInput);

            radiusLabel = new Label();
            radiusLabel.Text = "Радиус зоны (1-30):";
            radiusLabel.Location = new Point(rightMargin, topMargin + 120);
            radiusLabel.Size = new Size(controlWidth, 20);
            this.Controls.Add(radiusLabel);

            radiusInput = new NumericUpDown();
            radiusInput.Minimum = 1;
            radiusInput.Maximum = 30;
            radiusInput.Value = (decimal)toleranceRadius;
            radiusInput.Location = new Point(rightMargin, topMargin + 150);
            radiusInput.Size = new Size(controlWidth, 20);
            radiusInput.ValueChanged += RadiusInput_ValueChanged;
            this.Controls.Add(radiusInput);

            testButton = new Button();
            testButton.Text = "Test and Save Plot";
            testButton.Location = new Point(rightMargin, topMargin + 190);
            testButton.Size = new Size(controlWidth, 30);
            testButton.Click += TestButton_Click;
            this.Controls.Add(testButton);
        }

        private void NInput_ValueChanged(object sender, EventArgs e)
        {
            seriesTerms = (int)nInput.Value;
        }

        private void RadiusInput_ValueChanged(object sender, EventArgs e)
        {
            toleranceRadius = (float)radiusInput.Value;
            drawingPanel.Invalidate();
        }

        private async void DrawButton_Click(object sender, EventArgs e)
        {
            var graphics = drawingPanel.CreateGraphics();
            graphics.Clear(Color.White);
            
            graphics.FillEllipse(Brushes.Red, startPoint.X - 5, startPoint.Y - 5, 10, 10);
            graphics.FillEllipse(Brushes.Green, endPoint.X - 5, endPoint.Y - 5, 10, 10);
            
            graphics.DrawLine(new Pen(Color.Red, 2), startPoint, endPoint);
            
            graphics.DrawEllipse(Pens.Blue, 
                endPoint.X - toleranceRadius, 
                endPoint.Y - toleranceRadius, 
                toleranceRadius * 2, 
                toleranceRadius * 2);

            float x = startPoint.X;
            float y = startPoint.Y;
            float dx = endPoint.X - startPoint.X;
            float dy = endPoint.Y - startPoint.Y;
            double angle = Math.Atan2(dy, dx);

            while (Distance(x, y, endPoint.X, endPoint.Y) > toleranceRadius)
            {
                x += stepSize * (float)TeilorCos(angle, seriesTerms);
                y += stepSize * (float)TeilorSin(angle, seriesTerms);

                graphics.FillEllipse(Brushes.Black, x - 2, y - 2, 4, 4);
                await Task.Delay(10); 
            }
        }

        private void DrawingPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillEllipse(Brushes.Red, startPoint.X - 5, startPoint.Y - 5, 10, 10);
            e.Graphics.FillEllipse(Brushes.Green, endPoint.X - 5, endPoint.Y - 5, 10, 10);
            
            e.Graphics.DrawLine(new Pen(Color.Red, 2), startPoint, endPoint);
            
            e.Graphics.DrawEllipse(Pens.Blue, 
                endPoint.X - toleranceRadius, 
                endPoint.Y - toleranceRadius, 
                toleranceRadius * 2, 
                toleranceRadius * 2);
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            string path = "../result";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            using (StreamWriter sw = new StreamWriter(Path.Combine(path, "data.txt")))
            {
                for (int radius = 1; radius <= 30; radius++)
                {
                    int minTerms = FindMinimalSeriesTerms(radius);
                    sw.WriteLine($"{radius} {minTerms}");
                }
            }

            SaveChartToFile();
            MessageBox.Show("Тестирование завершено. Данные сохранены в result/data.txt и result/plot.png");
        }

        private void SaveChartToFile()
        {
            using (Chart chart = new Chart())
            {
                chart.Size = new Size(800, 600);
                ChartArea chartArea = new ChartArea();
                chart.ChartAreas.Add(chartArea);
                
                Series series = new Series();
                series.ChartType = SeriesChartType.Point; 
                series.Color = Color.Blue;
                series.MarkerSize = 8;
                series.MarkerStyle = MarkerStyle.Circle;

                string[] lines = File.ReadAllLines("../result/data.txt");
                foreach (string line in lines)
                {
                    string[] parts = line.Split(' ');
                    float radius = float.Parse(parts[0]);
                    int terms = int.Parse(parts[1]);
                    series.Points.AddXY(radius, terms);
                }

                chart.Titles.Add("Зависимость точности (количество членов ряда) от радиуса зоны попадания");
                chart.Series.Add(series);
                chart.ChartAreas[0].AxisX.Title = "Радиус зоны попадания";
                chart.ChartAreas[0].AxisY.Title = "Минимальное количество членов ряда (n)";
                chart.ChartAreas[0].AxisX.Interval = 1;

                Series lineSeries = new Series();
                lineSeries.ChartType = SeriesChartType.Line;
                lineSeries.Color = Color.Blue;
                lineSeries.BorderWidth = 3;
                foreach (var point in series.Points)
                {
                    lineSeries.Points.AddXY(point.XValue, point.YValues[0]);
                }
                chart.Series.Add(lineSeries);

                chart.SaveImage("../result/plot.png", ChartImageFormat.Png);
            }
        }

        private int FindMinimalSeriesTerms(int radius)
        {
            for (int n = 1; n <= 20; n++)
            {
                if (TestMovementWithParameters(n, radius))
                    return n;
            }
            return 20;
        }

        private bool TestMovementWithParameters(int terms, float tolerance)
        {
            float x = startPoint.X;
            float y = startPoint.Y;
            float dx = endPoint.X - startPoint.X;
            float dy = endPoint.Y - startPoint.Y;
            double angle = Math.Atan2(dy, dx);

            while (Distance(x, y, endPoint.X, endPoint.Y) > tolerance)
            {
                x += stepSize * (float)TeilorCos(angle, terms);
                y += stepSize * (float)TeilorSin(angle, terms);

                if (Distance(x, y, startPoint.X, startPoint.Y) > 2 * Distance(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y))
                    return false;
            }

            return Distance(x, y, endPoint.X, endPoint.Y) <= tolerance;
        }

        private float Distance(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        private double TeilorSin(double x, int terms)
        {
            double result = 0;
            for (int n = 0; n < terms; n++)
            {
                result += Math.Pow(-1, n) * Math.Pow(x, 2 * n + 1) / Factorial(2 * n + 1);
            }
            return result;
        }

        private double TeilorCos(double x, int terms)
        {
            double result = 0;
            for (int n = 0; n < terms; n++)
            {
                result += Math.Pow(-1, n) * Math.Pow(x, 2 * n) / Factorial(2 * n);
            }
            return result;
        }

        private long Factorial(int n)
        {
            long result = 1;
            for (int i = 2; i <= n; i++)
                result *= i;
            return result;
        }
    }
}