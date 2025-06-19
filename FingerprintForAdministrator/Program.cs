using Audiofingerprint.Services;
using AudioFingerprinting;
using FingerprintForAdministrator.Data;
using FingerprintForAdministrator.Models;

FingerprintContext _context = new FingerprintContext();
byte Menu()
{
	Console.WriteLine("\n\tВыберите действие");
	Console.WriteLine("\t1 - ввод данных");
	Console.WriteLine("\t2 - удаление данных");
	Console.WriteLine("\t3 - изменение данных");
	Console.WriteLine("\t4 - вывод всех данных");
	Console.WriteLine("\t0 - выход");
	Console.Write("\tВыбор: ");
	byte choice;
	while (!byte.TryParse(Console.ReadLine(), out choice) || choice > 4)
		Console.Write("Сделайте выбор повторно: ");
	return choice;
}

void PostData()
{
	try
	{
		Console.Write("\n\tВведите путь к файлу .wav: ");
		string path = Console.ReadLine().Trim();

		if (string.IsNullOrWhiteSpace(path))
			throw new ArgumentException("Path cannot be empty");

		if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("Only .wav files are supported");

		if (!System.IO.File.Exists(path))
			throw new FileNotFoundException($"File not found: {path}");

		var audioFile = new AudioFile
		{
			TitleAudio = Path.GetFileNameWithoutExtension(path),
			IdAudio = _context.AudioFiles.Any()
				? _context.AudioFiles.Max(x => x.IdAudio) + 1
				: 1
		};

		var workingDir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads",
					"FingerprintResults");

		Directory.CreateDirectory(workingDir);

		FingerprintService fingerprintService = new FingerprintService();
		string resultGenerateFFT = fingerprintService.GenerateFingerprint(path, workingDir);
		try
		{
			audioFile.FftPrint = System.IO.File.ReadAllBytes(resultGenerateFFT);
		}
		catch (IOException ex)
		{
			Console.WriteLine($"Failed to read FFT file: {ex.Message}");
		}

		MfccFingerprinter _fingerprinterMFCC = new MfccFingerprinter();

		audioFile.MfccPrint = _fingerprinterMFCC.GenerateFingerprint(path);

		_context.AudioFiles.Add(audioFile);
		_context.SaveChangesAsync();
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Ошибка: {ex.Message}\n");
	}
}
void DeleteData()
{

}
void PutData()
{

}
//wwwroot\Pictures\Histogram
void GetData()
{
    Console.WriteLine("\n\nВсе записи\n");
	foreach(var item  in _context.AudioFiles)
	{
		Console.WriteLine($"\n\t\tid : {item.IdAudio}");
		Console.WriteLine($"\n\t\ttitle : {item.TitleAudio}");
		Console.Write($"\n\t\tFFT : ");
		foreach(var i in item.FftPrint)
			Console.Write(i.ToString());

		Console.Write($"\n\t\tMFCC : ");
		foreach (var i in item.MfccPrint)
			Console.Write(i.ToString());

        Console.WriteLine();
	}
}


Console.WriteLine("\n\n\t\t<<<<<<< РАБОТА С БАЗОЙ ДАННЫХ ДЛЯ АДМИНИСТАТОРА >>>>>>>>>\n\n");
while (true)
{
	switch (Menu())
	{
		case 0: return;
		case 1:
			PostData();
			break;
		case 2:
			DeleteData();
			break;
		case 3:
			PutData();
			break;
		case 4:
			GetData();
			break;
	}
}
