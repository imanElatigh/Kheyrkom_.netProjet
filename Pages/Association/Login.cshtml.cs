using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KheyrkoumProjet.Pages.Association
{
    public class Login : PageModel  
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public Login(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult OnGet()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Association/Profile");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT Password FROM Associations WHERE Email = @Email";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", Email);
                    string storedPassword = (await command.ExecuteScalarAsync())?.ToString();

                    if (storedPassword != null && storedPassword == Password)
                    {
                        // Authentication successful
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, Email)
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                        return RedirectToPage("/Association/Profile");
                    }
                }
            }

            // Authentication failed
            TempData["ErrorMessage"] = "Invalid email or password";
            return RedirectToPage("/Association/Login");
        }
    }
}
