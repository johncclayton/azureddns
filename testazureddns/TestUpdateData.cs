using System;
using azureddns;
using Newtonsoft.Json;
using Xunit;

namespace testazureddns;

public class TestUpdateData
{
    [Fact]
    public void TestValidation()
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
        Assert.Equal(true, d.IsValid(out _));
    }

    [Theory]
    [InlineData(true, 3, @"{ zone: 'z', name: 'nah', resgroup: 'g', reqip: '1.2.3.4', names: ['one', 'two', 'three'] }")]
    [InlineData(true, 3, @"{ zone: 'z', resgroup: 'g', reqip: '1.2.3.4', names: ['one', 'two', 'three'] }")]    
    [InlineData(false, 3, @"{ resgroup: 'g', reqip: '1.2.3.4', names: ['one', 'two', 'three'] }")]
    [InlineData(false, 0, @"{ resgroup: 'g', reqip: '1.2.3.4' }")]
    public void TestCanDeserializeMultipleNames(bool expectValid, int numNames, string json_data)
    {
        var data = JsonConvert.DeserializeObject<UpdateMultipleNames>(json_data);
        if (numNames > 0)
        {
            Assert.Equal(3, data.names.Length);
            Assert.Equal("one", data.names[0]);
            Assert.Equal("two", data.names[1]);
            Assert.Equal("three", data.names[2]);
        }

        Assert.Equal(expectValid, data.IsValid(out _));
    }
   
}