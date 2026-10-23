using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace QuestCompass.Launcher;

public sealed class LauncherForm : Form
{
    private const string BundledVersion = "1.8.0";
    private const string BundledSha256 = "277a6094d9f3b42e1549be2720f02540f301d90fe47d918a9d749413d6afcc3d";
    private const long MaximumPackageSize = 100 * 1024 * 1024;
    private const long MaximumExtractedSize = 250 * 1024 * 1024;
    private const string BundledPackagePath = "downloads\\QuestCompass-1.8.0.zip";
    private const string SettingsFileName = "launcher-settings.json";
    private static readonly string[] AddonFolderNames = ["QuestCompass", "QuestCompass_Data"];
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly TextBox _gamePath = new();
    private readonly Label _status = new();
    private readonly Label _version = new();
    private readonly Button _installButton = new();
    private readonly Button _checkButton = new();
    private string _manifestUrl = string.Empty;
    private string _installedVersion = BundledVersion;
    private bool _hasInstalledRelease;
    private string? _availableVersion;
    private Uri? _availablePackageUri;
    private string? _availablePackageHash;

    public LauncherForm()
    {
        Text = "QuestCompass Launcher";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 530);
        Size = new Size(780, 590);
        BackColor = Color.FromArgb(16, 22, 20);
        ForeColor = Color.FromArgb(240, 241, 233);
        Font = new Font("Segoe UI", 9F);

