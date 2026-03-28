using ImportCA.FtpApplication;
using System.IO.Compression;

namespace ImportCA.FtpManagement
{
	public static class ManagementFileFtpService
	{
		//Enumera todas as linhas de um arquivo.
		public static async Task EnumLinesAsync(string path, Action<string> action)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("O arquivo especificado não existe.");
			}

			if (!ApplicationFtpService.IsSupportedExtension(path))
			{
				throw new NotSupportedException("O arquivo especificado não é de um formato suportado para realizar a conversão.");
			}

			await Task.Run(() =>
			{
				using (var reader = new StreamReader(path))
				{
					while (!reader.EndOfStream)
					{
						string? line = reader.ReadLine();

						if(line is not null)
						{
							action(line);
						}
						
					}
				}
			});
		}

		public enum CheckInvalidChars { path, filename };

		//Verifica se o diretório ou caminho do arquivo informado, contém caractéres inválidos.
		internal static bool HasInvalidChars(string fileOrDirectory, ManagementFileFtpService.CheckInvalidChars check = CheckInvalidChars.path) => check switch
		{
			CheckInvalidChars.path => (string.IsNullOrEmpty(fileOrDirectory) || fileOrDirectory.IndexOfAny(Path.GetInvalidPathChars()) >= 0),
			CheckInvalidChars.filename => (string.IsNullOrEmpty(fileOrDirectory) || fileOrDirectory.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
		};

		//Verifica se o arquivo informado está compactado.
		internal static bool IsCompressedFile(string path)
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
		internal static string SolvePath(string defaultFileName, string defaultFileExtension, string directory, string? fileName = null)
		{
			fileName ??= Path.ChangeExtension(defaultFileName, defaultFileExtension);

			//Exceção se o diretório informado conter caractéres inválidos.
			if (ManagementFileFtpService.HasInvalidChars(directory, ManagementFileFtpService.CheckInvalidChars.path))
			{
				throw new ArgumentException("O diretório informado contém caractéres inválidos.");
			}

			//Exceção se o nome do arquivo informado conter caractéres inválidos.
			if (ManagementFileFtpService.HasInvalidChars(fileName, ManagementFileFtpService.CheckInvalidChars.filename))
			{
				throw new ArgumentException("O nome do arquivo informado contém caractéres inválidos.");
			}

			//Exceção se o caminho informado corresponde a um arquivo existente.
			if (File.Exists(directory))
			{
				throw new ArgumentException("O caminho informado corresponde a um arquivo existente. Informe um diretório válido.");
			}

			fileName = (!Path.HasExtension(fileName) || Path.GetExtension(fileName) != defaultFileExtension) ?
			Path.ChangeExtension(fileName, defaultFileExtension) :
			fileName;

			return Path.Combine(directory, fileName);
		}

		//Move o arquivo para o diretório informado,
		//caso o arquivo já exista e a opção de sobrescrever seja falsa,
		//o arquivo será movido para o mesmo diretório com um número entre parênteses adicionado ao nome do arquivo.
		internal static string MoveEx(string sourceFileName, string destFileName, bool overwrite = false)
		{
			int count = 0;

			while (!overwrite && File.Exists(destFileName))
			{
				count++;
				string newFileName = $"{Path.GetFileNameWithoutExtension(destFileName)} ({count}){Path.GetExtension(sourceFileName)}";
				string newDestFileName = Path.Combine(new FileInfo(destFileName).Directory.FullName, newFileName);
				destFileName = (!File.Exists(newDestFileName)) ? newDestFileName : destFileName;
			}

			File.Move(sourceFileName, destFileName, overwrite);

			return destFileName;

		}

	}
}
