using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TwitchChatBot.Models;
using TwitchChatBot.Service;

namespace TwitchChatBot.Controllers
{
    public class MemberController : Controller
    {

        private readonly MemberService MemberService;
        private readonly ThreadExecutorService ThreadExecutorService;

        public MemberController(MemberService service, ThreadExecutorService teservice)
        {
            MemberService = service;
            ThreadExecutorService = teservice;
        }

        /// <summary>
        /// Twitch Login 을 하는 GET 메소드.
        /// code 가 없으면 코드를 가져오는 요청을 하고
        /// code 가 있으면 Token을 가지고 온 뒤
        /// Token 을 이용해서 User data를 얻어오고
        /// Streamer Table 에 Insert를 한다.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpGet, Route("/member/index")]
        public IActionResult Index(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                string url = MemberService.GetRedirectURL();
                return Redirect(url);
            }
            TwitchToken twitchToken = MemberService.ConnectReleasesWebClient(code);
            User user = MemberService.ValidatingRequests(twitchToken.AccessToken);
            int insertMember = MemberService.Insert(twitchToken,user);

            if (string.IsNullOrWhiteSpace(Request.Cookies["user_id"]))
            {

                Response.Cookies.Append("access_token", twitchToken.AccessToken);
                Response.Cookies.Append("user_id", Convert.ToString(user.UserId));
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Startbot View 로 이동하는 GET 메소드
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("/member/startbot")]
        public IActionResult StartBot() // 로그인한 유저가 봇 사용중인지 확인하는 작업 요망.. 아마 처음 insert할때 같이 Insert해야 할듯함
        {
            //string userId = Request.Cookies["user_id"];
            //string userId = "704190345";
            //int result = MemberService.FindBotInUseByUserId(userId);
            //ViewData["result"] = result;
            return View();
        }

        /// <summary>
        /// Member 정보를 받아 봇을 실행시키는 POST 메소드
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [HttpPost, Route("/member/startbot")]
        public IActionResult StartBot(string msg = "jongwoonlee")
        {
            //string userId = Request.Cookies["user_id"];
            //long lUserId = Convert.ToInt64(userId);

            ThreadExecutorService.RegisterBot(101, "simple_irc_bot", "jongwoonlee");
            ThreadExecutorService.RegisterBot(102, "simple_irc_bot", "mbnv262");

            return RedirectToAction("Index", "Home");
        }
        
    }


}
