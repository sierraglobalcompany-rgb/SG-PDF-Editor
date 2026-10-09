using System.Reflection;
using System.Runtime.InteropServices;
using SGPdf.App.Pdf;

namespace SGPdf.App.Tests;

internal readonly record struct NativeImageMatrix(double A, double B, double C, double D, double E, double F);
internal readonly record struct NativeQuad(
    double X1, double Y1,
    double X2, double Y2,
    double X3, double Y3,
    double X4, double Y4);
internal readonly record struct NativePageObject(int Index, int Type, IntPtr Handle);

internal static class ImageEditNativeCharacterizationHarness
{
    private const int ImageObjectType = 3;
    private static readonly Assembly AppAssembly = typeof(PdfDocumentSession).Assembly;
    private static readonly Type NativeType = AppAssembly.GetType("SGPdf.App.Pdf.PdfiumNative")
        ?? throw new InvalidOperationException("PdfiumNative not found.");
    private static readonly Type RuntimeType = AppAssembly.GetType("SGPdf.App.Pdf.PdfiumRuntime")
        ?? throw new InvalidOperationException("PdfiumRuntime not found.");
    private static readonly Type MatrixType = NativeType.GetNestedType("Matrix", BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("PdfiumNative.Matrix not found.");
    private static readonly Type QuadType = NativeType.GetNestedType("QuadPointsF", BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("PdfiumNative.QuadPointsF not found.");

    internal static void MutateAndSave(
        string sourcePath,
        string destinationPath,
        Action<NativeMutationContext> mutation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentNullException.ThrowIfNull(mutation);

        EnsureRuntimeInitialized();
        var gate = GetNativeGate();
        gate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        try
        {
            document = Invoke<IntPtr>("FPDF_LoadDocument", sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not open characterization source.");

            page = Invoke<IntPtr>("FPDF_LoadPage", document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load characterization page.");

            var context = new NativeMutationContext(document, page);
            mutation(context);

            if (Invoke<int>("FPDFPage_GenerateContent", page) == 0)
                throw new InvalidOperationException("PDFium failed to regenerate characterization page content.");

            SaveDocument(document, destinationPath);
        }
        finally
        {
            if (page != IntPtr.Zero)
                Invoke<object?>("FPDF_ClosePage", page);
            if (document != IntPtr.Zero)
                Invoke<object?>("FPDF_CloseDocument", document);
            gate.Release();
        }
    }

    internal static T Inspect<T>(string sourcePath, Func<NativeMutationContext, T> inspect)
    {
        ArgumentNullException.ThrowIfNull(inspect);
        EnsureRuntimeInitialized();
        var gate = GetNativeGate();
        gate.Wait();
        IntPtr document = IntPtr.Zero;
        IntPtr page = IntPtr.Zero;
        try
        {
            document = Invoke<IntPtr>("FPDF_LoadDocument", sourcePath, null);
            if (document == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not open characterization source.");
            page = Invoke<IntPtr>("FPDF_LoadPage", document, 0);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException("PDFium could not load characterization page.");
            return inspect(new NativeMutationContext(document, page));
        }
        finally
        {
            if (page != IntPtr.Zero)
                Invoke<object?>("FPDF_ClosePage", page);
            if (document != IntPtr.Zero)
                Invoke<object?>("FPDF_CloseDocument", document);
            gate.Release();
        }
    }

    internal sealed class NativeMutationContext
    {
        internal NativeMutationContext(IntPtr document, IntPtr page)
        {
            Document = document;
            Page = page;
        }

        internal IntPtr Document { get; }
        internal IntPtr Page { get; }

        internal IReadOnlyList<NativePageObject> GetObjects()
        {
            var count = Invoke<int>("FPDFPage_CountObjects", Page);
            if (count < 0)
                throw new InvalidOperationException("Invalid page-object count.");

            var result = new List<NativePageObject>(count);
            for (var index = 0; index < count; index++)
            {
                var handle = Invoke<IntPtr>("FPDFPage_GetObject", Page, index);
                if (handle == IntPtr.Zero)
                    throw new InvalidOperationException($"Could not resolve page object {index}.");
                result.Add(new NativePageObject(index, Invoke<int>("FPDFPageObj_GetType", handle), handle));
            }
            return result;
        }

        internal NativePageObject GetFirstImage()
            => GetObjects().First(item => item.Type == ImageObjectType);

        internal NativeImageMatrix GetMatrix(IntPtr pageObject)
        {
            object?[] args = { pageObject, null };
            if (InvokeWithArgs<int>("FPDFPageObj_GetMatrix", args) == 0)
                throw new InvalidOperationException("Could not read image matrix.");
            var value = args[1] ?? throw new InvalidOperationException("Matrix was not returned.");
            return new NativeImageMatrix(
                ReadFloat(value, "A"), ReadFloat(value, "B"), ReadFloat(value, "C"),
                ReadFloat(value, "D"), ReadFloat(value, "E"), ReadFloat(value, "F"));
        }

        internal void SetMatrix(IntPtr pageObject, NativeImageMatrix matrix)
        {
            var native = Activator.CreateInstance(MatrixType)
                ?? throw new InvalidOperationException("Could not create matrix.");
            SetFloat(native, "A", matrix.A);
            SetFloat(native, "B", matrix.B);
            SetFloat(native, "C", matrix.C);
            SetFloat(native, "D", matrix.D);
            SetFloat(native, "E", matrix.E);
            SetFloat(native, "F", matrix.F);
            object?[] args = { pageObject, native };
            if (InvokeWithArgs<int>("FPDFPageObj_SetMatrix", args) == 0)
                throw new InvalidOperationException("Could not set image matrix.");
        }

        internal void SetOpacity(IntPtr pageObject, byte alpha)
        {
            if (Invoke<int>("FPDFPageObj_SetFillColor", pageObject, 255u, 255u, 255u, (uint)alpha) == 0)
                throw new InvalidOperationException("Could not set image opacity.");
        }

        internal void MoveObjectToIndex(IntPtr pageObject, int targetIndex)
        {
            if (Invoke<int>("FPDFPage_RemoveObject", Page, pageObject) == 0)
                throw new InvalidOperationException("Could not remove page object for reordering.");

            var inserted = false;
            try
            {
                if (Invoke<int>("FPDFPage_InsertObjectAtIndex", Page, pageObject, (nuint)targetIndex) == 0)
                    throw new InvalidOperationException("Could not insert page object at requested index.");
                inserted = true;
            }
            finally
            {
                if (!inserted)
                    Invoke<object?>("FPDFPageObj_Destroy", pageObject);
            }
        }

        internal NativeQuad GetRotatedBounds(IntPtr pageObject)
        {
            object?[] args = { pageObject, null };
            if (InvokeWithArgs<int>("FPDFPageObj_GetRotatedBounds", args) == 0)
                throw new InvalidOperationException("Could not read rotated bounds.");
            var value = args[1] ?? throw new InvalidOperationException("Rotated bounds were not returned.");
            return new NativeQuad(
                ReadFloat(value, "X1"), ReadFloat(value, "Y1"),
                ReadFloat(value, "X2"), ReadFloat(value, "Y2"),
                ReadFloat(value, "X3"), ReadFloat(value, "Y3"),
                ReadFloat(value, "X4"), ReadFloat(value, "Y4"));
        }

        internal (int Width, int Height) GetRenderedBitmapSize(IntPtr imageObject)
        {
            var bitmap = Invoke<IntPtr>("FPDFImageObj_GetRenderedBitmap", Document, Page, imageObject);
            if (bitmap == IntPtr.Zero)
                throw new InvalidOperationException("Rendered image bitmap route returned null.");
            try
            {
                return (
                    Invoke<int>("FPDFBitmap_GetWidth", bitmap),
                    Invoke<int>("FPDFBitmap_GetHeight", bitmap));
            }
            finally
            {
                Invoke<object?>("FPDFBitmap_Destroy", bitmap);
            }
        }
    }

    private static void EnsureRuntimeInitialized()
    {
        RuntimeType.GetMethod("EnsureInitialized", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, null);
    }

    private static SemaphoreSlim GetNativeGate()
        => (SemaphoreSlim)(RuntimeType.GetField("NativeGate", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null) ?? throw new InvalidOperationException("NativeGate not found."));

    private static MethodInfo Method(string name)
        => NativeType.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(NativeType.FullName, name);

    private static T Invoke<T>(string name, params object?[] args)
    {
        var result = Method(name).Invoke(null, args);
        if (typeof(T) == typeof(object) || typeof(T) == typeof(object?))
            return (T)(object?)result!;
        return result is null ? default! : (T)result;
    }

    private static T InvokeWithArgs<T>(string name, object?[] args)
    {
        var result = Method(name).Invoke(null, args);
        return result is null ? default! : (T)result;
    }

    private static double ReadFloat(object value, string field)
        => Convert.ToDouble(value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value));

    private static void SetFloat(object value, string field, double number)
        => value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(value, (float)number);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileWrite
    {
        internal int Version;
        internal IntPtr WriteBlock;
        internal IntPtr Context;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int WriteBlockDelegate(IntPtr self, IntPtr data, uint size);

    private sealed class FileWriteContext
    {
        internal required FileStream Stream { get; init; }
        internal Exception? Error { get; set; }
    }

    private static void SaveDocument(IntPtr document, string path)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        var context = new FileWriteContext { Stream = stream };
        var contextHandle = GCHandle.Alloc(context);
        IntPtr nativeWrite = IntPtr.Zero;
        WriteBlockDelegate callback = WriteBlock;
        try
        {
            var fileWrite = new FileWrite
            {
                Version = 1,
                WriteBlock = Marshal.GetFunctionPointerForDelegate(callback),
                Context = GCHandle.ToIntPtr(contextHandle)
            };
            nativeWrite = Marshal.AllocHGlobal(Marshal.SizeOf<FileWrite>());
            Marshal.StructureToPtr(fileWrite, nativeWrite, false);
            if (Invoke<int>("FPDF_SaveAsCopy", document, nativeWrite, 0u) == 0)
                throw context.Error ?? new InvalidOperationException("PDFium characterization save failed.");
            if (context.Error is not null)
                throw context.Error;
            stream.Flush(flushToDisk: true);
        }
        finally
        {
            GC.KeepAlive(callback);
            if (nativeWrite != IntPtr.Zero)
                Marshal.FreeHGlobal(nativeWrite);
            if (contextHandle.IsAllocated)
                contextHandle.Free();
        }
    }

    private static int WriteBlock(IntPtr self, IntPtr data, uint size)
    {
        try
        {
            var fileWrite = Marshal.PtrToStructure<FileWrite>(self);
            var handle = GCHandle.FromIntPtr(fileWrite.Context);
            if (handle.Target is not FileWriteContext context || size > int.MaxValue)
                return 0;
            var buffer = new byte[(int)size];
            Marshal.Copy(data, buffer, 0, buffer.Length);
            context.Stream.Write(buffer, 0, buffer.Length);
            return 1;
        }
        catch (Exception ex)
        {
            try
            {
                var fileWrite = Marshal.PtrToStructure<FileWrite>(self);
                var handle = GCHandle.FromIntPtr(fileWrite.Context);
                if (handle.Target is FileWriteContext context)
                    context.Error = ex;
            }
            catch
            {
            }
            return 0;
        }
    }
}
