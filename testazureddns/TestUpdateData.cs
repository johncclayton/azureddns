using azureddns;
using Xunit;

namespace testazureddns;

public class TestUpdateData
{
    [Fact]
    public void TestValid()
    {
        UpdateData d = new UpdateData();
        Assert.False(d.IsValid(out string msg));
        Assert.Contains("no 'name' value", msg);

        d.name = "me";
        Assert.False(d.IsValid(out string msg1));
        Assert.Contains("no 'group',", msg1);
        
        d.resgroup = "them";
        Assert.False(d.IsValid(out string msg2));
        Assert.Contains("no 'zone',", msg2);
        
        d.zone = "there";
        Assert.False(d.IsValid(out string msg3));
        Assert.Contains("despite", msg3);

        d.reqip = "1.2.3.4";
        Assert.True(d.IsValid(out string msg4));
    }
    
    [Fact]
    public void TestCanSerializeWithNullBody()
    {
        update.GetUpdateDataFromRequest("z", "n", "g", "ip");
    }
    
    [Fact]
    public void TestCanSerializeWithEmptyBody()
    {
        update.GetUpdateDataFromRequest("z", "n", "g", "ip", "");
    }
    
    [Fact]
    public void TestCanSerializeWithSenselessBody()
    {
        update.GetUpdateDataFromRequest("z", "n", "g", "ip", "stupid");
    }

    [Fact]
    public void TestCanSerializeAndOverrideNameInBody()
    {
        UpdateData d = update.GetUpdateDataFromRequest("z", null, "g", "ip", "{name: 'something'}");
        Assert.Equal("something", d.name);
    }


}