        BuildInterface();
        LoadSettings();
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(32, 24, 32, 24),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 165));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill };
        var title = new Label
        {
            Text = "QuestCompass",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
            ForeColor = Color.FromArgb(198, 226, 138),
            Location = new Point(0, 0)
        };
        var subtitle = new Label
        {
            Text = "Публичный установщик аддона для WoW Forever",
            AutoSize = true,
            ForeColor = Color.FromArgb(157, 169, 160),
            Location = new Point(3, 48)
        };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        root.Controls.Add(header, 0, 0);

        var installPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            BackColor = Color.FromArgb(23, 30, 27)
        };
        var pathLabel = new Label
        {
            Text = "Папка клиента WoW Forever",
            AutoSize = true,
            ForeColor = Color.FromArgb(200, 209, 199),
            Location = new Point(18, 18)
        };
        _gamePath.SetBounds(18, 48, 520, 34);
        _gamePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _gamePath.BackColor = Color.FromArgb(13, 18, 16);
        _gamePath.ForeColor = ForeColor;
        _gamePath.BorderStyle = BorderStyle.FixedSingle;
        _gamePath.PlaceholderText = @"Например: C:\World of Warcraft\_classic_beta_";

        var browseButton = CreateButton("Обзор…", primary: false);
        browseButton.SetBounds(548, 47, 120, 36);
        browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        browseButton.Click += BrowseForGameFolder;

        _version.Text = $"В комплекте: GPL-релиз {BundledVersion}";
        _version.AutoSize = true;
        _version.ForeColor = Color.FromArgb(198, 226, 138);
        _version.Location = new Point(18, 97);

        _installButton.Text = $"Установить {BundledVersion}";
        _installButton.SetBounds(18, 124, 190, 39);
        _installButton.Click += async (_, _) => await InstallSelectedReleaseAsync();

        _checkButton.Text = "Проверить обновления";
        _checkButton.SetBounds(218, 124, 190, 39);
        _checkButton.Click += async (_, _) => await CheckForUpdatesAsync();

        installPanel.Controls.Add(pathLabel);
        installPanel.Controls.Add(_gamePath);
        installPanel.Controls.Add(browseButton);
        installPanel.Controls.Add(_version);
        installPanel.Controls.Add(_installButton);
        installPanel.Controls.Add(_checkButton);
        root.Controls.Add(installPanel, 0, 1);

        var infoPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 14, 18, 12),
            BackColor = Color.FromArgb(27, 37, 30)
        };
        _status.Text = "Выберите папку игры. Текущий аддон бесплатный и продолжит работать независимо от подписки.";
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = Color.FromArgb(207, 218, 204);
        _status.AutoEllipsis = true;
        infoPanel.Controls.Add(_status);
        root.Controls.Add(infoPanel, 0, 2);

        var footer = new Panel { Dock = DockStyle.Fill };
        var license = new Label
        {
            Text = "QuestCompass распространяется по GPLv3. Лаунчер устанавливает публичные релизы и не блокирует аддон.",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 42,
            ForeColor = Color.FromArgb(131, 145, 135)
        };
        var closeGame = new Label
        {
            Text = "Перед установкой закройте игру. Архив содержит QuestCompass и QuestCompass_Data.",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = Color.FromArgb(131, 145, 135)
        };
        footer.Controls.Add(closeGame);
        footer.Controls.Add(license);
        root.Controls.Add(footer, 0, 3);
    }

    private static Button CreateButton(string text, bool primary)
    {
        return new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(198, 226, 138) : Color.FromArgb(31, 41, 35),
            ForeColor = primary ? Color.FromArgb(23, 32, 20) : Color.FromArgb(226, 233, 224),
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
    }

    private void BrowseForGameFolder(object? sender, EventArgs e)
    {
        using var picker = new FolderBrowserDialog
        {
            Description = "Выберите папку WoW Forever или папку _classic_beta_",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_gamePath.Text) ? _gamePath.Text : string.Empty
        };

        if (picker.ShowDialog(this) == DialogResult.OK)
        {
            _gamePath.Text = picker.SelectedPath;
            SaveSettings();
            _status.Text = "Папка выбрана. Нажмите «Установить», чтобы добавить обе папки аддона.";
        }
    }

    private async Task InstallSelectedReleaseAsync()
    {
        if (_hasInstalledRelease && _availableVersion is null)
        {
            _status.Text = "Аддон уже установлен. Сначала проверьте обновления, чтобы установить более новую версию.";
            return;
        }

        if (!TryGetAddonDirectory(out var addonDirectory))
        {
            return;
        }

        var isUpdate = _availableVersion is not null
            && _availablePackageUri is not null
            && _availablePackageHash is not null;
        SetBusy(true, isUpdate
            ? $"Скачиваем и проверяем публичный релиз {_availableVersion}…"
            : "Проверяем архив и устанавливаем GPL-релиз…");
        try
        {
            var version = isUpdate ? _availableVersion! : BundledVersion;
            var package = isUpdate
                ? await DownloadPackageAsync(_availablePackageUri!)
                : await Task.Run(() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, BundledPackagePath)));

            if (isUpdate)
            {
                VerifyHash(package, _availablePackageHash!);
            }
            else
            {
                VerifyHash(package, BundledSha256);
            }

            await Task.Run(() => InstallArchive(package, addonDirectory));

            _installedVersion = version;
            _hasInstalledRelease = true;
            _availableVersion = null;
            _availablePackageUri = null;
            _availablePackageHash = null;
            _installButton.Text = $"Установлен {_installedVersion}";
            _version.Text = $"Установлен релиз {_installedVersion}";
            _status.Text = "Установка завершена. GPL-аддон останется работоспособным после окончания любых платных сервисов.";
            SaveSettings();
        }
        catch (Exception exception) when (exception is IOException
            or InvalidDataException
            or UnauthorizedAccessException
            or InvalidOperationException
            or HttpRequestException
            or TaskCanceledException)
        {
            _status.Text = $"Не удалось установить аддон: {exception.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        if (!TryGetAddonDirectory(out _))
        {
            return;
        }

        if (!Uri.TryCreate(_manifestUrl, UriKind.Absolute, out var manifestUri)
            || manifestUri.Scheme != Uri.UriSchemeHttps
            || manifestUri.Host.EndsWith(".example", StringComparison.OrdinalIgnoreCase))
        {
            _status.Text = "Лента обновлений ещё не настроена. Замените YOUR-DOMAIN.example в appsettings.json на HTTPS-адрес сайта.";
            return;
        }

        SetBusy(true, "Проверяем публичную ленту обновлений…");
        try
        {
            var json = await HttpClient.GetStringAsync(manifestUri);
            var manifest = JsonSerializer.Deserialize<ReleaseManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidDataException("Лента обновлений имеет неверный формат.");

            if (string.IsNullOrWhiteSpace(manifest.Version)
                || string.IsNullOrWhiteSpace(manifest.DownloadUrl)
                || string.IsNullOrWhiteSpace(manifest.Sha256)
                || !Version.TryParse(manifest.Version, out var availableVersion)
                || !Version.TryParse(_installedVersion, out var installedVersion))
            {
                throw new InvalidDataException("Номер версии в ленте обновлений некорректен.");
            }

            if (!Uri.TryCreate(manifestUri, manifest.DownloadUrl, out var packageUri)
                || packageUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidDataException("Ссылка на архив должна использовать HTTPS.");
            }

            if (manifest.Sha256.Length != 64 || !manifest.Sha256.All(Uri.IsHexDigit))
            {
                throw new InvalidDataException("SHA-256 архива в ленте обновлений некорректен.");
            }

            if (availableVersion <= installedVersion)
            {
                _availableVersion = null;
                _availablePackageUri = null;
                _availablePackageHash = null;
                _installButton.Text = _hasInstalledRelease
                    ? $"Установлен {_installedVersion}"
                    : $"Установить {BundledVersion}";
                _version.Text = _hasInstalledRelease
                    ? $"Установлен релиз {_installedVersion}"
                    : $"Последний публичный релиз: {manifest.Version}";
                _status.Text = _hasInstalledRelease
                    ? "Установлена актуальная публичная версия."
                    : "Публичный релиз доступен для установки.";
            }
            else
            {
                _availableVersion = manifest.Version;
                _availablePackageUri = packageUri;
                _availablePackageHash = manifest.Sha256;
                _installButton.Text = $"Обновить до {manifest.Version}";
                _version.Text = $"Доступно обновление: {manifest.Version}";
                _status.Text = "Найдена новая публичная версия. Нажмите кнопку обновления для проверки и установки.";
            }
        }
        catch (HttpRequestException exception)
        {
            _status.Text = $"Не удалось получить ленту обновлений: {exception.Message}";
        }
        catch (TaskCanceledException)
        {
            _status.Text = "Время ожидания ответа сервера обновлений истекло.";
        }
        catch (JsonException exception)
        {
            _status.Text = $"Не удалось прочитать ленту обновлений: {exception.Message}";
        }
        catch (InvalidDataException exception)
        {
            _status.Text = $"Ошибка в ленте обновлений: {exception.Message}";
        }
        catch (UnauthorizedAccessException exception)
        {
            _status.Text = $"Нет доступа к данным обновления: {exception.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private bool TryGetAddonDirectory(out string addonDirectory)
    {
        addonDirectory = string.Empty;
        var selectedPath = _gamePath.Text.Trim();
        if (!Directory.Exists(selectedPath))
        {
            _status.Text = "Укажите существующую папку WoW Forever.";
            return false;
        }

        try
        {
            addonDirectory = ResolveAddonDirectory(selectedPath);
            return true;
        }
        catch (ArgumentException exception)
        {
            _status.Text = exception.Message;
            return false;
        }
    }

    private static string ResolveAddonDirectory(string selectedPath)
    {
        var selected = Path.GetFullPath(selectedPath);
        var leaf = new DirectoryInfo(selected).Name;

        if (leaf.Equals("AddOns", StringComparison.OrdinalIgnoreCase)
            && new DirectoryInfo(selected).Parent?.Name.Equals("Interface", StringComparison.OrdinalIgnoreCase) == true)
        {
            return selected;
        }

        if (leaf.Equals("Interface", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(selected, "AddOns");
        }

        var betaClient = Path.Combine(selected, "_classic_beta_");
        if (Directory.Exists(betaClient))
        {
            return Path.Combine(betaClient, "Interface", "AddOns");
        }

        if (leaf.Equals("_classic_beta_", StringComparison.OrdinalIgnoreCase)
            || File.Exists(Path.Combine(selected, "Wow.exe"))
            || File.Exists(Path.Combine(selected, "WowT.exe"))
            || Directory.Exists(Path.Combine(selected, "Interface")))
        {
            return Path.Combine(selected, "Interface", "AddOns");
        }

        throw new ArgumentException("Выберите папку _classic_beta_, папку с WoW.exe или Interface\\AddOns.");
    }

    private static void InstallArchive(byte[] package, string addonDirectory)
    {
        var fullAddonDirectory = Path.GetFullPath(addonDirectory);
        Directory.CreateDirectory(fullAddonDirectory);

        var stagingDirectory = Path.Combine(fullAddonDirectory, $".QuestCompass-stage-{Guid.NewGuid():N}");
        var backupDirectory = Path.Combine(fullAddonDirectory, $".QuestCompass-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);
        Directory.CreateDirectory(backupDirectory);
        var installationSucceeded = false;

        try
        {
            using (var archiveStream = new MemoryStream(package, writable: false))
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read))
            {
                ExtractValidatedArchive(archive, stagingDirectory);
            }

            foreach (var folderName in AddonFolderNames)
            {
                if (!File.Exists(Path.Combine(stagingDirectory, folderName, folderName == "QuestCompass" ? "QuestCompass.toc" : "QuestCompass_Data.toc")))
                {
                    throw new InvalidDataException($"В архиве отсутствует {folderName} или его TOC-файл.");
                }
            }

            var backedUp = new List<string>();
            var installed = new List<string>();
            try
            {
                foreach (var folderName in AddonFolderNames)
                {
                    var destination = Path.GetFullPath(Path.Combine(fullAddonDirectory, folderName));
                    EnsureChildPath(fullAddonDirectory, destination);
                    if (Directory.Exists(destination))
                    {
                        Directory.Move(destination, Path.Combine(backupDirectory, folderName));
                        backedUp.Add(folderName);
                    }
                }

                foreach (var folderName in AddonFolderNames)
                {
                    var source = Path.Combine(stagingDirectory, folderName);
                    var destination = Path.GetFullPath(Path.Combine(fullAddonDirectory, folderName));
                    EnsureChildPath(fullAddonDirectory, destination);
                    Directory.Move(source, destination);
                    installed.Add(folderName);
                }

                installationSucceeded = true;
            }
            catch
            {
                foreach (var folderName in installed)
                {
                    var destination = Path.Combine(fullAddonDirectory, folderName);
                    if (Directory.Exists(destination))
                    {
                        Directory.Delete(destination, recursive: true);
                    }
                }

                foreach (var folderName in backedUp)
                {
                    var backup = Path.Combine(backupDirectory, folderName);
                    if (Directory.Exists(backup))
                    {
                        Directory.Move(backup, Path.Combine(fullAddonDirectory, folderName));
                    }
                }

                throw;
            }
        }
        finally
        {
            DeleteGeneratedDirectory(stagingDirectory);
            if (installationSucceeded || !Directory.EnumerateFileSystemEntries(backupDirectory).Any())
            {
                DeleteGeneratedDirectory(backupDirectory);
            }
        }
    }

    private static void ExtractValidatedArchive(ZipArchive archive, string stagingDirectory)
    {
        var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long extractedBytes = 0;
        foreach (var entry in archive.Entries)
        {
            var normalizedName = entry.FullName.Replace('\\', '/');
            if (normalizedName.StartsWith("/", StringComparison.Ordinal)
                || normalizedName.Contains(':')
                || normalizedName.Split('/').Any(part => part is "." or ".."))
            {
                throw new InvalidDataException("Архив содержит небезопасный путь.");
            }

            if (entry.Length > MaximumExtractedSize - extractedBytes)
            {
                throw new InvalidDataException("Размер распакованного архива превышает допустимый лимит.");
            }

            extractedBytes += entry.Length;

            var root = normalizedName.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (root is null || !AddonFolderNames.Contains(root, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("В архиве найдены файлы вне папок аддона.");
            }
            seenRoots.Add(root);

            var destination = Path.GetFullPath(Path.Combine(stagingDirectory, normalizedName.Replace('/', Path.DirectorySeparatorChar)));
            EnsureChildPath(stagingDirectory, destination);

            if (normalizedName.EndsWith("/", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(destination);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using var input = entry.Open();
            using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
        }

        if (!AddonFolderNames.All(seenRoots.Contains))
        {
            throw new InvalidDataException("Архив не содержит обе папки QuestCompass.");
        }
    }

    private static void EnsureChildPath(string rootPath, string candidatePath)
    {
        var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(candidatePath);
        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Путь установки выходит за пределы выбранной папки.");
        }
    }

    private static void VerifyHash(byte[] content, string expectedHash)
    {
        var actualHash = Convert.ToHexStringLower(SHA256.HashData(content));
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(actualHash),
                Convert.FromHexString(expectedHash)))
        {
            throw new InvalidDataException("Контрольная сумма архива не совпадает.");
        }
    }

    private static async Task<byte[]> DownloadPackageAsync(Uri packageUri)
    {
        using var response = await HttpClient.GetAsync(packageUri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is > MaximumPackageSize)
        {
            throw new InvalidDataException("Размер архива превышает допустимый лимит.");
        }

        await using var input = await response.Content.ReadAsStreamAsync();
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var bytesRead = await input.ReadAsync(buffer);
            if (bytesRead == 0)
            {
                break;
            }

            if (output.Length + bytesRead > MaximumPackageSize)
            {
                throw new InvalidDataException("Размер архива превышает допустимый лимит.");
            }

            await output.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        return output.ToArray();
    }

    private static void DeleteGeneratedDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private void LoadSettings()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        try
        {
            if (File.Exists(configPath))
            {
                using var config = JsonDocument.Parse(File.ReadAllText(configPath));
                if (config.RootElement.TryGetProperty("UpdateManifestUrl", out var url))
                {
                    _manifestUrl = url.GetString() ?? string.Empty;
                }
            }

            var settingsPath = Path.Combine(Application.LocalUserAppDataPath, SettingsFileName);
            if (File.Exists(settingsPath))
            {
                using var settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
                if (settings.RootElement.TryGetProperty("GamePath", out var path))
                {
                    var savedPath = path.GetString();
                    if (!string.IsNullOrWhiteSpace(savedPath) && Directory.Exists(savedPath))
                    {
                        _gamePath.Text = savedPath;
                    }
                }

                if (settings.RootElement.TryGetProperty("InstalledVersion", out var version))
                {
                    var savedVersion = version.GetString();
                    if (!string.IsNullOrWhiteSpace(savedVersion) && Version.TryParse(savedVersion, out _))
                    {
                        _installedVersion = savedVersion;
                        _hasInstalledRelease = true;
                        _version.Text = $"Установлен релиз {_installedVersion}";
                        _installButton.Text = $"Установлен {_installedVersion}";
                    }
                }
            }
        }
        catch (JsonException exception)
        {
            _status.Text = $"Не удалось прочитать настройки: {exception.Message}";
        }
        catch (IOException exception)
        {
            _status.Text = $"Не удалось открыть настройки: {exception.Message}";
        }
        catch (UnauthorizedAccessException exception)
        {
            _status.Text = $"Нет доступа к настройкам: {exception.Message}";
        }

        _installButton.Enabled = !_hasInstalledRelease;
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Application.LocalUserAppDataPath);
            var settingsPath = Path.Combine(Application.LocalUserAppDataPath, SettingsFileName);
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(new LauncherSettings(_gamePath.Text, _installedVersion)));
        }
        catch (IOException exception)
        {
            _status.Text = $"Не удалось сохранить папку игры: {exception.Message}";
        }
        catch (UnauthorizedAccessException exception)
        {
            _status.Text = $"Нет доступа для сохранения настроек: {exception.Message}";
        }
    }

    private void SetBusy(bool busy, string? message = null)
    {
        _installButton.Enabled = !busy && (!_hasInstalledRelease || _availableVersion is not null);
        _checkButton.Enabled = !busy;
        _status.Text = message ?? _status.Text;
        UseWaitCursor = busy;
    }

    private sealed record ReleaseManifest(string Version, string DownloadUrl, string Sha256);
    private sealed record LauncherSettings(string GamePath, string InstalledVersion);
}
