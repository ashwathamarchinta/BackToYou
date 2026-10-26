using BackToYou.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BackToYou.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created and up to date
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roleNames = { "Admin", "Student" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Default Admin User
            var adminEmail = "admin@campus.edu";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Campus Administrator",
                    StudentRollNumber = "STAFF-ADM-01",
                    Department = "Campus Security & Administration",
                    PhoneNumber = "9876543210",
                    CreatedAt = DateTime.UtcNow
                };

                var createAdminResult = await userManager.CreateAsync(adminUser, "Admin@123");
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Default Student User
            var studentEmail = "student@campus.edu";
            var studentUser = await userManager.FindByEmailAsync(studentEmail);
            if (studentUser == null)
            {
                studentUser = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    EmailConfirmed = true,
                    FullName = "Rahul Sharma",
                    StudentRollNumber = "CS2023045",
                    Department = "Computer Science & Engineering",
                    PhoneNumber = "9876501234",
                    CreatedAt = DateTime.UtcNow
                };

                var createStudentResult = await userManager.CreateAsync(studentUser, "Student@123");
                if (createStudentResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(studentUser, "Student");
                }
            }

            // 4. Seed Categories if empty
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Electronics & Gadgets", Description = "Laptops, phones, chargers, earphones, pendrives", IconClass = "bi-laptop" },
                    new Category { Name = "ID Cards & Documents", Description = "College ID cards, driver licenses, certificates, folders", IconClass = "bi-person-badge" },
                    new Category { Name = "Wallets, Bags & Purses", Description = "Backpacks, sling bags, pouches, coin wallets", IconClass = "bi-wallet2" },
                    new Category { Name = "Keys & Keychains", Description = "Room keys, bike/car keys, locker keys", IconClass = "bi-key" },
                    new Category { Name = "Books, Notes & Stationery", Description = "Textbooks, engineering notebooks, pencil boxes, calculators", IconClass = "bi-book" },
                    new Category { Name = "Bottles & Lunch Boxes", Description = "Water flasks, tiffin containers, tumblers", IconClass = "bi-cup-straw" },
                    new Category { Name = "Clothing & Accessories", Description = "Jackets, watches, glasses, umbrella, caps", IconClass = "bi-smartwatch" },
                    new Category { Name = "Miscellaneous / Others", Description = "Any other items found across campus", IconClass = "bi-box-seam" }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 5. Seed Realistic Sample Items
            if (!await context.Items.AnyAsync() && studentUser != null && adminUser != null)
            {
                var categories = await context.Categories.ToListAsync();
                var elecCat = categories.FirstOrDefault(c => c.Name.Contains("Electronics"))?.Id ?? categories[0].Id;
                var idCat = categories.FirstOrDefault(c => c.Name.Contains("ID Cards"))?.Id ?? categories[0].Id;
                var bookCat = categories.FirstOrDefault(c => c.Name.Contains("Books"))?.Id ?? categories[0].Id;
                var keysCat = categories.FirstOrDefault(c => c.Name.Contains("Keys"))?.Id ?? categories[0].Id;

                var sampleItems = new List<Item>
                {
                    new Item
                    {
                        Title = "Found: HP Scientific Calculator (fx-991ES Plus)",
                        Description = "Found silver-grey Casio scientific calculator on the 3rd row desk after the Math exam.",
                        Type = ItemType.Found,
                        CategoryId = elecCat,
                        Location = "Academic Block B, Room 302",
                        EventDate = DateTime.Today.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Status = ItemStatus.Reported,
                        ReporterUserId = studentUser.Id
                    },
                    new Item
                    {
                        Title = "Lost: Blue Dell Laptop Backpack",
                        Description = "Dark blue Dell backpack containing class notebooks and an engineering drawing set. Has a keychain with a blue guitar.",
                        Type = ItemType.Lost,
                        CategoryId = categories.FirstOrDefault(c => c.Name.Contains("Wallets"))?.Id ?? categories[0].Id,
                        Location = "Central Library Reading Hall (Ground Floor)",
                        EventDate = DateTime.Today.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        Status = ItemStatus.Reported,
                        ReporterUserId = studentUser.Id
                    },
                    new Item
                    {
                        Title = "Found: Honda Bike Key with Red Tag",
                        Description = "Single bike key with a red rubber tag labeled 'Speed'. Kept securely with security desk.",
                        Type = ItemType.Found,
                        CategoryId = keysCat,
                        Location = "Main Student Parking Lot near Gate 2",
                        EventDate = DateTime.Today,
                        CreatedAt = DateTime.UtcNow,
                        Status = ItemStatus.Reported,
                        ReporterUserId = adminUser.Id
                    },
                    new Item
                    {
                        Title = "Found: Student ID Card - CSE 3rd Year",
                        Description = "Official college smart card found near the canteen counter. Belonging to department of CSE.",
                        Type = ItemType.Found,
                        CategoryId = idCat,
                        Location = "Central Campus Canteen",
                        EventDate = DateTime.Today.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        Status = ItemStatus.UnderClaim,
                        ReporterUserId = studentUser.Id
                    }
                };

                await context.Items.AddRangeAsync(sampleItems);
                await context.SaveChangesAsync();

                // Seed sample claim for the ID card
                var underClaimItem = sampleItems.Last();
                var sampleClaim = new Claim
                {
                    ItemId = underClaimItem.Id,
                    ClaimerUserId = adminUser.Id, // demo claimer
                    DescriptionProof = "The ID card has roll number CS2022099 with a faded hologram sticker on top right corner.",
                    SubmittedAt = DateTime.UtcNow.AddDays(-1),
                    Status = ClaimStatus.Pending,
                    AdminRemarks = null
                };

                await context.Claims.AddAsync(sampleClaim);
                await context.SaveChangesAsync();
            }
        }
    }
}
