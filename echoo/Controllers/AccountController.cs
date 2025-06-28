using echoo.Data;
using echoo.Models;
using echoo.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace echoo.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, ApplicationDbContext context)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this._context = context;  
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await userManager.FindByEmailAsync(model.email);
                var result = await signInManager.PasswordSignInAsync(model.email, model.password, true, false);
                if (result.Succeeded)
                {
                        return RedirectToAction("Main", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email or password is incorrect");
                    return View(model);
                }

            }
            return View(model);

        }
        

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {

            if (ModelState.IsValid)
            {
                User user = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.email,
                    UserName = model.email,
                };

                var result = await userManager.CreateAsync(user, model.password);
                if (result.Succeeded )
                {
                    TempData["ShowRegisterPopup"] = true;
                    TempData["PopupMessage"] = "You have successfully registered!";

                }
                else
                {
                    foreach (var error in result.Errors)
                    {

                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

            }
            return View(model);

        }



        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Promo", "Home");
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddContact(addContactViewModel model, string returnUrl = null)
        {
            var currentUser = await userManager.GetUserAsync(User);
            
            if (!ModelState.IsValid)
            {
                model.SuggestedContacts = await GetSuggestedContacts(currentUser);
                return returnUrl == "GetProfile"
                    ? RedirectToAction("GetProfile", new { toUserId = model.FriendId, showError = true, errorMessage = "Invalid model state." })
                    : View(model);
            }

            var friend = await userManager.FindByEmailAsync(model.Email);

            if (friend == null)
            {
                ModelState.AddModelError(string.Empty, "No user found with that email.");

                if (returnUrl == "GetProfile")
                {
                    TempData["ErrorMessage"] = "No user found with that email.";
                    return RedirectToAction("GetProfile", new { toUserId = model.FriendId });
                }

                model.SuggestedContacts = await GetSuggestedContacts(currentUser);
                return View(model);
            }

            if (friend.Id == currentUser.Id)
            {
                ModelState.AddModelError(string.Empty, "You can't add yourself.");

                if (returnUrl == "GetProfile")
                {
                    TempData["ErrorMessage"] = "You can't add yourself.";
                    return RedirectToAction("GetProfile", new { toUserId = model.FriendId });
                }

                model.SuggestedContacts = await GetSuggestedContacts(currentUser);
                return View(model);
            }

            bool alreadyExists = await _context.Contacts
                .AnyAsync(c => c.OwnerId == currentUser.Id && c.FriendId == friend.Id
                || c.FriendId == currentUser.Id && c.OwnerId== friend.Id);

            if (alreadyExists)
            {
                ModelState.AddModelError(string.Empty, "This contact is already in your list.");

                if (returnUrl == "GetProfile")
                {
                    TempData["ErrorMessage"] = "This contact is already in your list.";
                    return RedirectToAction("GetProfile", new { toUserId = model.FriendId });
                }

                model.SuggestedContacts = await GetSuggestedContacts(currentUser);
                return View(model);
            }

            var contact = new Contact
            {
                OwnerId = currentUser.Id,
                FriendId = friend.Id
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
            TempData["ShowRegisterPopup"] = true;
            TempData["PopupMessage"] = "You have successfully added to your contact!";

            return returnUrl == "GetProfile"
                ? RedirectToAction("GetProfile", new { toUserId = friend.Id })
                : RedirectToAction("AddContactPage");
        }

        [Authorize]
        public async Task<IActionResult> AddContactPage()
        {
            var currentUser = await userManager.GetUserAsync(User);

            var model = new addContactViewModel
            {
                SuggestedContacts = await GetSuggestedContacts(currentUser)
            };

            return View("AddContact", model); 
        }
        [Authorize]
        private async Task<List<User>> GetSuggestedContacts(User currentUser)
        {
            // get all friend IDs 
            var myFriendIds = await _context.Contacts
                .Where(c => c.OwnerId == currentUser.Id || c.FriendId == currentUser.Id)
                .Select(c => c.OwnerId == currentUser.Id ? c.FriendId : c.OwnerId)
                .ToListAsync();

            var myFriendSet = myFriendIds.ToHashSet();

            // find friends-of-friends excluding myself and existing friends
            var suggestedFriendIds = await _context.Contacts
                .Where(c =>
                    myFriendSet.Contains(c.OwnerId) || myFriendSet.Contains(c.FriendId))
                .Select(c => new { c.OwnerId, c.FriendId })
                .ToListAsync();

            var secondDegreeFriendIds = suggestedFriendIds
                .SelectMany(x => new[] { x.OwnerId, x.FriendId }) // flatten both sides
                .Where(id => id != currentUser.Id && !myFriendSet.Contains(id)) // exclude myself and my friends
                .Distinct()
                .ToList();

            var suggestedUsers = await _context.Users
                .Where(u => secondDegreeFriendIds.Contains(u.Id))
                .ToListAsync();

            return suggestedUsers;
        }

        [Authorize]
        public async Task<IActionResult> ContactList()
        {
            var currentUser = await userManager.GetUserAsync(User);

            var contacts = await _context.Contacts
              .Where(c => c.OwnerId == currentUser.Id || c.FriendId == currentUser.Id) 
              .Include(c => c.Friend)  // we include the Friend entity to get the user's details
              .Select(c => c.OwnerId == currentUser.Id ? c.Friend : c.Owner)  
              .ToListAsync();

            return View(contacts);  
        }
        [Authorize]
        public async Task<IActionResult> myProfile(EditProfileViewModel models)
        {
            if (models != null)
            {
                View(models);
            }
            var user = await userManager.GetUserAsync(User);

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                About = user.About,
                email = user.Email,
                Id = user.Id,
                ProfilePicturePath = user.ProfilePicturePath
            };
            return View(model);
        }
        [Authorize]
        public async Task<IActionResult> GetProfile(string toUserId)
        {
            var user = await _context.Users.Where(u => u.Id == toUserId).FirstOrDefaultAsync();

            var contacts = await _context.Contacts
              .Where(c => c.OwnerId == user.Id || c.FriendId == user.Id)
              .Include(c => c.Friend)  // we include the Friend entity to get the user's details
              .Select(c => c.OwnerId == user.Id ? c.Friend : c.Owner)
              .ToListAsync();

            ViewBag.contacts = contacts;

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                About = user.About,
                email = user.Email,
                Id = toUserId,
                ProfilePicturePath = user.ProfilePicturePath
            };



            if (TempData["ErrorMessage"] != null)
            {
                ModelState.AddModelError(string.Empty, TempData["ErrorMessage"].ToString());
            }

            return View("myProfile", model);
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await userManager.GetUserAsync(User);
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.About = model.About;

            if (model.ProfilePicture != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(model.ProfilePicture.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError(string.Empty, "Invalid file type. Only .jpg, .jpeg, .png files are allowed.");
                    return View("myProfile",model);
                }

                const long maxSizeInBytes = 5 * 1024 * 1024; // 5 MB
                if (model.ProfilePicture.Length > maxSizeInBytes)
                {
                    ModelState.AddModelError("", "File size should not exceed 5 MB.");
                    return View("myProfile", model);
                }

                // generate new file name
                var newFileName = $"{Guid.NewGuid()}{fileExtension}";
                var newFilePath = Path.Combine("wwwroot/img/users", newFileName);

                // save the new file
                using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(stream);
                }

                // delete old file if it’s not the default and different from the new one
                if (!string.IsNullOrEmpty(user.ProfilePicturePath) &&
                    !user.ProfilePicturePath.Contains("Default.jpg"))
                {
                    var currentFileName = Path.GetFileName(user.ProfilePicturePath);
                    if (!currentFileName.Equals(newFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        var oldFilePath = Path.Combine("wwwroot", user.ProfilePicturePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                }

                // update the user's profile picture path
                user.ProfilePicturePath = "/img/users/" + newFileName;
            }

            await userManager.UpdateAsync(user);
            return RedirectToAction("myProfile");
        }
       
    }
}
