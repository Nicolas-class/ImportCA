using ImportCA;
using System.IO.Compression;

namespace Lab
{
    
    [TestClass]
    public sealed class SettingsFtpServiceLab
    {

        [TestMethod]
        public void TestInit()
        {
			ApplicationFtpService.Init();
		}

        [TestMethod]
        public async Task TestLoad()
        {
			using var ftpsv = new ImportFtpService(ApplicationFtpService.GetJson());
			
			try
			{
				var dir = await ftpsv.ImportFile(
				ftpUser: "anonymous",
				ftpPass: "",
				localDirectory: null,
				fileName: null,
				true);
			}
			finally
			{
				ftpsv.Dispose();
			}
		}

        [TestMethod]
        public void MiscTest()
        {

		}
    }
}
