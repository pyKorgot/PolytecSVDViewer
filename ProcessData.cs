using Polytec.Interop.PolyFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CplxPointAvgSharp
{
    internal class ProcessData
    {
        public void OpenSelectableFile(OpenFileDialog openFileDialog)
        {
            PolyFileClass file = null;
            file = new PolyFileClass();

            file.Open(openFileDialog.FileName);

            string fileName = openFileDialog.FileName;

        }
        public bool CheckValidOpenFile(ref PolyFileClass file)
        {
            PTCFileID fileID = file.Version.FileID;
            switch (fileID)
            {
                case PTCFileID.ptcFileIDCombinedFile:
                case PTCFileID.ptcFileIDPSVFile:
                case PTCFileID.ptcFileIDVibSoftFile:
                    return true;
                default:
                    //string msg = fileName + (" is not an VibSoft or PSV file");
                    //MessageBox.Show(msg, "File Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
            }
        }
    }
}
