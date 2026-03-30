using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using ImportCA.FtpApplication;
using ImportCA.FtpFileManagement;

namespace ImportCA.FtpConvert
{
    public static class ConvertFtpService
    {
        private const string CmdCreateTable = "";

        public enum FileToConvert { SQLite, CSV, Text, Json }

        public static string ConvertFile(FtpSettingsJson settings, string sourceFileName, string? destDirectory = null, string? destFileName = null, FileToConvert filetype = FileToConvert.SQLite, bool overwrite = false, bool deleteSourceFile = false)
        {
            //Verificando se o arquivo existe.
            if (!File.Exists(sourceFileName))
            {
				throw new FileNotFoundException($"O arquivo informado \"{sourceFileName}\" não foi localizado.");
			}

			//Criando o caminho do arquivo de destino.
			string fullPath = ManagementFileFtpService.SolvePath(Path.GetFileNameWithoutExtension(sourceFileName), ".sqlite",
                                                                    destDirectory ?? ApplicationFtpService.GetApplicationDirectory(ApplicationFtpService.DirectoryName.Converted),
                                                                    destFileName);

        }
    }
}
