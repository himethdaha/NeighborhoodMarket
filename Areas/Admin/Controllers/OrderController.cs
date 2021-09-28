using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeighborhoodMarket.DataAccess.Data.Repository.IRepository;
using NeighborhoodMarket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeighborhoodMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetOrderList()
        {
            IEnumerable<OrderOverview> orderOverviewList;

            orderOverviewList = _unitOfWork.OrderOverView.GetAll(includeProperties: "ApplicationUser");

            return Json(new { data = orderOverviewList });
        }
        #endregion
    }


}
