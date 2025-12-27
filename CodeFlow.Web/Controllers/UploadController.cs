using CodeFlow.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System.Security.Cryptography;

namespace CodeFlow.Web.Controllers
{
    public class UploadController : Controller
    {
        private readonly string[] _allowedExtenstions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024;
        private const int MaxFileWidth = 600;
        private const int MaxFileHeight = 400;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public UploadController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost]
        [Authorize]
        [LogAction]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                if (file.Length > MaxFileSize)
                {
                    return BadRequest(new { error = @"File size exceeds 5 mb limit" });
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(extension) || !_allowedExtenstions.Contains(extension))
                {
                    return BadRequest(new { error = "File type not allowed" });
                }

                var fileName = GenerateFileName(file.FileName);
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "images");

                Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                using var image = await Image.LoadAsync(file.OpenReadStream());
                int width = image.Width < MaxFileWidth ? image.Width : MaxFileWidth;
                int height = image.Height < MaxFileHeight? image.Height : MaxFileHeight;
                image.Mutate(x => x.Resize(width, height, KnownResamplers.Lanczos8));
                image.Save(stream, new PngEncoder());

                var imageUrl = $"{Request.Scheme}://{Request.Host}/uploads/images/{fileName}";

                return Ok(new {
                    data = new
                    {
                        filePath = imageUrl
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new {error = "An error occured while uploading image"});
            }
        }

        // TODO periodically remove unused images. 

        [Route("api/delete")]
        [HttpDelete("image/{fileName}")]
        [Authorize]
        public IActionResult DeleteImage(string fileName)
        {
            try
            {
                if(fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                {
                    return BadRequest(new { error = "Invalid file name" });
                }

                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "images", fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return Ok(new { success = true });
                }

                return NotFound(new { error = "File not found" });
            }
            catch(Exception)
            {
                return StatusCode(500, new { error = "An error occurred while uploading the image" });
            }
        }

        private static string GenerateFileName(string originalFileName)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[16];
            rng.GetBytes(bytes);

            var stringRandom = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            var timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            return $"{timeStamp}_{stringRandom}{extension}";

        }
    }
}
