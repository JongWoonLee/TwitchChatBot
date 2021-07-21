using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using TwitchChatBot.Models;
using TwitchChatBot.Service;

namespace TwitchChatBot.Controllers
{
    public class MemberController : Controller
    {

        private readonly MemberService MemberService;
        private readonly ThreadExecutorService ThreadExecutorService;

        /// <summary>
        /// API 컨트롤러
        /// </summary>
        /// <param name="MemberService">Member관련 로직 서비스</param>
        /// <param name="ThreadExecutorService">Bot관련 로직 서비스</param>
        public MemberController(MemberService MemberService, ThreadExecutorService ThreadExecutorService)
        {
            this.MemberService = MemberService;
            this.ThreadExecutorService = ThreadExecutorService;
        }

        /// <summary>
        /// Twitch Login
        /// </summary>
        /// <param name="Code">Token을 가져오기 위한 AccessCode</param>
        /// <returns></returns>
        [HttpGet, Route("/member/index")]
        public IActionResult Index(string Code)
        {
            if (string.IsNullOrWhiteSpace(Code)) // code 가 없으면 코드를 가져오는 요청을 하고
            {
                string url = MemberService.GetRedirectURL();
                return Redirect(url);
            }
            TwitchToken TwitchToken = MemberService.ConnectReleasesWebClient(Code); // code 가 있으면 Token을 가지고 온 뒤
            User User = MemberService.ValidatingRequests(TwitchToken.AccessToken); // Token 을 이용해서 User data를 얻어오고
            int InsertResult = MemberService.FindStreamer(User.UserId).StreamerIsValid() ? 
            MemberService.InsertStreamer(TwitchToken, User) : 0; // Streamer Table 에 Insert 유효하지 않은 Insert면 0 반환

            if (string.IsNullOrWhiteSpace(Request.Cookies["user_id"])) // Cookie 에 User정보가 없으면 저장.
            {
                Response.Cookies.Append("access_token", TwitchToken.AccessToken);
                Response.Cookies.Append("user_id", Convert.ToString(User.UserId));
                Response.Cookies.Append("channel_name", User.Login);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet, Route("/member/details")]
        public IActionResult Details()
        {
            string UserId = Request.Cookies["user_id"];
            long lUserId = Convert.ToInt64(UserId);
            ViewData["streamer_detail"] = MemberService.FindStreamerDetail(lUserId);
            return View();
        }

        [HttpPost, Route("/member/details")]
        public string DetailsPost()
        {
            StreamerDetail StreamerDetail = null;
            try
            {
                using (var Reader = new StreamReader(Request.Body))
                {
                    var Body = Reader.ReadToEndAsync();
                    string userId = Request.Cookies["user_id"];
                    long StreamerId = Convert.ToInt64(userId);
                    string ChannelName = Request.Cookies["channel_name"];

                    JObject JObject = JObject.Parse(Body.Result);
                    var GreetingMessage = (string)JObject["GreetingMessage"];
                    var DonationLink = (string)JObject["DonationLink"];
                    var ForbiddenWordLimit = (bool)JObject["ForbiddenWordLimit"];
                    var ForbiddenWordTimeout = Convert.ToInt32(JObject["forbiddenWordTimeout"]);
                    StreamerDetail = new StreamerDetail(StreamerId, ChannelName, DonationLink, GreetingMessage, ForbiddenWordLimit ? 1 : 0, ForbiddenWordTimeout);
                    MemberService.UpdateStreamerDetail(StreamerDetail);
                }
                return "success";
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return "failed";
            }
            
        }

        /// <summary>
        /// Startbot View User 의 봇사용 정보를 가지고 이동
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("/member/startbot")]
        public IActionResult StartBot()
        {
            string UserId = Request.Cookies["user_id"];
            if (!string.IsNullOrWhiteSpace(UserId))
            {
                int Result = MemberService.FindBotInUseByUserId(UserId);
                ViewData["result"] = Result;
            }
            return View();
        }

        /// <summary>
        /// 봇 사용 여부를 받아 봇을 실행/폐기
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("/member/startbot")]
        public string StartBotPost()
        {
            int BotInUse = 0;
            string Result = null;
            using (var Reader = new StreamReader(Request.Body))
            {
                var Body = Reader.ReadToEndAsync();
                JObject JObject = JObject.Parse(Body.Result);
                BotInUse = (int)JObject["BotInUse"]; // Request에 들어있는 BotInUse
            }
            string userId = Request.Cookies["user_id"];
            long lUserId = Convert.ToInt64(userId);
            string ChannelName = Request.Cookies["channel_name"];
            string AccessToken = Request.Cookies["access_token"];

            if (BotInUse == 1) // 봇을 사용 한다면
            {
                Streamer Streamer = MemberService.FindStreamer(lUserId);
                MemberService.UpdateStreamerDetailBotInUse(lUserId, BotInUse);
                ThreadExecutorService.RegisterBot(lUserId, ChannelName, ChannelName, new TwitchToken(AccessToken, Streamer.RefreshToken));
            }
            else // 봇을 사용 안한다면
            {
                MemberService.UpdateStreamerDetailBotInUse(lUserId, BotInUse);
                Result = ThreadExecutorService.DisposeBot(lUserId);
            }
            return Result;
        }

        [HttpGet, Route("/member/logout")]
        public IActionResult Logout()
        {
            if (!string.IsNullOrWhiteSpace(Request.Cookies["user_id"])) // Cookie 에 User정보가 없으면 저장.
            {
                Response.Cookies.Delete("access_token");
                Response.Cookies.Delete("user_id");
                Response.Cookies.Delete("channel_name");
            }
            return RedirectToAction("Index", "Home");
        }
    }


}
