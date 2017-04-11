using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BP_CloudSpeechTest
{
	class RecordModel : IDisposable
	{
		#region 変数

		private WaveInEvent waveIn;
		private bool isStoped = false;

		#endregion

		#region メソッド

		public void Start()
		{
			if (this.waveIn != null)
			{
				return;
			}

			this.waveIn = new WaveInEvent();
			this.waveIn.DataAvailable += this.OnDataAvailable;
			this.waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

			this.waveIn.StartRecording();
		}

		public void Stop()
		{
			this.waveIn.StopRecording();
			this.isStoped = true;

			this.waveIn.Dispose();

			this.waveIn = null;
		}

		public void Dispose()
		{
			this.Stop();
		}

		~RecordModel()
		{
			this.Dispose();
			GC.SuppressFinalize(this);
		}

		private void OnDataAvailable(object sender, WaveInEventArgs e)
		{
			this.RecordDataAvailabled?.Invoke(this, new RecordDataAvailabledEventArgs(e.Buffer, e.BytesRecorded));
			if (this.isStoped) return;
		}
		#endregion

		#region イベント

		public event RecordDataAvailabledEventHandler RecordDataAvailabled;

		#endregion
	}

	public delegate void RecordDataAvailabledEventHandler(object sender, RecordDataAvailabledEventArgs e);
	public class RecordDataAvailabledEventArgs : EventArgs
	{
		public byte[] Buffer { get; }
		public int Length { get; }

		public RecordDataAvailabledEventArgs(byte[] buffer, int length)
		{
			this.Buffer = buffer;
			this.Length = length;
		}
	}
}
