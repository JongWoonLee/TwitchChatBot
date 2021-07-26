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
        /// <param name="MemberService">Member관련 서비스 로직</param>
        /// <param name="ThreadExecutorService">Bot관련 서비스 로직</param>
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
            try
            {
                // code 가 없으면 코드를 가져오는 요청
                if (string.IsNullOrWhiteSpace(Code))
                {
                    string url = MemberService.GetRedirectURL();
                    return Redirect(url);
                } // end if
                TwitchToken TwitchToken = MemberService.ConnectReleasesWebClient(Code); // code 가 있으면 TwitchToken을 얻어오고
                User User = MemberService.ValidatingRequests(TwitchToken.AccessToken); // TwitchToken 을 이용해서 User를 얻어오고
                int InsertResult = MemberService.FindStreamer(User.UserId).StreamerIsValid() ?
                MemberService.InsertStreamer(TwitchToken, User) : 0; // Streamer Table 에 Insert 유효하지 않은 Insert면 0 반환

                // Cookie 에 User정보가 없으면 저장
                if (string.IsNullOrWhiteSpace(Request.Cookies["UserId"]))
                {
                    Response.Cookies.Append("AccessToken", TwitchToken.AccessToken);
                    Response.Cookies.Append("UserId", Convert.ToString(User.UserId));
                    Response.Cookies.Append("ChannelName", User.Login);
                } // end if
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            } // end try
            return RedirectToAction("Index", "Home");
        } // end Index

        /// <summary>
        /// StreamerDetail 정보를 설정하는 페이지
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("/member/details")]
        public IActionResult Details()
        {
            string UserId = Request.Cookies["UserId"];
            long StreamerId = Convert.ToInt64(UserId);
            ViewData["StreamerDetail"] = MemberService.FindStreamerDetail(StreamerId); // StreamerId 를 가지고 StreamerDetail정보를 읽어온다.
            return View();
        } // end Details

        /// <summary>
        /// StreamerDetail 정보를 수정
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("/member/details")]
        public string DetailsPost()
        {
            try
            {
                /// 입력받은 StreamerDetail 정보를 읽어오고 Update
                using (var Reader = new StreamReader(Request.Body))
                {
                    var Body = Reader.ReadToEndAsync();
                    string userId = Request.Cookies["UserId"];
                    long StreamerId = Convert.ToInt64(userId);
                    string ChannelName = Request.Cookies["channel_name"];

                    JObject JObject = JObject.Parse(Body.Result);
                    var GreetingMessage = (string)JObject["GreetingMessage"];
                    var DonationLink = (string)JObject["DonationLink"];
                    var ForbiddenWordLimit = (bool)JObject["ForbiddenWordLimit"];
                    var ForbiddenWordTimeout = Convert.ToInt32(JObject["forbiddenWordTimeout"]);
                    StreamerDetail StreamerDetail = new StreamerDetail(StreamerId, ChannelName, DonationLink, GreetingMessage, ForbiddenWordLimit ? 1 : 0, ForbiddenWordTimeout);
                    MemberService.UpdateStreamerDetail(StreamerDetail); // StreamerDetail 정보를 Update
                } // end using
                return "success";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return "failed";
            } // end try
        } // end DetailsPost

        /// <summary>
        /// 회원의 봇 사용 여부를 설정하는 페이지
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("/member/startbot")]
        public IActionResult StartBot()
        {
            string UserId = Request.Cookies["UserId"];
            // 로그인이 되어있지 않으면 돌려보내기
            if (!string.IsNullOrWhiteSpace(UserId))
            {
                int Result = MemberService.FindBotInUseByUserId(UserId); // 유저가 봇을 사용하는지 여부를 읽어온다.
                ViewData["Result"] = Result;
                return View();
            } // end if
            return RedirectToAction("Index", "Home");
        } // end StartBot

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
            } // end using
            string UserId = Request.Cookies["UserId"];
            long StreamerId = Convert.ToInt64(UserId);
            string ChannelName = Request.Cookies["ChannelName"];
            string AccessToken = Request.Cookies["AccessToken"];

            // 봇을 사용 한다면
            if (BotInUse == 1)
            {
                Streamer Streamer = MemberService.FindStreamer(StreamerId); // 스트리머 정보를 찾아서 
                MemberService.UpdateStreamerDetailBotInUse(StreamerId, BotInUse);  // 봇 사용 여부를 Update
                ThreadExecutorService.RegisterBot(StreamerId, ChannelName, ChannelName, new TwitchToken(AccessToken, Streamer.RefreshToken)); // 봇을 실행
            }
            // 봇을 사용 안한다면
            else
            {
                MemberService.UpdateStreamerDetailBotInUse(StreamerId, BotInUse); // 봇 사용 여부를 Update
                Result = ThreadExecutorService.DisposeBot(StreamerId); // 봇을 폐기
            } // end if
            return Result;
        } // end StartBotPost

        /// <summary>
        /// Logout 하고 브라우저 정보 초기화
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("/member/logout")]
        public IActionResult Logout()
        {
            if (!string.IsNullOrWhiteSpace(Request.Cookies["UserId"])) // Cookie 에 User정보가 없으면 저장.
            {
                Response.Cookies.Delete("AccessToken");
                Response.Cookies.Delete("UserId");
                Response.Cookies.Delete("ChannelName");
            }
            return RedirectToAction("Index", "Home");
        } // end Logout()

        /// <summary>
        /// 금지어 설정 페이지
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("/member/words")]
        public IActionResult Words()
        {
            string UserId = Request.Cookies["UserId"];
            long StreamerId = Convert.ToInt64(UserId);
            var ForbiddenWordList = MemberService.FindForbiddenWordList(StreamerId); // 유저의 금지어 정보를 읽어온다

            ViewData["ForbiddenWordList"] = ForbiddenWordList;
            return View();
        } // end Words

        [HttpPost, Route("/member/words")]
        public string WordsPost()
        {
            int Result = 0;
            string ForbiddenWord = "";
            string Todo = "";
            string PrevWord = "";

            // Request Parameter 수집
            try
            {
                using (var Reader = new StreamReader(Request.Body))
                {
                    var Body = Reader.ReadToEndAsync();
                    JObject JObject = JObject.Parse(Body.Result);
                    ForbiddenWord = ((string)JObject["ForbiddenWord"]).Trim(); // 금지어
                    Todo = (string)JObject["Todo"]; // 작업의 이름
                    PrevWord = (string)JObject["PrevWord"]; // 이전 입력 내용(Update 시에만 필요)
                } // end using

                string UserId = Request.Cookies["UserId"];
                long StreamerId = Convert.ToInt64(UserId);

                // Todo 값에 의해 3가지 작업 분리
                switch (Todo)
                {
                    case "Insert":
                        Result = MemberService.InsertForbiddenWord(StreamerId, ForbiddenWord);
                        ThreadExecutorService.ManagedBot[StreamerId].RenewForbiddenWordList();
                        return "Insert";
                    case "Update":
                        Result = MemberService.UpdateForbiddenWord(StreamerId, ForbiddenWord, PrevWord);
                        ThreadExecutorService.ManagedBot[StreamerId].RenewForbiddenWordList();
                        return "Update";
                    case "Delete":
                        Result = MemberService.DeleteForbiddenWord(StreamerId, ForbiddenWord);
                        ThreadExecutorService.ManagedBot[StreamerId].RenewForbiddenWordList();
                        return "Delete";
                } // end switch
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            if (Result == 0)
            {
                return null;
            } // end if
            return "success";
        } // end WordsPost
    } // end class
} // end namespace
