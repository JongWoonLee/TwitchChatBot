﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchChatBot.Service
{
    public class BackgroundTokenRefreshService : HostedService
    {
        public string BotRefreshToken;

        public ThreadExecutorService ThreadExecutorService { get; set; }

        /// <summary>
        /// Background에서 BotToken을 Refresh 해주는 Service
        /// </summary>
        /// <param name="ThreadExecutorService">ThreadExecutorService에 접근해서 BotToken값을 설정해야 하므로 주입</param>
        public BackgroundTokenRefreshService(ThreadExecutorService ThreadExecutorService)
        {
            this.ThreadExecutorService = ThreadExecutorService;
        }

        /// <summary>
        /// IHostedService ExecuteAsync
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ThreadExecutorService.ValidateAccessTokenEveryHour(); // TimeSpan마다 주기적으로 해야할 작업.
                ThreadExecutorService.UpdateCommandData();
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
            }
        }
    }
}
