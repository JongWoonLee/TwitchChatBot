using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchChatBot.Service
{
    public class BackgroundTokenRefreshService : HostedService
    {
        public string BotRefreshToken;

        public ThreadExecutorService ThreadExecutorService { get; set; }
        public BackgroundTokenRefreshService(ThreadExecutorService ThreadExecutorService)
        {
            this.ThreadExecutorService = ThreadExecutorService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ThreadExecutorService.ValidateAccessTokenEveryHour();
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
            }
        }
    }
}
