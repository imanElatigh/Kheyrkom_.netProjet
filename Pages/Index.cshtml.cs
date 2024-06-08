using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace KheyrkoumProjet.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly string _connectionString;

        public List<AssociationModel> Associations { get; set; } = new List<AssociationModel>();
        public List<Post> Posts { get; set; } = new List<Post>();

        public IndexModel(ILogger<IndexModel> logger, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task OnGetAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Fetch associations
                    Associations = await FetchAssociationsAsync(connection);

                    // Fetch posts with association names
                    Posts = await FetchPostsAsync(connection);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching data from the database.");
                throw; // Rethrow the exception to be handled by the caller
            }
        }

        private async Task<List<AssociationModel>> FetchAssociationsAsync(SqlConnection connection)
        {
            var associations = new List<AssociationModel>();

            string associationSql = "SELECT * FROM Associations";
            using (SqlCommand command = new SqlCommand(associationSql, connection))
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    associations.Add(new AssociationModel
                    {
                        Id = reader.GetInt32(0),
                        Email = reader.GetString(1),
                        Password = reader.GetString(2),
                        Nom = reader.GetString(3),
                        Information = reader.GetString(4),
                        Adresse = reader.GetString(5),
                        Tel = reader.GetString(6),
                        NumeroLicence = reader.GetString(7),
                        ProfileImagePath = reader.GetString(8),
                        AnotherImagePath = reader.GetString(9),
                        CreatedAt = reader.GetDateTime(10)
                    });
                }
            }

            return associations;
        }

        private async Task<List<Post>> FetchPostsAsync(SqlConnection connection)
        {
            var posts = new List<Post>();

            string postSql = "SELECT p.*, a.Nom AS AssociationName " +
                             "FROM Post p " +
                             "INNER JOIN Associations a ON p.AssociationID = a.Id";

            using (SqlCommand command = new SqlCommand(postSql, connection))
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    posts.Add(new Post
                    {
                        PostID = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Content = reader.GetString(2),
                        ImagePath = reader.GetString(3),
                        AssociationID = reader.GetInt32(4),
                        AssociationName = reader.GetString(6), // Index of AssociationName column
                        CreatedAt = reader.GetDateTime(5)
                    });
                }
            }

            return posts;
        }
    }

    public class AssociationModel
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Nom { get; set; }
        public string Information { get; set; }
        public string Adresse { get; set; }
        public string Tel { get; set; }
        public string NumeroLicence { get; set; }
        public string ProfileImagePath { get; set; }
        public string AnotherImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Post
    {
        public int PostID { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImagePath { get; set; }
        public int AssociationID { get; set; }
        public string AssociationName { get; set; } // New property for association name
        public DateTime CreatedAt { get; set; }
    }
}