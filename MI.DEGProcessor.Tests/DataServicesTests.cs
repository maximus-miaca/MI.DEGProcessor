using IGS.DataServices;
using MI.Common.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MI.DEGProcessor.Tests
{
    [TestClass]
    public class DataServicesTests
    {
        [TestMethod]
        public void TestGetNextTransferSequenceNumber()
        {
            var data =
                new DataServices(SqlHelper
                                    .EnsureTrustedServerCertificate("Data Source=ucocdmmsql01mia;Initial Catalog=MIACA_IGS;Integrated Security=SSPI;"));

            var valueTask = data.ApplicationTransfers.GetNextTransferSequenceNumber("WorkerDCT");
            valueTask.Wait();
            var value = valueTask.Result;

            Assert.IsTrue(value > 0);
        }
    }
}