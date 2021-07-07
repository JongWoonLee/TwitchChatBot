using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TwitchChatBot.Models;
using TwitchChatBot.Service;
using TwitchChatBot.Service.ChatBotEx.bot;

namespace TwitchChatBot.Controllers
{
    public class MemberController : Controller
    {
        
        private readonly MemberService _service;
        private SimpleTwitchBotService _stbservice;
        private ThreadExecutorService _teservice;

        public MemberController(MemberService service, SimpleTwitchBotService stbservice, ThreadExecutorService teservice)
        {
            _service = service;
            _stbservice = stbservice;
            _teservice = teservice;
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
            if(string.IsNullOrEmpty(code))
            {
                return Redirect("https://id.twitch.tv/oauth2/authorize?client_id=jjvh028bmtssj5x8fov8lu3snk3wut&redirect_uri=https://localhost:44348/member/index&response_type=code&scope=chat:edit chat:read user:edit whispers:read whispers:edit user:read:email");
            }
            Tuple<TwitchToken,User> tuple = _service.ConnectReleasesWebClient(code, "https://localhost:44348/member/index");
            _service.GetUsers(tuple.Item1.AccessToken);

            if(Request.Cookies["user_id"] == null)
            {
                Response.Cookies.Append("access_token", tuple.Item1.AccessToken);
                Response.Cookies.Append("id", tuple.Item2.UserId);
            }
            

            return RedirectToAction("Index", "Home");
        }

        [HttpGet, Route("/member/startbot")]
        public IActionResult StartBot()
        {

            return View();
        }

        [HttpPost, Route("/member/startbot")]
        public IActionResult StartBot(string msg)
        {
            //string channelName = "jongwoonlee";
            var token = Request.Cookies["access_token"];
            //_stbservice.StartBot(msg, token);

            string ip = "irc.chat.twitch.tv";
            int port = 6667;
            _teservice.RegisterBot(101,ip,port, "simple_irc_bot", "oauth:"+token, "jongwoonlee");
            _teservice.RegisterBot(102,ip,port, "simple_irc_bot", "oauth:"+token, "mbnv262");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet, Route("/member/admin")]
        public IActionResult Admin()
        {
            ViewData["ManagedBot"] = _stbservice.ManagedBot.Keys;
            return View();
        }
    }

    
}
