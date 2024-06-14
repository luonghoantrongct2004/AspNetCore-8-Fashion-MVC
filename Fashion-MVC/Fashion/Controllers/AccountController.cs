using Microsoft.AspNetCore.Mvc;
using App_Web.Models;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using App_Web.Models.ViewModel;
using System.Text.Encodings.Web;

namespace App_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("/login/")]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }


        [HttpPost("/login/")]
        [AllowAnonymous]

        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var hashedPassword = HashPassword(model.Password);
                var user = _context.Users.FirstOrDefault(u => u.Email == model.UserNameOrEmail && u.Password == hashedPassword);

                List<Claim> claims;
                if (user != null)
                {
                    var userRole = await _context.Roles.FindAsync(user.RoleId);
                    if (userRole != null && userRole.Id == user.RoleId)
                    {
                        HttpContext.Session.SetInt32("UserID", user.UserID);
                        claims = new List<Claim>
                        {
                        new Claim(ClaimTypes.Name, user.Email),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("UserID", user.UserID.ToString()),
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                        new Claim(ClaimTypes.Role, "Admin")
                    };
                    }
                    else
                    {
                        HttpContext.Session.SetInt32("UserID", user.UserID);
                        claims = new List<Claim>
                        {
                        new Claim(ClaimTypes.Name, user.Email),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("UserID", user.UserID.ToString()),
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString())
                        };
                    }
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Đặt cookie lưu trữ cố định
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20) // Hết hạn sau 20 phút
                    };

                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity), authProperties);
                    TempData["StatusMethod"] = "Đăng nhập thành công";
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Email hoặc password không đúng");

            return View(model);
        }

        [HttpPost("/logout/")]

        public async Task<IActionResult> LogOff()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/register/")]
        public IActionResult Register(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost("/register/")]
        [AllowAnonymous]

        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã được đăng kí");
                    return View(model);
                }

                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("Password", "Password không giống nhau");
                    return View(model);
                }
                var hashedPassword = HashPassword(model.Password);
                var user = new User
                {
                    Password = hashedPassword,
                    FullName = model.FullName,
                    Email = model.Email,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var claims = new List<Claim>
                {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("UserID", user.UserID.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString())
                };

                var userIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(userIdentity), authProperties);
                TempData["StatusMethod"] = "Đăng kí thành công";

                return LocalRedirect(returnUrl);
            }
            ModelState.AddModelError("Email", "Email hoặc password không đúng");
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Profile(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (user == null)
            {
                return Redirect("/login/");
            }
            var order = await _context.Orders.Where(u => u.UserId == id).ToListAsync();
            var userProfileViewModel = new UserProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
            };

            return View(userProfileViewModel);
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Profile(int id, UserProfileViewModel viewModel)
        {
            if (viewModel.CurrentPassword.Trim() == null || viewModel.ConfirmPassword.Trim() == null || viewModel.NewPassword.Trim() == null)
            {
                ModelState.AddModelError("CurrentPassword", "Vui lòng nhập đầy đủ");
                return View(viewModel);
            }
            var hashedPassword = HashPassword(viewModel.CurrentPassword);
            var hashedNewPassword = HashPassword(viewModel.NewPassword);
            var existUser = _context.Users.FirstOrDefault(u => u.UserID == id);
            if (hashedPassword != existUser.Password)
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                return View(viewModel);
            }
            else if (viewModel.NewPassword == viewModel.CurrentPassword)
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới không giống mật khẩu cũ");
                return View(viewModel);
            }
            else
            {
                existUser.FullName = viewModel.FullName;
                existUser.Email = viewModel.Email;
                existUser.Password = hashedNewPassword;
                _context.Update(existUser);
                await _context.SaveChangesAsync();
                TempData["StatusMethod"] = "Thay đổi mật khẩu thành công";
                return View(viewModel);
            }
        }
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int OrderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == OrderId);
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;

            int userId;
            if (!int.TryParse(userIdClaim, out userId))
            {
                return Redirect("/login");
            }
            if (order == null)
            {
                return NotFound();
            }
            if (order.Status.Equals("Đã nhận hàng"))
            {
                TempData["StatusMethodWar"] = "Bạn đã nhận hàng rồi nha";
                return RedirectToAction("DetailOrders", "Home", new { id = userId });
            }
            foreach (var orderDetail in order.OrderDetails)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == orderDetail.ProductId);

                if (product != null)
                {
                    product.StockQuantity += orderDetail.Quantity;
                }
            }
            order.Status = "Đã hủy hàng";
            await _context.SaveChangesAsync();

            TempData["StatusMethod"] = "Bạn hủy hàng thành công";
            return RedirectToAction("Profile", "Account", new { id = userId });
        }
        [HttpGet("/Account/AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
