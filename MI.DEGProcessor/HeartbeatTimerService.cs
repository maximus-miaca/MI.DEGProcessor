using System;
using System.Threading;
using System.Threading.Tasks;
using MI.DEGProcessor.Helpers;
using Microsoft.Extensions.Hosting;
using ML.Models.Enums;
using NLog;

namespace MI.DEGProcessor
{
    public sealed class HeartbeatTimerService : IHostedService, IDisposable
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly        Timer  _heartbeatTimer;

        public HeartbeatTimerService()
        {
            _heartbeatTimer = new Timer(DoWork);
        }

        public void Dispose()
        {
            _heartbeatTimer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var heartbeatMilliseconds = AppSettings.Instance.HeartbeatMilliseconds;
            _heartbeatTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(heartbeatMilliseconds));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _heartbeatTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _logger.Info("Heartbeat", LogType.AppHeartbeat);
        }
    }
}