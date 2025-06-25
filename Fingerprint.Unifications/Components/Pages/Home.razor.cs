using Fingerprint.Unifications.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

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

				// Предварительная очистка временных директорий
				var histogramPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Pictures", "Histogram");
				CleanDirectory(histogramPath);

				var audioPath = Path.Combine(Directory.GetCurrentDirectory(), "AudioFilesUser");
				CleanDirectory(audioPath);

				// Сохраняем файл
				var uploadsDir = Path.Combine("AudioFilesUser");
				Directory.CreateDirectory(uploadsDir);

				var uniqueFileName = "audioFile.wav";
				var filePath = Path.Combine(uploadsDir, uniqueFileName);

				await using var stream = e.File.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
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

		// Вспомогательная функция для безопасного закрытия файлов
		private void CleanDirectory(string directoryPath)
		{
			if (Directory.Exists(directoryPath))
			{
				foreach (var file in Directory.EnumerateFiles(directoryPath))
				{
					// Освобождаем файл от других процессов
					try
					{
						using (var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
						{
							fs.Close();
						}
						System.IO.File.Delete(file);
					}
					catch (IOException)
					{
						// Игнорируем ошибки, если файл занят другим процессом
					}
				}
			}
			else
			{
				Directory.CreateDirectory(directoryPath);
			}
		}
	}
}