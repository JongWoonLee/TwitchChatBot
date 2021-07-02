using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TwitchChatBot.Models;
using TwitchChatBot.Service;
using System.Net;


namespace TwitchChatBot.Controllers
{
    public class MemberController : Controller
    {
        
        private readonly MemberService _service;

        public MemberController(MemberService service)
        {
            _service = service;
        }

        /// <summary>
        /// Member View로 이동하는 GET 메소드
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("/member/index")]
        public IActionResult Index(string code, string scope)
        {
            if(string.IsNullOrEmpty(code))
            {
            return Redirect("https://id.twitch.tv/oauth2/authorize?client_id=jjvh028bmtssj5x8fov8lu3snk3wut&redirect_uri=https://localhost:44348/member/index&response_type=code&scope=viewing_activity_read");
            }
            Tuple<TwitchToken,User> tuple = _service.GetReleasesWebClient(code, "https://localhost:44348/member/index");

            if(Request.Cookies["user_id"] == null)
            {
                Response.Cookies.Append("access_token", tuple.Item1.AccessToken);
                Response.Cookies.Append("id", tuple.Item2.UserId);
            }
            

            return RedirectToAction("Index", "Home");
        }

    }
}
