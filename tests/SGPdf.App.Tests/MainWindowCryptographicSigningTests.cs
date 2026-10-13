using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class MainWindowCryptographicSigningTests
{
    [Fact]
    public void CryptographicSigningCommand_IsSingleFocusedCommand_DisabledWithoutPdf_AndNoOps()
    {
        OrganizeWindowTestHost.RunInSta(() =>
        {
            var window = new MainWindow();
            OrganizeWindowTestHost.EnsureLoaded(window);
            try
            {
                var command = OrganizeWindowTestHost.Element<MenuItem>(window, "CryptographicSignMenuItem");
                var status = OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText");
                string statusBefore = status.Text;

                Assert.Equal("Firmar digitalmente", command.Header);
                Assert.False(command.IsEnabled);

                OrganizeWindowTestHost.Invoke(
                    window,
                    "CryptographicSign_Click",
                    command,
                    new RoutedEventArgs(MenuItem.ClickEvent, command));

                Assert.Equal(statusBefore, status.Text);

                var center = Assert.IsType<Grid>(OrganizeWindowTestHost.Element<ScrollViewer>(window, "PdfScrollViewer").Parent);
                var root = Assert.IsType<Grid>(center.Parent);
                Assert.Equal(3, root.ColumnDefinitions.Count);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void CryptographicSigningCommand_DisablesDuringBusyOrIncompatibleModes()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var command = OrganizeWindowTestHost.Element<MenuItem>(window, "CryptographicSignMenuItem");

                OrganizeWindowTestHost.Invoke(window, "UpdateCryptographicSigningCommandAvailability");
                Assert.True(command.IsEnabled);

                foreach (var field in new[] { "_isBusy", "_editModeActive", "_organizeModeActive", "_signatureModeActive" })
                {
                    OrganizeWindowTestHost.SetField(window, field, true);
                    OrganizeWindowTestHost.Invoke(window, "UpdateCryptographicSigningCommandAvailability");
                    Assert.False(command.IsEnabled);
                    OrganizeWindowTestHost.SetField(window, field, false);
                }

                OrganizeWindowTestHost.Invoke(window, "UpdateCryptographicSigningCommandAvailability");
                Assert.True(command.IsEnabled);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void SuccessfulSigning_UsesPfxPasswordCertificatePreviewAndDestination_AndKeepsCurrentSession()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var destination = System.IO.Path.Combine(fixture.DirectoryPath, "signed.pdf");
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var sessionBefore = Assert.IsType<PdfDocumentSession>(OrganizeWindowTestHost.GetField(window, "_session"));
                var status = OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText");
                var serviceCalls = new List<(string Source, string Destination, string Pfx, string Password)>();
                var previewCalls = new List<(string Subject, string Issuer)>();

                OrganizeWindowTestHost.SetField(window, "_selectCryptographicPfx", (Func<string?>)(() => "certificate.pfx"));
                OrganizeWindowTestHost.SetField(window, "_requestCryptographicPfxPassword", (Func<Window, string?>)(_ => "secret"));
                OrganizeWindowTestHost.SetField(window, "_inspectCryptographicCertificate",
                    (Func<string, string, (string Subject, string Issuer, DateTime NotBefore, DateTime NotAfter)>)((path, password) =>
                    {
                        Assert.Equal("certificate.pfx", path);
                        Assert.Equal("secret", password);
                        return ("CN=Signer", "CN=Issuer", new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));
                    }));
                OrganizeWindowTestHost.SetField(window, "_confirmCryptographicCertificate",
                    (Func<string, string, DateTime, DateTime, bool>)((subject, issuer, _, _) =>
                    {
                        previewCalls.Add((subject, issuer));
                        return true;
                    }));
                OrganizeWindowTestHost.SetField(window, "_selectCryptographicDestination", (Func<string?>)(() => destination));
                OrganizeWindowTestHost.SetField(window, "_signCryptographicPdf",
                    (Action<string, string, string, string, CancellationToken>)((source, output, pfx, password, _) =>
                        serviceCalls.Add((source, output, pfx, password))));

                OrganizeWindowTestHost.Invoke(window, "CryptographicSign_Click",
                    OrganizeWindowTestHost.Element<MenuItem>(window, "CryptographicSignMenuItem"),
                    new RoutedEventArgs());

                Assert.Single(previewCalls);
                Assert.Equal(("CN=Signer", "CN=Issuer"), previewCalls[0]);
                Assert.Single(serviceCalls);
                Assert.Equal(fixture.Path, serviceCalls[0].Source);
                Assert.Equal(destination, serviceCalls[0].Destination);
                Assert.Equal("certificate.pfx", serviceCalls[0].Pfx);
                Assert.Equal("secret", serviceCalls[0].Password);
                Assert.Same(sessionBefore, OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Equal("Firma criptográfica creada y validada localmente.", status.Text);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void SigningCancellation_LeavesCurrentSessionUnchanged_AndShowsConservativeStatus()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var sessionBefore = OrganizeWindowTestHost.GetField(window, "_session");
                var serviceCalled = false;
                OrganizeWindowTestHost.SetField(window, "_selectCryptographicPfx", (Func<string?>)(() => null));
                OrganizeWindowTestHost.SetField(window, "_signCryptographicPdf",
                    (Action<string, string, string, string, CancellationToken>)((_, _, _, _, _) => serviceCalled = true));

                OrganizeWindowTestHost.Invoke(window, "CryptographicSign_Click",
                    OrganizeWindowTestHost.Element<MenuItem>(window, "CryptographicSignMenuItem"),
                    new RoutedEventArgs());

                Assert.False(serviceCalled);
                Assert.Same(sessionBefore, OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Equal(
                    "Firma criptográfica cancelada. No se modificó el PDF actual.",
                    OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void InvalidPfxPassword_LeavesCurrentSessionUnchanged_AndDoesNotCallService()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var sessionBefore = OrganizeWindowTestHost.GetField(window, "_session");
                var serviceCalled = false;
                OrganizeWindowTestHost.SetField(window, "_selectCryptographicPfx", (Func<string?>)(() => "certificate.pfx"));
                OrganizeWindowTestHost.SetField(window, "_requestCryptographicPfxPassword", (Func<Window, string?>)(_ => "wrong"));
                OrganizeWindowTestHost.SetField(window, "_inspectCryptographicCertificate",
                    (Func<string, string, (string Subject, string Issuer, DateTime NotBefore, DateTime NotAfter)>)((_, _) =>
                        throw new CryptographicException("bad password")));
                OrganizeWindowTestHost.SetField(window, "_signCryptographicPdf",
                    (Action<string, string, string, string, CancellationToken>)((_, _, _, _, _) => serviceCalled = true));

                OrganizeWindowTestHost.Invoke(window, "CryptographicSign_Click",
                    OrganizeWindowTestHost.Element<MenuItem>(window, "CryptographicSignMenuItem"),
                    new RoutedEventArgs());

                Assert.False(serviceCalled);
                Assert.Same(sessionBefore, OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Equal(
                    "No se pudo preparar la firma criptográfica. No se modificó el PDF actual.",
                    OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }

    [Fact]
    public void SigningServiceFailure_LeavesCurrentSessionUnchanged_AndShowsConservativeStatus()
    {
        OrganizeWindowTestHost.RunInSta(async () =>
        {
            using var fixture = OrganizeWindowPdfFixture.Create(1);
            var destination = System.IO.Path.Combine(fixture.DirectoryPath, "signed.pdf");
            var window = new MainWindow();
            try
            {
                Assert.True(await OrganizeWindowTestHost.OpenReaderAsync(window, fixture.Path));
                var sessionBefore = OrganizeWindowTestHost.GetField(window, "_session");
                OrganizeWindowTestHost.SetField(window, "_selectCryptographicPfx", (Func<string?>)(() => "certificate.pfx"));
                OrganizeWindowTestHost.SetField(window, "_requestCryptographicPfxPassword", (Func<Window, string?>)(_ => "secret"));
                OrganizeWindowTestHost.SetField(window, "_inspectCryptographicCertificate",
                    (Func<string, string, (string Subject, string Issuer, DateTime NotBefore, DateTime NotAfter)>)((_, _) =>
                        ("CN=Signer", "CN=Issuer", new DateTime(2026, 1, 1), new DateTime(2027, 1, 1))));
                OrganizeWindowTestHost.SetField(window, "_confirmCryptographicCertificate",
                    (Func<string, string, DateTime, DateTime, bool>)((_, _, _, _) => true));
                OrganizeWindowTestHost.SetField(window, "_selectCryptographicDestination", (Func<string?>)(() => destination));
                OrganizeWindowTestHost.SetField(window, "_signCryptographicPdf",
                    (Action<string, string, string, string, CancellationToken>)((_, _, _, _, _) =>
                        throw new IOException("validation failed")));

                OrganizeWindowTestHost.Invoke(window, "CryptographicSign_Click",
                    OrganizeWindowTestHost.Element<MenuItem>(window, "CryptographicSignMenuItem"),
                    new RoutedEventArgs());

                Assert.Same(sessionBefore, OrganizeWindowTestHost.GetField(window, "_session"));
                Assert.Equal(
                    "No se pudo crear la firma criptográfica. No se modificó el PDF actual.",
                    OrganizeWindowTestHost.Element<TextBlock>(window, "StatusText").Text);
            }
            finally
            {
                OrganizeWindowTestHost.CloseClean(window);
            }
        });
    }
}
