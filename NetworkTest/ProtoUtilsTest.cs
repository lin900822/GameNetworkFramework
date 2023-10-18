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
        var hello2 = ProtoUtils.Decode<Hello>(data);
        Assert.That(hello2.Content, Is.EqualTo(hello.Content));
    }
}