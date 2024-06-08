using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace KheyrkoumProjet.Pages.Chatbot
{
    public class ChatbotModel : PageModel
    {
        private readonly ILogger<ChatbotModel> _logger;
        private readonly ChatbotContext _context;

        public ChatbotModel(ILogger<ChatbotModel> logger, ChatbotContext context)
        {
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public string Query { get; set; }
        public string Response { get; set; }

        public void OnGet()
        {
        }

        public void OnPost()
        {
            var question = _context.ChatbotQuestions.FirstOrDefault(q => q.Question == Query);
            if (question != null)
            {
                Response = question.Response;
            }
            else
            {
                Response = "أعتذر, ليست لدي المعلومات الكافية";
            }
        }

        public class ChatbotQuestion
        {
            public int Id { get; set; }
            public string Question { get; set; }
            public string Response { get; set; }
        }

        public class ChatbotContext : DbContext
        {
            public ChatbotContext(DbContextOptions<ChatbotContext> options)
                : base(options)
            {
            }
  
            public DbSet<ChatbotQuestion> ChatbotQuestions { get; set; }
        }
    }
}
