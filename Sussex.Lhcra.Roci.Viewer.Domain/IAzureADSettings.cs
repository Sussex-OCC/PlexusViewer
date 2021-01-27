namespace Sussex.Lhcra.Roci.Viewer.Domain
{
    public interface IAzureADSettings
    {
        string[] UserScope { get; set; }
        string SystemToSystemScope { get; set; }
    }
}
