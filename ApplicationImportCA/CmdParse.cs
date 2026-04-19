using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.CompilerServices;
using ImportCA;
using ImportCA.FtpApplication;

RootCommand root = new RootCommand("Pipeline importação e gerenciamento da base de dados nacional.");

var init = new Command("init", "Inicializa a base de dados nacional.");

#region "Import"
var import = new Command("import", "Importa um arquivo para a base de dados nacional.");

var optPassword = new Option<string>("--password", "-p") { Description = "Password."};
var optLocalDir = new Option<DirectoryInfo>("--local-dir", "-l") { Description = "Remote directory."};
var optFileName = new Option<string>("--file-name", "-f") { Description = "Local file name." };
var optOverwrite = new Option<bool>("--overwrite", "-o") { Description = "overwrite file if already exists." };

import.Options.Add(optPassword);
import.Options.Add(optLocalDir);
import.Options.Add(optFileName);
import.Options.Add(optOverwrite);
#endregion

var openSettingsJson = new Command("open-json", "Abre o arquivo de configuração da aplicação.");


init.SetAction((x) =>
{
	ApplicationFtpService.Init();
});

import.SetAction(async a =>
{
	Progress<ImportProgress> ImportPgr = new Progress<ImportProgress>(x =>
	{
		
		char statusChar = x.Step switch
		{
			ImportProgress.ImportStep.Download => 'D',
			ImportProgress.ImportStep.Unpacking => 'U',
			ImportProgress.ImportStep.Validating => 'V',
			ImportProgress.ImportStep.Information => 'I',
			ImportProgress.ImportStep.Warning => '!'
		};

		if (x.Step == ImportProgress.ImportStep.Download)
		{
			Console.Write($"\r ({statusChar}) {x.Message}");
		}
		else
		{
			Console.WriteLine($"({statusChar}) {x.Message}\n");
		}
    });


	using(var ftp = new ImportFtpService(ApplicationFtpService.GetJson(), ImportPgr))
	{
		try
		{
			string res = await ftp.ImportFile(
				ftpPass: a.GetValue(optPassword) ?? "",
				localDirectory: a.GetValue(optLocalDir)?.FullName,
				fileName: a.GetValue(optFileName),
				overwriteFile: a.GetValue(optOverwrite)
			);

			Console.WriteLine($"Arquivo importado com sucesso: {res}");
		}
		catch(Exception ex)
		{
			Console.WriteLine($"Erro ao importar arquivo: {ex.Message}");
		}
	}	
});

openSettingsJson.SetAction((x) =>
{
	if (!ApplicationFtpService.IsInitialized())
	{
		Console.WriteLine("O arquivo de configuração da aplicação não foi inicializado. Por favor, inicialize o arquivo de configuração antes de tentar abri-lo.");
	}

	ApplicationFtpService.OpenJson();
});

root.Subcommands.Add(init);
root.Subcommands.Add(import);
root.Subcommands.Add(openSettingsJson);

return await root.Parse(args).InvokeAsync();


