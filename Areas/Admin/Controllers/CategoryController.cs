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
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        public CategoryController(IUnitOfWork _unitOfWork)
        {
            unitOfWork = _unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int?id)
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
                category = unitOfWork.Category.Get(id.GetValueOrDefault());
                if(category == null)
                {
                    return NotFound();
                }
                return View(category);
            }

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Category category)
        {
            if(ModelState.IsValid)
            {
                if(category.Id == 0)
                {
                    unitOfWork.Category.Add(category);
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
        public IActionResult GetAll()
        {
            var allObjs = unitOfWork.Category.GetAll();
            return Json(new { data = allObjs });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var objFromDb = unitOfWork.Category.Get(id);

            if(objFromDb != null)
            {
                unitOfWork.Category.Remove(objFromDb);
                unitOfWork.Save();
                return Json(new { success = true, message = "Delete Successfull" });
             
            }
            else
            {
                return Json(new { success = false, message = "Error Occured While Deleting" });
 
            }
        }
        #endregion
    }
}
