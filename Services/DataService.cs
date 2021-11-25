using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyBlog.Data;
using MyBlog.Enums;
using MyBlog.Models;

namespace MyBlog.Services
{
    public class DataService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<BlogUser> _userManager;

        public DataService(ApplicationDbContext dbContext, RoleManager<IdentityRole> roleManager, UserManager<BlogUser> userManager)
        {
            _dbContext = dbContext;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task ManageDataAsync()
        {
            // create database from the migrations
            await _dbContext.Database.MigrateAsync();

            // Seeding a few roles into the system
            await SeedRolesAsync();
            
            // Seed users into system
            await SeedUsersAsync();
        }

        private async Task SeedRolesAsync()
        {
            // If there are already roles in the system do nothing
            if (_dbContext.Roles.Any()) return;

            // Create a few roles
            foreach (var role in Enum.GetNames(typeof(BlogRole)))
            {
                // use the role manager to create roles
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private async Task SeedUsersAsync()
        {
            // if there are any users do nothing
            if (_dbContext.Users.Any()) return;

            // create new instance of blog user
            var adminUser = new BlogUser()
            {
                Email = "john.winko@gmail.com",
                UserName = "john.winko@gmail.com",
                DisplayName = "John",
                FirstName = "John",
                LastName = "Winko",
                PhoneNumber = "(904) 703-4856",
                EmailConfirmed = true
            };

            // use UserManager to create new user defined by adminUser
            await _userManager.CreateAsync(adminUser, "Abc&123!");

            // add new user to administrator role
            await _userManager.AddToRoleAsync(adminUser, BlogRole.Administrator.ToString());

            // Add a moderator user
            var modUser = new BlogUser()
            {
                Email = "eternal81@msn.com",
                UserName = "eternal81@msn.com",
                DisplayName = "John",
                FirstName = "John",
                LastName = "Winko",
                PhoneNumber = "(904) 703-4856",
                EmailConfirmed = true
            };

            // use UserManager to create new user defined by modUser
            await _userManager.CreateAsync(modUser, "Abc&123!");

            // add new user to administrator role
            await _userManager.AddToRoleAsync(modUser, BlogRole.Moderator.ToString());
        }
    }
}
