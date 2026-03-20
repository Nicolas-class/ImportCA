using System.Text.Json;
using System.IO;
using System.Data.SQLite;
using FluentFTP;
using System.Text.Json.Serialization;
using FluentFTP.Exceptions;
using System.IO.Compression;

namespace ImportCA
{

	//Importação do arquivo ftp.
	public class ImportFtpService : IDisposable
	{
		private const string DefaultCredentials = "anonymous";
		private bool _disposed = false;
		private string _tmpFolderPath = string.Empty;
		private FtpSettingsJson? _settingsJson = null;

		public ImportFtpService(FtpSettingsJson settingsJson)
		{
			this._settingsJson = settingsJson ?? throw new ArgumentNullException(nameof(settingsJson));
		}

		public void Dispose()
		{
			if (this._disposed)
				return;

			DeleteTempFolder();

			this._disposed = true;
		}

		//Cria uma pasta temporária para realizar as devidas operações.
		private void CreateTempFolder()
		{
			//Apagando o diretório temporário anterior caso existir.
			DeleteTempFolder();

			var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

			this._tmpFolderPath = dir.FullName;
		}

		//Apaga a pasta temporária.
		private void DeleteTempFolder()
		{
			//Se a pasta anterior ainda existir, será apagada.
			if (!Directory.Exists(this._tmpFolderPath))
				return;

			try
			{
				Directory.Delete(this._tmpFolderPath,true);
			}
			finally
			{
				this._tmpFolderPath = string.Empty;
			}
		}
		
		private enum checkInvalidChars { path, filename };

		//Verifica se o diretório ou caminho do arquivo informado, contém caractéres inválidos.
		private static bool HasInvalidChars(string fileOrDirectory, ImportFtpService.checkInvalidChars check) => check switch
		{
			checkInvalidChars.path => (string.IsNullOrEmpty(fileOrDirectory) || fileOrDirectory.IndexOfAny(Path.GetInvalidPathChars()) >= 0),
			checkInvalidChars.filename => (string.IsNullOrEmpty(fileOrDirectory) || fileOrDirectory.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0),
		};

		//Lança uma exceção se as credenciais para conexão ftp não forem preenchidas corretamente.
		private static void CheckCredentials(string ftpUser, string ftpPass)
		{
			if (string.IsNullOrEmpty(ftpUser))
				throw new ArgumentNullException("Nome de usuário é obrigatório.");

			if (ftpUser != "anonymous" && string.IsNullOrEmpty(ftpPass))
				throw new ArgumentNullException("A senha é obrigatória para usuários não-anônimos.");
		}

		//Conecta-se ao servidor FTP e baixa o arquivo.
		private static async Task<bool> AsyncCheckFtpConnection(FtpSettingsJson appSettings, string ftpUser = ImportFtpService.DefaultCredentials, string ftpPass = ImportFtpService.DefaultCredentials)
		{
			CheckCredentials(ftpUser, ftpPass);

			try
			{
				using (var ftpClient = new AsyncFtpClient(appSettings.HostFtpServer, ftpUser, ftpPass))
				{
					ftpClient.Config.ConnectTimeout = 5000;
					await ftpClient.AutoConnect();

					return ftpClient.IsConnected;
				}

			}
			catch
			{
				return false;
			}

		}

