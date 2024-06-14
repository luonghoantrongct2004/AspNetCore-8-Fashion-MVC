using App_Web.Models;
using App_Web.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App_Web.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? couponId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;

            int userId;
            if (!int.TryParse(userIdClaim, out userId))
            {
                return Redirect("/login");
            }
            var cartItems = await _context.CartDetails
                .Where(cd => cd.UserId == userId)
                .Join(
                    _context.Products,
                    cd => cd.ProductId,
                    p => p.ProductId,
                    (cd, p) => new { CartDetail = cd, Product = p }
                )
                .ToListAsync();

            var coupons = await _context.Coupons.ToListAsync();
            decimal couponDiscountAmount = 0;
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponId == couponId);
            if (couponId.HasValue)
            {
                foreach (var cartItem in cartItems)
                {
                    var existCoupon = cartItem.CartDetail.CouponId;
                    if (existCoupon == coupon?.CouponId)
                    {
                        var existCouponDis = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponId == existCoupon);
                        cartItem.CartDetail.CouponId = existCoupon;
                        couponDiscountAmount = (decimal)existCouponDis.DiscountAmount;
                        _context.Update(cartItem.CartDetail);
                        await _context.SaveChangesAsync();
                    }
                    var isValid = cartItem.CartDetail.ProductId;
                    if (isValid > 0)
                    {
                        if (coupon != null)
                        {
                            cartItem.CartDetail.CouponId = couponId;
                            couponDiscountAmount = (decimal)coupon.DiscountAmount;
                            _context.Update(cartItem.CartDetail);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            var totalPriceLast = 0m;
            var totalPrice = 0m;

            foreach (var cartItem in cartItems)
            {
                var productPrice = cartItem.Product.Price;
                var quantity = cartItem.CartDetail.Quantity;
                var discount = cartItem.Product.Discount.HasValue ? cartItem.Product.Discount.Value / 100m : 0m;

                // Tính tổng giá tiền cuối cùng của mỗi sản phẩm
                var productTotalPrice = productPrice * quantity * (1 - discount);

                totalPriceLast += productTotalPrice; // Thêm vào tổng giá tiền cuối cùng
                totalPrice += productTotalPrice; // Thêm vào tổng giá tiền đã giảm giá
            }

            var cartViewModel = new CartViewModel
            {
                CartItems = cartItems.Select(item => new CartItemViewModel
                {
                    CartDetailsId = item.CartDetail.CartDetailsId,
                    ProductId = item.Product.ProductId,
                    ProductName = item.Product.ProductName,
                    UnitPrice = item.Product.Price,
                    Image = item.Product.Images,
                    Quantity = item.CartDetail.Quantity,
                    Color = item.CartDetail.Color,
                    Size = item.CartDetail.Size,
                    TotalPrice = item.Product.Price * item.CartDetail.Quantity * (1 - (item.Product.Discount.HasValue ? item.Product.Discount.Value / 100m : 0))
                }).ToList(),
                TotalItems = cartItems.Sum(cd => cd.CartDetail.Quantity),
                TotalPriceLast = totalPriceLast,
                TotalPrice = totalPrice,
                Coupons = coupons,
                CouponId = couponId,
                UserId = userId,
                CouponDiscountAmount = couponDiscountAmount
            };

            return View(cartViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Redirect("/login");
            }

            var cartItem = await _context.CartDetails
                .FirstOrDefaultAsync(cd => cd.UserId == userId && cd.ProductId == productId);

            if (cartItem == null)
            {
                return NotFound();
            }

            // Cập nhật số lượng sản phẩm trong giỏ hàng
            cartItem.Quantity = quantity;
            _context.Update(cartItem);
            await _context.SaveChangesAsync();

            // Chuyển hướng lại đến trang giỏ hàng
            return RedirectToAction(nameof(Index));
        }

        private void UpdateCartItemQuantity(List<CartItemViewModel> cartItems)
        {
            foreach (var item in cartItems)
            {
                if (item.Quantity < 1)
                {
                    item.Quantity = 1;
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(int couponId, decimal totalPrice)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;

            int userId;
            if (!int.TryParse(userIdClaim, out userId))
            {
                return Unauthorized(); // Trả về mã lỗi 401 nếu không có UserID hợp lệ
            }

            var cartItems = await _context.CartDetails
                                .Where(c => c.UserId == userId)
                                .ToListAsync();

            var firstCartItem = cartItems.FirstOrDefault();

            // Tính toán tổng số tiền giảm giá từ mã giảm giá
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponId == couponId);

            decimal couponDiscountAmount = 0;

            // Nếu đã có mã giảm giá, loại bỏ giảm giá cũ trước khi áp dụng giảm giá mới
            if (firstCartItem?.CouponId.HasValue == true && firstCartItem.CouponId > 0)
            {
                var existingCoupon = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponId == firstCartItem.CouponId);
                couponDiscountAmount -= (decimal)existingCoupon.DiscountAmount;
            }

            // Áp dụng mã giảm giá mới
            if (coupon != null && totalPrice >= coupon.MinAmount)
            {
                couponDiscountAmount = (decimal)coupon.DiscountAmount;
                firstCartItem.CouponId = couponId;
            }
            else
            {
                // Nếu không thỏa điều kiện để áp dụng mã giảm giá mới, trả về lỗi
                return Json(new { success = false });
            }
            // Loại bỏ giảm giá từ mã giảm giá cũ
            foreach (var cartItem in cartItems)
            {
                if (cartItem.CouponId.HasValue && cartItem.CouponId == firstCartItem.CouponId)
                {
                    var existingCoupon = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponId == firstCartItem.CouponId);
                    cartItem.CartTotal += (decimal)existingCoupon.DiscountAmount;
                    _context.Update(cartItem);
                }
            }

            // Áp dụng giảm giá từ mã giảm giá mới
            if (coupon != null && totalPrice >= coupon.MinAmount)
            {
                foreach (var cartItem in cartItems)
                {
                    cartItem.CouponId = couponId;
                    cartItem.CartTotal -= (decimal)coupon.DiscountAmount;
                    _context.Update(cartItem);
                }
            }
            else
            {
                // Trả về lỗi nếu không thể áp dụng mã giảm giá mới
                return Json(new { success = false });
            }

            await _context.SaveChangesAsync();

            // Tính tổng giá trị cuối cùng sau khi áp dụng giảm giá
            decimal finalTotalPrice = totalPrice - couponDiscountAmount;

            return Json(new { totalPrice = finalTotalPrice, success = true });
        }
    }
}