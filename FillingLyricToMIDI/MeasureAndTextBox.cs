using MIDIDataCSWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FillingLyricToMIDI
{
    class MeasureAndTextBox
    {
        /// <summary>
        /// 歌詞格納用テキストボックス
        /// </summary>
        public TextBox LyricTextBox { get; set; }

        /// <summary>
        /// MIDIノートの配列
        /// </summary>
        public MIDIEvent[] MidiEvent { get; set; }

        public MeasureAndTextBox(TextBox lyricTextBox)
        {
            LyricTextBox = lyricTextBox;
        }
    }
}
