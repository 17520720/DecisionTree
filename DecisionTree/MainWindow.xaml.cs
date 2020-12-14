using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Excel = Microsoft.Office.Interop.Excel;
using System.Threading;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DecisionTree
{
    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate () { };
        

    }
    public class Node
    {
        public string nodeName;
        public List<String> linkName;
        public List<Node> listEndNode;

        public Node()
        {
            linkName = new List<String>();
            listEndNode = new List<Node>();
        }
        public Node(string link)
        {
            linkName = new List<String>();
            listEndNode = new List<Node>();
        }
    }

    public class Tree
    {
        public Node initNode;
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Properties
        private bool isAutomatic = true;
        private string _excelFileName;
        private object[,] valueArray;
        private string outputState;
        private string _resultOfTree;
        private List<TextBox> _listInputTextBox;
        private Label _labelResult;
        private StackPanel _spInput;
        private Canvas _csDrawArea;

        private static System.Timers.Timer aTimer;
        private int pauseTime = 5;

        private int _tickRate;

        private Tree nodeTree;

        public void SetTimer()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += new EventHandler(dispatcherTimer_Tick);
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Start();
        }

        public string OutputState {
            get { return outputState; }
            set
            {
                outputState = value;
                NotifyPropertyChanged("OutputState");
            }
        }

        #endregion
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            _tickRate = 0;
            nodeTree = new Tree();
            nodeTree.initNode = new Node();
            _resultOfTree = "";
            _listInputTextBox = new List<TextBox>();

            _spInput = new StackPanel();
            _spInput.HorizontalAlignment = HorizontalAlignment.Left;
            _spInput.VerticalAlignment = VerticalAlignment.Center;
            _spInput.Margin = new Thickness(8, 50, 0, 0);
            _spInput.Width = Width / 3;

            _csDrawArea = new Canvas();
            _csDrawArea.HorizontalAlignment = HorizontalAlignment.Left;
            _csDrawArea.VerticalAlignment = VerticalAlignment.Top;
            _csDrawArea.Margin = new Thickness(270, 120, 0, 0);
            _csDrawArea.Background = Brushes.Red;
            _csDrawArea.Width = 630;

            gridLayout.Children.Add(_spInput);
            gridLayout.Children.Add(_csDrawArea);

            btnCreate.IsEnabled = false;

            OutputState = "...";
            SetTimer();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            _tickRate += 1;
            //lbOutput.Content = _tickRate.ToString();
        }

        public async void ChangeUI()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            lbOutput.Content = OutputState;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        protected void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.DynamicInvoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //////////////////////////////EVENT/////////////////////////////////
        #region Events
        private void DockPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Grid_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void txtboxDelayTime_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void txtAuto_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isAutomatic = true;
            //txtblockAuto.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 69, 209, 36));
            //txtblockHand.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 0, 0));
        }

        private void txtHand_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isAutomatic = false;
            //txtblockHand.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 69, 209, 36));
            //txtblockAuto.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 0, 0));
        }
        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Excel File (*.xlsx, *xls)|*.xls;*.xlsx";

            if (openFileDialog.ShowDialog() == true)
            {
                btnChooseFile.Content = openFileDialog.SafeFileName;
                _excelFileName = openFileDialog.FileName;
            }

            ReadFileExcel();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            OutputState = "Entrophy bảng H({S})= " + calculateEntrophyTable(valueArray).ToString();

            nodeTree.initNode = null;
            nodeTree.initNode = new Node();

            calculateEntrophyF(valueArray, nodeTree.initNode);
            generateInputControl();

            _csDrawArea.Children.Clear();
            drawTree();

            btnResult.IsEnabled = true;
        }

        private void txtblockNextStep_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!isAutomatic)
            {
                //Enter code here
            }
        }

        private void btnResult_Click(object sender, RoutedEventArgs e)
        {
            //Predict
            calculateResult();
            _labelResult.Content = _resultOfTree;
            Console.WriteLine(_resultOfTree);
        }
        #endregion

        //////////////////////////////METHOD////////////////////////////////
        #region Methods
        public void ReadFileExcel()
        {
            if (!System.IO.File.Exists(_excelFileName))
            {
                Console.WriteLine("Dường dẫn không chinh xác!");
            }
            else
            {
                Console.WriteLine("YEAH");

                try
                {
                    Excel.Application xlApp = new Excel.Application();
                    Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(_excelFileName);
                    //sheet1
                    Excel.Worksheet xlWorksheet = (Excel.Worksheet)xlWorkbook.Sheets.get_Item(1);

                    //pham vi can lay du lieu
                    Excel.Range xlRange = xlWorksheet.UsedRange;

                    //tạo mảng lưu dữ liệu 
                    valueArray = (object[,])xlRange.get_Value(Excel.XlRangeValueDataType.xlRangeValueDefault);

                    //hiển thị dữ liệu 
                    //for (int row = 1; row <= xlRange.Rows.Count; row++)
                    //{
                    //    for (int col = 1; col <= xlRange.Columns.Count; col++)
                    //    {
                    //        Console.Write(valueArray[row, col].ToString() + " ");
                    //    }
                    //    Console.WriteLine();
                    //}

                    //Đóng Workbook & ung dung
                    xlWorkbook.Close(false);
                    xlApp.Quit();

                    //GIải phóng service
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(xlWorkbook);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(xlApp);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    OutputState = "";
                }

                btnCreate.IsEnabled = true;
                OutputState = "Đã đọc file xong!";
            }
        }

        //Hàm tạo input control de nhap du lieu
        public void generateInputControl()
        {
            int initX = 0;
            int initY = 0;
            _spInput.Children.Clear();
            _listInputTextBox.Clear();

            for (int col = 1; col < valueArray.GetLength(1); col++)
            {
                //tao label 
                Label newLabel = new Label();
                newLabel.Content = valueArray[1, col].ToString();
                newLabel.HorizontalAlignment = HorizontalAlignment.Left;
                newLabel.VerticalAlignment = VerticalAlignment.Top;
                newLabel.Margin = new Thickness(initX, initY , 0, 0);
                newLabel.FontSize = 16;
                newLabel.Foreground = Brushes.White;

                //tao textbox
                TextBox newTextBox = new TextBox();
                newTextBox.Name = valueArray[1, col].ToString();
                newTextBox.FontSize = 16;
                newTextBox.Height = 32;
                newTextBox.Width = 90;
                newTextBox.HorizontalAlignment = HorizontalAlignment.Left;
                newTextBox.VerticalAlignment = VerticalAlignment.Top;
                newTextBox.Padding = new Thickness(5, 5 , 0, 0);
                newTextBox.Margin = new Thickness(initX + 150, initY - 26, 0, 0);

                _listInputTextBox.Add(newTextBox);
                _spInput.Children.Add(newLabel);
                _spInput.Children.Add(newTextBox);

            }

            Label resultLabel = new Label();
            resultLabel.Name = "lbResult";
            resultLabel.Content = "Result";
            resultLabel.HorizontalAlignment = HorizontalAlignment.Left;
            resultLabel.VerticalAlignment = VerticalAlignment.Top;
            resultLabel.Margin = new Thickness(initX + 28, initY, 0, 0);
            resultLabel.FontSize = 20;
            resultLabel.FontWeight = FontWeights.SemiBold;
            resultLabel.Foreground = Brushes.GreenYellow;

            _labelResult = resultLabel;
            _spInput.Children.Add(resultLabel);
        }

        //Hàm tính Entrophy cho bảng
        public double calculateEntrophyTable(object[,] valueArray)
        {
            var mapResult = new Dictionary<string, int>();
            int numberOfValue = valueArray.GetLength(0) - 1;
            double entrophy = 0.00f;

            //Bảng tính excel tính từ 1 dòng số 2 là tiêu đề
            int[] hadCount = new int[valueArray.GetLength(0) + 1];
            hadCount[0] = -1;
            hadCount[1] = -1;
            hadCount[2] = 0;

            for (int row = 2; row <= valueArray.GetLength(0); row++)
            {
                int count = 1;
                for (int final_row = row + 1; final_row <= valueArray.GetLength(0); final_row++)
                {
                    if (valueArray[final_row, valueArray.GetLength(1)].ToString() == valueArray[row, valueArray.GetLength(1)].ToString())
                    {
                        count++;
                        hadCount[final_row] = -1;
                    }
                }

                if (hadCount[row] != -1)
                {
                    hadCount[row] = count;
                    mapResult.Add(valueArray[row, valueArray.GetLength(1)].ToString(), count);
                }
            }

            //calculate
            foreach (var item in mapResult)
            {
                entrophy += -(item.Value / (double)numberOfValue) * Math.Log(item.Value / (double)numberOfValue);
            }

            return entrophy;
        }

        public void calculateEntrophyF(object[,] valueArray, Node node)
        {
            if (calculateEntrophyTable(valueArray) == 0) {
                node.nodeName = valueArray[2, valueArray.GetLength(1)].ToString();

                return;
            };

            int minEntrophyCol = 1;
            double minEntrophyMetric = 0;
            List<string> arrRowExistTemp = new List<string>();
            List<string> arrRowExist = new List<string>();

            if (valueArray.GetLength(0) <= 2 || valueArray[2, 1] == null) return;

            for (int col = 1; col < valueArray.GetLength(1); col++)
            {
                string arrExist = "";
                double entrophySum = 0.00f;
                int numberOfRowValue = valueArray.GetLength(0) - 1;

                for (int row = 2; row <= valueArray.GetLength(0); row++)
                {
                    if (!arrExist.Contains(valueArray[row, col].ToString()))
                    {
                        arrExist += valueArray[row, col].ToString() + ", ";

                        var mapResult = new Dictionary<string, int>();
                        int numberOfValue = 0;
                        double entrophy = 0.00f;

                        //Bảng tính excel tính từ 1 dòng số 2 là tiêu đề
                        int[] hadCount = new int[valueArray.GetLength(0) + 1];
                        hadCount[0] = -1;
                        hadCount[1] = -1;
                        hadCount[2] = 0;

                        for (int second_row = row; second_row <= valueArray.GetLength(0); second_row++)
                        {
                            if (valueArray[second_row, col].ToString() == valueArray[row, col].ToString())
                            {
                                int count = 1;
                                for (int final_row = second_row + 1; final_row <= valueArray.GetLength(0); final_row++)
                                {
                                    if (valueArray[final_row, valueArray.GetLength(1)].ToString() == valueArray[second_row, valueArray.GetLength(1)].ToString()
                                        && valueArray[final_row, col].ToString() == valueArray[row, col].ToString())
                                    {
                                        count++;
                                        hadCount[final_row] = -1;
                                    }
                                }

                                if (hadCount[second_row] != -1)
                                {
                                    hadCount[second_row] = count;
                                    mapResult.Add(valueArray[second_row, valueArray.GetLength(1)].ToString(), count);
                                    //Console.WriteLine(valueArray[second_row, valueArray.GetLength(1)].ToString() + count);
                                }

                                numberOfValue++;
                            }
                        }

                        //calculate
                        foreach (var item in mapResult)
                        {
                            entrophy += -(item.Value / (double)numberOfValue) * Math.Log(item.Value / (double)numberOfValue);
                        }

                        //entrophy outlook temperature,...
                        Console.WriteLine(entrophy + valueArray[row, col].ToString());
                        entrophySum += (numberOfValue / (double)numberOfRowValue) * entrophy;

                        arrRowExistTemp.Add(valueArray[row, col].ToString());
                    }
                }
                Console.WriteLine("ENTROPHY " + valueArray[1, col].ToString() + entrophySum);

                if (col == 1) minEntrophyMetric = entrophySum;

                if (entrophySum <= minEntrophyMetric)
                {
                    minEntrophyCol = col;
                    minEntrophyMetric = entrophySum;
                    arrRowExist.Clear();
                    
                    foreach (var item in arrRowExistTemp)
                    {
                        arrRowExist.Add(item);
                    }
                }

                arrRowExistTemp.Clear();

                Console.WriteLine("ENTROPHY MIN " + minEntrophyMetric + "   COL CONTENT " + valueArray[1, minEntrophyCol] + " Min_COL " + minEntrophyCol);

                OutputState = "ENTROPHY " + valueArray[1, col].ToString() + " = " + entrophySum;
                //lbOutput.Content = OutputState;
            }

            //Tao node moi cho cay
            node.nodeName = valueArray[1, minEntrophyCol].ToString();
            Console.WriteLine("____________" + node.nodeName);

            foreach (var item in arrRowExist)
            {
                //them link cho node
                node.linkName.Add(item);
                Console.WriteLine("_________" + node.nodeName +"_______LINK " + item);

                Node temp = new Node();
                node.listEndNode.Add(temp);

                int[] lowerBounds = { 1, 1 };
                int[] lengthsOfTemp = { valueArray.GetLength(0), valueArray.GetLength(1) };
                object[,] newArrFormRow = (object[,])Array.CreateInstance(typeof(object), lengthsOfTemp, lowerBounds);
                int numberNewCol = 1;
                int numberNewRow = 1;

                for (int col = 1, newArrCol = 1; col <= valueArray.GetLength(1); col++)
                {
                    if (col == minEntrophyCol) continue;

                    for (int row = 1, newArrRow = 1; row <= valueArray.GetLength(0); row++)
                    {
                        if (valueArray[row, minEntrophyCol].ToString() != item && row != 1) continue;

                        newArrFormRow[newArrRow, newArrCol] = valueArray[row, col];
                        newArrRow++;
                        numberNewRow = newArrRow;
                    }
                    newArrCol++;
                    numberNewCol = newArrCol;
                }

                //Console.WriteLine(" NEWARRAY " + newArrFormRow[1, 1]);

                int[] lengths = { numberNewRow - 1, numberNewCol - 1};
                object[,] newArrFixed = (object[,])Array.CreateInstance(typeof(object), lengths, lowerBounds);

                for (int col = 1; col < numberNewCol; col++)
                {
                    for (int row = 1; row < numberNewRow; row++)
                    {
                        newArrFixed[row, col] = newArrFormRow[row, col];
                    }
                }
                //Console.WriteLine("NEWARRAY " + newArrFixed[1, 1]);
                newArrFixed[1, 1] = newArrFormRow[1, 1];

                Console.WriteLine("------------------------------------");
                calculateEntrophyF(newArrFixed, temp);
            }
            //So sánh H của thuộc tính (cột) chọn 1 thuộc tính làm node
            //ROW Exist tất cả thuộc tính trong một cột
            //Các giá trị cần quan tâm là  minEntrophyCol, minEntrophyMetric, arrRowExist
            //mang duoc trích xuất là newArrFixed
        }


        public void calculateResult()
        {
            List<string> listTitle = new List<string>();
            var json_data_raw = new
            {
                //outlook = "sunny",
                //temperatrue = "hot",
                //humidity = "normal",
                //wind = "weak",
            };
            string json_data = JsonConvert.SerializeObject(json_data_raw);
            //Parse the json object 
            JObject json_object = JObject.Parse(json_data);

            for (int col = 1; col < valueArray.GetLength(1); col++)
            {
                json_object[valueArray[1, col].ToString()] = _listInputTextBox[col - 1].Text;
            }

            //sử dụng json_object đưa vào tree
            //Đưa json vào trong tree 
            recursionFindResult(json_object, nodeTree.initNode);
            //Dò node đầu tiên, 
        }

        public void recursionFindResult(JObject jObject, Node node)
        {
            if (node.linkName.Count <= 0)
            {
                _resultOfTree = node.nodeName;
            }  
            else
            {
                foreach (var item in node.linkName)
                {
                    if (item == jObject[node.nodeName].ToString())
                    {
                        recursionFindResult(jObject, node.listEndNode[node.linkName.FindIndex(str => str == item)]);
                    }
                }
            }
        }

        public void drawTree()
        {
            int initX = 265;
            int initY = 0;

            recursionDrawTree(nodeTree.initNode, initX, initY);
        }

        public void recursionDrawTree(Node node, int x, int y)
        {
            string nameOfNode = node.nodeName;

            Button nodeBtn = new Button();
            nodeBtn.Content = nameOfNode;
            nodeBtn.FontSize = 16;
            nodeBtn.Foreground = Brushes.Black;
            nodeBtn.Background = Brushes.LightGray;
            nodeBtn.FontWeight = FontWeights.SemiBold;
            nodeBtn.HorizontalAlignment = HorizontalAlignment.Left;
            nodeBtn.VerticalAlignment = VerticalAlignment.Top;
            nodeBtn.Padding = new Thickness(12, 0, 12, 0);
            nodeBtn.Height = 34;
            nodeBtn.Width = 100;
            nodeBtn.Margin = new Thickness(x, y, 0, 0);

            _csDrawArea.Children.Add(nodeBtn);

            if (node.linkName.Count <= 0)
            {
                return;
            }

            int cacheGap = 0;
            for (int i = 0; i < node.linkName.Count; i++)
            {
                double x1 = x + nodeBtn.Width / 2;
                double y1 = y + 34;
                double x2 = x - 40 + cacheGap;
                double y2 = y + 90;
                Line linkLine = new Line();

                linkLine.Stroke = System.Windows.Media.Brushes.GreenYellow;
                linkLine.X1 = x1;
                linkLine.Y1 = y1;
                linkLine.X2 = x2;
                linkLine.Y2 = y2;
                linkLine.HorizontalAlignment = HorizontalAlignment.Left;
                linkLine.VerticalAlignment = VerticalAlignment.Top;
                linkLine.StrokeThickness = 2;

                _csDrawArea.Children.Add(linkLine);

                Label linkLabel = new Label();
                linkLabel.Foreground = Brushes.GhostWhite;
                linkLabel.Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
                linkLabel.FontSize = 14;
                linkLabel.FontWeight = FontWeights.Bold;
                linkLabel.HorizontalAlignment = HorizontalAlignment.Left;
                linkLabel.VerticalAlignment = VerticalAlignment.Top;
                linkLabel.Content = node.linkName[i];
                linkLabel.Margin = new Thickness(x2 - 45, y2 - 30, 0, 0);

                _csDrawArea.Children.Add(linkLabel);

                recursionDrawTree(node.listEndNode[i], (int)linkLine.X2 - 50, (int)linkLine.Y2);

                cacheGap += 120;
            }
        }
        #endregion
    }
}
