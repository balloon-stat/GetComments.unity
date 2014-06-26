using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;

public class LiveComments {
	
	BackgroundWorker worker;
	ManualResetEvent getCookieDone;
	CommentClient client;
	CookieContainer cc;
	
	public LiveComments() {
		ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => { return true; };

		getCookieDone = new ManualResetEvent(false);
		worker = new BackgroundWorker();
		worker.DoWork += new DoWorkEventHandler(DoGetCookie);
		worker.RunWorkerAsync();
	}

	public void Run(string liveID) {
		worker = new BackgroundWorker();
		worker.DoWork += new DoWorkEventHandler(DoGetComment);
		worker.RunWorkerAsync(liveID);
	}
	
	public void DoGetCookie(object sender, DoWorkEventArgs ev) {
		var file = "cookie.dat";
		if (File.Exists(file))
			cc = readCookie(file);
		if (!isLogin(cc))
			cc = login();
		getCookieDone.Set();
	}

	CookieContainer login() {
		var file = "account.info";
		if (!File.Exists(file)) {
			Debug.Log("can not found 'account.info' file.");
			throw new Exception("can not found 'account.info' file.");
		}
		var account = File.ReadAllLines(file);
		var ccont = NicoLiveAPI.Login(account);
		writeCookie(ccont);
		return ccont;
	}

	CookieContainer readCookie(string file) {
		var container = new CookieContainer();
		var data = File.ReadAllText(file).Trim();
		if (data == "") {
			Debug.Log(file + " is empty");
			return container;
		}
		var cookie = new Cookie("user_session", data, "/", ".nicovideo.jp");
		container.Add(cookie);
		return container;
	}

	void writeCookie(CookieContainer ccont) {
		var file = "cookie.dat";
		var uri = new Uri("http://live.nicovideo.jp");
		string data = null;
		foreach(Cookie cookie in ccont.GetCookies(uri)) {
			if (cookie.Name == "user_session")
			data = cookie.Value;
		}
		File.WriteAllText(file, data);
	}
	
	bool isLogin(CookieContainer ccont) {
		var url = "http://live.nicovideo.jp/notifybox";
		var notifybox = NicoLiveAPI.get(url, ref ccont);
		var ret = notifybox.Length > 124;

		Debug.Log("login: " + ret);
		return ret;
	}
	
	public void DoGetComment(object sender, DoWorkEventArgs ev) {
		getCookieDone.WaitOne();
		var liveID = (string)ev.Argument;
		var info = NicoLiveAPI.GetPlayerStatus(cc, liveID);
		client = NicoLiveAPI.GetComments(info);
	}
	
	public string[] GetComment {
		get {
			if (client == null)
				return null;
			if (client.Comments.Count == 0)
				return null;
			return client.Comments.Dequeue();
		}
	}
}

static class NicoLiveAPI {
	
	public static CookieContainer Login(string[] account) {
		var url = "https://secure.nicovideo.jp/secure/login";
		var cc = new CookieContainer();
		var param = new Dictionary<string, string>();
		param.Add("mail", account[0]);
		param.Add("password", account[1]);
		
		post(url, ref cc, param);
		Debug.Log("login is finished");
		return cc;
	}
	
	public static Dictionary<string, string> GetPlayerStatus(CookieContainer cc, string liveID) {
		var url = "http://live.nicovideo.jp/api/getplayerstatus?v=" + liveID; 
		var xdoc = XDocument.Parse(get(url, ref cc));
		var ret = new Dictionary<string, string>();
		var ms = xdoc.Descendants("ms").Single();
		ret.Add("base_time", xdoc.Descendants("base_time").Single().Value);
		ret.Add("addr", ms.Element("addr").Value);
		ret.Add("port", ms.Element("port").Value);
		ret.Add("thread", ms.Element("thread").Value);
		var comnID1 = xdoc.Descendants("default_community").Single().Value;
		var comnID2 = xdoc.Descendants("room_label").Single().Value;
		Debug.Log("default_community : " + comnID1 + ", room_label : " + comnID2);
		ret.Add("comnID", comnID1);
		
		return ret;
	}
	
	public static CommentClient GetComments(Dictionary<string, string> info) {
		var client = new CommentClient(info);
		client.StartRecive();
		return client;
	}
	
	public static string get(string url, ref CookieContainer cc) {
		var req = (HttpWebRequest)WebRequest.Create(url);
		req.CookieContainer = cc;
		
		string ret;
		using(var res = req.GetResponse())
			using(var resStream = res.GetResponseStream())
		using(var sr = new StreamReader(resStream, Encoding.GetEncoding("UTF-8"))) {
			ret = sr.ReadToEnd();
		}
		return ret;
	}
	
