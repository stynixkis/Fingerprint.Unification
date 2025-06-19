using Audiofingerprint.Interfaces;
using Audiofingerprint.Services;
using AudioFingerprinting;
using Fingerprint.Unifications.Models;
using Fingerprint.Unifications.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Xml.Linq;

namespace Fingerprint.Unifications.Components.Pages
{
	public class UploadingAudioForComparisonModel : ComponentBase
	{
		[Inject]
		private NavigationManager Navigation { get; set; }
		private MfccFingerprinter _mfccFingerprinter = new MfccFingerprinter();
		private FingerprintService _fingerprintService = new FingerprintService();

		protected bool showLoading = false;
		public async Task HandleFileSelected(InputFileChangeEventArgs e)
		{
			try
			{
				// Проверяем формат файла
				UploadedFile.FileName = e.File.Name;
				Console.WriteLine(e.File.Name);
				if (!e.File.Name.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
				{
					return;
				}

				// Создаем папку для сохранения файлов
				var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "AudioFilesUser");
				Directory.CreateDirectory(uploadsDir);

				// Готовим путь для сохранения файла
				var uniqueFileName = "audioFileCompare.wav";
				var filePath = Path.Combine(uploadsDir, uniqueFileName);

				// Удаляем старый файл, если он существует
				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}

				// Сохраняем новый файл с правильным освобождением ресурсов
				await using (var stream = e.File.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024))
				await using (var fileStream = new FileStream(filePath, FileMode.CreateNew))
				{
					await stream.CopyToAsync(fileStream);
				}

				Console.WriteLine($"Файл сохранен: {filePath}");
				showLoading = true;
				StateHasChanged();

				try
				{
					// Пути к аудиофайлам
					var pathCompare = Path.Combine(uploadsDir, "audioFileCompare.wav");
					var path = Path.Combine(uploadsDir, "audioFile.wav");

					// Первый запрос для основного файла
					using (var client = new HttpClient())
					{
						var formData = new MultipartFormDataContent();
						formData.Add(new StringContent(path), "path");

						var response = await client.PostAsync(
							"https://localhost:7199/api/audiofiles/Generation-without-saving-in-the-database",
							formData);

						if (response.IsSuccessStatusCode)
						{
							UploadedFile.AudioFileInformation = await response.Content.ReadFromJsonAsync<AudioFile>();
							Console.WriteLine($"id = {UploadedFile.AudioFileInformation.IdAudio}\n - title = {UploadedFile.AudioFileInformation.TitleAudio}\n" +
								$" - fft = {UploadedFile.AudioFileInformation.FftPrint}\n - mfcc = {UploadedFile.AudioFileInformation.MfccPrint}\n");
						}
						else
						{
							Console.WriteLine($"Ошибка: {response.StatusCode}. {await response.Content.ReadAsStringAsync()}\n");
						}
					}

					// Второй запрос для сравниваемого файла
					using (var client = new HttpClient())
					{
						var formDataCompare = new MultipartFormDataContent();
						formDataCompare.Add(new StringContent(pathCompare), "path");

						var responseCompare = await client.PostAsync(
							"https://localhost:7199/api/audiofiles/Generation-without-saving-in-the-database",
							formDataCompare);

						if (responseCompare.IsSuccessStatusCode)
						{
							var informationFromAPI = await responseCompare.Content.ReadFromJsonAsync<AudioFile>();
							Console.WriteLine($"id = {informationFromAPI.IdAudio}\n - title = {informationFromAPI.TitleAudio}\n" +
								$" - fft = {informationFromAPI.FftPrint}\n - mfcc = {informationFromAPI.MfccPrint}\n");
						}
						else
						{
							Console.WriteLine($"Ошибка: {responseCompare.StatusCode}. {await responseCompare.Content.ReadAsStringAsync()}\n");
						}
					}

					// Ждем генерации файлов отпечатков
					await Task.Delay(5000);

					var workingDir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads",
					"FingerprintResults");

					// Пути к файлам отпечатков
					var pathCompareFFT = Path.Combine(workingDir, "fingerprint_audioFileCompare.bin");
					var pathFFT = Path.Combine(workingDir, "fingerprint_audioFile.bin");
					var pathCompareMFCC = Path.Combine(workingDir, "audioFileCompare_MFCC.bin");
					var pathMFCC = Path.Combine(workingDir, "audioFile_MFCC.bin");

					// Проверяем существование файлов перед сравнением
					if (!File.Exists(pathCompareFFT) || !File.Exists(pathFFT) ||
						!File.Exists(pathCompareMFCC) || !File.Exists(pathMFCC))
					{
						Console.WriteLine("Ошибка: Один или несколько файлов отпечатков не найдены");
						return;
					}

					// Запросы на сравнение
					using (var client = new HttpClient())
					{
						// Сравнение FFT
						var formDataCompareFFT = new MultipartFormDataContent();
						formDataCompareFFT.Add(new StringContent(pathFFT), "pathFirst");
						formDataCompareFFT.Add(new StringContent(pathCompareFFT), "pathSecond");

						// Сравнение MFCC
						var formDataCompareMFCC = new MultipartFormDataContent();
						formDataCompareMFCC.Add(new StringContent(pathMFCC), "pathFirst");
						formDataCompareMFCC.Add(new StringContent(pathCompareMFCC), "pathSecond");

						var responseFFT = await client.PostAsync(
										"https://localhost:7199/api/audiofiles/compare-fft",
										formDataCompareFFT);

						var responseMFCC = await client.PostAsync(
							"https://localhost:7199/api/audiofiles/compare-mfcc",
							formDataCompareMFCC);

						if (responseFFT.IsSuccessStatusCode && responseMFCC.IsSuccessStatusCode)
						{
							try
							{
								// Читаем ответ как строку перед десериализацией
								var fftResponseContent = await responseFFT.Content.ReadAsStringAsync();
								var mfccResponseContent = await responseMFCC.Content.ReadAsStringAsync();

								UploadedFile.CompareFFT = fftResponseContent; // Если ответ - просто строка
								UploadedFile.CompareMFCC = mfccResponseContent;

								Console.WriteLine($"FFT сравнение: {UploadedFile.CompareFFT}");
								Console.WriteLine($"MFCC сравнение: {UploadedFile.CompareMFCC}");

								// Визуализация
								_mfccFingerprinter.PlotAudioWaveformTwo(path, pathCompare);
								_fingerprintService.GenerateComparisonHistogram(path, pathCompare);

								Navigation.NavigateTo("/AudioFileComparison");
							}
							catch (Exception ex)
							{
								Console.WriteLine($"Ошибка обработки ответа: {ex.Message}");
							}
						}
						else
						{
							var fftError = await responseFFT.Content.ReadAsStringAsync();
							var mfccError = await responseMFCC.Content.ReadAsStringAsync();
							Console.WriteLine($"Ошибка FFT: {responseFFT.StatusCode}. {fftError}");
							Console.WriteLine($"Ошибка MFCC: {responseMFCC.StatusCode}. {mfccError}");
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Критическая ошибка: {ex.ToString()}");
				}

			}
			finally
			{
				showLoading = false;
				StateHasChanged();
			}
		}
	}
}
