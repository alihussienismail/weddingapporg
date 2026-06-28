using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace weddingapporg.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatClient _chatClient;

        public ChatController(IChatClient chatClient)
        {
            _chatClient = chatClient;
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
                // Send the user prompt to the local llama3.2 model
                var response = await _chatClient.CompleteAsync(request.Message);

                return Json(new { response = response.Message.Text });
            }
            catch (Exception ex)
            {
                // Gracefully handle if Ollama isn't running or crashes
                return Json(new { response = "Error: Unable to reach the local AI. Ensure Ollama is running." });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}