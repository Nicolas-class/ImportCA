using System.Text.Json;
using System.IO;
using System.Data.SQLite;
using FluentFTP;
using System.Text.Json.Serialization;
using FluentFTP.Exceptions;
using System.Runtime.CompilerServices;

namespace ImportCA
{
    public class ApplicationFtpService
	{

		//Formatos de arquivos suportados.
		public static readonly string[] SupportedExtensions = ".txt;.csv;.xlsx;.sqlite".Split(";");
		
		public enum ExtensionIndex : int { TXT, CSV, XLSX, SQLITE, DEFAULT = 0 };

		public static bool IsSupportedExtension(string extension)
		{
			if (string.IsNullOrWhiteSpace(extension))
				return false;

			return ApplicationFtpService.SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
		}

		//Tempo máximo para download de arquivos em servidores FTP.
		public const int FtpConnectTimeout = 10000;

		//Diretório do arquivo FTP
		private readonly static string _settingsFileName = Path.Combine(Directory.GetCurrentDirectory(), "app_settings.json");
        private readonly static string _recoveredDir = Path.Combine(Directory.GetCurrentDirectory(), "recovered");
        private readonly static string _downloadsDir = Path.Combine(Directory.GetCurrentDirectory(), "downloads");

		public static bool IsInitialized() => File.Exists(ApplicationFtpService._settingsFileName);

        public static string RecoveredFolder
        {
            get
            {
                if (!Directory.Exists(ApplicationFtpService._recoveredDir))
                    Directory.CreateDirectory(ApplicationFtpService._recoveredDir);

                return ApplicationFtpService._recoveredDir;
            }
        }

        public static string DownloadsFolder
        {
            get
            {
                if (!Directory.Exists(ApplicationFtpService._downloadsDir))
                    Directory.CreateDirectory(ApplicationFtpService._downloadsDir);
                return ApplicationFtpService._downloadsDir;
            }
		}

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


		//Obtendo informações do arquivo de configuração.
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
	}

    public class FtpSettingsJson
	{
        #region "Campos"

        private string _host = "ftp.mtps.gov.br";

        private string _hostDirectory = "portal/fiscalizacao/seguranca-e-saude-no-trabalho/caepi/";

        private string _hostFileNameContains = "tgg_export_caepi";

        private string _expectedHostFileExtension = ".zip";

        private bool _extractIfCompressed = false;

        private string _expectedInternalExtension = ".txt";

        private string _internalFileNameContains = "tgg_export_caepi";

        private string _delimiter = "|";
        #endregion

        #region "Propriedades"

        /// <summary>
        /// Nome do servidor FTP.
        /// </summary>
        [JsonPropertyName("host")]
        public string HostFtpServer { get => this._host; set => this._host = value; }

        /// <summary>
        /// Diretório do arquivo do servidor.
        /// </summary>
        [JsonPropertyName("host_directory")]
        public string HostDirectory { get => this._hostDirectory; set => this._hostDirectory = value; }

        /// <summary>
        /// Nome do arquivo salvo dentro do servidor.
        /// </summary>
        [JsonPropertyName("host_file_name")]
        public string HostFileName { get => this._hostFileNameContains; set => this._hostFileNameContains = value; }

        /// <summary>
        /// Extensão do arquivo FTP esperada pelo software.
        /// </summary>
        [JsonPropertyName("expected_host_file_extension")]
        public string ExpectedHostFileExtension { get => this._expectedHostFileExtension; set => this._expectedHostFileExtension = value; }

        /// <summary>
        /// Extrair se o arquivo estiver compactado.
        /// </summary>
        [JsonPropertyName("extract_if_compressed")]
        public bool ExtractIfCompressed { get => this._extractIfCompressed; set => this._extractIfCompressed = value; }

        /// <summary>
        /// Extensão esperada dentro do arquivo compactado.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        [JsonPropertyName("expected_internal_extension")]
        public string ExpectedInternalExtension
        {
            get => this._expectedInternalExtension;
            set => this._expectedInternalExtension = (!ApplicationFtpService.IsSupportedExtension(value)) ? 
					throw new ArgumentException($"Extensão: \"{value}\" não é suportada pelo software.") :
					value;

        }

        [JsonPropertyName("internal_file_name")]
        public string InternalFileName { get => this._internalFileNameContains; set => this._internalFileNameContains = value; }

        [JsonPropertyName("delimiter_convert")]
        public string DelimiterConvert { get => this._delimiter; set => this._delimiter = value; }

        #endregion
    }
}
