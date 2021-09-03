using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;
using NeighborhoodMarket.DataAccess.Data.Repository;
using NeighborhoodMarket.DataAccess.Data.Repository.IRepository;
using NeighborhoodMarket.Models;
using NeighborhoodMarket.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NeighborhoodMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        public readonly IUnitOfWork _unitOfWork;
        public readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;

        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            ProductVm productVm = new ProductVm()
            {
                Product = new Product(),
                CategoryList = _unitOfWork.Category.GetAll().Select(i=>new SelectListItem
                {
                    Text = i.CategoryName,
                    Value = i.Id.ToString()
                })
            };

            //For create
            if(id == null)
            {
                return View(productVm);
            }
            //For edit
            else
            {
                //Retrieve the product
               productVm.Product = _unitOfWork.Product.Get(id.GetValueOrDefault());
                if(productVm.Product==null)
                {
                    return NotFound();
                }
                return View(productVm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVm productVm)
        {
            if (ModelState.IsValid)
            {
                //Root path
                string webRootPath = _webHostEnvironment.WebRootPath;
                //Retrieve the uploaded files
                var files = HttpContext.Request.Form.Files;

                if(files.Count>0)
                {
                    //Giving the image an identifier
                    string fileName = Guid.NewGuid().ToString();
                    var uploadPath = Path.Combine(webRootPath, @"images\products");
                    var extension = Path.GetExtension(files[0].FileName);

                    //For edit
                    if(productVm.Product.ImageUrl != null)
                    {
                        //Remove previous image
                        var imagePath = Path.Combine(webRootPath, productVm.Product.ImageUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                        //Adding the new file
                        using(var filesStreams = new FileStream(Path.Combine(uploadPath,fileName+extension),FileMode.Create))
                        {
                            //Copy the file to filestreams
                            //Copies into our images folder
                            files[0].CopyTo(filesStreams);
                        }
                        productVm.Product.ImageUrl = @"\images\products\" + fileName + extension;
                    }
                    //when they don't change the image
                    else
                    {
                        if(productVm.Product.Id!=0)
                        {
                            Product objFromDb = _unitOfWork.Product.Get(productVm.Product.Id);
                            productVm.Product.ImageUrl = objFromDb.ImageUrl;
                        }
                    
                    }
                }
                //For Create
           
                    if(productVm.Product.Id ==0)
                    {
                        _unitOfWork.Product.Add(productVm.Product);
                        _unitOfWork.Save();
                    }
                    else
                    {
                        _unitOfWork.Product.Update(productVm.Product);
                        _unitOfWork.Save();
                    }
                
                     return RedirectToAction(nameof(Index));
             }
            else
            {
                productVm.CategoryList = _unitOfWork.Category.GetAll().Select(i => new SelectListItem
                {
                    Text = i.CategoryName,
                    Value = i.Id.ToString()
                });

                //populate product if for update
                if(productVm.Product.Id!=0)
                {
                    productVm.Product = _unitOfWork.Product.Get(productVm.Product.Id);
                }
            }
            return View(productVm);
            
        }

        #region APICalls
        [HttpGet]
        public IActionResult GetAll()
        {
            var allObjs = _unitOfWork.Product.GetAll(includeProperties:"Category");
            return Json(new { data = allObjs });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var objFromDb = _unitOfWork.Product.Get(id);

            if (objFromDb != null)
            {
                string webRootPath = _webHostEnvironment.WebRootPath;
                var imagePath = Path.Combine(webRootPath, objFromDb.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
                _unitOfWork.Product.Remove(objFromDb);
                _unitOfWork.Save();
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
