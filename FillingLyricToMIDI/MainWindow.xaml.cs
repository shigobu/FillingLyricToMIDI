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
            if (!File.Exists(MidiFileName))
            {
                return;
            }

            MidiData = new MIDIData(MidiFileName);
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
                    return;
                default:
                    return;
            }
            //結合できるイベントを結合
            foreach (MIDIEvent @event in track)
            {
                if (@event.IsNoteOn)            //ノートONで
                {
                    if (!@event.IsCombined)     //結合されていない場合
                    {
                        @event.Combine();
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
    }
}
