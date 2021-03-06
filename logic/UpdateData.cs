namespace azureddns;

/// <summary>
/// Summary description for Class1
/// </summary>
/// 
public class UpdateData
{
    public string zone, name, resgroup, reqip;

    public virtual bool IsValid(out string msg)
    {
        if (string.IsNullOrEmpty(name))
        {
            msg = new string("no 'name' value, use this to specify the hostname");
            return false;
        }

        if (string.IsNullOrEmpty(resgroup))
        {
            msg = new string("no 'group', please specify a resource group where the zone exists");
            return false;
        }

        if (string.IsNullOrEmpty(zone))
        {
            msg = new string("no 'zone', specify the DNS zone - is must exist already");
            return false;
        }

        if (string.IsNullOrEmpty(reqip))
        {
            msg = new string("despite assumptions - you still need to supply the IP address to set");
            return false;
        }

        msg = null;

        return true;
    }
}