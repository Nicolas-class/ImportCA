using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using ImportCA.FtpApplication;

namespace ImportCA
{
    public static class SQLite
    {
        private const string CmdCreateTable = "";

        public static string ConvertFile(FtpSettingsJson settings, string sourceFileName, string? destFileName = null, bool overwrite = false, bool deleteSourceFile = false)
        {
			if (!File.Exists(sourceFileName))
                throw new ArgumentException("Arquivo informado não existe.");

            return string.Empty;
        }
    }
}
