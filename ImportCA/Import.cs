using System.Text.Json;
using System.IO;
using System.Data.SQLite;
using FluentFTP;
using System.Text.Json.Serialization;
using FluentFTP.Exceptions;
using System.Data;

namespace ImportCA
{

    //Parâmetros de importação.
    public class FtpImportSettings
    {

        #region "Campos"
        
        private string _host = "ftp.mtps.gov.br";

        private string _remoteDirectory = "portal/fiscalizacao/seguranca-e-saude-no-trabalho/caepi/";

        private string _remoteFileName = "tgg_export_caepi";

        private string _expectedRemoteExtension = ".zip";

        private bool _extractIfCompressed = false;

        private string _expectedInternalExtension = ".txt";

        private string _internalFileName = "tgg_export_caepi";

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
        [JsonPropertyName("remote_directory")]
        public string RemoteDirectory { get => this._remoteDirectory; set => this._remoteDirectory = value; }

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
            set
            {
                //Verificando se a extensão especficada é suportada pela aplicação
                if(!FtpImportSettings.SupportedExtensions.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"Extensão: \"{value}\" não é suportada pelo software.");
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

        public enum ExtensionIndex : int {CSV,XLSX,TXT,SQLITE,DEFAULT = 0};

        private readonly static string SettingsFileName = Path.Combine(Directory.GetCurrentDirectory(),"app_settings.Json");

        /// <summary>
        /// Cria um arquivo de configuração da aplicação.
        /// </summary>
        /// <param name="overwriteFile">
        /// Especificar se deve ou não sobreescrever um arquivo com o mesmo nome caso existir.
        /// </param>
        public static void Init(bool overwriteFile = true)
        {
            string jsonContent = JsonSerializer.Serialize(new FtpImportSettings(), new JsonSerializerOptions()
            {
                WriteIndented = true
            });
            
            File.WriteAllText(FtpImportSettings.SettingsFileName,jsonContent);
        }

        //Obtendo informações do arquivo de configuração.
        public static FtpImportSettings GetJson()
        {
            //Verificando se já existe um arquivo de configuração
            if (!File.Exists(FtpImportSettings.SettingsFileName))
            {
                throw new InvalidOperationException("O arquivo de configuração da aplicação não foi inicializado antes do uso.");
            }
                
            return JsonSerializer.Deserialize<FtpImportSettings>(File.ReadAllText(FtpImportSettings.SettingsFileName), new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Falha ao carregar as configurações.");
            
        }
    }

    //Importação do arquivo ftp.
    public class ImportFtpService: IDisposable
    {   
        
        private const string DefaultCredentials = "anonymous";
        private bool _disposed = false;
        private string _tmpFolderPath = string.Empty;
        
        public void Dispose()
        {
            if(this._disposed)
            {
                return;
            }

            DeleteTempFolder();
        }

        private void CreateTempFolder()
        {
            DeleteTempFolder();

            var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            this._tmpFolderPath = dir.FullName;
        }

        private void DeleteTempFolder()
        {
            if (!Directory.Exists(this._tmpFolderPath))
            {
                return;
            }

            try
            {
                Directory.Delete(this._tmpFolderPath);
            } 
            catch
            {
                this._tmpFolderPath = string.Empty;
            }
        }

        //Verifica se o diretório ou caminho do arquivo informado, contém caractéres inválidos.
        public static bool PathHasInvalidChars(string path)
        {
            return (string.IsNullOrEmpty(path) && path.IndexOfAny(Path.GetInvalidPathChars()) >= 0);
        }

        //Lança uma exceção se as credenciais para conexão ftp não forem preenchidas corretamente.
        private static void CheckCredentials(string ftpUser, string ftpPass)
        {
            if (string.IsNullOrEmpty(ftpUser))
            {
                throw new ArgumentNullException("Nome de usuário é obrigatório.");
            }

            if (ftpUser != "anonymous" && string.IsNullOrEmpty(ftpPass))
            {
                throw new ArgumentNullException("A senha é obrigatória para usuários não-anônimos.");
            }
        }
        
        //Conecta-se ao servidor FTP e baixa o arquivo.
        private static async Task<bool> AsyncCheckFtpConnection(FtpImportSettings appFtp, string ftpUser = ImportFtpService.DefaultCredentials, string ftpPass = ImportFtpService.DefaultCredentials)
        {
            CheckCredentials(ftpUser, ftpPass);
            
            using(var ftpClient = new AsyncFtpClient())
            {
                try
                {
                    ftpClient.Host = appFtp.HostFtpServer;
                    ftpClient.Config.ConnectTimeout = 5000;
                    ftpClient.Credentials = new System.Net.NetworkCredential()
                    {
                        UserName = ftpUser,
                        Password = ftpPass  
                    };

                    var res = await ftpClient.AutoConnect();

                    if (!ftpClient.IsConnected)
                    {
                        return false;
                    }

                    return true;

                } catch(FtpException)
                {
                    return false;
                }
                finally
                {
                    await ftpClient.Disconnect();
                }
            }
        }

        private static string SolvePath(FtpImportSettings appFtp, string? directory = null, string? fileName = null)
        {
            directory ??= Directory.GetCurrentDirectory();
            fileName ??= appFtp.InternalFileName + appFtp.ExpectedInternalExtension;

            fileName = (Path.HasExtension(fileName) || Path.GetExtension(fileName) != appFtp.ExpectedInternalExtension) ?  
            Path.GetFileNameWithoutExtension(fileName) + appFtp.ExpectedInternalExtension :
            Path.GetFileNameWithoutExtension(fileName) + FtpImportSettings.SupportedExtensions[(int)FtpImportSettings.ExtensionIndex.DEFAULT];

            //Exceção se o diretório informado conter caractéres inválidos.
            if (ImportFtpService.PathHasInvalidChars(directory))
                throw new ArgumentException("O diretório informado contém caractéres inválidos.");

            //Exceção se o nome do arquivo informado conter caractéres inválidos.
            if (ImportFtpService.PathHasInvalidChars(fileName))
                throw new ArgumentException("O nome do arquivo informado contém caractéres inválidos.");

            //Exceção se o caminho informado corresponde a um arquivo existente.
            if(File.Exists(directory))
                throw new ArgumentException("O caminho informado corresponde a um arquivo existente. Informe um diretório válido.");

            return Path.Combine(directory,fileName);
        }

        public async Task<string> ImportFile(string ftpUser = ImportFtpService.DefaultCredentials, string ftpPass = ImportFtpService.DefaultCredentials, string? directory = null, string? fileName = null, bool overwriteFile = false)
        {
            FtpImportSettings appFtp = FtpImportSettings.GetJson();

            CheckCredentials(ftpUser, ftpPass);

            string fullPath = SolvePath(appFtp, directory, fileName);

            if (await ImportFtpService.AsyncCheckFtpConnection(appFtp, ftpUser, ftpPass))
            {
                throw new InvalidOperationException($"Não foi possível conectar-se ao servidor \"{appFtp.HostFtpServer}\" informado no arquivo de configuração.");
            }

            try
            {
                using(var ftp = new AsyncFtpClient(appFtp.HostFtpServer, ftpUser, ftpPass))
                {
                    await ftp.AutoConnect();
                    if (!await ftp.DirectoryExists(appFtp.RemoteDirectory))
                    {
                        throw new ArgumentException($"O diretório informado não existe dentro do servidor \"{appFtp.HostFtpServer}\"");
                    }

                    var ftpDirList = await ftp.GetListing(appFtp.RemoteDirectory);

                    foreach(var ftpItem in ftpDirList)
                    {
                        
                    }
                }
            }
            catch
            {
                
            }

            return string.Empty;
        }
    }
}