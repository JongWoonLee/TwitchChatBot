using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchChatBot.Service;

namespace TwitchChatBot.Services
{
    public class BackgroundUpdateCommandService : HostedService
    {
        public ThreadExecutorService ThreadExecutorService { get; set; }

        /// <summary>
        /// ThreadExecutorService 의 Command 값을 변경하기 위한 Service
        /// </summary>
        /// <param name="ThreadExecutorService">ThreadExecutorService에 접근해서 BotToken값을 설정해야 하므로 주입</param>
        public BackgroundUpdateCommandService(ThreadExecutorService ThreadExecutorService)
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
                ThreadExecutorService.UpdateCommandData();
                await Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);
            }
        }
    }
}
