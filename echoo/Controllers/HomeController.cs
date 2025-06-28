using System.Diagnostics;
using echoo.Data;
using echoo.Models;
using echoo.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace echoo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, SignInManager<User> signInManager, UserManager<User> userManager, ApplicationDbContext context)
        {
            _logger = logger;
            this.signInManager = signInManager;
            this.userManager = userManager;
            _context = context;

        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Promo()
        {
            return View();
        }
        [Authorize]
        public async Task<IActionResult> Main()
        {
            var currentUser = await userManager.GetUserAsync(User);

            ViewBag.User = currentUser.FirstName +" "+ currentUser.LastName;


            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Private chats
            var privateChats = await (
                from session in _context.ChatSessions
                where session.UserId == currentUser.Id || session.ToUserId == currentUser.Id
                let otherUserId = session.UserId == currentUser.Id ? session.ToUserId : session.UserId
                let lastMessageDate = _context.Chats
                    .Where(c =>
                        (c.UserId == currentUser.Id && c.ToUserId == otherUserId) ||
                        (c.ToUserId == currentUser.Id && c.UserId == otherUserId))
                    .Max(c => (DateTime?)c.Date)
                join user in _context.Users on otherUserId equals user.Id
                select new ChatListViewModel
                {
                    IsGroup = false,
                    User = user,
                    Group = null,
                    LastMessageDate = lastMessageDate ?? DateTime.MaxValue
                }
            ).ToListAsync();

            // Group chats
            var groupChats = await (
                from member in _context.GroupMembers
                where member.UserId == currentUser.Id
                    join groups in _context.Groups on member.GroupId equals groups.Id
            let lastGroupMessage = _context.GroupMessages
                .Where(m => m.GroupId == groups.Id)
                .Max(m => (DateTime?)m.Date)
            select new ChatListViewModel
            {
                IsGroup = true,
                User = null,
                Group = groups,
                LastMessageDate = lastGroupMessage ?? DateTime.MaxValue
            }
        ).ToListAsync();

            // Combine and sort
            var allChats = privateChats.Concat(groupChats)
                .OrderByDescending(x => x.LastMessageDate)
                .ToList();

            return View(allChats);
        }







    }
}
