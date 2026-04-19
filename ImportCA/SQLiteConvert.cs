using System.Data.SQLite;
using ImportCA.FtpApplication;
using ImportCA.FtpFileManagement;

namespace ImportCA.FtpConvert
{
    public static class ConvertFtpService
    {
        private const string CmdCreateTable = "";

        public enum FileToConvert { SQLite, CSV, Text, Json }

        public static string ConvertFile(FtpSettingsJson settings, string sourceFileName, string? destDirectory = null, string? destFileName = null, string? dataBaseName = null, FileToConvert filetype = FileToConvert.SQLite, bool overwrite = false, bool deleteSourceFile = false)
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
                                                                    
            using var manageFileFtp = new ManagementFileFtpService();

            //Criando uma pasta temporária para realizar as devidas operaeções
            manageFileFtp.CreateTempFolder();

            //Criando um arquivo de banco de dados dentro da pasta temporária
            string dbfile = Path.Combine(manageFileFtp.TempFolderPath,$"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.sqlite");
            SQLiteConnection.CreateFile(dbfile);

            //
            using(var sqliteConnection = new SQLiteConnection($@"Data Source={dbfile};Version=3"))
            {
                using(var createCmd = new SQLiteCommand(sqliteConnection))
                {
                    
                }
            }
            
            return string.Empty;
        }


    }
}
