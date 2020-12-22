namespace Sussex.Lhcra.Roci.Viewer.UI.Helpers
{
    public interface IIpAddressProvider
    {
        string GetClientIpAddress();

        string GetHostIpAddress();
    }
}