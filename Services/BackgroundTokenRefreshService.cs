using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwitchChatBot.Models;

namespace TwitchChatBot.Service
{
    /// <summary>
    /// 봇토큰, 사용자 토큰을 Background에서 돌면서 갱신
    /// </summary>
    public class BackgroundTokenRefreshService : HostedService
    {
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
        /// 주기적으로 실행되는 작업
        /// </summary>
        /// <param name="cancellationToken">CancellationToken 작업 완료 여부 flag값</param>
        /// <returns>Task</returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ThreadExecutorService.ValidateBotTokenEveryHour();
                var ManagedBot = ThreadExecutorService.ManagedBot; // 봇 리스트를 얻어온다.

                // 봇을 돌면서 StreamerToken 값을 갱신해준다.
                foreach (long Key in ManagedBot.Keys)
                {
                    ManagedBot[Key].StreamerToken = ThreadExecutorService.ValidateAccessToken(ManagedBot[Key].StreamerToken.RefreshToken);
                }
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken); // 1시간 주기로 반복
            }
        }
    } // end class
} // end namespace
