#if !UNITY_WEBGL
using System;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;

namespace UNIHper
{
    [RequireComponent(typeof(AudioSource))]
    public class MicrophoneManager : SingletonBehaviourDontDestroy<MicrophoneManager>
    {
        const int HEADER_SIZE = 44;
        private AudioSource _audioSource;
        private AudioSource audioSource
        {
            get
            {
                if (_audioSource is null)
                {
                    _audioSource = GetComponent<AudioSource>();
                }
                return _audioSource;
            }
        }

        public float ClipLength
        {
            get { return audioSource.clip ? audioSource.clip.length : 0; }
        }

        private void Awake()
        {
            this.GetComponent<AudioSource>().playOnAwake = false;
        }

        public void Play()
        {
            audioSource.mute = false;
            audioSource.Play();
        }

        public void StartRecord()
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = Microphone.Start(null, true, 300, 16000);

            // Observable.Timer (TimeSpan.FromSeconds (0.5f)).Subscribe (_ => {
            // 	audioSource.Play ();
            // 	audioSource.timeSamples = Microphone.GetPosition (null);
            // 	int min;
            // 	int max;
            // 	Microphone.GetDeviceCaps (null, out min, out max);
            // });
        }

        public bool StopRecord()
        {
            audioSource.Play();

            int _lastTime = Microphone.GetPosition(null);
            if (_lastTime == 0)
                return false;
            if (!Microphone.IsRecording(null))
                return false;
            Microphone.End(null);
            float[] samples = new float[audioSource.clip.samples];
            audioSource.clip.GetData(samples, 0);
            float[] _clipSamplers = new float[_lastTime];
            Array.Copy(samples, _clipSamplers, _clipSamplers.Length - 1);
            audioSource.clip = AudioClip.Create(
                "_tempAudioClip",
                _clipSamplers.Length,
                1,
                16000,
                false
            );
            audioSource.clip.SetData(_clipSamplers, 0);

            return true;
        }

        public float LevelMax()
        {
            if (audioSource.clip == null)
                return 0;

            int _sampleWindow = 128;
            float levelMax = 0;
            float[] waveData = new float[_sampleWindow];
            int micPosition = Microphone.GetPosition(null) - (_sampleWindow + 1); // null means the first microphone
            if (micPosition < 0)
                return 0;
            audioSource.clip.GetData(waveData, micPosition);
            // Getting a peak on the last 128 samples
            for (int i = 0; i < _sampleWindow; i++)
            {
                float wavePeak = waveData[i] * waveData[i];
                if (levelMax < wavePeak)
                {
                    levelMax = wavePeak;
                }
            }

            return levelMax * 100;
        }

        public bool Save2File(string InFileName)
        {
            var _filePath = Path.Combine(Application.streamingAssetsPath, InFileName);
            if (Directory.Exists(Path.GetDirectoryName(_filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            }

            using (var _fileStream = CreateEmpty(_filePath))
            {
                ConvertAndWrite(_fileStream, audioSource.clip);
                WriteHeader(_fileStream, audioSource.clip);
            }

            return true;
        }

        FileStream CreateEmpty(string InFilePath)
        {
            var _fileStream = new FileStream(InFilePath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
            {
                _fileStream.WriteByte(emptyByte);
            }

            return _fileStream;
        }

        void ConvertAndWrite(FileStream InFileStream, AudioClip InAudioClip)
        {
            var _samples = new float[InAudioClip.samples];

            InAudioClip.GetData(_samples, 0);

            Int16[] intData = new Int16[_samples.Length];
            //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

            Byte[] bytesData = new Byte[_samples.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            int rescaleFactor = 32767; //to convert float to Int16

            for (int i = 0; i < _samples.Length; i++)
            {
                intData[i] = (short)(_samples[i] * rescaleFactor);
                Byte[] byteArr = new Byte[2];
                byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            InFileStream.Write(bytesData, 0, bytesData.Length);
        }

        static void WriteHeader(FileStream fileStream, AudioClip clip)
        {
            var hz = clip.frequency;
            var channels = clip.channels;
            var samples = clip.samples;

            fileStream.Seek(0, SeekOrigin.Begin);

            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            Byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            //UInt16 two = 2;
            UInt16 one = 1;

            Byte[] audioFormat = BitConverter.GetBytes(one);
            fileStream.Write(audioFormat, 0, 2);

            Byte[] numChannels = BitConverter.GetBytes(channels);
            fileStream.Write(numChannels, 0, 2);

            Byte[] sampleRate = BitConverter.GetBytes(hz);
            fileStream.Write(sampleRate, 0, 4);

            Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 16000*2*2
            fileStream.Write(byteRate, 0, 4);

            UInt16 blockAlign = (ushort)(channels * 2);
            fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            UInt16 bps = 16;
            Byte[] bitsPerSample = BitConverter.GetBytes(bps);
            fileStream.Write(bitsPerSample, 0, 2);

            Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(datastring, 0, 4);

            Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            fileStream.Write(subChunk2, 0, 4);

            //		fileStream.Close();
        }

        // Start is called before the first frame update
        void Start() { }

        // Update is called once per frame
        void Update()
        {
            // float[] spectrum = new float[16];

            // audioSource.GetSpectrumData (spectrum, 0, FFTWindow.Rectangular);
            // for (int i = 1; i < spectrum.Length - 1; i++) {
            // 	Debug.DrawLine (new Vector3 (i - 1, spectrum[i] + 10, 0), new Vector3 (i, spectrum[i + 1] + 10, 0), Color.red);
            // 	Debug.DrawLine (new Vector3 (i - 1, Mathf.Log (spectrum[i - 1]) + 10, 2), new Vector3 (i, Mathf.Log (spectrum[i]) + 10, 2), Color.cyan);
            // 	Debug.DrawLine (new Vector3 (Mathf.Log (i - 1), spectrum[i - 1] - 10, 1), new Vector3 (Mathf.Log (i), spectrum[i] - 10, 1), Color.green);
            // 	Debug.DrawLine (new Vector3 (Mathf.Log (i - 1), Mathf.Log (spectrum[i - 1]), 3), new Vector3 (Mathf.Log (i), Mathf.Log (spectrum[i]), 3), Color.blue);
            // }
        }
    }
}
#endif
