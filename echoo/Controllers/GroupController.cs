using System.Reflection;
using System.Security.Claims;
using echoo.Data;
using echoo.Models;
using echoo.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace echoo.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;
        private const int MaxGroupMembers = 100;

        public GroupController(ApplicationDbContext context, UserManager<User> userManager, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> CreateGroup()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var contacts = await _context.Contacts
                .Where(c => c.OwnerId == currentUser.Id )
                .Select(c => new
                {
                    id = c.FriendId,
                    name = c.Friend.FirstName+" "+ c.Friend.LastName,
                    profilePicture = c.Friend.ProfilePicturePath
                })
                .ToListAsync();

            var contac = await _context.Contacts
                .Where(c => c.FriendId == currentUser.Id)
                .Select(c => new
                {
                    id = c.OwnerId,
                    name = c.Owner.FirstName + " " + c.Owner.LastName,
                    profilePicture = c.Owner.ProfilePicturePath
                })
                .ToListAsync();

            var all = contacts.Concat(contac);
            ViewBag.Contacts = all;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(string groupName, IFormFile groupPhoto, List<string> members)
        {
            if (string.IsNullOrEmpty(groupName) || members == null || !members.Any())
            {
                return BadRequest("Group name and at least one member are required");
            }

            if (members.Count + 1 > MaxGroupMembers) // +1 for the current user
            {
                return Json(new { success = false, message = $"Group cannot have more than {MaxGroupMembers} members." });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            string photoPath = null;
            if (groupPhoto != null && groupPhoto.Length > 0)//checks group photo is default or not
            {
                var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "img", "groups");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // unique filename
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + groupPhoto.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await groupPhoto.CopyToAsync(fileStream);
                }

                photoPath = "/img/groups/" + uniqueFileName;
            }
            else
            {
                photoPath = "/img/default-group.png";
            }

            var group = new Group
            {
                Name = groupName,
                PhotoPath = photoPath,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.Now
            };

            _context.Add(group);
            await _context.SaveChangesAsync();

            var groupMembers = new List<GroupMember>();

            // current user as admin
            groupMembers.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId = currentUser.Id,
                JoinedAt = DateTime.Now,
                IsAdmin = true
            });

            // Add other members
            foreach (var memberId in members)
            {
                // Skip current user, already added
                if (memberId == currentUser.Id)
                    continue;

                groupMembers.Add(new GroupMember
                {
                    GroupId = group.Id,
                    UserId = memberId,
                    JoinedAt = DateTime.Now,
                    IsAdmin = false
                });
            }

            _context.GroupMembers.AddRange(groupMembers);

            var chatSession = new ChatSession
            {
                UserId = currentUser.Id,
                ToUserId = string.Empty, // Not used for groups
                GroupId = group.Id,
                DateStarted = DateTime.Now
            };

            _context.ChatSessions.Add(chatSession);
            await _context.SaveChangesAsync();

            return Json(new { success = true, groupId = group.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupMessages(int groupId)
        {
            if (groupId == 0)
            {
                return BadRequest("Group ID is required.");
            }

            var messages = await _context.GroupMessages
                .Where(m => m.GroupId == groupId)
                .OrderBy(m => m.Date)
                .Include(m => m.User) // this pulls the sender's info
                .Select(m => new
                {
                    m.Id,
                    m.GroupId,
                    m.UserId,
                    m.isStarred,
                    m.Message,
                    Date = m.Date,
                    SenderName = m.User.FirstName + " " + m.User.LastName,
                    SenderProfilePicture = string.IsNullOrEmpty(m.User.ProfilePicturePath) ? "/img/Default.jpg" : m.User.ProfilePicturePath
                })
                .ToListAsync();

            return Json(messages);
        }

        public async Task<IActionResult> GroupDetails(int groupId)
        {
            var group = await _context.Groups
                .Where(g => g.Id == groupId)
                .FirstOrDefaultAsync();

            var groupMembers = await _context.GroupMembers
                .Include(m => m.User)
                .Where(m => m.GroupId == groupId)
                .ToListAsync();

            
                var createdByUserName = await _context.Users
                    .Where(u => u.Id == group.CreatedByUserId)  
                    .Select(u => new
                    {
                        UserName = u.FirstName + " " + u.LastName,
                    })
                    .FirstOrDefaultAsync();

                var viewModel = new GroupViewModel
            {
                Id = group.Id,
                Name = group.Name,
                PhotoPath = group.PhotoPath,
                CreatedByUserId = group.CreatedByUserId,
                CreatedByUsername = createdByUserName.UserName,
                Members = groupMembers,
                Admins = groupMembers.Where(m => m.IsAdmin).Select(m => m.User).ToList()
            };

            var fileMessages = await _context.FileMessages
                .Where(f => f.GroupId == group.Id)
                .OrderByDescending(f => f.Date)
                .ToListAsync();

            ViewBag.FileMessages = fileMessages;

            return View(viewModel);

        }

        public async Task<IActionResult> EditGroup(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser.Id;

            var group = await _context.Groups
                 .Include(g => g.Members)
                 .ThenInclude(m => m.User)
                 .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var existingMemberIds = group.Members.Select(m => m.UserId).ToList();

            var availableContacts = await _context.Contacts
                .Where(c => (c.OwnerId == currentUserId || c.FriendId == currentUserId) &&
                           !existingMemberIds.Contains(c.OwnerId == currentUserId ? c.FriendId : c.OwnerId))
                .Select(c => new {
                    Id = c.OwnerId == currentUserId ? c.FriendId : c.OwnerId,
                    User = c.OwnerId == currentUserId ? c.Friend : c.Owner
                })
                .Select(x => new {
                    Id = x.Id,
                    FirstName = x.User.FirstName,
                    LastName = x.User.LastName,
                    ProfilePicturePath = x.User.ProfilePicturePath ?? "/img/Default.jpg"
                })
                .ToListAsync();

            var viewModel = new EditGroupViewModel
            {
                Id = id,
                Name = group.Name,
                CreatedByUserId = group.CreatedByUserId,
                Members = group.Members,
                SelectedMemberIds = group.Members.Select(m => m.UserId).ToList(),
                ProfilePicturePath = group.PhotoPath
            };

            ViewBag.Contacts = availableContacts;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditGroup(EditGroupViewModel viewModel)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == viewModel.Id);

            if (group == null)
            {
                return Json(new { success = false, message = "Group not found." });
            }

            group.Name = viewModel.Name;

            // Update group photo
            if (viewModel.Photo != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(viewModel.Photo.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Invalid file type. Only .jpg, .jpeg, .png files are allowed." });
                }

                const long maxSizeInBytes = 5 * 1024 * 1024; // 5 MB
                if (viewModel.Photo.Length > maxSizeInBytes)
                {
                    return Json(new { success = false, message = "File size should not exceed 5 MB." });
                }

                var newFileName = $"{Guid.NewGuid()}{fileExtension}";
                var newFilePath = Path.Combine("wwwroot/img/groups", newFileName);

                try
                {
                    using (var stream = new FileStream(newFilePath, FileMode.Create))
                    {
                        await viewModel.Photo.CopyToAsync(stream);
                    }

                    // delete old photo if not default and not same
                    if (!string.IsNullOrEmpty(group.PhotoPath) && !group.PhotoPath.Contains("default-group.png"))
                    {
                        var currentFileName = Path.GetFileName(group.PhotoPath);
                        if (!currentFileName.Equals(newFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            var oldFilePath = Path.Combine("wwwroot", group.PhotoPath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                    }

                    group.PhotoPath = "/img/groups/" + newFileName;
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Failed to upload photo: " + ex.Message });
                }
            }

            var selectedUserIds = viewModel.SelectedMemberIds ?? new List<string>();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!selectedUserIds.Contains(currentUserId))
            {
                return Json(new { success = false, message = "You can't remove yourself in edit mode. Use the Leave Group option instead." });
            }

            if (selectedUserIds.Count > MaxGroupMembers)
            {
                return Json(new { success = false, message = $"Group cannot have more than {MaxGroupMembers} members." });
            }

            foreach (var member in group.Members.ToList())
            {

                if (!selectedUserIds.Contains(member.UserId))
                {
                    group.Members.Remove(member);
                }
            }

            foreach (var userId in selectedUserIds)
            {
                if (!group.Members.Any(m => m.UserId == userId))
                {
                    group.Members.Add(new GroupMember { GroupId = group.Id, UserId = userId });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, groupId = group.Id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return Json(new { success = false, message = "Group not found" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            if (group.CreatedByUserId != currentUser.Id)
            {
                return Json(new { success = false, message = "Only the creator of the group can delete it" });
            }

            // transaction to delete group messages,files,members,chatssesions,group; everything about a group
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var groupMessages = await _context.GroupMessages
                        .Where(m => m.GroupId == id)
                        .ToListAsync();

                    _context.GroupMessages.RemoveRange(groupMessages);

                    var fileMessages = await _context.FileMessages
                        .Where(f => f.GroupId == id)
                        .ToListAsync();

                    _context.FileMessages.RemoveRange(fileMessages);

                    _context.GroupMembers.RemoveRange(group.Members);

                    var chatSessions = await _context.ChatSessions
                        .Where(c => c.GroupId == id)
                        .ToListAsync();

                    _context.ChatSessions.RemoveRange(chatSessions);
                    await _context.SaveChangesAsync();//otherwise chatsessions are not deleted properly

                    _context.Groups.Remove(group);

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "An error occurred: " + ex.Message });
                }
            }
        }

        public async Task<IActionResult> MakeAdmin(int groupId, string UserId)
        {

            var group = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return NotFound();
            }
            var member = group.Members.FirstOrDefault(m => m.UserId == UserId);
            var name = member.User.FirstName+" "+member.User.LastName;

            member.IsAdmin = true;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Successfully promoted "+name+" to an admin.";
            return RedirectToAction("GroupDetails", "Group", new { groupId = group.Id });

        }
        public async Task<IActionResult> UnmakeAdmin(int groupId, string UserId)
        {

            var group = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                return NotFound();
            }
            var member = group.Members.FirstOrDefault(m => m.UserId == UserId);
            var name = member.User.FirstName + " " + member.User.LastName;

            member.IsAdmin = false;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Successfully removed admin privileges from "+name;
            return RedirectToAction("GroupDetails", "Group", new { groupId = group.Id });

        }

        public async Task<IActionResult> StarredMessages(int groupId)
        {
            var currentUserId = _userManager.GetUserId(User);

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null || !group.Members.Any(m => m.UserId == currentUserId))
            {
                return NotFound();
            }

            var starredMessages = await _context.GroupMessages
                .Where(m => m.GroupId == groupId && m.isStarred)
                .OrderBy(m => m.Date)
                .Select(m => new StarredItemViewModel
                {
                    Id = m.Id,
                    MessageText = m.Message,
                    UserId = m.UserId,
                    Date = m.Date,
                    IsFile = false,
                    GroupId = groupId,
                    SenderName = m.User.FirstName+" "+m.User.LastName // include sender name for group messages
                })
                .ToListAsync();

            var starredFiles = await _context.FileMessages
                .Where(f => f.GroupId == groupId && f.isStarred)
                .Join(_context.Users, // 
                    f => f.UserId,
                    m => m.Id,
                    (f, m) => new StarredItemViewModel
                    {
                        Id = f.Id,
                        UserId = f.UserId,
                        Date = f.Date,
                        IsFile = true,
                        FileType = f.FileType,
                        FileName = f.FileName,
                        FilePath = f.FilePath,
                        GroupId = groupId,
                        SenderName = m.FirstName + " " + m.LastName 
                    })
                .OrderBy(item => item.Date)
                .ToListAsync();

            var allStarredItems = starredMessages.Concat(starredFiles)
                .OrderBy(item => item.Date)
                .ToList();


            return View(allStarredItems);
        }

        public async Task<IActionResult> GetGroupFileMessages(int groupId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == currentUser.Id);

            if (!isMember)
                return Forbid();

            // get file messages for the group
            var fileMessages = await _context.FileMessages
                .Where(fm => fm.GroupId == groupId)
                .OrderBy(fm => fm.Date)
                .Join(_context.Users, 
                    fm => fm.UserId,  
                    u => u.Id,        
                    (fm, u) => new {  
                        fm.Id,
                        fm.UserId,
                        fm.GroupId,
                        fm.FilePath,
                        fm.FileName,
                        FileType = fm.FileType.ToString(),
                        fm.Date,
                        fm.isStarred,
                        SenderName = u.FirstName + " " + u.LastName,
                        SenderProfilePicture = u.ProfilePicturePath
                    })
                .ToListAsync();

            return Json(fileMessages);
        }


        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int id)//user leaves the group and if no admins left promotes one randomly, if no users left in the group automatically deletes it.
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var group = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound("Group not found");
            }

            var membership = group.Members.FirstOrDefault(m => m.UserId == currentUser.Id);
            if (membership == null)
            {
                return BadRequest("You are not a member of this group");
            }

            //  if the leaving user is an only admin 
            bool isLastAdmin = membership.IsAdmin &&
                               !group.Members.Any(m => m.IsAdmin && m.UserId != currentUser.Id);

            // if last admin and there are other members, promote someone else
            if (isLastAdmin && group.Members.Count > 1)
            {
                var nonAdminMembers = group.Members.Where(m => !m.IsAdmin && m.UserId != currentUser.Id).ToList();
                if (nonAdminMembers.Any())
                {
                    var random = new Random();
                    int index = random.Next(nonAdminMembers.Count);
                    var newAdmin = nonAdminMembers[index];

                    newAdmin.IsAdmin = true;
                    await _context.SaveChangesAsync();
                }
            }

            bool willBeEmpty = group.Members.Count <= 1;

            _context.GroupMembers.Remove(membership);
            await _context.SaveChangesAsync();

            // if no members left, delete the group
            if (willBeEmpty)
            {
                var messages = await _context.GroupMessages
                    .Where(m => m.GroupId == id)
                    .ToListAsync();
                _context.GroupMessages.RemoveRange(messages);

                var fileMessages = await _context.FileMessages
                    .Where(fm => fm.GroupId == id)
                    .ToListAsync();
                _context.FileMessages.RemoveRange(fileMessages);

                var chatSession = await _context.ChatSessions
                    .FirstOrDefaultAsync(cs => cs.GroupId == id);
                if (chatSession != null)
                {
                    _context.ChatSessions.Remove(chatSession);
                    await _context.SaveChangesAsync();

                }

                _context.Groups.Remove(group);

                await _context.SaveChangesAsync();

            }
            TempData["Message"] = "You have successfully left the group.";
            return RedirectToAction("Main", "Home");
        }

    }

}
