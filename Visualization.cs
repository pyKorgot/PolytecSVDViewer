using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CplxPointAvgSharp
{
    public partial class Visualization : Form
    {
        private readonly string[] dir = Directory.GetFiles(@"D:\Coding\VKR\PolytecChanges\tst");
        public Visualization()
        {
            InitializeComponent();

            trackBar1.Minimum = 0;
            trackBar1.Maximum = this.dir.Length - 1;

            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            LoadImageByIndex(0);
        }

        private void LoadImageByIndex(int index)
        {
            string pathImage = this.dir[index];
            FileStream fs = File.OpenRead(pathImage);
            pictureBox1.Image = Image.FromStream(fs);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            try
            {
                int index = trackBar1.Value;
                LoadImageByIndex(index);
            } catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
