using FluentFTP;
using FluentFTP.Exceptions;
using System.IO.Compression;
using ImportCA.FtpApplication;
using ImportCA.FtpFileManagement;

namespace ImportCA
{
	public class ImportProgress
	{
		public enum ImportStep { Locating, Download, Extract, MovingFile, Validating, Warning, Starting, Information }
		private string _message = string.Empty;
		private ImportStep _step = ImportStep.Starting;
		private int _percent = 0;

		public string Message { get => this._message; set => this._message = value; }
		public int Percent { get => this._percent; set => this._percent = value; }
		public ImportStep Step { get => this._step; set => this._step = value; }
	}

    public class ImportFtpService : IDisposable
	{

		private FtpSettingsJson? _settingsJson = null;
		private IProgress<ImportProgress>? _progress = null;
		private bool _disposed = false;

		public bool Disposed => this._disposed;

		public ImportFtpService(FtpSettingsJson settingsJson, IProgress<ImportProgress>? progress)
		{
            this._settingsJson = settingsJson ?? throw new ArgumentNullException(nameof(settingsJson));
			this._progress = progress;
        }

		public void Dispose()
		{
			if (this._disposed)
			{
				return;
			}


		}

		//Lança uma exceção se as credenciais para conexão ftp não forem preenchidas corretamente.
		private static void CheckCredentials(string ftpUser, string ftpPass)
		{
			if (string.IsNullOrEmpty(ftpUser))
			{
				throw new ArgumentNullException("Nome de usuário é obrigatório.");
			}

			if (ftpUser != ApplicationFtpService.DefaultCredentials && string.IsNullOrEmpty(ftpPass))
			{
				throw new ArgumentNullException("A senha é obrigatória para conexão de usuários não-anônimos.");
			}
		}

		//Conecta-se ao servidor FTP e baixa o arquivo.
		private static async Task<bool> AyncTestConnection(FtpSettingsJson appSettings, string ftpUser = ApplicationFtpService.DefaultCredentials, string ftpPass = ApplicationFtpService.DefaultCredentials)
		{
			CheckCredentials(ftpUser, ftpPass);

			try
			{
				using (var ftpClient = new AsyncFtpClient(appSettings.HostFtpServer, ftpUser, ftpPass))
				{
					ftpClient.Config.ConnectTimeout = ApplicationFtpService.FtpConnectTimeout;
					await ftpClient.AutoConnect();

					return ftpClient.IsConnected;
				}
			}
			catch(TimeoutException timeoutEx)
			{
				return false;
			}
			catch(FtpAuthenticationException faEx)
			{
				return false;
			}
			catch(Exception ex)
			{
				return false;
			}

		}

