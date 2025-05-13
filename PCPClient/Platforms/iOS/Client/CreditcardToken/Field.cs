namespace Com.Payone.PcpClientSdk.iOS;

public class Field
{
    public string Selector    { get; }
    public string Style       { get; }
    public string Type        { get; }
    public string Size        { get; }
    public string MaxLength   { get; }
    public Dictionary<string,int> Length { get; }
    public Dictionary<string,string> Iframe { get; }

    public Field(string selector, string style, string type,
        string size, string maxlength,
        Dictionary<string,int> length,
        Dictionary<string,string> iframe)
    {
        Selector  = selector;
        Style     = style;
        Type      = type;
        Size      = size;
        MaxLength = maxlength;
        Length    = length ?? new Dictionary<string,int>();
        Iframe    = iframe ?? new Dictionary<string,string>();
    }
}