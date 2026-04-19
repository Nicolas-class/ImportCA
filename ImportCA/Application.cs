using ImportCA.FtpFileManagement;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImportCA.FtpApplication
{
    public static class ApplicationFtpService
	{
		//Formatos de arquivos suportados.
		public static readonly string[] SupportedExtensions = ".txt;.csv;.json;.sqlite".Split(";");

		public enum ExtensionIndex : int { TXT, CSV, JSON, SQLITE, DEFAULT = 0 };

        public enum DirectoryName { Recovered, Downloads, Converted }

		//Verifica se a extensão do arquivo é suportada pelo software.
		public static bool IsSupportedExtension(string extension) =>
            !string.IsNullOrWhiteSpace(extension) && ApplicationFtpService.SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

		//Tempo máximo para download de arquivos em servidores FTP.
		public const int FtpConnectTimeout = 10000;

		//Credencial padrão para login servidor ftp.
		internal const string DefaultCredentials = "anonymous";

		//Diretório do arquivo FTP
		private readonly static string _settingsFileName = Path.Combine(Directory.GetCurrentDirectory(), "app_settings.json");
        private readonly static string _recoveredDir = Path.Combine(Directory.GetCurrentDirectory(), "recovered");
        private readonly static string _downloadsDir = Path.Combine(Directory.GetCurrentDirectory(), "downloads");
        private readonly static string _convertedDir = Path.Combine(Directory.GetCurrentDirectory(), "converted");

		/// <summary>
		/// //Verifica se o arquivo de configuração foi inicializado.
		/// </summary>
		/// <returns>true se o arquivo existir, false caso contrário.</returns>
		public static bool IsInitialized() => File.Exists(ApplicationFtpService._settingsFileName);

		/// <summary>
		/// //Pasta de recuperação de arquivos, caso haja falha no processo de manipulação no arquivo baixado do servidor.
		/// </summary>
		public static string RecoveredFolder
        {
            get
            {
                if (!Directory.Exists(ApplicationFtpService._recoveredDir))
                {
					Directory.CreateDirectory(ApplicationFtpService._recoveredDir);
				}

                return ApplicationFtpService._recoveredDir;
            }
        }

		/// <summary>
		/// //Pasta de downloads padrão, caso não seja especificada outra pasta para download do arquivo do servidor FTP.
		/// </summary>
		public static string DownloadsFolder
        {
            get
            {
                if (!Directory.Exists(ApplicationFtpService._downloadsDir))
                {
					Directory.CreateDirectory(ApplicationFtpService._downloadsDir);
				}

                return ApplicationFtpService._downloadsDir;
            }
		}

        //Cria um diretório automaticamente caso não existir.
        private static string AutoCreateDirectory(string directory)
        {
            if (ManagementFileFtpService.HasInvalidChars(directory, ManagementFileFtpService.CheckInvalidChars.path))
            {
                throw new ArgumentException("Caminho informado contém carácteres inválidos.");
            }

            if (!Directory.Exists(directory) && File.Exists(directory))
            {
                throw new ArgumentException("Caminho informado não é um diretório.");
            }
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        public static string GetApplicationDirectory(ApplicationFtpService.DirectoryName folderGet) => folderGet switch
        {
            DirectoryName.Recovered => AutoCreateDirectory(ApplicationFtpService._recoveredDir),
			DirectoryName.Converted => AutoCreateDirectory(ApplicationFtpService._convertedDir),
			DirectoryName.Downloads => AutoCreateDirectory(ApplicationFtpService._downloadsDir),
		};

		/// <summary>
		/// Cria um arquivo de configuração da aplicação.
		/// </summary>
		/// <param name="overwriteFile">
		/// Especificar se deve ou não sobreescrever um arquivo com o mesmo nome caso existir.
		/// </param>
		public static void Init(bool overwriteFile = true)
		{
			string jsonContent = JsonSerializer.Serialize(new FtpSettingsJson(), new JsonSerializerOptions()
			{
				WriteIndented = true,
                PropertyNameCaseInsensitive = true
			});

			File.WriteAllText(ApplicationFtpService._settingsFileName, jsonContent);

		}


		/// <summary>
		/// Obtém as informações do arquivo de configuração da aplicação e as desserializa para um objeto do tipo FtpSettingsJson.
		/// </summary>
		/// <returns><seealso cref="FtpSettingsJson"/></returns>
		/// <exception cref="InvalidOperationException">
        /// Caso arquivo de configuração não tenha sido inicializado.
        /// </exception>
		public static FtpSettingsJson GetJson()
		{
			//Verificando se já existe um arquivo de configuração
			if (!File.Exists(ApplicationFtpService._settingsFileName))
				throw new InvalidOperationException("O arquivo de configuração da aplicação não foi inicializado antes do uso.");

			return JsonSerializer.Deserialize<FtpSettingsJson>(File.ReadAllText(ApplicationFtpService._settingsFileName), new JsonSerializerOptions()
			{
				PropertyNameCaseInsensitive = true
			}) ?? 
                throw new InvalidOperationException("Falha ao carregar as configurações.");

		}

		/// <summary>
		/// Abre o arquivo json.
		/// </summary>
		/// <exception cref="InvalidOperationException">O arquivo não tenha sido inicializado usando o método <seealso cref="Init(bool)"/>.</exception>
		public static void OpenJson()
        {
            //Lança exceção se o arquivo de configuração não foi inicializado.
            if (!ApplicationFtpService.IsInitialized())
            {
                throw new InvalidOperationException("Settings file is not initialized. Please initialize the settings file before trying to open it.");
            }

            Process.Start(ApplicationFtpService._settingsFileName);
        }
	}

    public class FtpSettingsJson
	{
        #region "Campos"

        private string _host = "ftp.mtps.gov.br";

        private string _username = ApplicationFtpService.DefaultCredentials;

        private string _hostDirectory = "portal/fiscalizacao/seguranca-e-saude-no-trabalho/caepi/";

        private string _hostFileNameContains = "tgg_export_caepi";

        private string _expectedHostFileExtension = ".zip";

        private string _expectedInternalExtension = ".txt";

        private string _internalFileNameContains = "tgg_export_caepi";

        private char _txtDelimiter = '|';

        private char _csvDelimiter = ',';
		#endregion

		#region "Propriedades"

		/// <summary>
		/// Nome do servidor FTP.
		/// </summary>
		[JsonPropertyName("host")]
        public string HostFtpServer 
        { 
            get => this._host; 
            set => this._host = value; 
        }

		[JsonPropertyName("username")]
		public string UserName
		{
			get => this._username;
            set => this._username = (string.IsNullOrEmpty(value)) ? ApplicationFtpService.DefaultCredentials : value;
		}

		/// <summary>
		/// Diretório do arquivo do servidor.
		/// </summary>
		[JsonPropertyName("host_directory")]
        public string HostDirectory 
        { 
            get => this._hostDirectory; 
            set => this._hostDirectory = value; 
        }

        /// <summary>
        /// Nome do arquivo salvo dentro do servidor.
        /// </summary>
        [JsonPropertyName("host_file_name")]
        public string HostFileName { get => this._hostFileNameContains; set => this._hostFileNameContains = value; }

        /// <summary>
        /// Extensão do arquivo FTP esperada pelo software.
        /// </summary>
        [JsonPropertyName("expected_host_file_extension")]
        public string ExpectedHostFileExtension 
        { 
            get => this._expectedHostFileExtension; 
            set => this._expectedHostFileExtension = value; 
        }

        /// <summary>
        /// Extensão esperada dentro do arquivo compactado.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        [JsonPropertyName("expected_internal_file_extension")]
        public string ExpectedInternalExtension
        {
            get => this._expectedInternalExtension;
            set => this._expectedInternalExtension = (!ApplicationFtpService.IsSupportedExtension(value)) ? 
					throw new ArgumentException($"Extensão: \"{value}\" não é suportada pelo software.") :
					value;
        }

        /// <summary>
        /// Nome do arquivo esperado dentro do arquivo compactado.
        /// </summary>
        [JsonPropertyName("internal_file_name_contains")]
        public string InternalFileNameContains { get => this._internalFileNameContains; set => this._internalFileNameContains = value; }

        /// <summary>
        /// Delimitador padrão para conversão de arquivo.
        /// </summary>
        [JsonPropertyName("txt_delimiter_convert")]
        public char TxtDelimiter
        {
            get => this._txtDelimiter;
            set => this._txtDelimiter = (char.IsWhiteSpace(value)) ? 
                    throw new ArgumentException("O delimitador para conversão de arquivos CSV não pode ser um caractere de espaço em branco.") :
				    value;
		}


        [JsonPropertyName("csv_delimiter_convert")]
        public char CsvDelimiter
        {
            get => this._csvDelimiter;
            set => this._csvDelimiter = (char.IsWhiteSpace(value)) ? 
                    throw new ArgumentException("O delimitador para conversão de arquivos de texto não pode ser um caractere de espaço em branco.") :
                     this._csvDelimiter = value;

		}
		#endregion
	}
}
