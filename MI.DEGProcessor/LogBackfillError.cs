using IGS.DataServices;
using IGS.Models.Entities;
using IGS.Models.Interfaces;
using MI.Common.Configuration;
using MI.Common.Helper;
using NLog;
using System.Xml.Linq;

namespace MI.DEGProcessor
{
    public class LogBackfillError
    {
        private IDataServices _data;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public async Task<Boolean> SaveTransferResults(int applicationTransferId,
                                                                   string status,
                                                                   string errorMessage,
                                                                   CancellationToken stoppingToken)
        {
            try
            {
               _data = new DataServices(SqlHelper.EnsureTrustedServerCertificate(GlobalConnectionStrings.Instance.MIACA_IGS));
               var transferResult =
                    await _data.AwsAtXmlTransfers.FirstOrDefaultAsync(x => x.ApplicationTransferId == applicationTransferId);
                if (transferResult == null)
                {
                    var newTransfer = new AwsAtXmlTransfer
                    {
                        ApplicationTransferId = applicationTransferId,
                        TransferStatus = status,
                        TransferDate = DateTime.Now,
                        Message = errorMessage
                    };
                    _data.AwsAtXmlTransfers.Add(newTransfer);
                    await _data.SaveChanges();
                }
                else
                {
                    transferResult.TransferStatus = status;
                    transferResult.TransferDate = DateTime.Now;
                    transferResult.Message = errorMessage;
                    await _data.SaveChanges();
                }

                return true;
            }
            catch (Exception ex) 
            {
                _logger.Error(ex, "Error saving AwsAtXmlTransfer Record:");
                return false;
            }
        }
    }
}
