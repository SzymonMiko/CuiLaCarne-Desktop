using QuiLaCarne.Data;
using Xunit;

namespace QuiLaCarne.Tests.Data;

public sealed class DpapiStringConverterTests
{
    [Fact]
    public void Converter_RoundTripsProtectedString()
    {
        var converter = new DpapiStringConverter();
        var toProvider = converter.ConvertToProviderExpression.Compile();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();

        var encrypted = toProvider("secret-value");
        var decrypted = fromProvider(encrypted);

        Assert.NotEqual("secret-value", encrypted);
        Assert.Equal("secret-value", decrypted);
    }

    [Fact]
    public void Converter_LeavesEmptyStringEmpty()
    {
        var converter = new DpapiStringConverter();
        var toProvider = converter.ConvertToProviderExpression.Compile();
        var fromProvider = converter.ConvertFromProviderExpression.Compile();

        Assert.Equal("", toProvider(""));
        Assert.Equal("", fromProvider(""));
    }
}
