using AudioFingerprinting;
using Fingerprint.Unifications;
using Fingerprint.Unifications.Controllers;
using Fingerprint.Unifications.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

public class AudioFilesControllerTests
{
	private readonly Mock<FingerprintDatabaseContext> _mockContext;
	private readonly AudioFilesController _controller;

	public AudioFilesControllerTests()
	{
		_mockContext = new Mock<FingerprintDatabaseContext>();
		_controller = new AudioFilesController(_mockContext.Object);
	}


	[Fact]
	public async Task GetAudioFiles_ReturnsListOfAudioFiles()
	{
		// Arrange
		// Создаем новый сервис-провайдер для каждого теста
		var serviceProvider = new ServiceCollection()
			.AddEntityFrameworkInMemoryDatabase()
			.BuildServiceProvider();

		var options = new DbContextOptionsBuilder<FingerprintDatabaseContext>()
			.UseInMemoryDatabase(databaseName: "TestDatabase")
			.UseInternalServiceProvider(serviceProvider) // Используем наш сервис-провайдер
			.Options;

		// Добавляем тестовые данные в InMemory базу данных
		using (var context = new FingerprintDatabaseContext(options))
		{
			context.AudioFiles.Add(new AudioFile
			{
				IdAudio = 1,
				TitleAudio = "Test Audio",
				FftPrint = new byte[0], // Пустой массив байтов
				MfccPrint = new byte[0] // Пустой массив uint
			});
			await context.SaveChangesAsync();
		}

		using (var context = new FingerprintDatabaseContext(options))
		{
			var controller = new AudioFilesController(context);

			// Act
			var result = await controller.GetAudioFiles();

			// Assert
			var actionResult = Assert.IsType<ActionResult<IEnumerable<AudioFile>>>(result);
			var returnValue = Assert.IsType<List<AudioFile>>(actionResult.Value);
			Assert.Single(returnValue);
			Assert.Equal("Test Audio", returnValue.First().TitleAudio);
		}
	}

	// Вспомогательный класс, если CreateMockDbSet() не работает
	public static class MockDbSetHelper
	{
		public static DbSet<T> CreateMockDbSet<T>(List<T> data) where T : class
		{
			var queryable = data.AsQueryable();
			var mockSet = new Mock<DbSet<T>>();

			mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
			mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
			mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
			mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

			mockSet.As<IAsyncEnumerable<T>>()
				.Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
				.Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

			return mockSet.Object;
		}
	}

	internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
	{
		private readonly IEnumerator<T> _inner;
		public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
		public T Current => _inner.Current;
		public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
		public ValueTask DisposeAsync() => new ValueTask();
	}

	[Fact]
	public async Task GetAudioFile_ReturnsNotFound_WhenIdInvalid()
	{
		// Arrange
		_mockContext.Setup(c => c.AudioFiles.FindAsync(It.IsAny<int>()))
			.ReturnsAsync((AudioFile)null);

		// Act
		var result = await _controller.GetAudioFile(1);

		// Assert
		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task PostAudioFileDB_ReturnsBadRequest_WhenPathEmpty()
	{
		// Act
		var result = await _controller.PostAudioFileDB("");

		// Assert
		Assert.IsType<BadRequestObjectResult>(result.Result);
	}

	[Fact]
	public async Task CompareMFCC_ReturnsComparisonResult()
	{
		// Arrange
		var testFile1 = Path.Combine(Path.GetTempPath(), "test1.bin");
		var testFile2 = Path.Combine(Path.GetTempPath(), "test2.bin");

		// Создаем корректные тестовые данные
		var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
		await File.WriteAllBytesAsync(testFile1, testData);
		await File.WriteAllBytesAsync(testFile2, testData);

		// Используем реальный MfccFingerprinter с переопределением метода Compare
		var fingerprinter = new TestableMfccFingerprinter();

		// Создаем контроллер
		var controller = new AudioFilesController(_mockContext.Object)
		{
			_fingerprinterMFCC = fingerprinter
		};

		// Act
		var result = await controller.PostAudioFilesComparisonMFCC(testFile1, testFile2);

		// Assert
		Assert.NotNull(result); // Проверяем что результат не null

		var actionResult = Assert.IsType<ActionResult<string>>(result);
		Assert.NotNull(actionResult.Value); // Проверяем что Value не null

		var value = Assert.IsType<string>(actionResult.Value);
		Assert.Contains("Процент схожести по методу MFCC:", value);
	}

	// Тестовая реализация с гарантированным результатом
	public class TestableMfccFingerprinter : MfccFingerprinter
	{
		public virtual double Compare(byte[] first, byte[] second)
		{
			// Всегда возвращаем фиксированное значение для теста
			return 0.85;
		}
	}

	[Fact]
	public async Task DeleteAudioFile_ReturnsNoContent_WhenSuccessful()
	{
		// Arrange
		var testFile = new AudioFile { IdAudio = 1 };
		_mockContext.Setup(c => c.AudioFiles.FindAsync(1))
			.ReturnsAsync(testFile);
		_mockContext.Setup(c => c.SaveChangesAsync(default))
			.ReturnsAsync(1);

		// Act
		var result = await _controller.DeleteAudioFile(1);

		// Assert
		Assert.IsType<NoContentResult>(result);
	}
}