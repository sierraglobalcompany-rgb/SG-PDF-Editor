using System.Reflection;
using System.Windows.Controls;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowProtectionUtilitiesTests
{
    [Fact]
    public void Herramientas_ProtectionCommand_IsPresentAndDisabledWithoutPdf()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var window = new MainWindow();
            OrganizeWindowTestHost.EnsureLoaded(window);
            try
            {
                var protection = OrganizeWindowTestHost.Element<MenuItem>(window, "ProtectionMenuItem");
                Assert.Equal("_Proteger con contraseña...", protection.Header);
                Assert.False(protection.IsEnabled);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void Herramientas_ProtectionCommand_EnablesAfterOpeningPdf()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                Assert.True(OrganizeWindowTestHost.Element<MenuItem>(window, "ProtectionMenuItem").IsEnabled);
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void ProtectionRequest_RejectsPasswordMismatchAndMissingAuthorization()
    {
        var mismatch = CreateRequest("new-secret", "different", null, true);
        Assert.False(TryBuild(mismatch, sourceOpenedWithPassword: false, out _, out var mismatchError));
        Assert.Contains("coincid", mismatchError, StringComparison.OrdinalIgnoreCase);

        var unauthorized = CreateRequest("new-secret", "new-secret", null, false);
        Assert.False(TryBuild(unauthorized, sourceOpenedWithPassword: false, out _, out var authorizationError));
        Assert.Contains("autoriz", authorizationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProtectionRequest_ProtectedSourceRequiresCurrentPassword()
    {
        var request = CreateRequest("new-secret", "new-secret", null, true);

        Assert.False(TryBuild(request, sourceOpenedWithPassword: true, out _, out var error));
        Assert.Contains("actual", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProtectionCommand_SaveAsCancellation_IsNoOp()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var original = File.ReadAllBytes(fixture.Path);
                var request = CreateRequest("new-secret", "new-secret", null, true);

                Assert.False(await InvokeProtectionAsync(window, request, null));
                Assert.Equal(original, File.ReadAllBytes(fixture.Path));
                Assert.Contains("cancel", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(ReadBoolField(window, "_utilityExecuting"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    [Fact]
    public void ProtectionCommand_SaveAsCopy_ProtectsAndReopens()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(2);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var original = File.ReadAllBytes(fixture.Path);
                var destination = Path.Combine(fixture.DirectoryPath, "ui-protected.pdf");
                var request = CreateRequest("new-secret", "new-secret", null, true);

                Assert.True(await InvokeProtectionAsync(window, request, destination));
                Assert.Equal(original, File.ReadAllBytes(fixture.Path));
                var noPassword = Assert.Throws<PdfDocumentOpenException>(() => PdfDocumentSession.Open(destination));
                Assert.Equal(PdfDocumentOpenError.PasswordRequiredOrIncorrect, noPassword.Error);
                using var output = PdfDocumentSession.Open(destination, "new-secret");
                Assert.Equal(2, output.PageCount);
                Assert.Contains("protegida", OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text, StringComparison.OrdinalIgnoreCase);
                Assert.False(ReadBoolField(window, "_utilityExecuting"));
            }
            finally { OrganizeWindowTestHost.CloseClean(window); }
        });
    }

    private static object CreateRequest(
        string openPassword,
        string confirmOpenPassword,
        string? sourcePassword,
        bool authorizationConfirmed)
    {
        var type = typeof(MainWindow).Assembly.GetType("SGPdf.App.Features.Utilities.PdfProtectionRequest");
        Assert.NotNull(type);
        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(string), typeof(string), typeof(string), typeof(bool)],
            modifiers: null);
        Assert.NotNull(constructor);
        return constructor.Invoke([openPassword, confirmOpenPassword, sourcePassword, authorizationConfirmed]);
    }

    private static bool TryBuild(
        object request,
        bool sourceOpenedWithPassword,
        out object? settings,
        out string error)
    {
        var method = request.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == "TryBuild" && candidate.GetParameters().Length == 3);
        var arguments = new object?[] { sourceOpenedWithPassword, null, null };
        var succeeded = Assert.IsType<bool>(method.Invoke(request, arguments));
        settings = arguments[1];
        error = arguments[2] as string ?? string.Empty;
        return succeeded;
    }

    private static async Task<bool> InvokeProtectionAsync(MainWindow window, object request, string? destinationPath)
    {
        var method = typeof(MainWindow).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(candidate => candidate.Name == "TryRunProtectionUtilityAsync" && candidate.GetParameters().Length == 2);
        Assert.NotNull(method);
        var task = Assert.IsAssignableFrom<Task>(method.Invoke(window, [request, destinationPath]));
        await task;
        var result = task.GetType().GetProperty("Result")?.GetValue(task);
        return Assert.IsType<bool>(result);
    }

    private static bool ReadBoolField(MainWindow window, string name)
        => Assert.IsType<bool>(OrganizeWindowTestHost.GetField(window, name));
}
