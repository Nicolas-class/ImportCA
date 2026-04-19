using ImportCA.FtpApplication;

namespace ImportCA.FtpFileManagement
{

	public class FileEnumerator
	{
		public enum EnumFileType { Text, Json, SQLite, CSV }

		//Enumera todas as linhas de um arquivo.
		public static async Task EnumTextLines(string path, Action<string> action)
		{
			//Verificando se o arquivo exite.
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("O arquivo especificado não existe.");
			}
			
			//Verificando se a extensão do arquivo é suportada para conversão.
			if (!ApplicationFtpService.IsSupportedExtension(Path.GetExtension(path)))
			{
				throw new NotSupportedException("O arquivo especificado não é de um formato suportado para realizar a conversão.");
			}

			//Enumrando linhas no thread pool  de forma assincrona.
			await Task.Run(() =>
			{
				using (var reader = new StreamReader(path))
				{
					while (!reader.EndOfStream)
					{
						string? line = reader.ReadLine();

						if (line is not null)
						{
							action(line);
						}
					}
				}
			});
		}
	}

}