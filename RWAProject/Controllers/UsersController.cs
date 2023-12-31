﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NuGet.Protocol.Plugins;
using RWAProject.Middleware;
using RWAProject.Models;
using RWAProject.ViewModels;

namespace RWAProject.Controllers
{
    [TypeFilter(typeof(AuthFilter))]
    public class UsersController : Controller
    {
        private readonly RwaMoviesContext _context;

        public UsersController(RwaMoviesContext context)
        {
            _context = context;
        }

        // GET: Users
        [TypeFilter(typeof(PermissionsFilter))]
        public async Task<IActionResult> Index(string searchString)
        {
            IQueryable<User> usersQuery = _context.Users.Include(u => u.CountryOfResidence);

            if (String.IsNullOrEmpty(searchString) && HttpContext.Session.GetString("userSearchString") != null)
            {
                searchString = HttpContext.Session.GetString("userSearchString")!;
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u => u.Username.Contains(searchString) || 
                    u.FirstName.Contains(searchString) || 
                    u.LastName.Contains(searchString) || 
                    u.Email.Contains(searchString) ||
                    u.CountryOfResidence.Name.Contains(searchString));

                HttpContext.Session.SetString("userSearchString", searchString);
            }

            return View(await usersQuery.ToListAsync());
        }

        // GET: Users/Details/5
        [TypeFilter(typeof(PermissionsFilter))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.CountryOfResidence)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        //get details for logged in user
        public async Task<IActionResult> MyDetails()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.CountryOfResidence)
                .FirstOrDefaultAsync(m => m.Id == int.Parse(userId));

            if (user == null)
            {
                return NotFound();
            }

            return View("MyDetails", user);
        }

        // GET: Users/Create
        [TypeFilter(typeof(PermissionsFilter))]
        public IActionResult Create()
        {
            ViewData["CountryOfResidenceId"] = new SelectList(_context.Countries, "Id", "Name");
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [TypeFilter(typeof(PermissionsFilter))]
        public async Task<IActionResult> Create(UserVM userVM)
        {
            ModelState.Clear();

            if (_context.Users.Any(u => u.Username == userVM.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
            }

            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            string saltString = Convert.ToBase64String(salt);

            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(userVM.Password + saltString);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                string hashedPassword = Convert.ToBase64String(hashBytes);
                string securityToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

                User user = new User
                {
                    CreatedAt = DateTime.Now,
                    DeletedAt = null,
                    Username = userVM.Username,
                    FirstName = userVM.FirstName,
                    LastName = userVM.LastName,
                    Email = userVM.Email,
                    PwdHash = hashedPassword,
                    PwdSalt = saltString,
                    Phone = userVM.Phone,
                    IsConfirmed = true,
                    SecurityToken = securityToken,
                    CountryOfResidenceId = userVM.CountryOfResidenceId
                };

                if (ModelState.IsValid)
                {
                    _context.Add(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "Users");
                }
                ViewData["CountryOfResidenceId"] = new SelectList(_context.Countries, "Id", "Id", user.CountryOfResidenceId);
                return View(userVM);
            }
        }

        // GET: Users/Edit/5
        [TypeFilter(typeof(PermissionsFilter))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userVM = new UserVM
            {
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                CountryOfResidenceId = user.CountryOfResidenceId
            };

            ViewData["CountryOfResidenceId"] = new SelectList(_context.Countries, "Id", "Name", user.CountryOfResidenceId);
            return View(userVM);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [TypeFilter(typeof(PermissionsFilter))]
        public async Task<IActionResult> Edit(int id, UserVM userVM)
        {
            ModelState.Remove("Password");
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users.FindAsync(id);
                    if (user == null)
                    {
                        return NotFound();
                    }

                    user.Username = userVM.Username;
                    user.FirstName = userVM.FirstName;
                    user.LastName = userVM.LastName;
                    user.Email = userVM.Email;
                    user.Phone = userVM.Phone;
                    user.CountryOfResidenceId = userVM.CountryOfResidenceId;

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(id)) 
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
            ViewData["CountryOfResidenceId"] = new SelectList(_context.Countries, "Id", "Id", userVM.CountryOfResidenceId);
            return View(userVM);
        }

        // GET: Users/Delete/5
        [TypeFilter(typeof(PermissionsFilter))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.CountryOfResidence)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [TypeFilter(typeof(PermissionsFilter))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Users == null)
            {
                return Problem("Entity set 'RwaMoviesContext.Users'  is null.");
            }
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
          return (_context.Users?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
