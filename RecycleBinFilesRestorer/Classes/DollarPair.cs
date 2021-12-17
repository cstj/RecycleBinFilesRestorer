using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecycleBinFilesRestorer.Classes
{
    internal class DollarPair
    {
        public override string ToString()
        {
            return FilePairName;
        }

        public void SetFileName(string path, string filePairName)
        {
            FilePairName = filePairName;
            FilePath = path;
        }

        public string? FilePairName { get; private set; }
        public string? FilePath { get; private set; }
        public string DollarIName { get { return "$I" + FilePairName; } }
        public string DollarIFullPath { get { return System.IO.Path.Combine(FilePath, DollarIName); } }
        public string DollarRName { get { return "$R" + FilePairName; } }
        public string DollarRFullPath { get { return System.IO.Path.Combine(FilePath, DollarRName); } }


        public byte[]? header;
        public byte[]? Header {
            get
            {
                if (header == null) GetInfo();
                return header;
            }
        }

        private ulong? fileSize;
        public ulong? InfoFileSize
        {
            get
            {
                if (fileSize == null) GetInfo();
                return fileSize;
            }
        }

        private DateTime? timeStamp;
        public DateTime? InfoTimeStamp
        {
            get
            {
                if (timeStamp == null) GetInfo();
                return timeStamp;
            }
        }

        private uint? fileNameLength;
        public uint? InfoFileNameLength
        {
            get
            {
                if (fileNameLength == null) GetInfo();
                return fileNameLength;
            }
        }

        public string? InfoProperFileName
        {
            get
            {
                return System.IO.Path.GetFileName(InfoProperFilePath);
            }
        }

        private string? properFilePath;
        public string? InfoProperFilePath
        {
            get
            {
                if (properFilePath == null) GetInfo();
                return properFilePath;
            }
        }

        public void GetInfo()
        {

            //Read $I
            if (File.Exists(DollarIFullPath))
            {
                var bytes = File.ReadAllBytes(DollarIFullPath);
                //Sections
                /*
                O   S   Desc
                0   8   Header
                8   8   FileSize
                16  8   DeletedTimeStamp
                24  4   FileNameLength
                28  var FileName
                */
                var bHeader = new byte[8];
                var bFileSize = new byte[8];
                var bDetailedTiemStamp = new byte[8];
                var bFileNameLength = new byte[4];
                var bFileName = new byte[bytes.Length - (8 + 8 + 8 + 4)];

                Buffer.BlockCopy(bytes, 0, bHeader, 0, 8);
                Buffer.BlockCopy(bytes, 8, bFileSize, 0, 8);
                Buffer.BlockCopy(bytes, 16, bDetailedTiemStamp, 0, 8);
                Buffer.BlockCopy(bytes, 24, bFileNameLength, 0, 4);
                Buffer.BlockCopy(bytes, 28, bFileName, 0, bytes.Length - (8 + 8 + 8 + 4));
                /*if (BitConverter.IsLittleEndian)
                {
                    bHeader.Reverse();
                    bFileSize.Reverse();
                    bDetailedTiemStamp.Reverse();
                    bFileNameLength.Reverse().Reverse();
                    bFileName.Reverse();
                }*/
                header = bHeader;
                fileSize = BitConverter.ToUInt64(bFileSize, 0);

                var ts = BitConverter.ToInt64(bDetailedTiemStamp, 0);
                timeStamp = DateTime.FromFileTime(ts);

                fileNameLength = BitConverter.ToUInt32(bFileNameLength, 0);
                properFilePath = System.Text.Encoding.Unicode.GetString(bFileName).Trim('\0');
                
                //Cleanup
                bHeader = null;
                bFileSize = null; 
                bDetailedTiemStamp = null;
                bFileNameLength = null;
                bFileName = null;
                bytes = null;
            }
        }
    }
}
