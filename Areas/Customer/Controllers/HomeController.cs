using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeighborhoodMarket.Areas.Admin.Controllers;
using NeighborhoodMarket.DataAccess.Data;
using NeighborhoodMarket.DataAccess.Data.Repository.IRepository;
using NeighborhoodMarket.Models;
using NeighborhoodMarket.Models.ViewModels;
using NeighborhoodMarket.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NeighborhoodMarket.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _db;
        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, ApplicationDbContext db)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _db = db;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> ProductList = _unitOfWork.Product.GetAll(includeProperties:"Category");
            //Retrieving the shoppingCart when the user logs in
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if(claim!=null)
            {
                var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).ToList().Count();
                //Set session
                HttpContext.Session.SetInt32(StaticDetails.ssShoppingCart, count);

            }
            return View(ProductList);
        }
        public IActionResult Details(int id)
        {
            var ProductFromDb = _unitOfWork.Product
                .GetFirstOrDefault(u => u.Id == id, includeProperties:"Category");
            ShoppingCart shoppingCart = new ShoppingCart()
            {
                Product = ProductFromDb,
                ProductId = ProductFromDb.Id
            };
            return View(shoppingCart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart ShoppingCart)
        {
            ShoppingCart.Id = 0;
            if(ModelState.IsValid)
            {
                //Add to cart
                //Find the Id of the logged in user
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                //Stores the actual user Id
                ShoppingCart.ApplicationUserId = claim.Value;

                //Retrieve ShoppingCart info based on user and product Id
                ShoppingCart shoppingCartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(
                    u => u.ApplicationUserId == ShoppingCart.ApplicationUserId && u.ProductId == ShoppingCart.ProductId
                    , includeProperties: "Product");

                if(shoppingCartFromDb == null)
                {
                    //No records in database for the product/s of that user
                    _unitOfWork.ShoppingCart.Add(ShoppingCart);
                }
                else
                {
                    shoppingCartFromDb.count += ShoppingCart.count;
                    //_unitOfWork.ShoppingCart.Update(shoppingCartFromDb);
                }
                _unitOfWork.Save();
                var count = _unitOfWork.ShoppingCart
                    .GetAll(u => u.ApplicationUserId == claim.Value).Select(t=>t.count).Sum();
                HttpContext.Session.SetInt32(StaticDetails.ssShoppingCart, count);


                return RedirectToAction(nameof(Index));
            }
            else
            {
                //Return to the view
                var ProductFromDb = _unitOfWork.Product
                    .GetFirstOrDefault(u => u.Id == ShoppingCart.ProductId, includeProperties: "Category");
                ShoppingCart shoppingCart = new ShoppingCart()
                {
                    Product = ProductFromDb,
                    ProductId = ProductFromDb.Id
                };
                return View(shoppingCart);

            }

        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
