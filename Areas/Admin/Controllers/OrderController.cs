using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeighborhoodMarket.DataAccess.Data.Repository.IRepository;
using NeighborhoodMarket.Models;
using NeighborhoodMarket.Models.ViewModels;
using NeighborhoodMarket.Utilities;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NeighborhoodMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public OrderDetailsVM OrderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            OrderVM = new OrderDetailsVM()
            {
                OrderOverview = _unitOfWork.OrderOverView.GetFirstOrDefault(u => u.Id == id, includeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetails.GetAll(o => o.OrderId == id, includeProperties: "Product")
        };
            return View(OrderVM);
        }

        [Authorize(Roles =StaticDetails.Role_User_Admin + "," + StaticDetails.Role_User_Emp)]
        public IActionResult StartProcessing(int id)
        {
            //Retrieve OrderOverview from the database
            OrderOverview orderOverview = _unitOfWork.OrderOverView.GetFirstOrDefault(u => u.Id == id);
            orderOverview.OrderStatus = StaticDetails.StatusInProcess;
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = StaticDetails.Role_User_Admin + "," + StaticDetails.Role_User_Emp)]
        public IActionResult ShipOrder()
        {
            //Retrieve OrderOverview from the database
            OrderOverview orderOverview = _unitOfWork.OrderOverView.GetFirstOrDefault(u => u.Id == OrderVM.OrderOverview.Id);
            orderOverview.TrackingNo = OrderVM.OrderOverview.TrackingNo;
            orderOverview.Carrier = OrderVM.OrderOverview.Carrier;
            orderOverview.OrderStatus = StaticDetails.StatusShipped;
            orderOverview.ShippingDate = DateTime.Now;

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = StaticDetails.Role_User_Admin + "," + StaticDetails.Role_User_Emp)]
        public IActionResult CancelOrder(int id)
        {
            //Retrieve OrderOverview from the database
            OrderOverview orderOverview = _unitOfWork.OrderOverView.GetFirstOrDefault(u => u.Id == id);
            
            if(orderOverview.PaymentStatus == StaticDetails.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Amount = Convert.ToInt32(orderOverview.OrderTotal * 100),
                    Reason = RefundReasons.RequestedByCustomer,
                    Charge = orderOverview.TransactionId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                orderOverview.OrderStatus = StaticDetails.StatusRefunded;
                orderOverview.PaymentStatus = StaticDetails.StatusRefunded;
            }
            else
            {
                orderOverview.OrderStatus = StaticDetails.StatusCancelled;
                orderOverview.PaymentStatus = StaticDetails.StatusCancelled;
            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetOrderList(string status)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            IEnumerable<OrderOverview> orderOverviewList;

            if(User.IsInRole(StaticDetails.Role_User_Admin) || User.IsInRole(StaticDetails.Role_User_Emp))
            {
                orderOverviewList = _unitOfWork.OrderOverView.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                orderOverviewList = _unitOfWork.OrderOverView.GetAll(u => u.ApplicationUserId == claim.Value,
                                                                    includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "inprocess":
                    orderOverviewList = orderOverviewList.Where(o => o.OrderStatus == StaticDetails.StatusApproved ||
                                                                      o.OrderStatus == StaticDetails.StatusInProcess || 
                                                                      o.OrderStatus == StaticDetails.StatusPending);
                    break;
                case "pending":
                    orderOverviewList = orderOverviewList.Where(o => o.PaymentStatus == StaticDetails.PaymentStatusDelayedPayment);
                    break;
                case "completed":
                    orderOverviewList = orderOverviewList.Where(o => o.OrderStatus == StaticDetails.StatusShipped);
                    break;
                case "rejected":
                    orderOverviewList = orderOverviewList.Where(o => o.OrderStatus == StaticDetails.StatusCancelled ||
                                                                    o.OrderStatus == StaticDetails.StatusRefunded ||
                                                                    o.PaymentStatus == StaticDetails.PaymentStatusRejected);
                    break;
                default:
                    break;
            }


            return Json(new { data = orderOverviewList });
        }
        #endregion
    }


}
