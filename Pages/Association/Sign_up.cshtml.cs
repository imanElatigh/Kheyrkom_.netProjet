using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KheyrkoumProjet.Pages.Association
{
    public class Sign_upModel : PageModel
    {
        private readonly ILogger<Sign_upModel> _logger;
        private readonly IConfiguration _configuration;

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

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
        public IFormFile ProfileImage { get; set; }

        [BindProperty]
        public IFormFile AnotherImage { get; set; }

        public string ErrorMessage { get; set; } // New property for error message

        public Sign_upModel(ILogger<Sign_upModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string profileImagePath = null;
            string anotherImagePath = null;

            if (ProfileImage != null)
            {
                string uploadsDirectory = Path.Combine("wwwroot", "uploads");
                Directory.CreateDirectory(uploadsDirectory);
                profileImagePath = "/uploads/" + ProfileImage.FileName;
                Console.WriteLine("Profile Image Path: " + profileImagePath);
                using (var stream = new FileStream(Path.Combine(uploadsDirectory, ProfileImage.FileName), FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }
            }

            if (AnotherImage != null)
            {
                string uploadsDirectory = Path.Combine("wwwroot", "uploads");
                Directory.CreateDirectory(uploadsDirectory);
                anotherImagePath = "/uploads/" + AnotherImage.FileName;
                Console.WriteLine("Another Image Path: " + anotherImagePath);
                using (var stream = new FileStream(Path.Combine(uploadsDirectory, AnotherImage.FileName), FileMode.Create))
                {
                    await AnotherImage.CopyToAsync(stream);
                }
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if Nom and NumeroLicence exist in the licence table
                    string checkSql = "SELECT COUNT(*) FROM licence WHERE Nom = @Nom AND NumeroLicence = @NumeroLicence";
                    using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Nom", Nom);
                        checkCommand.Parameters.AddWithValue("@NumeroLicence", NumeroLicence);
                        int count = (int)await checkCommand.ExecuteScalarAsync();
                        if (count == 0)
                        {
                            ErrorMessage = "!!لا يمكنك التسجيل تأكد من ترخيص الجمعية ";
                            return Page();
                        }
                    }

                    string sql = @"
                        INSERT INTO Associations (Email, Password, Nom, Information, Adresse, Tel, NumeroLicence, ProfileImagePath, AnotherImagePath)
                        VALUES (@Email, @Password, @Nom, @Information, @Adresse, @Tel, @NumeroLicence, @ProfileImagePath, @AnotherImagePath)";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Email", Email);
                        command.Parameters.AddWithValue("@Password", Password); // Note: In a real application, hash the password before storing it.
                        command.Parameters.AddWithValue("@Nom", Nom);
                        command.Parameters.AddWithValue("@Information", (object)Information ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Adresse", (object)Adresse ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Tel", (object)Tel ?? DBNull.Value);
                        command.Parameters.AddWithValue("@NumeroLicence", (object)NumeroLicence ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ProfileImagePath", (object)profileImagePath ?? DBNull.Value);
                        command.Parameters.AddWithValue("@AnotherImagePath", (object)anotherImagePath ?? DBNull.Value);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while signing up.");
                ModelState.AddModelError(string.Empty, "An error occurred while signing up. Please try again.");
                return Page();
            }

            return RedirectToPage("/Association/Login");
        }
    }
}
