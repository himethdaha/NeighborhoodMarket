﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NeighborhoodMarket.DataAccess.Data.Repository.IRepository;
using NeighborhoodMarket.Models.ViewModels;
using NeighborhoodMarket.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace NeighborhoodMarket.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;

        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender, UserManager<IdentityUser> userManager)

        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            //Get the logged in user Id
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //Retrieving shoppingcart from the database
            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderOverview = new Models.OrderOverview(),
                CartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product")

            };

            ShoppingCartVM.OrderOverview.OrderTotal = 0;
            //Populate the applicationUser
            ShoppingCartVM.OrderOverview.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            //Loop through all the items in the shopping cart
            foreach (var list in ShoppingCartVM.CartList)
            {
                list.Price = StaticDetails.GetPrice(list.Price);
                //Calculate price
                ShoppingCartVM.OrderOverview.OrderTotal += (list.Price * list.Count);
                //Show the description
                list.Product.Description = StaticDetails.ConvertToRawHtml(list.Product.Description);
                if (list.Product.Description.Length > 100)
                {
                    list.Product.Description = list.Product.Description.Substring(0, 99) + "...";
                }
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPost()
        {
            //Get the logged in user Id
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            //Retrieve the user from the database
            var user = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email not verified!");
            }

            //Generate the Token
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            //Setting the callback URL
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            ModelState.AddModelError(string.Empty, "Verification Email Sent. Check your email");
            return RedirectToAction("Index");

        }

        public IActionResult Add(int cartId)
        {
            //Get the shoppingcart from the database
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(c => c.Id == cartId, includeProperties: "Product");
            cart.Count += 1;
            cart.Price = StaticDetails.GetPrice(cart.Product.Price);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Remove(int cartId)
        {
            //Get the shoppingcart from the database
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(c => c.Id == cartId, includeProperties: "Product");

            if (cart.Count == 1)
            {
                //Update the session before removing in order to get the count
                var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                //Remove the last item completely
                _unitOfWork.ShoppingCart.Remove(cart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(StaticDetails.ssShoppingCart, count - 1);
            }
            else
            {
                cart.Count -= 1;
                cart.Price = StaticDetails.GetPrice(cart.Product.Price);
                _unitOfWork.Save();
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int cartId)
        {
            //Get the shoppingcart from the database
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(c => c.Id == cartId, includeProperties: "Product");

            //Update the session before removing in order to get the count
            var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            //Remove the item completely
            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.Save();
            HttpContext.Session.SetInt32(StaticDetails.ssShoppingCart, count - 1);

            cart.Count -= 1;
            cart.Price = StaticDetails.GetPrice(cart.Product.Price);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        //Summary Get Action
        public IActionResult Summary()
        {
            //Get the logged in user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderOverview = new Models.OrderOverview(),
                CartList = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == claim.Value, includeProperties: "Product")
            };

            ShoppingCartVM.OrderOverview.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            //Loop through all the items in the shopping cart
            foreach (var list in ShoppingCartVM.CartList)
            {
                list.Price = StaticDetails.GetPrice(list.Price);
                //Calculate price
                ShoppingCartVM.OrderOverview.OrderTotal += (list.Price * list.Count);

            }
            //Populate OrderOverview from the ApplicationUser
            ShoppingCartVM.OrderOverview.Phone = ShoppingCartVM.OrderOverview.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderOverview.StreetAddress1 = ShoppingCartVM.OrderOverview.ApplicationUser.StreetAddress1;
            ShoppingCartVM.OrderOverview.StreetAddress2 = ShoppingCartVM.OrderOverview.ApplicationUser.StreetAddress2;
            ShoppingCartVM.OrderOverview.City = ShoppingCartVM.OrderOverview.ApplicationUser.City;
            ShoppingCartVM.OrderOverview.State = ShoppingCartVM.OrderOverview.ApplicationUser.State;
            ShoppingCartVM.OrderOverview.PostalCode = ShoppingCartVM.OrderOverview.ApplicationUser.PostalCode;

            return View(ShoppingCartVM);
        }
    }
}
