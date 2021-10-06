using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeighborhoodMarket.DataAccess.Data.Repository;
using NeighborhoodMarket.DataAccess.Data.Repository.IRepository;
using NeighborhoodMarket.Models;
using NeighborhoodMarket.Models.ViewModels;
using NeighborhoodMarket.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeighborhoodMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =StaticDetails.Role_User_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
       

        public CategoryController(IUnitOfWork _unitOfWork)
        {
            unitOfWork = _unitOfWork;

        }
        //productPage is the current page the user is in
        public async Task<IActionResult> Index(int productPage = 1)
        {
            CategoryVM categoryVM = new CategoryVM()
            {
                Categories = await unitOfWork.Category.GetAllAsync()
            };

            var count = categoryVM.Categories.Count();
            categoryVM.Categories = categoryVM.Categories.OrderBy(p => p.CategoryName)
                //Take(2) - taking the next 2 categories
                .Skip((productPage - 1) * 5).Take(5).ToList();

            categoryVM.pagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = 5,
                TotalItems = count,
                urlParam = "/Admin/Category/Index?productPage=:"
            };

            return View(categoryVM);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(int?id)
        {
            Category category = new Category();

            if(id == null)
            {
                //For create
                return View(category);
            }
            else
            {
                //For Edit
                category = await unitOfWork.Category.GetAsync(id.GetValueOrDefault());
                if(category == null)
                {
                    return NotFound();
                }
                return View(category);
            }

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Category category)
        {
            if(ModelState.IsValid)
            {
                if(category.Id == 0)
                {
                    await unitOfWork.Category.AddAsync(category);
                    unitOfWork.Save();
                }
                else
                {
                    unitOfWork.Category.Update(category);
                }
                return View(nameof(Index));
            }
           
                return View(category);
           
        }

        #region APICalls

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var allObjs = await unitOfWork.Category.GetAllAsync();
            return Json(new { data = allObjs });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var objFromDb = await unitOfWork.Category.GetAsync(id);

            if(objFromDb != null)
            {
                await unitOfWork.Category.RemoveAsync(objFromDb);
                unitOfWork.Save();

                TempData["Success"] = "Category Successfully Deleted";
                return Json(new { success = true, message = "Delete Successfull" });
             
            }
            else
            {
                TempData["Error"] = "Error Deleting Category";
                return Json(new { success = false, message = "Error Occured While Deleting" });
 
            }
        }
        #endregion
    }
}
