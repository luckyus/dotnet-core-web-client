using System.Threading.Tasks;

namespace dotnet_core_web_client.Services
{
	public interface IClientWebSocketHandler
	{
		Task CloseAsync(bool isToReconnect = false);
		Task Initialize();
		Task ReceiveAsync();
		Task SendAsync(string jsonStr);
	}
}