		//Verifica se o arquivo informado está compactado.
		private bool IsCompressedFile(string path)
		{
			try
			{
				using (var file = ZipFile.OpenRead(path))
				{
					var entries = file.Entries;
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

		//Função que faz a montagem do caminho completo do arquivo à ser movido.
		private static string SolvePath(FtpSettingsJson appFtp, string? directory = null, string? fileName = null)
		{
			directory ??= Directory.GetCurrentDirectory();
			fileName ??= appFtp.InternalFileName + appFtp.ExpectedInternalExtension;

			//Exceção se o diretório informado conter caractéres inválidos.
			if (ImportFtpService.HasInvalidChars(directory,checkInvalidChars.path))
				throw new ArgumentException("O diretório informado contém caractéres inválidos.");

			//Exceção se o nome do arquivo informado conter caractéres inválidos.
			if (ImportFtpService.HasInvalidChars(fileName, checkInvalidChars.filename))
				throw new ArgumentException("O nome do arquivo informado contém caractéres inválidos.");

			//Exceção se o caminho informado corresponde a um arquivo existente.
			if (File.Exists(directory))
				throw new ArgumentException("O caminho informado corresponde a um arquivo existente. Informe um diretório válido.");

			fileName = (Path.HasExtension(fileName) || Path.GetExtension(fileName) != appFtp.ExpectedInternalExtension) ?
			Path.GetFileNameWithoutExtension(fileName) + appFtp.ExpectedInternalExtension :
			Path.GetFileNameWithoutExtension(fileName) + ApplicationFtpService.SupportedExtensions[(int)ApplicationFtpService.ExtensionIndex.DEFAULT];

			return Path.Combine(directory, fileName);
		}

		//private ZipArchiveEntry FindInternalFile()
		//{

		//}
		

		/// <summary>
		/// Realiza a importação do arquivo do servidor FTP do governo.
		/// </summary>
		/// <param name="ftpUser">Usuário</param>
		/// <param name="ftpPass">Senha</param>
		/// <param name="localDirectory">Diretório onde o arquivo deve ser baixado.</param>
		/// <param name="fileName">Novo nome do arquivo.</param>
		/// <param name="overwriteFile">Sobreescrever o arquivo existente.</param>
		/// <returns>Caminho completo para o arquivo importado.</returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="DirectoryNotFoundException"></exception>
		public async Task<string> ImportFile(string ftpUser = ImportFtpService.DefaultCredentials, string ftpPass = ImportFtpService.DefaultCredentials, string? localDirectory = null, string? fileName = null, bool overwriteFile = false)
		{
            if (this._settingsJson is null)
				throw new InvalidOperationException("Settings object is not set. Please set the FtpSettings property before calling this method.");

			CheckCredentials(ftpUser, ftpPass);

			//Obtendo o caminho completo do arquivo a ser movido.
			string fullPath = SolvePath(this._settingsJson, localDirectory, fileName);
			
			if (!await ImportFtpService.AsyncCheckFtpConnection(this._settingsJson, ftpUser, ftpPass))
				throw new FtpException($"Não foi possível conectar-se ao servidor \"{this._settingsJson.HostFtpServer}\" informado no arquivo de configuração.");

			using (var ftp = new AsyncFtpClient(this._settingsJson.HostFtpServer, ftpUser, ftpPass))
			{
				ftp.Config.ConnectTimeout = ApplicationFtpService.FtpConnectTimeout;
				await ftp.AutoConnect();

				//Lança exceção se não existir nenhum diretório dentro do servidor informado pelo arquivo de configuração.
				if (!await ftp.DirectoryExists(this._settingsJson.RemoteDirectory))
					throw new DirectoryNotFoundException($"O diretório informado não existe dentro do servidor \"{this._settingsJson.HostFtpServer}\"");
					
				//Obtendo a listagem do nome de todos os arquivos dentro do diretório
				var items = await ftp.GetListing(this._settingsJson.RemoteDirectory);

				//Lança exceção se não existir nenhum arquivo dentro do diretório informado pelo arquivo de configuração.
				if (items.Length <= 0)
					throw new InvalidOperationException($"Não existe nenhum arquivo no diretório \"{this._settingsJson.RemoteDirectory}\".");

				//Localizando o primeiro arquivo com o nome informado no arquivo de configuração, ignorando letras maiúsculas ou minúsculas.
				var file = items.FirstOrDefault((x) =>
					x.Type == FtpObjectType.File && x.Name.Contains(this._settingsJson.RemoteFileName, StringComparison.OrdinalIgnoreCase)
				) ??
					throw new FileNotFoundException($"Não foi possível localizar o arquivo \"{this._settingsJson.RemoteFileName}\".");

				//Obtendo a extensão do arquivo localizado.
				string remotefileExtension = Path.GetExtension(file.Name) ?? 
												throw new NotSupportedException("O arquivo informado não contém nenhuma extensão definida.");

				if (remotefileExtension is null || remotefileExtension != this._settingsJson.ExpectedRemoteExtension)
					throw new NotSupportedException($"O arquivo localiado \"{file.Name}\" contém uma extensão inválida ou não é suportada pelo software.");

				this.CreateTempFolder();

				//Caminhho para o arquivo baixo temporariamente
				string tmpDownloadedPath = Path.Combine(this._tmpFolderPath, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + this._settingsJson.ExpectedRemoteExtension);

				FtpStatus downloadResult = await ftp.DownloadFile(tmpDownloadedPath, file.FullName, verifyOptions: FtpVerify.OnlyVerify);
				
				if (downloadResult != FtpStatus.Success)
					throw new FtpException("Ocorreu um erro durante o download do arquivo. Por favor, tente novamente mais tarde.");

				if (this.IsCompressedFile(tmpDownloadedPath))
				{
					using(var packed = ZipFile.OpenRead(tmpDownloadedPath))
					{
						var inFile = packed.Entries.FirstOrDefault(x =>
							
							x.Name.Contains(this._settingsJson.InternalFileName, StringComparison.OrdinalIgnoreCase)

						) ?? throw new FileNotFoundException("");
						
						string unpackedFilePath = Path.Combine(this._tmpFolderPath, inFile.Name);

						inFile.ExtractToFile(unpackedFilePath);

						if (!ApplicationFtpService.IsSupportedExtension(Path.GetExtension(unpackedFilePath)))
							throw new NotSupportedException($"A extensão do arquivo \"{Path.GetExtension(unpackedFilePath)}\" não é suportada pelo software.");

						File.Move(unpackedFilePath, fullPath);
					}

					File.Delete(tmpDownloadedPath);
				}

			}

			return fullPath;
		}
	}
}