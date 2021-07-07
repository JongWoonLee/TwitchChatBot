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
        private ThreadExecutorService ThreadExecutorService;

        public MemberController(MemberService service, ThreadExecutorService teservice)
        {
            MemberService = service;
            ThreadExecutorService = teservice;
        }

        /// <summary>
        /// Member View로 이동하는 GET 메소드
        /// </summary>
        /// <param name="code"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        [HttpGet, Route("/member/index")]
        public IActionResult Index(string code, string[] scope)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Redirect("https://id.twitch.tv/oauth2/authorize?client_id=jjvh028bmtssj5x8fov8lu3snk3wut&redirect_uri=https://localhost:44348/member/index&response_type=code&scope=chat:edit chat:read user:edit whispers:read whispers:edit user:read:email");
            }
            Tuple<TwitchToken, User> tuple = MemberService.ConnectReleasesWebClient(code, "https://localhost:44348/member/index");

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
        public IActionResult StartBot(string msg)
        {
            //string channelName = "jongwoonlee";
            var Token = Request.Cookies["access_token"];
            //_stbservice.StartBot(msg, token);

            string Ip = "irc.chat.twitch.tv";
            int Port = 6667;
            ThreadExecutorService.RegisterBot(101, Ip, Port, "simple_irc_bot", "oauth:" + Token, "jongwoonlee");
            ThreadExecutorService.RegisterBot(102, Ip, Port, "simple_irc_bot", "oauth:" + Token, "mbnv262");

            return RedirectToAction("Index", "Home");
        }
        
    }


}
