using ImportCA;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lab
{
    
    [TestClass]
    public sealed class SettingsFtpServiceLab
    {
        private readonly NullLogger<ApplicationFtpService> lgo = NullLogger<ApplicationFtpService>.Instance;

        [TestMethod]
        public void TestInit()
        {
            new ApplicationFtpService(lgo).Init();
        }

        [TestMethod]
        public void TestLoad()
        {
            var opt = new ApplicationFtpService(lgo);
        }
    }
}
