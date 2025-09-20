using FragEngine.EngineCore;
using FragEngine.EngineCore.Config;
using FragEngine.EngineCore.Enums;
using FragEngine.Graphics.Constants;
using FragEngine.Logging;
using Veldrid;

namespace FragEngine.UnitTests.EngineCore;

/// <summary>
/// Unit tests for the <see cref="PlatformService"/> singleton.
/// </summary>
[TestOf(typeof(PlatformService))]
public sealed class PlatformServiceTest
{
	#region Fields

	private ILogger logger;
	private EngineConfig config;

	#endregion
	#region Tests

	[SetUp]
	public void Setup()
	{
		logger = new ConsoleLogger();
		config = EngineConfig.CreateDefault();
	}

	[TearDown]
	public void TearDown()
	{
		logger.Dispose();
	}

	[Test]
	[Description("Tests that the platform service returns a valid root directory path.")]
	public void GetRootDirectoryPath_Test()
	{
		// Act:
		string rootDir = string.Empty;
		Assert.DoesNotThrow(() => rootDir = PlatformService.GetRootDirectoryPath(), "Exception thrown by method.");

		Assert.That(rootDir, Is.Not.Null, "Root directory was null.");
		Assert.That(rootDir, Is.Not.Empty, "Root directory was an empty string.");

		bool rootDirExists = Directory.Exists(rootDir);
		Assert.That(rootDirExists, Is.True, "Root directory does not exist.");
	}

	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	[Description("Tests that an exception is thrown if constructor parameters are null.")]
	public void Constructor_NullParameters_Test(bool _isLoggerNull, bool _isConfigNull)
	{
		// Arrange:
		ILogger? loggerParam = _isLoggerNull ? null : logger;
		EngineConfig? configParam = _isConfigNull ? null : config;

		// Assert:
		Assert.Throws<ArgumentNullException>(() => new PlatformService(loggerParam!, configParam!), "Null argument exception wasn't thrown.");
	}

	[Test]
	[Description("Tests that the right operating system is detected.")]
	public void Constructor_DetectOS_Test()
	{
		// Act:
		PlatformService platformService = new(logger, config);

		// Assert:
		Assert.Multiple(() =>
		{
			Assert.That(platformService.OperatingSystem, Is.Not.EqualTo(OperatingSystemType.Unknown), "Failed to identify OS platform.");
			Assert.That(platformService.GraphicsBackend, Is.Not.EqualTo(GraphicsConstants.invalidBackend), "Failed to identify graphics backend.");
		});
	}

	[Test]
	[Description("Tests that required directories are created if missing.")]
	public void Constructor_CreateDirectories_Test()
	{
		// Arrange:
		string rootDirPath = PlatformService.GetRootDirectoryPath();
		string settingsDirPath = Path.Combine(rootDirPath, "settings");

		// Act:
		if (Directory.Exists(settingsDirPath))
		{
			Directory.Delete(settingsDirPath, true);
		}

		// Assert:
		Assert.DoesNotThrow(() => new PlatformService(logger, config), "Exception thrown by constructor.");

		bool createdSettingsDir = Directory.Exists(settingsDirPath);
		Assert.That(createdSettingsDir, Is.True, "Failed to create settings directory.");
	}

	#endregion
}
