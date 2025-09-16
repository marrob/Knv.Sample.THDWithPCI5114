using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Knv.Sample.THDWithPCI5114.Data
{
    public class WaveformStorage 
    {
        public WaveformCollection Waveforms { get; }
        public int LoadedTimeMs { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public WaveformStorage()
        {
            Waveforms = new WaveformCollection();
        }

        /// <summary>
        /// 
        /// </summary>
        public void LoadCsv(string path)
        {
            var csv = new CsvImport().Load(path);

            for (int column = 0; column < csv.ColumCount; column++)
            {
                var wave = new Waveform();
                var row = 0;

                for (row = 0; row < csv.RowCount; row++)
                {
                    var cell = csv.Table[row, column];

                    if (!string.IsNullOrEmpty(cell))
                    {
                        if (double.TryParse(cell, out double dummy) == false)
                        {
                            cell = cell.ToUpper();

                            if (cell.Contains("NAME"))
                                wave.Name = cell.Split('=')[1].Trim();

                            if (cell.Contains("DELTAX"))
                               wave.DeltaX = double.Parse(cell.Split('=')[1].Trim());
                        }
                        else break;
                    }
                }

                wave.YArray = new double[csv.RowCount - row];
                var yIndex = 0;

                for (; row < csv.RowCount; row++)
                {
                    var cell = csv.Table[row, column];

                    if (string.IsNullOrEmpty(cell))
                    {
                        break;
                    }
                    else if (double.TryParse(cell, out double value) == true)
                    {
                        wave.YArray[yIndex++] = value;
                    }
                    else
                    {
                        throw new InvalidCastException();
                    }
                }

                Waveforms.Add(wave);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public string SaveToCsv(string path)
        {
            List<string> lines = new List<string>();
            foreach (Waveform wave in Waveforms)
            {
                List<string> properties = new List<string>();

                if (!string.IsNullOrWhiteSpace(wave.Name))
                    properties.Add("NAME = " + wave.Name);

                if (wave.DeltaX != 0)
                    properties.Add("DELTAX = " + wave.DeltaX);

                // --- properties ---
                int row = 0;
                for (; row < properties.Count; row++)
                {
                    if (lines.ElementAtOrDefault(row) == null)
                    {
                        lines.Add(AppConstants.CsvEscape + properties[row] + AppConstants.CsvEscape);
                        if (Waveforms.Last() == wave)
                            lines[row] += AppConstants.CsvSeparator;
                    }
                    else
                    {
                        lines[row] += AppConstants.CsvSeparator;
                        lines[row] += AppConstants.CsvEscape + properties[row].ToString() + AppConstants.CsvEscape;
                        if (Waveforms.Last() == wave)
                            lines[row] += AppConstants.CsvSeparator;
                    }
                }

                // --- YArray --- 
                int data_index = 0;
                for (; data_index < wave.YArray.Length; row++, data_index++)
                {
                    if (lines.ElementAtOrDefault(row) == null)
                    {
                        lines.Add(AppConstants.CsvEscape + wave.YArray[data_index].ToString() + AppConstants.CsvEscape);
                        if (Waveforms.Last() == wave)
                            lines[row] += AppConstants.CsvSeparator;
                    }
                    else
                    {
                        lines[row] += AppConstants.CsvSeparator;
                        lines[row] += AppConstants.CsvEscape + wave.YArray[data_index].ToString() + AppConstants.CsvEscape;
                        if (Waveforms.Last() == wave)
                            lines[row] += AppConstants.CsvSeparator;
                    }
                }
            }

            if (!System.IO.File.Exists(path))
                System.IO.File.WriteAllLines(path, lines);
            else
            {
                string filename = System.IO.Path.GetFileNameWithoutExtension(path);
                string extension = System.IO.Path.GetExtension(path);
                string directory = System.IO.Path.GetDirectoryName(path);
                for (int i = 1; i < 999; i++)
                {
                    var newName = filename + "_" + i.ToString("000") ;
                    var newPath = directory + newName + extension;

                    if (!System.IO.File.Exists(newPath))
                    {
                        System.IO.File.WriteAllLines(newPath, lines);
                        return newPath;
                    }
                }
            } 
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            Waveforms.Clear();
        }

    }
}
