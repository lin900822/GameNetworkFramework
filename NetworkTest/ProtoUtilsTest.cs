using Common;
using NUnit.Framework;

namespace NetworkTest;

[TestFixture]
public class ProtoUtilsTest
{
    [Test]
    public void EncodeDecode_GiveData_ReturnSameData()
    {
        // Arrange
        var hello = new Hello();
        hello.Content = "123";
        
        // Act
        var data = ProtoUtils.Encode(hello);
        
        // Assert
        if (!ProtoUtils.TryDecode<Hello>(data, out var hello2)) return;
        Assert.That(hello2.Content, Is.EqualTo(hello.Content));
    }
}