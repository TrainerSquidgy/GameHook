using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace GameHook.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OverlayEditorController : ControllerBase
    {
        [HttpPost]
        public IActionResult Launch()
        {
            try
            {
                var exePath = Path.Combine(AppContext.BaseDirectory, "GameHook.OverlayEditor.exe");

                if (!System.IO.File.Exists(exePath))
                    return NotFound("Overlay Editor executable not found.");

                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });

                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message);
            }
        }
    }
}
