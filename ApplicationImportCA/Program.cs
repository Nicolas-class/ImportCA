using System.CommandLine;
using System.CommandLine.Parsing;
using System.ComponentModel;
using ImportCA;

RootCommand root = new RootCommand("Pipeline importação e gerenciamento da base de dados nacional.");

var init = new Command("init", "Inicializa a base de dados nacional.");
var import = new Command("import", "Importa um arquivo para a base de dados nacional.");

var optUser = new Option<string>("--user", "-u") { Description = "Username." };
var optPassword = new Option<string>("--password", "-p") { Description = "Password."};
var optLocalDir = new Option<DirectoryInfo>("--local-dir", "-l") { Description = "Remote directory."};
var optFileName = new Option<string>("--file-name", "-f") { Description = "Local file name." };
var optOverwrite = new Option<bool>("--overwrite", "-o") { Description = "overwrite file if already exists." };

import.Options.Add(optUser);
import.Options.Add(optPassword);
import.Options.Add(optLocalDir);
import.Options.Add(optFileName);
import.Options.Add(optOverwrite);

init.SetAction((x) =>
{
	ApplicationFtpService.Init();
});

import.SetAction(async a =>
{
	using(var ftp = new ImportFtpService(ApplicationFtpService.GetJson()))
	{
		try
		{
			string res = await ftp.ImportFile(
			ftpUser: a.GetValue(optUser) ?? "anonymous",
			ftpPass: a.GetValue(optPassword) ?? "",
			localDirectory: a.GetValue(optLocalDir)?.FullName,
			fileName: a.GetValue(optFileName),
			overwriteFile: a.GetValue(optOverwrite)
			);

			Console.WriteLine($"\nArquivo importado com sucesso: {res}");
		}
		catch(Exception ex)
		{
			Console.WriteLine($"\nErro ao importar arquivo: {ex.Message}");
		}
	}	
});



root.Subcommands.Add(init);
root.Subcommands.Add(import);

return await root.Parse(args).InvokeAsync();


