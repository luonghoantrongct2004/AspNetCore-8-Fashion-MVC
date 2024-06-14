using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App_Web.Models;
using Microsoft.AspNetCore.Hosting;

namespace App_Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class NewsController : Controller
    {
        private readonly AppDbContext _context;
        private IWebHostEnvironment _webHostEnvironment;

        public NewsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }


        // GET: Admin/News
        public async Task<IActionResult> Index()
        {
            return View(await _context.News.ToListAsync());
        }

        // GET: Admin/News/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News
                .FirstOrDefaultAsync(m => m.Id == id);
            if (news == null)
            {
                return NotFound();
            }

            return View(news);
        }

        // GET: Admin/News/Create
        public async Task<IActionResult> Create()
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
            int userId = Convert.ToInt32(userIdClaim);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user != null)
            {
                ViewBag.Fullname = user.FullName;
            }
            return View();
        }

        // POST: Admin/News/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        
        public async Task<IActionResult> Create([Bind("Id,Title,Content,Author,PublishedAt")] News news, IFormFileCollection files)
        {
            if (ModelState.IsValid)
            {
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
                int userId = Convert.ToInt32(userIdClaim);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);

                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "news", news.Id.ToString());
                var imageUrls = await App.Helper.Utilities.UploadFiles(files, "news\\" + news.Id.ToString().ToString(), _webHostEnvironment);

                if (imageUrls != null && imageUrls.Any())
                {
                    news.Image = imageUrls;
                }
                news.PublishedAt = DateTime.Now;
                news.Author = user.FullName;
                _context.Add(news);
                await _context.SaveChangesAsync();
                return Redirect("/Admin/News");
            }
            return View(news);
        }

        // GET: Admin/News/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserID")?.Value;
            int userId = Convert.ToInt32(userIdClaim);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user != null)
            {
                ViewBag.Fullname = user.FullName;
            }
            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return NotFound();
            }
            return View(news);
        }

        // POST: Admin/News/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,Author,PublishedAt")] News news, IFormFileCollection files)
        {
            if (id != news.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var newsOld = await _context.News.FindAsync(id);
                    if (newsOld == null)
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
                            oldImageUrls = newsOld.Image;
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
                            string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "news", news.Id.ToString());
                            var newImageUrls = await App.Helper.Utilities.UploadFiles(files, "news\\" + news.Id.ToString(), _webHostEnvironment);

                            if (newImageUrls != null && newImageUrls.Any())
                            {
                                newsOld.Image = newImageUrls;
                            }
                        }

                        newsOld.Title = news.Title;
                        newsOld.Content = news.Content;
                        newsOld.PublishedAt = DateTime.Now;

                        _context.Update(newsOld);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!NewsExists(news.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return Redirect("/Admin/News");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NewsExists(news.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(news);
        }
        // POST: Admin/News/Delete/5
        [HttpPost, ActionName("Delete")]
        
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news != null)
            {
                _context.News.Remove(news);
            }

            await _context.SaveChangesAsync();
            return Redirect("/Admin/News");
        }

        private bool NewsExists(int id)
        {
            return _context.News.Any(e => e.Id == id);
        }
    }
}
