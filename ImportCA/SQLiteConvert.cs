using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace ImportCA
{
    public static class SQLite
    {
        private const string CmdCreateTable = "";

        public static string ConvertFile(string sourceFileName, char? delimiter = null, string? destFileName = null, bool overwrite = false, bool deleteFile = false)
        {
            if (!File.Exists(sourceFileName))
                throw new ArgumentException("Arquivo informado não existe.");

            return string.Empty;
        }
    }
}
