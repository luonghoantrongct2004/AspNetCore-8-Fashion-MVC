using App.Areas.Admin.Models;
using App_Web.Helper;
using App_Web.Models;
using App_Web.Models.ViewModel;
using App_Web.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using Product = App_Web.Models.Product;

namespace App_Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRepo<Product> _repo;
        private readonly AppDbContext _context;

        public HomeController(IRepo<Product> repo, AppDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            List<Product> productList = (await _repo.Gets()).ToList();
            await HeaderAsync();
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;

            int cartCount = 0;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                cartCount = await _context.CartDetails.Where(cd => cd.UserId == userId).CountAsync();
            }
            HttpContext.Session.SetInt32("CartCount", cartCount);
            return View(productList);
        }
        public async Task<IActionResult> HeaderAsync()
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
            int userId = Convert.ToInt32(userIdClaim);

            // Tính số lượng các mục trong giỏ hàng
            var cartCount = await _context.CartDetails.CountAsync(u => u.UserId == userId);

            // Lấy danh sách sản phẩm trong giỏ hàng
            var carts = await _context.CartDetails
                .Where(u => u.UserId == userId)
                .Join(
                    _context.Products,
                    cd => cd.ProductId,
                    p => p.ProductId,
                    (cd, p) => new { CartDetail = cd, Product = p }
                )
                .ToListAsync();
            HttpContext.Session.SetObjectAsJson("Carts", carts);
            var categories = _context.Categories.ToList();

            var categoriesJson = JsonConvert.SerializeObject(categories);
            var categoriesBytes = Encoding.UTF8.GetBytes(categoriesJson);

            HttpContext.Session.SetInt32("CartCount", cartCount);
            HttpContext.Session.Set("Categories", categoriesBytes);

            return PartialView("_HeaderPartial");
        }

        public async Task<IActionResult> Details(int id)
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
            int userId = Convert.ToInt32(userIdClaim);
            var product = _repo.Get(id);
            List<Product> ls = await _context.Products
                .Where(p=>p.ProductName == product.ProductName || p.BrandId == product.BrandId || p.Price == product.Price)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(c => c.Comments).ThenInclude(u=> u.User)
                .ToListAsync();
            var orderStatus = await _context.Orders
            .AnyAsync(o => o.OrderDetails.Any(od => od.ProductId == product.ProductId && o.Status.Equals("Đã nhận hàng") && o.UserId == userId));
            bool userHasRated = ls.Any(p => p.Comments.Any(c => c.UserId == userId && c.ProductId == id));
            List<Orderdetail> orderdetails = new List<Orderdetail>();
            await CalculateSoldQuantity(id);
            await AssignSoldQuantityToOrderDetails(orderdetails);
            var viewModel = new ProductViewModel
            {
                Product = product,
                RelatedProducts = ls,
                IsReceived = orderStatus,
                UserHasRated = userHasRated
            };
            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpPost]
        public async Task<IActionResult> AddtoCart(AddToCartViewModel model, int Quantity, string Color, string Size)
        {
            try
            {
                var existingCartItem = await _context.CartDetails.FirstOrDefaultAsync(cd => cd.UserId == model.UserId && cd.ProductId == model.ProductId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += model.Quantity;
                }
                else
                {
                    var cartDetail = new CartDetails
                    {
                        ProductId = model.ProductId,
                        UserId = model.UserId,
                        Quantity = model.Quantity,
                        Color = Color,
                        Size = Size
                    };

                    _context.CartDetails.Add(cartDetail);
                }

                await _context.SaveChangesAsync();
                TempData["StatusMethod"] = "Đã thêm vào giỏ hàng";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["StatusMethodWar"] = "Thêm vào giỏ hàng thất bại";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItemCart(int cartDetailId)
        {
                var cartItem = await _context.CartDetails.FindAsync(cartDetailId);
                if (cartItem != null)
                {
                    _context.CartDetails.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Sản phẩm đã được xóa khỏi giỏ hàng." });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
                }
        }



        public async Task<int> CalculateSoldQuantity(int productId)
        {
            // Tính toán số lượng đã bán dựa trên các đơn hàng đã hoàn thành
            int soldQuantity = await _context.Orderdetails
                .Where(od => od.ProductId == productId && od.Order.Status == "success")
                .SumAsync(od => od.Quantity);

            return soldQuantity;
        }
        [HttpPost]
        public async Task<IActionResult> AddComment(int productId, int rating, string? commentContent)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/login");
            }
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                var newComment = new Comment
                {
                    ProductId = productId,
                    UserId = userId,
                    StarRating = rating,
                    Content = commentContent,
                    PostedAt = DateTime.Now
                };
                _context.Comments.Add(newComment);
                await _context.SaveChangesAsync();
                TempData["StatusMethod"] = "Cảm ơn bạn đã đánh giá";
                return RedirectToAction("Details", new { id = productId });
            }
            TempData["StatusMethodWar"] = "Không thể đánh giá cho sản phẩm này";
            return RedirectToAction("Details", new { id = productId });
        }
        public async Task<IActionResult> GetProductsByCategory(int categoryId, string? searchKeyword,
       string? sortOption, int pageNumber = 1, int pageSize = 12)
        {
            pageNumber = Math.Max(pageNumber, 1);
            IQueryable<Product> productsQuery; 
            if (!string.IsNullOrEmpty(sortOption) && sortOption.Equals("asc"))
            {
                productsQuery = _context.Products
                                       .Where(p => (EF.Functions.Like(p.ProductName, $"%{searchKeyword}%")
                                                 || EF.Functions.Like(p.Description, $"%{searchKeyword}%")))
                                       .OrderBy(p => p.Price);
            }
            else if (!string.IsNullOrEmpty(sortOption) && sortOption.Equals("desc"))
            {
                productsQuery = _context.Products
                                        .Where(p => (EF.Functions.Like(p.ProductName, $"%{searchKeyword}%")
                                                 || EF.Functions.Like(p.Description, $"%{searchKeyword}%")))
                                        .OrderByDescending(p => p.Price);
            }
            else if (categoryId > 0)
            {
                productsQuery = _context.Products
                                           .Where(p => p.CategoryId == categoryId
                                                 && (EF.Functions.Like(p.ProductName, $"%{searchKeyword}%")
                                                 || EF.Functions.Like(p.Description, $"%{searchKeyword}%")))
                                           .OrderByDescending(p => p.CreatedAt);
            }
            else
            {
                productsQuery = _context.Products
                                           .Where(p =>(EF.Functions.Like(p.ProductName, $"%{searchKeyword}%")
                                                 || EF.Functions.Like(p.Description, $"%{searchKeyword}%")))
                                           .OrderByDescending(p => p.CreatedAt);
            }

            var totalProducts = await productsQuery.CountAsync();
            var products = await productsQuery.Skip((pageNumber - 1) * pageSize)
                                             .Take(pageSize)
                                             .ToListAsync();

            var viewModel = new PagingModel<Product>
            {
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize),
                PageSize = pageSize,
                Items = products
            };
            ViewBag.CategoryId = categoryId;
            return PartialView("_ProductListPartial", viewModel);
        }


        public async Task<IActionResult> SortProducts(int categoryId, string searchKeyword, string sortOption,
            int pageNumber = 1, int pageSize = 12)
        {
            pageNumber = Math.Max(pageNumber, 1);
            var productsQuery = _context.Products
                                        .Where(p => p.CategoryId == categoryId
                                                && (string.IsNullOrEmpty(searchKeyword)
                                                    || p.ProductName.Contains(searchKeyword)));

            if (sortOption == "asc")
            {
                productsQuery = productsQuery.OrderBy(p => p.Price);
            }
            else if (sortOption == "desc")
            {
                productsQuery = productsQuery.OrderByDescending(p => p.Price);
            }

            var totalProducts = await productsQuery.CountAsync();
            var products = await productsQuery.Skip((pageNumber - 1) * pageSize)
                                             .Take(pageSize)
                                             .ToListAsync();

            var viewModel = new PagingModel<Product>
            {
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize),
                PageSize = pageSize,
                Items = products
            };

            return PartialView("_ProductListPartial", viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmPurchar(int id)
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest("Invalid user ID");
            }
            var order = await _context.Orders
               .Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.OrderId == id);
            if (order != null && order.Status.Equals("Đã xác nhận"))
            {
                var orderDetail = order.OrderDetails.FirstOrDefault();
                if (orderDetail != null)
                {
                    var productId = orderDetail.ProductId;
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
                    if (product != null)
                    {
                        product.SoldQuantity += orderDetail.Quantity;
                        order.Status = "Đã nhận hàng";
                        _context.Products.Update(product);
                        _context.Orders.Update(order);
                        await _context.SaveChangesAsync();
                        TempData["StatusMethod"] = "Đã nhận hàng thành công";
                        return RedirectToAction("Profile", "Account", new { id = userId });
                    }
                }
            }

            return RedirectToAction("Profile", "Account", new { id = userId });
        }

        public IActionResult Contact()
        {
            return View();
        }

        public async Task<IActionResult> DetailOrders(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (user == null)
            {
                return Redirect("/login/");
            }
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(oi => oi.Product).ThenInclude(c => c.Category)
                .Where(o => o.UserId == id)
                .ToListAsync();

            // Tạo ViewModel cho UserProfile
            var userProfileViewModel = new UserProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Orders = orders
            };

            return View(userProfileViewModel);
        }
        public async Task<IActionResult> OrderDetails(int id, int orderId)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (user == null)
            {
                return Redirect("/login/");
            }
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(oi => oi.Product).ThenInclude(c => c.Category)
                .Where(o => o.UserId == id && o.OrderId == orderId)
                .ToListAsync();

            // Tạo ViewModel cho UserProfile
            var userProfileViewModel = new UserProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Orders = orders
            };

            return View(userProfileViewModel);
        }
        public async Task<IActionResult> Introduction()
        {
            var ls = await _context.Introductions.ToListAsync();
            return View(ls);
        }
        public async Task<IActionResult> News()
        {
            var ls = await _context.News.ToListAsync();
            return View(ls);
        }

        public async Task AssignSoldQuantityToOrderDetails(List<Orderdetail> orderDetails)
        {
            foreach (var orderDetail in orderDetails)
            {
                orderDetail.Product.SoldQuantity = await CalculateSoldQuantity(orderDetail.ProductId);
            }
        }
    }
}
