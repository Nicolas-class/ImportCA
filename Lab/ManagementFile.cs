using System;
using System.Threading.Tasks;
using ImportCA;
using System.IO.Compression;
using ImportCA.FtpApplication;
using ImportCA.FtpFileManagement;
using System.Diagnostics;
using System.Data.SQLite;
using ImportCA.FtpConvert;

namespace Lab
{
	[TestClass]
	public class ManagementFile
	{
	
		[TestMethod]
		public void ConvertToDataBase()
		{
			ApplicationFtpService.Init();
			var ftpsettings = ApplicationFtpService.GetJson();
			ConvertFtpService.ConvertFile(ftpsettings, @"E:\Projetos\ImportCA\ApplicationImportCA\bin\Debug\net9.0\downloads\banco_de_dados.txt");
		}

	}
}
