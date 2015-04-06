using System.IO;
using System.Text;

namespace SocketHelpers.Messaging
{
    public interface IMessage
	{
		string FromGuid { get; set; }
	}
}
