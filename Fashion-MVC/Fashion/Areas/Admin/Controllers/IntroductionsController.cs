using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App_Web.Models;
using Microsoft.AspNetCore.Hosting;

namespace App_Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class IntroductionsController : Controller
    {
        private readonly AppDbContext _context;
        private IWebHostEnvironment _webHostEnvironment;

        public IntroductionsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Introductions
        public async Task<IActionResult> Index()
        {
            return View(await _context.Introduction.ToListAsync());
        }

        // GET: Admin/Introductions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var introduction = await _context.Introduction
                .FirstOrDefaultAsync(m => m.Id == id);
            if (introduction == null)
            {
                return NotFound();
            }

            return View(introduction);
        }

        // GET: Admin/Introductions/Create
        public async Task<IActionResult> Create()
        {
                return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,ImageUrl,CreatedAt")] Introduction introduction, IFormFileCollection files)
        {
            if (ModelState.IsValid)
            {
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "introducts", introduction.Id.ToString());
                var imageUrls = await App.Helper.Utilities.UploadFiles(files, "products\\" + introduction.Id.ToString(), _webHostEnvironment);

                if (imageUrls != null && imageUrls.Any())
                {
                    introduction.ImageUrl = imageUrls;
                }
                introduction.CreatedAt = DateTime.Now;
                _context.Add(introduction);
                await _context.SaveChangesAsync();
                return Redirect("/Admin/Introductions");
            }
            return View(introduction);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var introduction = await _context.Introduction.FindAsync(id);
            if (introduction == null)
            {
                return NotFound();
            }
            return View(introduction);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,ImageUrl,CreatedAt")] Introduction introduction, IFormFileCollection files)
        {
            if (id != introduction.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var introductionOld = await _context.Introductions.FindAsync(id);
                if (introductionOld == null)
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
                        oldImageUrls = introductionOld.ImageUrl;
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
                        string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "introductions", introduction.Id.ToString());
                        var newImageUrls = await App.Helper.Utilities.UploadFiles(files, "introductions\\" + introduction.Id.ToString(), _webHostEnvironment);

                        if (newImageUrls != null && newImageUrls.Any())
                        {
                            introductionOld.ImageUrl = newImageUrls;
                        }
                    }

                    introductionOld.Title = introduction.Title;
                    introductionOld.Description = introduction.Description;
                    introductionOld.CreatedAt = introduction.CreatedAt;
                    _context.Update(introductionOld);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IntroductionExists(introduction.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return Redirect("/Admin/Introductions");
            }
            return View(introduction);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var introduction = await _context.Introduction.FindAsync(id);
            if (introduction != null)
            {
                _context.Introduction.Remove(introduction);
            }

            await _context.SaveChangesAsync();
            return Redirect("/Admin/Introductions");
        }

        private bool IntroductionExists(int id)
        {
            return _context.Introduction.Any(e => e.Id == id);
        }
    }
}
