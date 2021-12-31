namespace Sussex.Lhcra.Roci.Viewer.Domain.Interfaces
{
    public interface IIpAddressProvider
    {
        string GetClientIpAddress();

        string GetHostIpAddress();
    }
}