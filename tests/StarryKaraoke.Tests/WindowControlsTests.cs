using System;
using System.Threading;
using MusicBeePlugin;
using Xunit;

namespace StarryKaraoke.Tests
{
    public class WindowControlsTests
    {
        /// <summary>Each window command reaches the plugin's command exactly once (a seek that called
        /// itself overflowed the stack and took MusicBee down).</summary>
        [Fact]
        public void Window_commands_call_the_player_once()
        {
            int seeks = 0, plays = 0, prev = 0, next = 0;
            double seekedTo = -1;
            Exception failure = null;
            var t = new Thread(() =>
            {
                try
                {
                    var player = new PlayerControls
                    {
                        Seek = ms => { seeks++; seekedTo = ms; },
                        PlayPause = () => plays++,
                        Previous = () => prev++,
                        Next = () => next++
                    };
                    var w = new KaraokeWindow(new Settings(), () => (0.0, false), player);
                    w.Controls.Seek(42000);
                    w.Controls.PlayPause();
                    w.Controls.Previous();
                    w.Controls.Next();
                    w.Close();
                }
                catch (Exception e) { failure = e; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            Assert.Null(failure);
            Assert.Equal(1, seeks);
            Assert.Equal(42000, seekedTo);
            Assert.Equal(1, plays);
            Assert.Equal(1, prev);
            Assert.Equal(1, next);
        }
    }
}
