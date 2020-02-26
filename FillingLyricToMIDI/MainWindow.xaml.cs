using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using MIDIDataCSWrapper;
using System.IO;

namespace FillingLyricToMIDI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        MIDIData MidiData { get; set; } = null;

        /// <summary>
        /// midiファイル名を取得、設定します。
        /// </summary>
        string MidiFileName
        {
            get
            {
                return MidiFileNameTextBox.Text;
            }
            set
            {
                MidiFileNameTextBox.Text = value;
            }
        }

        /// <summary>
        /// 結果に表示する小節の配列
        /// </summary>
        List<MeasureAndTextBox> Measures { get; set; } = new List<MeasureAndTextBox>();

		MIDITrack TargetTrack
		{
			get
			{
				if (MidiData == null)
				{
					return null;
				}

				switch (MidiData.Format)
				{
					case MIDIData.Formats.Format0:
						return MidiData[0];
					case MIDIData.Formats.Format1:
						return MidiData[1];
					case MIDIData.Formats.Format2:
						MessageBox.Show("Format2には対応していません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
						return null;
					default:
						MessageBox.Show("不明なフォーマットです。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
						return null;
				}
			}
		}

        public MainWindow()
        {
            InitializeComponent();
			MIDIDataLib.SetDefaultCharCode(MIDIEvent.CharCodes.JP);
        }

        #region イベントハンドラ

        private void FileSelectButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "MIDIファイル|*.mid|全てのファイル|*.*"
            };
            bool dialogResult = fileDialog.ShowDialog(this) ?? false; //戻り値がnullの場合、falseを代入
            if (dialogResult)
            {
                MidiFileName = fileDialog.FileName;
            }
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
			OutputStackPanel.Children.Clear();
			Measures.Clear();

			if (!File.Exists(MidiFileName))
            {
                return;
            }

            try
            {
                MidiData = new MIDIData(MidiFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            if (MidiData.TimeMode != MIDIData.TimeModes.TPQN)
            {
                MessageBox.Show("TPQN以外の形式には対応していません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MIDITrack track = TargetTrack;
			if (track == null)
			{
				MessageBox.Show("未対応のフォーマットです。\n対応フォーマットは「Format0」と「Format1」です。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			//結合できるイベントを結合
			foreach (MIDIEvent midiEvent in track)
            {
                if (midiEvent.IsNoteOn)            //ノートONで
                {
                    if (!midiEvent.IsCombined)     //結合されていない場合
                    {
                        midiEvent.Combine();
                    }
                }
            }

            int currentMeasure = -1;
            List<MIDIEvent> events = new List<MIDIEvent>();                         //一小節分のノートイベントを保管する。
            //ノートのある小節の個数分、テキストボックス追加
            foreach (MIDIEvent midiEvent in track)
            {
                if (midiEvent.IsNoteOn)
                {
                    int measure = MidiData.BreakTime(midiEvent.Time).Measure;       //このノートの所属している小節取得
                    if (measure > currentMeasure)                                   //次の小節に行ったら
                    {
                        if (events.Count != 0)                                      //小節へイベントの追加、その後クリア
                        {
                            Measures.Last().MidiEvent = events.ToArray();
                            events.Clear();
                        }

                        TextBox textBox = AddTextBox(measure);                      //ここで作成しないと、小節番号がわからない。
                        Measures.Add(new MeasureAndTextBox(textBox));

                        events.Add(midiEvent);

                        currentMeasure = measure;
                    }
                    else
                    {
                        events.Add(midiEvent);
                    }
                }
            }
            //最後のイベント追加
            if (events.Count != 0)
            {
                Measures.Last().MidiEvent = events.ToArray();
                events.Clear();
            }

            SaveButton.IsEnabled = true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
			MakeLyric(TargetTrack, Measures);

			SaveFileDialog fileDialog = new SaveFileDialog
            {
                Filter = "MIDIファイル|*.mid",
                InitialDirectory = Path.GetDirectoryName(MidiFileName),
                FileName = Path.GetFileName(MidiFileName),
            };
            bool dialogResult = fileDialog.ShowDialog(this) ?? false; //戻り値がnullの場合、falseを代入
            if (dialogResult)
            {
                MidiData?.SaveAsSMF(fileDialog.FileName);   //nullなら実行されない
            }
        }

        #endregion イベントハンドラ

        #region メソッド

        /// <summary>
        /// 出力部分にテキストボックスを追加します。左側には、小節番号が付きます。
        /// </summary>
        /// <param name="measureNumber">小節番号</param>
        private TextBox AddTextBox(int measureNumber)
        {
            TextBlock textBlock = new TextBlock()
            {
                Margin = new Thickness(3),
                Text = measureNumber.ToString("D3")
            };
            TextBox textBox = new TextBox()
            {
                Margin = new Thickness(3)
            };
            DockPanel docPanel = new DockPanel();
            docPanel.Children.Add(textBlock);
            docPanel.Children.Add(textBox);
            DockPanel.SetDock(textBlock, Dock.Left);

            OutputStackPanel.Children.Add(docPanel);

            return textBox;
        }

		/// <summary>
		/// MIDIデータに歌詞を追加します。
		/// </summary>
		private void MakeLyric(MIDITrack midiTrack, List<MeasureAndTextBox> measures)
		{
			RemoveLyric(midiTrack);

			foreach (MeasureAndTextBox measure in measures)
			{
				StringBuilder stringBuilder = new StringBuilder(measure.Lyric);
				for (int i = 0; i < measure.MidiEvent.Length; i++)
				{
					if (stringBuilder.Length <= 0)
					{
						break;
					}
					string lyricChar = stringBuilder[0].ToString(); //先頭の文字を取得
					stringBuilder.Remove(0, 1);                     //先頭の文字を削除
					//次の文字が小文字の場合、一緒に歌詞に含める。
					if (IsFirstCharacterLowerCase(stringBuilder.ToString()))
					{
						lyricChar += stringBuilder[0].ToString();   //先頭の文字を追加
						stringBuilder.Remove(0, 1);                 //先頭の文字を削除
					}

					MIDIEvent targetEvent = measure.MidiEvent[i];
					MIDIEvent lyricEvent = MIDIEvent.CreateLyric(targetEvent.Time, lyricChar);
					midiTrack.InsertEventAfter(lyricEvent, targetEvent);
				}
			}
		}
		
		/// <summary>
		/// MIDIデータから歌詞を削除します。
		/// </summary>
		public void RemoveLyric(MIDITrack midiTrack)
		{
			List<MIDIEvent> lyricEvents = new List<MIDIEvent>();

			//ループ中にイベントを除外又は削除してはならない。イベントの消滅により次のイベントが探索できなくなるからだ。
			foreach (MIDIEvent midiEvent in midiTrack)
			{
				if (midiEvent.IsLyric)
				{
					lyricEvents.Add(midiEvent);
				}
			}

			foreach (MIDIEvent midiEvent in lyricEvents)
			{
				midiTrack.RemoveEvent(midiEvent);
			}
		}

		/// <summary>
		/// 先頭文字が小文字が判定します。
		/// </summary>
		/// <param name="str">判定する文字列</param>
		/// <returns>小文字の場合true</returns>
		private bool IsFirstCharacterLowerCase(string str)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return false;
			}
			char firstChar = str[0];
			string lowerCases = "ぁぃぅぇぉゃゅょっ";
			int index = lowerCases.IndexOf(firstChar);

			if (index >= 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		#endregion メソッド
	}
}