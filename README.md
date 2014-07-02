GetComments.unity
=================

Asset to get NicoLive comments for Unity  
ニコニコ生放送のコメントを取得します。

GetComments.csを参考に使用してください。

投稿コメントはキューにためられます。  
live.Res はキューからコメント配列を１つ取り出して返します。

コメント配列の要素は、コメント内容、コメント番号、アカウント種別、ID、部屋名の順です。

FromRes は既に投稿されたコメントを、さかのぼっていくつまで取得するか設定する値です。  
NumRoom はアリーナや立ち見などの部屋の数を設定する値です。

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
