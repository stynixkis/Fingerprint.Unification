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
        // Инициализация mock контекста базы данных и контроллера для тестирования
        _mockContext = new Mock<FingerprintDatabaseContext>();
        _controller = new AudioFilesController(_mockContext.Object);
    }

    /// <summary>
    /// Тестирует метод GetAudioFiles на возврат списка аудиофайлов
    /// Проверяет что:
    /// 1. Возвращается ActionResult с коллекцией AudioFile
    /// 2. Коллекция содержит ожидаемое количество элементов
    /// 3. Данные в коллекции соответствуют тестовым данным
    /// </summary>
    [Fact]
    public async Task GetAudioFiles_ReturnsListOfAudioFiles()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<FingerprintDatabaseContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        using (var context = new FingerprintDatabaseContext(options))
        {
            context.AudioFiles.Add(new AudioFile
            {
                IdAudio = 1,
                TitleAudio = "Test Audio",
                FftPrint = new byte[0],
                MfccPrint = new byte[0]
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

    /// <summary>
    /// Тестирует метод GetAudioFile на успешное получение аудиофайла по существующему ID
    /// Проверяет что:
    /// 1. Для существующего/несуществующего ID возвращается корректный AudioFile
    /// 2. Возвращаемый объект содержит ожидаемые значения
    /// </summary>
    [Theory]
    [InlineData(1)]  // Существующий ID
    [InlineData(999)] // Несуществующий ID
    public async Task GetAudioFile_ReturnsAudioFile_WhenIdExists(int id)
    {
        // Arrange
        var testAudioFile = new AudioFile
        {
            IdAudio = 1,
            TitleAudio = "Test Audio File",
            FftPrint = new byte[] { 0x01, 0x02, 0x03 },
            MfccPrint = new byte[] { 0x07, 0x04, 0x03 }
        };

        _mockContext.Setup(c => c.AudioFiles.FindAsync(id))
            .ReturnsAsync(testAudioFile);

        // Act
        var result = await _controller.GetAudioFile(id);

        // Assert

        if (result != null)
        {
            var actionResult = Assert.IsType<ActionResult<AudioFile>>(result);
            var returnValue = Assert.IsType<AudioFile>(actionResult.Value);

            Assert.Equal(testAudioFile.IdAudio, returnValue.IdAudio);
            Assert.Equal(testAudioFile.TitleAudio, returnValue.TitleAudio);
            Assert.Equal(testAudioFile.FftPrint, returnValue.FftPrint);
            Assert.Equal(testAudioFile.MfccPrint, returnValue.MfccPrint);
        }
        else
            Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    /// Тестирует метод PostAudioFileDB на валидацию пустого пути
    /// Проверяет что:
    /// 1. При передаче пустого пути возвращается BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task PostAudioFileDB_ReturnsBadRequest_WhenPathEmpty()
    {
        // Act
        var result = await _controller.PostAudioFileDB("");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Тестирует метод PostAudioFilesComparisonMFCC на корректное сравнение файлов
    /// Проверяет что:
    /// 1. Возвращается ActionResult с результатом сравнения
    /// 2. Результат содержит ожидаемый текст с процентом схожести
    /// </summary>
    [Fact]
    public async Task CompareMFCC_ReturnsComparisonResult()
    {
        // Arrange
        var testFile1 = Path.Combine(Path.GetTempPath(), "test1.bin");
        var testFile2 = Path.Combine(Path.GetTempPath(), "test2.bin");

        var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        await File.WriteAllBytesAsync(testFile1, testData);
        await File.WriteAllBytesAsync(testFile2, testData);

        var fingerprinter = new TestableMfccFingerprinter();

        var controller = new AudioFilesController(_mockContext.Object)
        {
            _fingerprinterMFCC = fingerprinter
        };

        // Act
        var result = await controller.PostAudioFilesComparisonMFCC(testFile1, testFile2);

        // Assert
        Assert.NotNull(result);
        var actionResult = Assert.IsType<ActionResult<string>>(result);
        Assert.NotNull(actionResult.Value);
        var value = Assert.IsType<string>(actionResult.Value);
        Assert.Contains("Процент схожести по методу MFCC:", value);
    }

    /// <summary>
    /// Тестирует метод DeleteAudioFile на успешное удаление
    /// Проверяет что:
    /// 1. Для существующего ID возвращается NoContentResult
    /// 2. Метод Remove вызывается ровно один раз
    /// </summary>
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
        _mockContext.Verify(c => c.AudioFiles.Remove(testFile), Times.Once());
    }

    // Вспомогательные классы

    /// <summary>
    /// Вспомогательный класс для создания mock DbSet
    /// </summary>
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

    /// <summary>
    /// Тестовая реализация IAsyncEnumerator для поддержки async тестов
    /// </summary>
    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public T Current => _inner.Current;
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
        public ValueTask DisposeAsync() => new ValueTask();
    }

    /// <summary>
    /// Тестовая реализация MfccFingerprinter с фиксированным результатом сравнения
    /// </summary>
    public class TestableMfccFingerprinter : MfccFingerprinter
    {
        public virtual double Compare(byte[] first, byte[] second)
        {
            return 0.85;
        }
    }
}