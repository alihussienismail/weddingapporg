using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace weddingapporg.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<ChatController> _logger;

        // ✅ تم إصلاح الـ Constructor عشان يستقبل الـ ILogger
        public ChatController(IChatClient chatClient, ILogger<ChatController> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            try
            {
                // ✅ استخدام GetResponseAsync (الصحيحة لـ IChatClient)
                var response = await _chatClient.GetResponseAsync(request.Message);
                return Json(new { response = response.Text });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return BadRequest("Message wasn't sent.");
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}