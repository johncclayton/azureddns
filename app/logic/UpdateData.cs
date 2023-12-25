using System;

namespace AzureAppFunc.logic;

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
            msg = new string("no 'zone', specify the DNS zone - it must exist already");
            return false;
        }

        if (string.IsNullOrEmpty(reqip))
        {
            msg = new string("no IP address set");
            return false;
        }

        msg = String.Empty;

        return true;
    }
}