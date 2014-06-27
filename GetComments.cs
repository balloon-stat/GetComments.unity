﻿using UnityEngine;

public class GetComments : MonoBehaviour {

	string liveID = "";
	string liveURL = "";
	LiveComments live = null;

	void OnGUI() {
		GUI.Label(new Rect(15, 5, 100, 30), "Input URL");
		liveURL = GUI.TextField(new Rect(10, 30, 300, 25), liveURL);
		if (GUI.Button(new Rect(315, 30, 50, 25), "接続")) {
			var url = "http://live.nicovideo.jp/watch/";
			var ix = liveURL.IndexOf("?");
			ix = ix != -1 ? ix : liveURL.Length;
			liveID = liveURL.Substring(0, ix)
					.Substring(url.Length);
			Debug.Log("View: " + liveID);
			live.Run(liveID);
		}
		if (GUI.Button(new Rect(315, 60, 50, 25), "切断")) {
			live.DisConnect();
			Debug.Log("Disconnect");
		}
	}

	void Start() {
		Debug.Log("Start");
		live = new LiveComments();
		live.numRoom = 2;
		CommentClient.FromRes = 0;
	}

	// res: コメント内容, no, prem, id, room_label
	void Update() {
		var res = live.Res;
		if (res != null)
			Debug.Log(res[4] + ": " + res[0]);
	}
}

