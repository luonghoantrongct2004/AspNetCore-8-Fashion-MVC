using App_Web.Models;
using App_Web.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Stripe;
using Stripe.Checkout;
using System.Text;

namespace App_Web.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;

        public CheckoutController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(bool? paymentOneProduct, bool? paymentManyProduct, int? paymentId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user != null)
                {
                    ViewBag.Email = user.Email;
                    ViewBag.Fullname = user.FullName;
                }
                if (paymentOneProduct.HasValue && paymentOneProduct == true)
                {
                    var jsonViewModel = TempData["CheckoutViewModel"] as string;
                    if (jsonViewModel != null)
                    {
                        var viewModel = JsonConvert.DeserializeObject<CheckoutViewModel>(jsonViewModel);
                        viewModel.PaymentType = paymentId;
                        viewModel.Products = _context.Products.Where(p => p.ProductId == viewModel.ProductId).ToList();

                        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == viewModel.ProductId);
                        if (product != null)
                        {
                            if (viewModel.Quantity > product.StockQuantity)
                            {
                                viewModel.Quantity = product.StockQuantity;
                                TempData["StatusMethodWar"] = $"Số lượng hàng \"{product.ProductName}\" vượt quá số lượng tồn kho.";
                                return Redirect("/Cart/Index");
                            }
                        }
                        else
                        {
                            TempData["StatusMethodWar"] = "Không tìm thấy thông tin sản phẩm";
                            return Redirect("/Home/Index");
                        }

                        return View(viewModel);
                    }
                    else
                    {
                        TempData["StatusMethodWar"] = "Lỗi không tìm thấy sản phẩm";
                        return Redirect("/Home/Index");
                    }
                }

                else if (paymentManyProduct.HasValue && paymentManyProduct == true)
                {
                    var jsonViewModel = TempData["CheckoutViewModelMany"] as string;
                    if (jsonViewModel != null)
                    {
                        var viewModelMany = JsonConvert.DeserializeObject<CheckoutViewModel>(jsonViewModel);
                        viewModelMany.PaymentType = paymentId;
                        var productInfos = new List<Models.ViewModel.ProductInfo>();

                        foreach (var productInfo in viewModelMany.ProductInfos)
                        {
                            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productInfo.ProductId);
                            if (product == null && productInfo.Quantity > product.StockQuantity)
                            {
                                TempData["StatusMethodWar"] = $"Số lượng hàng \"{product.ProductName}\" vượt quá số lượng tồn kho.";
                                return Redirect("/Cart/Index");
                            }
                            else
                            {
                                var productInfoWithDetails = new Models.ViewModel.ProductInfo
                                {
                                    ProductId = product.ProductId,
                                    ProductName = product.ProductName,
                                    Price = product.Price,
                                    Quantity = productInfo.Quantity,
                                    Color = productInfo.Color,
                                    Size = productInfo.Size,
                                };

                                productInfos.Add(productInfoWithDetails);
                            }
                        }
                        viewModelMany.ProductInfos = productInfos;

                        return View(viewModelMany);
                    }
                    else
                    {
                        TempData["StatusMethodWar"] = "Lỗi không tìm thấy sản phẩm";
                        return Redirect("/Home/Index");
                    }
                }
                else
                {
                    var cartItems = await _context.CartDetails
                                .Where(c => c.UserId == userId)
                                .ToListAsync();

                    foreach (var cartItem in cartItems)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == cartItem.ProductId);
                        cartItem.Product = product;
                    }
                    var totalPrice = cartItems.Sum(item => item.Product.Price * item.Quantity * (1 - (item.Product.Discount.HasValue ? item.Product.Discount.Value / 100m : 0)));

                    decimal couponDiscountAmount = 0;
                    var firstCartItem = cartItems.FirstOrDefault();
                    int? couponId = firstCartItem != null ? firstCartItem.CouponId : null;

                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponId == couponId);
                    if (coupon != null)
                    {
                        couponDiscountAmount = (decimal)coupon.DiscountAmount;
                    }
                    totalPrice -= couponDiscountAmount;
                    var viewModel = new CheckoutViewModel
                    {
                        PaymentType = paymentId,
                        CartItems = cartItems,
                        TotalPrice = totalPrice
                    };

                    foreach (var cartItem in cartItems)
                    {
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == cartItem.ProductId);
                        if (cartItem.Quantity > product.StockQuantity)
                        {
                            TempData["StatusMethodWar"] = $"Số lượng hàng \"{product.ProductName}\" vượt quá số lượng tồn kho.";
                            return Redirect("/Cart/Index");
                        }
                        cartItem.Product = product;
                    }
                    return View(viewModel);
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int productId, int quantity, int userId, string Color, string Size)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                TempData["StatusMethodWar"] = "Lỗi không tìm thấy sản phẩm";
                return Redirect("/Home/Index");
            }
            var totalPrice = product.Price * quantity * (1 - (product.Discount.HasValue ? product.Discount.Value / 100m : 0));
            var viewModel = new CheckoutViewModel
            {
                ProductId = productId,
                Quantity = quantity,
                UserId = userId,
                Color = Color,
                Size = Size,
                TotalPrice = totalPrice
            };
            if (viewModel.Quantity > product.StockQuantity)
            {
                TempData["StatusMethodWar"] = $"Số lượng hàng \"{product.ProductName}\" vượt quá số lượng tồn kho.";
                return Redirect($"/Home/Details?id={product.ProductId}");
            }
            var jsonViewModel = JsonConvert.SerializeObject(viewModel);
            TempData["CheckoutViewModel"] = jsonViewModel;
            return RedirectToAction("Index", "Checkout", new { paymentOneProduct = true });
        }
        [HttpPost]
        public async Task<IActionResult> Buy([FromBody] BuyRequestModel model)
        {
            foreach (var productInfo in model.Products)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productInfo.ProductId);
                if (productInfo.Quantity > product.StockQuantity)
                {

                    return Json(new { success = false, message = $"Số lượng hàng \"{product.ProductName}\" vượt quá số lượng tồn kho." });
                }
                if (product == null)
                {
                    return Json(new { success = false, message = "Lỗi không tìm thấy sản phẩm" });
                }
            }

            var productInfos = model.Products.Select(p => new Models.ViewModel.ProductInfo
            {
                ProductId = p.ProductId,
                Quantity = p.Quantity
            }).ToList();
            decimal couponDiscountAmount = 0;
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponId == model.CouponId);
            if (coupon != null)
            {
                couponDiscountAmount = (decimal)coupon.DiscountAmount;
            }
            var viewModel = new CheckoutViewModel
            {
                TotalPrice = model.TotalPrice,
                UserId = model.UserId,
                ProductInfos = productInfos,
                PaymentType = model.PaymentId,
                Color = model.Color,
                Size = model.Size,
            };

            var jsonViewModel = JsonConvert.SerializeObject(viewModel);
            TempData["CheckoutViewModelMany"] = jsonViewModel;
            return Json(new { success = true, redirectUrl = Url.Action("Index", "Checkout", new { paymentManyProduct = true }) });
        }
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (model.PaymentType == 0 || model.PaymentType == 1)
            {
                if (model.CartItems != null)
                {
                    var order = new Models.Order
                    {
                        UserId = model.UserId,
                        TotalPrice = model.TotalPrice,
                        OrderDate = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        Status = "Đang xử lý",
                        OrderDetails = new List<Orderdetail>(),
                        ContactPhone = model.ContactPhone,
                        Note = model.Note,
                        PaymentDate = DateTime.Now,
                        ShippingAddress = model.ShippingAddress,
                        PaymentMethod = "Thanh toán khi nhận hàng"
                    };

                    _context.Orders.Add(order);
                    // Gán thông tin sản phẩm từ giỏ hàng vào đơn hàng
                    foreach (var cartItem in model.CartItems)
                    {
                        var orderDetail = new Orderdetail
                        {
                            OrderId = order.OrderId,
                            ProductId = cartItem.ProductId,
                            Quantity = cartItem.Quantity,
                            CreatedAt = DateTime.Now,
                            Color = cartItem.Color,
                            Size = cartItem.Size,
                        };

                        var product = await _context.Products.FirstOrDefaultAsync(o => o.ProductId == cartItem.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity -= cartItem.Quantity;
                        }
                        order.OrderDetails.Add(orderDetail);
                    }
                    _context.SaveChanges();


                    TempData["StatusMethod"] = "Đặt hàng thành công";
                    return RedirectToAction("Index", "Home");
                }
                else if (model.ProductId > 0)
                {
                    var order = new Models.Order
                    {
                        UserId = model.UserId,
                        TotalPrice = model.TotalPrice,
                        OrderDate = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        Status = "Đang xử lý",
                        OrderDetails = new List<Orderdetail>(),
                        ContactPhone = model.ContactPhone,
                        Note = model.Note,
                        PaymentDate = DateTime.Now,
                        ShippingAddress = model.ShippingAddress,
                        PaymentMethod = "Thanh toán khi nhận hàng"
                    };


                    _context.Orders.Add(order);
                    var orderDetail = new Orderdetail
                    {
                        OrderId = order.OrderId,
                        ProductId = model.ProductId,
                        Quantity = model.Quantity,
                        CreatedAt = DateTime.Now,
                        Color = model.Color,
                        Size = model.Size,
                    };
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == model.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= model.Quantity;
                    }
                    order.OrderDetails.Add(orderDetail);

                    await _context.SaveChangesAsync();

                    TempData["StatusMethod"] = "Đặt hàng thành công";
                    return RedirectToAction("Index", "Home");
                }
            }
            else if (model.ProductInfos != null)
            {
                var order = new Models.Order
                {
                    UserId = model.UserId,
                    TotalPrice = model.TotalPrice,
                    OrderDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    Status = "Đang xử lý",
                    OrderDetails = new List<Orderdetail>(),
                    ContactPhone = model.ContactPhone,
                    Note = model.Note,
                    PaymentDate = DateTime.Now,
                    ShippingAddress = model.ShippingAddress,
                    PaymentMethod = "Thanh toán khi nhận hàng"
                };


                _context.Orders.Add(order);
                if (model.ProductInfos != null)
                {
                    foreach (var products in model.ProductInfos)
                    {
                        // Gán thông tin sản phẩm từ giỏ hàng vào đơn hàng
                        var orderDetail = new Orderdetail
                        {
                            OrderId = order.OrderId,
                            ProductId = products.ProductId,
                            Quantity = products.Quantity,
                            CreatedAt = DateTime.Now,
                            Color = products.Color,
                            Size = products.Size,
                        };
                        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == products.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity -= products.Quantity;
                        }
                        order.OrderDetails.Add(orderDetail);
                    }
                }
                else
                {
                    var orderDetail = new Orderdetail
                    {
                        OrderId = order.OrderId,
                        ProductId = model.ProductId,
                        Quantity = model.Quantity,
                        CreatedAt = DateTime.Now,
                        Color = model.Color,
                        Size = model.Size,
                    };
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == model.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= model.Quantity;
                    }
                    order.OrderDetails.Add(orderDetail);
                }
                await _context.SaveChangesAsync();

                TempData["StatusMethod"] = "Đặt hàng thành công";
                return RedirectToAction("Index", "Home");
            }

            var currentHost = HttpContext.Request.Host;
            var domain = $"{(HttpContext.Request.IsHttps ? "https" : "http")}://{currentHost}";
            var totalItem = 1;
            if (model.CartItems != null && model.CartItems.Count > 0)
            {
                totalItem = model.CartItems.Count;
                HttpContext.Session.SetString("CartItems", JsonConvert.SerializeObject(model.CartItems));
            }
            try
            {
                var lineItems = new List<SessionLineItemOptions>();

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "vnd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Bạn đang thanh toán {totalItem} sản phẩm trên Store"
                        },
                        UnitAmount = (long)model.TotalPrice,
                    },
                    Quantity = 1,
                });

                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = domain + "/Checkout/Success",
                    CancelUrl = domain + "/Home/Error",
                };

                var service = new Stripe.Checkout.SessionService();
                var session = await service.CreateAsync(options);

                HttpContext.Session.SetInt32("UserId", model.UserId);
                HttpContext.Session.SetString("CheckoutViewModel", JsonConvert.SerializeObject(model));
                HttpContext.Session.SetInt32("TotalPrice", (int)model.TotalPrice);

                TempData["stripeSessionId"] = session.Id;

                return Redirect(session.Url);
            }
            catch (StripeException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<IActionResult> Success()
        {
            if (TempData.ContainsKey("stripeSessionId"))
            {
                var sessionId = TempData["stripeSessionId"].ToString();

                var sessionService = new Stripe.Checkout.SessionService();
                var session = await sessionService.GetAsync(sessionId);

                if (session.PaymentStatus == "paid")
                {
                    if (HttpContext.Session.TryGetValue("CartItems", out byte[] cartItemsData) && cartItemsData != null)
                    {
                        if (HttpContext.Session.TryGetValue("CheckoutViewModel", out byte[] modelData) &&
                        HttpContext.Session.GetInt32("UserId") != null)
                        {
                            var userId = HttpContext.Session.GetInt32("UserId").Value;
                            var cartItemsJson = Encoding.UTF8.GetString(cartItemsData);
                            var cartItems = JsonConvert.DeserializeObject<List<CartItemViewModel>>(cartItemsJson);
                            var totalPrice = HttpContext.Session.GetInt32("TotalPrice").Value;

                            // Deserialize CheckoutViewModel from session data
                            var modelJson = Encoding.UTF8.GetString(modelData);
                            var checkoutModel = JsonConvert.DeserializeObject<CheckoutViewModel>(modelJson);

                            var order = new Models.Order
                            {
                                UserId = userId,
                                TotalPrice = totalPrice,
                                OrderDate = DateTime.Now,
                                CreatedAt = DateTime.Now,
                                Status = "Đang xử lý",
                                OrderDetails = new List<Orderdetail>(),
                                ContactPhone = checkoutModel.ContactPhone,
                                Note = checkoutModel.Note,
                                PaymentDate = DateTime.Now,
                                ShippingAddress = checkoutModel.ShippingAddress,
                                PaymentMethod = "Thẻ visa hoặc Master card"
                            };

                            _context.Orders.Add(order);
                            // Gán thông tin sản phẩm từ giỏ hàng vào đơn hàng
                            foreach (var cartItem in cartItems)
                            {
                                var orderDetail = new Orderdetail
                                {
                                    OrderId = order.OrderId,
                                    ProductId = cartItem.ProductId,
                                    Quantity = cartItem.Quantity,
                                    CreatedAt = DateTime.Now,
                                    Color = cartItem.Color,
                                    Size = cartItem.Size,
                                };

                                var product = await _context.Products.FirstOrDefaultAsync(o => o.ProductId == cartItem.ProductId);
                                if (product != null)
                                {
                                    product.StockQuantity -= cartItem.Quantity;
                                }
                                order.OrderDetails.Add(orderDetail);
                            }
                            _context.SaveChanges();
                        }
                        else
                        {
                            TempData["StatusMethodWar"] = "Thanh toán thất bại";
                            return RedirectToAction("Index", "Home", new { success = false });
                        }
                    }
                    else if (HttpContext.Session.TryGetValue("CheckoutViewModel", out byte[] modelData) &&
                        HttpContext.Session.GetInt32("UserId") != null)
                    {
                        var userId = HttpContext.Session.GetInt32("UserId").Value;
                        var totalPrice = HttpContext.Session.GetInt32("TotalPrice").Value;

                        // Deserialize CheckoutViewModel from session data
                        var modelJson = Encoding.UTF8.GetString(modelData);
                        var checkoutModel = JsonConvert.DeserializeObject<CheckoutViewModel>(modelJson);

                        var order = new Models.Order
                        {
                            UserId = userId,
                            TotalPrice = totalPrice,
                            OrderDate = DateTime.Now,
                            CreatedAt = DateTime.Now,
                            Status = "Đang xử lý",
                            OrderDetails = new List<Orderdetail>(),
                            ContactPhone = checkoutModel.ContactPhone,
                            Note = checkoutModel.Note,
                            PaymentDate = DateTime.Now,
                            ShippingAddress = checkoutModel.ShippingAddress,
                            PaymentMethod = "Thẻ visa hoặc Master card"
                        };

                        _context.Orders.Add(order);
                        if (checkoutModel.ProductInfos != null)
                        {
                            foreach (var products in checkoutModel.ProductInfos)
                            {
                                // Gán thông tin sản phẩm từ giỏ hàng vào đơn hàng
                                var orderDetail = new Orderdetail
                                {
                                    OrderId = order.OrderId,
                                    ProductId = products.ProductId,
                                    Quantity = products.Quantity,
                                    CreatedAt = DateTime.Now,
                                    Color = products.Color,
                                    Size = products.Size,
                                };
                                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == products.ProductId);
                                if (product != null)
                                {
                                    product.StockQuantity -= products.Quantity;
                                }
                                order.OrderDetails.Add(orderDetail);
                            }
                        }
                        else
                        {
                            var orderDetail = new Orderdetail
                            {
                                OrderId = order.OrderId,
                                ProductId = checkoutModel.ProductId,
                                Quantity = checkoutModel.Quantity,
                                CreatedAt = DateTime.Now,
                                Color = checkoutModel.Color,
                                Size = checkoutModel.Size,
                            };
                            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == checkoutModel.ProductId);
                            if (product != null)
                            {
                                product.StockQuantity -= checkoutModel.Quantity;
                            }
                            order.OrderDetails.Add(orderDetail);
                        }
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        TempData["StatusMethodWar"] = "Thanh toán thất bại";
                        return RedirectToAction("Index", "Home", new { success = false });
                    }
                }
                TempData["StatusMethod"] = "Đặt hàng thành công";
                return RedirectToAction("Index", "Home");
            }
            TempData["StatusMethodWar"] = "Thanh toán thất bại";
            return RedirectToAction("Index", "Home");
        }

    }
}
