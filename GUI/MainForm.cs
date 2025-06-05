using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using Accord.Audio.Filters;
using Recorder.Recorder;
using Recorder.MFCC;
using Recorder.SpeakerID;      // for DTW
using System.Collections.Generic;
using System.Xml.Linq;
using System.Diagnostics;


namespace Recorder
{
    /// <summary>
    ///   Speaker Identification application.
    /// </summary>
    /// 
    public partial class MainForm : Form
    {
        // holds each enrolled user

        public class Template
        {
            public string Name { get; set; }
            public Sequence Seq { get; set; }
            public Template(string name, Sequence seq)
            {
                Name = name; Seq = seq;
            }
        }

        // list of all enrolled templates
        List<Template> templates = new List<Template>();

        // the most-recently computed feature sequence
        //Sequence currentSeq = null;
        private void UpdateUI()
        {
            bool hasSeq = (seq != null);
            bool hasTpl = (templates.Count > 0);

            // name textbox
            textBox1.Enabled = hasSeq;

            // enroll button
            btnAdd.Enabled = hasSeq;

            // identify (no prune)
            button1.Enabled = hasSeq && hasTpl;

            // identify (pruned)
            button2.Enabled = hasSeq && hasTpl;

            // pruning‐width selector
            numericUpDown2.Enabled = hasSeq && hasTpl;
        }
        /// <summary>
        /// Data of the opened audio file, contains:
        ///     1. signal data
        ///     2. sample rate
        ///     3. signal length in ms
        /// </summary>
        private AudioSignal signal = null;
        Sequence seq = null;
       
        private string path;

        private Encoder encoder;
        private Decoder decoder;

        private bool isRecorded;

        public MainForm()
        {
            
            InitializeComponent();

            // Configure the wavechart
            chart.SimpleMode = true;
            chart.AddWaveform("wave", Color.Green, 1, false);
            updateButtons();
        }


        /// <summary>
        ///   Starts recording audio from the sound card
        /// </summary>
        /// 
        private void btnRecord_Click(object sender, EventArgs e)
        {
            isRecorded = true;
            this.encoder = new Encoder(source_NewFrame, source_AudioSourceError);
            this.encoder.Start();
            updateButtons();
        }

        /// <summary>
        ///   Plays the recorded audio stream.
        /// </summary>
        /// 
        private void btnPlay_Click(object sender, EventArgs e)
        {
            InitializeDecoder();
            // Configure the track bar so the cursor
            // can show the proper current position
            if (trackBar1.Value < this.decoder.frames)
                this.decoder.Seek(trackBar1.Value);
            trackBar1.Maximum = this.decoder.samples;
            this.decoder.Start();
            updateButtons();
        }

