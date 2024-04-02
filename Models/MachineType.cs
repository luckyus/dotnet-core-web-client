namespace dotnet_core_web_client.Models
{
	public enum MachineType
	{
		iGuard = 1,
		iGuardExpress = 2,
		iGuard530 = 3,
		iGuardExpress540 = 4,   // this one uses CM3+ and websocket to connect (201112)
		iGuard540 = 5,          // ditto (221024)
		unknown = 99
	}
}
