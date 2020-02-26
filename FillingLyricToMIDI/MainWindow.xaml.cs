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
using System.Windows.Shapes;
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

        public MainWindow()
        {
            InitializeComponent();
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
            //テキストボックステスト
            for (int i = 0; i < 10; i++)
            {
                AddTextBox(i);
            }

            return;

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

            MIDITrack track = null;
            switch (MidiData.Format)
            {
                case MIDIData.Formats.Format0:
                    track = MidiData[0];
                    break;
                case MIDIData.Formats.Format1:
                    track = MidiData[1];
                    break;
                case MIDIData.Formats.Format2:
                    MessageBox.Show("Format2には対応していません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                default:
                    MessageBox.Show("不明なフォーマットです。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
            //ノートのある小節の個数分、テキストボックス追加
            foreach (MIDIEvent midiEvent in track)
            {
                if (midiEvent.IsNote)
                {
                    int measure = MidiData.BreakTime(midiEvent.Time).Measure;       //このノートの含まれている小節取得
                    if (measure > currentMeasure)
                    {
                        AddTextBox(measure);
                        currentMeasure = measure;
                    }
                    else
                    {
                        //何もしない
                    }
                }
            }


            SaveButton.IsEnabled = true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog
            {
                Filter = "MIDIファイル|*.mid"
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

        #endregion メソッド
    }
}