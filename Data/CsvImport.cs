
namespace Knv.Sample.THDWithPCI5114.Data
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Diagnostics;

    class CsvImport 
    {
        enum LineParseStates
        {
            FIND_DATAFIELD_START_ESCAPE,
            IN_DATAFIELD,
            FIND_SEPARATOR,
        };

        /// <summary>
        /// Konstructor
        /// </summary>
        public CsvImport() { }

        /// <summary>
        /// Sorok száma
        /// </summary>
        private int CalcRowCount(List<string> lines)
        {
            return lines.Count;
        }

        /// <summary>
        /// Visszadja a használt mezők számát.
        /// </summary>
        private int CalcColumnCount(List<string> lines)
        {
            var columnsCount = 0;
            for (int rowIndex = 0; rowIndex < lines.Count; rowIndex++)
            {
                int columnIndex = 0;
                var row = lines[rowIndex];
                LineParseStates state = LineParseStates.FIND_DATAFIELD_START_ESCAPE;

                for (int chIndex = 0; chIndex < row.Length; chIndex++)
                {
                    char ch = row[chIndex];
                    switch (state)
                    {
                        case LineParseStates.FIND_DATAFIELD_START_ESCAPE:
                            {
                                if (ch == AppConstants.CsvEscape  /*'\"'*/)
                                    state = LineParseStates.IN_DATAFIELD;
                                break;
                            };

                        case LineParseStates.IN_DATAFIELD:
                            {
                                if (ch == AppConstants.CsvEscape  /*'\"'*/)
                                {
                                    columnIndex++;
                                    state = LineParseStates.FIND_SEPARATOR;

                                    if (columnIndex > columnsCount)
                                        columnsCount = columnIndex;
                                }
                                break;
                            }
                        case LineParseStates.FIND_SEPARATOR:
                            {
                                if (ch == AppConstants.CsvSeparator /*','*/)
                                    state = LineParseStates.FIND_DATAFIELD_START_ESCAPE;
                                break;
                            }
                    }
                }
            }
            return columnsCount;
        }

        /// <summary>
        /// Feldoglogzza CSV fájlt
        /// </summary>
        public void Parser(List<string> lines, ref string[,] table)
        {
            var columnsCount = 0;

            for (int rowIndex = 0; rowIndex < lines.Count; rowIndex++)
            {
                int columnIndex = 0;
                var row = lines[rowIndex];
                string dataField = string.Empty;
                LineParseStates state = LineParseStates.FIND_DATAFIELD_START_ESCAPE;

                for (int chIndex = 0; chIndex < row.Length; chIndex++)
                {
                    char ch = row[chIndex];
                    switch (state)
                    {
                        case LineParseStates.FIND_DATAFIELD_START_ESCAPE:
                            {
                                if (ch == '\"')
                                    state = LineParseStates.IN_DATAFIELD;
                                break;
                            };

                        case LineParseStates.IN_DATAFIELD:
                            {
                                if (ch != '\"')
                                    dataField += row[chIndex];
                                if (ch == '\"')
                                {
                                    table[rowIndex, columnIndex] = dataField;
                                    columnIndex++;
                                    dataField = string.Empty;
                                    state = LineParseStates.FIND_SEPARATOR;

                                    if (columnIndex > columnsCount)
                                        columnsCount = columnIndex;
                                }
                                break;
                            }
                        case LineParseStates.FIND_SEPARATOR:
                            {
                                if (ch == ',')
                                {
                                    state = LineParseStates.FIND_DATAFIELD_START_ESCAPE;
                                }
                                break;
                            }
                    }
                }
            }
        }

        /// <summary>
        /// CSV Importálás
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ImportResult Load(string path)
        {
            // --- Stopper Start ---
            var stopwatch = new Stopwatch();
            stopwatch.Restart();

            // --- All read in  ---
            var lines = Read(path);

            // --- Get current row and column counts ---
            var rows = CalcRowCount(lines);
            var columns = CalcColumnCount(lines);

            // --- array allocation ---
            string[,] table = new string[rows, columns];

            // --- parsing... ---
            Parser(lines, ref table);

            // --- Stopper stop ---
            stopwatch.Stop();
#if DEBUG
            Debug.WriteLine("CsvImport-> Elapsed Time:" + stopwatch.ElapsedMilliseconds.ToString() + "ms");
#endif
            // --- Create Result ---
            return new ImportResult(rows, columns, stopwatch.ElapsedMilliseconds, table);

        }

        List<string> Read(string path)
        {
            List<string> lines = new List<string>();
            string line = null;
            StreamReader sr;
            //http://stackoverflow.com/questions/897796/how-do-i-open-an-already-opened-file-with-a-net-streamreader
            Stream s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            sr = new StreamReader(s, Encoding.ASCII);
            while ((line = sr.ReadLine()) != null)
            {
                lines.Add(line);
            }
            sr.Close();

            return lines;
        }

        public class ImportResult
        {
            public int RowCount { get; private set; }
            public int ColumCount { get; private set; }
            public long LoadedTimeMs { get; private set; }
            public string[,] Table { get; private set; }

            public ImportResult(int row, int column, long loadedTime, string[,] table)
            {
                RowCount = row;
                ColumCount = column;
                LoadedTimeMs = loadedTime;
                Table = table;
            }
        }
    }
}