        private void InitializeDecoder()
        {
            if (isRecorded)
            {
                // First, we rewind the stream
                this.encoder.stream.Seek(0, SeekOrigin.Begin);
                this.decoder = new Decoder(this.encoder.stream, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
            else
            {
                this.decoder = new Decoder(this.path, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
        }

        /// <summary>
        ///   Stops recording or playing a stream.
        /// </summary>
        /// 
        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();   
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This callback will be called when there is some error with the audio 
        ///   source. It can be used to route exceptions so they don't compromise 
        ///   the audio processing pipeline.
        /// </summary>
        /// 
        private void source_AudioSourceError(object sender, AudioSourceErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   This method will be called whenever there is a new input audio frame 
        ///   to be processed. This would be the case for samples arriving at the 
        ///   computer's microphone
        /// </summary>
        /// 
        private void source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            this.encoder.addNewFrame(eventArgs.Signal);
            updateWaveform(this.encoder.current, eventArgs.Signal.Length);
       }


        /// <summary>
        ///   This event will be triggered as soon as the audio starts playing in the 
        ///   computer speakers. It can be used to update the UI and to notify that soon
        ///   we will be requesting additional frames.
        /// </summary>
        /// 
        private void output_FramePlayingStarted(object sender, PlayFrameEventArgs e)
        {
            updateTrackbar(e.FrameIndex);

            if (e.FrameIndex + e.Count < this.decoder.frames)
            {
                int previous = this.decoder.Position;
                decoder.Seek(e.FrameIndex);

                Signal s = this.decoder.Decode(e.Count);
                decoder.Seek(previous);

                updateWaveform(s.ToFloat(), s.Length);
            }
        }

        /// <summary>
        ///   This event will be triggered when the output device finishes
        ///   playing the audio stream. Again we can use it to update the UI.
        /// </summary>
        /// 
        private void output_PlayingFinished(object sender, EventArgs e)
        {
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This event is triggered when the sound card needs more samples to be
        ///   played. When this happens, we have to feed it additional frames so it
        ///   can continue playing.
        /// </summary>
        /// 
        private void output_NewFrameRequested(object sender, NewFrameRequestedEventArgs e)
        {
            this.decoder.FillNewFrame(e);
        }


        void output_AudioOutputError(object sender, AudioOutputErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   Updates the audio display in the wave chart
        /// </summary>
        /// 
        private void updateWaveform(float[] samples, int length)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    chart.UpdateWaveform("wave", samples, length);
                }));
            }
            else
            {
                if (this.encoder != null) { chart.UpdateWaveform("wave", this.encoder.current, length); }
            }
        }

        /// <summary>
        ///   Updates the current position at the trackbar.
        /// </summary>
        /// 
        private void updateTrackbar(int value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
                }));
            }
            else
            {
                trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
            }
        }

        private void updateButtons()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(updateButtons));
                return;
            }

            if (this.encoder != null && this.encoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = false;
            }
            else if (this.decoder != null && this.decoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = this.path != null || this.encoder != null;
                btnIdentify.Enabled = false;
                btnPlay.Enabled = this.path != null || this.encoder != null;//stream != null;
                btnStop.Enabled = false;
                btnRecord.Enabled = true;
                trackBar1.Enabled = this.decoder != null;
                trackBar1.Value = 0;
            }
        }

        private void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
        }

        private void saveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.encoder != null)
            {
                Stream fileStream = saveFileDialog1.OpenFile();
                this.encoder.Save(fileStream);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog(this);
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (this.encoder != null) { lbLength.Text = String.Format("Length: {0:00.00} sec.", this.encoder.duration / 1000.0); }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                isRecorded = false;
                path = open.FileName;
                //Open the selected audio file
                signal = AudioOperations.OpenAudioFile(path);
                signal = AudioOperations.RemoveSilence(signal);
                 seq = AudioOperations.ExtractFeatures(signal);
                for (int i = 0; i < seq.Frames.Length; i++)
                {
                    for (int j = 0; j < 13; j++)
                    {

                        if (double.IsNaN(seq.Frames[i].Features[j]) || double.IsInfinity(seq.Frames[i].Features[j]))
                            throw new Exception("NaN");
                    }
                }
                updateButtons();

            }
        }

        private void Stop()
        {
            if (this.encoder != null) { this.encoder.Stop(); }
            if (this.decoder != null) { this.decoder.Stop(); }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            // make sure you’ve got the right field here:
            if (seq == null)
            {
                MessageBox.Show("No features loaded – please open or record audio first.");
                return;
            }

            string name = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter your name in the textbox.");
                return;
            }

            // use currentSeq (not seq)
            templates.Add(new Template(name, seq));
            MessageBox.Show($"Enrolled '{name}' ({templates.Count} total).");

            textBox1.Clear();
            UpdateUI();
        }

        private void loadTrain1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.ShowDialog();

            var hobba = TestcaseLoader.LoadTestcase2Training(fileDialog.FileName);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (seq == null || templates.Count == 0)
            {
                MessageBox.Show("Nothing to match.");
                return;
            }

            double bestDist = double.MaxValue;
            string bestName = null;

            // loop through all templates
            foreach (var tpl in templates)
            {


                var sw = Stopwatch.StartNew();
                
               
                // plain DTW:
                double d = DTW.Compute(tpl.Seq, seq);
                sw.Stop();
                // or use pruning: DTW.ComputePruned(tpl.Seq, currentSeq, W);
                Console.WriteLine($"Compute plain: {sw.Elapsed.TotalSeconds:F4}s");
                if (d < bestDist)
                {
                    bestDist = d;
                    bestName = tpl.Name;
                }
            }

            MessageBox.Show($"Identified as: {bestName}\nDistance = {bestDist:F2}");
            
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (seq == null || templates.Count == 0)
            {
                MessageBox.Show("Nothing to match.");
                return;
            }

            int w = int.Parse(textBox2.Text);
            double bestDist = double.MaxValue;
            string bestName = null;

            foreach (var tpl in templates)
            {
                var sw = Stopwatch.StartNew();
                
                double d = DTW.ComputePruned(tpl.Seq, seq, w);
                sw.Stop();
                
                Console.WriteLine($"Compute plain in Pruned: {sw.Elapsed.TotalSeconds:F4}s");
                if (d < bestDist)
                {// for best so far 34an tfdl tkarn be a3la rkm
                    bestDist = d;
                    bestName = tpl.Name;
                }
            }

            MessageBox.Show(
                $"Identified as: {bestName}\n" +
                $"Pruned distance (W={w}): {bestDist:F2}"
            );
            //UpdateUI();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void chart_Click(object sender, EventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (seq == null || templates.Count == 0)
            {
                MessageBox.Show("Nothing to match.");
                return;
            }

            
            int w = int.Parse(textBox2.Text);
          //  int t = (int)numericUpDown3.Value;
          // mmokn n3ml a7sn mn el code hna rkm kber masln

            double bestDist = double.MaxValue;
            string bestName = null;

            foreach (var tpl in templates)
            {
                var sw = Stopwatch.StartNew();
                double d = DTW.ComputePruned(tpl.Seq, seq, w,bestDist);
                sw.Stop();
                Console.WriteLine($"Compute plain in threshold: {sw.Elapsed.TotalSeconds:F4}s");

                if (d < bestDist)
                {
                    bestDist = d;
                    bestName = tpl.Name;
                }
            }

            MessageBox.Show(
                $"Identified as: {bestName}\n" +
                $"Pruned distance (W={w}): {bestDist:F2}"
            );
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
