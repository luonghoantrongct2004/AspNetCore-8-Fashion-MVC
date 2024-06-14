using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App_Web.Models;
using App.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;

namespace App_Web.Areas.Admin.Controllers
{
    [Authorize(Policy = "RequireOwnerAndManageRole")]
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private IWebHostEnvironment _webHostEnvironment;

        public ProductsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }


        // GET: Admin/Products
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 6)
        {
            pageNumber = Math.Max(pageNumber, 1);

            IQueryable<Product> productsQuery = _context.Products
                .AsQueryable()
                .Include(p=>p.Brand)
                .Include(p=>p.Category)
                .AsNoTracking()
                .OrderBy(p => p.CreatedAt); // Sắp xếp theo ngày tạo

            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery
                    .Where(p => EF.Functions.Like(p.ProductName, $"%{searchString}%")
                             || EF.Functions.Like(p.Description, $"%{searchString}%"));
            }

            var totalProducts = await productsQuery.CountAsync();

            var products = await productsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new PagingModel<Product>
            {
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize),
                PageSize = pageSize,
                Items = products
            };

            ViewBag.CurrentFilter = searchString;
            return View(viewModel);
        }
    // GET: Admin/Products/Details/5
    public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Admin/Products/Create
        public IActionResult Create()
        {
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "BrandName");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFileCollection files, List<string>? selectedColors, List<string>? selectedSizes)
        {
            if (ModelState.IsValid)
            {
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", product.ProductId.ToString());
                var imageUrls = await App.Helper.Utilities.UploadFiles(files, "products\\" + product.ProductId.ToString(), _webHostEnvironment);

                if (imageUrls != null && imageUrls.Any())
                {
                    product.Images = imageUrls;
                }
                product.CreatedAt = DateTime.Now;
                product.Color = selectedColors;
                product.Size = selectedSizes;
                _context.Add(product);
                await _context.SaveChangesAsync();
                return Redirect("/Admin/Products/Index");
            }
            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "BrandName");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            return View(product);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product, IFormFileCollection files, List<string>? selectedColors, List<string>? selectedSizes)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var productOld = await _context.Products.FindAsync(id);
                if (productOld == null)
                {
                    return NotFound();
                }

                try
                {
                    // Temporary variable to store old image URLs
                    List<string> oldImageUrls = new List<string>();

                    if (files.Count() > 0)
                    {
                        // Delete old images if there are any
                        oldImageUrls = productOld.Images;
                        if (oldImageUrls != null)
                        {
                            foreach (var oldImageUrl in oldImageUrls)
                            {
                                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, oldImageUrl);
                                if (System.IO.File.Exists(oldImagePath))
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                            }
                        }

                        // Upload new images
                        string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", product.ProductId.ToString());
                        var newImageUrls = await App.Helper.Utilities.UploadFiles(files, "products\\" + product.ProductId.ToString(), _webHostEnvironment);

                        if (newImageUrls != null && newImageUrls.Any())
                        {
                            productOld.Images = newImageUrls;
                        }
                    }

                    productOld.ProductName = product.ProductName;
                    productOld.Description = product.Description;
                    productOld.Price = product.Price;
                    productOld.CategoryId = product.CategoryId;
                    productOld.BrandId = product.BrandId;
                    productOld.StockQuantity = product.StockQuantity;
                    productOld.Discount = product.Discount;
                    productOld.Color = selectedColors;
                    productOld.Size = selectedSizes;
                    _context.Update(productOld);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("Products", "Admin");
            }

            return View(product);
        }


        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var orderDetails = _context.Orderdetails.Where(od => od.ProductId == id);
            _context.Orderdetails.RemoveRange(orderDetails);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction("Products", "Admin");
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
