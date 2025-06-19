using Fingerprint.Unifications.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Fingerprint.Unifications.Components.Pages
{
	public class HomeModel : ComponentBase
	{
		[Inject]
		private NavigationManager Navigation { get; set; }

		public async Task HandleFileSelected(InputFileChangeEventArgs e)
		{
			try
			{
				UploadedFile.Clear();

				UploadedFile.FileName = e.File.Name;
				Console.WriteLine(e.File.Name);

				if (!e.File.Name.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
				{
					return;
				}

				var histogramPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Pictures", "Histogram");
				if (Directory.Exists(histogramPath))
				{
					foreach (var file in Directory.GetFiles(histogramPath))
					{
						System.IO.File.Delete(file);
					}
				}
				else
				{
					Directory.CreateDirectory(histogramPath);
				}

				var audioPath = Path.Combine(Directory.GetCurrentDirectory(), "AudioFilesUser");
				if (Directory.Exists(audioPath))
				{
					foreach (var file in Directory.GetFiles(audioPath))
					{
						System.IO.File.Delete(file);
					}
				}
				else
				{
					Directory.CreateDirectory(audioPath);
				}

				var uploadsDir = Path.Combine("AudioFilesUser");
				Directory.CreateDirectory(uploadsDir);

				var uniqueFileName = "audioFile.wav";
				var filePath = Path.Combine(uploadsDir, uniqueFileName);

				// Сохраняем файл
				await using var stream = e.File.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB max
				await using var fileStream = new FileStream(filePath, FileMode.Create);
				await stream.CopyToAsync(fileStream);

				Console.WriteLine($"Файл сохранен: {filePath}");
				Navigation.NavigateTo("/ChosingAction");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка: " + ex.Message);
			}
		}
	}
}