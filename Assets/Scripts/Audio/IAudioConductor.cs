// IAudioConductor.cs — 音频控制器接口，供其他模块通过ServiceLocator获取
using StarPipe.Core;

namespace StarPipe.Audio
{
    public interface IAudioConductor
    {
        /// <summary>当前歌曲时间（秒），基于dspTime</summary>
        double SongTime { get; }

        /// <summary>是否正在播放</summary>
        bool IsPlaying { get; }

        /// <summary>当前加载的歌曲数据</summary>
        SongData CurrentSongData { get; }

        /// <summary>加载MIDI文件并准备音频</summary>
        void LoadSong(string midiFileName);

        /// <summary>开始播放</summary>
        void Play();

        /// <summary>暂停</summary>
        void Pause();

        /// <summary>恢复播放</summary>
        void Resume();

        /// <summary>停止并重置</summary>
        void Stop();

        /// <summary>静音指定轨道</summary>
        void MuteTrack(TrackType track);

        /// <summary>取消静音指定轨道</summary>
        void UnmuteTrack(TrackType track);
    }
}
