using UnityEngine;

public class GetComments : MonoBehaviour {

	string liveID = "";
	string liveURL = "";
	LiveComments live = null;

	void OnGUI() {
		GUI.Label(new Rect(15, 5, 100, 30), "Input URL");
		liveURL = GUI.TextField(new Rect(10, 30, 300, 25), liveURL);
		if ( GUI.Button(new Rect(315, 30, 50, 25), "取得")) {
			var url = "http://live.nicovideo.jp/watch/";
			liveID = liveURL.Substring(0, liveURL.IndexOf("?"))
					.Substring(url.Length);
			Debug.Log("listen: " + liveID);
			live.Run(liveID);
		}
	}

	void Start() {
		Debug.Log("start");
		live = new LiveComments();
	}

	// comm: コメント内容, no, prem, id
	void Update() {
		if (live == null)
			return;
		var comm = live.GetComment;
		if (comm != null)
			Debug.Log(comm[0]);
	}
}

