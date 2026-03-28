using System;
using System.Threading.Tasks;
using ImportCA;
using System.IO.Compression;
using ImportCA.FtpApplication;
using ImportCA.FtpManagement;
using System.Diagnostics;

namespace Lab
{
	[TestClass]
	public sealed class ManagementFile
	{
		[TestMethod]
		public async Task EnumeLines()
		{
			await ManagementFileFtpService.EnumLinesAsync(@"F:\Projetos\ImportCA\ApplicationImportCA\bin\Debug\net9.0\downloads\tgg_export_caepi (1).txt", str =>
			{
				//Debug.Print(str);
			});
		}
	}
}
