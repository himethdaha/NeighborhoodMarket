using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NeighborhoodMarket.DataAccess.Data.Repository.IRepository;
using NeighborhoodMarket.Models;
using NeighborhoodMarket.Utilities;

namespace NeighborhoodMarket.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string Name { get; set; }
            [Required]
            public string StreetAddress1 { get; set; }
            public string StreetAddress2 { get; set; }
            [Required]
            public string State { get; set; }
            [Required]
            public string City { get; set; }
            public string PostalCode { get; set; }
            [Required]
            [DataType(DataType.PhoneNumber)]
            [Display(Name = "Phone Number")]
            [RegularExpression(@"^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$")]
            public string Phone { get; set; }
            public string Role { get; set; }

            //DropDown for admin when creating a user
            public IEnumerable<SelectListItem> RoleList{ get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            //Populating RoleList
            Input = new InputModel()
            {
                //Where clause-> Admin can't register new individual users
                RoleList = _roleManager.Roles.Where(u => u.Name != StaticDetails.Role_User_Indi).Select(x=>x.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                })
            };

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                //User Object
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    Name = Input.Name,
                    StreetAddress1 = Input.StreetAddress1,
                    StreetAddress2 = Input.StreetAddress2,
                    City = Input.City,
                    State = Input.State,
                    PostalCode = Input.PostalCode,
                    PhoneNumber = Input.Phone,
                    
                };
                //Creating the user
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    //Assigning user roles
                    //Checking for role in the db
                    if(!await _roleManager.RoleExistsAsync(StaticDetails.Role_User_Admin))
                    {
                        //If not found create role
                        await _roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_User_Admin));
                    }
                    if (!await _roleManager.RoleExistsAsync(StaticDetails.Role_User_Indi))
                    {
                        //If not found create role
                        await _roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_User_Indi));
                    }
                    if (!await _roleManager.RoleExistsAsync(StaticDetails.Role_User_Emp))
                    {
                        //If not found create role
                        await _roleManager.CreateAsync(new IdentityRole(StaticDetails.Role_User_Emp));
                    }

                    //Grant admin permission to every user created
                    await _userManager.AddToRoleAsync(user, StaticDetails.Role_User_Admin);

                    if(user.Role == null)
                    {
                        await _userManager.AddToRoleAsync(user, StaticDetails.Role_User_Indi);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, user.Role);
                    }
                    //SEND CONFIRMATION EMAIL
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        //Means user is signing up from the website. The admin isn't creating one
                        if(user.Role == null)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            return LocalRedirect(returnUrl);
                        }
                        //Admin creating the user
                        else
                        {
                            return RedirectToAction("Index", "ApplicationUser", new { Area = "Admin" });
                        }
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            //Populating RoleList
            Input = new InputModel()
            {
                //Where clause-> Admin can't register new individual users
                RoleList = _roleManager.Roles.Where(u => u.Name != StaticDetails.Role_User_Indi).Select(x => x.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                })
            };
            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
