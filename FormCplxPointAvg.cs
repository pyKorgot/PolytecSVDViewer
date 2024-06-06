using System;

//using System.Drawing;
//using System.Collections;
//using System.ComponentModel;
using System.Windows.Forms;
//using System.Data;
using System.Runtime.InteropServices; // needed for System Menu
using Polytec.Interop.PolyFile;
using Polytec.Interop.PolySignal;
/*
using Polytec.Interop.PolyProperties;
using System.Text.RegularExpressions;
using Polytec.Interop.PolySignalGenerator;
*/
using System.Collections.Generic;

using System.IO;
/*
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
*/
using System.Text.Json;
using System.Text.Json.Serialization;
using NumSharp;
using NumSharp.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;


namespace CplxPointAvgSharp
{
    public class FormCplxPointAvg : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Button SelectFile;
        private System.Windows.Forms.Button End;
        private System.Windows.Forms.Label labelExplain;

        private System.ComponentModel.Container components = null;

        public FormCplxPointAvg()
        {
            InitializeComponent();

            this.SetupSystemMenu();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormCplxPointAvg));
            this.labelExplain = new System.Windows.Forms.Label();
            this.End = new System.Windows.Forms.Button();
            this.SelectFile = new System.Windows.Forms.Button();
            this.SuspendLayout();

            this.labelExplain.Location = new System.Drawing.Point(32, 32);
            this.labelExplain.Name = "labelExplain";
            this.labelExplain.Size = new System.Drawing.Size(320, 56);
            this.labelExplain.TabIndex = 1;
            this.labelExplain.Text = "Выберете файл полученный после ультразвукового зондирования в формате .SVD.";

            this.End.Location = new System.Drawing.Point(296, 160);
            this.End.Name = "End";
            this.End.Size = new System.Drawing.Size(72, 24);
            this.End.TabIndex = 2;
            this.End.Text = "Close";
            this.End.Click += new System.EventHandler(this.End_Click);

            this.SelectFile.Location = new System.Drawing.Point(32, 96);
            this.SelectFile.Name = "SelectFile";
            this.SelectFile.Size = new System.Drawing.Size(200, 56);
            this.SelectFile.TabIndex = 3;
            this.SelectFile.Text = "Select SVD Scan File...";
            this.SelectFile.Click += new System.EventHandler(this.SelectFile_Click);

            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(384, 196);
            this.Controls.Add(this.SelectFile);
            this.Controls.Add(this.End);
            this.Controls.Add(this.labelExplain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormCplxPointAvg";
            this.Text = "Complex Point Average - Polytec File Access DEMO";
            this.ResumeLayout(false);

        }
        #endregion

        [DllImport("user32.dll")]
        private static extern int GetSystemMenu(int hwnd, int bRevert);

        [DllImport("user32.dll")]
        private static extern int AppendMenu(
            int hMenu, int Flagsw, int IDNewItem, string lpNewItem);

        private enum SystemMenuIDs
        {
            About = 1234,
        }

        private enum MenuFlags
        {
            Separator = 0xa00
        }

        private void SetupSystemMenu()
        {
            int menu = GetSystemMenu(this.Handle.ToInt32(), 0);
            AppendMenu(menu, (int)MenuFlags.Separator, 0, null);
            AppendMenu(menu, 0, (int)SystemMenuIDs.About, "About CplxPointAvg...");
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            const int WM_SYSCOMMAND = 0x112;
            if (m.Msg == WM_SYSCOMMAND)
            {
                if (m.WParam.ToInt32() == (int)SystemMenuIDs.About)
                {
                    About aboutDlg = new About();
                    aboutDlg.ShowDialog();
                }
            }
        }

        [STAThread]
        static void Main()
        {
            Application.Run(new FormCplxPointAvg());
        }

        private void SelectFile_Click(object sender, System.EventArgs eventArgs)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "ScanFiles (*.svd)|*.svd|Single Point Files (*.pvd)|*.pvd|PSV Files (*.svd; *.pvd)|*.svd; *.pvd|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.Cancel)
                return;

            PolyFileClass file = null;
            try
            {
                file = new PolyFileClass();

                file.Open(openFileDialog.FileName);
                PTCFileID fileID = file.Version.FileID;
                switch (fileID)
                {
                    case PTCFileID.ptcFileIDCombinedFile:
                    case PTCFileID.ptcFileIDPSVFile:
                    case PTCFileID.ptcFileIDVibSoftFile:
                        break;
                    default:
                        string msg = openFileDialog.FileName + (" is not an VibSoft or PSV file");
                        MessageBox.Show(msg, "File Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                }

                AcquisitionInfoModes acqInfoModes = file.Infos.AcquisitionInfoModes;

                CalcComplexAverage(file, out double[] xNet, out double[] yNet, out double[] time, out List<float[]> dt);

                MessageBox.Show("Complex Point Average data has been saved successfully", "Save data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                Application.Run(new Visualization());
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                if (file != null && file.IsOpen)
                {
                    try
                    {
                        file.Close();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                    }
                }
            }

        }
        private void CalcComplexAverage(PolyFileClass file, out double[] xNet, out double[] yNet, out double[] time, out List<float[]> dt)
        {
            this.Cursor = Cursors.WaitCursor;

            try
            {
                PointDomains pointDomains = file.GetPointDomains(PTCSignalBuildFlags.ptcBuildPointDataXYZ);

                PointDomain domain;

                domain = pointDomains.get_Type(PTCDomainType.ptcDomainTime);
                string channel = "Vib";
                string signalName = "Velocity";
                string displayName = "Samples";

                Signal signal = domain.Channels[channel].Signals[signalName];
                Display display = signal.Displays[displayName];

                Display displayReal = signal.Displays[displayName];
                SignalDescription singnalDesc = signal.Description;

                SignalXAxis xaxiss = singnalDesc.XAxis;
                SignalYAxis yaxiss = singnalDesc.YAxis;

                XAxis xaxis = domain.GetXAxis(displayReal);
                YAxes yaxis = domain.GetYAxes(displayReal);

                dt = new List<float[]>();

                time = LinSpace(xaxis.Min, xaxis.Max, xaxis.MaxCount);

                DataPoints dataPoints = domain.DataPoints;
                long pointCount = dataPoints.Count;

                FindNet(file, out xNet, out yNet);

                for (int point = 1; point <= pointCount; ++point)
                {
                    DataPoint dataPoint = dataPoints[point];
                    bool pointIsValid = true;
                    if (PTCFileID.ptcFileIDPSVFile == file.Version.FileID)
                    {
                        pointIsValid = (PTCScanStatus.ptcScanStatusValid & dataPoint.MeasPoint.ScanStatus) != 0;
                    }
                    if (!pointIsValid) continue;

                    float[] ytemp = (float[])dataPoint.GetData(displayReal, 0);

                    dt.Add(ytemp);
                }
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        public void FindNet(PolyFileClass file, out double[] xLinspace, out double[] yLinspace)
        {
            bool firstPoint = true;
            MeasPoints measpoints = file.Infos.MeasPoints;

            double maxX = new double(),
                minX = new double(),
                maxY = new double(),
                minY = new double();
            int counts = measpoints.Count;

            List<List<double>> XYZ = new List<List<double>>();
            for (int i = 1; i < counts + 1; i++)
            {
                MeasPoint measpoint = measpoints[i];
                measpoint.CoordXYZ(out double xPd, out double yPd, out double zPd);

                if (firstPoint || (maxX < xPd))
                    maxX = xPd;
                if (firstPoint || (maxY < yPd))
                    maxY = yPd;
                if (firstPoint || (minX > xPd))
                    minX = xPd;
                if (firstPoint || (minY > yPd))
                    minY = yPd;

                firstPoint = false;
                XYZ.Add(new List<double>() { xPd, yPd, zPd });
            }

            int x = 0, y = 0;
            for (int i = 2; i < counts; i++)
            {
                float t = (float)counts / i;
                if (t == (int)t)
                {
                    if (XYZ[i - 1][0] < XYZ[i][0])
                    {
                        x = i;
                        y = (int)t;
                        break;
                    }
                }
            }
            xLinspace = LinSpace(minX, maxX, x);
            yLinspace = LinSpace(minY, maxY, y);
        }

        public double[] LinSpace(double x1, double x2, int n)
        {
            if (n <= 3)
            {
                return new double[] { x1, x2 };
            }
            double step = (x2 - x1) / (n - 1);
            double[] y = new double[n];
            for (int i = 0; i < n; i++)
                y[i] = x1 + step * i;
            return y;
        }

        private void End_Click(object sender, System.EventArgs e)
        {
            Application.Exit();
        }

    }

    public class SaveJsonData
    {
        public List<float[]> dt { get; set; }
        public double[] time { get; set; }
        public double[] xNet { get; set; }
        public double[] yNet { get; set; }
    }
}