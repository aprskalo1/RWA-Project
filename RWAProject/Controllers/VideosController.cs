﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RWAProject.Middleware;
using RWAProject.Models;
using PagedList;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text;

namespace RWAProject.Controllers
{
    [TypeFilter(typeof(AuthFilter))]
    public class VideosController : Controller
    {
        private readonly RwaMoviesContext _context;

        public VideosController(RwaMoviesContext context)
        {
            _context = context;
        }

        // GET: Videos
        public async Task<IActionResult> Index(string currentFilter, string searchString, int? page)
        {
            IQueryable<Video> videosQuery = _context.Videos.Include(v => v.Genre).Include(v => v.Image);

            if (!(HttpContext.Session.GetString("videosSearchString") == null))
            {
                currentFilter = HttpContext.Session.GetString("videosSearchString")!;
            }

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                videosQuery = videosQuery.Where(v => v.Name.Contains(searchString) || v.Genre.Name.Contains(searchString));
                HttpContext.Session.SetString("videosSearchString", searchString);
            }

            int pageSize = 6;
            int pageNumber = (page ?? 1);

            var videos = await videosQuery.ToListAsync();
            return View(videos.ToPagedList(pageNumber, pageSize));
        }


        // GET: Videos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Videos == null)
            {
                return NotFound();
            }

            var video = await _context.Videos
                .Include(v => v.Genre)
                .Include(v => v.Image)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (video == null)
            {
                return NotFound();
            }

            return View(video);
        }

        // GET: Videos/Create
        public IActionResult Create()
        {
            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name");
            ViewData["ImageId"] = new SelectList(_context.Images, "Id", "Id");
            return View();
        }

        // POST: Videos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CreatedAt,Name,Description,GenreId,TotalSeconds,StreamingUrl,ImageId")] Video video)
        {
            ModelState.Remove("Genre");
            if (ModelState.IsValid)
            {
                _context.Add(video);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", video.GenreId);
            ViewData["ImageId"] = new SelectList(_context.Images, "Id", "Id", video.ImageId);
            return View(video);
        }

        // GET: Videos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Videos == null)
            {
                return NotFound();
            }

            var video = await _context.Videos.FindAsync(id);
            if (video == null)
            {
                return NotFound();
            }
            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", video.GenreId);
            ViewData["ImageId"] = new SelectList(_context.Images, "Id", "Id", video.ImageId);
            return View(video);
        }

        // POST: Videos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CreatedAt,Name,Description,GenreId,TotalSeconds,StreamingUrl,ImageId")] Video video)
        {
            if (id != video.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Genre");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(video);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VideoExists(video.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["GenreId"] = new SelectList(_context.Genres, "Id", "Name", video.GenreId);
            ViewData["ImageId"] = new SelectList(_context.Images, "Id", "Id", video.ImageId);
            return View(video);
        }

        // GET: Videos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Videos == null)
            {
                return NotFound();
            }

            var video = await _context.Videos
                .Include(v => v.Genre)
                .Include(v => v.Image)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (video == null)
            {
                return NotFound();
            }

            return View(video);
        }

        // POST: Videos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Videos == null)
            {
                return Problem("Entity set 'RwaMoviesContext.Videos'  is null.");
            }
            var video = await _context.Videos.FindAsync(id);
            if (video != null)
            {
                _context.Videos.Remove(video);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VideoExists(int id)
        {
          return (_context.Videos?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