	public static string post(string url, ref CookieContainer cc, Dictionary<string, string> param) {
		var postData = string.Join("&",
		                           param.Select(x=>x.Key + "=" + System.Uri.EscapeUriString(x.Value)).ToArray());
		
		byte[] postDataBytes = Encoding.ASCII.GetBytes(postData);
		
		var req = (HttpWebRequest)WebRequest.Create(url);
		req.CookieContainer = cc;
		req.Method = "POST";
		req.ContentType = "application/x-www-form-urlencoded";
		req.ContentLength = postDataBytes.Length;
		
		using(var reqStream = req.GetRequestStream()) {
			reqStream.Write(postDataBytes, 0, postDataBytes.Length);
		};
		
		string ret;
		using(var res = req.GetResponse())
			using(var resStream = res.GetResponseStream())
		using(var sr = new StreamReader(resStream, Encoding.GetEncoding("UTF-8"))) {
			ret = sr.ReadToEnd();
		}
		return ret;
	}
}

class StateObject {
	public Socket workSocket = null;
	public const int BufferSize = 1024;
	public byte[] buffer = new byte[BufferSize];
	public string content = "";
	public Queue<string[]> res = new Queue<string[]>();
}

class CommentClient : IDisposable {
	
	public bool AllDone = false;
	public bool IsExistRev = false;
	StateObject state;
	Socket sock;
	Dictionary<string, string> info;
	BackgroundWorker gWorker;
	
	public Queue<string[]> Comments {
		get { return state.res; }
	}

	public BackgroundWorker Worker {
		get { return gWorker; }
	}

	public void Dispose() {
		AllDone = true;
		sock.Disconnect(false);
	}
	
	public CommentClient(Dictionary<string, string> info) {
		try {
			this.info = info;
			var addr = info["addr"];
			var port = info["port"];
			state = new StateObject();
			
			var hostaddr = Dns.GetHostEntry(addr).AddressList[0];
			var ephost = new IPEndPoint(hostaddr, int.Parse(port));
			sock = new Socket(AddressFamily.InterNetwork,
			                  SocketType.Stream, ProtocolType.Tcp);
			
			sock.Connect(ephost);
			state.workSocket = sock;
		}
		catch (Exception e) {
			Debug.Log(e.ToString());
		}
	}
	
	public void StartRecive() {
		var rw = new BackgroundWorker();
		rw.DoWork += new DoWorkEventHandler(DoReceive);
		rw.RunWorkerAsync();
		gWorker = rw;
	}
	
	void DoReceive(object sender, DoWorkEventArgs ev) {
		
		try {
			AllDone = false;
			IsExistRev = false;
			
			var thread = info["thread"];
			var worker = sender as BackgroundWorker;
			
			Debug.Log("Sending request...");
			send(string.Format("<thread thread=\"{0}\" version=\"20061206\" res_from=\"-100\"/>\0", thread));
			
			Debug.Log("BeginReceive...");
			sock.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,
			                  new AsyncCallback(ReadCallback), state);
			var count = 0;
			while (true) {
				send("\0");
				while (count < 600) {
					if (AllDone || worker.CancellationPending)
						return;
					Thread.Sleep(1000);
					count++;
				}
				count = 0;
			}
		}	
		catch (Exception e) {
			Debug.Log(e.ToString());
		}
	}
	
	public void ReadCallback(IAsyncResult ar) {
		var state = (StateObject)ar.AsyncState;
		var handler = state.workSocket;
		
		var bytesRead = handler.EndReceive(ar);
		
		if (bytesRead > 0) {
			IsExistRev = true;
			var content = Encoding.UTF8.GetString(state.buffer, 0, bytesRead);
			state.content = resProc(content, state.content);
		}
		if (!AllDone) {
			handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
			                     new AsyncCallback(ReadCallback), state);
		}
	}
	
	string resProc(string content, string prev) {
		
		string xdocs;
		if (content.StartsWith("<chat")) {
			xdocs = content;
		}
		else {
			xdocs = prev + content;
		}
		
		foreach (string line in xdocs.Split('\0')) {
			if (line.StartsWith("<thread"))	{
				// var th = XElement.Parse(line);
				
				// ticket = th.Attribute("ticket").Value;
				// srvTime = th.Attribute("server_time").Value;
				// DateTimeStart = DateTime.Now;
				continue;
			}
			if (line.StartsWith("<chat_result")) {
				continue;
			}
			if (!line.EndsWith("</chat>")) {
				return line;
			}
			if (line.StartsWith("<chat ")) {
				chatProc(line);
			}
		}
		return "";
	}
	
	void chatProc(string chat_el)
	{
		var xelem = XElement.Parse(chat_el);
		var chat = xelem.Value;
		var id = xelem.Attribute("user_id").Value;
		var no = xelem.Attribute("no").Value;
		var premattr = xelem.Attribute("premium");
		string prem;
		
		if (premattr == null)
			prem = "0";
		else
			prem = premattr.Value;
		
		state.res.Enqueue(new string[]{chat, no, prem, id});
		
		if (chat == "/disconnect" && prem == "3")
			AllDone = true;
	}
	
	void send(string data) {
		var byteData = Encoding.UTF8.GetBytes(data);
		sock.Send(byteData, 0, byteData.Length, 0);
	}
	
}
