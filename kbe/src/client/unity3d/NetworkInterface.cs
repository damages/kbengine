namespace KBEngine
{
  	using UnityEngine; 
	using System; 
	using System.Net.Sockets; 
	using System.Net; 
	using System.Collections; 
	using System.Collections.Generic;
	using System.Text;
	
	using MessageID = System.UInt16;
	using MessageLength = System.UInt16;
	
    public class NetworkInterface 
    {
    	public const int TCP_PACKET_MAX = 1460;
    	
        private Socket socket_ = null;
        private KBEngineApp app_ = null;
		private List<MemoryStream> packets_ = null;
		private MessageReader msgReader = new MessageReader();
		
        public NetworkInterface(KBEngineApp app)
        {
        	this.app_ = app;
			bindMessage();
        	packets_ = new List<MemoryStream>();
        }
		
		public bool valid()
		{
			return ((socket_ != null) && (socket_.Connected == true));
		}
		
		public void bindMessage()
		{
			if(Message.messages.Count == 0)
			{
				Message.messages["Loginapp_importClientMessages"] = new Message(5, "importClientMessages", 0, new List<Byte>(), null);
				Message.messages["Baseapp_importClientMessages"] = new Message(207, "importClientMessages", 0, new List<Byte>(), null);
				Message.messages["Baseapp_importClientEntityDef"] = new Message(208, "importClientMessages", 0, new List<Byte>(), null);
				
				Message.messages["Client_onImportClientMessages"] = new Message(518, "Client_onImportClientMessages", -1, new List<Byte>(), 
					this.app_.GetType().GetMethod("Client_onImportClientMessages"));
				Message.clientMessages[Message.messages["Client_onImportClientMessages"].id] = Message.messages["Client_onImportClientMessages"];
			}
		}
		
		public bool connect(string ip, int port) 
		{
			if(socket_ != null && socket_.Connected == true)
				socket_.Close();
			
			socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); 
			
            try 
            { 
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(ip), port); 
                
				socket_.Connect(endpoint);
            } 
            catch (Exception e) 
            {
                MonoBehaviour.print(e.ToString());
				return false;
            } 
			
			return true;
		}
        
        public void close()
        {
            socket_.Close(0);
            socket_ = null;
        }

        public void send(byte[] datas)
        {
           if(socket_ == null || socket_.Connected == false) 
			{
               throw new ArgumentException ("invalid socket!");
            }
			
            if (datas == null || datas.Length == 0 ) 
			{
                throw new ArgumentException ("invalid datas!");
            }
			
			try
			{
				socket_.Send(datas);
			}
			catch (SocketException err)
			{
                if (err.ErrorCode == 10054 || err.ErrorCode == 10053)
                {
					Debug.Log(string.Format("NetworkInterface::send(): disable connect!"));
					socket_.Close();
					socket_ = null;
                }
				else{
					Debug.LogError(string.Format("NetworkInterface::send(): socket error(" + err.ErrorCode + ")!"));
				}
			}
        }
		
		public void recv()
		{
           if(socket_ == null || socket_.Connected == false) 
			{
               throw new ArgumentException ("invalid socket!");
            }
			
            if (socket_.Poll(1000, SelectMode.SelectRead))
            {
				byte[] datas = new byte[MemoryStream.BUFFER_MAX];
				
				int successReceiveBytes = 0;
				
				try
				{
					successReceiveBytes = socket_.Receive(datas, MemoryStream.BUFFER_MAX, 0);
				}
				catch (SocketException err)
				{
                    if (err.ErrorCode == 10054 || err.ErrorCode == 10053)
                    {
						Debug.Log(string.Format("NetworkInterface::recv(): disable connect!"));
						socket_.Close();
						socket_ = null;
                    }
					else{
						Debug.LogError(string.Format("NetworkInterface::recv(): socket error(" + err.ErrorCode + ")!"));
					}
				}
				
				if(successReceiveBytes > 0)
				{
					Debug.Log(string.Format("NetworkInterface::recv(): size={0}!", successReceiveBytes));
				}
				else if(successReceiveBytes == 0)
				{
					Debug.Log(string.Format("NetworkInterface::recv(): disable connect!"));
					socket_.Close();
					socket_ = null;
				}
				else
				{
					Debug.LogError(string.Format("NetworkInterface::recv(): socket error!"));
				}
				
				msgReader.process(datas, (MessageLength)successReceiveBytes);
            }
		}
		
		public void process() 
		{
			if(socket_ != null && socket_.Connected)
				recv();
			else
				System.Threading.Thread.Sleep(1);
		}
	}
} 