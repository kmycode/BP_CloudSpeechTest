using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1Beta1;
using Google.Protobuf;
using Grpc.Auth;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BP_CloudSpeechTest
{
	class Program
	{
		static void Main(string[] args)
		{
			// 証明書を作成
			var credential = GoogleCredential.FromJson(File.ReadAllText("SpeechTest-4db378c087bb.json"));
			credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");

			// サーバに接続するためのチャンネルを作成
			var channel = new Channel("speech.googleapis.com:443", credential.ToChannelCredentials());

			// Google Speech APIを利用するためのクライアントを作成
			var client = new Speech.SpeechClient(channel);

			// ストリーミングの設定
			var streamingConfig = new StreamingRecognitionConfig
			{
				Config = new RecognitionConfig
				{
					SampleRate = 16000,
					Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
					LanguageCode = "ja-JP",
				},
				InterimResults = true,
				SingleUtterance = false,
			};

			// ストリーミングを開始
			using (var call = client.StreamingRecognize())
			{
				Console.WriteLine("-----------\nstart.\n");

				// Cloud Speech APIからレスポンスが返ってきた時の挙動を設定
				var responseReaderTask = Task.Run(async () =>
				{
					// MoveNext１回につきレスポンス１回分のデータがくる
					while (await call.ResponseStream.MoveNext())
					{
						var note = call.ResponseStream.Current;

						// データがあれば、認識結果を出力する
						if (note.Results != null && note.Results.Count > 0 &&
							note.Results[0].Alternatives.Count > 0)
						{
							Console.WriteLine("result: " + note.Results[0].Alternatives[0].Transcript);
						}
					}
				});

				// 最初の呼び出しを行う。最初は設定データだけを送る
				var initialRequest = new StreamingRecognizeRequest
				{
					StreamingConfig = streamingConfig,
				};
				call.RequestStream.WriteAsync(initialRequest).Wait();

				// 録音モデルの作成
				var recorder = new RecordModel();

				// 録音モデルが音声データを吐いたら、それをすかさずサーバに送信する
				recorder.RecordDataAvailabled += (sender, e) =>
				{
					if (e.Length > 0)
					{
						// WriteAsyncは一度に一回しか実行できないので非同期処理の時は特に注意
						// ここではlockをかけて処理が重ならないようにしている
						lock (recorder)
						{
							call.RequestStream.WriteAsync(new StreamingRecognizeRequest
							{
								AudioContent = ByteString.FromBase64(Convert.ToBase64String(e.Buffer, 0, e.Length)),
							}).Wait();
						}
					}
				};

				// 録音の開始
				recorder.Start();

				// Cloud Speech APIのストリーミングは1回60秒までなので、50秒まできたら打ち切る
				var timer = new Timer(1000 * 50);
				timer.Start();

				// 50秒経過した時、実際に打ち切るコード
				timer.Elapsed += async (sender, e) =>
				{
					recorder.Stop();
					await call.RequestStream.CompleteAsync();
				};

				// 待機
				responseReaderTask.Wait();

				// ここに到達した時点で、APIの呼び出しが終了したということなので、タイマーを切る
				timer.Dispose();
			}

			Console.WriteLine("\n-----------\nCompleted (Time out)");
			Console.ReadKey();
		}
	}
}
