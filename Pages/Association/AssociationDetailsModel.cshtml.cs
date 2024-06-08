using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace KheyrkoumProjet.Pages.Association
{
    public class AssociationDetailsModel : PageModel
    {
        private readonly string _connectionString;

        public AssociationModel Association { get; set; }
        public List<Post> Posts { get; set; } // List to hold posts

        public AssociationDetailsModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Fetch association details based on ID
                Association = await FetchAssociationAsync(connection, id);

                // Fetch posts associated with the association
                Posts = await FetchPostsAsync(connection, id);
            }

            if (Association == null)
            {
                return NotFound();
            }

            return Page();
        }

        private async Task<AssociationModel> FetchAssociationAsync(SqlConnection connection, int id)
        {
            var association = new AssociationModel();

            string associationSql = "SELECT * FROM Associations WHERE Id = @Id";
            using (SqlCommand command = new SqlCommand(associationSql, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        association.Id = reader.GetInt32(0);
                        association.Email = reader.GetString(1);
                        association.Password = reader.GetString(2);
                        association.Nom = reader.GetString(3);
                        association.Information = reader.GetString(4);
                        association.Adresse = reader.GetString(5);
                        association.Tel = reader.GetString(6);
                        association.NumeroLicence = reader.GetString(7);
                        association.ProfileImagePath = reader.GetString(8);
                        association.AnotherImagePath = reader.GetString(9);
                        association.CreatedAt = reader.GetDateTime(10);
                    }
                }
            }

            return association;
        }

        private async Task<List<Post>> FetchPostsAsync(SqlConnection connection, int associationId)
        {
            var posts = new List<Post>();

            string postSql = "SELECT *FROM Post WHERE AssociationID = @AssociationID";
            using (SqlCommand command = new SqlCommand(postSql, connection))
            {
                command.Parameters.AddWithValue("@AssociationID", associationId);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var post = new Post
                        {
                            PostID = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Content = reader.GetString(2),
                            ImagePath = reader.GetString(3),
                            AssociationID = reader.GetInt32(4),
                            CreatedAt = reader.GetDateTime(5)
                        };
                        posts.Add(post);
                    }
                }
            }

            return posts;
        }
    }
}
