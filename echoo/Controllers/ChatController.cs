using echoo.Data;
using echoo.Hubs;
using echoo.Models;
using echoo.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace echoo.Controllers
{

    [Authorize]
    public class ChatController : Controller
    {
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> hubContext;

        public ChatController(IHubContext<ChatHub> hubContext, SignInManager<User> signInManager, UserManager<User> userManager, ApplicationDbContext context)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this._context = context;
            this.hubContext = hubContext;
        }


        public async Task<IActionResult> GetChatHistory(string userId)
        {
            var currentUser = await userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Unauthorized(); 
            }

            var chats = await _context.Chats
                .Where(c => (c.UserId == currentUser.Id && c.ToUserId == userId) ||
                            (c.UserId == userId && c.ToUserId == currentUser.Id))
                .OrderBy(c => c.Date)
                .Select(c => new {c.Id, c.UserId, c.ToUserId, c.Message, c.Date ,c.isStarred})
                .ToListAsync();

            return Json(chats);
        }

        [HttpPost]
        public async Task<IActionResult> OpenChat(string toUserId) //contactlist method
        {
            var currentUser = await userManager.GetUserAsync(User);

            var existingChatSession = _context.ChatSessions
                .FirstOrDefault(cs => (cs.UserId == currentUser.Id && cs.ToUserId == toUserId) ||
                                      (cs.UserId == toUserId && cs.ToUserId == currentUser.Id));

            if (existingChatSession == null)
            {
                var newChatSession = new ChatSession
                {
                    UserId = currentUser.Id,
                    ToUserId = toUserId,
                    DateStarted = DateTime.UtcNow
                };

                _context.ChatSessions.Add(newChatSession);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Main","Home");
        }

        public async Task<IActionResult> ChatDetails(string toUserId) 
        {

            var user = await _context.Users
                .Where(u => u.Id == toUserId)
                .FirstOrDefaultAsync();
            var currentUser = await userManager.GetUserAsync(User);



            if (user == null)
            {
                return NotFound("User not found.");
            }

            var model = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicturePath = user.ProfilePicturePath,
                About = user.About 
            };
            var files = await _context.FileMessages
                .Where(c  => ( (c.ToUserId == user.Id && c.UserId ==currentUser.Id) || (c.ToUserId==currentUser.Id && c.UserId == user.Id)))
                .OrderByDescending(c => c.Date)
                .ToListAsync();

            ViewBag.FileMessages = files;

            return View(model);
        }

        public async Task<IActionResult> ToggleStarMessage(int messageId)
        {
            // First try to find in regular chat messages
            var chatMessage = await _context.Chats.FindAsync(messageId);
            if (chatMessage != null)
            {
                chatMessage.isStarred = !chatMessage.isStarred;
                await _context.SaveChangesAsync();
                return Ok();
            }

            // If not found in regular chats, check group messages
            var groupMessage = await _context.GroupMessages.FindAsync(messageId);
            if (groupMessage != null)
            {
                groupMessage.isStarred = !groupMessage.isStarred;
                await _context.SaveChangesAsync();
                return Ok();
            }

            return NotFound();
        }

        public async Task<IActionResult> StarredMessages(string ToUserId)
        {
            var currentUserId = userManager.GetUserId(User);

            var starredMessages = _context.Chats
                .Where(c =>
                    (c.UserId == currentUserId && c.ToUserId == ToUserId ||
                     c.UserId == ToUserId && c.ToUserId == currentUserId)
                    && c.isStarred
                )
                .OrderBy(c => c.Date)
                .Select(c => new StarredItemViewModel
                {
                    Id = c.Id,
                    MessageText = c.Message,
                    UserId = c.UserId,
                    Date = c.Date,
                    IsFile = false
                })
                .ToList();

            var starredFiles = _context.FileMessages
                .Where(f =>
                    (f.UserId == currentUserId && f.ToUserId == ToUserId ||
                     f.UserId == ToUserId && f.ToUserId == currentUserId)
                    && f.isStarred
                )
                .OrderBy(f => f.Date)
                .Select(f => new StarredItemViewModel
                {
                    Id = f.Id,
                    FilePath = f.FilePath,
                    FileName = f.FileName,
                    FileType = f.FileType,
                    UserId = f.UserId,
                    Date = f.Date,
                    IsFile = true
                })
                .ToList();

            var combined = starredMessages.Concat(starredFiles)
                .OrderBy(item => item.Date)
                .ToList();

            return View(combined);
        }


        //FILES

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, string toUserId = null, int? groupId = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            if ((toUserId == null && groupId == null) || (toUserId != null && groupId != null))
                return BadRequest("Either provide a user ID or a group ID, not both or neither.");

            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            // if group message verify the user is a member of the group
            if (groupId.HasValue)
            {
                var isMember = await _context.GroupMembers
                    .AnyAsync(m => m.GroupId == groupId.Value && m.UserId == currentUser.Id);

                if (!isMember)
                    return Forbid("You are not a member of this group.");
            }

            // allowed file extensions
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var allowedDocumentExtensions = new[] { ".pdf", ".doc", ".docx", ".xlsx", ".xls", ".txt" };

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedImageExtensions.Contains(fileExtension) && !allowedDocumentExtensions.Contains(fileExtension))
                return BadRequest("File type not allowed.");

            // determine file type
            FileType fileType = allowedImageExtensions.Contains(fileExtension) ? FileType.Image : FileType.Document;

            // create a unique file name
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            // set up path for files
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            // create directory if it doesn't exist used when matching users 
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // create file path
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var fileMessage = new FileMessage
            {
                UserId = currentUser.Id,
                ToUserId = toUserId,
                GroupId = groupId,
                FilePath = $"/uploads/{uniqueFileName}",
                FileName = file.FileName,
                FileType = fileType,
                Date = DateTime.Now,
                isStarred = false
            };

            await _context.FileMessages.AddAsync(fileMessage);
            await _context.SaveChangesAsync();

            return Json(new
            {
                id = fileMessage.Id,
                userId = fileMessage.UserId,
                toUserId = fileMessage.ToUserId,
                groupId = fileMessage.GroupId,
                filePath = fileMessage.FilePath,
                fileName = fileMessage.FileName,
                fileType = fileMessage.FileType.ToString(),
                date = fileMessage.Date,
                isStarred = fileMessage.isStarred
            });
        }

        // Method to get file message history
        public async Task<IActionResult> GetFileMessageHistory(string userId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var fileMessages = await _context.FileMessages
                .Where(fm => (fm.UserId == currentUser.Id && fm.ToUserId == userId) ||
                            (fm.UserId == userId && fm.ToUserId == currentUser.Id))
                .OrderBy(fm => fm.Date)
                .Select(fm => new {
                    fm.Id,
                    fm.UserId,
                    fm.ToUserId,
                    fm.FilePath,
                    fm.FileName,
                    FileType = fm.FileType.ToString(),
                    fm.Date,
                    fm.isStarred
                })
                .ToListAsync();

            return Json(fileMessages);
        }

        // toggle star on file messages
        public async Task<IActionResult> ToggleStarFileMessage(int messageId)
        {
            var fileMessage = await _context.FileMessages.FindAsync(messageId);
            if (fileMessage != null)
            {
                fileMessage.isStarred = !fileMessage.isStarred;
                await _context.SaveChangesAsync();
                return Ok();
            }

            return NotFound();
        }

        public async Task<IActionResult> GetDocuments(string toUserId)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var fileMessages = await _context.FileMessages
                .Where(fm => (fm.UserId == currentUser.Id && fm.ToUserId == toUserId) ||
                            (fm.UserId == toUserId && fm.ToUserId == currentUser.Id))
                .OrderByDescending(fm => fm.Date) 
                .Select(fm => new {
                    fm.Id,
                    fm.UserId,
                    fm.ToUserId,
                    fm.FilePath,
                    fm.FileName,
                    FileType = fm.FileType.ToString(),
                    fm.Date,
                    fm.isStarred
                })
                .ToListAsync();

            return Json(fileMessages);
        }





    }
}
