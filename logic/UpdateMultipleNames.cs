namespace azureddns;

public class UpdateMultipleNames : UpdateData
{
    public override bool IsValid(out string msg)
    {
        if (string.IsNullOrEmpty(name) && (names == null || names.Length == 0))
        {
            msg = "the names array must be populated with *something* if name is null/blank";
            return false;
        }

        if (string.IsNullOrEmpty(name) && names.Length > 0)
            name = names[0];
        
        return base.IsValid(out msg);
    }

    public string[] names { get; set; }
}