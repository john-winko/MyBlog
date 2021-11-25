using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyBlog.Data;
using MyBlog.Enums;
using MyBlog.Models;
using MyBlog.Services;
using MyBlog.ViewModels;
using Npgsql.PostgresTypes;
using X.PagedList;

namespace MyBlog.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISlugService _slugService;
        private readonly IImageService _imageService;
        private readonly UserManager<BlogUser> _userManager;
        private readonly BlogSearchService _blogSearchService;

        public PostsController(
            ApplicationDbContext context,
            ISlugService slugService,
            IImageService imageService,
            UserManager<BlogUser> userManager,
            BlogSearchService blogSearchService)
        {
            _context = context;
            _slugService = slugService;
            _imageService = imageService;
            _userManager = userManager;
            _blogSearchService = blogSearchService;
        }

        public async Task<IActionResult> SearchIndex(int? page, string searchTerm)
        {
            ViewData["SearchTerm"] = searchTerm;
            var pageNumber = page ?? 1;
            var pageSize = 2;

            var posts = _blogSearchService.Search(searchTerm);

            return View(await posts.ToPagedListAsync(pageNumber, pageSize));
        }

        // GET: Posts
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Posts.Include(p => p.Blog).Include(p => p.BlogUser);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> BlogPostIndex(int? id, int? page)
        {
            if (id is null) return NotFound();
            var pageNumber = page ?? 1;
            var pageSize = 2;

            var posts = await _context.Posts
                .Where(p => p.BlogId == id && p.ReadyStatus == ReadyStatus.ProductionReady)
                .OrderByDescending(p => p.Created)
                .ToPagedListAsync(pageNumber, pageSize);

            return View(await posts.ToPagedListAsync(pageNumber, pageSize));
        }

        // GET: Posts/Details/5
        // public async Task<IActionResult> Details(int? id)
        // {
        //     if (id == null)
        //     {
        //         return NotFound();
        //     }
        //
        //     var post = await _context.Posts
        //         .Include(p => p.Blog)
        //         .Include(p => p.BlogUser)
        //         .Include(p => p.Tags)
        //         .FirstOrDefaultAsync(m => m.Id == id);
        //     if (post == null)
        //     {
        //         return NotFound();
        //     }
        //
        //     return View(post);
        // }

        public async Task<IActionResult> Details(string slug)
        {
            ViewData["Title"] = "Post Details Page";
            
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.BlogUser)
                .Include(p => p.Tags)
                .Include(p => p.Comments)
                .ThenInclude(c => c.BlogUser) 
                .Include(p=>p.Comments)
                .ThenInclude(c=>c.Moderator)
                .FirstOrDefaultAsync(m => m.Slug == slug);
            
            if (post == null)
            {
                return NotFound();
            }

            var dataVM = new PostDetailViewModel()
            {
                Post = post,
                Tags = _context.Tags
                    .Select(t => t.Text.ToLower())
                    .Distinct()
                    .ToList()
            };
            return View(dataVM);
        }

        // Was shown in video before coding this up
        // public async Task<IActionResult> Details(string slug)
        // {
        //     if (string.IsNullOrEmpty(slug))
        //     {
        //         return NotFound();
        //     }
        //
        //     var post = await _context.Posts
        //         .Include(p => p.Blog)
        //         .Include(p => p.BlogUser)
        //         .Include(p => p.Tags)
        //         .Include(p => p.Comments)
        //         .ThenInclude(c=>c.BlogUser) // Blog user of post may not be person currently logged in
        //         .FirstOrDefaultAsync(m => m.Slug == slug);
        //     if (post == null)
        //     {
        //         return NotFound();
        //     }
        //
        //     return View(post);
        // }

        // GET: Posts/Create
        // Does not show an authorize attribute in video but page authenticates before showing create in video
        // [Authorize]
        public IActionResult Create()
        {
            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Name");

            // was in video but we commented out... is it still useful as a hidden field?
            //ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Posts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // TODO: Don't we need to authorize before creating?        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BlogId,Title,Abstract,Content,ReadyStatus,Image")] Post post, List<string> tagValues)
        {
            if (ModelState.IsValid)
            {
                post.Created = DateTime.Now;

                var authorId = _userManager.GetUserId(User);
                post.BlogUserId = authorId;

                // use the _imageService to store incoming image
                post.ImageData = await _imageService.EncodeImageAsync(post.Image);
                post.ContentType = _imageService.ContentType(post.Image);

                // create the slug and determine if it is unique
                var slug = _slugService.UrlFriendly(post.Title);

                var validationError = false;

                // detect empty slug
                if (string.IsNullOrEmpty(slug))
                {
                    ModelState.AddModelError("", "The title you provided is empty");
                    validationError = true;
                }

                // detect incoming duplicate slugs
                else if (!_slugService.IsUnique(slug))
                {
                    ModelState.AddModelError("Title", "The title you provided is not unique for url slug");
                    validationError = true;
                }

                // else if (slug.Contains("test"))
                // {
                //     ModelState.AddModelError("", "Ohh are you testing again");
                //     ModelState.AddModelError("Content", "The title cannot contain the word test");
                //     validationError = true;
                // }
                if (validationError)
                {
                    ViewData["TagValues"] = string.Join(",", tagValues);
                    return View(post);
                }

                post.Slug = slug;

                _context.Add(post);
                await _context.SaveChangesAsync();

                foreach (var tagText in tagValues)
                {
                    _context.Add(new Tag()
                    {
                        PostId = post.Id,
                        BlogUserId = authorId,
                        Text = tagText
                    });
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Name", post.BlogId);
            return View(post);
        }

        // GET: Posts/Edit/5
        // TODO: change to slug vs id
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                return NotFound();
            }
            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Name", post.BlogId);
            ViewData["TagValues"] = string.Join(",", post.Tags.Select(t => t.Text));
            return View(post);
        }

        // POST: Posts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BlogId,Title,Abstract,Content,ReadyStatus")] Post post, IFormFile newImage, List<string> tagValues)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // pull prior data before so we don't overwrite with a null (the originalPost)
                    var newPost = await _context.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == post.Id);

                    newPost.Updated = DateTime.Now;
                    newPost.Title = post.Title;
                    newPost.Abstract = post.Abstract;
                    newPost.Content = post.Content;
                    newPost.ReadyStatus = post.ReadyStatus;

                    var newSlug = _slugService.UrlFriendly(post.Title);
                    if (newSlug != newPost.Slug)
                    {
                        if (_slugService.IsUnique(newSlug))
                        {
                            newPost.Title = post.Title;
                            newPost.Slug = newSlug;
                        }
                        else
                        {
                            ModelState.AddModelError("Title", "This title is not unique and creates a duplicate slug");
                            ViewData["TagValues"] = string.Join(",", tagValues);
                            return View(post);
                        }
                    }

                    if (newImage is not null)
                    {
                        newPost.ImageData = await _imageService.EncodeImageAsync(newImage);
                        newPost.ContentType = _imageService.ContentType(newImage);
                    }

                    // remove all tags previously associate with this Post
                    // newpost started by grabbing data from old post
                    _context.Tags.RemoveRange(newPost.Tags);

                    // add new tags
                    foreach (var tagText in tagValues)
                    {
                        _context.Add(new Tag()
                        {
                            PostId = post.Id,
                            BlogUserId = newPost.BlogUserId,
                            Text = tagText
                        });
                    }

                    //_context.Update(post);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.Id))
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
            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Description", post.BlogId);
            return View(post);
        }

        // GET: Posts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Blog)
                .Include(p => p.BlogUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }
    }
}
