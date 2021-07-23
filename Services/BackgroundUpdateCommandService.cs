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
        /// Background에서 Command 의 값을 갱신해주기 위한 Service
        /// </summary>
        /// <param name="ThreadExecutorService">ThreadExecutorService BotToken값을 설정해야 하므로 주입</param>
        public BackgroundUpdateCommandService(ThreadExecutorService ThreadExecutorService)
        {
            this.ThreadExecutorService = ThreadExecutorService;
        }

        /// <summary>
        /// 주기적으로 실행되는 작업
        /// </summary>
        /// <param name="cancellationToken">CancellationToken 작업 완료 여부 flag값</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ThreadExecutorService.UpdateCommandData(); // Command 데이터를 Update 해준다.

                await Task.Delay(TimeSpan.FromMinutes(2), cancellationToken); // 2분마다 반복
            } // end while
        } // end ExecuteAsync
    } // end class
} // end namespace
