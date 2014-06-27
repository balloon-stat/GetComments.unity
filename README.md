GetComments.unity
=================

Asset to get NicoLive comments for Unity  
ニコニコ生放送のコメントを取得します。

GetComments.csを参考に使用してください。

CommentClient.FromRes は既に投稿されたコメントを  
さかのぼっていくつまで取得するか設定する値です。  
live.numRoom はアリーナや立ち見などの部屋の数を  
設定する値です。


### 設定

プロジェクトのルートディレクトリに  

* account.info  
* cookie.dat  

のどちらかを用意してください。

#### account.info

1行目　mail address  
2行目　password  

ニコニコへログインするための情報を上記のフォーマットで書きます。

#### cookie.dat

user_session の値を書きます。
