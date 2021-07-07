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
        /// Member View로 이동하는 GET 메소드
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
            Tuple<TwitchToken, User> tuple = MemberService.ConnectReleasesWebClient(code);

            if (string.IsNullOrWhiteSpace(Request.Cookies["user_id"]))
            {
                Response.Cookies.Append("access_token", tuple.Item1.AccessToken);
                Response.Cookies.Append("user_id", tuple.Item2.UserId);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Startbot View 로 이동하는 GET 메소드
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("/member/startbot")]
        public IActionResult StartBot()
        {
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
            var Token = Request.Cookies["access_token"];

            ThreadExecutorService.RegisterBot(101, "simple_irc_bot", "oauth:" + Token, "jongwoonlee");
            ThreadExecutorService.RegisterBot(102, "simple_irc_bot", "oauth:" + Token, "mbnv262");

            return RedirectToAction("Index", "Home");
        }
        
    }


}
