namespace Sussex.Lhcra.Plexus.Viewer.Domain.Interfaces
{
    public interface IIpAddressProvider
    {
        string GetClientIpAddress();

        string GetHostIpAddress();
    }
}