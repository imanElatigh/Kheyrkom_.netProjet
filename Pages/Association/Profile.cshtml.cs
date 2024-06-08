using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace KheyrkoumProjet.Pages.Association
{
    public class Profile : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Nom { get; set; }
        [BindProperty]
        public string Information { get; set; }
        [BindProperty]
        public string Adresse { get; set; }
        [BindProperty]
        public string Tel { get; set; }
        [BindProperty]
        public string NumeroLicence { get; set; }
        [BindProperty]
        public string ProfileImagePath { get; set; }
        [BindProperty]
        public string AnotherImagePath { get; set; }
        public List<Post> Posts { get; set; } // List to hold posts

        public Profile(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            string email = User.Identity.Name;

            if (string.IsNullOrEmpty(email))
            {
                // Redirect to login if user is not authenticated
                return RedirectToPage("/Association/Login");
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT * FROM Associations WHERE Email = @Email";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Email = reader["Email"].ToString();
                            Nom = reader["Nom"].ToString();
                            Information = reader["Information"].ToString();
                            Adresse = reader["Adresse"].ToString();
                            Tel = reader["Tel"].ToString();
                            NumeroLicence = reader["NumeroLicence"].ToString();
                            ProfileImagePath = reader["ProfileImagePath"].ToString().Replace("\\", "/");
                            AnotherImagePath = reader["AnotherImagePath"].ToString().Replace("\\", "/");
                        }
                    }
                }
            }

            // Fetch posts associated with the association
            Posts = await FetchPostsAsync(connectionString, email);

            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Association/Login");
        }

      public async Task<IActionResult> OnPostCreatePostAsync(string postTitle, string postContent, IFormFile postImage)
{
    try
    {
        // Get association email from the authenticated user
        string associationEmail = User.Identity.Name;

        // Get association ID based on email
        string connectionString = _configuration.GetConnectionString("DefaultConnection");
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            string getAssociationIdQuery = "SELECT Id FROM Associations WHERE Email = @Email";
            using (SqlCommand command = new SqlCommand(getAssociationIdQuery, connection))
            {
                command.Parameters.AddWithValue("@Email", associationEmail);
                object result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    throw new Exception("Association ID not found for email: " + associationEmail);
                }

                int associationId = (int)(result);

                // Save post data to the database
                string insertQuery = @"
                    INSERT INTO Post (Title, Content, ImagePath, AssociationID, CreatedAt)
                    VALUES (@Title, @Content, @ImagePath, @AssociationID, @CreatedAt);
                    SELECT SCOPE_IDENTITY();"; // Get the newly inserted post ID

                using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@Title", postTitle);
                    insertCommand.Parameters.AddWithValue("@Content", postContent);

                    string imagePath = null;
                    if (postImage != null)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(postImage.FileName);
                        imagePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(imagePath, FileMode.Create))
                        {
                            await postImage.CopyToAsync(stream);
                        }
                        imagePath = "/uploads/" + uniqueFileName;
                    }

                    insertCommand.Parameters.AddWithValue("@ImagePath", imagePath ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@AssociationID", associationId);
                    insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    var postIdObj = await insertCommand.ExecuteScalarAsync();
                    if (postIdObj == null)
                    {
                        throw new Exception("Failed to insert the post and retrieve the new post ID.");
                    }

                    int postId = Convert.ToInt32(postIdObj);

                    // Post created successfully, redirect to the profile page
                    return RedirectToPage("/Association/Profile");
                }
            }
        }
    }
    catch (Exception ex)
    {
        // Log the detailed error
        Console.Error.WriteLine(ex);
        // Return an error page or message
        ModelState.AddModelError(string.Empty, "An error occurred while creating the post. Please try again.");
        return Page();
    }
}


        private async Task<List<Post>> FetchPostsAsync(string connectionString, string email)
        {
            var posts = new List<Post>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string postSql = "SELECT [PostID], [Title], [Content], [ImagePath], [AssociationID], [CreatedAt] FROM [Post] WHERE [AssociationID] IN (SELECT [Id] FROM [Associations] WHERE [Email] = @Email)";
                using (SqlCommand command = new SqlCommand(postSql, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var post = new Post
                            {
                                PostID = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Content = reader.GetString(2),
                                ImagePath = reader.IsDBNull(3) ? null : reader.GetString(3),
                                AssociationID = reader.GetInt32(4),
                                CreatedAt = reader.GetDateTime(5)
                            };
                            posts.Add(post);
                        }
                    }
                }
            }

            return posts;
        }
    }
}