		private string ExtractFile(string pathFile)
		{

            this._progress?.Report(new ImportProgress()
            {
                Message = "\nDescompactando arquivo.",
                Step = ImportProgress.ImportStep.Extract
            });

            if (this._settingsJson is null)
			{
				return string.Empty;
			}

			using (var packed = ZipFile.OpenRead(pathFile))
			{
				var entry = packed.Entries.FirstOrDefault(x =>

					x.Name.Contains(this._settingsJson.InternalFileNameContains, StringComparison.OrdinalIgnoreCase)

				) ?? throw new FileNotFoundException("O arquivo compactado não foi encontrado.");

				string unpackedFilePath = Path.Combine(this._tmpFolderPath, entry.Name);

				entry.ExtractToFile(unpackedFilePath);

				return unpackedFilePath;
			}
		}

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
		/// <exception cref="FileNotFoundException"></exception>
		/// <exception cref="FtpException"></exception>
		/// <exception cref="NotSupportedException"></exception>"
		public async Task<string> ImportFile(string ftpPass = ApplicationFtpService.DefaultCredentials, string? localDirectory = null, string? fileName = null, bool overwriteFile = false)
		{

            if (this._settingsJson is null)
			{
				throw new InvalidOperationException("O arquivo de configurações não foi inicializado. Use o comando \"Init\" antes executar essa função.");
			}

			CheckCredentials(this._settingsJson.UserName, ftpPass);

            //Obtendo o caminho completo do arquivo a ser movido.
            string fullPath = ManagementFileFtpService.SolvePath(fileName ?? this._settingsJson.InternalFileNameContains, this._settingsJson.ExpectedInternalExtension, localDirectory ?? ApplicationFtpService.DownloadsFolder, fileName);

            if (!await ImportFtpService.AyncTestConnection(this._settingsJson, this._settingsJson.UserName, ftpPass))
			{
				throw new FtpException($"Não foi possível conectar-se ao servidor \"{this._settingsJson.HostFtpServer}\" informado no arquivo de configuração.");
			}

            string tmpDownloadedPath = string.Empty;
			string unpackedFilePath = string.Empty;

			using (var ftp = new AsyncFtpClient(this._settingsJson.HostFtpServer, this._settingsJson.UserName, ftpPass))
			{
                this._progress?.Report(new ImportProgress()
                {
                    Message = "\nEstabelecendo conexão com servidor...",
                    Step = ImportProgress.ImportStep.Validating
                });

                ftp.Config.ConnectTimeout = ApplicationFtpService.FtpConnectTimeout;
				var profile = await ftp.AutoConnect();

                this._progress?.Report(new ImportProgress()
                {
                    Message = "\nConexão estabelecida com sucesso...",
                    Step = ImportProgress.ImportStep.Information
                });

                //Lança exceção se não existir nenhum diretório dentro do servidor informado pelo arquivo de configuração.
                if (!await ftp.DirectoryExists(this._settingsJson.HostDirectory))
				{
					throw new DirectoryNotFoundException($"O diretório informado não existe dentro do servidor \"{this._settingsJson.HostFtpServer}\"");
				}

                this._progress?.Report(new ImportProgress()
                {
                    Message = "\nLocalizando arquivo...",
                    Step = ImportProgress.ImportStep.Information
                });

				//Obtendo a listagem do nome de todos os arquivos dentro do diretório
				var items = await ftp.GetListing(this._settingsJson.HostDirectory, options: FtpListOption.Recursive);

				//Lança exceção se não existir nenhum arquivo dentro do diretório informado pelo arquivo de configuração.
				if (items.Length <= 0)
				{
					throw new DirectoryNotFoundException($"O diretório informado não existe dentro do servidor \"{this._settingsJson.HostFtpServer}\"");
				}

				//Localizando o primeiro arquivo com o nome informado no arquivo de configuração, ignorando letras maiúsculas ou minúsculas.
				var file = items.FirstOrDefault((x) =>
					x.Type == FtpObjectType.File && x.Name.Contains(this._settingsJson.HostFileName, StringComparison.OrdinalIgnoreCase)
				) ??
					throw new FileNotFoundException($"Não foi possível localizar o arquivo \"{this._settingsJson.HostFileName}\".");

				//Obtendo a extensão do arquivo localizado.
				string remotefileExtension = Path.GetExtension(file.Name) ??
												throw new NotSupportedException("O arquivo informado não contém nenhuma extensão definida.");

				if (remotefileExtension != this._settingsJson.ExpectedHostFileExtension)
				{
					throw new NotSupportedException($"O arquivo localizado \"{file.Name}\" não é do tipo esperado informado no arquivo de configuração.");
				}

                this._progress?.Report(new ImportProgress()
                {
                    Message = "\nArquivo foi localizado com sucesso.",
                    Step = ImportProgress.ImportStep.Information
                });

				using var manageFileFtp = new ManagementFileFtpService();
				manageFileFtp.CreateTempFolder();

				//Caminhho para o arquivo baixo temporariamente
				tmpDownloadedPath = Path.Combine(manageFileFtp.TempFolderPath, Path.ChangeExtension(Path.GetRandomFileName(),this._settingsJson.ExpectedHostFileExtension));

				Progress<FtpProgress> pgrDownload = new Progress<FtpProgress>(ftp =>
				{
					this._progress?.Report(new ImportProgress()
					{
						Message = $"Baixando arquivo {(int)ftp.Progress}% ...",
						Step = ImportProgress.ImportStep.Download
					});
				});

                FtpStatus downloadResult = await ftp.DownloadFile(tmpDownloadedPath, file.FullName, verifyOptions: FtpVerify.OnlyVerify, progress:pgrDownload);

                if (downloadResult != FtpStatus.Success)
				{
					throw new FtpException("Ocorreu um erro durante o download do arquivo. Por favor, tente novamente mais tarde.");
				}

                this._progress?.Report(new ImportProgress()
                {
                    Message = $"\nDownlaod realizado com sucesso.",
                    Step = ImportProgress.ImportStep.Information
                });
            }

			unpackedFilePath = (ManagementFileFtpService.IsCompressedFile(tmpDownloadedPath)) ? this.ExtractFile(tmpDownloadedPath) : tmpDownloadedPath;

			if (!ApplicationFtpService.IsSupportedExtension(Path.GetExtension(unpackedFilePath)))
			{
                ManagementFileFtpService.MoveEx(manageFileFtp.TempFolderPath, ApplicationFtpService.RecoveredFolder);
				manageFileFtp.DeleteTempFolder();
				throw new NotSupportedException("Arquivo não suportado. O conteúdo foi movido para a pasta de recuperação para processamento manual.");
			}

			string pathResult = string.Empty;

			try
			{
				pathResult = ManagementFileFtpService.MoveEx(unpackedFilePath, fullPath, overwriteFile);
			}
			catch (Exception ex)
			{
				fullPath = ManagementFileFtpService.SolvePath(this._settingsJson.InternalFileNameContains, this._settingsJson.ExpectedInternalExtension, , fileName);
                pathResult = ManagementFileFtpService.MoveEx(unpackedFilePath, fullPath);

                this._progress?.Report(new ImportProgress()
                {
                    Message = $"\nArquivo movido para a pasta padrão de downloads, pois o diretório informado exige um nível permissões elevadas.\n\nDetalhes: {ex.Message}",
                    Step = ImportProgress.ImportStep.Information
                });
            }

			return pathResult;
		}
	}
}