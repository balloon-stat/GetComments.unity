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
			var ix = liveURL.IndexOf("?");
			ix = ix != -1 ? ix : liveURL.Length;
			liveID = liveURL.Substring(0, ix)
					.Substring(url.Length);
			Debug.Log("view: " + liveID);
			live.Run(liveID);
		}
	}

	void Start() {
		Debug.Log("start");
		CommentClient.FromRes = 0;
		live = new LiveComments();
		live.numRoom = 2;
	}

	// comm: コメント内容, no, prem, id, room_label
	void Update() {
		if (live == null)
			return;
		var comm = live.GetComment;
		if (comm != null)
			Debug.Log(comm[4] + ": " + comm[0]);
	}
}

