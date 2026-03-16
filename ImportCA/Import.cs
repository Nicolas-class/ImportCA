using System.Text.Json;
using System.Text;
using System.IO;
using System.Data.SQLite;
using FluentFTP;
using System.ComponentModel;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;



namespace ImportCA
{

    //Parâmetros de importação.
    public class ApplicationFtpService
    {

        #region "Campos"
        
        private string _remoteFtpServer = "ftp.mtps.gov.br";

        private string _remotePath = "portal/fiscalizacao/seguranca-e-saude-no-trabalho/caepi/";

        private string _remoteFileName = "tgg_export_caepi";

        private string _expectedRemoteExtension = ".zip";

        private bool _unpackIfAlreadyIs = false;

        private string _expectedInternalExtension = ".txt";

        private string _internalFileName = "tgg_export_caepi";

        private string _delimiter = "|";
        #endregion

        #region "Propriedades"

        /// <summary>
        /// Nome do servidor FTP.
        /// </summary>
        [JsonPropertyName("remote_ftp_server")]
        public string RemoteFtpServer { get => this._remoteFtpServer; set => this._remoteFtpServer = value; }
        
        /// <summary>
        /// Diretório do arquivo do servidor.
        /// </summary>
        [JsonPropertyName("remote_path")]
        public string RemotePath { get => this._remotePath; set => this._remotePath = value; }

        /// <summary>
        /// Nome do arquivo salvo dentro do servidor.
        /// </summary>
        [JsonPropertyName("remote_file_name")]
        public string RemoteFileName { get => this._remoteFileName; set => this._remoteFileName = value; }

        /// <summary>
        /// Extensão do arquivo FTP esperada pelo software.
        /// </summary>
        [JsonPropertyName("expected_remote_extension")]
        public string ExpectedRemoteExtension { get => this._expectedRemoteExtension; set => this._expectedRemoteExtension = value; }

        /// <summary>
        /// Extrair se o arquivo estiver compactado.
        /// </summary>
        [JsonPropertyName("unpack_if_already_is")]
        public bool UnpackIfAlreadyExists { get => this._unpackIfAlreadyIs; set => this._unpackIfAlreadyIs = value; }


        /// <summary>
        /// Extensão esperada dentro do arquivo compactado.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        [JsonPropertyName("expected_internal_extension")]
        public string ExpectedInternalExtension 
        { 
            get => this._expectedInternalExtension; 
            set
            {
                //Verificando se a extensão especficada é suportada pela aplicação
                if(!ApplicationFtpService.SupportedExtensions.Contains(value.ToLower()))
                {
                    throw new ArgumentException($"Extension: \"{value}\" is not supported by application.");
                }

                this._expectedInternalExtension = value;
            } 
        }

        [JsonPropertyName("internal_file_name")]
        public string InternalFileName { get => this._internalFileName; set => this._internalFileName = value; }

        [JsonPropertyName("delimiter")]
        public string Delimiter { get => this._delimiter; set => this._delimiter = value; }
        
        #endregion

        public static readonly string[] SupportedExtensions = ".csv;.xlsx;.txt;.sqlite".Split(";");

        public enum ExtensionIndex {CSV,XLSX,TXT,SQLITE};

        private readonly ILogger<ApplicationFtpService>? _logger;

        private readonly static string SettingsFileName = Path.Combine(Directory.GetCurrentDirectory(),"app_settings.Json");
        
        public ApplicationFtpService()
        {
            this._logger = NullLogger<ApplicationFtpService>.Instance;
        }

        public ApplicationFtpService(ILogger<ApplicationFtpService> logger)
        {
            this._logger = logger;
        }

        //Cria um arquivo de configuração da aplicação.
        public void Init(bool overwriteFile = true)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(this, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });

                File.WriteAllText(ApplicationFtpService.SettingsFileName,jsonContent);
            }
            catch(NotSupportedException notSupportedException)
            {
                this._logger?.LogError(notSupportedException, "Um dos campos ou propriedades contém tipos não suportados ou inválidos.");
                throw;
            }
            catch(IOException ioException)
            {
                this._logger?.LogError(ioException,"Ocorreu uma falha na escrita do arquivo de configuração.");
                throw;
            }
            catch(UnauthorizedAccessException uaException)
            {
                this._logger?.LogError(uaException,"Diretório inacessível.");
                throw;
            }
        }

        //Obtendo informações do arquivo de configuração.
        public ApplicationFtpService? Load()
        {
            //Verificando se já existe um arquivo de configuração
            if (File.Exists(ApplicationFtpService.SettingsFileName))
            {
                //Realizando a leitura e devolvendo o objeto com as informações preenchidas.
                try
                {
                    return JsonSerializer.Deserialize<ApplicationFtpService>(File.ReadAllText(ApplicationFtpService.SettingsFileName), new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch(ArgumentException argumentException)
                {
                    this._logger?.LogError(argumentException,"A extensão especificada no arquivo de configuração não é suportada pelo software.");
                    throw;
                }
                catch(JsonException jsonException)
                {
                    this._logger?.LogError(jsonException, "Ocorreu um erro durante a leitura de configuração.");
                    throw;
                }
                catch(IOException ioException)
                {
                    this._logger?.LogError(ioException, "Ocorreu um erro durante a leitura de configuração.");
                    throw;
                }
                catch(UnauthorizedAccessException uaException)
                {
                    this._logger?.LogError(uaException,"O arquivo de configuração está inacessível");
                    throw;
                }

            }
            else
            {
                throw new InvalidOperationException("O arquivo de configuração da aplicação não foi inicializado.");
            }
        }
    }

    //Importação do arquivo ftp.
    public class ImportFtpService: IDisposable
    {
        
        private ILogger<ImportFtpService>? _logger;
        private bool _disposed = false;

        public ImportFtpService()
        {
            this._logger = NullLogger<ImportFtpService>.Instance;
        }

        public ImportFtpService(ILogger<ImportFtpService> logger)
        {
            this._logger = logger;
        }

        public void Dispose()
        {
            if(this._disposed)
            {
                return;
            }
        }

    
    }

}