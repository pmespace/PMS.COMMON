using System;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Runtime;
using COMMON;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics.Eventing.Reader;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace COMMON.WSServer
{
	public class CWSConnectedClient
	{
		public CWSConnectedClient(WebSocket ws, object id = default)
		{
			//Requests = new WSStatistics();
			//Replies = new WSStatistics();
			Connection = DateTime.Now;
			WS = ws;
			ID = id;
		}

		public object ID { get; private set; }
		public WebSocket WS { get; internal set; }
		public DateTime Connection { get; private set; }
		//public WSStatistics Requests { get; }
		//public WSStatistics Replies { get; }

		//public override string ToString() => $"{(ID.IsNullOrEmpty() ? string.Empty : $"ID: {ID}; ")}Connected at : {Connection}; [Requests: {Requests}]; [Replies: {Replies}]";
		public override string ToString() => $"{(null == ID ? string.Empty : $"ID: {ID}; ")}Connected at : {Connection}";
	}
	public class WSStatistics : CWSSize
	{
		public WSStatistics()
		{
			_Exchanges = new List<CWSExchange>();
			Exchanges = new ReadOnlyCollection<CWSExchange>(_Exchanges);
		}

		List<CWSExchange> _Exchanges;
		public ReadOnlyCollection<CWSExchange> Exchanges { get; }
		public override decimal Size
		{
			get
			{
				decimal d = 0;
				foreach (CWSExchange k in Exchanges) d += k.Size;
				return d;
			}
		}

		public void Add(CWSExchange exchange) => _Exchanges.Add(exchange);
		public List<CWSExchange> GetMessages() => new List<CWSExchange>(Exchanges);
		public override string ToString() { return $"{Exchanges.Count} for {Size} bytes"; }
	}
	public class CWSExchange : CWSSize
	{
		public CWSExchange(CWSMessage incoming)
		{
			Incoming = incoming;
			_Outgoing = new List<CWSMessage>();
			Outgoing = new ReadOnlyCollection<CWSMessage>(_Outgoing);
		}

		public CWSMessage Incoming { get; }
		List<CWSMessage> _Outgoing;
		public ReadOnlyCollection<CWSMessage> Outgoing { get; }
		public override decimal Size
		{
			get
			{
				decimal d = Incoming.Size;
				foreach (CWSMessage k in Outgoing) d += k.Size;
				return d;
			}
		}

		public override string ToString()
		{
			string s = string.Empty;
			List<string> ls = ToStrings();
			for (int i = 0; i < ls.Count; i++)
				s += $"{ls[i]}" + (i < ls.Count - 1 ? " " : string.Empty);
			ls.Add($"total size: {Size} bytes");
			return s;
		}
		public List<string> ToStrings()
		{
			List<string> ls = new List<string>();
			ls.Add($">>> {Incoming.Message}");
			for (int i = 0; i < Outgoing.Count; i++)
				ls.Add($"<<< {(1 == Outgoing.Count ? string.Empty : "#{i + 1} ")}{Outgoing[i]}");
			return ls;
		}
		public void AddOutgoingMessage(CWSMessage outgoing)
		{
			if (default != outgoing) _Outgoing.Add(outgoing);
		}
	}
	public class CWSMessage : CWSSize
	{
		public CWSMessage(string msg)
		{
			Timestamp = DateTime.Now;
			Size = Message.Length;
			Message = msg;
		}
		public string Message { get; }
		public DateTime Timestamp { get; }
		public override string ToString() { return $"{Timestamp.ToString("s")} => {Message}"; }
	}
	public class CWSSize
	{
		public virtual decimal Size { get; protected set; }
	}
	class WSMessageException : Exception { }

	/// <summary>
	/// A dictionary of <see cref="CWSConnectedClient"/>,
	/// the key is the connected address (IP:port)
	/// </summary>
	public class CWSClients : ConcurrentDictionary<string, CWSConnectedClient>
	{
		public override string ToString()
		{
			var l = this.ToArray();
			bool equal0 = 0 == l.Length;
			bool lessthan2 = 2 <= l.Length;
			string s = $"Currently {l.Length} client{(lessthan2 ? "s" : string.Empty)} connected" + (!equal0 ? Chars.CRLF : string.Empty);
			for (int i = 1; i <= l.Length; i++)
				s += l[i - 1].ToString() + (l.Length != i ? Chars.CRLF : string.Empty);
			return s;
		}
	}
	public class CWSReadOnlyClients : ReadOnlyDictionary<string, CWSConnectedClient>
	{
		public CWSReadOnlyClients(CWSClients clients) : base(clients) { }
	}
}