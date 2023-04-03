using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

using System.Reflection.Emit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


// do error handling of Field Separator
// Decimal Delimeter
// Grid and Axes
// Add "Completed successfully" in StatusBar
// Fill Statistics
// Add autofillable graph during loading(every 3-5%). It should show 5 channels.
// Показывать лейбл при наведении курсора
// Сделать закрывание формы FOpenFile после 100% загрузки
// Разобратся с автоматическим определением точки или запятой в переменных
// Use counterOfNaN

namespace CSV
{
    public partial class Form1 : Form
    {
        private const string V = "";
        public string firstLineInFile = V;
        public string _fileLoadPercentage = "";
        string[] firstVariables = Array.Empty<string>();
        char _sysDecimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];
        char separators = '\0';
        string filename = "";
        int counterOfReadedLines = 0;
        int countOfLinesInFile = 0;
        int counterOfNaN = 0;
        public bool stopOpeningFile = false;
        bool firstVar = true;
        bool endOfReadingFile = false;
        Dictionary<string, List<double>> variables = new Dictionary<string, List<double>>();


        public Form1()
        {
            InitializeComponent();
            openFileDialog.Filter = "CSV files(*.csv)|*.csv|Text files(*.txt)|*.txt|All files(*.*)|*.*";
        }

        async private void BtnOpen_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.Cancel)
                return;
            infoLabel.Text = "Opening CSV file...";
            // clear listbox Statistics
            lbStatistics.Items.Clear();
            CheckFirstLine3();
            if      (rbtnFieldSeparatorAuto.Checked){ ChechSeparator(); }
            else if (rbtnFieldSeparatorTab.Checked){ separators = '\t'; }
            else if (rbtnFieldSeparatorComma.Checked) { separators = ','; }
            else if (rbtnFieldSeparatorSemicolon.Checked) { separators = ';'; }
            
            //PrintSeparator();
            lbChannels.Items.Clear();
            SplitIntoVariables();
            InProgress fInProgress = new InProgress(this);
            fInProgress.Show();
            fInProgress.labelOpenFileDirection.Text = filename;
            ReadFileAsync(fInProgress);
            //ReadFile(fInProgress);
            ReturnVariables();
            for (int i = 0; i < 5; i++)
            {
                lbChannels.SetSelected(i, true);
            }

            await Task.Run(() =>
            {
                while (true)
                {
                    if (endOfReadingFile)
                    {
                        FillLbStatistics2();
                        fInProgress.Close();
                        break;
                    }
                }
            });

        }


        async void ReturnVariables()
        {
            await Task.Run(() =>
            {
                foreach (var variable in variables.Keys)
                {
                    foreach (var value in variables[variable])
                    {
                        lbStatistics.Items.Add(value.ToString());
                    }
                }
            });
        }

        bool StopOpeningFile()
        {
            return stopOpeningFile;
        }

        async void ReadFileAsync(InProgress fInProgress)
        {
            string[] line = Array.Empty<string>();
            stopOpeningFile = false;
            counterOfReadedLines = 0;
            firstVar = true;
            counterOfNaN = 0;
            variables.Clear();
            endOfReadingFile = false;

            await Task.Run(() =>
            {
                using (StreamReader stream = new StreamReader(filename))
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(stream.ReadToEnd());
                    stream.Dispose();
                    stringBuilder.Remove(0, firstLineInFile.Length + 2); // deletes empty char after clipping the first row with variables
                    line = stringBuilder.ToString().Trim().Split('\n');
                    countOfLinesInFile = line.Length;
                    fInProgress.progBarOpening.Maximum = countOfLinesInFile;
                    foreach (string item in line)
                    {
                        addingToDictionary(item, fInProgress);
                        if (StopOpeningFile())
                            break;
                    }
                    endOfReadingFile = true;
                }
            });
        }

        void ReadFile(InProgress fInProgress)
        {
            string[] line = Array.Empty<string>();
            stopOpeningFile = false;
            counterOfReadedLines = 0;
            firstVar = true;
            counterOfNaN = 0;
            variables.Clear();
            endOfReadingFile = false;

            using (StreamReader stream = new StreamReader(filename))
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(stream.ReadToEnd());
                stream.Dispose();
                stringBuilder.Remove(0, firstLineInFile.Length + 2); // deletes empty char after clipping the first row with variables
                line = stringBuilder.ToString().Trim().Split('\n');
                countOfLinesInFile = line.Length;
                fInProgress.progBarOpening.Maximum = countOfLinesInFile;
                foreach (string item in line)
                {
                    addingToDictionary(item, fInProgress);
                    if (StopOpeningFile())
                        break;
                }
                endOfReadingFile = true;
            }
        }

        void addingToDictionary(string item, InProgress fInProgress)
        {
            string[] someStr = Array.Empty<string>();

            counterOfReadedLines++;
            _fileLoadPercentage = (counterOfReadedLines * 100 / countOfLinesInFile).ToString(); //Ту стринг перенести в строку ниже, хотя может будет хуже, надо проверить
            fInProgress.gbProgressBar.Text = $"Download: {_fileLoadPercentage:#.#} %";
            fInProgress.progBarOpening.Value = counterOfReadedLines;
            label3.Text = counterOfReadedLines.ToString();
            if (firstVar)
            {
                firstVar = false;
                someStr = item.Split(separators);
                int i = 0;
                ParseFirstLines(ref someStr, firstVariables.Length, ref i);
            }
            else
            {
                someStr = item.Split(separators);
                for (int i = 0; i < firstVariables.Length; i++)
                {
                    // if count of variables in line < then count of Parametrs then
                    if (someStr.Length < firstVariables.Length)
                    {
                        ParseLines(ref someStr, someStr.Length, ref i);
                        variables[firstVariables[i]].Add(double.NaN);
                        counterOfNaN++;
                    }
                    else
                    {
                        ParseLines(ref someStr, firstVariables.Length, ref i);
                    }
                }
            }
        }

        void ParseFirstLines(ref string[] someStr, int length, ref int i)
        {
            while (i < length)
            {
                someStr[i] = someStr[i].Replace('.', ',');
                if (Double.TryParse(someStr[i], out double someDouble))
                {
                    variables.Add(firstVariables[i], AddToDict(someDouble));
                }
                else
                {
                    variables.Add(firstVariables[i], AddToDict(double.NaN));
                    counterOfNaN++;
                }
                i++;
            }
        }

        void ParseLines(ref string[] someStr, int length, ref int i)
        {
            while (i < length)
            {
                someStr[i] = someStr[i].Replace('.', ',');
                if (Double.TryParse(someStr[i], out double someDouble))
                {
                    variables[firstVariables[i]].Add(someDouble);
                }
                else
                {
                    variables[firstVariables[i]].Add(double.NaN);
                    counterOfNaN++;
                }
                i++;
            }
        }

        static List<double> AddToDict(double number)
        {
            List<double> numbers = new List<double>()
            {
                number
            };
            return numbers;
        }

        void CheckFirstLine() //650 ms
        {
            filename = openFileDialog.FileName;
            string[] readFile = File.ReadAllLines(filename);
            firstLineInFile = readFile[0];
        }

        void CheckFirstLine2() //13 ms
        {
            filename = openFileDialog.FileName;
            using(StreamReader reader = new StreamReader(filename))
            {
                firstLineInFile = reader.ReadLine() ?? "";
            }
        }

        void CheckFirstLine3() //8 ms
        {
            filename = openFileDialog.FileName;
            IEnumerable<string> readFile = File.ReadLines(filename);
            firstLineInFile = readFile.First();
        }

        void SplitIntoVariables()
        {
            firstVariables = firstLineInFile.Split(separators);
            lbChannels.Items.AddRange(firstVariables);
        }

        async void FillLbStatistics()
        {
            await Task.Run(() =>
            {
                lbStatistics.Items.Clear();
                lbStatistics.Items.Add($"File: {filename}. Total chanels: {countOfLinesInFile}");
                foreach (var item in firstVariables)
                {
                    lbStatistics.Items.Add($"Chanell: {item}. Count = {variables[item].Count}");
                }
            });
        }

        void FillLbStatistics2()
        {
            lbStatistics.Items.Clear();
            lbStatistics.Items.Add($"File: {filename}. Total chanels: {countOfLinesInFile}");
            foreach (var item in firstVariables)
            {
                lbStatistics.Items.Add($"Chanell: {item}. Count = {variables[item].Count}");
            }
        }

        void ChechSeparator()
        {
            char separator = '\0';
            for (int i = 0; i < firstLineInFile.Length; i++)
            {
                if (firstLineInFile[i] == ',')
                {
                    separator = ',';
                    separators = separator;
                    return;
                }
                else if (firstLineInFile[i] == ';')
                {
                    separator = ';';
                    separators = separator;
                    return;
                }
                else if (firstLineInFile[i] == '\t')
                {
                    separator = '\t';
                    separators = separator;
                    return;
                }
            }
            separators = separator;
        }

        void PrintSeparator()
        {
            if (separators == '\t')
            {
                MessageBox.Show("Separator = Tab", "Separator");
            }
            else if ((separators == ',') || (separators == ';'))
            {
                MessageBox.Show(String.Format(@"Separator = {0}", separators), "Separator");
            }
            else MessageBox.Show("Separator = ???", "Separator");
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            // close the form
            this.Close();
            // deactivate buttons
            //btnClose.Enabled = false;
        }

        private void LbChannels_SelectedIndexChanged(object sender, EventArgs e)
        {
            label4.Text = "";
            chart1.Series.Clear();
            foreach (var item in firstVariables)
            {
                chart1.Series.Add(item);
            }
            for (int i = 0; i < firstVariables.Length; i++)
            {
                chart1.Series[i].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline;
            }       
            foreach (string variable in lbChannels.SelectedItems)
            {
                if (variables.TryGetValue(variable, out List<double> myValue))
                {
                    foreach (double value in myValue)
                    {
                        int i = 0;
                        variables.TryGetValue(firstVariables[0], out var timix).ToString();
                        chart1.Series[variable].Points.AddXY(timix[0], value);
                        i++;
                    }
                }
            }
            // 
            //foreach (string variable in lbChannels.SelectedItems)
            //{
            //    label4.Text += (label4.Text == "" ? "" : ",") + variable.ToString();
            //}
        }

        private void CbLegend_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLegend.Checked == true)
            {
                chart1.Legends[0].Enabled = true;
            }
            else
            {
                chart1.Legends[0].Enabled = false;
            }
        }

        private void cbAxes_CheckedChanged(object sender, EventArgs e)
        {
            if (cbAxes.Checked == true)
            {
                chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = true;
            }
            else
            {
                chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            }
        }

        private void cbGrid_CheckedChanged(object sender, EventArgs e)
        {
            if (cbGrid.Checked == true)
            {
                chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = true;
            }
            else
            {
                chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            }
        }
    }
    public static class Extensions
    {
        public static string ReplaceWhiteSpaces(this string str)
        {
            char[] whitespace = new char[] { ' ', '\t', '\r', '\n' };
            //char[] whitespace = new char[] { ' ', '\r', '\n' };
            return String.Join(" ", str.Split(whitespace, StringSplitOptions.RemoveEmptyEntries));
        }
    }

}