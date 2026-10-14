using System.Reflection;
using SGPdf.App.Pdf;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class PdfBatchContractTests
{
    [Fact]
    public void MinimalBatchContract_ExistsWithoutSpeculativeArchitecture()
    {
        Assembly appAssembly = typeof(PdfDocumentSession).Assembly;

        Type? operationType = appAssembly.GetType(
            "SGPdf.App.Features.Batch.PdfBatchOperation",
            throwOnError: false);
        Assert.NotNull(operationType);
        Assert.True(operationType.IsEnum);
        Assert.Equal(["TextWatermark"], Enum.GetNames(operationType));

        Type? requestType = appAssembly.GetType(
            "SGPdf.App.Features.Batch.PdfBatchRequest",
            throwOnError: false);
        Assert.NotNull(requestType);
        AssertProperty(requestType, "InputPaths");
        AssertProperty(requestType, "OutputDirectory", typeof(string));
        AssertProperty(requestType, "Operation", operationType);

        Type? resultType = appAssembly.GetType(
            "SGPdf.App.Features.Batch.PdfBatchItemResult",
            throwOnError: false);
        Assert.NotNull(resultType);
        AssertProperty(resultType, "InputPath", typeof(string));
        AssertProperty(resultType, "OutputPath", typeof(string));
        AssertProperty(resultType, "Success", typeof(bool));
        AssertProperty(resultType, "ErrorMessage", typeof(string));
    }

    private static void AssertProperty(Type owner, string name, Type? expectedType = null)
    {
        PropertyInfo? property = owner.GetProperty(name);
        Assert.NotNull(property);
        if (expectedType is not null)
            Assert.Equal(expectedType, property.PropertyType);
    }
}
